function updateChatInputForTurn() {
    const input = document.getElementById('chatInput');
    if (!input || roomStatus !== 'ingame') return;

    const myTurn = window.mySlotId && String(window.mySlotId) === String(idTurn);
    input.disabled = !myTurn;
    input.placeholder = myTurn ? 'Ваше сообщение...' : 'Сейчас ход другого игрока';
    if (!myTurn) input.value = '';
}

async function sendMessage() {
    if (!window.mySlotId || String(window.mySlotId) !== String(idTurn)) return;

    const input = document.getElementById('chatInput');
    if (!input || input.value.trim() === "") return;

    const text = input.value;
    input.value = '';

    if (window.isBackendReady && window.connection) {
        try {
            await window.connection.invoke("MakeTurn", text);
        } catch (err) {
            console.error("Ошибка отправки хода:", err);
            if (String(window.mySlotId) === String(idTurn) && input.value === '')
                input.value = text;
        }
    } else {
        addMessage("1", text);
    }
}

function addMessage(id, message) {
    const chat = document.getElementById('chatArea');
    if (!chat) return;

    const nickname = getPlayerNickname(id);

    const msg = document.createElement('div');
    msg.className = 'message';
    if (window.mySlotId && String(id) === String(window.mySlotId)) {
        msg.classList.add('mine');
    }
    msg.innerHTML = `<strong>${nickname}</strong>: ${message}`;

    chat.appendChild(msg);
    chat.scrollTop = chat.scrollHeight;
}

function getPlayerNickname(id) {
    if (!roomData?.players) return 'Игрок';
    const player = roomData.players.find(p => String(p.player?.id ?? p.id) === String(id));
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
    messages.forEach(m => addMessage(m.playerId, m.messageBody));
}
