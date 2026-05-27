async function sendMessage() {

    const input = document.getElementById('chatInput');

    if (input && input.value.trim() !== "") {

        if (window.isBackendReady && window.connection) {
            await window.connection.invoke("MakeTurn", input);
        }
        else {
            addMessage("1", input.value);
        }
    }
}

function addMessage(id, message) {
    const chat = document.getElementById('chatArea');
    let nickname = roomData.players.find(player => player.id === id);
    const msg = document.createElement('div');
    msg.className = 'message';
    msg.innerHTML = `${nickname}: ${message}`;
    chat.appendChild(msg);
    chat.scrollTop = chat.scrollHeight;
}
