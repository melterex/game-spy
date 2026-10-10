let rooms = [];
let selectedRoom = null;
let myRoom = null;

async function fetchMyRoom() {
    const response = await fetch('/api/v1/rooms/my-room', {
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });

    myRoom = response.ok ? await response.json() : null;
    renderMyRoomBanner();
    return myRoom;
}

function isMyRoom(roomId) {
    return myRoom !== null && String(myRoom.id).toLowerCase() === String(roomId).toLowerCase();
}

function goToMyRoom() {
    window.location.href = '../room/index.html';
}

function renderMyRoomBanner() {
    const banner = document.getElementById('myRoomBanner');
    if (!banner) return;

    if (!myRoom) {
        banner.classList.add('hidden');
        return;
    }

    document.getElementById('myRoomBannerName').textContent = myRoom.name;
    banner.classList.remove('hidden');
}

function showAlreadyInRoom() {
    closeModal('joinModal');
    closeModal('createModal');
    document.getElementById('alreadyRoomName').textContent = myRoom?.name ?? '';
    openModal('alreadyInRoomModal');
}

async function renderRooms() {
    const response = await fetch('/api/v1/rooms', {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });

    if (response.ok) {
        rooms = await response.json();
    }

    await fetchMyRoom();

    const container = document.getElementById('roomsContainer');
    container.innerHTML = '';

    rooms.forEach(room => {
        const mine = isMyRoom(room.id);
        const card = document.createElement('div');
        card.className = mine ? 'room-card mine' : 'room-card';
        card.onclick = () => mine ? goToMyRoom() : openJoinConfirm(room);

        const occupancyColor = room.usersCount >= room.userMaxCount ? '#ff4d4d' : '#4dff4d';

        card.innerHTML = `
            <div>
                <div class="room-name">${room.name}</div>
                <small style="color: #666">ID: ${room.id}</small>
            </div>
            ${mine ? '<span class="mine-badge">вы здесь</span>' : ''}
            <div style="color: ${occupancyColor}; font-weight: bold;">
                ${room.usersCount} / ${room.userMaxCount}
            </div>
        `;
        container.appendChild(card);
    });
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

async function renderRoomWithTimeout(){
    await renderRooms();
    setInterval(async () => {
        try {
            await renderRooms();
        } catch (error) {
        }
    }, 3000)
}

document.addEventListener('DOMContentLoaded', renderRoomWithTimeout);

window.addEventListener('pageshow', (event) => {
    if (event.persisted) {
        renderRooms();
    }
});

function exitRoomList(){
    window.location.href = './../index.html';
}
