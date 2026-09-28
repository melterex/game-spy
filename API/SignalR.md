API for SignalR (path: `/room_hub`)

## Authentication

All hub methods require a JWT token. Use `accessTokenFactory` with the SignalR client; for WebSocket connections, the token is sent in the `access_token` query parameter.

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/room_hub", {
        accessTokenFactory: () => myJwtToken
    })
    .build();
```

## Identifiers

- Lobby methods and events use **user IDs** identifying registered users.
- Game methods and events use **slot IDs** identifying a human player or bot within the current game. Slot IDs are serialized as numeric strings, such as `"0"` or `"2"`.
- The server resolves the caller from their JWT and maps them to their game slot. Clients pass a target slot ID only when voting.

## Client-to-Server Methods

Clients call these methods with `connection.invoke(methodName, ...arguments)`. All methods return `Task` with no result payload. Parameter types below use C# notation; `bool` arguments are JavaScript booleans.

### `EnterRoom()`

Adds the current connection to the caller's room group. The caller must already belong to a room, created or joined through the HTTP API.

- **Parameters:** None.
- **Action:** Broadcasts `EnteredRoom` to the room group.

### `MakeReady(bool isReady)`

Marks the caller as ready in the lobby.

- **Parameters:** `isReady` must be `true`. Passing `false` throws a hub error with the message `Unsupported`.
- **Action:** Broadcasts `Ready` with the caller's user ID and `true`.

### `KickUser(string userId)`

Called to kick a specific user from the room.

- **Parameters:** `userId` is the user ID of the lobby member to kick.
- **Action:** Broadcasts `KickUser` on success. Throws a hub error if the target is absent or the kick is rejected.

### `StartGame()`

Called by a client to start the match.

- **Parameters:** None.
- **Action:** Broadcasts `StartGame` and schedules the turn timer. Throws a hub error with the message `Can't start game` if the lobby rejects the start.

### `MakeTurn(string message)`

Submits a message for the caller's current turn.

- **Parameters:** `message` is the player's message.
- **Action:** Broadcasts `TurnMade` using slot IDs. Throws a hub error with the message `Not your turn` if the caller's slot is not active.

### `MakeReadyEndVote(bool isReady)`

Changes whether the caller is ready to end voting early.

- **Parameters:** `isReady` is the desired readiness state (`true` or `false`).
- **Action:** Broadcasts `UserEarlyVoteStatusChange` when the state changes. If everyone is ready, schedules voting to end in 10 seconds and broadcasts `ChangeVoteEnd`. If a player withdraws readiness while everyone was ready, restores the original five-minute voting deadline and broadcasts the remaining time. Sending the existing readiness state has no effect.
- `VoteFinish` is sent when the voting timer expires.

### `MakeVote(string slotId)`

Casts or changes the caller's vote during the voting phase.

- **Parameters:** `slotId` is the target's slot ID as a string. It may identify a human player or bot in the current game.
- **Action:** Broadcasts `VoteChange`. Malformed or unknown slot IDs are rejected with the hub error `Id incorrect`. Voting outside the voting phase or for the caller's own slot is rejected.

```javascript
await connection.invoke("MakeVote", "2");
```

## Server-to-Client Events

Clients listen with `connection.on(eventName, handler)`. Payload arguments are positional and are sent to clients in the room group, including the caller.

### `UserEarlyVoteStatusChange(string slotId, bool isReady)`

Triggered when a player's readiness to end voting changes.

- `slotId`: The player's slot ID.
- `isReady`: Whether the player is ready to end voting early.

### `ChangeVoteEnd(number secondsToEnd)`

Triggered when the voting deadline changes.

- `secondsToEnd`: A JSON number containing the remaining seconds. It is `10` when everyone becomes ready, or the remaining time until the original deadline when readiness is withdrawn.

### `EnteredRoom(string id, string nickname)`

Triggered when a connection joins the room group through `EnterRoom`.

- `id`: The joined user's user ID.
- `nickname`: The user's display name.

### `Ready(string id, bool isReady)`

Triggered when a player marks themselves ready in the lobby.

- `id`: The player's user ID.
- `isReady`: Always `true` in the current implementation.

### `KickUser(string userId)`

Triggered when a user is forcefully removed from the lobby.

- `userId`: The user ID of the kicked player.

### `StartGame()`

Triggered when the lobby transitions into the active game phase.

- **Payload:** None.

### `TurnMade(string slotId, bool hasMessage, string message, bool hasNextSlot, string nextSlotId)`

Triggered when a turn is completed or times out.

- `slotId`: The slot ID of the player whose turn ended.
- `hasMessage`: `true` for a submitted message; `false` for the timeout event.
- `message`: The submitted message, or an empty string for the timeout event.
- `hasNextSlot`: Whether another slot has a turn. `false` indicates the transition to voting.
- `nextSlotId`: The next player's slot ID, or an empty string when there is no next turn.

### `VoteChange(string[] slotIds, int[] votesForThem)`

Triggered when voting tallies change. This event has two arguments, both arrays.

- `slotIds`: The slot IDs receiving votes.
- `votesForThem`: Vote counts matching `slotIds` by index.
- Slots with no votes are omitted; treat their counts as zero when updating the UI.

### `VoteFinish(string slotIdToKick, bool wasAmogus)`

Triggered when the voting phase concludes.

- `slotIdToKick`: The slot ID of the player voted out, or `"tie"` when the result is a tie.
- `wasAmogus`: Whether the player voted out was the spy. It is `false` for a tie.
