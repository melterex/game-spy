const modal = document.getElementById("authModal");
const loginBtn = document.getElementById("loginBtn");
const closeBtn = document.querySelector(".close");
const playBtn = document.getElementById("playBtn");

function openAuthModal() {
    modal.style.display = "block";
}

loginBtn.onclick = openAuthModal;

closeBtn.onclick = () => {
    modal.style.display = "none";
}

window.onclick = (event) => {
    if (event.target === modal) {
        modal.style.display = "none";
    }
}

function setPlayButton(isLoggedIn) {
    document.body.classList.toggle('logged-in', isLoggedIn);
    if (isLoggedIn) {
        playBtn.innerText = "Играть";
        playBtn.onclick = () => {
            window.location.href = 'room-list/index.html';
        };
    } else {
        playBtn.innerText = "Вход";
        playBtn.onclick = openAuthModal;
    }
}

let authCheckId = 0;

async function checkAuth() {
    const token = localStorage.getItem('jwt_token');
    const checkId = ++authCheckId;
    if (!token) {
        setPlayButton(false);
        return;
    }

    try {
        const response = await fetch('/me', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        if (checkId !== authCheckId || localStorage.getItem('jwt_token') !== token) return;
        setPlayButton(response.ok);
        if (!response.ok) localStorage.removeItem('jwt_token');
    } catch (error) {
        if (checkId !== authCheckId) return;
        console.error("Ошибка проверки авторизации:", error);
        setPlayButton(false);
    }
}

checkAuth();
window.addEventListener('pageshow', (event) => {
    if (event.persisted) checkAuth();
});


function openTab(evt, tabName) {
    let i, tabContent, tabLinks;
    tabContent = document.getElementsByClassName("tab-content");
    for (i = 0; i < tabContent.length; i++) {
        tabContent[i].classList.remove("active");
    }
    tabLinks = document.getElementsByClassName("tab-link");
    for (i = 0; i < tabLinks.length; i++) {
        tabLinks[i].classList.remove("active");
    }
    document.getElementById(tabName).classList.add("active");
    evt.currentTarget.classList.add("active");
}