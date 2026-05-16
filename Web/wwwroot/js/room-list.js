
let rooms = [
    { id: 101, name: "room1", current: 3, max: 6 },
    { id: 102, name: "room2", current: 10, max: 10 }
];//TODO(пример)

let selectedRoom = null;

function renderRooms() {
    //TODO(заглушка)

    const container = document.getElementById('roomsContainer');
    container.innerHTML = '';

    rooms.forEach(room => {
        const card = document.createElement('div');
        card.className = 'room-card';
        card.onclick = () => openJoinConfirm(room);

        const occupancyColor = room.current >= room.max ? '#ff4d4d' : '#4dff4d';

        card.innerHTML = `
            <div>
                <div class="room-name">${room.name}</div>
                <small style="color: #666">ID: ${room.id}</small>
            </div>
            <div style="color: ${occupancyColor}; font-weight: bold;">
                ${room.current} / ${room.max}
            </div>
        `;
        container.appendChild(card);
    });
}

function createNewRoom() {
    // TODO(заглушка)
}

function openJoinConfirm(room) {
    //TODO(заглушка)
    selectedRoom = room;
    document.getElementById('joinRoomTitle').innerText = room.name;
    document.getElementById('joinRoomDesc').innerText = `Подключиться к комнате? (${room.current}/${room.max})`;
    openModal('joinModal');
}

function enterRoom() {
    //TODO(возможно тут надо еще доделать)
    if (selectedRoom) {
        window.location.href = 'room.html';
    }
}

function openModal(id) {
    document.getElementById(id).style.display = 'block';
}

function closeModal(id) {
    document.getElementById(id).style.display = 'none';
}

window.onclick = function(event) {
    if (event.target.className === 'modal') {
        event.target.style.display = 'none';
    }
}

document.addEventListener('DOMContentLoaded', renderRooms);