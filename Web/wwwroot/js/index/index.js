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

async function checkAuth() {
    const token = localStorage.getItem('jwt_token');
    if (!token) {
        setPlayButton(false);
        return;
    }

    setPlayButton(true);
    try {
        const response = await fetch('/me', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        setPlayButton(response.ok);
    } catch (error) {
        console.error("Ошибка проверки авторизации:", error);
    }
}

checkAuth();


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