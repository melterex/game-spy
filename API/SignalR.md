API for WebSocket: (path: `/room_hub`)

## Authentification
You must pass JWT token in `access_token` propery in query. Like that:
```
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/room_hub", {
        accessTokenFactory: () => myJwtToken 
    })
    .build();
```

## Client-to-Server Methods
*Clients must call (`invoke`) these methods to perform actions on the server.*

### `EnterRoom()`
Called by a client when they successfully connect to the room.
* **Parameters:** None
* **Action:** Broadcasts `EnteredRoom` to all clients in the room.

### `MakeReady(isReady: boolean)`
Called by a client to toggle their ready state in the lobby.
* **Parameters:**
    * `isReady` (boolean): The desired ready state.
* **Action:** Broadcasts `Ready` to all clients in the room.

### `KickUser(userId: string)`
Called to kick a specific user from the room.
* **Parameters:**
    * `userId` (string): The ID of the user to kick.
* **Action:** Broadcasts `KickUser` to all clients in the room.

### `StartGame()`
Called by a client to start the match.
* **Parameters:** none
* **Action:** Broadcasts `StartGame` to all clients in the room.

### `MakeTurn(message: string)`
Called by a player to execute their turn/action.
* **Parameters:** `message` (string)
* **Action:** Broadcasts `TurnMade` to all clients.

### `MakeVote(userId: string)`
Called by a player during the voting phase to cast a vote against someone.
* **Parameters:**
    * `userId` (string): The ID of the player being voted for.
* **Action:** Broadcasts `VoteChange` to all clients.

---

## Server-to-Client Events
*Clients must listen (`on`) for these events to update the UI based on server state.*

### `EnteredRoom(string id, string nickname, int order)`
Triggered when a player successfully joins the room.
* **Payload:**
    * `id` (string): The unique identifier of the joined user.
    * `nickname` (string): The display name of the joined user.
    * `order` number: The turn of the player(counting from first in the array)

### `Ready(string id, bool isReady)`
Triggered when a player changes their ready state.
* **Payload:**
    * `id` (string): The user ID changing state.
    * `isReady` (boolean): Whether they are ready (`true`) or not (`false`).

### `KickUser(string userId)`
Triggered when a user is forcefully removed from the lobby.
* **Payload:**
    * `userId` (string): The ID of the user who was kicked.

### `StartGame`
Triggered when the lobby transitions into the active game phase.
* **Payload:** None

### `TurnMade(string userId, bool hasMessage, string message, bool hasNextUser, string nextUserId)`
Triggered when a user completes their turn.
* **Payload:**
    * `userId` (string): The ID of the user who just took their turn.
    * `hasMessage` (boolean): Indicates if there is an accompanying message.
    * `message` (string): message.
    * `hasNextUser` (boolean): Indicates if the turn passes to another user (`true`) or if a voting phase begins (`false`).
    * `nextUserId` (string): The ID of the next user to play.

### `VoteChange(string userId1, int newValue1, string userId2, int newValue2)`
Triggered when the voting tallies change.
* **Payload:**
    * `userId1` (string): ID of a user.
    * `newValue1` number: The new vote tally for userId1.
    * `userId2` (string): ID of another user.
    * `newValue2` number: The new vote tally for userId2.

### `VoteFinish(string userIdToKick, bool wasAmogus)`
Triggered when the voting phase concludes.
* **Payload:**
    * `userIdToKick` (string): The ID of the player voted out (or null if a tie/skip).
    * `wasAmogus` boolean: Indicates whether the ejected player had the impostor/traitor role.
