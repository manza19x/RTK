// Landmarks III: Christ the Redeemer + lightning, Golden Gate, Great Wall, Las Vegas
import { THREE, rng } from './world.js';
import { shadowed } from './props2.js';
import { windowTexture, billboardTexture } from './props.js';

const std = (o) => new THREE.MeshStandardMaterial({ roughness: 0.85, ...o });

// ---------- Rio ----------
export function cristo() {
  const g = new THREE.Group();
  const stone = std({ color: 0xdedfd6, roughness: 0.75 }), base = std({ color: 0xb9b5a8 });
  const ped = new THREE.Mesh(new THREE.BoxGeometry(9, 8, 9).translate(0, 4, 0), base); g.add(ped);
  const cap = new THREE.Mesh(new THREE.BoxGeometry(10, 0.8, 10).translate(0, 8.2, 0), base); g.add(cap);
  const robe = new THREE.Mesh(new THREE.CylinderGeometry(2.5, 3.3, 19, 12).translate(0, 18, 0), stone); robe.scale.z = 0.72; g.add(robe);
  const folds = new THREE.Mesh(new THREE.BoxGeometry(1.2, 17, 0.6).translate(0, 17.5, 2.35), stone); g.add(folds);
  const chest = new THREE.Mesh(new THREE.BoxGeometry(5.4, 5.2, 3.0).translate(0, 29.4, 0), stone); g.add(chest);
  const arms = new THREE.Mesh(new THREE.BoxGeometry(28, 1.9, 1.9).translate(0, 31.0, 0), stone); g.add(arms);
  for (const s of [-1, 1]) {
    const sleeve = new THREE.Mesh(new THREE.BoxGeometry(5.5, 3.6, 1.5).translate(s * 5.2, 29.3, 0), stone); g.add(sleeve);
    const hand = new THREE.Mesh(new THREE.BoxGeometry(1.4, 1.6, 0.8).translate(s * 14.3, 30.8, 0), stone); g.add(hand);
  }
  const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.8, 0.9, 1.4).translate(0, 32.6, 0), stone); g.add(neck);
  const head = new THREE.Mesh(new THREE.SphereGeometry(1.55, 14, 10), stone); head.scale.set(1, 1.2, 1); head.position.set(0, 34.9, 0.1); g.add(head);
  const hair = new THREE.Mesh(new THREE.SphereGeometry(1.62, 14, 8, 0, Math.PI * 2, 0, Math.PI * 0.55), stone); hair.scale.set(1, 1.2, 1); hair.position.set(0, 35.2, -0.15); g.add(hair);
  const rodM = std({ color: 0x8a8f96, metalness: 0.7, roughness: 0.3 });
  for (let i = 0; i < 6; i++) { const a = i / 6 * Math.PI * 2; const rod = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 1.4), rodM); rod.position.set(Math.cos(a) * 0.9, 37.1, Math.sin(a) * 0.9); g.add(rod); }
  for (const sx of [-1, 1]) { const rod = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 1.2), rodM); rod.position.set(sx * 13.5, 32.5, 0); g.add(rod); }
  const beard = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1.2, 1.0).translate(0, 33.7, 0.7), stone); g.add(beard);
  // terrace with railing
  const terr = new THREE.Mesh(new THREE.CylinderGeometry(24, 26, 2, 24).translate(0, -1, 0), std({ color: 0xcfc6b4 })); g.add(terr);
  const rail = new THREE.Mesh(new THREE.TorusGeometry(23.5, 0.15, 4, 48).rotateX(Math.PI / 2), std({ color: 0xffffff })); rail.position.y = 1.1; g.add(rail);
  g.userData.top = new THREE.Vector3(0, 37, 0); g.userData.hand = new THREE.Vector3(14.3, 31.6, 0);
  return shadowed(g);
}
export function bolt(from, to, seed = 1, branches = 3) {
  const r = rng(seed), g = new THREE.Group();
  const core = new THREE.MeshBasicMaterial({ color: 0xf4f8ff, toneMapped: false, transparent: true, fog: false });
  const glow = new THREE.MeshBasicMaterial({ color: 0x9fb8ff, toneMapped: false, transparent: true, opacity: 0.35, blending: THREE.AdditiveBlending, depthWrite: false, fog: false });
  const make = (a, b, n, jag, rad) => {
    const pts = [];
    for (let i = 0; i <= n; i++) { const k = i / n; const p = a.clone().lerp(b, k); if (i > 0 && i < n) p.add(new THREE.Vector3((r() - 0.5) * jag, (r() - 0.5) * jag * 0.3, (r() - 0.5) * jag)); pts.push(p); }
    const curve = new THREE.CatmullRomCurve3(pts, false, 'catmullrom', 0.05);
    g.add(new THREE.Mesh(new THREE.TubeGeometry(curve, n * 3, rad, 5), core));
    g.add(new THREE.Mesh(new THREE.TubeGeometry(curve, n * 3, rad * 3.5, 6), glow));
    return pts;
  };
  const A = new THREE.Vector3(...from), B = new THREE.Vector3(...to);
  const main = make(A, B, 22, A.distanceTo(B) * 0.06, 0.22);
  for (let i = 0; i < branches; i++) {
    const s = main[3 + Math.floor(r() * 12)];
    make(s, s.clone().add(new THREE.Vector3((r() - 0.5) * 120, -40 - r() * 80, (r() - 0.5) * 120)), 8, 30, 0.12);
  }
  g.userData.mats = [core, glow];
  g.userData.set = (k) => { core.opacity = k; glow.opacity = 0.35 * k; g.visible = k > 0.01; };
  g.userData.set(0);
  return g;
}
export function rainbow(radius = 900) {
  const g = new THREE.Group();
  const C = [0xff3b30, 0xff9500, 0xffcc00, 0x34c759, 0x32ade6, 0x5856d6, 0xaf52de];
  const mats = C.map((c) => new THREE.MeshBasicMaterial({ color: c, transparent: true, opacity: 0, fog: false, depthWrite: false }));
  C.forEach((c, i) => g.add(new THREE.Mesh(new THREE.TorusGeometry(radius - i * 9, 4.5, 4, 80, Math.PI), mats[i])));
  g.userData.set = (k) => mats.forEach((m) => (m.opacity = 0.35 * k));
  return g;
}

// ---------- San Francisco ----------
const ORANGE = 0xc0362c;
export function goldenGate() {
  const g = new THREE.Group(), o = std({ color: ORANGE, roughness: 0.6 });
  const DECK_Y = 67, HALF = 1370, TX = 640, TOP = 227, W = 27;
  // towers
  for (const sx of [-1, 1]) {
    const tw = new THREE.Group(); tw.position.x = sx * TX;
    for (const sz of [-1, 1]) {
      [[0, 80, 11], [80, 160, 9.5], [160, TOP, 8]].forEach(([y0, y1, w]) => { const leg = new THREE.Mesh(new THREE.BoxGeometry(w, y1 - y0, w).translate(0, (y0 + y1) / 2, 0), o); leg.position.z = sz * (W / 2); tw.add(leg); });
    }
    [DECK_Y - 4, 120, 165, 205, TOP - 6].forEach((y, i) => { const p = new THREE.Mesh(new THREE.BoxGeometry(7, i === 4 ? 12 : 7, W), o); p.position.y = y; tw.add(p); });
    const pier = new THREE.Mesh(new THREE.BoxGeometry(30, 20, 60).translate(0, -8, 0), std({ color: 0x8c8a85 })); tw.add(pier);
    g.add(tw);
  }
  // deck in segments (so it can sway)
  const segs = [];
  const N = 60, L = (2 * HALF) / N;
  for (let i = 0; i < N; i++) {
    const x = -HALF + L * (i + 0.5);
    const d = new THREE.Mesh(new THREE.BoxGeometry(L + 0.3, 7, W), o); d.position.set(x, DECK_Y, 0); g.add(d); segs.push(d);
    const road = new THREE.Mesh(new THREE.BoxGeometry(L + 0.3, 0.3, W - 4), std({ color: 0x3d3f44 })); road.position.set(0, 3.6, 0); d.add(road);
  }
  // approach piers
  for (let x = -HALF + 40; x < HALF; x += 80) if (Math.abs(Math.abs(x) - TX) > 60 && Math.abs(x) > 990) { const p = new THREE.Mesh(new THREE.BoxGeometry(8, DECK_Y, 8).translate(0, DECK_Y / 2 - 3, 0), std({ color: ORANGE })); p.position.x = x; g.add(p); }
  // main cables + suspenders
  const cableY = (x) => {
    const ax = Math.abs(x);
    if (ax <= TX) return DECK_Y + 6 + (TOP - 6 - DECK_Y - 6) * Math.pow(ax / TX, 2);
    const k = (ax - TX) / (983 - TX); return TOP - 6 - (TOP - 6 - (DECK_Y + 10)) * (1 - Math.pow(1 - k, 2)) * 1.0;
  };
  for (const sz of [-1, 1]) {
    const pts = []; for (let x = -983; x <= 983; x += 20) pts.push(new THREE.Vector3(x, cableY(x), sz * W / 2));
    g.add(new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts), 200, 0.9, 6), o));
  }
  const susp = new THREE.InstancedMesh(new THREE.BoxGeometry(0.25, 1, 0.25), o, 260);
  let k = 0; const M = new THREE.Matrix4();
  for (let x = -975; x <= 975 && k < 260; x += 15) for (const sz of [-1, 1]) {
    if (Math.abs(Math.abs(x) - TX) < 8) continue;
    const h = cableY(x) - DECK_Y; if (h < 1) continue;
    M.compose(new THREE.Vector3(x, DECK_Y + h / 2, sz * W / 2), new THREE.Quaternion(), new THREE.Vector3(1, h, 1)); susp.setMatrixAt(k++, M);
  }
  susp.count = k; g.add(susp);
  // anchorages
  for (const sx of [-1, 1]) { const a = new THREE.Mesh(new THREE.BoxGeometry(40, 60, 50).translate(0, 30, 0), std({ color: 0x9a968d })); a.position.x = sx * 990; g.add(a); }
  g.userData = { segs, TX, DECK_Y };
  g.userData.sway = (amp, t) => {
    for (const d of segs) {
      const ax = Math.abs(d.position.x);
      const f = ax < TX ? Math.cos((d.position.x / TX) * Math.PI / 2) : Math.max(0, Math.sin(((ax - TX) / 350) * Math.PI)) * 0.3;
      d.position.z = amp * f * Math.sin(t * 2.2);
      d.position.y = DECK_Y + amp * 0.35 * f * Math.sin(t * 3.1 + 1);
    }
  };
  return shadowed(g);
}
export function island(w, h, seed, rock = 0x8a7f6b, top = 0x6e8f4e) {
  const r = rng(seed), g = new THREE.Group();
  const m = new THREE.Mesh(new THREE.SphereGeometry(1, 12, 8), std({ color: rock, flatShading: true }));
  m.scale.set(w, h, w * 0.7); g.add(m);
  const t = new THREE.Mesh(new THREE.SphereGeometry(1, 12, 6, 0, Math.PI * 2, 0, Math.PI * 0.35), std({ color: top, flatShading: true }));
  t.scale.set(w * 0.95, h * 1.02, w * 0.68); g.add(t);
  return shadowed(g);
}

// ---------- Great Wall ----------
export function greatWall(pathFn, x0, x1, step = 3.2, towerEvery = 40, ruin = () => 1) {
  const g = new THREE.Group();
  const stone = std({ color: 0x9c8d77, flatShading: true }), dark = std({ color: 0x7d705e, flatShading: true });
  const n = Math.floor((x1 - x0) / step);
  const segs = new THREE.InstancedMesh(new THREE.BoxGeometry(step + 0.35, 8, 6.5), stone, n);
  const cren = new THREE.InstancedMesh(new THREE.BoxGeometry(1.1, 1.3, 0.6), dark, n * 2);
  const M = new THREE.Matrix4(), Q = new THREE.Quaternion(), V = new THREE.Vector3(), S = new THREE.Vector3(1, 1, 1), E = new THREE.Euler();
  const towers = [];
  for (let i = 0; i < n; i++) {
    const x = x0 + i * step, [y, z, yaw] = pathFn(x), [y2] = pathFn(x + step);
    const pitch = Math.atan2(y2 - y, step);
    E.set(0, -yaw, pitch, 'YXZ'); Q.setFromEuler(E);
    const rf = ruin(x);
    V.set(x + step / 2, (y + y2) / 2 + 2.6 - (1 - rf) * 4, z); M.compose(V, Q, S.set(1, rf, 1)); segs.setMatrixAt(i, M); S.set(1, 1, 1);
    for (const s of [-1, 1]) {
      const off = new THREE.Vector3(0, 4.6 - (1 - rf) * 8, s * 3.0).applyQuaternion(Q);
      M.compose(V.clone().add(off), Q, rf > 0.92 ? S : S.clone().setScalar(0)); cren.setMatrixAt(i * 2 + (s > 0 ? 1 : 0), M);
    }
    if (i % towerEvery === 0 && rf > 0.9) towers.push([x, y, z, yaw]);
  }
  segs.castShadow = segs.receiveShadow = true; cren.castShadow = true; g.add(segs, cren);
  for (const [x, y, z, yaw] of towers) {
    const t = new THREE.Group();
    t.add(new THREE.Mesh(new THREE.BoxGeometry(11, 13, 11).translate(0, 4.5, 0), stone));
    const top = new THREE.Mesh(new THREE.BoxGeometry(12, 1, 12).translate(0, 11.5, 0), dark); t.add(top);
    for (const [a, b] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) { const win = new THREE.Mesh(new THREE.BoxGeometry(a ? 0.3 : 2, 2.4, b ? 0.3 : 2), std({ color: 0x2a241d })); win.position.set(a * 5.55, 5, b * 5.55); t.add(win); }
    const roof = new THREE.Mesh(new THREE.ConeGeometry(6.5, 3.5, 4).rotateY(Math.PI / 4).translate(0, 14, 0), std({ color: 0x5a3b2a, flatShading: true }));
    if (rng(Math.floor(x))() > 0.5) t.add(roof);
    t.position.set(x, y, z); t.rotation.y = -yaw; g.add(shadowed(t));
  }
  return g;
}

// ---------- Las Vegas ----------
export function welcomeSign() {
  const cv = document.createElement('canvas'); cv.width = 512; cv.height = 640; const c = cv.getContext('2d');
  c.fillStyle = '#f4f1e6'; c.beginPath(); c.moveTo(256, 10); c.lineTo(502, 320); c.lineTo(256, 630); c.lineTo(10, 320); c.closePath(); c.fill();
  c.fillStyle = '#d6202a';
  for (let i = 0; i < 40; i++) { const a = i / 40; const pts = [[256, 10], [502, 320], [256, 630], [10, 320], [256, 10]]; const seg = Math.floor(a * 4), f = a * 4 - seg; const [x0, y0] = pts[seg], [x1, y1] = pts[seg + 1]; c.beginPath(); c.arc(x0 + (x1 - x0) * f, y0 + (y1 - y0) * f, 7, 0, 7); c.fillStyle = i % 2 ? '#ffd23f' : '#ffffff'; c.fill(); }
  c.textAlign = 'center'; c.fillStyle = '#1f3c88'; c.font = '700 44px Inter'; c.fillText('BIENVENIDO A', 256, 230);
  c.fillStyle = '#d6202a'; c.font = '900 86px Serif4'; c.fillText('LAS', 256, 330); c.fillText('VEGAS', 256, 420);
  c.fillStyle = '#1f3c88'; c.font = '700 40px Inter'; c.fillText('NEVADA', 256, 500);
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace;
  const g = new THREE.Group();
  const board = new THREE.Mesh(new THREE.PlaneGeometry(8, 10), new THREE.MeshBasicMaterial({ map: t, transparent: true, alphaTest: 0.5, side: THREE.DoubleSide, toneMapped: false }));
  board.position.y = 9; g.add(board);
  for (const s of [-1, 1]) { const p = new THREE.Mesh(new THREE.BoxGeometry(0.4, 6, 0.4).translate(s * 1.5, 3, -0.1), std({ color: 0xdddddd })); g.add(p); }
  return g;
}
const NEON = ['CASINO', 'HOTEL', '777', 'BUFFET', 'SHOW', 'JACKPOT', 'SLOTS', 'BINGO', 'ROYAL', 'LUCKY', 'POKER', 'DINER'];
export function neonTexture(seed) {
  const r = rng(seed), cv = document.createElement('canvas'); cv.width = 512; cv.height = 256; const g = cv.getContext('2d');
  g.fillStyle = '#0b0712'; g.fillRect(0, 0, 512, 256);
  const cols = ['#ff2bd6', '#00e5ff', '#ffe600', '#ff3b30', '#39ff14', '#ff9500'];
  const c1 = cols[Math.floor(r() * cols.length)], c2 = cols[Math.floor(r() * cols.length)];
  g.strokeStyle = c2; g.lineWidth = 10; g.shadowColor = c2; g.shadowBlur = 25; g.strokeRect(14, 14, 484, 228);
  g.fillStyle = c1; g.shadowColor = c1; g.shadowBlur = 30; g.font = '900 110px Inter'; g.textAlign = 'center'; g.textBaseline = 'middle';
  g.fillText(NEON[Math.floor(r() * NEON.length)], 256, 132);
  for (let i = 0; i < 26; i++) { g.beginPath(); g.arc(30 + i * 18, 236, 4, 0, 7); g.fillStyle = '#fff6c8'; g.fill(); }
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}
export function hotelTower(w, h, d, seed, tint = 0x2b2f3a) {
  const tex = windowTexture(seed, 10, Math.round(h / 3.2), 0.7);
  tex.wrapS = tex.wrapT = THREE.RepeatWrapping; tex.repeat.set(w / 20, 1);
  const m = std({ color: tint, emissive: 0xffffff, emissiveMap: tex, emissiveIntensity: 1.2, roughness: 0.4, metalness: 0.3 });
  const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, d).translate(0, h / 2, 0), m); b.castShadow = b.receiveShadow = true;
  return b;
}
export function billTexture() {
  const cv = document.createElement('canvas'); cv.width = 256; cv.height = 108; const g = cv.getContext('2d');
  g.fillStyle = '#cfe3c4'; g.fillRect(0, 0, 256, 108);
  g.strokeStyle = '#3f6b45'; g.lineWidth = 6; g.strokeRect(6, 6, 244, 96);
  g.fillStyle = '#3f6b45'; g.font = '900 34px Inter'; g.fillText('100', 16, 44); g.fillText('100', 180, 96);
  g.beginPath(); g.ellipse(128, 54, 30, 36, 0, 0, 7); g.fillStyle = '#9fbf95'; g.fill();
  g.fillStyle = '#3f6b45'; g.font = '700 14px Inter'; g.fillText('$ $ $ $ $ $', 96, 98);
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}
export function moneyRain(w, n = 500, seed = 4) {
  const r = rng(seed), tex = billTexture();
  const m = new THREE.InstancedMesh(new THREE.PlaneGeometry(1.56, 0.66), new THREE.MeshStandardMaterial({ map: tex, side: THREE.DoubleSide, roughness: 0.8 }), n);
  m.frustumCulled = false; w.scene.add(m);
  const P = [...Array(n)].map(() => ({ x: (r() - 0.5) * 80, z: (r() - 0.5) * 80, y0: 20 + r() * 60, sp: 2 + r() * 2.5, ph: r() * 6.28, sw: 0.5 + r() * 1.5 }));
  const M = new THREE.Matrix4(), V = new THREE.Vector3(), Q = new THREE.Quaternion(), E = new THREE.Euler(), S = new THREE.Vector3();
  return {
    mesh: m,
    update(t, cx, cz, k) {
      for (let i = 0; i < n; i++) {
        const p = P[i];
        const y = ((p.y0 - t * p.sp) % 80 + 80) % 80;
        V.set(cx + p.x + Math.sin(t * p.sw + p.ph) * 2, y, cz + p.z + Math.cos(t * p.sw * 0.8 + p.ph) * 2);
        E.set(t * 2 * p.sw + p.ph, t * 1.3 + p.ph, Math.sin(t * 3 + p.ph)); Q.setFromEuler(E); S.setScalar(k);
        M.compose(V, Q, S); m.setMatrixAt(i, M);
      }
      m.instanceMatrix.needsUpdate = true;
    },
  };
}
export { billboardTexture };
