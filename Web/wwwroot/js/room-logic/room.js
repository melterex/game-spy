let roomData;
let roomStatus;
let idTurn;


async function kickPlayer(playerId, nickname) {
    if (!window.connection) return;
    if (!confirm(`Выгнать игрока ${nickname}?`)) return;

    try {
        await window.connection.invoke("KickUser", String(playerId));
    } catch (err) {
        console.error("Ошибка кика игрока:", err);
        alert("Не удалось выгнать игрока.");
    }
}

async function loadRoomTitle(token) {
    try {
        const response = await fetch('/api/v1/rooms/my-room', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        if (!response.ok) return;

        const room = await response.json();
        window.isRoomCreator = room.isCreator === true;
        showRoomTitle(room.name);
    } catch (error) {
        console.error("Ошибка загрузки названия комнаты:", error);
    }
}

window.addEventListener('resize', () => {
    if (roomData?.players) {
        renderRoom(roomData.players);
    }
});

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('jwt_token');

    let url = `/api/v1/rooms/my-room/lobby`;

    if (window.isBackendReady && token) {
        try {
            const response = await fetch(`/api/v1/rooms/my-room/status`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (response.ok) {
                const status = await response.text();
                roomStatus = status;
                if (status === 'ingame') {
                    url = `/api/v1/rooms/my-room/game`;
                }
            }
        } catch (e) {
            console.error("Ошибка получения статуса комнаты:", e);
        }

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                roomData = await response.json();
                showRoomId(roomData.roomId);
            }
            await loadRoomTitle(token);
        } catch (err) {
            console.error("Ошибка загрузки данных комнаты:", err);
        }

        const votingJustFinished = sessionStorage.getItem('voting_finished') === '1';
        sessionStorage.removeItem('voting_finished');

        if (roomStatus === 'ingame' && roomData?.isVoting && !votingJustFinished) {
            roomLeaveIntentional = true;
            window.location.href = '../voting/index.html';
            return;
        }

        await getMyId();

        if (votingJustFinished) {
            applyLobbyAfterVoting(getPlayersAfterVoting(roomData));
        } else if (roomStatus === 'ingame' && roomData) {
            applyInGameState(roomData);
        } else if (roomData?.players) {
            const chatInput = document.getElementById('chatInput');
            if (chatInput) {
                chatInput.disabled = false;
                chatInput.placeholder = 'Ваше сообщение...';
            }

            const myProfile = roomData.players.find(p => {
                const id = p.player?.id ?? p.id;
                return String(id) === String(window.myId);
            });

            if (myProfile && myProfile.ready === true) {
                const readyBtn = document.getElementById('readyBtn');
                if (readyBtn) {
                    readyBtn.disabled = true;
                    readyBtn.classList.add('active');
                }
            }

            renderRoom(roomData.players);
        }

        await startSignalR(token);

        if (window.connection) {
            await window.connection.invoke("EnterRoom");
        }
    } else if (roomData?.players) {
        renderRoom(roomData.players);
    }

    const input = document.getElementById('chatInput');
    if (input) {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                sendMessage();
            }
        });
    }

    installRoomHistoryGuard();
});

let roomLeaveIntentional = false;
let leaveInProgress = false;

async function leaveRoom() {
    if (roomLeaveIntentional || leaveInProgress) return false;
    if (!window.connection || !window.myId) return false;

    leaveInProgress = true;
    try {
        await window.connection.invoke("KickUser", window.myId);
        return true;
    } catch (err) {
        leaveInProgress = false;
        console.error("Ошибка выхода из комнаты:", err);
        return false;
    }
}

function installRoomHistoryGuard() {
    const navigation = performance.getEntriesByType('navigation')[0];
    const alreadyGuarded = navigation?.type === 'reload' || navigation?.type === 'back_forward';
    if (alreadyGuarded) {
        history.replaceState({ roomGuard: true }, '');
    } else {
        history.pushState({ roomGuard: true }, '');
    }
}

window.addEventListener('popstate', async (event) => {
    if (event.state?.roomGuard || window.leavingByHistory) return;

    window.leavingByHistory = true;
    const left = await leaveRoom();
    if (!left) {
        window.leavingByHistory = false;
        history.pushState({ roomGuard: true }, '');
        return;
    }

    history.back();
});

async function exitRoom() {
    if (!confirm("Выйти из комнаты?")) return;
    await leaveRoom();
}