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
