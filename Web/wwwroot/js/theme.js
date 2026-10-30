(function () {
    var KEY = 'spy-theme';
    var root = document.documentElement;
    var MOON = '<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M15.2 2.4a8.8 8.8 0 1 0 6.4 14.9A7.4 7.4 0 0 1 15.2 2.4z"/></svg>';
    var SUN = '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4" fill="none" stroke="currentColor" stroke-width="2"/><path stroke="currentColor" stroke-width="2" stroke-linecap="round" d="M12 2.4v2.3M12 19.3v2.3M2.4 12h2.3M19.3 12h2.3M4.9 4.9l1.6 1.6M17.5 17.5l1.6 1.6M19.1 4.9l-1.6 1.6M6.5 17.5l-1.6 1.6"/></svg>';

    function readSaved() {
        try {
            var saved = localStorage.getItem(KEY);
            if (saved === 'dark' || saved === 'light') return saved;
        } catch (e) {}
        return null;
    }

    function systemTheme() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function apply(theme, persist) {
        if (theme === 'dark') root.setAttribute('data-theme', 'dark');
        else root.removeAttribute('data-theme');

        if (persist) {
            try { localStorage.setItem(KEY, theme); } catch (e) {}
        }

        var btn = document.getElementById('themeToggle');
        if (btn) {
            var dark = theme === 'dark';
            btn.setAttribute('aria-pressed', dark ? 'true' : 'false');
            btn.setAttribute('aria-label', dark ? 'Включить светлую тему' : 'Включить тёмную тему');
            btn.innerHTML = dark ? SUN : MOON;
        }

        var meta = document.querySelector('meta[name="theme-color"]');
        if (!meta && document.head) {
            meta = document.createElement('meta');
            meta.setAttribute('name', 'theme-color');
            document.head.appendChild(meta);
        }
        if (meta) meta.setAttribute('content', theme === 'dark' ? '#14111E' : '#FFCF3F');
    }

    apply(readSaved() || systemTheme(), false);

    function mount() {
        var btn = document.getElementById('themeToggle');
        if (!btn) {
            btn = document.createElement('button');
            btn.type = 'button';
            btn.id = 'themeToggle';
            btn.className = 'theme-btn';
            btn.addEventListener('click', function () {
                apply(root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark', true);
            });

            var header = document.querySelector('.header-right');
            var roomActions = document.querySelector('.top-ui-right');
            var timer = document.querySelector('.timer-wrap');
            if (header) {
                header.prepend(btn);
            } else if (roomActions) {
                roomActions.prepend(btn);
            } else if (timer) {
                timer.prepend(btn);
            } else {
                var page = document.querySelector('.page');
                var logo = page && page.querySelector('.logo');
                if (page && logo) {
                    var bar = document.createElement('div');
                    bar.className = 'top';
                    var actions = document.createElement('div');
                    actions.className = 'header-right';
                    logo.before(bar);
                    bar.appendChild(logo);
                    actions.appendChild(btn);
                    bar.appendChild(actions);
                }
            }
        }

        apply(root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light', false);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', mount);
    else mount();

    window.addEventListener('storage', function (event) {
        if (event.key === KEY && (event.newValue === 'dark' || event.newValue === 'light')) {
            apply(event.newValue, false);
        }
    });

    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (event) {
            if (readSaved()) return;
            apply(event.matches ? 'dark' : 'light', false);
        });
    }
})();
