const startGameBtn = document.getElementById('startGameBtn');

function showRoomId(roomId) {
    const label = document.getElementById('roomIdLabel');
    if (!label || !roomId) return;
    label.textContent = `Room #${String(roomId).slice(0, 4)}`;
}

function showRoomTitle(name) {
    const label = document.getElementById('roomIdLabel');
    if (!label || !name) return;
    label.textContent = name;
}

function humanPlayers(players) {
    return (players || []).filter(p => p.isBot !== true && p.player?.isBot !== true);
}

function lobbyBotPlaceholders(players) {
    const settings = roomData?.roomSettings;
    if (!settings || roomStatus === 'ingame') return [];

    const humans = humanPlayers(players);
    const maxPlayers = Number(settings.userMaxCount) || 0;
    const configuredBots = Number(settings.maxBots) || 0;
    const count = Math.max(0, Math.min(configuredBots, maxPlayers - humans.length));
    const bots = [];

    for (let i = 0; i < count; i++) {
        bots.push({
            id: `bot-preview-${i}`,
            nickname: `Бот_${i}`,
            isBot: true,
            ready: false
        });
    }

    return bots;
}

function playersForDisplay(players) {
    if (roomStatus === 'ingame') return players || [];
    return humanPlayers(players).concat(lobbyBotPlaceholders(players));
}

async function renderRoom(players) {
    if (roomStatus === 'ingame') {
        startGameBtn.disabled = true;
        startGameBtn.style.display = 'none';
    }
    const leftContainer = document.getElementById('leftSide');
    const rightContainer = document.getElementById('rightSide');

    if (!leftContainer || !rightContainer) return;

    leftContainer.innerHTML = '';
    rightContainer.innerHTML = '';

    if (!players || !Array.isArray(players)) {
        console.warn("Список игроков пуст или некорректен");
        return;
    }

    const displayPlayers = playersForDisplay(players);
    const half = Math.ceil(displayPlayers.length / 2);
    const leftPlayers = displayPlayers.slice(0, half);
    const rightPlayers = displayPlayers.slice(half);

    const createTag = (playerData) => {
        const tag = document.createElement('div');

        const nickname = playerData.player?.nickname ?? playerData.nickname ?? "Аноним";
        const playerId = playerData.player?.id ?? playerData.id;
        const isHisTurn = roomStatus === 'ingame' && idTurn && String(playerId) === String(idTurn);

        tag.className = `player-tag ${isHisTurn ? 'current-turn' : ''}`;

        if (roomStatus === 'waiting') {
            const isBot = playerData.isBot === true;
            const isReady = playerData.ready === true;
            const icon = isReady ? '●' : '';
            const statusClass = isReady ? 'is-ready' : 'not-ready';
            const botBadge = isBot ? '<span class="bot-badge">бот</span>' : '';

            tag.innerHTML = `
                <span class="player-name">${nickname} ${botBadge}</span>
                <div class="player-status-wrapper">
                    ${isBot ? '' : `<span class="ready-icon ${statusClass}" title="${isReady ? 'Готов' : 'Не готов'}">
                        ${icon}
                    </span>`}
                </div>
            `;
        } else {
            const botBadge = playerData.isBot ? '<span class="bot-badge">бот</span>' : '';
            tag.innerHTML = `
                <span class="player-name">
                    ${nickname} ${botBadge} ${isHisTurn ? '<span class="turn-dot">●</span>' : ''}
                </span>
            `;
        }

        tag.dataset.id = playerId;
        return tag;
    };


    leftPlayers.forEach(item => leftContainer.appendChild(createTag(item)));
    rightPlayers.forEach(item => rightContainer.appendChild(createTag(item)));

    updateTurnStatusLabel(players);
    if (typeof updateChatInputForTurn === 'function') {
        updateChatInputForTurn();
    }

    checkEveryoneReady(players);
}

function setGameData(theme, word, isAmogus) {
    const themeElem = document.getElementById('themeValue');
    const wordElem = document.getElementById('wordValue');

    if (themeElem) themeElem.innerText = theme ?? '';
    if (wordElem) wordElem.innerText = isAmogus ? 'ты шпион' : (word ?? '');
}

function checkEveryoneReady(players) {
    if (!startGameBtn) return;

    if (roomStatus === 'ingame') return;

    const humans = humanPlayers(players);
    const isEveryoneReady = humans.length > 0 && humans.every(p => p.ready === true);

    if (isEveryoneReady) {
        startGameBtn.disabled = false;
        startGameBtn.style.opacity = "1";
        startGameBtn.style.cursor = "pointer";
    } else {
        startGameBtn.disabled = true;
        startGameBtn.style.opacity = "0.5";
        startGameBtn.style.cursor = "not-allowed";
    }
}

function updateTurnStatusLabel(players) {
    const banner = document.getElementById('turnStatus');
    if (!banner) return;

    if (roomStatus !== 'ingame') {
        banner.classList.add('hidden');
        return;
    }

    banner.classList.remove('hidden');

    if (typeof idTurn === 'undefined' || !idTurn) {
        banner.innerText = "Ожидание распределения ходов...";
        banner.classList.remove('my-turn-bg');
        return;
    }

    const activePlayer = players.find(p => String(p.player?.id ?? p.id) === String(idTurn));
    const nickname = activePlayer ? (activePlayer.player?.nickname ?? activePlayer.nickname) : "Неизвестно";

    if (window.mySlotId && String(idTurn) === String(window.mySlotId)) {
        banner.innerText = "ВАШ ХОД! Напишите сообщение в чат!";
        banner.classList.add('my-turn-bg');
    } else {
        banner.innerText = `Ход игрока: ${nickname}`;
        banner.classList.remove('my-turn-bg');
    }
}