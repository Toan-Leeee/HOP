(() => {
    'use strict';
    const app = document.getElementById('room-app');
    if (!app) return;
    const filters = document.getElementById('room-filters');
    const editor = document.getElementById('room-editor');
    const rows = document.getElementById('room-rows');
    const modalNode = document.getElementById('room-modal');
    const modal = new bootstrap.Modal(modalNode);
    const notice = document.getElementById('room-notice');
    const errors = document.getElementById('room-errors');
    const loading = document.getElementById('room-loading');
    const prev = document.getElementById('room-prev');
    const next = document.getElementById('room-next');
    let page = Math.max(1, Number(new URLSearchParams(location.search).get('Page')) || 1);
    let totalPages = 0, sequence = 0, pending = null, timer, mutating = false;
    let items = new Map(), closingModal = false;
    const message = (text, bad = false) => {
        notice.textContent = text;
        notice.className = 'alert ' + (bad ? 'alert-danger' : 'alert-success');
        notice.hidden = false;
    };
    const busy = value => {
        document.getElementById('room-table-wrap').setAttribute('aria-busy', String(value));
        loading.textContent = value ? 'Đang tải phòng họp…' : '';
        prev.disabled = value || page <= 1;
        next.disabled = value || page >= totalPages;
    };
    const iconButton = (icon, label, action, id) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-secondary';
        button.title = label;
        button.setAttribute('aria-label', label);
        button.dataset.action = action;
        button.dataset.id = id;
        button.style.width = '36px';
        button.style.height = '36px';
        const symbol = document.createElement('i');
        symbol.dataset.lucide = icon;
        button.append(symbol);
        return button;
    };
    function render(data) {
        items = new Map(data.items.map(item => [String(item.roomId), item]));
        rows.replaceChildren();
        for (const item of data.items) {
            const row = document.createElement('tr');
            for (const text of [item.roomName, item.capacity + ' người', item.equipment || '']) {
                const cell = document.createElement('td');
                cell.textContent = text;
                row.append(cell);
            }
            row.cells[0].className = 'primary-cell';
            const small = document.createElement('small');
            small.textContent = item.location || '';
            row.cells[0].append(small);
            const status = document.createElement('td');
            const badge = document.createElement('span');
            badge.className = 'status' + (item.status === 'Maintenance' ? ' status-warning' : '');
            badge.textContent = item.status === 'Maintenance' ? 'Bảo trì' : 'Sẵn sàng';
            status.append(badge);
            row.append(status);
            const actions = document.createElement('td');
            const group = document.createElement('div');
            group.className = 'row-actions';
            group.append(iconButton('pencil', 'Sửa phòng ' + item.roomName, 'edit', item.roomId));
            group.append(iconButton(item.isDisabled ? 'rotate-ccw' : 'ban',
                (item.isDisabled ? 'Khôi phục ' : 'Ngừng sử dụng ') + item.roomName,
                item.isDisabled ? 'restore' : 'disable', item.roomId));
            actions.append(group);
            row.append(actions);
            rows.append(row);
        }
        if (!data.items.length) {
            const cell = rows.insertRow().insertCell();
            cell.colSpan = 5;
            cell.textContent = 'Không có phòng họp phù hợp.';
        }
        page = data.page;
        totalPages = data.totalPages;
        document.getElementById('room-count').textContent = data.totalCount + ' phòng họp';
        document.getElementById('room-page').textContent = 'Trang ' + page + ' / ' + Math.max(1, totalPages);
        window.lucide?.createIcons();
    }
    async function load(transport = 'xhr') {
        const request = ++sequence;
        pending?.abort();
        const params = new URLSearchParams(new FormData(filters));
        for (const [key, value] of [...params]) if (!value) params.delete(key);
        params.set('Page', page);
        const url = app.dataset.list + '?' + params;
        busy(true);
        try {
            let data;
            if (transport === 'fetch') {
                const controller = new AbortController();
                pending = controller;
                const response = await fetch(url, { signal: controller.signal, headers: { Accept: 'application/json' } });
                if (!response.ok || !response.headers.get('content-type')?.includes('application/json'))
                    throw new Error('Không tải được dữ liệu. Hãy kiểm tra đăng nhập và bộ lọc.');
                data = await response.json();
            } else {
                data = await new Promise((resolve, reject) => {
                    const xhr = new XMLHttpRequest();
                    pending = xhr;
                    xhr.open('GET', url);
                    xhr.responseType = 'json';
                    xhr.timeout = 15000;
                    xhr.onload = () => xhr.status === 200 && Array.isArray(xhr.response?.items)
                        ? resolve(xhr.response) : reject(new Error('Không tải được dữ liệu. Hãy kiểm tra đăng nhập và bộ lọc.'));
                    xhr.onerror = xhr.ontimeout = () => reject(new Error('Không kết nối được máy chủ. Vui lòng thử lại.'));
                    xhr.onabort = () => reject(new DOMException('Cancelled', 'AbortError'));
                    xhr.send();
                });
            }
            if (request !== sequence) return;
            render(data);
            params.set('Page', page);
            history.replaceState(null, '', location.pathname + '?' + params);
        } catch (error) {
            if (request === sequence && error.name !== 'AbortError') {
                rows.replaceChildren();
                items.clear();
                totalPages = 0;
                document.getElementById('room-count').textContent = '';
                document.getElementById('room-page').textContent = '';
                message(error.message, true);
            }
        } finally {
            if (request === sequence) busy(false);
        }
    }
    function openEditor(item) {
        if (closingModal) {
            modalNode.addEventListener('hidden.bs.modal', () => openEditor(item), { once: true });
            return;
        }
        editor.reset();
        errors.hidden = true;
        for (const [name, value] of Object.entries({ RoomId: item?.roomId || 0,
            RoomName: item?.roomName || '', Location: item?.location || '',
            Capacity: item?.capacity || 1, Equipment: item?.equipment || '', Status: item?.status || 'Active' }))
            editor.elements.namedItem(name).value = value;
        document.getElementById('room-modal-title').textContent = item ? 'Sửa phòng họp' : 'Thêm phòng họp';
        modal.show();
    }
    // jQuery serializes the antiforgery token with each write request.
    async function write(url, data) {
        return $.ajax({ url, method: 'POST', data, dataType: 'json', timeout: 15000 });
    }
    const failure = xhr => {
        const body = xhr.responseJSON;
        return body?.errors ? Object.values(body.errors).flat().join(' ') :
            body?.message || 'Yêu cầu chưa thành công. Kiểm tra đăng nhập/kết nối và tải lại danh sách trước khi thử lại.';
    };
    editor.addEventListener('submit', async event => {
        event.preventDefault();
        if (mutating || !editor.reportValidity()) return;
        mutating = true;
        document.getElementById('room-save').disabled = true;
        modalNode.querySelectorAll('[data-bs-dismiss]').forEach(button => button.disabled = true);
        errors.hidden = true;
        try {
            const result = await write(app.dataset.save, $(editor).serialize());
            mutating = false;
            modal.hide();
            message(result.message);
            await load();
        } catch (xhr) {
            errors.textContent = failure(xhr);
            errors.hidden = false;
        } finally {
            mutating = false;
            document.getElementById('room-save').disabled = false;
            modalNode.querySelectorAll('[data-bs-dismiss]').forEach(button => button.disabled = false);
        }
    });
    modalNode.addEventListener('hide.bs.modal', event => {
        if (mutating) event.preventDefault();
        else closingModal = true;
    });
    modalNode.addEventListener('hidden.bs.modal', () => { closingModal = false; });
    modalNode.addEventListener('shown.bs.modal', () => document.getElementById('room-name').focus());
    rows.addEventListener('click', async event => {
        const button = event.target.closest('button[data-action]');
        if (!button || mutating) return;
        const item = items.get(button.dataset.id);
        if (!item) return;
        if (button.dataset.action === 'edit') return openEditor(item);
        const restoring = button.dataset.action === 'restore';
        if (!confirm((restoring ? 'Khôi phục' : 'Ngừng sử dụng') + ' phòng ' + item.roomName + '?')) return;
        mutating = true;
        button.disabled = true;
        try {
            const result = await write(restoring ? app.dataset.restore : app.dataset.disable, {
                id: item.roomId, __RequestVerificationToken: editor.elements.namedItem('__RequestVerificationToken').value
            });
            message(result.message);
            await load();
        } catch (xhr) { message(failure(xhr), true); }
        finally { mutating = false; button.disabled = false; }
    });
    document.getElementById('room-add').addEventListener('click', () => { if (!mutating) openEditor(); });
    filters.addEventListener('submit', event => { event.preventDefault(); clearTimeout(timer); page = 1; load('fetch'); });
    filters.addEventListener('change', event => {
        if (event.target.id === 'search') return;
        clearTimeout(timer); page = 1; load('fetch');
    });
    document.getElementById('search').addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(() => { page = 1; load('fetch'); }, 300);
    });
    document.getElementById('room-reset').addEventListener('click', () => {
        clearTimeout(timer);
        filters.reset();
        for (const name of ['SearchString', 'Status', 'MinimumCapacity', 'SortOrder']) filters.elements.namedItem(name).value = '';
        filters.elements.namedItem('IsDisabled').value = 'false';
        filters.elements.namedItem('PageSize').value = '10';
        page = 1;
        load('fetch');
    });
    prev.addEventListener('click', () => { if (page > 1) { page--; load(); } });
    next.addEventListener('click', () => { if (page < totalPages) { page++; load(); } });
    load();
})();
