function findPlayer(slotId) {
    return votingData.players?.find(p => String(p.slotId) === String(slotId));
}

function getPlayerNickname(slotId) {
    const player = findPlayer(slotId);
    if (player?.nickname) return player.nickname;

    const fromVotes = lastCountVotes.find(p => String(p.slotId) === String(slotId));
    return fromVotes?.nickname ?? null;
}

function resolveSpyInfo(spySlotId, kickedSlotId, civiliansWon) {
    if (spySlotId && getPlayerNickname(spySlotId)) {
        return { known: true, nickname: getPlayerNickname(spySlotId) };
    }

    if (civiliansWon && kickedSlotId !== 'tie') {
        return {
            known: true,
            nickname: getPlayerNickname(kickedSlotId) ?? 'неизвестный игрок'
        };
    }

    if (votingData.isAmogus) {
        return {
            known: true,
            nickname: window.myNickname ?? getPlayerNickname(window.mySlotId) ?? 'вы'
        };
    }

    if (kickedSlotId !== 'tie') {
        const remaining = (votingData.players ?? []).filter(
            p => String(p.slotId) !== String(kickedSlotId)
        );

        if (remaining.length === 1) {
            return { known: true, nickname: remaining[0].nickname };
        }
    }

    return { known: false };
}

function buildVoteResult(slotIdToKick, civiliansWon, spySlotId) {
    const isSpy = votingData.isAmogus === true;
    const kickedSlotId = slotIdToKick;
    const spyInfo = resolveSpyInfo(spySlotId, kickedSlotId, civiliansWon);
    const kickedNickname = kickedSlotId === 'tie'
        ? null
        : (getPlayerNickname(kickedSlotId) ?? 'игрок');

    if (kickedSlotId === 'tie') {
        return {
            title: 'Ничья',
            message: 'Голоса разделились. Раунд завершён без выгнания.',
            isWin: null
        };
    }

    if (civiliansWon) {
        return {
            title: isSpy ? 'Поражение' : 'Победа!',
            message: isSpy
                ? `Вас выгнали. Шпионом был ${spyInfo.nickname}.`
                : `Мирные жители победили! Шпионом был ${spyInfo.nickname}.`,
            isWin: !isSpy
        };
    }

    if (isSpy) {
        return {
            title: 'Победа!',
            message: `Вы победили! Вы были шпионом. Выгнали не вас.`,
            isWin: true
        };
    }

    const message = spyInfo.known
        ? `Шпион победил! Шпионом был ${spyInfo.nickname}. Выгнали ${kickedNickname}.`
        : `Шпион победил! Выгнали ${kickedNickname} — он не был шпионом.`;

    return {
        title: 'Поражение',
        message,
        isWin: false
    };
}

function showVoteResult(slotIdToKick, civiliansWon, spySlotId) {
    const overlay = document.getElementById('voteResultOverlay');
    const titleEl = document.getElementById('voteResultTitle');
    const messageEl = document.getElementById('voteResultMessage');
    const modal = document.querySelector('.vote-result-modal');

    if (!overlay || !titleEl || !messageEl) {
        goToRoomAfterVoting();
        return;
    }

    const result = buildVoteResult(slotIdToKick, civiliansWon, spySlotId);

    titleEl.textContent = result.title;
    messageEl.textContent = result.message;

    if (modal) {
        modal.classList.remove('vote-result-win', 'vote-result-lose', 'vote-result-tie');
        if (result.isWin === true) {
            modal.classList.add('vote-result-win');
        } else if (result.isWin === false) {
            modal.classList.add('vote-result-lose');
        } else {
            modal.classList.add('vote-result-tie');
        }
    }

    overlay.classList.remove('hidden');
}

function hideVoteResultAndGoToRoom() {
    const overlay = document.getElementById('voteResultOverlay');
    if (overlay) {
        overlay.classList.add('hidden');
    }
    goToRoomAfterVoting();
}

function bindVoteResultModal() {
    const btn = document.getElementById('voteResultBtn');
    if (!btn || btn.dataset.bound) return;

    btn.dataset.bound = '1';
    btn.addEventListener('click', hideVoteResultAndGoToRoom);
}
