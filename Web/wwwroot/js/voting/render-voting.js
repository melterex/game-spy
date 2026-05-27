let myCurrentVote = null;

function renderVoting(players, countVotes = [0, 0, 0, 0, 0, 0, 0 ]) {
    const grid = document.getElementById('usersGrid');
    grid.innerHTML = '';
    console.log(players);

    let i = 0;
    players.forEach(player => {
        const card = document.createElement('div');
        card.className = `user-card ${myCurrentVote === player.id ? 'selected' : ''}`;
        card.id = `card-${player.id}`;

        card.onclick = () => handleVoteClick(player.id);

        card.innerHTML = `
            <div class="avatar-placeholder">👤</div>
            <div class="user-info">
                <span class="username">${player.nickname}</span>
            </div>
            <div class="votes-counter">
                <span class="votes-count" id="votes-${player.id}">${countVotes[i]}</span>
                <span class="votes-label">голосов</span>
            </div>
        `;

        grid.appendChild(card);
        i++;
    });
}

function handleVoteClick(targetPlayerId) {
    if (myCurrentVote === targetPlayerId) return;

    if (myCurrentVote) {
        const oldCard = document.getElementById(`card-${myCurrentVote}`);
        if (oldCard) oldCard.classList.remove('selected');
    }

    myCurrentVote = targetPlayerId;
    const newCard = document.getElementById(`card-${targetPlayerId}`);
    if (newCard) newCard.classList.add('selected');

    console.log(`Отправляем на бэкенд голос за: ${targetPlayerId}`);

}


function updateVotes(votesMap) {
    for (const [playerId, count] of Object.entries(votesMap)) {
        const counterElement = document.getElementById(`votes-${playerId}`);
        if (counterElement) {
            counterElement.textContent = count;
        }
    }
}

// Первичный запуск при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    renderVoting(roomData.players);
    console.log(roomData.players);
});