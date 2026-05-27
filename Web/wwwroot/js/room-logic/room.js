let roomData;
let roomStatus;
let idTurn;

function exitRoom() {
    if (confirm("Выйти из комнаты?")) {
        localStorage.removeItem('selected_room_id');
        window.location.href = '../';
    }
}

window.addEventListener('resize', () => {
    console.log('resize');
    renderRoom(roomData.players);
});

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('jwt_token');

    let url = `/api/v1/rooms/my-room/lobby`;

    if (window.isBackendReady && token) {
        console.log(token);
        try{
            const response = await fetch(`/api/v1/rooms/my-room/status`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (response.ok) {
                let vsp = await response.text();
                roomStatus = vsp;
                if (vsp === 'ingame'){
                    url = `/api/v1/rooms/my-room/game`;
                }
            }
        }
        catch(e) {}
        try {
            console.log(url);
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            console.log(response);

            if (response.ok) {
                let vsp = await response.json();
                console.log(vsp);
                roomData = vsp;
            }
        } catch (err) {
        }
        console.log(roomData);

        await getMyId();

        const myProfile = roomData.players.find(p => {
            const id = p.player?.id ?? p.id;
            return String(id) === String(window.myId);
        });

        if (myProfile && myProfile.ready === true) {
            const readyBtn = document.getElementById('readyBtn');
            if (readyBtn) {
                readyBtn.innerText = "ОЖИДАНИЕ ИГРОКОВ...";
                readyBtn.disabled = true;
                readyBtn.classList.add('active');
            }
        }

        renderRoom(roomData.players);

        await startSignalR(token);


        if (window.connection) {
            await window.connection.invoke("EnterRoom");
        }
    }
    else{

        renderRoom(roomData.players);
    }

    const input = document.getElementById('chatInput');
    if (input) {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                sendMessage();
            }
        });
    }
});
