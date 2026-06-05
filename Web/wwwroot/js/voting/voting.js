document.addEventListener('DOMContentLoaded', async () => {
    ensureFinishVoteSection();

    const token = localStorage.getItem('jwt_token');

    try {
        await getMyId();
        await startSignalR(token);
        if (window.connection) {
            await window.connection.invoke('EnterRoom');
        }
    } catch (err) {
        console.error('Ошибка подключения SignalR:', err);
    }

    const response = await fetch('/api/v1/rooms/my-room/game', {
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

    restoreMyVote(votingData.players);
    renderVoting(buildVoteCards(votingData.players, votingData.voteStatistics));
    startTimer(votingData.timeToVote);
});
