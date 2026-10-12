function syncBotLimit() {
    const limitSelect = document.getElementById('playerLimit');
    const botSelect = document.getElementById('botCount');
    if (!limitSelect || !botSelect) return;

    const maxPlayers = parseInt(limitSelect.value, 10);
    const maxBots = Math.max(0, maxPlayers - 1);
    const current = parseInt(botSelect.value, 10) || 0;

    botSelect.innerHTML = '';
    for (let i = 0; i <= maxBots; i++) {
        const option = document.createElement('option');
        option.value = String(i);
        option.textContent = String(i);
        botSelect.appendChild(option);
    }
    botSelect.value = String(Math.min(current, maxBots));
}

document.addEventListener('DOMContentLoaded', () => {
    const limitSelect = document.getElementById('playerLimit');
    if (!limitSelect) return;
    limitSelect.addEventListener('change', syncBotLimit);
    syncBotLimit();
});

async function createNewRoom() {
    const selectTheme = document.getElementById('roomTheme');
    const limitSelect = document.getElementById('playerLimit');
    const botSelect = document.getElementById('botCount');

    const roomTheme = selectTheme.value;
    const maxCount = limitSelect ? parseInt(limitSelect.value, 10) : 10;
    const maxBots = botSelect ? parseInt(botSelect.value, 10) : 0;

    console.log(roomTheme);
    console.log(maxCount);

    let createdRoom = [];

    try {
        await fetchMyRoom();
        if (myRoom) {
            showAlreadyInRoom();
            return;
        }

        const response = await fetch('/api/v1/rooms', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            },
            body: JSON.stringify({
                name: '',
                theme: roomTheme,
                userMaxCount: maxCount,
                maxBots: maxBots,
            })
        });
        console.log(response);

        if (!response.ok) {
            console.error(response.status);
            alert(`Не удалось создать комнату. Ошибка: ${response.status}`);
        }
        else {
            window.location.href = '../room/index.html';
        }
    }
    catch (error) {
        console.error(error);
    }
    closeModal('createModal');
    await renderRooms();
}

