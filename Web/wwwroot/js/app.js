(() => {
    'use strict';
    const $ = id => document.getElementById(id);
    const path = location.pathname;
    const page = path.includes('/room-list') ? 'list' : path.includes('/voting') ? 'voting' : path.includes('/room') ? 'room' : 'home';
    let me, state, connection, selectedRoom, historyOffset = 0, refreshPromise, refreshAgain = false;
    let clockOffset = 0, errorTimeout;
    const token = () => localStorage.getItem('jwt_token');
    const show = (id, visible) => $(id)?.classList.toggle('hidden', !visible);
    function error(message) {
        $('error').textContent = message;
        show('error', true);
        const dialog = document.querySelector('dialog[open]');
        if (dialog) {
            let inline = dialog.querySelector('.dialog-error');
            if (!inline) { inline = element('p', null, 'dialog-error'); inline.setAttribute('role', 'alert'); dialog.prepend(inline); }
            inline.textContent = message;
        }
        clearTimeout(errorTimeout);
        errorTimeout = setTimeout(() => { show('error', false); document.querySelectorAll('.dialog-error').forEach(e => e.remove()); }, 8000);
    }
    async function api(url, options = {}, allowMissing = false) {
        const headers = { Authorization: `Bearer ${token()}`, ...options.headers };
        if (options.body && !(options.body instanceof FormData)) headers['Content-Type'] = 'application/json';
        const response = await fetch(url, { ...options, headers });
        if (response.status === 401) {
            localStorage.removeItem('jwt_token');
            if (page !== 'home') location.replace('/');
            throw new Error('Сессия истекла. Войдите снова.');
        }
        if (allowMissing && response.status === 404) return null;
        const text = await response.text();
        let data;
        try { data = JSON.parse(text); } catch { data = text; }
        if (!response.ok) throw new Error(typeof data === 'string' && data ? data
            : data?.message || (data?.errors ? Object.values(data.errors).flat().join(' ') : 'Не удалось выполнить запрос'));
        return data;
    }
    const safe = fn => async (...args) => { try { await fn(...args); } catch (e) { error(e.message); } };
    const bind = (id, event, fn) => $(id)?.addEventListener(event, safe(fn));
    const post = (url, body = {}) => api(url, { method: 'POST', body: JSON.stringify(body) });
    async function invoke(method, ...args) {
        if (connection?.state !== signalR.HubConnectionState.Connected) throw new Error('Нет связи с сервером. Подождите подключения.');
        await connection.invoke(method, ...args);
        await refresh();
    }
    function element(tag, text, className) {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (className) node.className = className;
        return node;
    }
    function avatar(player) {
        if (!player.avatarUrl) return element('span', player.isBot ? '🤖' : '👤', 'avatar');
        const image = element('img', null, 'avatar');
        image.src = player.avatarUrl;
        image.alt = `Аватар ${player.nickname}`;
        return image;
    }
    function connectionStatus(connected) {
        show('connectionStatus', !connected);
        $('connectionStatus').textContent = 'Связь потеряна. Подключаемся снова… Место сохраняется 30 секунд.';
        if (state) render();
    }
    async function connect() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/room_hub', { accessTokenFactory: token })
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000]).build();
        connection.on('RoomUpdated', safe(() => refresh()));
        connection.on('RoomsUpdated', safe(() => page === 'list' ? refresh() : undefined));
        connection.on('RemovedFromRoom', safe(async roomId => {
            // Ignore a delayed leave event if this user has already joined a different room.
            if (state?.id === roomId || page !== 'list') {
                const current = await api('/api/v1/rooms/my-room', {}, true);
                if (!current) location.replace('/room-list/');
                else await refresh();
            } else await refresh();
        }));
        connection.onreconnecting(() => connectionStatus(false));
        connection.onreconnected(safe(async () => { connectionStatus(true); await refresh(); }));
        connection.onclose(() => { connectionStatus(false); setTimeout(safe(startConnection), 3000); });
        await startConnection();
    }
    async function startConnection() {
        if (!token()) return;
        try {
            await connection.start();
            connectionStatus(true);
            await refresh();
        } catch {
            connectionStatus(false);
            await api('/me'); // Expired sessions go back to sign-in instead of retrying forever.
            setTimeout(safe(startConnection), 3000);
        }
    }
    async function refresh() {
        if (refreshPromise) { refreshAgain = true; return refreshPromise; }
        refreshPromise = (async () => {
            do {
                refreshAgain = false;
                if (page === 'list') await renderList();
                else if (page === 'room' || page === 'voting') {
                    const snapshot = await api('/api/v1/rooms/my-room/state', {}, true);
                    if (!snapshot) { location.replace('/room-list/'); return; }
                    state = snapshot;
                    clockOffset = Date.parse(state.serverTime) - Date.now();
                    if (state.stage === 'voting' && page !== 'voting') { location.replace('/voting/'); return; }
                    if (state.stage === 'round' && page !== 'room') { location.replace('/room/'); return; }
                    const resultPending = state.result && !sessionStorage.getItem(`result_seen_${me.id}_${state.result.gameId}`);
                    if (state.stage === 'waiting' && page === 'voting' && !resultPending) { location.replace('/room/'); return; }
                    render();
                    if (resultPending) renderResult(state.result);
                }
            } while (refreshAgain);
        })();
        try { await refreshPromise; } finally { refreshPromise = null; }
    }
    async function renderList() {
        const [rooms, current] = await Promise.all([api('/api/v1/rooms'), api('/api/v1/rooms/my-room', {}, true)]);
        show('resumeRoom', !!current);
        $('createBtn').disabled = !!current;
        const container = $('roomsContainer');
        container.replaceChildren();
        if (!rooms.length) container.append(element('p', 'Пока нет открытых комнат. Создайте первую.', 'muted'));
        rooms.forEach(room => {
            const full = room.usersCount >= room.userMaxCount;
            const card = element('button', null, `room-card${full ? ' full' : ''}`);
            const title = element('div');
            title.append(element('div', `${room.isPrivate ? '🔒 ' : ''}${room.name}`, 'room-name'), element('small', room.theme));
            card.append(title, element('span', `${room.usersCount} / ${room.userMaxCount}${full ? ' · Заполнена' : ''}`));
            card.disabled = full && current?.id !== room.id;
            card.addEventListener('click', () => {
                if (current?.id === room.id) { location.href = '/room/'; return; }
                if (current) { error('Сначала выйдите из своей комнаты.'); return; }
                openJoin(room);
            });
            container.append(card);
        });
        const invite = new URLSearchParams(location.search).get('join');
        if (invite && !selectedRoom) {
            const room = rooms.find(r => r.id === invite);
            if (room && !current) openJoin(room);
            else if (!room) error('Приглашение недоступно: комната закрыта или партия уже началась.');
            history.replaceState(null, '', '/room-list/');
        }
    }
    function openJoin(room) {
        selectedRoom = room;
        $('joinTitle').textContent = room.name;
        $('joinDescription').textContent = `Подключиться к комнате? ${room.usersCount}/${room.userMaxCount}`;
        $('joinForm').reset();
        show('joinPasswordLabel', room.isPrivate);
        $('joinForm').elements.password.required = room.isPrivate;
        $('joinDialog').showModal();
    }
    function render() {
        const connected = connection?.state === signalR.HubConnectionState.Connected;
        const mine = state.players.find(p => p.id === me.id);
        const owner = state.ownerId === me.id;
        if (page === 'room') {
            $('roomName').textContent = state.name;
            $('roomSettings').textContent = `${state.settings.isPrivate ? '🔒 ' : ''}${state.theme} · ${state.players.length}/${state.settings.userMaxCount} игроков · ${state.settings.rounds} раунда · ${state.settings.turnSeconds} с/ход`;
            const lobby = state.stage === 'waiting';
            show('lobbyControls', lobby); show('gameInfo', !lobby); show('addBotBtn', lobby && owner);
            $('addBotBtn').disabled = !connected || state.players.length >= state.settings.userMaxCount;
            $('readyBtn').textContent = mine?.ready ? 'Отменить готовность' : 'Готов';
            $('readyBtn').disabled = !connected;
            show('startGameBtn', owner);
            $('startGameBtn').disabled = !connected || state.players.length < 5 || !state.players.every(p => p.ready && p.connected);
            const list = $('players'); list.replaceChildren();
            state.players.forEach(player => {
                const row = element('div', null, `player${state.turnPlayerId === player.id ? ' current' : ''}`);
                row.append(avatar(player));
                const info = element('div', null, 'info');
                info.append(element('span', `${player.nickname}${player.id === state.ownerId ? ' ♛' : ''}${player.id === me.id ? ' (вы)' : ''}`));
                info.append(element('small', player.left ? 'Вышел' : !player.connected ? 'Нет связи' : lobby ? (player.ready ? 'Готов' : 'Не готов') : player.isBot ? 'Бот' : 'В игре', player.ready && lobby ? 'ready' : ''));
                row.append(info);
                if (lobby && owner && player.id !== me.id) {
                    const kick = element('button', '×', 'secondary kick');
                    kick.title = `Исключить ${player.nickname}`; kick.setAttribute('aria-label', kick.title);
                    kick.disabled = !connected;
                    kick.addEventListener('click', safe(async () => { if (confirm(kick.title + '?')) await invoke('KickUser', player.id); }));
                    row.append(kick);
                }
                list.append(row);
            });
            $('roundLabel').textContent = `Раунд ${state.round} из ${state.settings.rounds}`;
            $('themeValue').textContent = state.theme;
            $('wordValue').textContent = state.isSpy ? 'Вы шпион' : state.word || '';
            const myTurn = state.stage === 'round' && state.turnPlayerId === me.id;
            $('turnStatus').textContent = myTurn ? 'Ваш ход! Напишите сообщение.' : `Ход игрока: ${state.players.find(p => p.id === state.turnPlayerId)?.nickname || ''}`;
            $('turnStatus').classList.toggle('mine', myTurn);
            $('chatInput').disabled = !connected || !myTurn;
            $('sendBtn').disabled = !connected || !myTurn;
            $('chatInput').placeholder = lobby ? 'Чат будет доступен в игре' : myTurn ? 'Ваше сообщение…' : 'Дождитесь своего хода';
            const chat = $('chatArea');
            const atBottom = chat.scrollHeight - chat.scrollTop - chat.clientHeight < 60;
            chat.replaceChildren();
            (state.messages || []).forEach(message => {
                const item = element('div', null, 'message');
                item.append(element('strong', `${state.players.find(p => p.id === message.playerId)?.nickname || 'Игрок'}: `), document.createTextNode(message.messageBody));
                chat.append(item);
            });
            if (atBottom) chat.scrollTop = chat.scrollHeight;
        } else if (page === 'voting') {
            const grid = $('usersGrid'); grid.replaceChildren();
            state.players.forEach(player => {
                const card = element('button', null, `vote-card${state.myVote === player.id ? ' selected' : ''}`);
                card.setAttribute('aria-pressed', String(state.myVote === player.id));
                card.disabled = !connected || state.stage !== 'voting' || player.id === me.id;
                card.append(avatar(player), element('span', `${player.nickname}${player.id === me.id ? ' (вы)' : ''}`), element('strong', `${player.votes} голосов`), element('small', player.readyToEndVoting ? '✓ Готов завершить' : player.left ? 'Вышел' : 'Выбирает', player.readyToEndVoting ? 'ready' : ''));
                card.addEventListener('click', safe(() => invoke('MakeVote', player.id)));
                grid.append(card);
            });
            $('finishVoteBtn').disabled = !connected || state.stage !== 'voting';
            $('finishVoteBtn').textContent = mine?.readyToEndVoting ? 'Отменить готовность' : 'Готов завершить';
        }
        tickTimer();
    }
    function tickTimer() {
        if (!$('timer') || !state) return;
        if (!state.deadline) { $('timer').textContent = ''; return; }
        const seconds = Math.max(0, Math.ceil((Date.parse(state.deadline) - Date.now() - clockOffset) / 1000));
        $('timer').textContent = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`;
    }
    function renderResult(result) {
        const spy = result.players.find(p => p.id === result.spyId);
        const amSpy = result.spyId === me.id;
        const won = (result.outcome === 'spy') === amSpy;
        $('resultTitle').textContent = result.outcome === 'tie' ? 'Ничья' : won ? 'Победа!' : 'Поражение';
        $('resultMessage').textContent = result.outcome === 'tie'
            ? `Голоса разделились или никто не проголосовал. Шпионом был ${spy.nickname}. Слово: ${result.word}.`
            : `${amSpy ? (won ? 'Вы остались нераскрытым!' : 'Вас вычислили!') : `Шпионом был ${spy.nickname}. ${won ? 'Мирные победили!' : 'Шпион победил!'}`} Слово: ${result.word}.`;
        if (!$('resultDialog').open) $('resultDialog').showModal();
    }
    async function loadHistory() {
        const data = await api(`/api/v1/profile/history?offset=${historyOffset}`);
        $('statistics').textContent = `Партий: ${data.total} · Побед: ${data.wins} · Поражений: ${data.losses} · Ничьих: ${data.ties} · За шпиона: ${data.spyGames}`;
        $('historyList').replaceChildren();
        data.games.forEach(game => {
            const mine = game.players.find(p => p.id === me.id);
            const outcome = game.outcome === 'tie' ? 'Ничья' : (game.outcome === 'spy') === mine.isSpy ? 'Победа' : 'Поражение';
            const item = element('div', null, 'history-entry');
            item.append(element('strong', `${outcome} · ${game.roomName}`), element('small', `${new Date(game.finishedAt).toLocaleString()} · ${mine.isSpy ? 'Шпион' : 'Мирный'} · ${game.theme}: ${game.word}`));
            $('historyList').append(item);
        });
        if (!data.total) $('historyList').append(element('p', 'Вы ещё не сыграли ни одной партии.'));
        $('historyPrev').disabled = historyOffset === 0;
        $('historyNext').disabled = historyOffset + 20 >= data.total;
    }
    document.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => $(button.dataset.close).close()));
    bind('resultClose', 'click', () => {
        sessionStorage.setItem(`result_seen_${me.id}_${state.result.gameId}`, '1');
        $('resultDialog').close(); location.replace('/room/');
    });
    $('resultDialog').addEventListener('cancel', e => e.preventDefault());
    bind('leaveBtn', 'click', async () => {
        if (confirm('Выйти из комнаты? В текущую партию вернуться будет нельзя.')) {
            await post('/api/v1/rooms/my-room/leave'); location.replace('/room-list/');
        }
    });
    bind('readyBtn', 'click', () => invoke('MakeReady', !state.players.find(p => p.id === me.id).ready));
    bind('startGameBtn', 'click', () => invoke('StartGame'));
    bind('addBotBtn', 'click', () => invoke('AddBot'));
    bind('finishVoteBtn', 'click', () => invoke('MakeReadyEndVote', !state.players.find(p => p.id === me.id).readyToEndVoting));
    bind('inviteBtn', 'click', async () => {
        const link = `${location.origin}/room-list/?join=${state.id}`;
        if (navigator.clipboard) { await navigator.clipboard.writeText(link); error('Приглашение скопировано. Пароль сообщите друзьям отдельно.'); }
        else prompt('Скопируйте приглашение:', link);
    });
    bind('chatForm', 'submit', async e => {
        e.preventDefault();
        if (!$('chatInput').value.trim()) return;
        $('sendBtn').disabled = true;
        try { await invoke('MakeTurn', $('chatInput').value); $('chatInput').value = ''; }
        finally { if (state) render(); }
    });
    async function init() {
        if (page === 'home') {
            show('logoutBtn', !!token());
            bind('loginBtn', 'click', () => $('authDialog').showModal());
            bind('logoutBtn', 'click', () => { localStorage.removeItem('jwt_token'); location.reload(); });
            bind('playBtn', 'click', async () => {
                if (!token()) { $('authDialog').showModal(); return; }
                const room = await api('/api/v1/rooms/my-room', {}, true);
                location.href = room ? '/room/' : '/room-list/';
            });
            bind('authForm', 'submit', async e => {
                e.preventDefault();
                const form = new FormData(e.target);
                const result = await post(`/api/v1/auth/${e.submitter?.value || 'login'}`, { username: form.get('username'), password: form.get('password') });
                localStorage.setItem('jwt_token', result);
                location.href = '/room-list/';
            });
            return;
        }
        if (!token()) { location.replace('/'); return; }
        me = await api('/me');
        if (page === 'list') {
            $('profileName').textContent = me.username;
            const [themes, profile] = await Promise.all([api('/api/v1/rooms/themes'), api('/api/v1/profile')]);
            themes.forEach(theme => { const option = element('option', theme); option.value = theme; $('roomTheme').append(option); });
            if (profile.avatarUrl) { $('myAvatar').src = profile.avatarUrl; show('myAvatar', true); }
            bind('createBtn', 'click', () => $('createDialog').showModal());
            $('createForm').elements.isPrivate.addEventListener('change', e => {
                show('passwordLabel', e.target.checked);
                $('createForm').elements.password.disabled = !e.target.checked;
                $('createForm').elements.password.required = e.target.checked;
            });
            bind('createForm', 'submit', async e => {
                e.preventDefault(); const data = Object.fromEntries(new FormData(e.target));
                for (const key of ['userMaxCount', 'rounds', 'turnSeconds']) data[key] = Number(data[key]);
                data.isPrivate = data.isPrivate === 'on';
                await post('/api/v1/rooms', data); location.href = '/room/';
            });
            bind('joinForm', 'submit', async e => {
                e.preventDefault();
                await post(`/api/v1/rooms/${selectedRoom.id}/enter`, { password: e.target.elements.password.value });
                location.href = '/room/';
            });
            bind('avatarFile', 'change', async e => {
                const file = e.target.files[0]; if (!file) return;
                if (file.size > 2097152) throw new Error('Максимальный размер аватара — 2 МБ.');
                const form = new FormData(); form.append('file', file);
                const profile = await api('/api/v1/profile/avatar', { method: 'POST', body: form });
                $('myAvatar').src = profile.avatarUrl; show('myAvatar', true);
            });
            bind('historyBtn', 'click', async () => { historyOffset = 0; await loadHistory(); $('historyDialog').showModal(); });
            bind('historyPrev', 'click', async () => { historyOffset = Math.max(0, historyOffset - 20); await loadHistory(); });
            bind('historyNext', 'click', async () => { historyOffset += 20; await loadHistory(); });
        }
        await connect();
        setInterval(tickTimer, 250);
        // Reconcile missed events as well as reconnects; the server remains authoritative.
        setInterval(safe(refresh), 5000);
        document.addEventListener('visibilitychange', safe(() => document.hidden ? undefined : refresh()));
    }
    safe(init)();
})();
