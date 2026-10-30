function openOupModal() {
    const error = document.getElementById('oupJoinError');
    if (error) {
        error.hidden = true;
        error.textContent = '';
    }
    openModal('oupModal');
    document.getElementById('joinRoomId')?.focus();
}

function showOupJoinError(message) {
    const error = document.getElementById('oupJoinError');
    if (!error) {
        alert(message);
        return;
    }
    error.hidden = false;
    error.textContent = message;
}

async function enterRoomByOup() {
    const roomId = document.getElementById('joinRoomId')?.value.trim() ?? '';
    const oup = document.getElementById('joinOup')?.value.trim() ?? '';

    if (!roomId || !oup) {
        showOupJoinError('Нужны номер комнаты и код.');
        return;
    }

    if (!/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(roomId)) {
        showOupJoinError('Номер комнаты должен быть полным идентификатором.');
        return;
    }

    await fetchMyRoom();
    if (myRoom) {
        if (isMyRoom(roomId)) {
            goToMyRoom();
            return;
        }
        showAlreadyInRoom();
        return;
    }

    try {
        const response = await fetch(`/api/v1/rooms/${roomId}/enter-by-oup`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            },
            body: JSON.stringify({ oup })
        });

        if (response.ok) {
            window.location.href = '../room/index.html';
            return;
        }

        if (response.status === 404) {
            showOupJoinError('Такой комнаты нет.');
            return;
        }

        showOupJoinError('Код не подошёл: он уже использован или истёк.');
    } catch (error) {
        console.error(error);
        showOupJoinError('Не удалось войти. Проверьте соединение.');
    }
}

function openJoinConfirm(room) {
    selectedRoom = room.id;
    document.getElementById('joinRoomTitle').innerText = room.name;
    document.getElementById('joinRoomDesc').innerText = `Подключиться к комнате? (${room.usersCount}/${room.userMaxCount})`;
    const passwordInput = document.getElementById('joinPassword');
    if (passwordInput) passwordInput.value = '';
    openModal('joinModal');
}

async function enterRoom() {
    if (!selectedRoom)
        return;

    if (window.isBackendReady) {
        await fetchMyRoom();
        if (myRoom) {
            if (isMyRoom(selectedRoom)) {
                goToMyRoom();
                return;
            }
            showAlreadyInRoom();
            return;
        }

        const response = await fetch(`/api/v1/rooms/${selectedRoom}/enter`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            },
            body: JSON.stringify({
                password: document.getElementById('joinPassword')?.value.trim() ?? ''
            })
        });
        if (response.ok) {
            window.location.href = '../room/index.html';
            return;
        }

        const mine = await fetch('/api/v1/rooms/my-room', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            }
        });
        if (mine.ok) {
            const room = await mine.json();
            if (room && String(room.id).toLowerCase() === String(selectedRoom).toLowerCase()) {
                window.location.href = '../room/index.html';
                return;
            }
        }

        alert('Не удалось войти в комнату.');
    }
    else{
        window.location.href = '../room/index.html';
    }
}

