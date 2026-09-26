
async function registerUser(username, password) {
    const url = '/api/v1/auth/register';
    const registerData = {
        username: username,
        password: password
    };
    try{
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(registerData)
        });
        if (!response.ok) {
            throw new Error(`Ошибка сервера: ${response.status}`);
        }

        const result = await response.text();

        console.log("Token: ", result);

        localStorage.setItem('jwt_token', result);
        window.location.href = '/room-list/index.html';

    } catch (error) {
        console.error("Ошибка регистрации ", error.message);
        alert("Логин уже занят, выберите другой");
    }
}


const registerForm = document.getElementById('register');
if (registerForm) {
    registerForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const formData = new FormData(registerForm);
        const usernameInput = formData.get('regNick');
        const passwordInput = formData.get('regPass');

        console.log(usernameInput);
        console.log(passwordInput);

        await registerUser(usernameInput, passwordInput);
    });
}