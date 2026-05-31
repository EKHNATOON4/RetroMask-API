using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.AI;
using RetroMask.Application.Services.AI;
using RetroMask.Domain.Entities.AI;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.AI;

namespace RetroMask.Infrastructure.Services.AI;

public class AIService : IAIService
{
    private readonly IUnitOfWork _uow;
    private readonly IAIInsightProvider _ai;
    private readonly IMapper _mapper;
    private readonly ILogger<AIService> _logger;

    public AIService(IUnitOfWork uow, IAIInsightProvider ai, IMapper mapper, ILogger<AIService> logger)
    {
        _uow = uow;
        _ai = ai;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<AIInsightDto>>> GetSessionInsightsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var insights = await _uow.Repository<AIInsight>().Query()
            .Where(i => i.SessionId == sessionId)
            .OrderByDescending(i => i.GeneratedAt)
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<AIInsightDto>>.Ok(_mapper.Map<IEnumerable<AIInsightDto>>(insights));
    }

    public async Task<ApiResponse<AIInsightDto>> GenerateSummaryAsync(Guid sessionId, CancellationToken ct = default)
    {
        var points = await GetSessionPointTexts(sessionId, ct);
        if (!points.Any())
            return ApiResponse<AIInsightDto>.Fail("No discussion points to summarize.");

        try
        {
            var prompt = PromptTemplates.SessionSummary(points);
            var result = await _ai.GenerateSummaryAsync(prompt, ct);

            var insight = new AIInsight
            {
                SessionId = sessionId,
                InsightType = AIInsightType.Summary,
                Content = result,
                ModelUsed = "gpt-4o-mini",
                PromptUsed = prompt,
                ConfidenceScore = 0.85,
                GeneratedAt = DateTime.UtcNow
            };

            await _uow.Repository<AIInsight>().AddAsync(insight, ct);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI summary generation failed for session {SessionId}", sessionId);
            return await FallbackSummaryAsync(sessionId, points);
        }
    }

    public async Task<ApiResponse<IEnumerable<AIClusterDto>>> ClusterPointsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var points = await GetSessionPointTexts(sessionId, ct);
        if (!points.Any())
            return ApiResponse<IEnumerable<AIClusterDto>>.Fail("No discussion points to cluster.");

        try
        {
            var json = await _ai.ClusterPointsAsync(points, ct);
            var clusters = ParseClusters(sessionId, json, points);

            var existing = await _uow.Repository<AICluster>().FindAsync(c => c.SessionId == sessionId, ct);
            foreach (var e in existing)
                _uow.Repository<AICluster>().Remove(e);

            await _uow.Repository<AICluster>().AddRangeAsync(clusters, ct);
            await _uow.SaveChangesAsync(ct);

            var dtos = clusters.Select(c =>
            {
                var dto = _mapper.Map<AIClusterDto>(c);
                dto.PointIds = JsonSerializer.Deserialize<List<Guid>>(c.PointIdsJson) ?? new();
                return dto;
            });

            return ApiResponse<IEnumerable<AIClusterDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI clustering failed for session {SessionId}", sessionId);
            return await FallbackClustersAsync(sessionId, points);
        }
    }

    public async Task<ApiResponse<AIInsightDto>> AnalyzeSentimentAsync(Guid sessionId, CancellationToken ct = default)
    {
        var points = await GetSessionPointTexts(sessionId, ct);
        if (!points.Any())
            return ApiResponse<AIInsightDto>.Fail("No discussion points to analyze.");

        try
        {
            var combined = string.Join(" ", points);
            var json = await _ai.AnalyzeSentimentAsync(combined, ct);

            var parsed = JsonSerializer.Deserialize<SentimentResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var sentiment = parsed?.Sentiment?.ToLower() switch
            {
                "positive" => SentimentType.Positive,
                "negative" => SentimentType.Negative,
                _ => SentimentType.Neutral
            };

            var score = new SentimentScore
            {
                SessionId = sessionId,
                Sentiment = sentiment,
                Score = parsed?.PositiveScore - parsed?.NegativeScore ?? 0,
                PositiveScore = parsed?.PositiveScore ?? 0.33,
                NeutralScore = parsed?.NeutralScore ?? 0.34,
                NegativeScore = parsed?.NegativeScore ?? 0.33,
                ModelUsed = "gpt-4o-mini"
            };

            await _uow.Repository<SentimentScore>().AddAsync(score, ct);

            var insight = new AIInsight
            {
                SessionId = sessionId,
                InsightType = AIInsightType.Sentiment,
                Content = $"Overall sentiment: {sentiment}. Positive: {score.PositiveScore:P0}, Neutral: {score.NeutralScore:P0}, Negative: {score.NegativeScore:P0}",
                ModelUsed = "gpt-4o-mini",
                ConfidenceScore = Math.Max(score.PositiveScore, Math.Max(score.NeutralScore, score.NegativeScore)),
                GeneratedAt = DateTime.UtcNow
            };

            await _uow.Repository<AIInsight>().AddAsync(insight, ct);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI sentiment analysis failed for session {SessionId}", sessionId);
            return await FallbackSentimentAsync(sessionId, points);
        }
    }

    public async Task<ApiResponse<AIReportDto>> GenerateReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Repository<Session>().Query()
            .Include(s => s.Team)
            .Include(s => s.Phases).ThenInclude(p => p.DiscussionPoints)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return ApiResponse<AIReportDto>.Fail("Session not found.");

        var wentWell = session.Phases
            .Where(p => p.PhaseType == SessionPhaseType.WentWell)
            .SelectMany(p => p.DiscussionPoints.Select(d => d.Content)).ToList();
        var toImprove = session.Phases
            .Where(p => p.PhaseType == SessionPhaseType.ToImprove)
            .SelectMany(p => p.DiscussionPoints.Select(d => d.Content)).ToList();
        var actionItems = session.Phases
            .Where(p => p.PhaseType == SessionPhaseType.ActionItems)
            .SelectMany(p => p.DiscussionPoints.Select(d => d.Content)).ToList();

        try
        {
            var prompt = PromptTemplates.FullReport(session.Team.Name, wentWell, toImprove, actionItems);
            var markdown = await _ai.GenerateReportAsync(prompt, ct);

            var report = new AIReport
            {
                SessionId = sessionId,
                Title = $"Retrospective Report — {session.Title}",
                MarkdownContent = markdown,
                HtmlContent = null,
                ModelUsed = "gpt-4o-mini",
                GeneratedAt = DateTime.UtcNow
            };

            await _uow.Repository<AIReport>().AddAsync(report, ct);

            var insight = new AIInsight
            {
                SessionId = sessionId,
                InsightType = AIInsightType.Report,
                Content = $"Report generated: {report.Title}",
                ModelUsed = "gpt-4o-mini",
                GeneratedAt = DateTime.UtcNow
            };
            await _uow.Repository<AIInsight>().AddAsync(insight, ct);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<AIReportDto>.Ok(_mapper.Map<AIReportDto>(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI report generation failed for session {SessionId}", sessionId);
            return await FallbackReportAsync(sessionId, session.Title, session.Team.Name, wentWell, toImprove, actionItems);
        }
    }

    public async Task<ApiResponse<AIInsightDto>> GenerateRecommendationsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var points = await GetSessionPointTexts(sessionId, ct);
        if (!points.Any())
            return ApiResponse<AIInsightDto>.Fail("No discussion points to analyze.");

        try
        {
            var context = string.Join("\n- ", points);
            var result = await _ai.GenerateRecommendationsAsync(context, ct);

            var insight = new AIInsight
            {
                SessionId = sessionId,
                InsightType = AIInsightType.Recommendation,
                Content = result,
                ModelUsed = "gpt-4o-mini",
                ConfidenceScore = 0.80,
                GeneratedAt = DateTime.UtcNow
            };

            await _uow.Repository<AIInsight>().AddAsync(insight, ct);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recommendations failed for session {SessionId}", sessionId);

            var insight = new AIInsight
            {
                SessionId = sessionId,
                InsightType = AIInsightType.Recommendation,
                Content = "**[Process]**: Review recurring improvement items from past retrospectives.\n**[Communication]**: Ensure action items have clear owners and deadlines.\n**[Technical]**: Consider automating repetitive tasks identified in this session.",
                ModelUsed = "fallback",
                ConfidenceScore = 0.5,
                GeneratedAt = DateTime.UtcNow
            };

            await _uow.Repository<AIInsight>().AddAsync(insight, ct);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight), "AI unavailable — generic recommendations provided.");
        }
    }

    private async Task<List<string>> GetSessionPointTexts(Guid sessionId, CancellationToken ct)
    {
        return await _uow.Repository<DiscussionPoint>().Query()
            .Where(p => p.Phase.SessionId == sessionId)
            .Select(p => p.Content)
            .ToListAsync(ct);
    }

    private async Task<ApiResponse<AIInsightDto>> FallbackSummaryAsync(Guid sessionId, List<string> points)
    {
        var content = $"Session had {points.Count} discussion point(s). Key themes could not be extracted due to AI service unavailability. Please review the points manually.";
        var insight = new AIInsight
        {
            SessionId = sessionId,
            InsightType = AIInsightType.Summary,
            Content = content,
            ModelUsed = "fallback",
            ConfidenceScore = 0.3,
            GeneratedAt = DateTime.UtcNow
        };
        await _uow.Repository<AIInsight>().AddAsync(insight);
        await _uow.SaveChangesAsync();
        return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight), "AI unavailable — fallback summary provided.");
    }

    private async Task<ApiResponse<IEnumerable<AIClusterDto>>> FallbackClustersAsync(Guid sessionId, List<string> points)
    {
        var cluster = new AICluster
        {
            SessionId = sessionId,
            Label = "All Points",
            Summary = "AI clustering unavailable. All points grouped together.",
            ColorHex = "#6B7280",
            PointCount = points.Count,
            PointIdsJson = "[]"
        };
        await _uow.Repository<AICluster>().AddAsync(cluster);
        await _uow.SaveChangesAsync();

        var dto = _mapper.Map<AIClusterDto>(cluster);
        dto.PointIds = new List<Guid>();
        return ApiResponse<IEnumerable<AIClusterDto>>.Ok(new[] { dto }, "AI unavailable — fallback clustering.");
    }

    private async Task<ApiResponse<AIInsightDto>> FallbackSentimentAsync(Guid sessionId, List<string> points)
    {
        var insight = new AIInsight
        {
            SessionId = sessionId,
            InsightType = AIInsightType.Sentiment,
            Content = $"Sentiment analysis unavailable. {points.Count} point(s) collected. Manual review recommended.",
            ModelUsed = "fallback",
            ConfidenceScore = 0.3,
            GeneratedAt = DateTime.UtcNow
        };
        await _uow.Repository<AIInsight>().AddAsync(insight);
        await _uow.SaveChangesAsync();
        return ApiResponse<AIInsightDto>.Ok(_mapper.Map<AIInsightDto>(insight), "AI unavailable — fallback sentiment.");
    }

    private async Task<ApiResponse<AIReportDto>> FallbackReportAsync(Guid sessionId, string title, string teamName,
        List<string> wentWell, List<string> toImprove, List<string> actionItems)
    {
        var md = $"# Retrospective Report — {title}\n\n**Team:** {teamName}\n\n" +
                 $"## What Went Well ({wentWell.Count})\n{string.Join("\n", wentWell.Select(w => $"- {w}"))}\n\n" +
                 $"## To Improve ({toImprove.Count})\n{string.Join("\n", toImprove.Select(w => $"- {w}"))}\n\n" +
                 $"## Action Items ({actionItems.Count})\n{string.Join("\n", actionItems.Select(w => $"- {w}"))}\n\n" +
                 "*Note: AI-generated analysis was unavailable. This is a raw summary.*";

        var report = new AIReport
        {
            SessionId = sessionId,
            Title = $"Retrospective Report — {title}",
            MarkdownContent = md,
            ModelUsed = "fallback",
            GeneratedAt = DateTime.UtcNow
        };
        await _uow.Repository<AIReport>().AddAsync(report);
        await _uow.SaveChangesAsync();
        return ApiResponse<AIReportDto>.Ok(_mapper.Map<AIReportDto>(report), "AI unavailable — raw report generated.");
    }

    private List<AICluster> ParseClusters(Guid sessionId, string json, List<string> points)
    {
        try
        {
            var start = json.IndexOf('[');
            var end = json.LastIndexOf(']');
            if (start >= 0 && end > start)
                json = json.Substring(start, end - start + 1);

            var parsed = JsonSerializer.Deserialize<List<ClusterResult>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is null || parsed.Count == 0)
                return new List<AICluster>
                {
                    new() { SessionId = sessionId, Label = "Uncategorized", PointCount = points.Count, PointIdsJson = "[]" }
                };

            var colors = new[] { "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899" };
            return parsed.Select((c, i) => new AICluster
            {
                SessionId = sessionId,
                Label = c.Label ?? $"Cluster {i + 1}",
                Summary = c.Summary,
                ColorHex = colors[i % colors.Length],
                PointCount = c.PointIndices?.Count ?? 0,
                PointIdsJson = "[]"
            }).ToList();
        }
        catch
        {
            return new List<AICluster>
            {
                new() { SessionId = sessionId, Label = "Uncategorized", Summary = "Failed to parse clusters.", PointCount = points.Count, PointIdsJson = "[]" }
            };
        }
    }

    private class SentimentResult
    {
        public string? Sentiment { get; set; }
        public double PositiveScore { get; set; }
        public double NeutralScore { get; set; }
        public double NegativeScore { get; set; }
    }

    private class ClusterResult
    {
        public string? Label { get; set; }
        public string? Summary { get; set; }
        public List<int>? PointIndices { get; set; }
    }
}
