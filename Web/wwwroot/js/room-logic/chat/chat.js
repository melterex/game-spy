async function sendMessage() {
    if (String(window.mySlotId) !== String(idTurn)) return;

    const input = document.getElementById('chatInput');
    if (!input || input.value.trim() === "") return;

    if (window.isBackendReady && window.connection) {
        try {
            await window.connection.invoke("MakeTurn", input.value);
            input.value = '';
        } catch (err) {
            console.error("Ошибка отправки хода:", err);
        }
    } else {
        addMessage("1", input.value);
        input.value = '';
    }
}

function addMessage(slotId, message) {
    const chat = document.getElementById('chatArea');
    if (!chat) return;

    const nickname = getPlayerNickname(slotId);

    const msg = document.createElement('div');
    msg.className = 'message';
    msg.innerHTML = `<strong>${nickname}</strong>: ${message}`;

    chat.appendChild(msg);
    chat.scrollTop = chat.scrollHeight;
}

function getPlayerNickname(slotId) {
    if (!roomData?.players) return 'Игрок';
    const player = roomData.players.find(p => String(p.player?.slotId ?? p.slotId) === String(slotId));
    return player?.player?.nickname ?? player?.nickname ?? 'Игрок';
}

function loadChatMessages(messages) {
    const chat = document.getElementById('chatArea');
    if (!chat) return;

    chat.innerHTML = '';
    if (!messages || messages.length === 0) {
        chat.innerHTML = '<div class="message"><span>Система:</span> Игра продолжается...</div>';
        return;
    }
    messages.forEach(m => addMessage(m.slotId, m.messageBody));
}
