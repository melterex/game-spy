let roomData;
let gameData = {players: [],
    isVoting: true,
    timeToVote: 0,
    timeToMakeTurn: 0,
    turnPlayerId: 0,
    card: '',
    theme:'',
    messages: [],
    round: 0,
    voteStatistics: [],
    isAmogus: false,
};

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
    const selectedRoom = localStorage.getItem('selected_room_id');

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

        console.log(myProfile, "aaaa");

        if (myProfile && myProfile.ready === true) {
            const readyBtn = document.getElementById('readyBtn');
            if (readyBtn) {
                readyBtn.innerText = "ОЖИДАНИЕ ИГРОКОВ...";
                readyBtn.disabled = true; // Замораживаем, так как бэкенд не поддерживает отмену
                readyBtn.classList.add('active'); // На случай, если у тебя есть CSS-стиль для нажатой кнопки
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
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage(); //TODO(заглушка)
        });
    }
});
