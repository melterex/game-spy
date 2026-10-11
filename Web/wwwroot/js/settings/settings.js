const AVATAR_MAX_SIZE = 5 * 1024 * 1024;
const AVATAR_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/bmp'];

let settingsUser = null;
let settingsPendingFile = null;
let settingsPreviewUrl = null;
let settingsAvatarUrl = null;

function authHeaders() {
    return { 'Authorization': `Bearer ${localStorage.getItem('jwt_token')}` };
}

async function loadAvatarBlobUrl(hash) {
    const response = await fetch(`/api/images/${encodeURIComponent(hash)}/file?size=256x256&crop=center`, {
        headers: authHeaders()
    });
    if (!response.ok) return null;
    return URL.createObjectURL(await response.blob());
}

function setAvatarImage(element, url) {
    const initial = (settingsUser?.username ?? '?').trim().charAt(0).toUpperCase() || '?';

    if (url) {
        element.style.backgroundImage = `url("${url}")`;
        element.textContent = '';
        element.classList.add('has-image');
    } else {
        element.style.backgroundImage = '';
        element.textContent = initial;
        element.classList.remove('has-image');
    }
}

function storedAvatarKey(userId) {
    return `avatar_${userId}`;
}

async function refreshAvatar() {
    if (settingsAvatarUrl) URL.revokeObjectURL(settingsAvatarUrl);
    const hash = settingsUser?.id ? localStorage.getItem(storedAvatarKey(settingsUser.id)) : null;
    settingsAvatarUrl = hash ? await loadAvatarBlobUrl(hash) : null;

    setAvatarImage(document.getElementById('settingsAvatarBtn'), settingsAvatarUrl);
    setAvatarImage(document.getElementById('settingsAvatarPreview'), settingsAvatarUrl);
}

function isAuthOptionalPage() {
    return document.body.dataset.auth === 'optional';
}

async function loadSettingsUser() {
    if (!localStorage.getItem('jwt_token')) {
        if (!isAuthOptionalPage()) window.location.href = '/index.html';
        return;
    }

    const response = await fetch('/me', { headers: authHeaders() });

    if (response.status === 401) {
        localStorage.removeItem('jwt_token');
        if (!isAuthOptionalPage()) window.location.href = '/index.html';
        return;
    }
    if (!response.ok) return;

    settingsUser = await response.json();
    document.body.classList.add('logged-in');
    document.getElementById('settingsUsername').textContent = settingsUser.username;
    await refreshAvatar();
}

function setSettingsStatus(text, isError = false) {
    const status = document.getElementById('settingsStatus');
    status.textContent = text;
    status.classList.toggle('error', isError);
}

function resetPendingAvatar() {
    settingsPendingFile = null;
    if (settingsPreviewUrl) URL.revokeObjectURL(settingsPreviewUrl);
    settingsPreviewUrl = null;
    document.getElementById('settingsAvatarInput').value = '';
    document.getElementById('settingsSaveBtn').disabled = true;
}

function openSettings() {
    resetPendingAvatar();
    setAvatarImage(document.getElementById('settingsAvatarPreview'), settingsAvatarUrl);
    setSettingsStatus('PNG, JPG, GIF, WEBP или BMP, до 5 МБ.');
    document.getElementById('settingsModal').style.display = 'block';
}

function closeSettings() {
    resetPendingAvatar();
    document.getElementById('settingsModal').style.display = 'none';
}

function pickAvatarFile(file) {
    if (!file) return;

    if (!AVATAR_TYPES.includes(file.type)) {
        setSettingsStatus('Этот формат не подходит. Нужна картинка PNG, JPG, GIF, WEBP или BMP.', true);
        return;
    }
    if (file.size > AVATAR_MAX_SIZE) {
        setSettingsStatus('Файл больше 5 МБ.', true);
        return;
    }

    resetPendingAvatar();
    settingsPendingFile = file;
    settingsPreviewUrl = URL.createObjectURL(file);
    setAvatarImage(document.getElementById('settingsAvatarPreview'), settingsPreviewUrl);
    document.getElementById('settingsSaveBtn').disabled = false;
    setSettingsStatus(file.name);
}

async function saveAvatar() {
    if (!settingsPendingFile) return;

    const saveBtn = document.getElementById('settingsSaveBtn');
    saveBtn.disabled = true;
    setSettingsStatus('Загружаем...');

    try {
        const formData = new FormData();
        formData.append('File', settingsPendingFile);

        const uploadResponse = await fetch('/api/images/upload?source=1', {
            method: 'POST',
            headers: authHeaders(),
            body: formData
        });
        if (!uploadResponse.ok) {
            throw new Error(`Не удалось загрузить картинку (${uploadResponse.status})`);
        }
        const { hash } = await uploadResponse.json();
        localStorage.setItem(storedAvatarKey(settingsUser.id), hash);

        resetPendingAvatar();
        await refreshAvatar();
        if (settingsAvatarUrl) {
            setSettingsStatus('Аватар обновлён!');
        } else {
            setSettingsStatus('Сервер принял картинку, но не отдал файл.', true);
        }
    } catch (error) {
        console.error(error);
        setSettingsStatus(error.message, true);
        saveBtn.disabled = !settingsPendingFile;
    }
}

function logoutAccount() {
    localStorage.removeItem('jwt_token');
    window.location.href = '/index.html';
}

function renderSettingsModal() {
    const modal = document.createElement('div');
    modal.id = 'settingsModal';
    modal.className = 'modal';
    modal.innerHTML = `
        <div class="modal-content card">
            <span class="card-label">Настройки</span>
            <h2 id="settingsUsername" class="settings-title">Профиль</h2>

            <label id="settingsDropZone" class="avatar-drop" for="settingsAvatarInput">
                <span id="settingsAvatarPreview" class="avatar-preview"></span>
                <span class="avatar-drop-text">
                    <span class="avatar-drop-title">Выбрать картинку</span>
                    <span id="settingsStatus" class="settings-status"></span>
                </span>
                <input type="file" id="settingsAvatarInput" class="visually-hidden"
                       accept="${AVATAR_TYPES.join(',')}">
            </label>

            <div class="settings-actions">
                <button id="settingsSaveBtn" class="cta ink" onclick="saveAvatar()" disabled>Сохранить</button>
                <button class="link-btn" onclick="logoutAccount()">Выйти из аккаунта</button>
                <button class="link-btn" onclick="closeSettings()">Закрыть</button>
            </div>
        </div>
    `;
    modal.addEventListener('click', (event) => {
        if (event.target === modal) closeSettings();
    });
    document.body.appendChild(modal);
}

document.addEventListener('DOMContentLoaded', () => {
    if (!document.getElementById('settingsAvatarBtn')) return;

    renderSettingsModal();

    const input = document.getElementById('settingsAvatarInput');
    const dropZone = document.getElementById('settingsDropZone');

    input.addEventListener('change', () => pickAvatarFile(input.files[0]));

    dropZone.addEventListener('dragover', (event) => {
        event.preventDefault();
        dropZone.classList.add('dragover');
    });
    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));
    dropZone.addEventListener('drop', (event) => {
        event.preventDefault();
        dropZone.classList.remove('dragover');
        pickAvatarFile(event.dataTransfer.files[0]);
    });

    loadSettingsUser().catch(error => console.error('Ошибка загрузки профиля:', error));
});
