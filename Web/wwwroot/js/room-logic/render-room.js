const startGameBtn = document.getElementById('startGameBtn');

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

    const half = Math.ceil(players.length / 2);
    const leftPlayers = players.slice(0, half);
    const rightPlayers = players.slice(half);

    const createTag = (playerData) => {
        const tag = document.createElement('div');

        const nickname = playerData.player?.nickname ?? playerData.nickname ?? "Аноним";
        const playerId = playerData.player?.id ?? playerData.id;
        const isHisTurn = roomStatus === 'ingame' && (typeof idTurn !== 'undefined') && playerId === idTurn;

        tag.className = `player-tag ${isHisTurn ? 'current-turn' : ''}`;

        if (roomStatus === 'waiting') {
            const isReady = playerData.ready === true;
            const icon = isReady ? '●' : '';
            const statusClass = isReady ? 'is-ready' : 'not-ready';

            tag.innerHTML = `
                <span class="player-name">${nickname}</span>
                <div class="player-status-wrapper">
                    <span class="ready-icon ${statusClass}" title="${isReady ? 'Готов' : 'Не готов'}">
                        ${icon}
                    </span>
                </div>
            `;
        } else {
            tag.innerHTML = `
                <span class="player-name">
                    ${nickname} ${isHisTurn ? '<span class="turn-dot">●</span>' : ''}
                </span>
            `;
        }

        tag.dataset.id = playerId;
        return tag;
    };


    leftPlayers.forEach(item => leftContainer.appendChild(createTag(item)));
    rightPlayers.forEach(item => rightContainer.appendChild(createTag(item)));

    updateTurnStatusLabel(players);

    checkEveryoneReady(players);
}

function setGameData(theme, word) {
    const themeElem = document.getElementById('themeValue');
    const wordElem = document.getElementById('wordValue');

    if (themeElem) themeElem.innerText = theme ?? '';
    if (wordElem) wordElem.innerText = word ?? '';
}

function checkEveryoneReady(players) {
    if (!startGameBtn) return;

    if (roomStatus === 'ingame') return;

    const isEveryoneReady = players.length > 0 && players.every(p => p.ready === true);

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

    const activePlayer = players.find(p => (p.player?.id ?? p.id) === idTurn);
    const nickname = activePlayer ? (activePlayer.player?.nickname ?? activePlayer.nickname) : "Неизвестно";

    if (idTurn === window.myId) {
        banner.innerText = "ВАШ ХОД! Напишите сообщение в чат!";
        banner.classList.add('my-turn-bg');
    } else {
        banner.innerText = `Ход игрока: ${nickname}`;
        banner.classList.remove('my-turn-bg');
    }
}