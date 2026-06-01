const startBtn = document.getElementById('startGameBtn');
if (startBtn) {
    startBtn.addEventListener('click', async () => {
        if (window.isBackendReady && window.connection) {
            try {
                startBtn.disabled = true;
                await window.connection.invoke("StartGame");
            } catch (err) {
                console.error("Ошибка старта игры:", err);
            }
        }
    });
}

async function fetchGameData() {
    const response = await fetch(`/api/v1/rooms/my-room/game`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });
    if (response.ok) {
        return await response.json();
    }
    return null;
}

function hideLobbyControls() {
    const readyBtn = document.getElementById('readyBtn');
    const themeBlock = document.getElementById('themeBlock');
    const wordBlock = document.getElementById('wordBlock');

    if (readyBtn) readyBtn.style.display = 'none';
    if (startBtn) startBtn.style.display = 'none';
    if (themeBlock) themeBlock.classList.remove('hidden');
    if (wordBlock) wordBlock.classList.remove('hidden');
}

function applyInGameState(data) {
    roomStatus = 'ingame';
    roomData = data;
    hideLobbyControls();
    idTurn = data.turnPlayerId;
    renderRoom(data.players);
    setGameData(data.theme, data.card);
    startTimer(data.timeToMakeTurn);
    loadChatMessages(data.messages);
}

async function startGame() {
    const data = await fetchGameData();
    if (data) {
        applyInGameState(data);
    }
}
