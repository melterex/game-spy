async function handleVoteClick(targetPlayerId) {
    if (myCurrentVote === targetPlayerId) return;

    if (myCurrentVote) {
        const oldCard = document.getElementById(`card-${myCurrentVote}`);
        if (oldCard) oldCard.classList.remove('selected');
    }

    myCurrentVote = targetPlayerId;
    saveMyVote(targetPlayerId);

    const newCard = document.getElementById(`card-${targetPlayerId}`);
    if (newCard) newCard.classList.add('selected');

    if (window.connection) {
        try {
            await window.connection.invoke('MakeVote', targetPlayerId);
        } catch (error) {
            console.error('Ошибка голосования:', error);
        }
    }

    updateFinishButton(lastCountVotes);
}

function makingVote(users, counts) {
    const countVotes = users.map((userId, i) => ({
        playerId: userId,
        votedForHim: counts[i]
    }));
    renderVoting(buildVoteCards(votingData.players, countVotes));
}
