function findPlayer(playerId) {
    return votingData.players?.find(p => String(p.id) === String(playerId));
}

function getPlayerNickname(playerId) {
    const player = findPlayer(playerId);
    if (player?.nickname) return player.nickname;

    const fromVotes = lastCountVotes.find(p => String(p.playerId) === String(playerId));
    return fromVotes?.nickname ?? null;
}

function extractIdFromBrokenPayload(rawId) {
    const str = String(rawId).trim();

    const kvpMatch = str.match(/^\[(\d+),\s*\d+\]$/);
    if (kvpMatch) return kvpMatch[1];

    if (/^\d+$/.test(str)) return str;

    return null;
}

function resolveKickedPlayerId(userIdToKick) {
    if (userIdToKick === 'tie') return 'tie';

    if (getPlayerNickname(userIdToKick)) {
        return userIdToKick;
    }

    const extractedId = extractIdFromBrokenPayload(userIdToKick);
    if (extractedId && getPlayerNickname(extractedId)) {
        return extractedId;
    }

    if (lastCountVotes.length) {
        const maxVotes = Math.max(...lastCountVotes.map(p => p.votedForHim));
        const leaders = lastCountVotes.filter(p => p.votedForHim === maxVotes);
        if (leaders.length === 1) {
            return leaders[0].playerId;
        }
    }

    return userIdToKick;
}

function resolveSpyInfo(spyPlayerId, kickedPlayerId, civiliansWon) {
    if (spyPlayerId && getPlayerNickname(spyPlayerId)) {
        return { known: true, nickname: getPlayerNickname(spyPlayerId) };
    }

    if (civiliansWon && kickedPlayerId !== 'tie') {
        return {
            known: true,
            nickname: getPlayerNickname(kickedPlayerId) ?? 'неизвестный игрок'
        };
    }

    if (votingData.isAmogus) {
        return {
            known: true,
            nickname: window.myNickname ?? getPlayerNickname(window.myId) ?? 'вы'
        };
    }

    if (kickedPlayerId !== 'tie') {
        const remaining = (votingData.players ?? []).filter(
            p => String(p.id) !== String(kickedPlayerId)
        );

        if (remaining.length === 1) {
            return { known: true, nickname: remaining[0].nickname };
        }
    }

    return { known: false };
}

function buildVoteResult(userIdToKick, civiliansWon, spyPlayerId) {
    const isSpy = votingData.isAmogus === true;
    const kickedPlayerId = resolveKickedPlayerId(userIdToKick);
    const spyInfo = resolveSpyInfo(spyPlayerId, kickedPlayerId, civiliansWon);
    const kickedNickname = kickedPlayerId === 'tie'
        ? null
        : (getPlayerNickname(kickedPlayerId) ?? 'игрок');

    if (kickedPlayerId === 'tie') {
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

function showVoteResult(userIdToKick, civiliansWon, spyPlayerId) {
    const overlay = document.getElementById('voteResultOverlay');
    const titleEl = document.getElementById('voteResultTitle');
    const messageEl = document.getElementById('voteResultMessage');
    const modal = document.querySelector('.vote-result-modal');

    if (!overlay || !titleEl || !messageEl) {
        goToRoomAfterVoting();
        return;
    }

    const result = buildVoteResult(userIdToKick, civiliansWon, spyPlayerId);

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
