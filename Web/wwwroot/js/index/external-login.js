(async () => {
    const container = document.getElementById('external-logins');
    try {
        const response = await fetch('/api/v1/auth/providers', { cache: 'no-store' });
        if (!response.ok) return;
        const labels = { Yandex: 'Войти через Яндекс', Vkontakte: 'Войти через VK' };
        for (const provider of await response.json()) {
            if (!labels[provider]) continue;
            const paragraph = document.createElement('p');
            const link = document.createElement('a');
            link.href = `/api/v1/auth/login/${encodeURIComponent(provider)}`;
            link.textContent = labels[provider];
            paragraph.appendChild(link);
            container.appendChild(paragraph);
        }
    } catch {
    }
})();
