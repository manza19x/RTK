// Usage:
//   node render.js <video> --still 1,5,10      -> PNG previews in out/stills
//   node render.js <video> [--workers 4] [--fps 30] [--from 0 --to 10]
const http = require('http'), fs = require('fs'), path = require('path'), { spawn, execFileSync } = require('child_process');
const { chromium } = require('playwright');

const ROOT = __dirname;
const args = process.argv.slice(2);
const name = args[0];
const opt = (k, d) => { const i = args.indexOf('--' + k); return i >= 0 ? args[i + 1] : d; };
const FPS = +opt('fps', 30), WORKERS = +opt('workers', 4);
const OUT = path.join(ROOT, 'out'); fs.mkdirSync(OUT, { recursive: true });

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.woff2': 'font/woff2', '.json': 'application/json' };
function serve() {
  return new Promise((res) => {
    const srv = http.createServer((req, rsp) => {
      const p = path.join(ROOT, decodeURIComponent(req.url.split('?')[0]));
      if (!p.startsWith(ROOT) || !fs.existsSync(p) || fs.statSync(p).isDirectory()) { rsp.writeHead(404); return rsp.end(); }
      rsp.writeHead(200, { 'Content-Type': MIME[path.extname(p)] || 'application/octet-stream' });
      fs.createReadStream(p).pipe(rsp);
    }).listen(0, () => res(srv));
  });
}

async function openPage(browser, port) {
  const page = await browser.newPage({ viewport: { width: 1080, height: 1920 }, deviceScaleFactor: 1 });
  page.on('console', (m) => { if (m.type() === 'error' || m.type() === 'warning') console.log('[page]', m.text()); });
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));
  await page.goto(`http://localhost:${port}/engine/index.html?v=${name}&${opt('q', '')}`);
  await page.waitForFunction(() => window.ready === true, null, { timeout: 120000 });
  return page;
}

(async () => {
  const srv = await serve(); const port = srv.address().port;
  const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const still = opt('still');
  if (still) {
    const page = await openPage(browser, port);
    fs.mkdirSync(path.join(OUT, 'stills'), { recursive: true });
    for (const s of still.split(',').map(Number)) {
      const t0 = Date.now();
      await page.evaluate((t) => window.renderAt(t), s);
      const t1 = Date.now();
      await page.evaluate(() => new Promise(requestAnimationFrame));
      console.log('render+raf', Date.now() - t1, 'evaluate', t1 - t0, await page.evaluate(() => window.lastTiming));
      const f = path.join(OUT, 'stills', `${name}_${String(s).replace('.', '_')}.jpg`);
      await page.screenshot({ path: f, type: 'jpeg', quality: 85 });
      console.log(f, Date.now() - t0, 'ms');
    }
    await browser.close(); srv.close(); return;
  }
  const page0 = await openPage(browser, port);
  const info = await page0.evaluate(() => ({ duration: window.specInfo.duration, title: window.specInfo.title }));
  const audio = await page0.evaluate(() => JSON.stringify(window.specInfo.audio || {}));
  fs.writeFileSync(path.join(OUT, `${name}.audio.json`), JSON.stringify({ duration: info.duration, ...JSON.parse(audio) }));
  await page0.close();

  const from = +opt('from', 0), to = +opt('to', info.duration);
  const f0 = Math.round(from * FPS), f1 = Math.round(to * FPS);
  const per = Math.ceil((f1 - f0) / WORKERS);
  const segDir = path.join(OUT, 'seg_' + name); fs.rmSync(segDir, { recursive: true, force: true }); fs.mkdirSync(segDir);
  const started = Date.now(); let done = 0;
  await Promise.all([...Array(WORKERS).keys()].map(async (w) => {
    const a = f0 + w * per, b = Math.min(f1, a + per);
    if (a >= b) return;
    const page = await openPage(browser, port);
    const seg = path.join(segDir, `s${w}.mp4`);
    const ff = spawn('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-framerate', String(FPS), '-c:v', 'mjpeg', '-i', '-',
      '-c:v', 'libx264', '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p', '-r', String(FPS), seg]);
    for (let f = a; f < b; f++) {
      await page.evaluate((t) => window.renderAt(t), f / FPS);
      const buf = await page.screenshot({ type: 'jpeg', quality: 93 });
      if (!ff.stdin.write(buf)) await new Promise((r) => ff.stdin.once('drain', r));
      done++;
      if (done % 60 === 0) console.log(`${name}: ${done}/${f1 - f0} frames, ${((Date.now() - started) / done).toFixed(0)} ms/frame`);
    }
    ff.stdin.end(); await new Promise((r) => ff.on('close', r));
    await page.close();
  }));
  await browser.close(); srv.close();
  const list = [...Array(WORKERS).keys()].map((w) => path.join(segDir, `s${w}.mp4`)).filter(fs.existsSync);
  fs.writeFileSync(path.join(segDir, 'list.txt'), list.map((f) => `file '${f}'`).join('\n'));
  const silent = path.join(OUT, `${name}.silent.mp4`);
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', path.join(segDir, 'list.txt'), '-c', 'copy', silent]);
  fs.rmSync(segDir, { recursive: true, force: true });
  console.log('video done', silent, ((Date.now() - started) / 1000).toFixed(0) + 's');
})().catch((e) => { console.error(e); process.exit(1); });
