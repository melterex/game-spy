const playerAvatarUrls = new Map();
let myAvatar = null;

function playerAvatarHeaders() {
    return { 'Authorization': `Bearer ${localStorage.getItem('jwt_token')}` };
}

async function loadMyAvatar() {
    if (myAvatar) return myAvatar;

    const response = await fetch('/me', { headers: playerAvatarHeaders() });
    if (!response.ok) return null;

    const me = await response.json();
    myAvatar = {
        userId: String(me.id),
        hash: localStorage.getItem(`avatar_${me.id}`)
    };
    return myAvatar;
}

function playerAvatarUrl(hash) {
    if (!playerAvatarUrls.has(hash)) {
        const request = fetch(`/api/images/${encodeURIComponent(hash)}/file?size=128x128&crop=center`, {
            headers: playerAvatarHeaders()
        })
            .then(response => response.ok ? response.blob() : null)
            .then(blob => blob ? URL.createObjectURL(blob) : null)
            .catch(() => null);
        playerAvatarUrls.set(hash, request);
    }
    return playerAvatarUrls.get(hash);
}

function playerAvatarTargets() {
    const targets = [];

    document.querySelectorAll('.side-players .player-tag[data-id]').forEach(tag => {
        targets.push({ id: tag.dataset.id, apply: url => {
            tag.style.setProperty('--player-avatar', `url("${url}")`);
            tag.classList.add('has-avatar');
        }});
    });

    document.querySelectorAll('#usersGrid .user-card[id^="card-"]').forEach(card => {
        const placeholder = card.querySelector('.avatar-placeholder');
        if (!placeholder) return;
        targets.push({ id: card.id.slice('card-'.length), apply: url => {
            placeholder.style.backgroundImage = `url("${url}")`;
            placeholder.textContent = '';
            placeholder.classList.add('has-avatar');
        }});
    });

    return targets;
}

async function decoratePlayerAvatars() {
    const me = await loadMyAvatar();
    if (!me?.hash) return;

    const mine = new Set([me.userId, window.mySlotId ? String(window.mySlotId) : null]);
    const url = await playerAvatarUrl(me.hash);
    if (!url) return;

    for (const target of playerAvatarTargets()) {
        if (mine.has(target.id)) target.apply(url);
    }
}

let playerAvatarsScheduled = false;

function schedulePlayerAvatars() {
    if (playerAvatarsScheduled) return;
    playerAvatarsScheduled = true;
    requestAnimationFrame(() => {
        playerAvatarsScheduled = false;
        decoratePlayerAvatars();
    });
}

document.addEventListener('DOMContentLoaded', () => {
    const containers = ['leftSide', 'rightSide', 'usersGrid']
        .map(id => document.getElementById(id))
        .filter(Boolean);
    if (containers.length === 0) return;

    const observer = new MutationObserver(schedulePlayerAvatars);
    containers.forEach(container => observer.observe(container, { childList: true }));
    schedulePlayerAvatars();
});
