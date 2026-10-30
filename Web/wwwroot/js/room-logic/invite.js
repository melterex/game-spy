let currentInvite = null;

document.getElementById('inviteModal')?.addEventListener('click', (event) => {
    if (event.target.id === 'inviteModal') closeInviteModal();
});

function closeInviteModal() {
    const modal = document.getElementById('inviteModal');
    if (modal) modal.style.display = 'none';
}

function showInviteError(message) {
    const error = document.getElementById('inviteError');
    if (!error) return;
    error.hidden = !message;
    error.textContent = message || '';
}

function renderInvite(invite) {
    const roomNode = document.getElementById('inviteRoomId');
    const codeNode = document.getElementById('inviteOup');
    if (roomNode) roomNode.textContent = invite?.roomId || '…';
    if (codeNode) codeNode.textContent = invite?.oup || '…';
}

async function openInviteModal() {
    const modal = document.getElementById('inviteModal');
    if (!modal) return;

    showInviteError('');
    modal.style.display = 'block';

    const roomId = roomData?.roomId;
    if (!roomId) {
        renderInvite(null);
        showInviteError('Комната ещё не загрузилась.');
        return;
    }

    if (currentInvite && String(currentInvite.roomId).toLowerCase() === String(roomId).toLowerCase()) {
        renderInvite(currentInvite);
        return;
    }

    await createInviteCode();
}

async function createInviteCode() {
    const roomId = roomData?.roomId;
    if (!roomId) {
        showInviteError('Комната ещё не загрузилась.');
        return;
    }

    showInviteError('');
    const codeNode = document.getElementById('inviteOup');
    if (codeNode) codeNode.textContent = '…';

    try {
        const response = await fetch(`/api/v1/rooms/${roomId}/create-oup`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            }
        });

        if (!response.ok) {
            currentInvite = null;
            renderInvite({ roomId });
            showInviteError(response.status === 400
                ? 'Код можно выдать только пока стол ждёт игроков.'
                : 'Не удалось создать код.');
            return;
        }

        const data = await response.json();
        currentInvite = { roomId, oup: data.oup };
        renderInvite(currentInvite);
    } catch (error) {
        console.error(error);
        showInviteError('Не удалось создать код. Проверьте соединение.');
    }
}

async function copyInvite(part) {
    const value = part === 'room' ? currentInvite?.roomId : currentInvite?.oup;
    if (!value) return;

    try {
        await navigator.clipboard.writeText(value);
    } catch (error) {
        console.error(error);
        showInviteError('Не удалось скопировать. Выделите текст вручную.');
    }
}
