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
        localStorage.setItem('selected_room_id', selectedRoom);
        const response = await fetch(`/api/v1/rooms/${selectedRoom}/enter`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            },
            body: JSON.stringify({
                id: selectedRoom
            })
        });
        if (response.ok) {
            window.location.href = '../room/index.html';
        }
    }
    else{
        window.location.href = '../room/index.html';
    }
}

