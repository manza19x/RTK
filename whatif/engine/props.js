// Landmark props for famous locations (low-poly, generic — no real brands)
import { THREE, rng } from './world.js';

const std = (o) => new THREE.MeshStandardMaterial({ roughness: 0.85, ...o });
function shadow(m) { m.traverse((o) => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } }); return m; }

// ---------- textures ----------
export function windowTexture(seed, cols = 8, rows = 24, litRatio = 0.6, warm = true) {
  const cv = document.createElement('canvas'); cv.width = 256; cv.height = 512;
  const g = cv.getContext('2d'); g.fillStyle = '#000'; g.fillRect(0, 0, 256, 512);
  const r = rng(seed), cw = 256 / cols, ch = 512 / rows;
  for (let y = 0; y < rows; y++) for (let x = 0; x < cols; x++) {
    if (r() > litRatio) continue;
    const v = 0.6 + r() * 0.4;
    g.fillStyle = warm ? `rgba(255,${200 + r() * 40 | 0},${130 + r() * 60 | 0},${v})` : `rgba(190,220,255,${v})`;
    g.fillRect(x * cw + cw * 0.18, y * ch + ch * 0.2, cw * 0.64, ch * 0.55);
  }
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}
const AD_WORDS = ['SHOW', 'NEWS', 'LIVE', 'SALE', 'WOW', 'NYC', '24/7', 'PIZZA', 'MUSICAL', '★★★★★', 'HOY', 'FILM', 'SOUND', 'NEW', 'ICE', 'TECH'];
const AD_COLS = [['#ff2d55', '#ffcc00'], ['#00c2ff', '#7a00ff'], ['#00e08a', '#004dff'], ['#ff7a00', '#ff0066'], ['#ffffff', '#ff2d55'], ['#ffe600', '#00b3ff'], ['#ff00c8', '#3300ff'], ['#00ffd5', '#ff3d00']];
export function billboardTexture(seed, w = 512, h = 512) {
  const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
  const g = cv.getContext('2d'); const r = rng(seed);
  const [a, b] = AD_COLS[Math.floor(r() * AD_COLS.length)];
  const gr = g.createLinearGradient(0, 0, w * r(), h); gr.addColorStop(0, a); gr.addColorStop(1, b);
  g.fillStyle = gr; g.fillRect(0, 0, w, h);
  // abstract shapes
  for (let i = 0; i < 5; i++) {
    g.fillStyle = `rgba(255,255,255,${0.12 + r() * 0.25})`;
    g.beginPath(); g.arc(r() * w, r() * h, 30 + r() * 160, 0, 7); g.fill();
  }
  g.fillStyle = r() > 0.5 ? '#fff' : '#111';
  const word = AD_WORDS[Math.floor(r() * AD_WORDS.length)];
  g.font = `900 ${Math.min(w, h) * (word.length > 5 ? 0.22 : 0.34)}px Inter, sans-serif`; g.textAlign = 'center'; g.textBaseline = 'middle';
  g.fillText(word, w / 2, h / 2);
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}

// ---------- Times Square ----------
// returns {group, emissives:[materials], billboards:[materials]}
export function timesSquare(w) {
  const grp = new THREE.Group(); const r = rng(101);
  const emiss = [], boards = [];
  // asphalt + sidewalks
  const road = new THREE.Mesh(new THREE.PlaneGeometry(26, 400).rotateX(-Math.PI / 2), std({ color: 0x2b2c30 }));
  road.position.set(0, 0.02, -100); road.receiveShadow = true; grp.add(road);
  for (let i = 0; i < 40; i++) { // crosswalk / lane stripes
    const s = new THREE.Mesh(new THREE.PlaneGeometry(0.25, 3).rotateX(-Math.PI / 2), std({ color: 0xd8d8d0 }));
    s.position.set(i % 2 ? -4 : 4, 0.03, 40 - i * 8); grp.add(s);
  }
  // buildings on both sides with lit windows and stacked billboards
  for (const side of [-1, 1]) {
    let z = 50;
    while (z > -260) {
      const wd = 14 + r() * 10, dp = 18 + r() * 12, h = 30 + r() * 90;
      const tex = windowTexture(Math.floor(r() * 1e6), 8, Math.round(h / 3.5), 0.55);
      tex.wrapS = tex.wrapT = THREE.RepeatWrapping; tex.repeat.set(wd / 14, 1);
      const m = std({ color: 0x3a3d45, emissive: 0xffffff, emissiveMap: tex, emissiveIntensity: 1 });
      emiss.push(m);
      const b = new THREE.Mesh(new THREE.BoxGeometry(dp, h, wd), m);
      b.position.set(side * (13 + dp / 2), h / 2, z - wd / 2); b.castShadow = b.receiveShadow = true; grp.add(b);
      // billboards on the street face
      const nb = 1 + Math.floor(r() * 3);
      for (let k = 0; k < nb; k++) {
        const bw = wd * (0.6 + r() * 0.35), bh = 5 + r() * 12, by = 6 + k * (bh + 3) + r() * 4;
        if (by + bh / 2 > h) break;
        const bm = new THREE.MeshBasicMaterial({ map: billboardTexture(Math.floor(r() * 1e6), 512, Math.round(512 * bh / bw) || 256), toneMapped: false });
        boards.push(bm);
        const bb = new THREE.Mesh(new THREE.PlaneGeometry(bw, bh), bm);
        bb.position.set(side * 12.9, by, z - wd / 2); bb.rotation.y = -side * Math.PI / 2; grp.add(bb);
      }
      z -= wd + 0.5;
    }
  }
  // the iconic red glowing steps in the middle (generic grandstand)
  const stepsMat = std({ color: 0xb3121d, emissive: 0xff1a2a, emissiveIntensity: 1.6, roughness: 0.4 });
  emiss.push(stepsMat);
  const steps = new THREE.Group();
  for (let i = 0; i < 9; i++) {
    const s = new THREE.Mesh(new THREE.BoxGeometry(10, 0.45, 1.1), stepsMat);
    s.position.set(0, 0.22 + i * 0.45, -i * 1.0); s.scale.y = 1 + i * 0.001; steps.add(s);
    const fill = new THREE.Mesh(new THREE.BoxGeometry(10, 0.45 * (i + 1), 1.0), std({ color: 0x3a1214 }));
    fill.position.set(0, 0.45 * (i + 1) / 2, -i * 1.0); steps.add(fill);
  }
  steps.position.set(0, 0, -62); shadow(steps); grp.add(steps);
  // tall tower with big screen at the end (the "ball drop" style building, generic)
  const tower = new THREE.Mesh(new THREE.BoxGeometry(12, 110, 10), std({ color: 0x30333b }));
  tower.position.set(0, 55, -110); tower.castShadow = true; grp.add(tower);
  for (let k = 0; k < 5; k++) {
    const bm = new THREE.MeshBasicMaterial({ map: billboardTexture(900 + k, 512, 256), toneMapped: false }); boards.push(bm);
    const bb = new THREE.Mesh(new THREE.PlaneGeometry(11.5, 5.5), bm); bb.position.set(0, 10 + k * 7, -104.9); grp.add(bb);
  }
  w.scene.add(grp);
  return { group: grp, emiss, boards };
}

export function taxi() {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.BoxGeometry(1.9, 0.8, 4.6), std({ color: 0xf5c518, roughness: 0.45 })); body.position.y = 0.65;
  const cab = new THREE.Mesh(new THREE.BoxGeometry(1.7, 0.6, 2.3), std({ color: 0x223040, roughness: 0.2, metalness: 0.4 })); cab.position.set(0, 1.35, -0.1);
  const sign = new THREE.Mesh(new THREE.BoxGeometry(0.8, 0.25, 0.25), new THREE.MeshBasicMaterial({ color: 0xfff6c8 })); sign.position.set(0, 1.78, -0.1);
  const hl = new THREE.MeshBasicMaterial({ color: 0xfff3d0 });
  for (const x of [-0.65, 0.65]) { const l = new THREE.Mesh(new THREE.BoxGeometry(0.35, 0.18, 0.05), hl); l.position.set(x, 0.75, -2.31); g.add(l); }
  const tl = new THREE.MeshBasicMaterial({ color: 0xff2020 });
  for (const x of [-0.7, 0.7]) { const l = new THREE.Mesh(new THREE.BoxGeometry(0.3, 0.15, 0.05), tl); l.position.set(x, 0.8, 2.31); g.add(l); }
  for (const [x, z] of [[-0.95, -1.4], [0.95, -1.4], [-0.95, 1.4], [0.95, 1.4]]) {
    const wh = new THREE.Mesh(new THREE.CylinderGeometry(0.33, 0.33, 0.25, 10).rotateZ(Math.PI / 2), std({ color: 0x111111 })); wh.position.set(x, 0.33, z); g.add(wh);
  }
  g.add(body, cab, sign); g.userData.lights = [hl, tl, sign.material];
  return shadow(g);
}

// Milky Way: dense band of faint points along a great circle
export function milkyWay(w) {
  const r = rng(303), pts = [], cols = [];
  const axis = new THREE.Vector3(0.35, 0.25, 1).normalize();
  const u = new THREE.Vector3(1, 0, -0.35).normalize(), v = new THREE.Vector3().crossVectors(axis, u).normalize();
  for (let i = 0; i < 14000; i++) {
    const a = r() * Math.PI * 2, spread = (r() + r() + r() - 1.5) * 0.12;
    const d = u.clone().multiplyScalar(Math.cos(a)).add(v.clone().multiplyScalar(Math.sin(a))).add(axis.clone().multiplyScalar(spread)).normalize();
    if (d.y < -0.05) continue;
    pts.push(d.x * 1150, d.y * 1150, d.z * 1150);
    const c = 0.6 + r() * 0.4; cols.push(c, c * (0.9 + r() * 0.1), c * (0.85 + r() * 0.2));
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pts, 3)); geo.setAttribute('color', new THREE.Float32BufferAttribute(cols, 3));
  const mat = new THREE.PointsMaterial({ size: 2.0, sizeAttenuation: false, vertexColors: true, transparent: true, opacity: 0, fog: false, depthWrite: false });
  const p = new THREE.Points(geo, mat); p.renderOrder = -9; w.scene.add(p);
  // soft glow band
  const cv = document.createElement('canvas'); cv.width = 64; cv.height = 256;
  const g = cv.getContext('2d'); const gr = g.createLinearGradient(0, 0, 64, 0);
  gr.addColorStop(0, 'rgba(180,190,255,0)'); gr.addColorStop(0.5, 'rgba(210,200,255,0.55)'); gr.addColorStop(1, 'rgba(180,190,255,0)');
  g.fillStyle = gr; g.fillRect(0, 0, 64, 256);
  const tex = new THREE.CanvasTexture(cv);
  const bandMat = new THREE.MeshBasicMaterial({ map: tex, transparent: true, opacity: 0, fog: false, depthWrite: false, side: THREE.DoubleSide, blending: THREE.AdditiveBlending });
  const band = new THREE.Mesh(new THREE.CylinderGeometry(1100, 1100, 260, 64, 1, true), bandMat);
  band.geometry.rotateZ(Math.PI / 2); // placeholder orientation, aligned below
  band.quaternion.setFromUnitVectors(new THREE.Vector3(1, 0, 0), axis);
  band.renderOrder = -9; w.scene.add(band);
  return { points: p, band, set(o) { mat.opacity = o; bandMat.opacity = o * 0.7; }, follow(c) { p.position.copy(c); band.position.copy(c); } };
}

// ---------- Santa Monica ----------
export function palm(h = 9, seed = 1) {
  const r = rng(seed);
  const g = new THREE.Group();
  const trunk = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.3, h, 6).translate(0, h / 2, 0), std({ color: 0x8b6b45 }));
  trunk.rotation.z = (r() - 0.5) * 0.12; g.add(trunk);
  const leaf = std({ color: 0x2f7d3a, flatShading: true, side: THREE.DoubleSide });
  for (let k = 0; k < 7; k++) {
    const lf = new THREE.Mesh(new THREE.ConeGeometry(0.45, 3.4, 4).rotateZ(Math.PI / 2).translate(1.6, 0, 0), leaf);
    lf.position.y = h; lf.rotation.y = k / 7 * Math.PI * 2 + r(); lf.rotation.z = -0.45 - r() * 0.2; g.add(lf);
  }
  return shadow(g);
}
export function ferrisWheel(radius = 13) {
  const g = new THREE.Group(), wheel = new THREE.Group();
  const steel = std({ color: 0xf0f0f0, roughness: 0.5, metalness: 0.3 });
  for (const z of [-1.2, 1.2]) {
    const ring = new THREE.Mesh(new THREE.TorusGeometry(radius, 0.18, 6, 48), steel); ring.position.z = z; wheel.add(ring);
    const inner = new THREE.Mesh(new THREE.TorusGeometry(radius * 0.35, 0.12, 6, 24), steel); inner.position.z = z; wheel.add(inner);
  }
  const CC = [0xe63b3b, 0xf2d03b, 0x3b8fe6, 0x3fc46a, 0x8e44c9, 0xf08a2b, 0x2ec4c4, 0xf27ab0];
  const cabins = [];
  for (let i = 0; i < 20; i++) {
    const a = i / 20 * Math.PI * 2;
    const sp = new THREE.Mesh(new THREE.BoxGeometry(0.12, radius, 0.12).translate(0, radius / 2, 0), steel); sp.rotation.z = a; wheel.add(sp);
    const cab = new THREE.Mesh(new THREE.BoxGeometry(1.5, 1.6, 1.5), std({ color: CC[i % CC.length] }));
    cab.position.set(Math.cos(a) * radius, Math.sin(a) * radius - 1.0, 0); cabins.push(cab); g.add(cab);
  }
  const hub = new THREE.Mesh(new THREE.CylinderGeometry(0.8, 0.8, 3, 12).rotateX(Math.PI / 2), steel); wheel.add(hub);
  for (const s of [-1, 1]) for (const z of [-1.6, 1.6]) {
    const leg = new THREE.Mesh(new THREE.BoxGeometry(0.4, radius + 3, 0.4), steel);
    leg.position.set(s * (radius * 0.35), (radius + 3) / 2 - 3 + 0.0, z); leg.rotation.z = -s * 0.32; g.add(leg);
  }
  wheel.position.y = radius + 2; g.add(wheel);
  g.userData = { wheel, cabins, radius };
  g.userData.spin = (t) => {
    wheel.rotation.z = t * 0.12;
    cabins.forEach((c, i) => { const a = i / 20 * Math.PI * 2 + t * 0.12; c.position.set(Math.cos(a) * radius, radius + 2 + Math.sin(a) * radius - 1.0, 0); });
  };
  g.userData.spin(0);
  return shadow(g);
}
export function pier(len = 300, deckY = 6) {
  const g = new THREE.Group();
  const wood = std({ color: 0x8a6d4e });
  const deck = new THREE.Mesh(new THREE.BoxGeometry(22, 0.8, len), wood); deck.position.set(0, deckY, -len / 2); g.add(deck);
  const pile = std({ color: 0x5b4636 });
  for (let z = -4; z > -len; z -= 8) for (const x of [-10, -3.5, 3.5, 10]) {
    const p = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.3, deckY + 30), pile); p.position.set(x, deckY - (deckY + 30) / 2, z); g.add(p);
  }
  const rail = std({ color: 0xf2efe6 });
  for (const x of [-11, 11]) { const rl = new THREE.Mesh(new THREE.BoxGeometry(0.15, 1.1, len), rail); rl.position.set(x, deckY + 0.95, -len / 2); g.add(rl); }
  // arcade / restaurant buildings + entrance sign arch
  const BC = [0xf6e7c8, 0xe9c46a, 0x9fd3e0, 0xf4a261];
  for (let i = 0; i < 6; i++) {
    const b = new THREE.Mesh(new THREE.BoxGeometry(7, 4 + (i % 3), 10), std({ color: BC[i % BC.length] }));
    b.position.set(i % 2 ? 6 : -6, deckY + 2.4, -30 - i * 22); g.add(b);
  }
  const arch = new THREE.Group();
  const archM = std({ color: 0x2a6fb0 });
  for (const x of [-5.5, 5.5]) { const p = new THREE.Mesh(new THREE.BoxGeometry(0.6, 6, 0.6), archM); p.position.set(x, deckY + 3, -2); arch.add(p); }
  const top = new THREE.Mesh(new THREE.BoxGeometry(12, 1.6, 0.6), new THREE.MeshBasicMaterial({ color: 0xffc94d })); top.position.set(0, deckY + 6.2, -2); arch.add(top);
  g.add(arch);
  // roller coaster (simple looping track)
  const pts = [];
  for (let i = 0; i <= 80; i++) { const a = i / 80 * Math.PI * 2; pts.push(new THREE.Vector3(Math.cos(a) * 8 + 4, deckY + 5 + Math.sin(a * 3) * 2.5 + 2.5, Math.sin(a) * 14 - 140)); }
  const track = new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts, true), 160, 0.25, 6, true), std({ color: 0xd62828 }));
  g.add(track);
  for (let i = 0; i < 80; i += 5) { const p = pts[i]; const s = new THREE.Mesh(new THREE.BoxGeometry(0.2, p.y - deckY, 0.2), std({ color: 0xdddddd })); s.position.set(p.x, (p.y + deckY) / 2, p.z); g.add(s); }
  return shadow(g);
}
export function lifeguardTower(color = 0x5fb7e5, seed = 1) {
  const g = new THREE.Group();
  const legM = std({ color: 0xeeeeee });
  for (const [x, z] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) { const l = new THREE.Mesh(new THREE.BoxGeometry(0.2, 2.4, 0.2), legM); l.position.set(x, 1.2, z); g.add(l); }
  const deck = new THREE.Mesh(new THREE.BoxGeometry(3.4, 0.25, 3.4), legM); deck.position.y = 2.4; g.add(deck);
  const hut = new THREE.Mesh(new THREE.BoxGeometry(2.6, 2.0, 2.4), std({ color })); hut.position.y = 3.5; g.add(hut);
  const win = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.7, 0.05), std({ color: 0x1d2a3a, roughness: 0.2 })); win.position.set(0, 3.8, -1.22); g.add(win);
  const roof = new THREE.Mesh(new THREE.BoxGeometry(3.0, 0.3, 2.8), std({ color: 0xf6f3ea })); roof.position.y = 4.65; g.add(roof);
  const ramp = new THREE.Mesh(new THREE.BoxGeometry(1, 0.15, 4), legM); ramp.position.set(0, 1.2, 3.2); ramp.rotation.x = 0.55; g.add(ramp);
  return shadow(g);
}

// ---------- Miami ----------
export function artDeco(seed) {
  const r = rng(seed);
  const PAST = [0xf7c6d9, 0x9fe3d6, 0xfde3a7, 0xc9b6f2, 0xffffff, 0xa8d8f0, 0xf9d5b5];
  const ACC = [0xff5ea8, 0x20c4b0, 0xffb200, 0x7a5cff, 0x2a9df4];
  const g = new THREE.Group();
  const w = 12 + r() * 8, h = 9 + r() * 12, d = 12;
  const main = std({ color: PAST[Math.floor(r() * PAST.length)] }), acc = std({ color: ACC[Math.floor(r() * ACC.length)] });
  const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), main); b.position.y = h / 2; g.add(b);
  // horizontal "eyebrow" bands & windows
  const winM = std({ color: 0x24394d, roughness: 0.2 });
  for (let y = 3; y < h - 1; y += 3) {
    const band = new THREE.Mesh(new THREE.BoxGeometry(w + 0.3, 0.25, 0.8), acc); band.position.set(0, y + 0.9, d / 2 + 0.2); g.add(band);
    for (let x = -w / 2 + 1.5; x < w / 2 - 1; x += 2.6) { const wi = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1.3, 0.1), winM); wi.position.set(x, y - 0.2, d / 2 + 0.05); g.add(wi); }
  }
  // central tower fin with neon
  const fin = new THREE.Mesh(new THREE.BoxGeometry(2.4, h + 4, 1.2), main); fin.position.set((r() - 0.5) * w * 0.4, (h + 4) / 2, d / 2 + 0.5); g.add(fin);
  const neon = new THREE.Mesh(new THREE.BoxGeometry(0.25, h + 3, 0.25), new THREE.MeshBasicMaterial({ color: ACC[Math.floor(r() * ACC.length)] }));
  neon.position.set(fin.position.x, (h + 3) / 2, d / 2 + 1.15); g.add(neon);
  const awn = new THREE.Mesh(new THREE.BoxGeometry(w * 0.9, 0.2, 2.2), acc); awn.position.set(0, 3, d / 2 + 1.1); g.add(awn);
  g.userData.w = w;
  return shadow(g);
}

export function shark() {
  const g = new THREE.Group();
  const top = std({ color: 0x5b6b78, roughness: 0.6, flatShading: true }), belly = std({ color: 0xe8ece9, roughness: 0.7, flatShading: true });
  // body: elongated, along -z (head at -z)
  const body = new THREE.Mesh(new THREE.SphereGeometry(1, 14, 10), top); body.scale.set(1.6, 1.7, 8); g.add(body);
  const bel = new THREE.Mesh(new THREE.SphereGeometry(1, 14, 10), belly); bel.scale.set(1.5, 1.2, 7.4); bel.position.y = -0.55; g.add(bel);
  const snout = new THREE.Mesh(new THREE.ConeGeometry(1.25, 3, 10).rotateX(-Math.PI / 2), top); snout.position.z = -8.4; snout.scale.set(1.1, 0.9, 1); g.add(snout);
  const tri = (a, b, c, m) => { const geo = new THREE.BufferGeometry(); geo.setAttribute('position', new THREE.Float32BufferAttribute([...a, ...b, ...c], 3)); geo.computeVertexNormals(); const mm = m.clone(); mm.side = THREE.DoubleSide; return new THREE.Mesh(geo, mm); };
  const dorsal = tri([0, 1.4, -1.5], [0, 1.4, 2.2], [0, 3.2, 1.6], top); g.add(dorsal);
  const tail = new THREE.Group();
  tail.add(tri([0, 0, 0], [0, 4.2, 2.6], [0, 0.4, 1.0], top), tri([0, 0, 0], [0, -2.8, 2.0], [0, -0.3, 1.0], top));
  tail.position.z = 7.6; g.add(tail);
  for (const s of [-1, 1]) g.add(tri([s * 1.3, -0.6, -3.5], [s * 4.2, -1.6, -1.0], [s * 1.3, -0.7, -1.2], top));
  // eye + mouth line
  for (const s of [-1, 1]) { const e = new THREE.Mesh(new THREE.SphereGeometry(0.16, 8, 6), new THREE.MeshBasicMaterial({ color: 0x050505 })); e.position.set(s * 1.0, 0.5, -7.2); g.add(e); }
  const mouth = new THREE.Mesh(new THREE.TorusGeometry(1.0, 0.08, 4, 16, Math.PI), new THREE.MeshBasicMaterial({ color: 0x2a1015 }));
  mouth.rotation.set(Math.PI / 2, 0, Math.PI); mouth.position.set(0, -0.6, -7.6); g.add(mouth);
  g.userData.tail = tail;
  return shadow(g);
}

// splash particles (deterministic, function of time since t0)
export function splash(w, n = 160, seed = 9) {
  const r = rng(seed);
  const m = new THREE.InstancedMesh(new THREE.IcosahedronGeometry(0.35, 0), new THREE.MeshStandardMaterial({ color: 0xf2fbff, roughness: 0.3, transparent: true, opacity: 0.95 }), n);
  m.frustumCulled = false; w.scene.add(m);
  const p = [...Array(n)].map(() => ({ a: r() * Math.PI * 2, v: 4 + r() * 10, up: 6 + r() * 14, s: 0.5 + r() * 1.6 }));
  const M = new THREE.Matrix4(), V = new THREE.Vector3(), S = new THREE.Vector3(), Q = new THREE.Quaternion();
  return {
    update(dt, x, y, z, k = 1) {
      for (let i = 0; i < n; i++) {
        const q = p[i];
        if (dt < 0 || dt > 3.5) { M.makeScale(0, 0, 0); m.setMatrixAt(i, M); continue; }
        const yy = y + q.up * k * dt - 9.8 * dt * dt;
        const sc = yy < y - 0.5 ? 0 : q.s * (1 - dt / 3.5);
        V.set(x + Math.cos(q.a) * q.v * dt * k, yy, z + Math.sin(q.a) * q.v * dt * k); S.setScalar(sc);
        M.compose(V, Q, S); m.setMatrixAt(i, M);
      }
      m.instanceMatrix.needsUpdate = true;
    },
  };
}

export function fishSchool(w, n = 60, seed = 3) {
  const r = rng(seed);
  const geo = new THREE.ConeGeometry(0.25, 0.9, 5).rotateX(Math.PI / 2);
  const m = new THREE.InstancedMesh(geo, new THREE.MeshStandardMaterial({ color: 0xffb347, roughness: 0.5 }), n);
  const C = [0xffb347, 0xffe066, 0x66d9ff, 0xff6f91];
  for (let i = 0; i < n; i++) m.setColorAt(i, new THREE.Color(C[i % 4]));
  m.frustumCulled = false; w.scene.add(m);
  const p = [...Array(n)].map(() => ({ ph: r() * 6.28, rad: 3 + r() * 6, y: r() * 6 - 3, sp: 0.3 + r() * 0.3 }));
  const M = new THREE.Matrix4(), V = new THREE.Vector3(), S = new THREE.Vector3(1, 1, 1), Q = new THREE.Quaternion(), E = new THREE.Euler();
  return {
    mesh: m,
    update(t, cx, cy, cz) {
      for (let i = 0; i < n; i++) {
        const q = p[i], a = q.ph + t * q.sp;
        V.set(cx + Math.cos(a) * q.rad, cy + q.y + Math.sin(t + i) * 0.3, cz + Math.sin(a) * q.rad);
        E.set(0, -a, 0); Q.setFromEuler(E); M.compose(V, Q, S); m.setMatrixAt(i, M);
      }
      m.instanceMatrix.needsUpdate = true;
    },
  };
}
