async function loginUser(username, password) {
    const url = '/api/v1/auth/login';

    const loginData = {
        username: username,
        password: password
    };

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginData)
        });

        if (!response.ok) {
            throw new Error(`Ошибка сервера: ${response.status}`);
        }

        const result = await response.text();
        storeTokenAndGo(result.trim().replace(/^"|"$/g, ''), '/room-list/index.html');
    } catch (error) {
        console.error("Ошибка входа ", error.message);
        alert("Ошибка авторизации! Неверный логин/пароль.");
    }
}

function storeTokenAndGo(token, url) {
    rememberJwt(token);
    setTimeout(() => window.location.assign(url), 0);
}

const loginForm = document.getElementById('login');

if (loginForm) {
    loginForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        const formData = new FormData(loginForm);
        const usernameInput = formData.get('logNick');
        const passwordInput = formData.get('logPass');

        await loginUser(usernameInput, passwordInput);
    });
}