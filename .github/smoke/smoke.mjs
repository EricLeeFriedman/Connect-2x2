// Temporary browser smoke test of the PR build. Prints screenshots as base64 so they can be read from the log.
import { chromium } from 'playwright-core';

const base = 'http://127.0.0.1:8080/Connect-2x2/';
const browser = await chromium.launch({ channel: 'chrome' });
const errors = [];
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${what}`); if (!ok) failed++; };
const shot = async (page, name) => {
  const buf = await page.screenshot({ type: 'jpeg', quality: 55, fullPage: true });
  const b64 = buf.toString('base64');
  console.log(`SHOT-BEGIN ${name}`);
  for (let i = 0; i < b64.length; i += 4000) console.log('SHOT ' + b64.slice(i, i + 4000));
  console.log(`SHOT-END ${name}`);
};

const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));

await page.goto(base);
check((await page.locator('a.game').count()) === 3, 'landing page lists three games');
await shot(page, 'landing');

await page.goto(base + 'checkers/');
await page.waitForSelector('.board .sq.dark', { timeout: 30000 });
check((await page.locator('.sq.dark').count()) === 32, 'checkers still loads');

await page.goto(base + 'tictactoe/');
await page.waitForSelector('.board .cell', { timeout: 30000 });
check((await page.locator('.cell').count()) === 9, '3 x 3 board');
check((await page.locator('.fact').count()) === 9, 'every square shows a fact');
await shot(page, 'start');

// Answer the fact in square index i.
const answer = async (i, right = true) => {
  await page.locator('.cell').nth(i).click();
  await page.waitForSelector('.question-card');
  const nums = await page.locator('.equation > span').allInnerTexts();
  const value = right ? Number(nums[0]) * Number(nums[2]) : 1;
  for (const d of String(value)) await page.locator('.numpad button', { hasText: new RegExp(`^${d}$`) }).click();
  await page.locator('.key-enter').click();
  await page.waitForSelector('.question-card', { state: 'detached' });
};

await page.locator('.cell').nth(4).click();
await page.waitForSelector('.question-card');
await shot(page, 'question');
await page.keyboard.press('Escape');
await page.waitForSelector('.question-card', { state: 'detached' });

await answer(0);
check((await page.locator('.mark.x').count()) === 1, 'right answer places an X');
check((await page.locator('.turn').innerText()).includes('Player O'), 'turn passes to O');
await answer(3, false);
check((await page.locator('.feedback.bad').count()) === 1 && (await page.locator('.mark.o').count()) === 0, 'wrong answer places nothing');
check((await page.locator('.turn').innerText()).includes('Player X'), 'wrong answer passes the turn');
await answer(1);
await answer(3);
await answer(2);
check((await page.locator('.cell.win').count()) === 3, 'three in a row lights up');
check((await page.locator('.turn').innerText()).includes('X wins'), 'X wins');
await shot(page, 'win');

await page.locator('label', { hasText: '4 × 4' }).click();
check((await page.locator('.cell').count()) === 16, '4 x 4 board');
check((await page.locator('.fact').count()) === 16, '16 facts');
await shot(page, 'four');

await page.setViewportSize({ width: 1280, height: 900 });
await page.locator('label', { hasText: '3 × 3' }).click();
await shot(page, 'desktop');

check(errors.length === 0, 'no console errors' + (errors.length ? ': ' + errors.join(' | ') : ''));
await browser.close();
process.exit(failed ? 1 : 0);
