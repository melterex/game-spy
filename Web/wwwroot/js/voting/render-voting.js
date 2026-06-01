let votingData = {};
let myCurrentVote = null;

function buildVoteCards(players, voteStats) {
    if (!players) return [];
    return players.map(p => ({
        playerId: p.id,
        votedForHim: voteStats?.find(item => String(item.playerId) === String(p.id))?.votedForHim ?? 0,
        nickname: p.nickname
    }));
}

function renderVoting(countVotes) {
    const grid = document.getElementById('usersGrid');
    grid.innerHTML = '';

    countVotes.forEach(player => {
        const card = document.createElement('div');
        card.className = `user-card ${myCurrentVote === player.playerId ? 'selected' : ''}`;
        card.id = `card-${player.playerId}`;

        card.onclick = () => handleVoteClick(player.playerId);

        card.innerHTML = `
            <div class="avatar-placeholder">👤</div>
            <div class="user-info">
                <span class="username">${player.nickname}</span>
            </div>
            <div class="votes-counter">
                <span class="votes-count" id="votes-${player.playerId}">${player.votedForHim}</span>
                <span class="votes-label">голосов</span>
            </div>
        `;

        grid.appendChild(card);
    });
}

async function handleVoteClick(targetPlayerId) {
    if (myCurrentVote === targetPlayerId) return;

    if (myCurrentVote) {
        const oldCard = document.getElementById(`card-${myCurrentVote}`);
        if (oldCard) oldCard.classList.remove('selected');
    }

    myCurrentVote = targetPlayerId;
    const newCard = document.getElementById(`card-${targetPlayerId}`);
    if (newCard) newCard.classList.add('selected');

    if (window.connection) {
        try {
            await window.connection.invoke("MakeVote", targetPlayerId);
        } catch (error) {
            console.error("Ошибка голосования:", error);
        }
    }
}

function makingVote(users, counts) {
    const countVotes = users.map((userId, i) => ({
        playerId: userId,
        votedForHim: counts[i]
    }));
    renderVoting(buildVoteCards(votingData.players, countVotes));
}

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('jwt_token');

    try {
        await startSignalR(token);
        if (window.connection) {
            await window.connection.invoke("EnterRoom");
        }
    } catch (err) {
        console.error("Ошибка подключения SignalR:", err);
    }

    const response = await fetch(`/api/v1/rooms/my-room/game`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });

    if (!response.ok) return;

    votingData = await response.json();

    if (!votingData.isVoting) {
        window.location.href = '../room/index.html';
        return;
    }

    renderVoting(buildVoteCards(votingData.players, votingData.voteStatistics));
    startTimer(votingData.timeToVote);
});
