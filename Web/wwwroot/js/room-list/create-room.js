async function createNewRoom() {
    const nameInput = document.getElementById('roomNameInput');
    const limitSelect = document.getElementById('playerLimit');

    const roomName = nameInput ? nameInput.value.trim() : "";
    const maxCount = limitSelect ? parseInt(limitSelect.value) : 10;
    if (!roomName) {
        alert("Пожалуйста, введите название комнаты!");
        return;
    }
    console.log(roomName);
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
                    theme: roomName,
                    userMaxCount: maxCount,
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
        createdRoom = {name: roomName, id: lst.toString(), usersCount: 0, userMaxCount: maxCount};
        lst += 1;
        rooms.push(createdRoom);

        console.log(rooms);
    }
    closeModal('createModal');
    await renderRooms();
}

