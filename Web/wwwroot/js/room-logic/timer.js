function startTimer(timeLeft) {
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