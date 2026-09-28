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

    if (window.isBackendReady){
        try {
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
                const createdRoomId = await response.text();
                localStorage.setItem('selected_room_id', createdRoomId);
                window.location.href = '../room/index.html';
            }
        }
        catch (error) {
            console.error(error);
        }
    }
    else{
        createdRoom = {theme: roomTheme, id: lst.toString(), usersCount: 0, userMaxCount: maxCount};
        lst += 1;
        rooms.push(createdRoom);

        console.log(rooms);
    }
    closeModal('createModal');
    await renderRooms();
}

