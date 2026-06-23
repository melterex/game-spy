function bindFinishVoteButton() {
    const btn = document.getElementById('finishVoteBtn');
    if (!btn || btn.dataset.bound) return;

    btn.dataset.bound = '1';
    btn.addEventListener('click', handleEndVoteReady);
}

function ensureFinishVoteSection() {
    if (document.getElementById('finishVoteBtn')) {
        bindFinishVoteButton();
        return;
    }

    const container = document.querySelector('.voting-container');
    if (!container) return;

    const section = document.createElement('div');
    section.id = 'finishVoteSection';
    section.className = 'finish-vote-section';
    section.innerHTML = `
        <p id="finishVoteHint" class="finish-vote-hint">Проголосуйте за игрока, чтобы подтвердить готовность завершить.</p>
        <button id="finishVoteBtn" type="button" class="finish-vote-btn" disabled>Готов завершить</button>
    `;

    container.appendChild(section);

    bindFinishVoteButton();
}

function updateFinishButton(countVotes) {
    const btn = document.getElementById('finishVoteBtn');
    const hint = document.getElementById('finishVoteHint');
    if (!btn) return;

    const canPress = myCurrentVote && !myEndVoteReady;

    if (myEndVoteReady) {
        btn.innerText = 'Отменить готовность';
        btn.disabled = false;
        btn.classList.add('active');
    } else {
        btn.innerText = 'Готов завершить';
        btn.disabled = !canPress;
        btn.classList.remove('active');
    }

    if (hint) {
        if (myEndVoteReady) {
            hint.textContent = 'Вы готовы завершить. Ожидание остальных игроков. Можно отменить готовность.';
        } else if (!myCurrentVote) {
            hint.textContent = 'Выберите игрока, за которого голосуете.';
        } else {
            hint.textContent = 'Нажмите, когда будете готовы завершить.';
        }
    }
}

function restoreEndVoteReadyState(players) {
    endVoteReadyByPlayer = {};

    players?.forEach(player => {
        endVoteReadyByPlayer[String(player.id)] = player.readyToEndVoting === true;
    });

    myEndVoteReady = endVoteReadyByPlayer[String(window.myId)] === true;
}

function updatePlayerEndVoteReady(id, isReady) {
    endVoteReadyByPlayer[String(id)] = isReady;

    if (String(id) === String(window.myId)) {
        myEndVoteReady = isReady;
    }

    updateFinishButton(lastCountVotes);

    if (lastCountVotes.length) {
        renderVoting(lastCountVotes);
    } else {
        updateFinishButton([]);
    }
}

async function handleEndVoteReady() {
    const btn = document.getElementById('finishVoteBtn');
    if (!btn || btn.disabled) return;
    if (!myCurrentVote) return;

    if (!window.connection) {
        console.error('Нет подключения к серверу');
        return;
    }

    const newReady = !myEndVoteReady;

    try {
        btn.disabled = true;
        await window.connection.invoke('MakeReadyEndVote', newReady);
    } catch (error) {
        console.error('Ошибка отправки готовности к завершению:', error);
        updateFinishButton(lastCountVotes);
    }
}
