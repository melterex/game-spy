window.connection = null;

function isRoomPage() {
    return window.location.pathname.includes('/room/');
}

function isVotingPage() {
    return window.location.pathname.includes('/voting/');
}

function goToRoomAfterVoting() {
    sessionStorage.setItem('voting_finished', '1');

    if (typeof votingData !== 'undefined' && votingData.players?.length) {
        sessionStorage.setItem('lobby_players_after_vote', JSON.stringify(votingData.players));
    }

    if (window.mySlotId) {
        sessionStorage.removeItem(`voting_my_vote_${window.mySlotId}`);
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

    window.connection.on("EnteredRoom", (slotId, nickname) => {
        if (!isRoomPage()) return;

        const playerExists = roomData.players.some(p => {
            const existingSlotId = p.player?.slotId ?? p.slotId;
            return String(existingSlotId) === String(slotId);
        });

        if (playerExists) return;

        roomData.players.push({
            player: {
                slotId: slotId.toString(),
                nickname: nickname
            },
            ready: false
        });

        renderRoom(roomData.players);
    });

    window.connection.on("Ready", (slotId, isReady) => {
        if (!isRoomPage()) return;

        const player = roomData.players.find(p => {
            const existingSlotId = p.player?.slotId ?? p.slotId;
            return String(existingSlotId) === String(slotId);
        });

        if (player) {
            player.ready = isReady;
        } else {
            console.warn(`Игрок со SlotID ${slotId} не найден в текущем массиве roomData.players`);
        }
        renderRoom(roomData.players);
    });

    window.connection.on("StartGame", () => {
        if (!isRoomPage()) return;
        startGame();
    });

    window.connection.on("TurnMade", async (slotId, hasMessage, message, hasNextSlot, nextSlotId) => {
        if (!isRoomPage()) return;

        if (hasMessage) {
            addMessage(slotId, message);
        }
        if (hasNextSlot) {
            idTurn = nextSlotId;
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

    window.connection.on("UserEarlyVoteStatusChange", (slotId, isReady) => {
        if (!isVotingPage()) return;
        updatePlayerEndVoteReady(slotId, isReady);
    });

    window.connection.on("ChangeVoteEnd", (secondsToEnd) => {
        if (!isVotingPage()) return;
        startTimer(secondsToEnd);
    });

    window.connection.on("VoteFinish", (slotIdToKick, civiliansWon, spySlotId) => {
        if (!isVotingPage()) return;

        showVoteResult(slotIdToKick, civiliansWon, spySlotId);
    });

    try {
        await window.connection.start();
        console.log("Связь с сервером установлена!");
    } catch (err) {
        console.error("Не удалось запустить SignalR:", err);
    }
}
