const players = [
    "user1", "user2", "user3", "user4",
    "user5", "user6", "user7", "user8", "user9",
];//TODO(надо поддерживать игроков)

function setupPlayers() {
    const leftContainer = document.getElementById('leftSide');
    const rightContainer = document.getElementById('rightSide');

    if (!leftContainer || !rightContainer) return;

    leftContainer.innerHTML = '';
    rightContainer.innerHTML = '';

    const half = Math.ceil(players.length / 2);

    const leftPlayers = players.slice(0, half);
    const rightPlayers = players.slice(half);

    const createTag = (name) => {
        const tag = document.createElement('div');
        tag.className = 'player-tag';
        tag.innerText = name;
        return tag;
    };

    leftPlayers.forEach(name => leftContainer.appendChild(createTag(name)));
    rightPlayers.forEach(name => rightContainer.appendChild(createTag(name)));
}

let timeLeft = 300; //TODO(надо получать время)
function startTimer() {
    const timerElement = document.getElementById('timer');
    if (!timerElement) return;

    const interval = setInterval(() => {
        let minutes = Math.floor(timeLeft / 60);
        let seconds = timeLeft % 60;

        timerElement.innerText = `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;

        if (timeLeft <= 0) {
            clearInterval(interval);
            alert("Время вышло!");
        }
        timeLeft--;
    }, 1000);
}

function sendMessage() {
    //TODO(пока что заглушка)

    const input = document.getElementById('chatInput');
    const chat = document.getElementById('chatArea');

    if (input && input.value.trim() !== "") {
        const msg = document.createElement('div');
        msg.className = 'message';
        msg.innerHTML = `<span>Вы:</span> ${input.value}`;
        chat.appendChild(msg);

        chat.scrollTop = chat.scrollHeight;
        input.value = "";
    }
}

function exitRoom() {
    if (confirm("Выйти из комнаты?")) {
        window.location.href = 'index.html';
    }
}

window.addEventListener('resize', () => {
    setupPlayers();
});


function setGameData(theme, word) {
    document.getElementById('themeValue').innerText = theme;
    document.getElementById('wordValue').innerText = word;
}

document.addEventListener('DOMContentLoaded', () => {
    setupPlayers();
    startTimer();

    setGameData("Транспорт", "Автобус"); //TODO (заглушка)

    const input = document.getElementById('chatInput');
    if (input) {
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage();//TODO(заглушка)
        });
    }
});
