window.connection = null;

function isRoomPage() {
    return window.location.pathname.includes('/room/');
}

function isVotingPage() {
    return window.location.pathname.includes('/voting/');
}

function goToRoomAfterVoting() {
    sessionStorage.setItem('voting_finished', '1');
    sessionStorage.removeItem('lobby_players_after_vote');

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
    window.connection.on("KickUser", (userId) => {
        if (String(userId) === String(window.myId)) {
            if (window.leavingByHistory) return;
            window.location.href = "../";
            return;
        }

        if (!isRoomPage()) return;

        roomData.players = roomData.players.filter(player => {
            const existingId = player.player?.id ?? player.id;
            return String(existingId) !== String(userId);
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

    window.connection.on("TurnMade", async (slotId, hasMessage, message, hasNextSlot, nextSlotId) => {
        if (!isRoomPage()) return;

        if (hasNextSlot) {
            idTurn = nextSlotId;
            const data = await fetchGameData();
            if (data) {
                roomData = data;
                syncMySlotId(data.players);
                idTurn = data.turnPlayerId || nextSlotId;
                loadChatMessages(data.messages);
                startTimer(data.timeToMakeTurn);
            } else if (hasMessage) {
                addMessage(slotId, message);
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

    window.connection.on("UserEarlyVoteStatusChange", (id, isReady) => {
        if (!isVotingPage()) return;
        updatePlayerEndVoteReady(id, isReady);
    });

    window.connection.on("ChangeVoteEnd", (secondsToEnd) => {
        if (!isVotingPage()) return;
        startTimer(secondsToEnd);
    });

    window.connection.on("VoteFinish", (slotIdToKick, wasAmogus) => {
        if (!isVotingPage()) return;

        showVoteResult(slotIdToKick, wasAmogus);
    });

    try {
        await window.connection.start();
        console.log("Связь с сервером установлена!");
    } catch (err) {
        console.error("Не удалось запустить SignalR:", err);
    }
}
