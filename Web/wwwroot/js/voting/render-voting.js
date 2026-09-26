function buildVoteCards(players, voteStats) {
    if (!players) return [];
    return players.map(p => ({
        playerId: p.id,
        votedForHim: voteStats?.find(item => String(item.playerId) === String(p.id))?.votedForHim ?? 0,
        nickname: p.nickname
    }));
}

function renderVoting(countVotes) {
    lastCountVotes = countVotes;
    const grid = document.getElementById('usersGrid');
    grid.innerHTML = '';

    countVotes.forEach(player => {
        const card = document.createElement('div');
        card.className = `user-card ${String(myCurrentVote) === String(player.playerId) ? 'selected' : ''}`;
        card.id = `card-${player.playerId}`;

        card.onclick = () => handleVoteClick(player.playerId);

        const isReadyToEnd = endVoteReadyByPlayer[String(player.playerId)] === true;

        card.innerHTML = `
            <div class="avatar-placeholder">👤</div>
            <div class="user-info">
                <span class="username">${player.nickname}</span>
                ${isReadyToEnd ? '<span class="end-vote-ready-badge">Готов завершить</span>' : ''}
            </div>
            <div class="votes-counter">
                <span class="votes-count" id="votes-${player.playerId}">${player.votedForHim}</span>
                <span class="votes-label">голосов</span>
            </div>
        `;

        grid.appendChild(card);
    });

    updateFinishButton(countVotes);
}
