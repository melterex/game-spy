function openJoinConfirm(room) {
    selectedRoom = room.id;
    document.getElementById('joinRoomTitle').innerText = room.name;
    document.getElementById('joinRoomDesc').innerText = `Подключиться к комнате? (${room.usersCount}/${room.userMaxCount})`;
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
            if (String(room.id).toLowerCase() === String(selectedRoom).toLowerCase()) {
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

