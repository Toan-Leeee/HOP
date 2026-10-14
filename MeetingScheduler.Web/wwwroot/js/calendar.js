(() => {
  const calendar = document.querySelector('#meeting-calendar');
  if (!calendar) return;
  const list = document.querySelector('#meeting-list');
  const grid = calendar.querySelector('[data-calendar-grid]');
  const status = calendar.querySelector('[data-calendar-status]');
  const title = calendar.querySelector('[data-calendar-title]');
  let week = startOfWeek(new Date()), pending;
  const formatDay = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
  const formatTime = new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit' });
  function startOfWeek(date) {
    const result = new Date(date); result.setHours(0, 0, 0, 0);
    result.setDate(result.getDate() - (result.getDay() + 6) % 7); return result;
  }
  function nextDay(date, amount) { const result = new Date(date); result.setDate(result.getDate() + amount); return result; }
  function localDate(date) {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}T00:00:00`;
  }
  function element(tag, className, text) {
    const item = document.createElement(tag); item.className = className; item.textContent = text; return item;
  }
  async function loadWeek() {
    pending?.abort(); pending = new AbortController(); const signal = pending.signal;
    const end = nextDay(week, 7); const items = [];
    title.textContent = `${formatDay.format(week)} – ${formatDay.format(nextDay(week, 6))}`;
    grid.replaceChildren(); status.textContent = 'Đang tải lịch…';
    try {
      for (let page = 1; ; page++) {
        const url = new URL(calendar.dataset.calendarUrl, location.origin);
        url.search = new URLSearchParams({ From: localDate(week), To: localDate(end), PageSize: '100', Page: String(page), SortOrder: 'start' });
        const response = await fetch(url, { signal, headers: { Accept: 'application/json' } });
        if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error('Calendar unavailable');
        const data = await response.json(); items.push(...data.items);
        if (page >= data.totalPages) break;
      }
      if (signal.aborted) return;
      status.textContent = items.length ? `${items.length} cuộc họp` : 'Chưa có cuộc họp trong tuần này.';
      for (let offset = 0; offset < 7; offset++) {
        const day = nextDay(week, offset), tomorrow = nextDay(day, 1);
        const column = element('section', 'week-day', '');
        const heading = element('h3', day.toDateString() === new Date().toDateString() ? 'today' : '', formatDay.format(day));
        column.append(heading);
        const events = items.filter(item => new Date(item.startTime) < tomorrow && new Date(item.endTime) > day);
        if (!events.length) column.append(element('span', 'calendar-empty', 'Không có lịch'));
        for (const item of events) {
          const cancelled = item.status === 1 || item.status === 'Cancelled';
          const link = element('a', `calendar-event${cancelled ? ' cancelled' : ''}`, '');
          const url = new URL(calendar.dataset.detailsUrl, location.origin); url.searchParams.set('id', item.meetingId); link.href = url.pathname + url.search;
          link.append(element('span', 'calendar-time', `${formatTime.format(new Date(item.startTime))} – ${formatTime.format(new Date(item.endTime))}`));
          link.append(element('strong', '', item.title)); link.append(element('span', '', item.roomName));
          if (cancelled) link.append(element('span', '', 'Đã hủy'));
          column.append(link);
        }
        grid.append(column);
      }
    } catch (error) {
      if (error.name !== 'AbortError') status.textContent = 'Không tải được lịch. Vui lòng thử lại.';
    }
  }
  document.querySelectorAll('[data-meeting-mode]').forEach(button => button.addEventListener('click', () => {
    const showCalendar = button.dataset.meetingMode === 'calendar'; list.hidden = showCalendar; calendar.hidden = !showCalendar;
    document.querySelectorAll('[data-meeting-mode]').forEach(item => { const active = item === button; item.classList.toggle('active', active); item.setAttribute('aria-pressed', String(active)); });
    if (showCalendar) loadWeek(); else pending?.abort();
  }));
  calendar.querySelector('[data-calendar-prev]').addEventListener('click', () => { week = nextDay(week, -7); loadWeek(); });
  calendar.querySelector('[data-calendar-next]').addEventListener('click', () => { week = nextDay(week, 7); loadWeek(); });
  calendar.querySelector('[data-calendar-today]').addEventListener('click', () => { week = startOfWeek(new Date()); loadWeek(); });
})();
