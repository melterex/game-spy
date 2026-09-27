function buildVoteCards(players, voteStats) {
    if (!players) return [];
    return players.map(p => ({
        slotId: p.slotId,
        votedForHim: voteStats?.find(item => String(item.slotId) === String(p.slotId))?.votedForHim ?? 0,
        nickname: p.nickname
    }));
}

function renderVoting(countVotes) {
    lastCountVotes = countVotes;
    const grid = document.getElementById('usersGrid');
    grid.innerHTML = '';

    countVotes.forEach(player => {
        const card = document.createElement('div');
        card.className = `user-card ${String(myCurrentVote) === String(player.slotId) ? 'selected' : ''}`;
        card.id = `card-${player.slotId}`;

        card.onclick = () => handleVoteClick(player.slotId);

        const isReadyToEnd = endVoteReadyByPlayer[String(player.slotId)] === true;

        card.innerHTML = `
            <div class="avatar-placeholder">👤</div>
            <div class="user-info">
                <span class="username">${player.nickname}</span>
                ${isReadyToEnd ? '<span class="end-vote-ready-badge">Готов завершить</span>' : ''}
            </div>
            <div class="votes-counter">
                <span class="votes-count" id="votes-${player.slotId}">${player.votedForHim}</span>
                <span class="votes-label">голосов</span>
            </div>
        `;

        grid.appendChild(card);
    });

    updateFinishButton(countVotes);
}
