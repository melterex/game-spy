function toLobbyPlayers(players) {
    return (players || []).map(p => ({
        player: { id: p.id ?? p.player?.id, nickname: p.nickname ?? p.player?.nickname },
        ready: false
    }));
}

function applyLobbyAfterVoting(players) {
    roomStatus = 'waiting';
    idTurn = null;
    window.curStatus = false;

    const lobbyPlayers = toLobbyPlayers(players);
    roomData = { players: lobbyPlayers };

    const readyBtn = document.getElementById('readyBtn');
    const startGameBtn = document.getElementById('startGameBtn');
    const themeBlock = document.getElementById('themeBlock');
    const wordBlock = document.getElementById('wordBlock');
    const turnStatus = document.getElementById('turnStatus');
    const timer = document.getElementById('timer');
    const chat = document.getElementById('chatArea');
    const chatInput = document.getElementById('chatInput');
    const chatWrap = document.querySelector('.chat-input-wrap');

    if (readyBtn) {
        readyBtn.style.display = '';
        readyBtn.disabled = false;
        readyBtn.innerText = 'ГОТОВ';
        readyBtn.classList.remove('active');
    }
    if (startGameBtn) {
        startGameBtn.style.display = '';
        startGameBtn.disabled = true;
        startGameBtn.style.opacity = '0.5';
        startGameBtn.style.cursor = 'not-allowed';
    }
    if (themeBlock) themeBlock.classList.add('hidden');
    if (wordBlock) wordBlock.classList.add('hidden');
    if (turnStatus) turnStatus.classList.add('hidden');
    if (timer) timer.innerText = '';

    if (chat) {
        chat.innerHTML = '<div class="message"><span>Система:</span> Ожидание начала...</div>';
    }
    if (chatInput) {
        chatInput.value = '';
        chatInput.placeholder = 'Чат будет доступен в игре';
        chatInput.disabled = true;
    }
    if (chatWrap) {
        chatWrap.style.display = '';
    }

    renderRoom(lobbyPlayers);
}

function getPlayersAfterVoting(roomDataFromApi) {
    if (roomDataFromApi?.players?.length) {
        return roomDataFromApi.players;
    }

    const saved = sessionStorage.getItem('lobby_players_after_vote');
    sessionStorage.removeItem('lobby_players_after_vote');
    if (!saved) return [];

    try {
        return JSON.parse(saved);
    } catch {
        return [];
    }
}
