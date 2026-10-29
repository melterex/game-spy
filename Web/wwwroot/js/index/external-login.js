const VKID_SDK_URL = 'https://unpkg.com/@vkid/sdk@2.6.10/dist-sdk/umd/index.js';
const VKID_APP = 54855226;
const VKID_REDIRECT_URL = 'https://game-spy.duckdns.org/';

const yandexProvider = {
    label: 'Войти через Яндекс',
    className: 'oauth-btn oauth-btn--yandex',
    mark: 'Я'
};

function loadScript(src) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${src}"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = src;
        script.async = true;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Не удалось загрузить VK ID'));
        document.head.append(script);
    });
}

function vkidOnError() {
    const status = document.getElementById('external-login-status');
    if (status) status.textContent = 'Не удалось войти через VK. Попробуйте ещё раз.';
}

async function vkidOnSuccess(data) {
    const accessToken = data?.access_token;
    if (!accessToken) {
        vkidOnError();
        return;
    }

    try {
        const response = await fetch('/api/v1/auth/vkid', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ accessToken })
        });
        if (!response.ok) throw new Error('Вход не завершён');

        let token = await response.text();
        if (token.startsWith('"')) token = JSON.parse(token);
        localStorage.setItem('jwt_token', token);
        window.location.href = '/room-list/index.html';
    } catch {
        vkidOnError();
    }
}

async function renderVkid(container) {
    await loadScript(VKID_SDK_URL);
    if (!('VKIDSDK' in window)) throw new Error('Не удалось загрузить VK ID');

    const VKID = window.VKIDSDK;
    VKID.Config.init({
        app: VKID_APP,
        redirectUrl: VKID_REDIRECT_URL,
        responseMode: VKID.ConfigResponseMode.Callback,
        source: VKID.ConfigSource.LOWCODE,
        scope: ''
    });

    const oneTap = new VKID.OneTap();
    oneTap.render({
        container,
        showAlternativeLogin: false
    })
        .on(VKID.WidgetEvents.ERROR, vkidOnError)
        .on(VKID.OneTapInternalEvents.LOGIN_SUCCESS, (payload) => {
            VKID.Auth.exchangeCode(payload.code, payload.device_id)
                .then(vkidOnSuccess)
                .catch(vkidOnError);
        });
}

function renderYandexLink(container) {
    const link = document.createElement('a');
    link.className = yandexProvider.className;
    link.href = '/api/v1/auth/login/Yandex';
    const mark = document.createElement('span');
    mark.className = 'oauth-mark';
    mark.setAttribute('aria-hidden', 'true');
    mark.textContent = yandexProvider.mark;
    const label = document.createElement('span');
    label.textContent = yandexProvider.label;
    link.append(mark, label);
    container.append(link);
}

(async () => {
    const container = document.getElementById('external-logins');
    if (!container) return;

    let yandexEnabled = true;
    try {
        const response = await fetch('/api/v1/auth/providers', { cache: 'no-store' });
        if (response.ok) {
            const data = await response.json();
            const providers = Array.isArray(data) ? data : (data.providers || []);
            yandexEnabled = providers.includes('Yandex');
        }
    } catch {
        yandexEnabled = true;
    }

    container.replaceChildren();
    const divider = document.createElement('p');
    divider.className = 'oauth-divider';
    divider.textContent = 'или';
    container.append(divider);

    const vkid = document.createElement('div');
    vkid.id = 'vkid-container';
    container.append(vkid);
    if (yandexEnabled) renderYandexLink(container);

    const status = document.createElement('p');
    status.id = 'external-login-status';
    status.className = 'oauth-status';
    status.setAttribute('role', 'status');
    container.append(status);
    container.hidden = false;

    try {
        await renderVkid(vkid);
    } catch {
        vkidOnError();
    }
})();
