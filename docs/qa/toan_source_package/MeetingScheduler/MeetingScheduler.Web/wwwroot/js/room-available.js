(() => {
  const form = document.querySelector('[data-meeting-form]');
  if (!form) return;
  const button = form.querySelector('[data-check-availability]');
  const status = form.querySelector('[data-availability-status]');
  button.addEventListener('click', async () => {
    const start = form.elements.StartTime.value, end = form.elements.EndTime.value;
    if (!start || !end || new Date(end) <= new Date(start)) { status.textContent = 'Vui lòng chọn thời gian bắt đầu và kết thúc hợp lệ.'; return; }
    const selected = Array.from(form.querySelectorAll('[name="ParticipantIds"]:checked'), input => input.value);
    const actor = form.dataset.currentUserId;
    const minimumCapacity = new Set(actor ? [...selected, actor] : selected).size || 1;
    const url = new URL(form.dataset.availabilityUrl, location.origin);
    url.search = new URLSearchParams({ start, end, minimumCapacity: String(minimumCapacity) });
    if (Number(form.elements.MeetingId.value) > 0) url.searchParams.set('excludeMeetingId', form.elements.MeetingId.value);
    button.disabled = true; status.textContent = 'Đang kiểm tra…';
    try {
      const response = await fetch(url, { headers: { Accept: 'application/json' } });
      if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error('Unavailable');
      const rooms = await response.json(); const select = form.elements.RoomId; const selectedRoom = select.value;
      select.replaceChildren(new Option('Chọn phòng họp', ''));
      for (const room of rooms) select.add(new Option(`${room.roomName} · ${room.capacity} chỗ`, String(room.roomId)));
      select.value = rooms.some(room => String(room.roomId) === selectedRoom) ? selectedRoom : '';
      status.textContent = rooms.length ? `${rooms.length} phòng trống phù hợp.` : 'Không có phòng trống phù hợp trong khoảng thời gian này.';
    } catch { status.textContent = 'Không kiểm tra được phòng trống. Vui lòng thử lại.'; }
    finally { button.disabled = false; }
  });
})();
