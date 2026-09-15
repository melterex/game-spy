using System.Text.Json;
using authorization;
using BotService;
using CardsService;
using GameLogic.Enums;
using GameLogic.Services;
using Microsoft.Extensions.Configuration;
using WebAPI.Rooms;

var tests = new (string Name, System.Action Run)[]
{
    ("Five bundled themes contain 86 words", () => {
        var themes = new ThemesJsonParser().Parse();
        Check(themes.Count == 5 && themes.Values.Sum(words => words.Count) == 86);
    }),
    ("Room limits, duplicate membership, passwords and owner permissions", () => {
        using var f = new Fixture();
        Reject(() => f.Rooms.Create(f.Users[0], new() { UserMaxCount = 4 }));
        var id = f.Rooms.Create(f.Users[0], new() { IsPrivate = true, Password = "secret", UserMaxCount = 5 });
        Reject(() => f.Rooms.Create(f.Users[0], new()));
        Reject(() => f.Rooms.Enter(id, f.Users[1], "wrong"));
        for (int i = 1; i < 5; i++) f.Rooms.Enter(id, f.Users[i], "secret");
        Reject(() => f.Rooms.Enter(id, f.Users[5], "secret"));
        Reject(() => f.Rooms.Kick(f.Users[1].Id, f.Users[2].Id));
        Reject(() => f.Rooms.AddBot(f.Users[1].Id));
        Check(!f.State().GetRawText().Contains("secret"));
        f.Rooms.Kick(f.Users[0].Id, f.Users[4].Id);
        Check(f.Rooms.FindRoom(f.Users[4].Id) == null);
    }),
    ("Readiness can be cancelled and start requires five connected players", () => {
        using var f = new Fixture(); f.Lobby();
        f.Rooms.Ready(f.Users[0].Id, true); f.Rooms.Ready(f.Users[0].Id, false);
        Check(!f.State().GetProperty("players")[0].GetProperty("ready").GetBoolean());
        Reject(() => f.Rooms.Start(f.Users[0].Id));
        f.ReadyAll(); Reject(() => f.Rooms.Start(f.Users[1].Id)); f.Rooms.Start(f.Users[0].Id);
        Reject(() => f.Rooms.Start(f.Users[0].Id)); Reject(() => f.Rooms.Ready(f.Users[0].Id, false));
    }),
    ("Exactly one spy; no secret leaks; randomized complete turn order", () => {
        using var f = new Fixture(); f.Start();
        var states = f.Users.Take(5).Select(u => f.State(u.Id)).ToArray();
        Check(states.Count(s => s.GetProperty("isSpy").GetBoolean()) == 1);
        var spy = states.Single(s => s.GetProperty("isSpy").GetBoolean());
        Check(spy.GetProperty("word").ValueKind == JsonValueKind.Null);
        Check(states.Where(s => !s.GetProperty("isSpy").GetBoolean()).Select(s => s.GetProperty("word").GetString()).Distinct().Count() == 1);
        Check(f.State().GetProperty("players").EnumerateArray().Select(p => p.GetProperty("id").GetString()).Distinct().Count() == 5);
    }),
    ("Invalid and concurrent turns cannot cancel or advance another turn", () => {
        using var f = new Fixture(); f.Start();
        var current = f.Current(); var other = f.Users.First(u => !u.Id.Equals(current)).Id;
        var deadline = f.State().GetProperty("deadline").GetDateTime();
        Reject(() => f.Rooms.Turn(other, "invalid")); Reject(() => f.Rooms.Turn(current, " "));
        Check(f.State().GetProperty("deadline").GetDateTime() == deadline);
        Parallel.For(0, 10, _ => { try { f.Rooms.Turn(current, "one message"); } catch (RoomRuleException) {} });
        Check(f.State().GetProperty("messages").GetArrayLength() == 1);
    }),
    ("Three complete rounds and final timeout initialize voting", () => {
        using var f = new Fixture(); f.Start(rounds:3, seconds:15);
        for (int i = 0; i < 14; i++) f.Rooms.Turn(f.Current(), "clue");
        Check(f.State().GetProperty("round").GetInt32() == 3);
        f.Clock.Advance(16); f.Rooms.Tick();
        Check(f.State().GetProperty("stage").GetString() == "voting");
        Check(f.State().GetProperty("messages").GetArrayLength() == 15);
        Check(f.State().GetProperty("deadline").GetDateTime() == f.Clock.GetUtcNow().UtcDateTime.AddMinutes(5));
        f.Rooms.ReadyToFinish(f.Users[0].Id, true);
    }),
    ("Vote changes persist, self-votes fail, equivalent IDs identify spy", () => {
        using var f = new Fixture(); f.Start(); f.ToVoting();
        var voter = f.Users[0].Id;
        Reject(() => f.Rooms.Vote(voter, new(voter.Id)));
        f.Rooms.Vote(voter, f.Users[1].Id); f.Rooms.Vote(voter, f.Users[2].Id);
        Check(f.State().GetProperty("myVote").GetString() == f.Users[2].Id.ToString());
        var spy = f.Users.Take(5).Single(u => f.State(u.Id).GetProperty("isSpy").GetBoolean()).Id;
        foreach (var user in f.Users.Take(5).Where(u => !u.Id.Equals(spy))) f.Rooms.Vote(user.Id, new(spy.Id));
        f.Clock.Advance(301); f.Rooms.Tick();
        Check(f.State().GetProperty("result").GetProperty("outcome").GetString() == "civilians");
    }),
    ("Unanimous readiness shortens vote; cancellation restores deadline", () => {
        using var f = new Fixture(); f.Start(); f.ToVoting();
        var normal = f.State().GetProperty("deadline").GetDateTime();
        foreach (var user in f.Users.Take(5)) f.Rooms.ReadyToFinish(user.Id, true);
        Check(f.State().GetProperty("deadline").GetDateTime() == f.Clock.GetUtcNow().UtcDateTime.AddSeconds(10));
        f.Clock.Advance(2); f.Rooms.ReadyToFinish(f.Users[0].Id, false);
        Check(f.State().GetProperty("deadline").GetDateTime() == normal);
        f.Rooms.ReadyToFinish(f.Users[0].Id, true); f.Clock.Advance(11); f.Rooms.Tick();
        Check(f.State().GetProperty("stage").GetString() == "waiting");
        Check(f.State().GetProperty("result").GetProperty("outcome").GetString() == "tie");
    }),
    ("Voting cannot extend past five minutes", () => {
        using var f = new Fixture(); f.Start(); f.ToVoting();
        var normal = f.State().GetProperty("deadline").GetDateTime();
        f.Clock.Advance(296);
        foreach (var user in f.Users.Take(5)) f.Rooms.ReadyToFinish(user.Id, true);
        Check(f.State().GetProperty("deadline").GetDateTime() == normal);
    }),
    ("Spy victory and ties are counted correctly", () => {
        using var f = new Fixture(); f.Start(); f.ToVoting();
        var civilian = f.Users.Take(5).First(u => !f.State(u.Id).GetProperty("isSpy").GetBoolean());
        foreach (var user in f.Users.Take(5).Where(u => !u.Id.Equals(civilian.Id))) f.Rooms.Vote(user.Id, civilian.Id);
        f.Clock.Advance(301); f.Rooms.Tick();
        Check(f.State().GetProperty("result").GetProperty("outcome").GetString() == "spy");
        f.ReadyAll(); f.Rooms.Start(f.Users[0].Id); f.ToVoting();
        f.Rooms.Vote(f.Users[0].Id, f.Users[1].Id); f.Rooms.Vote(f.Users[1].Id, f.Users[0].Id);
        f.Clock.Advance(301); f.Rooms.Tick();
        Check(f.State().GetProperty("result").GetProperty("outcome").GetString() == "tie");
    }),
    ("Repeat games reset votes and readiness; durable history is idempotent", () => {
        using var f = new Fixture(); f.Start(); f.ToVoting(); f.Clock.Advance(301); f.Rooms.Tick();
        var result = f.State().GetProperty("result").Deserialize<GameResult>(Fixture.Json)!;
        f.Profiles.SaveResult(result); f.Profiles.SaveResult(result);
        var reopened = new ProfileStore(f.Config);
        var history = JsonSerializer.SerializeToElement(reopened.History(f.Users[0].Id));
        Check(history.GetProperty("total").GetInt32() == 1);
        Check(f.State().GetProperty("players").EnumerateArray().All(p => !p.GetProperty("ready").GetBoolean()));
        f.ReadyAll(); f.Rooms.Start(f.Users[0].Id);
        Check(f.State().GetProperty("result").ValueKind == JsonValueKind.Null);
        Check(f.State().GetProperty("messages").GetArrayLength() == 0);
    }),
    ("Reconnect grace, multiple tabs, removal and owner transfer", () => {
        using var f = new Fixture(); f.Lobby();
        var id = f.Users[0].Id;
        f.Rooms.Connected(id, "tab2"); f.Rooms.Disconnected(id, "connection0");
        f.Clock.Advance(31); f.Rooms.Tick(); Check(f.Rooms.FindRoom(id) != null);
        f.Rooms.Disconnected(id, "tab2"); f.Clock.Advance(20); f.Rooms.Connected(id, "tab3");
        f.Clock.Advance(20); f.Rooms.Tick(); Check(f.Rooms.FindRoom(id) != null);
        f.Rooms.Disconnected(id, "tab3"); f.Clock.Advance(31); f.Rooms.Tick();
        Check(f.Rooms.FindRoom(id) == null);
        Check(f.State(f.Users[1].Id).GetProperty("ownerId").GetString() == f.Users[1].Id.ToString());
    }),
    ("Leaving mid-game preserves roster but frees membership", () => {
        using var f = new Fixture(); f.Start();
        var current = f.Current(); f.Rooms.Leave(current); f.Rooms.Tick();
        Check(f.Rooms.FindRoom(current) == null);
        var remaining = f.Users.Take(5).First(u => !u.Id.Equals(current)).Id;
        var s = f.State(remaining);
        Check(s.GetProperty("players").GetArrayLength() == 5);
        Check(s.GetProperty("messages").GetArrayLength() == 1);
        Check(s.GetProperty("players").EnumerateArray().Single(p => p.GetProperty("id").GetString() == current.ToString()).GetProperty("left").GetBoolean());
    }),
    ("Bots take turns, vote and allow solo play with four bots", () => {
        using var f = new Fixture(); f.Rooms.Create(f.Users[0], new() { Rounds = 1 });
        f.Rooms.Connected(f.Users[0].Id, "solo");
        for (int i = 0; i < 4; i++) f.Rooms.AddBot(f.Users[0].Id);
        f.Rooms.Ready(f.Users[0].Id, true); f.Rooms.Start(f.Users[0].Id);
        for (int i = 0; i < 5; i++) {
            if (f.Current().Equals(f.Users[0].Id)) f.Rooms.Turn(f.Users[0].Id, "my clue");
            else { f.Clock.Advance(3); f.Rooms.Tick(); }
        }
        f.Clock.Advance(3); f.Rooms.Tick();
        Check(f.State().GetProperty("players").EnumerateArray().Where(p => p.GetProperty("isBot").GetBoolean()).All(p => p.GetProperty("readyToEndVoting").GetBoolean()));
        f.Rooms.ReadyToFinish(f.Users[0].Id, true); f.Clock.Advance(11); f.Rooms.Tick();
        Check(f.State().GetProperty("stage").GetString() == "waiting");
    }),
    ("Avatars persist and replacements update their URL", () => {
        using var f = new Fixture(); var id = f.Users[0].Id;
        f.Profiles.SetAvatar(id, [1,2,3], "image/png"); var url = f.Profiles.AvatarUrl(id);
        f.Profiles.SetAvatar(id, [4,5,6], "image/jpeg");
        var reopened = new ProfileStore(f.Config);
        Check(reopened.AvatarUrl(id) != url && reopened.Avatar(id)!.Value.Data.SequenceEqual(new byte[] {4,5,6}));
    })
};
var failures = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e}"); }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed");
return failures == 0 ? 0 : 1;

static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
static void Reject(System.Action action) { try { action(); } catch (RoomRuleException) { return; } throw new Exception("Expected rejected operation"); }

sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(int seconds) => now = now.AddSeconds(seconds);
}
sealed class Fixture : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public TestClock Clock { get; } = new();
    public authorization.User[] Users { get; } = Enumerable.Range(1, 6).Select(i => new authorization.User($"Player {i}", new(i))).ToArray();
    public IConfiguration Config { get; }
    public ProfileStore Profiles { get; }
    public RoomCoordinator Rooms { get; }
    private readonly string directory = Directory.CreateTempSubdirectory("spy-tests-").FullName;
    public Fixture()
    {
        Config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Storage:GameDatabase"] = Path.Combine(directory, "test.db") }).Build();
        Profiles = new(Config);
        var themes = new ThemesService(new ThemesJsonParser());
        Rooms = new(new GameService(new VotingService(), themes), themes, Profiles, Clock, new RuleBasedDecisionMaker());
    }
    public void Lobby(int rounds = 1, int seconds = 60)
    {
        var room = Rooms.Create(Users[0], new() { Rounds = rounds, TurnSeconds = seconds });
        for (int i = 0; i < 5; i++) {
            if (i > 0) Rooms.Enter(room, Users[i], null);
            Rooms.Connected(Users[i].Id, $"connection{i}");
        }
    }
    public void ReadyAll() { foreach (var user in Users.Take(5)) Rooms.Ready(user.Id, true); }
    public void Start(int rounds = 1, int seconds = 60) { Lobby(rounds, seconds); ReadyAll(); Rooms.Start(Users[0].Id); }
    public JsonElement State(UserId? user = null) => JsonSerializer.SerializeToElement(Rooms.Snapshot(user ?? Users[0].Id), Json);
    public UserId Current() => UserId.FromString(State().GetProperty("turnPlayerId").GetString()!);
    public void ToVoting() { while (State().GetProperty("stage").GetString() == "round") Rooms.Turn(Current(), "clue"); }
    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
}
