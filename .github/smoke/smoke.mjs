// Temporary browser smoke test of the PR build. Prints screenshots as base64 so they can be read from the log.
import { chromium } from 'playwright-core';
import { readFileSync } from 'node:fs';

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

const page = await browser.newPage({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce' });
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));

await page.goto(base);
check((await page.locator('a.game').count()) === 2, 'landing page lists two games');

await page.goto(base + 'connect2x2/');
await page.waitForSelector('.board .cell', { timeout: 30000 });
check((await page.locator('.cell').count()) === 42, 'connect2x2 loads under its new path');

await page.goto(base + 'checkers/');
await page.waitForSelector('.board .sq.dark', { timeout: 30000 });
check((await page.locator('.sq.dark').count()) === 32, '32 dark squares');
check((await page.locator('.piece').count()) === 24, '24 pieces');
check((await page.locator('.fact').count()) === 8, '8 open squares show facts');
check((await page.locator('.sq.movable').count()) === 4, '4 red pieces can move');
await shot(page, 'start');

await page.locator('.sq.movable').first().click();
check((await page.locator('.sq.landing').count()) >= 1, 'picking a piece highlights where it can go');
await page.locator('.sq.landing').first().click();
await page.waitForSelector('.question-card');
const nums = await page.locator('.equation > span').allInnerTexts();
const answer = Number(nums[0]) * Number(nums[2]);
await shot(page, 'question');
for (const d of String(answer)) await page.keyboard.press(d);
await page.keyboard.press('Enter');
await page.waitForSelector('.question-card', { state: 'detached' });
check((await page.locator('.feedback.good').count()) === 1, 'right answer shows Correct');
check((await page.locator('.turn').innerText()).includes('Black'), 'turn passes to Black');
check((await page.locator('.fact').count()) === 8, 'still 8 open squares after a move');

// Four wrong answers to reach the shuffle.
for (let i = 0; i < 4; i++) {
  await page.locator('.sq.movable').first().click();
  await page.locator('.sq.landing').first().click();
  await page.waitForSelector('.question-card');
  await page.keyboard.press('1');
  await page.keyboard.press('Enter');
  await page.waitForSelector('.question-card', { state: 'detached' });
}
check((await page.locator('.shuffle-meter.flash').count()) === 1, 'facts shuffle after 5 turns');
await shot(page, 'after-shuffle');

await page.setViewportSize({ width: 1280, height: 900 });
await shot(page, 'desktop');

check(errors.length === 0, 'no console errors' + (errors.length ? ': ' + errors.join(' | ') : ''));
await browser.close();
process.exit(failed ? 1 : 0);
