let rooms = [];
let selectedRoom = null;
let lst = 5; //TODO: удалить потом


async function renderRooms() {
    if (window.isBackendReady) {
        const response = await fetch('/api/v1/rooms', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            }
        });

        if (response.ok) {
            rooms = await response.json();
        }
    } else {
        console.error("Connection error");
        if (rooms.length === 0) {
            rooms = [
                {
                    name: "Room1",
                    id: "1",
                    usersCount: 3,
                    userMaxCount: 10
                },
                {
                    name: "Room2",
                    id: "2",
                    usersCount: 8,
                    userMaxCount: 8
                },
                {
                    name: "Room3",
                    id: "3",
                    usersCount: 1,
                    userMaxCount: 6
                },
                {
                    name: "Room4",
                    id: "4",
                    usersCount: 5,
                    userMaxCount: 10
                }
            ];
        }
    }

    const container = document.getElementById('roomsContainer');
    container.innerHTML = '';

    rooms.forEach(room => {
        const card = document.createElement('div');
        card.className = 'room-card';
        card.onclick = () => openJoinConfirm(room);

        const occupancyColor = room.usersCount >= room.userMaxCount ? '#ff4d4d' : '#4dff4d';

        card.innerHTML = `
            <div>
                <div class="room-name">${room.name}</div>
                <small style="color: #666">ID: ${room.id}</small>
            </div>
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

function exitRoomList(){
    window.location.href = './../index.html';
}
