if (typeof curStatus === 'undefined') {
    window.curStatus = false;
}

const readyBtn = document.getElementById('readyBtn');
if (readyBtn) {
    readyBtn.addEventListener('click', async () => {
        if (window.isBackendReady && window.connection) {
            try {
                readyBtn.disabled = true; // Сразу выключаем, чтобы не спамили

                // Отправляем строго true, как просит бэкенд
                await window.connection.invoke("MakeReady", true);

                window.curStatus = true;
                updateReadyButtonVisual(true);
            } catch (err) {
                console.error("Ошибка отправки статуса готовности:", err);
                // Если произошла сетевая ошибка (не Unsupported), возвращаем кнопку
                if (!window.curStatus) {
                    readyBtn.disabled = false;
                    readyBtn.innerText = "ГОТОВ";
                }
            }
        } else {
            readyBtn.innerText = "ОЖИДАНИЕ...";
            readyBtn.disabled = true;
            setTimeout(() => {
                readyBtn.style.display = 'none';
                addMessage("", "Игра началась");
                startGame();
            }, 1500);
        }
    });
}

function updateReadyButtonVisual(isReady) {
    const btn = document.getElementById('readyBtn');
    if (!btn) return;

    if (isReady) {
        btn.innerText = "ОЖИДАНИЕ ИГРОКОВ...";
        btn.disabled = true;
        btn.classList.add('active');
    } else {
        btn.innerText = "ГОТОВ";
        btn.disabled = false;
        btn.classList.remove('active');
    }
    renderRoom(roomData.players);
}