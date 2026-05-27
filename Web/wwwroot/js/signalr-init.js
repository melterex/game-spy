window.connection = null;

async function startSignalR(token) {
    window.connection = new signalR.HubConnectionBuilder()
        .withUrl("/room_hub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    // EnteredRoom(string id, string nickname)
    window.connection.on("EnteredRoom", (id, nickname) => {
        const playerExists = roomData.players.some(p => {
            const existingId = p.player?.id ?? p.id;
            return String(existingId) === String(id);
        });

        if (playerExists) {
            return;
        }

        roomData.players.push({
            player: {
                id: id.toString(),
                nickname: nickname
            },
            ready: false
        });
        console.log(roomData.players);

        renderRoom(roomData.players);
    });

    // Ready(string id, bool isReady)
    window.connection.on("Ready", (id, isReady) => {
        const player = roomData.players.find(p => {
            const existingId = p.player?.id ?? p.id;
            return String(existingId) === String(id);
        });

        if (player) {
            player.ready = isReady;
        } else {
            console.warn(`Игрок с ID ${id} не найден в текущем массиве roomData.players`);
        }
        renderRoom(roomData.players);
    });

    // KickUser(string userId)
    window.connection.on("KickUser", (userId) =>{
        //TODO
    });

    // StartGame
    window.connection.on("StartGame", () => {
        startGame();
    });

    // TurnMade(string userId, bool hasMessage, string message, bool hasNextUser, string nextUserId)
    window.connection.on("TurnMade", (userId, hasMessage, message, hasNextUser, nextUserId) => {
        if (hasMessage){
            addMessage(userId, message);
        }
        if (hasNextUser){
            idTurn = nextUserId;
        }
        else{
            window.location.href = "../voting/index.html";
        }
        console.log("TurnMade", idTurn, nextUserId);
        renderRoom(roomData.players);
        startTimer(roomData.timeToMakeTurn);
    });

    // VoteChange(string userId1, int newValue1, string userId2, int newValue2)
    window.connection.on("VoteChange", (userId1, newValue1, userId2,newValue2 ) => {
        //TODO
    });

    // VoteFinish(string userIdToKick, bool wasAmogus)
    window.connection.on("VoteFinish", (userIdToKick, wasAmogus) => {
        //TODO
    });

    try {
        await window.connection.start();
        console.log("Связь с сервером установлена!");
    } catch (err) {
        console.error("Не удалось запустить SignalR:", err);
    }
}