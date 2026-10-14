document.addEventListener('DOMContentLoaded', () => {
window.lucide?.createIcons();
document.querySelectorAll('[data-confirm]').forEach(form => form.addEventListener('submit', event => { if (!window.confirm(form.dataset.confirm)) event.preventDefault(); }));
document.querySelectorAll('[data-print]').forEach(button => button.addEventListener('click', () => window.print()));
document.querySelectorAll('[data-participant-search]').forEach(input => input.addEventListener('input', () => {
const query = input.value.trim().toLocaleLowerCase('vi');
document.querySelectorAll('[data-participant]').forEach(row => { row.hidden = !row.textContent.toLocaleLowerCase('vi').includes(query); });
}));
});
