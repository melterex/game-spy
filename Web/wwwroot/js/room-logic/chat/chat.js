async function sendMessage() {
    console.log("sendMessage", window.myId);
    if (window.myId !== idTurn)
        return;

    const input = document.getElementById('chatInput');

    if (input && input.value.trim() !== "") {

        if (window.isBackendReady && window.connection) {
            await window.connection.invoke("MakeTurn", input.value);
        }
        else {
            addMessage("1", input.value);
        }
    }
}

function addMessage(id, message) {
    const chat = document.getElementById('chatArea');
    const player = roomData.players.find(p => p.id === id);
    const nickname = player.nickname;

    const msg = document.createElement('div');
    msg.className = 'message';
    msg.innerHTML = `<strong>${nickname}</strong>: ${message}`;

    chat.appendChild(msg);
    chat.scrollTop = chat.scrollHeight;
}