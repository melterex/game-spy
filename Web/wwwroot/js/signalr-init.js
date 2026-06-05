window.connection = null;

function isRoomPage() {
    return window.location.pathname.includes('/room/');
}

function isVotingPage() {
    return window.location.pathname.includes('/voting/');
}

function goToRoomAfterVoting() {
    sessionStorage.setItem('voting_finished', '1');
    if (window.myId) {
        sessionStorage.removeItem(`voting_my_vote_${window.myId}`);
    }
    window.location.href = '../room/index.html';
}

async function startSignalR(token) {
    window.connection = new signalR.HubConnectionBuilder()
        .withUrl("/room_hub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    window.connection.on("EnteredRoom", (id, nickname) => {
        if (!isRoomPage()) return;

        const playerExists = roomData.players.some(p => {
            const existingId = p.player?.id ?? p.id;
            return String(existingId) === String(id);
        });

        if (playerExists) return;

        roomData.players.push({
            player: {
                id: id.toString(),
                nickname: nickname
            },
            ready: false
        });

        renderRoom(roomData.players);
    });

    window.connection.on("Ready", (id, isReady) => {
        if (!isRoomPage()) return;

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

    window.connection.on("StartGame", () => {
        if (!isRoomPage()) return;
        startGame();
    });

    window.connection.on("TurnMade", async (userId, hasMessage, message, hasNextUser, nextUserId) => {
        if (!isRoomPage()) return;

        if (hasMessage) {
            addMessage(userId, message);
        }
        if (hasNextUser) {
            idTurn = nextUserId;
            const data = await fetchGameData();
            if (data) {
                roomData = data;
                startTimer(data.timeToMakeTurn);
            }
            renderRoom(roomData.players);
        } else {
            window.location.href = "../voting/index.html";
        }
    });

    window.connection.on("VoteChange", (users, counts) => {
        if (!isVotingPage()) return;
        makingVote(users, counts);
    });

    window.connection.on("ReadyEndVote", (id, isReady) => {
        if (!isVotingPage()) return;
        updatePlayerEndVoteReady(id, isReady);
    });

    window.connection.on("VoteFinish", (userIdToKick, wasAmogus) => {
        if (!isVotingPage()) return;

        goToRoomAfterVoting();
    });

    try {
        await window.connection.start();
        console.log("Связь с сервером установлена!");
    } catch (err) {
        console.error("Не удалось запустить SignalR:", err);
    }
}
