async function renderRoom(players) {
    let isInGame = false;
    try{
        const response = await fetch(`/api/v1/rooms/my-room/status`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`            }
        });
        if (response.ok) {
            let vsp = await response.text();
            if (vsp === 'ingame'){
                isInGame = true;
            }
        }
    }
    catch(err){}

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
        tag.className = 'player-tag';

        const nickname = playerData.player?.nickname ?? playerData.nickname ?? "Аноним";
        const playerId = playerData.player?.id ?? playerData.id;
        if (!isInGame) {
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
        }
        else{
            tag.innerHTML = `<span class="player-name">${nickname}</span>`;
        }

        tag.dataset.id = playerId;
        return tag;
    };

    leftPlayers.forEach(item => leftContainer.appendChild(createTag(item)));
    rightPlayers.forEach(item => rightContainer.appendChild(createTag(item)));
    checkEveryoneReady(players);
}

function setGameData(theme, word) {
    const themeElem = document.getElementById('themeValue');
    const wordElem = document.getElementById('wordValue');

    if (themeElem) themeElem.innerText = theme ?? '';
    if (wordElem) wordElem.innerText = word ?? '';
}

function checkEveryoneReady(players) {
    const startGameBtn = document.getElementById('startGameBtn');
    if (!startGameBtn) return;

    // Проверяем, что в комнате есть хотя бы кто-то, и у ВСЕХ статус ready равен true
    const isEveryoneReady = players.length > 0 && players.every(p => p.ready === true);

    if (isEveryoneReady) {
        startGameBtn.disabled = false;
        startGameBtn.style.opacity = "1"; // Делаем кнопку яркой
        startGameBtn.style.cursor = "pointer";
    } else {
        startGameBtn.disabled = true;
        startGameBtn.style.opacity = "0.5"; // Делаем кнопку блеклой
        startGameBtn.style.cursor = "not-allowed";
    }
}