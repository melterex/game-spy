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

function getEndVoteReadyCount() {
    return Object.values(endVoteReadyByPlayer).filter(Boolean).length;
}

function updateFinishButton(countVotes) {
    const btn = document.getElementById('finishVoteBtn');
    const hint = document.getElementById('finishVoteHint');
    if (!btn) return;

    const canPress = myCurrentVote && !myEndVoteReady;

    if (myEndVoteReady) {
        btn.innerText = 'ОЖИДАНИЕ ИГРОКОВ...';
        btn.disabled = true;
        btn.classList.add('active');
    } else {
        btn.innerText = 'Готов завершить';
        btn.disabled = !canPress;
        btn.classList.remove('active');
    }

    if (hint) {
        const total = votingData.players?.length ?? 0;
        const readyCount = getEndVoteReadyCount();

        if (myEndVoteReady) {
            hint.textContent = `Вы готовы завершить. Готовы ${readyCount} из ${total}.`;
        } else if (!myCurrentVote) {
            hint.textContent = 'Выберите игрока, за которого голосуете.';
        } else {
            hint.textContent = `Нажмите, когда будете готовы завершить (${readyCount} из ${total} готовы).`;
        }
    }
}

function updatePlayerEndVoteReady(id, isReady) {
    endVoteReadyByPlayer[String(id)] = isReady;

    if (String(id) === String(window.myId)) {
        myEndVoteReady = isReady;
    }

    if (lastCountVotes.length) {
        renderVoting(lastCountVotes);
    } else {
        updateFinishButton([]);
    }
}

async function handleEndVoteReady() {
    const btn = document.getElementById('finishVoteBtn');
    if (!btn || btn.disabled || myEndVoteReady) return;
    if (!myCurrentVote) return;

    if (!window.connection) {
        console.error('Нет подключения к серверу');
        return;
    }

    try {
        btn.disabled = true;
        await window.connection.invoke('MakeReadyEndVote', true);
        myEndVoteReady = true;
        endVoteReadyByPlayer[String(window.myId)] = true;
        updateFinishButton(lastCountVotes);
        if (lastCountVotes.length) {
            renderVoting(lastCountVotes);
        }
    } catch (error) {
        console.error('Ошибка отправки готовности к завершению:', error);
        updateFinishButton(lastCountVotes);
    }
}
