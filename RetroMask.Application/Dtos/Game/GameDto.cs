namespace RetroMask.Application.Dtos.Game;

public class GameDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string GameType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ParticipantCount { get; set; }
}

public class GameResultDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public int? Score { get; set; }
    public bool IsCorrect { get; set; }
    public int Rank { get; set; }
}

public class StartGameRequest
{
    public string GameType { get; set; } = string.Empty;
    public string? ConfigJson { get; set; }
}

public class SubmitAnswerRequest
{
    public string Answer { get; set; } = string.Empty;
}

public class AvailableGameDto
{
    public string GameType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
