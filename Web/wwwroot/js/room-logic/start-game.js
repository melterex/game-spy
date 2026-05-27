const startBtn = document.getElementById('startGameBtn');
if (startBtn) {
    startBtn.addEventListener('click', async () => {
        if (window.isBackendReady && window.connection) {
            try {
                startBtn.disabled = true;
                await window.connection.invoke("StartGame");
            } catch (err) {
            }
        }
    });
}

async function startGame() {
    roomStatus = 'ingame';
    const url = `/api/v1/rooms/my-room/game`;
    console.log(url);
    const response = await fetch(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });
    if (response.ok) {
        roomData = await response.json();
    }

    const readyBtn = document.getElementById('readyBtn');
    const themeBlock = document.getElementById('themeBlock');
    const wordBlock = document.getElementById('wordBlock');

    if (readyBtn) readyBtn.style.display = 'none';
    if (startBtn) readyBtn.style.display = 'none';

    if (themeBlock) themeBlock.classList.remove('hidden');
    if (wordBlock) wordBlock.classList.remove('hidden');

    roomData.players.forEach(p => p.ready = "NotReady");
    renderRoom(roomData.players);
    setGameData(roomData.theme, roomData.card);
    startTimer(roomData.timeToMakeTurn);
    idTurn = roomData.turnPlayerId;
}

