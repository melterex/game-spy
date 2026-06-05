let roomData;
let roomStatus;
let idTurn;

function exitRoom() {
    if (confirm("Выйти из комнаты?")) {
        localStorage.removeItem('selected_room_id');
        window.location.href = '../';
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
            }
        } catch (err) {
            console.error("Ошибка загрузки данных комнаты:", err);
        }

        const votingJustFinished = sessionStorage.getItem('voting_finished') === '1';
        sessionStorage.removeItem('voting_finished');

        if (roomStatus === 'ingame' && roomData?.isVoting && !votingJustFinished) {
            window.location.href = '../voting/index.html';
            return;
        }

        await getMyId();

        if (votingJustFinished && roomData) {
            roomStatus = 'waiting';
            const players = (roomData.players || []).map(p => ({
                player: { id: p.id, nickname: p.nickname },
                ready: false
            }));
            roomData = { players };

            const readyBtn = document.getElementById('readyBtn');
            const startGameBtn = document.getElementById('startGameBtn');
            const themeBlock = document.getElementById('themeBlock');
            const wordBlock = document.getElementById('wordBlock');
            const turnStatus = document.getElementById('turnStatus');
            const timer = document.getElementById('timer');

            if (readyBtn) {
                readyBtn.style.display = '';
                readyBtn.disabled = false;
                readyBtn.innerText = 'ГОТОВ';
                readyBtn.classList.remove('active');
            }
            if (startGameBtn) {
                startGameBtn.style.display = '';
                startGameBtn.disabled = false;
                startGameBtn.style.opacity = '1';
            }
            if (themeBlock) themeBlock.classList.add('hidden');
            if (wordBlock) wordBlock.classList.add('hidden');
            if (turnStatus) turnStatus.classList.add('hidden');
            if (timer) timer.innerText = '';

            renderRoom(players);
        } else if (roomStatus === 'ingame' && roomData) {
            applyInGameState(roomData);
        } else if (roomData?.players) {
            const myProfile = roomData.players.find(p => {
                const id = p.player?.id ?? p.id;
                return String(id) === String(window.myId);
            });

            if (myProfile && myProfile.ready === true) {
                const readyBtn = document.getElementById('readyBtn');
                if (readyBtn) {
                    readyBtn.innerText = "ОЖИДАНИЕ ИГРОКОВ...";
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
});
