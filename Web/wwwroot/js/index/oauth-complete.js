(async () => {
    try {
        const response = await fetch('/api/v1/auth/external-token', {
            method: 'POST',
            credentials: 'same-origin',
            cache: 'no-store',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!response.ok) throw new Error('Вход не завершён. Попробуйте войти ещё раз.');

        const result = await response.json();
        const destination = new URL(result.returnUrl, window.location.origin);
        if (!result.token || destination.origin !== window.location.origin) {
            throw new Error('Не удалось завершить вход. Вернитесь на главную страницу.');
        }
        localStorage.setItem('jwt_token', result.token);
        setTimeout(() => window.location.replace(destination.href), 0);
    } catch (error) {
        document.getElementById('login-status').textContent = error.message;
    }
})();
