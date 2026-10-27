const externalProviders = {
    Yandex: {
        label: 'Войти через Яндекс',
        className: 'oauth-btn oauth-btn--yandex'
    },
    Vkontakte: {
        label: 'Войти через VK',
        className: 'oauth-btn oauth-btn--vk'
    }
};

function renderExternalLogins(providerNames) {
    const container = document.getElementById('external-logins');
    if (!container) return;

    container.replaceChildren();
    const available = providerNames.filter((name) => externalProviders[name]);
    if (!available.length) {
        container.hidden = true;
        return;
    }

    const divider = document.createElement('p');
    divider.className = 'oauth-divider';
    divider.textContent = 'или';
    container.append(divider);

    for (const name of available) {
        const provider = externalProviders[name];
        const link = document.createElement('a');
        link.className = provider.className;
        link.href = `/api/v1/auth/login/${encodeURIComponent(name)}`;
        const mark = document.createElement('span');
        mark.className = 'oauth-mark';
        mark.setAttribute('aria-hidden', 'true');
        mark.textContent = name === 'Yandex' ? 'Я' : 'VK';
        const label = document.createElement('span');
        label.textContent = provider.label;
        link.append(mark, label);
        container.append(link);
    }

    container.hidden = false;
}

(async () => {
    try {
        const response = await fetch('/api/v1/auth/providers', { cache: 'no-store' });
        if (!response.ok) return;
        renderExternalLogins(await response.json());
    } catch {
    }
})();
