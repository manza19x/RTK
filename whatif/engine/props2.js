// More landmark props: Colosseum, T-Rex, Giza, Everest, Eiffel Tower, Paris
import { THREE, rng } from './world.js';

const std = (o) => new THREE.MeshStandardMaterial({ roughness: 0.9, ...o });
export function shadowed(m) { m.traverse((o) => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } }); return m; }
const tex = (cv) => { const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; return t; };

// ring between two ellipses (a = x semi-axis, b = z semi-axis) at two heights
export function ellipseRing(a1, b1, y1, a2, b2, y2, seg = 96, uRep = 1, th0 = 0, thL = Math.PI * 2) {
  const pos = [], uv = [], idx = [];
  for (let i = 0; i <= seg; i++) {
    const th = th0 + thL * i / seg, c = Math.cos(th), s = Math.sin(th);
    pos.push(a1 * c, y1, b1 * s, a2 * c, y2, b2 * s);
    uv.push(i / seg * uRep, 0, i / seg * uRep, 1);
    if (i < seg) { const k = i * 2; idx.push(k, k + 1, k + 2, k + 1, k + 3, k + 2); }
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  g.setIndex(idx); g.computeVertexNormals(); return g;
}

// ---------- Colosseum ----------
function arcadeCanvas() {
  const cv = document.createElement('canvas'); cv.width = 256; cv.height = 512;
  const g = cv.getContext('2d');
  g.fillStyle = '#d6c29b'; g.fillRect(0, 0, 256, 512);
  const r = rng(12);
  for (let i = 0; i < 400; i++) { g.fillStyle = `rgba(${120 + r() * 60},${100 + r() * 40},${70 + r() * 30},${0.08 + r() * 0.1})`; g.fillRect(r() * 256, r() * 512, 2 + r() * 20, 2 + r() * 8); }
  // three arcade tiers (bottom = high v -> canvas bottom) + attic
  const tierH = 120;
  for (let k = 0; k < 3; k++) {
    const y0 = 512 - (k + 1) * tierH;
    g.fillStyle = '#b8a27a'; g.fillRect(0, y0, 256, 10);               // cornice
    g.fillStyle = '#c9b38a'; g.fillRect(18, y0 + 10, 22, tierH - 10); g.fillRect(256 - 40, y0 + 10, 22, tierH - 10); // pilasters
    g.fillStyle = '#2c241b';
    const ax = 64, aw = 128, ay = y0 + 34, ah = tierH - 34;
    g.beginPath(); g.moveTo(ax, y0 + tierH); g.lineTo(ax, ay + aw / 2); g.arc(ax + aw / 2, ay + aw / 2, aw / 2, Math.PI, 0); g.lineTo(ax + aw, y0 + tierH); g.closePath(); g.fill();
  }
  g.fillStyle = '#b8a27a'; g.fillRect(0, 512 - 3 * tierH - 12, 256, 12);
  g.fillStyle = '#2c241b'; g.fillRect(100, 40, 56, 44);                  // attic window
  return cv;
}
export function colosseum(w) {
  const grp = new THREE.Group();
  const cv = arcadeCanvas();
  const A = 94, B = 78, H = 48;
  // full-height outer wall (about 60% of the ring) + lower ruined section
  const t1 = tex(cv);
  const full = new THREE.Mesh(ellipseRing(A, B, 0, A, B, H, 120, 1, -0.4, Math.PI * 1.25), std({ map: t1, side: THREE.DoubleSide }));
  full.geometry.attributes.uv.array.forEach((v, i, arr) => { if (i % 2 === 0) arr[i] = v * 48; });
  grp.add(full);
  const t2 = tex(cv); t2.repeat.set(1, 0.5); t2.offset.set(0, 0);
  const lowG = ellipseRing(A, B, 0, A, B, H * 0.5, 80, 1, -0.4 + Math.PI * 1.25, Math.PI * 0.75);
  lowG.attributes.uv.array.forEach((v, i, arr) => { if (i % 2 === 0) arr[i] = v * 29; });
  const low = new THREE.Mesh(lowG, std({ map: t2, side: THREE.DoubleSide })); grp.add(low);
  // ruined inner ring wall where outer is missing (jagged)
  const inner = new THREE.Mesh(ellipseRing(A - 6, B - 6, 0, A - 6, B - 6, H * 0.72, 80, 1, -0.4 + Math.PI * 1.25, Math.PI * 0.75), std({ color: 0xc9ad85, side: THREE.DoubleSide }));
  grp.add(inner);
  // cavea (seating) — stepped tiers with stripe texture
  const sc = document.createElement('canvas'); sc.width = 8; sc.height = 64; const sg = sc.getContext('2d');
  for (let i = 0; i < 8; i++) { sg.fillStyle = i % 2 ? '#c8ad84' : '#a88d66'; sg.fillRect(0, i * 8, 8, 8); }
  const st = tex(sc); st.repeat.set(1, 3);
  const stone = std({ map: st, side: THREE.DoubleSide });
  const wall = std({ color: 0xb79a72, side: THREE.DoubleSide });
  const a0 = 41.5, b0 = 24;
  const tiers = [
    [a0, b0, 0, a0, b0, 4, wall], [a0, b0, 4, a0 + 2, b0 + 2, 4, wall],
    [a0 + 2, b0 + 2, 4, 64, 48, 19, stone], [64, 48, 19, 64, 48, 23, wall],
    [64, 48, 23, 80, 64, 34, stone], [80, 64, 34, 80, 64, 37, wall], [80, 64, 37, 90, 74, 44, stone],
  ];
  for (const [a1, b1, y1, a2, b2, y2, m] of tiers) { const mm = new THREE.Mesh(ellipseRing(a1, b1, y1, a2, b2, y2, 96), m); mm.receiveShadow = true; mm.castShadow = true; grp.add(mm); }
  // arena floor
  const floor = new THREE.Mesh(new THREE.CircleGeometry(1, 64).rotateX(-Math.PI / 2), std({ color: 0xd9bf8c }));
  floor.scale.set(a0, 1, b0); floor.position.y = 0.04; floor.receiveShadow = true; grp.add(floor);
  // gate
  const gate = new THREE.Mesh(new THREE.BoxGeometry(0.5, 6, 7), std({ color: 0x1d1712 })); gate.position.set(-a0 + 0.1, 3, 0); grp.add(gate);
  shadowed(grp); w.scene.add(grp);
  grp.userData = { a0, b0, tiers };
  return grp;
}
// sample a point on a cavea tier: s in [0,1] across, th angle
export function tierPoint(tier, s, th) {
  const [a1, b1, y1, a2, b2, y2] = tier;
  const a = a1 + (a2 - a1) * s, b = b1 + (b2 - b1) * s;
  return [a * Math.cos(th), y1 + (y2 - y1) * s, b * Math.sin(th)];
}
export function stonePine() {
  const g = new THREE.Group();
  const tr = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.5, 9, 6).translate(0, 4.5, 0), std({ color: 0x6b4f37 })); g.add(tr);
  const cm = std({ color: 0x3f6b35, flatShading: true });
  for (let i = 0; i < 4; i++) { const c = new THREE.Mesh(new THREE.SphereGeometry(1, 8, 5), cm); c.scale.set(4 + i % 2, 1.6, 4); c.position.set((i - 1.5) * 1.8, 9.5 + (i % 2) * 0.6, (i % 2) * 1.5 - 0.7); g.add(c); }
  return shadowed(g);
}

// ---------- T-Rex ----------
export function trex() {
  const g = new THREE.Group();
  const skin = std({ color: 0x5f6e3c, flatShading: true }), belly = std({ color: 0xc4b585, flatShading: true }), dark = std({ color: 0x3d4626, flatShading: true });
  const body = new THREE.Mesh(new THREE.SphereGeometry(1, 12, 9), skin); body.scale.set(1.6, 1.7, 3.4); body.position.set(0, 4.4, 0.2); g.add(body);
  const bel = new THREE.Mesh(new THREE.SphereGeometry(1, 12, 9), belly); bel.scale.set(1.35, 1.2, 2.8); bel.position.set(0, 3.8, -0.2); g.add(bel);
  const tail = new THREE.Mesh(new THREE.ConeGeometry(1.25, 7.5, 10).rotateX(Math.PI / 2), skin); tail.position.set(0, 4.5, 6.6); tail.rotation.x = 0.1; g.add(tail);
  const neck = new THREE.Mesh(new THREE.BoxGeometry(1.3, 1.6, 2.0), skin); neck.position.set(0, 5.2, -3.0); neck.rotation.x = -0.45; g.add(neck);
  // head (pivot at neck end)
  const head = new THREE.Group(); head.position.set(0, 5.7, -3.8); g.add(head);
  const skull = new THREE.Mesh(new THREE.BoxGeometry(1.4, 1.25, 2.6).translate(0, 0.1, -1.1), skin); head.add(skull);
  const snout = new THREE.Mesh(new THREE.BoxGeometry(1.05, 0.8, 0.8).translate(0, -0.05, -2.6), skin); head.add(snout);
  const brow = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.25, 0.7).translate(0, 0.72, -0.9), dark); head.add(brow);
  for (const s of [-1, 1]) { const e = new THREE.Mesh(new THREE.SphereGeometry(0.13, 8, 6), new THREE.MeshBasicMaterial({ color: 0xffc23d })); e.position.set(s * 0.71, 0.45, -0.85); head.add(e); const p = new THREE.Mesh(new THREE.SphereGeometry(0.07, 6, 4), new THREE.MeshBasicMaterial({ color: 0x000000 })); p.position.set(s * 0.78, 0.45, -0.88); head.add(p); }
  const jaw = new THREE.Group(); jaw.position.set(0, -0.45, -0.1); head.add(jaw);
  jaw.add(new THREE.Mesh(new THREE.BoxGeometry(1.1, 0.4, 2.7).translate(0, -0.1, -1.3), belly));
  const toothM = new THREE.MeshStandardMaterial({ color: 0xf4efe0 });
  for (let i = 0; i < 7; i++) for (const s of [-1, 1]) {
    const t = new THREE.Mesh(new THREE.ConeGeometry(0.07, 0.3, 4).rotateX(Math.PI), toothM); t.position.set(s * 0.45, -0.38, -0.5 - i * 0.32); head.add(t);
    const t2 = new THREE.Mesh(new THREE.ConeGeometry(0.06, 0.25, 4), toothM); t2.position.set(s * 0.42, 0.18, -0.5 - i * 0.32); jaw.add(t2);
  }
  // tiny arms
  for (const s of [-1, 1]) { const a = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.8, 0.22).translate(0, -0.4, 0), skin); a.position.set(s * 0.8, 4.2, -2.6); a.rotation.x = -0.9; g.add(a); }
  // legs
  const legs = [];
  for (const s of [-1, 1]) {
    const hip = new THREE.Group(); hip.position.set(s * 1.0, 4.2, 0.4); g.add(hip);
    hip.add(new THREE.Mesh(new THREE.BoxGeometry(1.0, 2.4, 1.5).translate(0, -1.0, 0), skin));
    const knee = new THREE.Group(); knee.position.set(0, -2.1, 0.1); hip.add(knee);
    knee.add(new THREE.Mesh(new THREE.BoxGeometry(0.6, 2.0, 0.7).translate(0, -1.0, 0.2), skin));
    const foot = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.35, 1.6).translate(0, -2.0, -0.35), dark); knee.add(foot);
    legs.push({ hip, knee, s });
  }
  g.userData = { head, jaw, legs, tail, body };
  // phase: walk cycle; amp: 0..1 walking; open: jaw 0..1; look: head yaw
  g.userData.pose = (phase, amp, open, look = 0, lift = 0) => {
    for (const L of legs) {
      const p = phase + (L.s > 0 ? Math.PI : 0);
      L.hip.rotation.x = Math.sin(p) * 0.5 * amp;
      L.knee.rotation.x = Math.max(0, Math.cos(p)) * 0.7 * amp;
    }
    jaw.rotation.x = open * 0.75;
    head.rotation.set(-0.15 * open + lift, look, 0);
    tail.rotation.y = Math.sin(phase * 0.5) * 0.12 * amp;
  };
  g.userData.pose(0, 0, 0);
  return shadowed(g);
}

// ---------- Giza ----------
export function limestoneCanvas() {
  const cv = document.createElement('canvas'); cv.width = 128; cv.height = 128; const g = cv.getContext('2d');
  g.fillStyle = '#d9c197'; g.fillRect(0, 0, 128, 128);
  const r = rng(5);
  for (let y = 0; y < 128; y += 16) { g.fillStyle = 'rgba(110,85,55,.45)'; g.fillRect(0, y, 128, 3); for (let x = (y / 16) % 2 * 16; x < 128; x += 32) { g.fillRect(x, y, 2, 16); } }
  for (let i = 0; i < 300; i++) { g.fillStyle = `rgba(255,240,210,${r() * 0.15})`; g.fillRect(r() * 128, r() * 128, 3, 2); }
  return cv;
}
const LIME = limestoneCanvas;
export function pyramid(base, height, hNow = height, smooth = false) {
  const rb = base / Math.SQRT2, rt = rb * (1 - hNow / height);
  const geo = new THREE.CylinderGeometry(Math.max(rt, 0.001), rb, Math.max(hNow, 0.01), 4, 1).rotateY(Math.PI / 4).translate(0, hNow / 2, 0);
  const t = tex(LIME()); t.repeat.set(base / 6, hNow / 6);
  const m = new THREE.Mesh(geo, std({ map: t, color: smooth ? 0xf3e6c8 : 0xffffff, flatShading: true }));
  m.castShadow = m.receiveShadow = true;
  return m;
}
export function sphinx() {
  const g = new THREE.Group();
  const m = std({ color: 0xcfae7c, flatShading: true });
  const body = new THREE.Mesh(new THREE.BoxGeometry(16, 11, 52), m); body.position.set(0, 5.5, 4); g.add(body);
  const haunch = new THREE.Mesh(new THREE.BoxGeometry(17, 13, 14), m); haunch.position.set(0, 6.5, 24); g.add(haunch);
  for (const s of [-1, 1]) { const paw = new THREE.Mesh(new THREE.BoxGeometry(4.5, 3.2, 18), m); paw.position.set(s * 5, 1.6, -27); g.add(paw); }
  const chest = new THREE.Mesh(new THREE.BoxGeometry(14, 10, 8), m); chest.position.set(0, 8, -19); g.add(chest);
  const nemes = new THREE.Mesh(new THREE.CylinderGeometry(4.5, 8, 10, 4).rotateY(Math.PI / 4), std({ color: 0xc5a06c, flatShading: true }));
  nemes.position.set(0, 16, -19); nemes.scale.set(1, 1, 0.7); g.add(nemes);
  const face = new THREE.Mesh(new THREE.BoxGeometry(6.5, 8, 3), std({ color: 0xd8b98a })); face.position.set(0, 17, -23.5); g.add(face);
  const nose = new THREE.Mesh(new THREE.BoxGeometry(1.2, 2, 1), m); nose.position.set(0, 16.5, -25.3); g.add(nose);
  return shadowed(g);
}
export function datePalm(h = 10) {
  const g = new THREE.Group();
  g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.25, 0.35, h, 6).translate(0, h / 2, 0), std({ color: 0x7b5a3a })));
  const lm = std({ color: 0x4b7a2e, flatShading: true, side: THREE.DoubleSide });
  for (let k = 0; k < 8; k++) { const l = new THREE.Mesh(new THREE.ConeGeometry(0.5, 4, 4).rotateZ(Math.PI / 2).translate(2, 0, 0), lm); l.position.y = h; l.rotation.y = k * 0.8; l.rotation.z = -0.5; g.add(l); }
  return shadowed(g);
}

// ---------- Everest ----------
export function mountainMesh(hFn, size = 900, seg = 220, seed = 3) {
  const geo = new THREE.PlaneGeometry(size, size, seg, seg).rotateX(-Math.PI / 2);
  const pos = geo.attributes.position, r = rng(seed);
  for (let i = 0; i < pos.count; i++) pos.setY(i, hFn(pos.getX(i), pos.getZ(i)) + (r() - 0.5) * 0.25);
  geo.computeVertexNormals();
  const nrm = geo.attributes.normal, cols = [];
  for (let i = 0; i < pos.count; i++) {
    const ny = nrm.getY(i); const snow = ny > 0.62 ? 1 : ny > 0.5 ? 0.5 : 0;
    const c = new THREE.Color(0x5b5552).lerp(new THREE.Color(0xf4f8fc), snow); cols.push(c.r, c.g, c.b);
  }
  geo.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3));
  const m = new THREE.Mesh(geo, std({ vertexColors: true, flatShading: true, roughness: 0.95 }));
  m.receiveShadow = true; m.castShadow = true; return m;
}
export function prayerFlags(a, b, n = 18, sag = 1.2) {
  const g = new THREE.Group();
  const C = [0x2e6fd8, 0xffffff, 0xd83a2e, 0x2ea84b, 0xf2c230];
  const A = new THREE.Vector3(...a), B = new THREE.Vector3(...b);
  const pts = [];
  for (let i = 0; i <= n; i++) { const k = i / n; const p = A.clone().lerp(B, k); p.y -= Math.sin(k * Math.PI) * sag; pts.push(p); }
  const line = new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts), 40, 0.02, 4), new THREE.MeshBasicMaterial({ color: 0x333333 })); g.add(line);
  const flags = [];
  for (let i = 1; i < n; i++) {
    const f = new THREE.Mesh(new THREE.PlaneGeometry(0.35, 0.42).translate(0, -0.21, 0), new THREE.MeshStandardMaterial({ color: C[i % 5], side: THREE.DoubleSide, roughness: 1 }));
    f.position.copy(pts[i]); f.lookAt(pts[i].clone().add(new THREE.Vector3(B.z - A.z, 0, A.x - B.x))); g.add(f); flags.push(f);
  }
  g.userData.flutter = (t) => flags.forEach((f, i) => { f.rotation.x = Math.sin(t * 9 + i) * 0.35; });
  return g;
}

// ---------- Paris ----------
function latticeCanvas() {
  const cv = document.createElement('canvas'); cv.width = 128; cv.height = 128; const g = cv.getContext('2d');
  g.clearRect(0, 0, 128, 128); g.strokeStyle = '#fff'; g.lineWidth = 7;
  g.strokeRect(3, 3, 122, 122);
  g.lineWidth = 5; g.beginPath(); g.moveTo(0, 0); g.lineTo(128, 128); g.moveTo(128, 0); g.lineTo(0, 128); g.stroke();
  g.lineWidth = 3; g.beginPath(); g.moveTo(64, 0); g.lineTo(64, 128); g.moveTo(0, 64); g.lineTo(128, 64); g.stroke();
  const t = new THREE.CanvasTexture(cv); t.wrapS = t.wrapT = THREE.RepeatWrapping; return t;
}
export function eiffelTower() {
  const g = new THREE.Group();
  const lat = latticeCanvas();
  const mk = (rep, color = 0x7a5a42) => { const t = lat.clone(); t.needsUpdate = true; t.repeat.set(...rep); return new THREE.MeshStandardMaterial({ color, alphaMap: t, transparent: false, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.7, metalness: 0.3, emissive: 0x000000 }); };
  const mats = [];
  const frustum = (rb, rt, y0, y1, rep) => {
    const m = mk(rep); mats.push(m);
    const mesh = new THREE.Mesh(new THREE.CylinderGeometry(rt, rb, y1 - y0, 4, 1, true).rotateY(Math.PI / 4).translate(0, (y0 + y1) / 2, 0), m); g.add(mesh); return mesh;
  };
  // four legs
  for (const [sx, sz] of [[1, 1], [1, -1], [-1, 1], [-1, -1]]) {
    const foot = new THREE.Vector3(sx * 52, 0, sz * 52), top = new THREE.Vector3(sx * 24, 57, sz * 24);
    const v = top.clone().sub(foot), L = v.length();
    const m = mk([2, 6]); mats.push(m);
    const leg = new THREE.Mesh(new THREE.CylinderGeometry(7, 12, L, 4, 1, true).rotateY(Math.PI / 4), m);
    leg.position.copy(foot).add(top).multiplyScalar(0.5);
    leg.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), v.normalize()); g.add(leg);
    const ped = new THREE.Mesh(new THREE.BoxGeometry(18, 3, 18), std({ color: 0x8f877a })); ped.position.copy(foot).setY(1.5); g.add(ped);
  }
  // decorative arches under the first platform
  const archM = std({ color: 0x6f513c, metalness: 0.3, roughness: 0.6 }); mats.push(archM);
  for (let k = 0; k < 4; k++) {
    const a = new THREE.Mesh(new THREE.TorusGeometry(36, 1.4, 6, 32, Math.PI), archM);
    a.position.y = 20; a.rotation.y = k * Math.PI / 2; a.translateZ(40); g.add(a);
  }
  const platM = std({ color: 0x5e4433, metalness: 0.2 }); mats.push(platM);
  const p1 = new THREE.Mesh(new THREE.BoxGeometry(72, 7, 72), platM); p1.position.y = 58; g.add(p1);
  frustum(30, 19, 61, 115, [4, 6]);
  const p2 = new THREE.Mesh(new THREE.BoxGeometry(42, 5, 42), platM); p2.position.y = 116; g.add(p2);
  frustum(17, 5.5, 118, 270, [3, 22]);
  frustum(5.5, 3.5, 270, 280, [1, 2]);
  const top = new THREE.Mesh(new THREE.BoxGeometry(9, 7, 9), platM); top.position.y = 282; g.add(top);
  const ant = new THREE.Mesh(new THREE.CylinderGeometry(0.6, 1.4, 45, 6), std({ color: 0xbbbbbb })); ant.position.y = 307; g.add(ant);
  g.userData.mats = mats;
  return shadowed(g);
}
function facadeCanvas(seed) {
  const cv = document.createElement('canvas'); cv.width = 256; cv.height = 256; const g = cv.getContext('2d');
  g.fillStyle = '#eadfc8'; g.fillRect(0, 0, 256, 256);
  const r = rng(seed);
  for (let y = 0; y < 5; y++) for (let x = 0; x < 6; x++) {
    g.fillStyle = '#4b5560'; g.fillRect(14 + x * 40, 20 + y * 48, 20, 32);
    if (y === 1 || y === 4) { g.fillStyle = '#2b2b2b'; g.fillRect(8 + x * 40, 50 + y * 48, 32, 4); }
  }
  g.fillStyle = 'rgba(0,0,0,.08)'; for (let y = 0; y < 256; y += 12) g.fillRect(0, y, 256, 1);
  return cv;
}
export function haussmann(w, d, h, seed) {
  const g = new THREE.Group();
  const t = tex(facadeCanvas(seed)); t.repeat.set(Math.max(1, Math.round(w / 24)), 1);
  const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), [std({ map: t }), std({ map: t }), std({ color: 0xeadfc8 }), std({ color: 0xeadfc8 }), std({ map: t }), std({ map: t })]);
  b.position.y = h / 2; g.add(b);
  const roof = new THREE.Mesh(new THREE.CylinderGeometry(0.6, 0.72, 5, 4, 1).rotateY(Math.PI / 4), std({ color: 0x5f6d7c, flatShading: true }));
  roof.scale.set(w * 0.98, 1, d * 0.98); roof.position.y = h + 2.5; g.add(roof);
  for (let i = 0; i < Math.floor(w / 9); i++) { const c = new THREE.Mesh(new THREE.BoxGeometry(1.4, 2.4, 1), std({ color: 0xb46b4f })); c.position.set(-w / 2 + 5 + i * 9, h + 5.5, 0); g.add(c); }
  return shadowed(g);
}
