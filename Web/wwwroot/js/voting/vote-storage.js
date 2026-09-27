function getVoteStorageKey() {
    return `voting_my_vote_${window.mySlotId ?? 'unknown'}`;
}

function saveMyVote(slotId) {
    sessionStorage.setItem(getVoteStorageKey(), String(slotId));
}

function clearMyVote() {
    sessionStorage.removeItem(getVoteStorageKey());
}

function restoreMyVote(players) {
    const savedVote = sessionStorage.getItem(getVoteStorageKey());
    if (!savedVote) return;

    const playerExists = players?.some(p => String(p.slotId) === String(savedVote));
    if (playerExists) {
        myCurrentVote = savedVote;
    } else {
        clearMyVote();
    }
}
