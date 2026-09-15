using System.ComponentModel.DataAnnotations;
using authorization;

namespace WebAPI.Rooms;

public sealed class RoomOptions
{
    [Required, StringLength(40)] public string Name { get; set; } = "Комната";
    [Required] public string Theme { get; set; } = "Animals";
    [Range(5, 10)] public int UserMaxCount { get; set; } = 8;
    [Range(15, 180)] public int TurnSeconds { get; set; } = 60;
    [Range(1, 10)] public int Rounds { get; set; } = 3;
    public bool IsPrivate { get; set; }
    [StringLength(64)] public string? Password { get; set; }
}

internal sealed class RoomMember(authorization.User user, bool bot, DateTime now)
{
    public authorization.User User { get; } = user;
    public bool IsBot { get; } = bot;
    public bool Ready { get; set; } = bot;
    public bool Left { get; set; }
    public DateTime? DisconnectedAt { get; set; } = bot ? null : now;
}

internal sealed class ActiveRoom(RoomOptions options, string? passwordHash, UserId owner)
{
    public Guid Id { get; } = Guid.NewGuid();
    public RoomOptions Options { get; } = options;
    public string? PasswordHash { get; } = passwordHash;
    public UserId Owner { get; set; } = owner;
    public Dictionary<UserId, RoomMember> Members { get; } = new();
    public GameSession? Game { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime BotDue { get; set; }
    public GameResult? Result { get; set; }
    public string Stage => Game == null ? "waiting" : Game.CurrentStage == GameLogic.Enums.GameStage.Voting ? "voting" : "round";
}

public sealed record ResultPlayer(string Id, string Nickname, bool IsBot, bool IsSpy);
public sealed record GameResult(Guid GameId, Guid RoomId, string RoomName, string Theme, string Word,
    DateTime FinishedAt, string Outcome, string SpyId, string? EliminatedId, ResultPlayer[] Players,
    Dictionary<string, int> Votes);
public sealed record RoomEvent(string? Group, string Name, string? UserId = null);

public sealed class RoomRuleException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
