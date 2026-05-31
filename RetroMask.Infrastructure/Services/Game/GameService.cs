using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Game;
using RetroMask.Application.Services.Game;
using RetroMask.Domain.Entities.Game;

namespace RetroMask.Infrastructure.Services.Game;

public class GameService : IGameService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ISessionBroadcaster _broadcaster;

    private static readonly Dictionary<string, (string Title, string Description)> GameCatalogue = new()
    {
        ["two-truths-one-lie"] = ("Two Truths and a Lie", "Each participant shares two truths and one lie. Others guess which is the lie."),
        ["word-association"] = ("Word Association", "A word is shown. Everyone types the first word that comes to mind."),
        ["emoji-mood"] = ("Emoji Mood Check", "Describe your current mood using a single emoji. Explain why."),
        ["desert-island"] = ("Desert Island", "If you were stranded on a desert island, what 3 items would you bring?"),
        ["superpower"] = ("Superpower Pick", "If you could have one superpower, what would it be and why?"),
        ["fun-fact"] = ("Fun Fact", "Share a fun fact about yourself that others might not know."),
        ["would-you-rather"] = ("Would You Rather", "Answer a 'Would you rather' question. Debate the choices!"),
        ["rose-thorn-bud"] = ("Rose, Thorn, Bud", "Share a Rose (positive), Thorn (challenge), and Bud (opportunity).")
    };

    public GameService(IUnitOfWork uow, ICurrentUser currentUser, IMapper mapper, ISessionBroadcaster broadcaster)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _broadcaster = broadcaster;
    }

    public Task<ApiResponse<IEnumerable<AvailableGameDto>>> GetAvailableGamesAsync(CancellationToken ct = default)
    {
        var games = GameCatalogue.Select(g => new AvailableGameDto
        {
            GameType = g.Key,
            Title = g.Value.Title,
            Description = g.Value.Description
        });
        return Task.FromResult(ApiResponse<IEnumerable<AvailableGameDto>>.Ok(games));
    }

    public async Task<ApiResponse<GameDto>> StartGameAsync(Guid sessionId, StartGameRequest request, CancellationToken ct = default)
    {
        var activeGame = await _uow.Repository<IcebreakerGame>().FirstOrDefaultAsync(
            g => g.SessionId == sessionId && !g.IsCompleted, ct);

        if (activeGame is not null)
            return ApiResponse<GameDto>.Fail("A game is already in progress for this session.");

        if (!GameCatalogue.TryGetValue(request.GameType, out var info))
            return ApiResponse<GameDto>.Fail($"Unknown game type: {request.GameType}");

        var game = new IcebreakerGame
        {
            SessionId = sessionId,
            GameType = request.GameType,
            Title = info.Title,
            Description = info.Description,
            ConfigJson = request.ConfigJson,
            StartedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<IcebreakerGame>().AddAsync(game, ct);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(sessionId, "GameStarted",
            new { GameId = game.Id, game.GameType, game.Title }, ct);

        return ApiResponse<GameDto>.Ok(_mapper.Map<GameDto>(game));
    }

    public async Task<ApiResponse<GameDto>> GetActiveGameAsync(Guid sessionId, CancellationToken ct = default)
    {
        var game = await _uow.Repository<IcebreakerGame>().Query()
            .Include(g => g.Results)
            .FirstOrDefaultAsync(g => g.SessionId == sessionId && !g.IsCompleted, ct);

        return game is null
            ? ApiResponse<GameDto>.Fail("No active game.")
            : ApiResponse<GameDto>.Ok(_mapper.Map<GameDto>(game));
    }

    public async Task<ApiResponse> SubmitAnswerAsync(Guid gameId, SubmitAnswerRequest request, CancellationToken ct = default)
    {
        var game = await _uow.Repository<IcebreakerGame>().GetByIdAsync(gameId, ct);
        if (game is null)
            return ApiResponse.Fail("Game not found.");

        if (game.IsCompleted)
            return ApiResponse.Fail("Game is already completed.");

        var existing = await _uow.Repository<GameResult>().FirstOrDefaultAsync(
            r => r.IcebreakerGameId == gameId && r.UserId == _currentUser.UserId, ct);

        if (existing is not null)
        {
            existing.Answer = request.Answer;
            existing.SubmittedAt = DateTime.UtcNow;
            _uow.Repository<GameResult>().Update(existing);
        }
        else
        {
            var result = new GameResult
            {
                IcebreakerGameId = gameId,
                UserId = _currentUser.UserId,
                Answer = request.Answer,
                Score = 1,
                IsCorrect = true,
                SubmittedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };
            await _uow.Repository<GameResult>().AddAsync(result, ct);
        }

        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(game.SessionId, "GameAnswerSubmitted",
            new { GameId = gameId, UserId = _currentUser.UserId }, ct);

        return ApiResponse.Ok("Answer submitted.");
    }

    public async Task<ApiResponse<GameDto>> CompleteGameAsync(Guid gameId, CancellationToken ct = default)
    {
        var game = await _uow.Repository<IcebreakerGame>().Query()
            .Include(g => g.Results)
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);

        if (game is null)
            return ApiResponse<GameDto>.Fail("Game not found.");

        if (game.IsCompleted)
            return ApiResponse<GameDto>.Fail("Game is already completed.");

        game.IsCompleted = true;
        game.CompletedAt = DateTime.UtcNow;
        _uow.Repository<IcebreakerGame>().Update(game);
        await _uow.SaveChangesAsync(ct);

        await _broadcaster.BroadcastToSessionAsync(game.SessionId, "GameCompleted",
            new { GameId = gameId }, ct);

        return ApiResponse<GameDto>.Ok(_mapper.Map<GameDto>(game));
    }

    public async Task<ApiResponse<IEnumerable<GameResultDto>>> GetLeaderboardAsync(Guid gameId, CancellationToken ct = default)
    {
        var results = await _uow.Repository<GameResult>().Query()
            .Where(r => r.IcebreakerGameId == gameId)
            .Include(r => r.User)
            .OrderByDescending(r => r.Score).ThenBy(r => r.SubmittedAt)
            .ToListAsync(ct);

        var dtos = results.Select((r, i) =>
        {
            var dto = _mapper.Map<GameResultDto>(r);
            dto.Rank = i + 1;
            return dto;
        });

        return ApiResponse<IEnumerable<GameResultDto>>.Ok(dtos);
    }
}
