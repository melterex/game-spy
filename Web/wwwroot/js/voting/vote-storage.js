function getVoteStorageKey() {
    return `voting_my_vote_${window.myId ?? 'unknown'}`;
}

function saveMyVote(playerId) {
    sessionStorage.setItem(getVoteStorageKey(), String(playerId));
}

function clearMyVote() {
    sessionStorage.removeItem(getVoteStorageKey());
}

function restoreMyVote(players) {
    const savedVote = sessionStorage.getItem(getVoteStorageKey());
    if (!savedVote) return;

    const playerExists = players?.some(p => String(p.id) === String(savedVote));
    const isOwnSlot = window.mySlotId && String(savedVote) === String(window.mySlotId);
    if (playerExists && !isOwnSlot) {
        myCurrentVote = savedVote;
    } else {
        clearMyVote();
    }
}
