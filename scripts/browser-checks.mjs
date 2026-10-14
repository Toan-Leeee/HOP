import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const base = process.env.MEETING_URL || 'http://localhost:5086';
const output = path.resolve('docs/qa');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
const page = await context.newPage();
const failures = [], results = [], consoleErrors = [];
page.on('pageerror', error => consoleErrors.push(error.message));
const assert = (condition, name) => { results.push({ name, passed: Boolean(condition) }); if (!condition) failures.push(name); };
async function visit(route) {
  const response = await page.goto(base + route, { waitUntil: 'networkidle' });
  assert(response.status() === 200, 'GET ' + route);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1);
  assert(!overflow, 'No page overflow ' + route);
}
async function login(email) {
  await page.goto(base + '/Account/Login');
  await page.locator('[name="Email"]').fill(email);
  await page.locator('[name="Password"]').fill('Demo@12345');
  await Promise.all([page.waitForURL(url => !url.pathname.includes('/Account/Login')), page.locator('form button[type="submit"]').click()]);
}
try {
  let ready = false;
  for (let attempt = 0; attempt < 30; attempt++) {
    try { ready = (await fetch(base + '/Account/Login')).ok; } catch { }
    if (ready) break;
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  assert(ready, 'Server is ready');
  if (!ready) throw new Error('Server did not become ready within 15 seconds');
  await visit('/Account/Login');
  await page.screenshot({ path: path.join(output, 'login-desktop.png'), fullPage: true });
  await login('admin@company.test');
  for (const route of ['/', '/Admin/Room', '/Admin/Room/Create', '/Admin/Room/Details/1', '/Admin/Room/Edit/1', '/Admin/Room/Delete/1', '/Admin/Department', '/Admin/Department/Create', '/Admin/Department/Edit/1', '/Admin/Department/Details/1', '/Admin/Department/Delete/1', '/Admin/User', '/Admin/User/Create', '/Admin/User/Edit/3', '/Admin/User/Details/3', '/Admin/User/Delete/3', '/Meeting/Create', '/Meeting/Details/1', '/Meeting/Edit/1', '/Meeting/Cancel/1', '/Meeting/RsvpTracking/1', '/Admin/Meeting', '/Admin/Report', '/Profile', '/Profile/ChangePassword']) await visit(route);
  const api = await context.request.get(base + '/api/meetings');
  assert(api.status() === 200, 'Authenticated calendar API');
  const list = await api.json();
  assert(list.totalCount === 3 && list.items.length === 3, 'Seed calendar has three meetings');
  const csrf = await context.request.post(base + '/Admin/Room/Delete/1');
  assert(csrf.status() === 400, 'Mutation without antiforgery is rejected');
  await visit('/Meeting/Create');
  const token = await page.locator('form[data-meeting-form] input[name="__RequestVerificationToken"]').inputValue();
  const booking = list.items.find(m => m.meetingId === 1);
  const rejected = await context.request.post(base + '/Meeting/Create', { form: {
    __RequestVerificationToken: token, Title: 'Overlap validation check', RoomId: '1',
    StartTime: booking.startTime, EndTime: booking.endTime, MeetingId: '0'
  } });
  const rejectionText = await page.evaluate(html => new DOMParser().parseFromString(html, 'text/html').body.textContent, await rejected.text());
  assert(rejected.status() === 200 && rejectionText.includes('đã được đặt'), 'Overlapping form submission returns validation');
  await page.locator('[name="StartTime"]').fill(booking.startTime.slice(0, 16));
  await page.locator('[name="EndTime"]').fill(booking.endTime.slice(0, 16));
  await page.locator('[data-check-availability]').click();
  await page.locator('[data-availability-status]').filter({ hasText: /phòng trống/ }).waitFor();
  assert(await page.locator('select[name="RoomId"] option[value="1"]').count() === 0, 'Occupied room removed from availability selector');
  await visit('/Admin/Room?SearchString=Lotus&SortOrder=capacity_desc&PageSize=1&Page=1');
  assert(await page.locator('tbody tr').count() === 1 && (await page.locator('tbody').innerText()).includes('Lotus'), 'Room search/sort/paging HTML');
  await visit('/');
  await page.screenshot({ path: path.join(output, 'meetings-desktop.png'), fullPage: true });
  await page.locator('[data-meeting-mode="calendar"]').click();
  await page.locator('.week-day').first().waitFor();
  assert(await page.locator('.week-day').count() === 7 && await page.locator('.calendar-event').count() === 2, 'Week calendar renders seven days and two seed meetings');
  await page.screenshot({ path: path.join(output, 'calendar-desktop.png'), fullPage: true });
  await page.locator('[data-calendar-next]').click();
  await page.locator('[data-calendar-status]').filter({ hasText: 'Chưa có cuộc họp' }).waitFor();
  assert(await page.locator('.calendar-event').count() === 0, 'Next week updates calendar');
  await visit('/Admin/Report?From=2026-10-01T00:00&To=2026-11-01T00:00');
  await page.screenshot({ path: path.join(output, 'report-desktop.png'), fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  for (const route of ['/', '/Admin/Room', '/Meeting/Create', '/Admin/Report', '/Profile']) await visit(route);
  await visit('/');
  await page.screenshot({ path: path.join(output, 'meetings-mobile.png'), fullPage: true });
  await page.locator('[data-meeting-mode="calendar"]').click();
  await page.locator('.week-day').first().waitFor();
  assert(!(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1)), 'Calendar fits mobile viewport');
  await page.screenshot({ path: path.join(output, 'calendar-mobile.png'), fullPage: true });
  const toggle = page.locator('.navbar-toggler');
  await toggle.click();
  await page.locator('#app-nav.show').waitFor({ state: 'visible' });
  assert(await page.locator('#app-nav').isVisible(), 'Mobile navigation opens');
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.locator('form[action="/Account/Logout"] button').click();
  await login('employee@company.test');
  await visit('/');
  for (const route of ['/Admin/Room', '/Meeting/Create']) {
    const response = await page.goto(base + route);
    assert(response.status() === 403, 'Employee denied ' + route);
  }
  await visit('/Meeting/Details/1');
  assert(consoleErrors.length === 0, 'No browser script errors');
} catch (error) { failures.push(error.stack); }
finally {
  await browser.close();
  await writeFile(path.join(output, 'browser-results.json'), JSON.stringify({ results, consoleErrors, failures }, null, 2));
  console.log(JSON.stringify({ checks: results.length, passed: results.filter(r => r.passed).length, failures }, null, 2));
}
process.exitCode = failures.length ? 1 : 0;
