import { clamp, rng, smooth } from './world.js';

// Spanish number formatting: 8.200.000.000 ; decimals with comma
export function fmt(n, dec = 0) {
  const neg = n < 0; n = Math.abs(n);
  let [i, d] = n.toFixed(dec).split('.');
  i = i.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return (neg ? '-' : '') + i + (d ? ',' + d : '');
}
// count-up value generator: returns fn(dt)->string
export function count(from, to, { dur = 1.2, dec = 0, pre = '', suf = '', curve = 'out' } = {}) {
  return (dt) => {
    let k = clamp(dt / dur);
    k = curve === 'out' ? 1 - Math.pow(1 - k, 3) : curve === 'lin' ? k : k * k * (3 - 2 * k);
    return pre + fmt(from + (to - from) * k, dec) + suf;
  };
}
// countdown mm:ss from seconds
export function clock(fromSec, toSec, dur, { lin = true } = {}) {
  return (dt) => {
    const k = clamp(dt / dur);
    const s = Math.max(0, Math.ceil(fromSec + (toSec - fromSec) * k - 1e-6));
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  };
}

export class HUD {
  constructor(spec) {
    this.spec = spec;
    this.el = (id) => document.getElementById(id);
    this.fx = this.el('fx').getContext('2d');
    this.stats = [...(spec.stats || [])].sort((a, b) => a.t - b.t);
    this.caps = spec.captions || [];
    this.lastKey = null;
  }
  render(t) {
    const S = this.spec;
    // ----- stat block
    let cur = null, next = null;
    for (let i = 0; i < this.stats.length; i++) if (this.stats[i].t <= t) { cur = this.stats[i]; next = this.stats[i + 1]; }
    const stat = this.el('stat');
    const endT = S.end ? S.end.t : 1e9;
    if (cur && t < endT && !cur.hide) {
      const dt = t - cur.t;
      const fadeIn = clamp(dt / 0.18);
      stat.style.opacity = fadeIn;
      stat.style.transform = `translateY(${(1 - fadeIn) * 10}px)`;
      this.el('slabel').textContent = cur.label || '';
      const val = typeof cur.value === 'function' ? cur.value(dt) : (cur.value ?? '');
      this.el('svalue').textContent = val;
      this.el('ssub').textContent = typeof cur.sub === 'function' ? cur.sub(dt) : (cur.sub || '');
      const pulse = cur.pulse ? 1 + 0.06 * Math.exp(-dt * 6) : 1;
      this.el('svalue').style.transform = `scale(${pulse})`;
      this.el('svalue').style.color = cur.color || '#fff';
    } else stat.style.opacity = 0;

    // ----- opening title
    const ti = this.el('title');
    if (S.titleCard) {
      const { t0 = 0, t1 = 3.2 } = S.titleCard;
      const o = clamp((t - t0) / 0.2) * clamp((t1 - t) / 0.35);
      ti.style.opacity = o; ti.textContent = S.title;
    } else ti.style.opacity = 0;

    // ----- captions
    const cap = this.el('caption');
    let c = null;
    for (const k of this.caps) if (t >= k.t && t < k.t + k.d) c = k;
    if (c && t < endT) {
      const a = clamp((t - c.t) / 0.22) * clamp((c.t + c.d - t) / 0.22);
      cap.style.opacity = a;
      cap.style.transform = `translateY(${(1 - clamp((t - c.t) / 0.3)) * 14}px)`;
      cap.innerHTML = c.text.replace(/\n/g, '<br>');
      cap.style.top = (c.y || 1240) + 'px';
    } else cap.style.opacity = 0;

    // ----- endcard
    const end = this.el('end');
    if (S.end && t >= S.end.t) {
      const dt = t - S.end.t;
      end.style.opacity = clamp(dt / 0.45);
      this.el('etitle').textContent = S.title;
      this.el('efact').textContent = S.end.fact;
      const btn = this.el('ebtn');
      const swap = dt > 1.9;
      const pop = swap ? 1 + 0.25 * Math.exp(-(dt - 1.9) * 7) * Math.sin((dt - 1.9) * 14 + 1.57) : 1;
      btn.className = swap ? 'done' : '';
      btn.style.transform = `scale(${pop})`;
      this.el('etext').style.opacity = clamp((dt - 0.35) / 0.4);
    } else end.style.opacity = 0;

    // ----- fx canvas
    this.drawFx(t);
  }
  drawTunnel(T, t) {
    const g = this.fx, W = 1080, H = 1920, cx = W / 2, cy = H * 0.5;
    g.save(); g.globalAlpha = T.a;
    const [r, gg, b] = T.col;
    g.fillStyle = `rgb(${r * 0.15 | 0},${gg * 0.15 | 0},${b * 0.15 | 0})`; g.fillRect(0, 0, W, H);
    const N = 30;
    // far to near filled discs -> banded tunnel walls
    const items = [];
    for (let i = 0; i < N; i++) {
      const z = (((i - T.phase) % N) + N) % N + 0.35;
      items.push([z, i]);
    }
    items.sort((a, b) => b[0] - a[0]);
    for (const [z, i] of items) {
      const rad = 1500 / z;
      const sh = clamp(1.3 - z / 30) * (i % 2 ? 1 : 0.5) * (0.85 + 0.15 * Math.sin(i * 2.3));
      const wob = Math.sin(i * 1.7 + t * 0.5) * 30 / z;
      g.fillStyle = `rgb(${r * sh | 0},${gg * sh | 0},${b * sh | 0})`;
      g.beginPath(); g.arc(cx + wob, cy + wob * 0.6, rad, 0, Math.PI * 2); g.fill();
      // cracks / texture marks
      if (z < 18) {
        g.strokeStyle = `rgba(0,0,0,${0.18 * clamp(1 - z / 18)})`; g.lineWidth = 2;
        for (let k = 0; k < 6; k++) { const a = i * 1.3 + k; g.beginPath(); g.arc(cx + wob, cy + wob * 0.6, rad * 0.97, a, a + 0.25); g.stroke(); }
      }
    }
    // centre (far end) glow / darkness
    const [gr, ggr, gb] = T.glow || [0, 0, 0];
    const rg = g.createRadialGradient(cx, cy, 0, cx, cy, 380);
    rg.addColorStop(0, `rgba(${gr},${ggr},${gb},${T.glowA ?? 0.9})`); rg.addColorStop(1, `rgba(${gr},${ggr},${gb},0)`);
    g.fillStyle = rg; g.fillRect(0, 0, W, H);
    // the faller seen from above
    if (T.person) {
      g.save(); g.translate(cx + Math.sin(t * 0.9) * 18, cy - 140 + Math.sin(t * 1.3) * 10); g.rotate(t * 0.35);
      const s = 1.7;
      g.fillStyle = '#1d2a4a'; g.fillRect(-26 * s, 10 * s, 18 * s, 60 * s); g.fillRect(8 * s, 10 * s, 18 * s, 60 * s);   // legs
      g.save(); g.rotate(-0.5); g.fillStyle = '#e63b3b'; g.fillRect(-75 * s, -30 * s, 50 * s, 15 * s); g.restore(); // arm
      g.save(); g.rotate(0.5); g.fillStyle = '#e63b3b'; g.fillRect(25 * s, -30 * s, 50 * s, 15 * s); g.restore();
      g.fillStyle = '#e63b3b'; g.fillRect(-30 * s, -38 * s, 60 * s, 52 * s);   // torso
      g.fillStyle = '#2a1d14'; g.beginPath(); g.arc(0, -54 * s, 21 * s, 0, 7); g.fill();  // head (hair)
      g.restore();
    }
    // speed lines
    if (T.speed > 0.05) {
      const rr = rng(61); g.strokeStyle = `rgba(255,240,220,${0.25 * T.speed})`;
      for (let i = 0; i < 60; i++) {
        const a = rr() * Math.PI * 2, ph = (rr() + t * (1.5 + rr()) * T.speed * 2) % 1;
        const r0 = 200 + ph * 1200, r1 = r0 + 80 + ph * 300;
        g.lineWidth = 1 + ph * 4;
        g.beginPath(); g.moveTo(cx + Math.cos(a) * r0, cy + Math.sin(a) * r0); g.lineTo(cx + Math.cos(a) * r1, cy + Math.sin(a) * r1); g.stroke();
      }
    }
    g.restore();
  }
  drawFx(t) {
    const g = this.fx, W = 1080, H = 1920, S = this.spec;
    g.clearRect(0, 0, W, H);
    const fx = S.fx ? S.fx(t) : {};
    // underwater
    if (fx.underwater) {
      const a = fx.underwater;
      const gr = g.createLinearGradient(0, 0, 0, H);
      gr.addColorStop(0, `rgba(40,110,120,${0.75 * a})`); gr.addColorStop(1, `rgba(6,30,40,${0.95 * a})`);
      g.fillStyle = gr; g.fillRect(0, 0, W, H);
      // light shafts
      g.save(); g.globalCompositeOperation = 'lighter';
      for (let i = 0; i < 7; i++) {
        const x = 150 + i * 140 + Math.sin(t * 0.6 + i) * 40;
        const lg = g.createLinearGradient(0, 0, 0, H * 0.8);
        lg.addColorStop(0, `rgba(160,230,230,${0.12 * a})`); lg.addColorStop(1, 'rgba(160,230,230,0)');
        g.fillStyle = lg; g.beginPath(); g.moveTo(x - 30, 0); g.lineTo(x + 30, 0); g.lineTo(x + 140, H * 0.8); g.lineTo(x - 10, H * 0.8); g.fill();
      }
      g.restore();
      const r = rng(11);
      g.strokeStyle = `rgba(220,255,255,${0.55 * a})`; g.lineWidth = 2.2;
      for (let i = 0; i < 140; i++) {
        const x0 = r() * W, sp = 90 + r() * 260, rad = 3 + r() * 11, ph = r() * H;
        const y = H - ((t * sp + ph) % (H + 100));
        const x = x0 + Math.sin(t * 2 + i) * 12;
        g.beginPath(); g.arc(x, y, rad, 0, Math.PI * 2); g.stroke();
      }
    }
    if (fx.tunnel) this.drawTunnel(fx.tunnel, t);
    if (fx.tint) { g.fillStyle = fx.tint; g.fillRect(0, 0, W, H); }
    // snow
    if (fx.snow) {
      const r = rng(21);
      g.fillStyle = `rgba(255,255,255,${0.85 * fx.snow})`;
      for (let i = 0; i < 420; i++) {
        const x0 = r() * W, sp = 60 + r() * 140, sz = 1.5 + r() * 4.5, ph = r() * H, drift = r() * 60;
        const y = (t * sp + ph) % H;
        const x = (x0 + Math.sin(t * 0.8 + i) * drift + t * (fx.snowWind || 0) * sp) % W;
        g.beginPath(); g.arc((x + W) % W, y, sz, 0, Math.PI * 2); g.fill();
      }
    }
    // wind streaks
    if (fx.wind) {
      const r = rng(41);
      g.strokeStyle = `rgba(255,255,255,${0.45 * fx.wind})`;
      for (let i = 0; i < 90; i++) {
        const y = r() * H, len = 120 + r() * 380, sp = 2500 + r() * 3500, ph = r() * 3000;
        const x = ((t * sp + ph) % (W + 800)) - 400;
        g.lineWidth = 1 + r() * 3;
        g.beginPath(); g.moveTo(x, y); g.lineTo(x + len, y + len * 0.04); g.stroke();
      }
    }
    // dust
    if (fx.dust) {
      const r = rng(51);
      for (let i = 0; i < 70; i++) {
        const x = r() * W, y = H * (0.55 + r() * 0.45), rad = 60 + r() * 200;
        const gg = g.createRadialGradient(x, y, 0, x, y, rad);
        gg.addColorStop(0, `rgba(205,185,150,${0.35 * fx.dust})`); gg.addColorStop(1, 'rgba(205,185,150,0)');
        g.fillStyle = gg; g.fillRect(x - rad, y - rad, rad * 2, rad * 2);
      }
    }
    if (fx.frost) {
      const a = fx.frost;
      const gr = g.createRadialGradient(W / 2, H / 2, H * 0.25, W / 2, H / 2, H * 0.7);
      gr.addColorStop(0, 'rgba(220,240,255,0)'); gr.addColorStop(1, `rgba(220,240,255,${0.75 * a})`);
      g.fillStyle = gr; g.fillRect(0, 0, W, H);
    }
    if (fx.dark) { g.fillStyle = `rgba(0,0,0,${fx.dark})`; g.fillRect(0, 0, W, H); }
    if (fx.flash) { g.fillStyle = `rgba(255,255,255,${fx.flash})`; g.fillRect(0, 0, W, H); }
    // vignette (always)
    const vg = g.createRadialGradient(W / 2, H * 0.48, H * 0.3, W / 2, H * 0.5, H * 0.75);
    vg.addColorStop(0, 'rgba(0,0,0,0)'); vg.addColorStop(1, `rgba(0,0,0,${0.32 + (fx.vignette || 0)})`);
    g.fillStyle = vg; g.fillRect(0, 0, W, H);
    // readability gradient at top for stat
    const tg = g.createLinearGradient(0, 0, 0, 620);
    tg.addColorStop(0, 'rgba(0,0,0,0.22)'); tg.addColorStop(0.45, 'rgba(0,0,0,0.12)'); tg.addColorStop(1, 'rgba(0,0,0,0)');
    g.fillStyle = tg; g.fillRect(0, 0, W, 620);
  }
}
