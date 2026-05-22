namespace RetroMask.Infrastructure.AI;

public static class PromptTemplates
{
    public static string SessionSummary(IEnumerable<string> points) =>
        $"""
        You are a skilled Agile coach. Summarize the following retrospective discussion points in 3-5 concise sentences.
        Focus on key themes, sentiment, and actionable outcomes.

        Discussion points:
        {string.Join("\n- ", points)}

        Provide a professional, constructive summary.
        """;

    public static string ClusterPoints(IEnumerable<string> points) =>
        $$"""
        Group the following retrospective discussion points into 3-6 meaningful clusters or themes.
        For each cluster, provide: a short label (2-4 words), a one-sentence summary, and list the point indices.

        Points (numbered):
        {{string.Join("\n", points.Select((p, i) => $"{i + 1}. {p}"))}}

        Respond in JSON format: [{"label":"...","summary":"...","pointIndices":[...]}]
        """;

    public static string SentimentAnalysis(string text) =>
        $$"""
        Analyze the sentiment of the following text and respond ONLY with a JSON object.
        Format: {"sentiment":"Positive|Neutral|Negative","positiveScore":0.0,"neutralScore":0.0,"negativeScore":0.0}
        All scores must sum to 1.0.

        Text: {{text}}
        """;

    public static string FullReport(string teamName, IEnumerable<string> wentWell, IEnumerable<string> toImprove, IEnumerable<string> actionItems) =>
        $"""
        Generate a comprehensive retrospective report in Markdown for team "{teamName}".

        ## Went Well
        {string.Join("\n- ", wentWell)}

        ## To Improve
        {string.Join("\n- ", toImprove)}

        ## Action Items
        {string.Join("\n- ", actionItems)}

        Include: Executive Summary, Key Insights, Recommendations, and Next Steps.
        """;

    public static string Recommendations(string context) =>
        $"""
        Based on this retrospective context, provide 3-5 specific, actionable recommendations for team improvement.
        Focus on process, communication, and technical practices.

        Context:
        {context}

        Format each recommendation as: **[Category]**: Description
        """;
}
