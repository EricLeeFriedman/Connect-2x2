// Temporary browser smoke test of the PR build. Prints screenshots as base64 so they can be read from the log.
import { chromium } from 'playwright-core';

const base = 'http://127.0.0.1:8080/Connect-2x2/';
const browser = await chromium.launch({ channel: 'chrome' });
const errors = [];
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${what}`); if (!ok) failed++; };
const shot = async (page, name) => {
  const buf = await page.screenshot({ type: 'jpeg', quality: 50, fullPage: true });
  const b64 = buf.toString('base64');
  console.log(`SHOT-BEGIN ${name}`);
  for (let i = 0; i < b64.length; i += 4000) console.log('SHOT ' + b64.slice(i, i + 4000));
  console.log(`SHOT-END ${name}`);
};

const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));

await page.goto(base);
check((await page.locator('a.game').count()) === 4, 'landing page lists four games');

await page.goto(base + 'tictactoe/');
await page.waitForSelector('.board .cell', { timeout: 30000 });
check((await page.locator('.cell').count()) === 9, 'tic-tac-toe still loads');

await page.goto(base + 'snakes/');
await page.waitForSelector('.board .label', { timeout: 30000 });
check((await page.locator('.label').count()) === 100, '100 squares');
check((await page.locator('.label .fact').count()) === 100, 'every square shows a fact');
check((await page.locator('.snake').count()) === 8 && (await page.locator('.ladder').count()) === 8, '8 snakes and 8 ladders');
await shot(page, 'start');

const answerCard = async (right) => {
  const nums = await page.locator('.equation > span').allInnerTexts();
  const value = right ? Number(nums[0]) * Number(nums[2]) : 1;
  for (const d of String(value)) await page.locator('.numpad button', { hasText: new RegExp(`^${d}$`) }).click();
  await page.locator('.key-enter').click();
  await page.waitForSelector('.question-card', { state: 'detached' });
  await page.waitForFunction(() => !document.querySelector('.roll')?.disabled || document.querySelector('.status.over'), null, { timeout: 15000 });
};

await page.locator('.roll').click();
await page.waitForSelector('.question-card', { timeout: 5000 });
check((await page.locator('.label.target').count()) === 1, 'the landing square is highlighted');
await shot(page, 'question');
await answerCard(false);
check((await page.locator('.feedback.bad').count()) === 1, 'a wrong answer says so');
check((await page.locator('.score').first().innerText()).includes('Start'), 'a wrong answer leaves Red at the start');
check((await page.locator('.turn').innerText()).includes('Blue'), 'and passes the turn to Blue');

await page.locator('.roll').click();
await page.waitForSelector('.question-card', { timeout: 5000 });
await answerCard(true);
check((await page.locator('.feedback.good').count()) === 1, 'a right answer says so');
check(!(await page.locator('.score').nth(1).innerText()).includes('Start'), 'a right answer moves Blue onto the board');
check((await page.locator('.tokens .token').count()) === 1, 'Blue\'s token is on the board');

// Play a while with keyboard input to cover snakes, ladders and the end of the game.
for (let i = 0; i < 400 && !(await page.locator('.status.over').count()); i++) {
  const feedback = await page.locator('.feedback').count() ? await page.locator('.feedback').innerText() : '';
  if (/ladder|snake/i.test(feedback) && i < 60) console.log('  ' + feedback);
  await page.locator('.roll').click();
  const card = await page.waitForSelector('.question-card', { timeout: 2500 }).catch(() => null);
  if (!card) { await page.waitForFunction(() => !document.querySelector('.roll')?.disabled, null, { timeout: 5000 }); continue; }
  const nums = await page.locator('.equation > span').allInnerTexts();
  await page.keyboard.type(String(Number(nums[0]) * Number(nums[2])));
  await page.keyboard.press('Enter');
  await page.waitForSelector('.question-card', { state: 'detached' });
  await page.waitForFunction(() => !document.querySelector('.roll')?.disabled || document.querySelector('.status.over'), null, { timeout: 15000 });
}
check((await page.locator('.status.over').count()) === 1, 'someone reaches 100 and wins');
check((await page.locator('.turn').innerText()).includes('wins'), 'the winner is announced');
await shot(page, 'win');

await page.locator('label', { hasText: /^\s*4\s*$/ }).click();
check((await page.locator('.score').count()) === 4, 'four players');
await page.setViewportSize({ width: 1280, height: 900 });
await shot(page, 'desktop');

check(errors.length === 0, 'no console errors' + (errors.length ? ': ' + errors.join(' | ') : ''));
await browser.close();
process.exit(failed ? 1 : 0);
