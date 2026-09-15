using System.Threading.Channels;
using authorization;
using BotService;
using GameLogic.Entities;
using GameLogic.Enums;
using GameLogic.Interfaces;

namespace WebAPI.Rooms;

// All reads and transitions share one gate: a timeout and a submitted turn cannot advance twice.
public sealed class RoomCoordinator(IGameService games, CardsService.IThemesService themes,
    ProfileStore profiles, TimeProvider clock, IDecisionMaker bots)
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, ActiveRoom> rooms = new();
    private readonly Dictionary<UserId, HashSet<string>> connections = new();
    private long nextBotId;
    public Channel<RoomEvent> Events { get; } = Channel.CreateUnbounded<RoomEvent>();
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private void Changed(ActiveRoom room, bool list = false)
    {
        Events.Writer.TryWrite(new(room.Id.ToString(), "RoomUpdated"));
        if (list) Events.Writer.TryWrite(new(null, "RoomsUpdated"));
    }

    private ActiveRoom Mine(UserId user) => rooms.Values.FirstOrDefault(r =>
        r.Members.TryGetValue(user, out var member) && !member.Left)
        ?? throw new RoomRuleException("Вы не в комнате", 404);

    private static void Lobby(ActiveRoom room)
    {
        if (room.Game != null) throw new RoomRuleException("Партия уже идёт", 409);
    }

    private static void Owner(ActiveRoom room, UserId user)
    {
        if (!room.Owner.Equals(user)) throw new RoomRuleException("Доступно только владельцу комнаты", 403);
    }

    private bool Online(UserId user) => connections.TryGetValue(user, out var ids) && ids.Count > 0;

    public Guid? FindRoom(UserId user)
    {
        lock (gate) return rooms.Values.FirstOrDefault(r => r.Members.TryGetValue(user, out var p) && !p.Left)?.Id;
    }

    public object List()
    {
        lock (gate) return rooms.Values.Where(r => r.Game == null).Select(r => new
        {
            id = r.Id, name = r.Options.Name, usersCount = r.Members.Count,
            userMaxCount = r.Options.UserMaxCount, isPrivate = r.Options.IsPrivate, theme = r.Options.Theme
        }).ToArray();
    }

    public Guid Create(authorization.User user, RoomOptions options)
    {
        lock (gate)
        {
            if (FindRoom(user.Id) != null) throw new RoomRuleException("Сначала выйдите из текущей комнаты", 409);
            if (string.IsNullOrWhiteSpace(options.Name) || options.Name.Length > 40
                || !themes.GetThemes().Contains(options.Theme) || options.UserMaxCount is < 5 or > 10
                || options.Rounds is < 1 or > 10 || options.TurnSeconds is < 15 or > 180)
                throw new RoomRuleException("Некорректные настройки комнаты");
            if (options.IsPrivate && (string.IsNullOrWhiteSpace(options.Password) || options.Password.Length is < 4 or > 64))
                throw new RoomRuleException("Пароль комнаты: от 4 до 64 символов");
            if (options.IsPrivate && System.Text.Encoding.UTF8.GetByteCount(options.Password!) > 72)
                throw new RoomRuleException("Пароль комнаты слишком длинный: максимум 72 байта UTF-8");
            var hash = options.IsPrivate ? BCrypt.Net.BCrypt.HashPassword(options.Password) : null;
            // Never expose the password through a room snapshot.
            var saved = new RoomOptions { Name = options.Name.Trim(), Theme = options.Theme,
                UserMaxCount = options.UserMaxCount, TurnSeconds = options.TurnSeconds,
                Rounds = options.Rounds, IsPrivate = options.IsPrivate };
            var room = new ActiveRoom(saved, hash, user.Id);
            room.Members.Add(user.Id, new(user, false, Now) { DisconnectedAt = Online(user.Id) ? null : Now });
            rooms.Add(room.Id, room);
            Changed(room, true);
            return room.Id;
        }
    }

    public void Enter(Guid id, authorization.User user, string? password)
    {
        lock (gate)
        {
            if (!rooms.TryGetValue(id, out var room)) throw new RoomRuleException("Комната не найдена", 404);
            if (FindRoom(user.Id) == id) return;
            if (FindRoom(user.Id) != null) throw new RoomRuleException("Сначала выйдите из текущей комнаты", 409);
            Lobby(room);
            if (room.Members.Count >= room.Options.UserMaxCount) throw new RoomRuleException("Комната заполнена", 409);
            if (room.Options.IsPrivate && (password == null || password.Length > 64
                || !BCrypt.Net.BCrypt.Verify(password, room.PasswordHash)))
                throw new RoomRuleException("Неверный пароль комнаты", 403);
            room.Members.Add(user.Id, new(user, false, Now) { DisconnectedAt = Online(user.Id) ? null : Now });
            Changed(room, true);
        }
    }

    public object Snapshot(UserId user)
    {
        lock (gate)
        {
            var room = Mine(user);
            var game = room.Game;
            var report = game != null ? games.GetVoteService(game).GetVotingReport(game).Votes : new();
            var order = game == null ? room.Members.Keys.ToList() : games.GetPlayerOrder(game);
            return new
            {
                id = room.Id, name = room.Options.Name, ownerId = room.Owner.ToString(),
                settings = room.Options, stage = room.Stage, serverTime = Now,
                deadline = game == null ? (DateTime?)null : room.Deadline,
                gameId = game?.GameId, round = game?.CurrentRound,
                turnPlayerId = game == null ? null : games.WhoseTurn(game)?.ToString(),
                theme = room.Options.Theme, isSpy = game?.PlayerCards[user].IsSpy ?? false,
                word = game == null || game.PlayerCards[user].IsSpy ? null : game.PlayerCards[user].Word,
                myVote = game?.Votes.GetValueOrDefault(user)?.ToString(),
                players = order.Select(id =>
                {
                    var p = room.Members[id];
                    return new { id = id.ToString(), nickname = p.User.Username, isBot = p.IsBot,
                        ready = p.Ready, connected = p.IsBot || Online(id), left = p.Left,
                        avatarUrl = p.IsBot ? null : profiles.AvatarUrl(id),
                        votes = report.GetValueOrDefault(id),
                        readyToEndVoting = game?.IsPlayerReadyToEndVotingDict.GetValueOrDefault(id) ?? false };
                }).ToArray(),
                messages = game?.MessagesList.Select(m => new { playerId = m.Id.ToString(), messageBody = m.MessageBody }).ToArray(),
                result = room.Result?.Players.Any(p => p.Id == user.ToString()) == true ? room.Result : null
            };
        }
    }

    public void Ready(UserId user, bool ready)
    {
        lock (gate)
        {
            var room = Mine(user);
            Lobby(room);
            room.Members[user].Ready = ready;
            Changed(room);
        }
    }

    public void AddBot(UserId user)
    {
        lock (gate)
        {
            var room = Mine(user);
            Lobby(room);
            Owner(room, user);
            if (room.Members.Count >= room.Options.UserMaxCount) throw new RoomRuleException("Комната заполнена", 409);
            var id = new UserId(--nextBotId);
            room.Members.Add(id, new(new authorization.User($"Бот {Math.Abs(id.Id)}", id), true, Now));
            Changed(room, true);
        }
    }

    public void Kick(UserId actor, UserId target)
    {
        lock (gate)
        {
            var room = Mine(actor);
            Lobby(room);
            Owner(room, actor);
            if (actor.Equals(target) || !room.Members.ContainsKey(target)) throw new RoomRuleException("Некорректный игрок");
            Remove(room, target);
        }
    }

    public void Leave(UserId user)
    {
        lock (gate) Remove(Mine(user), user);
    }

    private void Remove(ActiveRoom room, UserId user)
    {
        if (room.Game == null) room.Members.Remove(user);
        else
        {
            // Preserve the original roster/cards for a fair result, but release room membership.
            room.Members[user].Left = true;
            room.Members[user].Ready = false;
            if (room.Stage == "voting")
            {
                room.Game.IsPlayerReadyToEndVotingDict[user] = true;
                UpdateVotingDeadline(room);
            }
            else if (games.WhoseTurn(room.Game)?.Equals(user) == true) room.Deadline = Now;
        }
        Events.Writer.TryWrite(new(room.Id.ToString(), "RemovedFromRoom", user.ToString()));
        var humans = room.Members.Values.Where(p => !p.IsBot && !p.Left).ToArray();
        if (humans.Length == 0)
        {
            if (room.Game != null) games.RemoveGameSession(room.Game.GameId);
            rooms.Remove(room.Id);
        }
        else if (room.Owner.Equals(user)) room.Owner = humans[0].User.Id;
        Changed(room, true);
    }

    public void Start(UserId user)
    {
        lock (gate)
        {
            var room = Mine(user);
            Lobby(room);
            Owner(room, user);
            if (room.Members.Count < 5 || room.Members.Values.Any(p => !p.Ready || (!p.IsBot && !Online(p.User.Id))))
                throw new RoomRuleException("Нужно от 5 игроков, все должны быть подключены и готовы");
            var id = games.CreateGameSession(room.Members.Keys.ToList(),
                new GameSettings { Theme = room.Options.Theme, TotalRounds = room.Options.Rounds });
            room.Game = games.GetGameSessionById(id);
            room.Result = null;
            room.Deadline = Now.AddSeconds(room.Options.TurnSeconds);
            room.BotDue = Now.AddSeconds(2);
            Changed(room, true);
        }
    }

    public void Turn(UserId user, string message)
    {
        lock (gate)
        {
            var room = Mine(user);
            if (room.Stage != "round" || games.WhoseTurn(room.Game!)?.Equals(user) != true)
                throw new RoomRuleException("Сейчас не ваш ход", 409);
            if (Now >= room.Deadline) throw new RoomRuleException("Время хода истекло", 409);
            if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
                throw new RoomRuleException("Сообщение должно содержать от 1 до 1000 символов");
            Advance(room, message.Trim());
        }
    }

    private void Advance(ActiveRoom room, string message)
    {
        games.MessageReceived(room.Game!, message);
        var now = Now;
        room.BotDue = now.AddSeconds(2);
        if (room.Stage == "voting")
        {
            room.Game!.VotingStartTime = now;
            room.Deadline = now.AddMinutes(5);
            foreach (var p in room.Members.Where(p => p.Value.Left)) room.Game.IsPlayerReadyToEndVotingDict[p.Key] = true;
        }
        else
        {
            room.Game!.CurrentTurnStartTime = now;
            room.Deadline = now.AddSeconds(room.Options.TurnSeconds);
            if (room.Members[games.WhoseTurn(room.Game)].Left) room.Deadline = now;
        }
        Changed(room);
    }

    private ActiveRoom Voting(UserId user)
    {
        var room = Mine(user);
        if (room.Stage != "voting" || Now >= room.Deadline) throw new RoomRuleException("Голосование недоступно", 409);
        return room;
    }

    public void Vote(UserId user, UserId target)
    {
        lock (gate)
        {
            var room = Voting(user);
            if (user.Equals(target) || !room.Members.ContainsKey(target)) throw new RoomRuleException("Выберите другого участника партии");
            games.GetVoteService(room.Game!).Vote(room.Game!, user, target);
            Changed(room);
        }
    }

    public void ReadyToFinish(UserId user, bool ready)
    {
        lock (gate)
        {
            var room = Voting(user);
            games.GetVoteService(room.Game!).SetPlayerReadyToEndVoting(room.Game!, user, ready);
            UpdateVotingDeadline(room);
            Changed(room);
        }
    }

    private void UpdateVotingDeadline(ActiveRoom room)
    {
        var game = room.Game!;
        var normalEnd = game.VotingStartTime.AddMinutes(5);
        var allReady = games.GetVoteService(game).IsEveryoneReadyToEndVoting(game);
        if (allReady && !game.IsUsingExtraTime)
        {
            room.Deadline = Now.AddSeconds(10) < normalEnd ? Now.AddSeconds(10) : normalEnd;
            game.ExtraTime = room.Deadline;
        }
        else if (!allReady) room.Deadline = normalEnd;
        game.IsUsingExtraTime = allReady;
    }

    private BotService.GameContext BotContext(ActiveRoom room, UserId id)
    {
        var game = room.Game!;
        var card = game.PlayerCards[id];
        return new(id, card.IsSpy, card.Word,
            room.Members.Select(p => new BotService.Player(p.Value.User.Username, p.Key)).ToList(),
            game.MessagesList.Select(m => new BotService.Message(m.MessageBody, m.Id)).ToList());
    }

    public void Tick()
    {
        lock (gate)
        {
            foreach (var room in rooms.Values.ToArray())
            {
                foreach (var p in room.Members.Values.Where(p => !p.IsBot && !p.Left
                    && p.DisconnectedAt.HasValue && Now - p.DisconnectedAt.Value >= TimeSpan.FromSeconds(30)).ToArray())
                    Remove(room, p.User.Id);
                if (!rooms.ContainsKey(room.Id) || room.Game == null) continue;
                if (room.Stage == "round")
                {
                    var id = games.WhoseTurn(room.Game);
                    if (Now >= room.Deadline) Advance(room, "Ход пропущен");
                    else if (room.Members[id].IsBot && Now >= room.BotDue) Advance(room, bots.MakeMessage(BotContext(room, id)));
                }
                else if (Now >= room.Deadline) Finish(room);
                else if (Now >= room.BotDue)
                {
                    foreach (var p in room.Members.Where(p => p.Value.IsBot && !room.Game.Votes.ContainsKey(p.Key)))
                    {
                        var voteService = games.GetVoteService(room.Game);
                        voteService.Vote(room.Game, p.Key, bots.MakeVote(BotContext(room, p.Key)));
                        voteService.SetPlayerReadyToEndVoting(room.Game, p.Key, true);
                    }
                    room.BotDue = DateTime.MaxValue;
                    UpdateVotingDeadline(room);
                    Changed(room);
                }
            }
        }
    }

    private void Finish(ActiveRoom room)
    {
        var game = room.Game!;
        var voting = games.GetVoteService(game);
        var outcome = voting.SummarizeResults(game);
        var votes = voting.GetVotingReport(game).Votes;
        var spy = game.PlayerCards.Single(p => p.Value.IsSpy).Key;
        var result = new GameResult(game.GameId, room.Id, room.Options.Name, room.Options.Theme, game.CurrentWord,
            Now, outcome == VotingResults.Tie ? "tie" : outcome == VotingResults.CivilianWins ? "civilians" : "spy",
            spy.ToString(), outcome == VotingResults.Tie ? null : votes.MaxBy(p => p.Value).Key.ToString(),
            room.Members.Select(p => new ResultPlayer(p.Key.ToString(), p.Value.User.Username, p.Value.IsBot,
                game.PlayerCards[p.Key].IsSpy)).ToArray(), votes.ToDictionary(p => p.Key.ToString(), p => p.Value));
        // Persist first. A transient storage failure is retried, without losing the completed game.
        profiles.SaveResult(result);
        game.VotingEnded = true;
        room.Result = result;
        games.RemoveGameSession(game.GameId);
        room.Game = null;
        foreach (var p in room.Members.Where(p => p.Value.Left).ToArray()) room.Members.Remove(p.Key);
        foreach (var p in room.Members.Values) p.Ready = p.IsBot;
        Changed(room, true);
    }

    public void Connected(UserId user, string connection)
    {
        lock (gate)
        {
            if (!connections.TryGetValue(user, out var ids)) connections[user] = ids = new();
            ids.Add(connection);
            if (FindRoom(user) is { } id)
            {
                rooms[id].Members[user].DisconnectedAt = null;
                Changed(rooms[id]);
            }
        }
    }

    public void Disconnected(UserId user, string connection)
    {
        lock (gate)
        {
            if (connections.TryGetValue(user, out var ids))
            {
                ids.Remove(connection);
                if (ids.Count == 0) connections.Remove(user);
            }
            if (!Online(user) && FindRoom(user) is { } id)
            {
                rooms[id].Members[user].DisconnectedAt = Now;
                Changed(rooms[id]);
            }
        }
    }

    public string[] Connections(UserId user)
    {
        lock (gate) return connections.TryGetValue(user, out var ids) ? ids.ToArray() : [];
    }

    public void ProfileChanged(UserId user)
    {
        lock (gate) { if (FindRoom(user) is { } id) Changed(rooms[id]); }
    }
}
