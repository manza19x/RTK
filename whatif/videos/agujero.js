import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';

const JUMP = 10.0, FALL_IN = 10.9, CORE = 20.0, OUT = 29.5, BACK = 35.4, AIR = 41.6, END = 46.6;
let crowd, jumper, locals, sheep, hole, trees;

// tunnel speed (0..1) and depth (0 surface .. 1 centre)
function speed(t) {
  if (t < FALL_IN) return 0;
  if (t < CORE) return Math.sin(prog(t, FALL_IN, CORE) * Math.PI / 2);
  if (t < 23.6) return 1;
  if (t < OUT) return Math.cos(prog(t, 23.6, OUT) * Math.PI / 2);
  if (t < BACK + 0.6) return 0;
  if (t < AIR) return smooth(prog(t, BACK + 0.6, BACK + 2));
  return 1 - smooth(prog(t, AIR, END - 0.8));
}
function depth(t) {
  if (t < OUT) return t < 21.5 ? smooth(prog(t, FALL_IN, 21.5)) : 1 - smooth(prog(t, 21.5, OUT));
  if (t < AIR) return 0.25 + 0.6 * smooth(prog(t, BACK + 0.6, AIR));
  return 0.85 + 0.15 * smooth(prog(t, AIR, AIR + 2));
}
const phaseCache = [];
function phase(t) {
  // integral of speed, sampled at 60 Hz (deterministic)
  const i = Math.floor(t * 60);
  if (!phaseCache.length) { let acc = 0; for (let k = 0; k < 60 * 60; k++) { phaseCache.push(acc); acc += speed(k / 60) * 26 / 60; } }
  return phaseCache[Math.min(i, phaseCache.length - 1)];
}
const ROCK = [120, 86, 62], MAGMA = [210, 84, 28], CORECOL = [255, 214, 140];
function tunnelCol(d) {
  const a = d < 0.6 ? ROCK : MAGMA, b = d < 0.6 ? MAGMA : CORECOL, k = d < 0.6 ? d / 0.6 : (d - 0.6) / 0.4;
  return a.map((x, i) => x + (b[i] - x) * k);
}

export default {
  title: '¿Y si saltaras por un agujero que atraviesa la Tierra?',
  duration: 51.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Profundidad del agujero', value: count(0, 12742, { dur: 2.4, suf: ' km' }), sub: 'De lado a lado del planeta' },
    { t: 5.6, label: 'Resistencia del aire', value: '0', sub: 'La quitamos (de momento)' },
    { t: 8.0, label: 'Voluntarios', value: '1', sub: 'Valiente', pulse: true },
    { t: FALL_IN, label: 'Velocidad', value: count(0, 28000, { dur: CORE - FALL_IN, suf: ' km/h', curve: 'smooth' }), sub: 'Cayendo' },
    { t: 16.0, label: 'Temperatura fuera', value: count(15, 5200, { dur: 2.4, suf: ' °C' }), sub: 'Y subiendo', color: '#ffd2a8' },
    { t: 18.6, label: 'Centro de la Tierra', value: 'Minuto 21', sub: 'Velocidad: 28.000 km/h', pulse: true },
    { t: 21.0, label: 'Gravedad', value: '0 g', sub: 'Toda la Tierra tira de ti igual' },
    { t: 23.6, label: 'Velocidad', value: count(28000, 0, { dur: OUT - 23.6, suf: ' km/h', curve: 'smooth' }), sub: 'Frenando' },
    { t: 28.4, label: 'Tiempo total', value: '42 min', sub: 'De lado a lado' },
    { t: OUT + 0.3, label: 'Llegada', value: 'Nueva Zelanda', sub: 'El otro lado de España', pulse: true },
    { t: 36.8, label: 'Ida y vuelta', value: '84 min', sub: 'Una y otra vez' },
    { t: AIR, label: 'Con aire', value: 'Centro', sub: 'Acabarías flotando ahí', pulse: true },
  ],
  captions: [
    { t: 3.2, d: 2.3, text: 'Un agujero de lado a lado\ndel planeta.' },
    { t: 5.6, d: 2.3, text: 'Sin aire. Sin tocar\nlas paredes.' },
    { t: 8.0, d: 1.9, text: 'Alguien tiene que probarlo.' },
    { t: FALL_IN + 0.2, d: 2.3, text: 'Caes… y caes…' },
    { t: 13.6, d: 2.3, text: 'En un minuto ya vas\nmás rápido que un avión.' },
    { t: 16.0, d: 2.5, text: 'Al otro lado de la pared:\n5.200 °C.' },
    { t: 18.6, d: 2.3, text: 'Minuto 21:\nel centro de la Tierra.' },
    { t: 21.0, d: 2.5, text: 'Aquí la gravedad es cero.' },
    { t: 23.6, d: 2.3, text: 'Ahora la gravedad te frena.' },
    { t: 26.0, d: 2.3, text: 'Subes… cada vez\nmás despacio.' },
    { t: 28.4, d: 1.9, text: '42 minutos después…' },
    { t: OUT + 0.4, d: 2.4, text: '…sales en Nueva Zelanda.' },
    { t: 32.4, d: 2.1, text: 'Con velocidad cero.\nJusto en el borde.' },
    { t: 34.6, d: 2.0, text: 'Si nadie te agarra…' },
    { t: 36.8, d: 2.5, text: '…vuelves a caer.\nIda y vuelta, cada 84 minutos.' },
    { t: 39.3, d: 2.2, text: 'Como un péndulo humano.' },
    { t: AIR, d: 2.4, text: 'Pero con aire, frenarías\nun poco en cada viaje…' },
    { t: 44.0, d: 2.6, text: '…y acabarías flotando\nen el centro de la Tierra.' },
  ],
  end: { t: END, fact: 'El agujero más profundo jamás excavado llega a solo 12 km' },
  fx(t) {
    const inA = smooth(prog(t, FALL_IN - 0.3, FALL_IN + 0.2)) * (1 - smooth(prog(t, OUT - 0.3, OUT)));
    const backA = smooth(prog(t, BACK + 0.2, BACK + 0.6));
    const a = Math.max(inA, backA);
    const d = depth(t);
    const c = tunnelCol(d);
    return {
      tunnel: a > 0 ? { a, col: c, glow: d > 0.6 ? [255, 236, 190] : [0, 0, 0], glowA: d > 0.6 ? 0.25 + 0.7 * (d - 0.6) / 0.4 : 0.95, phase: phase(t), speed: speed(t), person: true } : null,
      tint: d > 0.5 && a > 0 ? `rgba(255,120,30,${0.12 * a * (d - 0.5) * 2})` : null,
      flash: (t > OUT && t < OUT + 0.3 ? (1 - (t - OUT) / 0.3) * 0.5 : 0),
      vignette: a * 0.2,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: 8.0, mood: 'tense' }, { t: FALL_IN, mood: 'epic' }, { t: 21, mood: 'drone' }, { t: OUT, mood: 'curious' }, { t: BACK, mood: 'epic' }, { t: AIR, mood: 'night' }, { t: END - 0.3, mood: 'none' }],
    amb: [
      { type: 'crowd', from: 0, to: FALL_IN, gain: 0.5, fout: 0.4 }, { type: 'storm', from: FALL_IN, to: OUT, gain: 0.45 },
      { type: 'wind', from: OUT, to: BACK + 0.5, gain: 0.35 }, { type: 'storm', from: BACK + 0.4, to: END, gain: 0.35, fout: 3 },
    ],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 5.6, type: 'tick' }, { t: 8, type: 'heartbeat' }, { t: 9, type: 'heartbeat' }, { t: JUMP - 0.6, type: 'riser', d: 0.7 },
      { t: JUMP + 0.1, type: 'gasp' }, { t: FALL_IN - 0.2, type: 'whoosh_up' }, { t: 16, type: 'rumble', d: 2.5 }, { t: 18.6, type: 'boom', gain: 0.5 },
      { t: 21, type: 'shimmer' }, { t: 23.6, type: 'tick' }, { t: 28.4, type: 'riser', d: 1.1 }, { t: OUT, type: 'whoosh' }, { t: OUT + 0.2, type: 'pop', gain: 0.6 },
      { t: 34.6, type: 'heartbeat' }, { t: BACK + 0.2, type: 'whoosh_up' }, { t: 36.8, type: 'tick' }, { t: AIR, type: 'tick' }, { t: 44, type: 'shimmer' },
      { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0);
    w.sea.visible = false;
    w.groundMat.color.set(0xb9ad9a);
    w.setSky(0x4683c6, 0xe3ecef, 0xd6e1e6);
    w.setFog(40, 260);
    w.setSunDir(0.5, 0.7, 0.3);
    // the hole: dark disc with gradient + raised rim
    const cv = document.createElement('canvas'); cv.width = cv.height = 256;
    const g = cv.getContext('2d'); const rg = g.createRadialGradient(128, 128, 20, 128, 128, 128);
    rg.addColorStop(0, '#000'); rg.addColorStop(0.62, '#050403'); rg.addColorStop(0.9, '#3a2e24'); rg.addColorStop(1, '#5b4a3a');
    g.fillStyle = rg; g.fillRect(0, 0, 256, 256);
    const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.SRGBColorSpace;
    hole = new THREE.Group();
    const disc = new THREE.Mesh(new THREE.CircleGeometry(2.6, 48).rotateX(-Math.PI / 2), new THREE.MeshBasicMaterial({ map: tex }));
    disc.position.y = 0.03; hole.add(disc);
    const rim = new THREE.Mesh(new THREE.TorusGeometry(2.7, 0.18, 8, 48).rotateX(Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x8a8f96, roughness: 0.6, metalness: 0.3 }));
    rim.position.y = 0.1; rim.castShadow = true; hole.add(rim);
    // warning cones
    for (let i = 0; i < 6; i++) {
      const a = i / 6 * Math.PI * 2 + 0.3;
      const c = new THREE.Mesh(new THREE.ConeGeometry(0.22, 0.6, 8), new THREE.MeshStandardMaterial({ color: 0xff6a1f }));
      c.position.set(Math.cos(a) * 3.4, 0.3, Math.sin(a) * 3.4); c.castShadow = true; hole.add(c);
    }
    w.scene.add(hole);
    // spectators in a ring
    crowd = new Crowd(w, 1200, 21);
    const r = rng(8);
    for (const p of crowd.p) {
      let x, z;
      do { x = (r() - 0.5) * 50; z = (r() - 0.5) * 50; } while (Math.hypot(x, z) < 4.6 || Math.hypot(x, z) > 25);
      p.bx = x; p.bz = z; p.bry = Math.atan2(-x, -z) + (r() - 0.5) * 0.4;
      p.ry = p.bry;
    }
    jumper = new Crowd(w, 1, 5);
    jumper.torso.setColorAt(0, new THREE.Color(0xe63b3b)); jumper.arms.setColorAt(0, new THREE.Color(0xe63b3b)); jumper.arms.setColorAt(1, new THREE.Color(0xe63b3b));
    jumper.legs.setColorAt(0, new THREE.Color(0x1d2a4a)); jumper.hair.setColorAt(0, new THREE.Color(0x2a1d14));
    // New Zealand set
    locals = new Crowd(w, 14, 33);
    locals.p.forEach((p, i) => { const a = i / 14 * Math.PI * 1.2 + 2.2; p.bx = Math.cos(a) * (6 + r() * 3); p.bz = Math.sin(a) * (6 + r() * 3); p.bry = Math.atan2(-p.bx, -p.bz); });
    sheep = new THREE.Group();
    const wool = new THREE.MeshStandardMaterial({ color: 0xf2f0ea, roughness: 1 }), dark = new THREE.MeshStandardMaterial({ color: 0x2b2522 });
    for (let i = 0; i < 26; i++) {
      const s = new THREE.Group();
      const b = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.6, 1.3), wool); b.position.y = 0.75; b.castShadow = true;
      const h = new THREE.Mesh(new THREE.BoxGeometry(0.38, 0.4, 0.45), dark); h.position.set(0, 0.95, 0.8);
      for (const [lx, lz] of [[-0.3, -0.45], [0.3, -0.45], [-0.3, 0.45], [0.3, 0.45]]) { const l = new THREE.Mesh(new THREE.BoxGeometry(0.14, 0.45, 0.14), dark); l.position.set(lx, 0.22, lz); s.add(l); }
      s.add(b, h);
      let x, z; do { x = (r() - 0.5) * 70; z = -r() * 60 + 8; } while (Math.hypot(x, z) < 9);
      s.position.set(x, 0, z); s.rotation.y = r() * 6.28; sheep.add(s);
    }
    trees = new THREE.Group();
    const leaf = new THREE.MeshStandardMaterial({ color: 0x3f7d3a, flatShading: true }), trunk = new THREE.MeshStandardMaterial({ color: 0x6b4a2f });
    for (let i = 0; i < 30; i++) {
      const tr = new THREE.Group();
      const c = new THREE.Mesh(new THREE.ConeGeometry(1.6, 4.5, 7), leaf); c.position.y = 3.6; c.castShadow = true;
      const k = new THREE.Mesh(new THREE.CylinderGeometry(0.2, 0.25, 1.6), trunk); k.position.y = 0.8;
      tr.add(c, k); const a = r() * 6.28, d = 30 + r() * 60; tr.position.set(Math.cos(a) * d, 0, Math.sin(a) * d - 20); tr.scale.setScalar(0.8 + r() * 0.8);
      trees.add(tr);
    }
    // hills
    const hillMat = new THREE.MeshStandardMaterial({ color: 0x6f9e52, flatShading: true });
    for (let i = 0; i < 7; i++) {
      const hgeo = new THREE.SphereGeometry(1, 10, 6); const hm = new THREE.Mesh(hgeo, hillMat);
      hm.position.set(-200 + i * 70, -20, -170 - r() * 60); hm.scale.set(60 + r() * 40, 35 + r() * 25, 40); trees.add(hm);
    }
    w.scene.add(sheep, trees);
  },

  update(t, w) {
    const nz = t >= OUT - 0.3 && t < BACK + 0.6;
    // sets
    crowd.p.forEach((p) => (p.vis = nz ? 0 : 1));
    locals.p.forEach((p) => (p.vis = nz ? 1 : 0));
    sheep.visible = trees.visible = nz;
    w.groundMat.color.set(nz ? 0x82b05e : 0xb9ad9a);
    if (nz) { w.setSky(0x2f78cf, 0xd6ecf7, 0xcfe6ef); w.setSunDir(-0.4, 0.6, 0.5); }
    else { w.setSky(0x4683c6, 0xe3ecef, 0xd6e1e6); w.setSunDir(0.5, 0.7, 0.3); }

    // camera
    let cam;
    if (!nz) cam = keys(t, [
      [0, [0, 16, 22, 0, 0, 0, 50]], [3.2, [6, 11, 15, 0, 0, 0, 50]], [7.8, [3, 5.5, 11, 0, 0.8, 0, 50]],
      [JUMP - 0.4, [1.5, 4.5, 8.5, 0, 0.5, 0, 50]], [FALL_IN, [0.4, 7.5, 2.4, 0, -2, 0, 54]], [BACK, [0.4, 7.5, 2.4, 0, -2, 0, 54]],
    ]);
    else cam = keys(t, [[OUT - 0.3, [0.5, 1.6, 9.5, 0, 1.8, 0, 52]], [32.4, [1.5, 1.5, 9, 0, 1.6, 0, 52]], [34.6, [3, 3.5, 9, 0, 0.8, 0, 52]], [BACK + 0.6, [0.5, 7, 3, 0, -2, 0, 54]]]);
    w.shake = speed(t) * 0.03;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // spectators: look at hole / jumper, react when he jumps
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry + Math.sin(t * 0.5 + p.rnd * 20) * 0.08; p.y = 0; p.lean = 0;
      p.armL = p.armR = 0.03;
      const shock = smooth(prog(t, JUMP + 0.2 + p.rnd * 0.4, JUMP + 0.7 + p.rnd * 0.4));
      p.lean = shock * 0.3 * (p.rnd2 > 0.5 ? 1 : 0.4);
      p.armL = p.armR = lerp(0.03, p.rnd3 > 0.5 ? 0.9 : 0.3, shock);
    }
    crowd.update();
    // locals in NZ stare at the hole, startled when he pops out
    for (const p of locals.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry; p.y = 0; p.lean = 0;
      const s = smooth(prog(t, OUT + 0.3 + p.rnd * 0.3, OUT + 0.8 + p.rnd * 0.3));
      p.lean = -0.25 * s; p.armL = p.armR = 0.6 * s * (p.rnd2 > 0.4 ? 1 : 0.2);
    }
    locals.update();

    // the jumper
    const j = jumper.p[0];
    j.vis = 1; j.rx = 0; j.rz = 0; j.lean = 0; j.armL = j.armR = 0.05;
    if (!nz) {
      j.ry = Math.PI; j.x = 0;
      if (t < 8) { j.z = 3.7; }
      else if (t < JUMP) { j.z = lerp(3.7, 2.9, smooth(prog(t, 8, 9.2))); j.y = Math.abs(Math.sin(t * 7)) * 0.04 * (t < 9.2 ? 1 : 0); j.armL = j.armR = smooth(prog(t, 9.2, JUMP)) * 0.9; }
      else {
        const k = t - JUMP;
        j.z = lerp(2.9, 0, smooth(k / 0.5)); j.y = 0.6 * Math.sin(Math.min(k / 0.45, 1) * Math.PI / 2) - Math.max(0, k - 0.3) ** 2 * 9;
        j.armL = j.armR = 0.95; j.lean = Math.min(k, 1) * 0.6;
        if (j.y < -4) j.vis = 0;
      }
    } else {
      j.x = 0; j.z = 0; j.ry = Math.PI * 0.1;
      if (t < 32.4) { const k = prog(t, OUT - 0.3, 31.6); j.y = lerp(-4, 0.9, Math.sin(k * Math.PI / 2)); }
      else if (t < 34.9) j.y = 0.9 + Math.sin((t - 32.4) * 2) * 0.03;
      else j.y = 0.9 - (t - 34.9) ** 2 * 9;
      j.armL = j.armR = 0.55 + 0.35 * Math.sin(t * 9); j.rz = Math.sin(t * 3) * 0.1;
      if (j.y < -3.5) j.vis = 0;
    }
    jumper.update();
  },
};
