let timerIntervalId = null;

function formatTime(timeLeft) {
    const clamped = Math.max(0, timeLeft);
    const minutes = Math.floor(clamped / 60);
    const seconds = clamped % 60;
    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
}

function stopTimer() {
    if (timerIntervalId !== null) {
        clearInterval(timerIntervalId);
        timerIntervalId = null;
    }

    const timerElement = document.getElementById('timer');
    if (timerElement) timerElement.innerText = '';
}

function startTimer(timeLeft) {
    const timerElement = document.getElementById('timer');
    if (!timerElement) return;

    if (timerIntervalId !== null) {
        clearInterval(timerIntervalId);
        timerIntervalId = null;
    }

    timeLeft = Math.max(0, timeLeft);
    timerElement.innerText = formatTime(timeLeft);

    if (timeLeft <= 0) return;

    timerIntervalId = setInterval(() => {
        timeLeft--;
        timerElement.innerText = formatTime(timeLeft);

        if (timeLeft <= 0) {
            clearInterval(timerIntervalId);
            timerIntervalId = null;
        }
    }, 1000);
}
