async function getMyId(){
    const url = '/me';
    const response = await fetch(url, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        }
    });
    if (response.ok){
        let json = await response.json();
        window.myId = json.id;
        window.myNickname = json.username;
    }
}

function syncMySlotId(players) {
    window.mySlotId = null;
    if (!players || !window.myNickname) return;

    const me = players.find(p => p.isBot !== true && p.nickname === window.myNickname);
    if (me) {
        window.mySlotId = String(me.id);
    }
}