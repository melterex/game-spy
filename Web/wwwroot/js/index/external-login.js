const oauthProviders = [
    {
        name: 'Vkontakte',
        label: 'Войти через VK',
        className: 'oauth-btn oauth-btn--vk',
        mark: 'VK'
    },
    {
        name: 'Yandex',
        label: 'Войти через Яндекс',
        className: 'oauth-btn oauth-btn--yandex',
        mark: 'Я'
    }
];

function renderOAuthLink(container, provider) {
    const link = document.createElement('a');
    link.className = provider.className;
    link.href = `/api/v1/auth/login/${provider.name}`;
    const mark = document.createElement('span');
    mark.className = 'oauth-mark';
    mark.setAttribute('aria-hidden', 'true');
    mark.textContent = provider.mark;
    const label = document.createElement('span');
    label.textContent = provider.label;
    link.append(mark, label);
    container.append(link);
}

(async () => {
    const container = document.getElementById('external-logins');
    if (!container) return;

    let enabled = oauthProviders.map(provider => provider.name);
    try {
        const response = await fetch('/api/v1/auth/providers', { cache: 'no-store' });
        if (response.ok) {
            const data = await response.json();
            const providers = Array.isArray(data) ? data : (data.providers || []);
            enabled = oauthProviders
                .map(provider => provider.name)
                .filter(name => providers.includes(name));
        }
    } catch {
        enabled = oauthProviders.map(provider => provider.name);
    }

    const visible = oauthProviders.filter(provider => enabled.includes(provider.name));
    container.replaceChildren();
    if (visible.length === 0) return;

    const divider = document.createElement('p');
    divider.className = 'oauth-divider';
    divider.textContent = 'или';
    container.append(divider);
    visible.forEach(provider => renderOAuthLink(container, provider));
    container.hidden = false;
})();
