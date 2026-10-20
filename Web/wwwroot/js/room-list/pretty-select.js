function closePrettySelects(except) {
    document.querySelectorAll('.pretty-select.is-open').forEach((wrap) => {
        if (wrap === except) return;
        wrap.classList.remove('is-open');
        const menu = wrap.querySelector('.pretty-select-menu');
        const button = wrap.querySelector('.pretty-select-btn');
        if (menu) menu.hidden = true;
        if (button) button.setAttribute('aria-expanded', 'false');
    });
}

function enhanceSelect(select) {
    if (select.closest('.pretty-select')) return;

    const wrap = document.createElement('div');
    wrap.className = 'pretty-select';
    select.parentNode.insertBefore(wrap, select);
    wrap.appendChild(select);
    select.tabIndex = -1;
    select.setAttribute('aria-hidden', 'true');

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'pretty-select-btn';
    button.setAttribute('aria-haspopup', 'listbox');
    button.setAttribute('aria-expanded', 'false');

    const valueEl = document.createElement('span');
    valueEl.className = 'pretty-select-value';

    const chevron = document.createElement('span');
    chevron.className = 'pretty-select-chevron';
    chevron.setAttribute('aria-hidden', 'true');

    button.append(valueEl, chevron);

    const menu = document.createElement('ul');
    menu.className = 'pretty-select-menu';
    menu.setAttribute('role', 'listbox');
    menu.hidden = true;

    wrap.append(button, menu);

    let activeIndex = select.selectedIndex;

    function render(keepActive) {
        const selectedIndex = select.selectedIndex;
        const selected = select.options[selectedIndex];
        valueEl.textContent = selected ? selected.textContent : '';
        if (!keepActive) activeIndex = selectedIndex;

        menu.innerHTML = '';
        Array.from(select.options).forEach((option, index) => {
            const item = document.createElement('li');
            item.className = 'pretty-select-option';
            item.setAttribute('role', 'option');
            item.dataset.value = option.value;
            item.textContent = option.textContent;
            const isSelected = index === selectedIndex;
            item.setAttribute('aria-selected', isSelected ? 'true' : 'false');
            if (isSelected) item.classList.add('is-selected');
            if (index === activeIndex) item.classList.add('is-active');
            item.addEventListener('click', (event) => {
                event.preventDefault();
                event.stopPropagation();
                choose(index);
            });
            menu.appendChild(item);
        });
    }

    function choose(index) {
        const option = select.options[index];
        if (!option) return;
        select.value = option.value;
        select.dispatchEvent(new Event('change', { bubbles: true }));
        closeMenu();
        render(false);
    }

    function openMenu() {
        closePrettySelects(wrap);
        activeIndex = Math.max(select.selectedIndex, 0);
        render(true);
        menu.hidden = false;
        wrap.classList.add('is-open');
        button.setAttribute('aria-expanded', 'true');
        const active = menu.children[activeIndex];
        if (active) active.scrollIntoView({ block: 'nearest' });
    }

    function closeMenu() {
        menu.hidden = true;
        wrap.classList.remove('is-open');
        button.setAttribute('aria-expanded', 'false');
    }

    function moveActive(step) {
        if (!select.options.length) return;
        if (menu.hidden) {
            openMenu();
            return;
        }
        activeIndex = (activeIndex + step + select.options.length) % select.options.length;
        render(true);
        const active = menu.children[activeIndex];
        if (active) active.scrollIntoView({ block: 'nearest' });
    }

    button.addEventListener('click', (event) => {
        event.preventDefault();
        event.stopPropagation();
        if (wrap.classList.contains('is-open')) closeMenu();
        else openMenu();
    });

    button.addEventListener('keydown', (event) => {
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            moveActive(1);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            moveActive(-1);
        } else if (event.key === 'Enter' || event.key === ' ') {
            if (!menu.hidden) {
                event.preventDefault();
                choose(activeIndex);
            }
        } else if (event.key === 'Escape') {
            closeMenu();
        }
    });

    select.addEventListener('change', () => render(false));

    const observer = new MutationObserver(() => render(false));
    observer.observe(select, { childList: true });

    render(false);
}

document.addEventListener('click', () => closePrettySelects());

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('select.line-select').forEach(enhanceSelect);
});
