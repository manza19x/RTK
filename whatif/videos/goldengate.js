import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { goldenGate, island } from '../engine/props3.js';

const FAULT = 3.2, TENSION = 9.8, QUAKE = 12.0, Y1906 = 18.6, Y1989 = 23.0, DROP = 25.2, PROB = 29.6, RETRO = 31.8, LA = 34.0, END = 43.6;
const VP = [1250, -380]; // viewpoint hill (Marin side)
let bridge, crowd, cars, smoke, cInfo = [];
const vpH = (x, z) => 95 * Math.sqrt(Math.max(0, 1 - ((x - VP[0]) ** 2) / 260 ** 2 - ((z - VP[1]) ** 2) / 182 ** 2));
const quakeAmp = (t) => smooth(prog(t, QUAKE, QUAKE + 0.6)) * (1 - smooth(prog(t, Y1906 - 1.5, Y1906)));
function deckOffset(x, amp, t) {
  const ax = Math.abs(x), TX = 640;
  const f = ax < TX ? Math.cos((x / TX) * Math.PI / 2) : Math.max(0, Math.sin(((ax - TX) / 350) * Math.PI)) * 0.3;
  return [amp * f * Math.sin(t * 2.2), amp * 0.35 * f * Math.sin(t * 3.1 + 1)];
}

export default {
  title: '¿Y si llegara el gran terremoto a San Francisco?',
  duration: 48.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Golden Gate, San Francisco', value: count(0, 227, { dur: 1.6, suf: ' m' }), sub: 'Altura de sus torres' },
    { t: FAULT, label: 'Falla de San Andrés', value: '≈ 1.200 km', sub: 'Dos placas que se rozan' },
    { t: 5.4, label: 'Movimiento', value: '≈ 5 cm/año', sub: 'Como crecen tus uñas' },
    { t: QUAKE, label: 'Magnitud', value: count(0, 7.9, { dur: 1, dec: 1 }), sub: 'Como el de 1906', pulse: true, color: '#ffd1c4' },
    { t: 14.2, label: 'Balanceo del puente', value: 'hasta ≈ 8 m', sub: 'Está diseñado para eso' },
    { t: Y1906, label: '1906', value: '80 %', sub: 'De la ciudad destruida (terremoto + incendios)' },
    { t: Y1989, label: '1989', value: 'M 6,9', sub: 'Loma Prieta' },
    { t: DROP, label: 'Qué hacer', value: 'Agáchate · Cúbrete · Agárrate', sub: 'Drop, cover, hold on' },
    { t: PROB, label: 'Probabilidad', value: '72 %', sub: 'De uno de 6,7+ en la bahía antes de 2043' },
    { t: RETRO, label: 'Refuerzo sísmico', value: 'Desde 1997', sub: 'Para aguantar el gran terremoto' },
    { t: 36.2, label: 'Los Ángeles', value: '≈ 5 cm/año', sub: 'Acercándose a San Francisco' },
  ],
  captions: [
    { t: FAULT + 0.1, d: 2.1, text: 'Bajo San Francisco pasa\nla falla de San Andrés.' },
    { t: 5.4, d: 2.1, text: 'Dos placas que se mueven\nunos centímetros al año.' },
    { t: 7.6, d: 2.1, text: 'Más o menos lo que\ncrecen tus uñas.' },
    { t: TENSION, d: 2.1, text: 'Pero la tensión\nse va acumulando…' },
    { t: QUAKE + 0.1, d: 2.0, text: '…y se libera de golpe.' },
    { t: 14.2, d: 2.1, text: 'El puente se balancea…' },
    { t: 16.4, d: 2.1, text: '…está diseñado para moverse\nhasta unos 8 metros.' },
    { t: Y1906, d: 2.2, text: 'En 1906, un terremoto así\ny los incendios…' },
    { t: 20.8, d: 2.1, text: '…destruyeron el 80 %\nde la ciudad.' },
    { t: Y1989, d: 2.1, text: 'En 1989, otro de 6,9 hundió\nun tramo del Bay Bridge.' },
    { t: DROP, d: 2.1, text: '¿Y si te pilla uno?' },
    { t: 27.4, d: 2.1, text: 'Agáchate, cúbrete\ny agárrate.' },
    { t: PROB, d: 2.1, text: 'Hay un 72 % de probabilidad\nde uno grande antes de 2043.' },
    { t: RETRO, d: 2.1, text: 'Por eso el Golden Gate\nse ha reforzado.' },
    { t: LA, d: 2.1, text: 'Y un dato loco:' },
    { t: 36.2, d: 2.1, text: 'Los Ángeles se acerca a\nSan Francisco cada año.' },
    { t: 38.4, d: 2.1, text: 'Unos 5 centímetros.' },
    { t: 40.6, d: 2.9, text: 'En millones de años,\nserán vecinas.' },
  ],
  end: { t: END, fact: 'El Golden Gate puede balancearse varios metros para aguantar terremotos y viento' },
  fx(t) {
    const q = quakeAmp(t);
    const sepia = t >= Y1906 && t < DROP ? smooth(prog(t, Y1906, Y1906 + 0.4)) * (1 - smooth(prog(t, DROP - 0.3, DROP)) ) : 0;
    return {
      dust: q * 0.35,
      tint: sepia > 0 ? (t < Y1989 ? `rgba(130,95,45,${0.38 * sepia})` : `rgba(60,80,110,${0.3 * sepia})`) : null,
      vignette: q * 0.2 + sepia * 0.25,
      flash: t > QUAKE - 0.05 && t < QUAKE + 0.15 ? 0.25 : 0,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: TENSION, mood: 'tense' }, { t: QUAKE, mood: 'epic' }, { t: Y1906, mood: 'drone' }, { t: DROP, mood: 'tense' }, { t: LA, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'waves', from: 0, to: END, gain: 0.45 }, { type: 'wind', from: 0, to: END, gain: 0.35 }, { type: 'city', from: 0, to: QUAKE, gain: 0.35 }, { type: 'storm', from: QUAKE, to: Y1906, gain: 0.5, fin: 0.3 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: FAULT, type: 'tick' }, { t: 5.4, type: 'tick' }, { t: TENSION, type: 'heartbeat' }, { t: 10.9, type: 'heartbeat' }, { t: QUAKE - 0.9, type: 'riser', d: 0.9 },
      { t: QUAKE, type: 'boom' }, { t: QUAKE, type: 'rumble', d: 6 }, { t: QUAKE + 0.3, type: 'scream' }, { t: 14.2, type: 'crack' }, { t: 16.4, type: 'rumble', d: 2 },
      { t: Y1906, type: 'whoosh' }, { t: Y1989, type: 'whoosh' }, { t: DROP, type: 'tick' }, { t: PROB, type: 'tick' }, { t: RETRO, type: 'tick' }, { t: LA, type: 'tick' },
      { t: 40.6, type: 'shimmer' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.ground.visible = false; w.groundH = () => 0;
    w.seaU.seabed.value = 120; w.seaU.deep.value.set(0x1d4f6b); w.seaU.shallow.value.set(0x2d6f86); w.seaU.waveAmp.value = 0.35; w.seaU.foamAmt.value = 0.2;
    w.sea.position.z = 0; w.sea.scale.set(4, 1, 4);
    w.setSky(0x3d7fc8, 0xe8eff3, 0xd9e4ea);
    w.setFog(700, 5200);
    w.setSunDir(-0.55, 0.5, 0.45);
    bridge = goldenGate(); w.scene.add(bridge);
    // headlands & hills
    const r = rng(41);
    const vp = island(260, 95, 2, 0x8b7d5c, 0x7d9a52); vp.position.set(VP[0], 0, VP[1]); vp.scale.z = 0.7; w.scene.add(vp);
    for (let i = 0; i < 9; i++) { const h = island(300 + r() * 250, 140 + r() * 160, 10 + i, 0x8b7d5c, 0x6f8f4a); h.position.set(1500 + r() * 900, 0, -1200 + i * 280 + r() * 100); w.scene.add(h); }
    const sfHill = island(1400, 70, 5, 0x9c937f, 0x93a07a); sfHill.position.set(-2400, 0, 1300); w.scene.add(sfHill);
    const presidio = island(500, 60, 6, 0x8b7d5c, 0x5f8a45); presidio.position.set(-1500, 0, 450); w.scene.add(presidio);
    const alca = island(80, 22, 7, 0x8a8170, 0x7c8a5a); alca.position.set(-300, 0, 1900); w.scene.add(alca);
    const prison = new THREE.Mesh(new THREE.BoxGeometry(70, 14, 22).translate(0, 7, 0), new THREE.MeshStandardMaterial({ color: 0xe9e2d0 })); prison.position.set(-300, 18, 1900); w.scene.add(prison);
    const light = new THREE.Mesh(new THREE.CylinderGeometry(2, 2.5, 28).translate(0, 14, 0), new THREE.MeshStandardMaterial({ color: 0xffffff })); light.position.set(-340, 18, 1890); w.scene.add(light);
    // city blocks + downtown
    const city = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1).translate(0, 0.5, 0), new THREE.MeshStandardMaterial({ roughness: 0.8 }), 1400);
    const M = new THREE.Matrix4(), CC = [0xf4efe6, 0xe9dcc5, 0xd7e3e8, 0xf2d7c4, 0xdfe6d8, 0xc9d3dc];
    for (let i = 0; i < 1400; i++) {
      const down = i > 1150;
      const x = down ? -2350 + (r() - 0.5) * 500 : -3600 + r() * 2300, z = down ? 2050 + (r() - 0.5) * 500 : 700 + r() * 2200;
      const h = down ? 80 + r() * 200 : 10 + r() * 25, s = down ? 30 + r() * 25 : 18 + r() * 14;
      const base = Math.max(0, 70 * Math.sqrt(Math.max(0, 1 - ((x + 2400) ** 2) / 1400 ** 2 - ((z - 1300) ** 2) / 980 ** 2)) - 6);
      M.compose(new THREE.Vector3(x, base, z), new THREE.Quaternion(), new THREE.Vector3(s, h, s)); city.setMatrixAt(i, M); city.setColorAt(i, new THREE.Color(down ? [0xb8c4cf, 0x9fb0bf, 0xe6e6e6][i % 3] : CC[i % CC.length]));
    }
    city.castShadow = true; w.scene.add(city);
    const spire = new THREE.Mesh(new THREE.ConeGeometry(28, 260, 4).translate(0, 130, 0), new THREE.MeshStandardMaterial({ color: 0xf2f2f2 })); spire.position.set(-2250, 0, 2000); w.scene.add(spire);
    // smoke columns for the 1906 flashback
    smoke = new THREE.InstancedMesh(new THREE.SphereGeometry(1, 8, 6), new THREE.MeshStandardMaterial({ color: 0x5a524c, transparent: true, opacity: 0.55, roughness: 1, depthWrite: false }), 220);
    smoke.frustumCulled = false; w.scene.add(smoke); this.sm = [...Array(220)].map(() => ({ x: -3300 + r() * 2000, z: 800 + r() * 1800, ph: r(), s: 14 + r() * 22 }));
    // cars on the deck
    cars = new THREE.InstancedMesh(new THREE.BoxGeometry(4.5, 1.6, 2).translate(0, 0.8, 0), new THREE.MeshStandardMaterial({ roughness: 0.4 }), 80);
    const CAR = [0xd23b3b, 0x2f6fd0, 0xf2f2f2, 0x2b2b2b, 0xf2c13b, 0x9aa0a6];
    for (let i = 0; i < 80; i++) { cInfo.push({ x: -1300 + r() * 2600, lane: [-9, -5.5, -2, 2, 5.5, 9][i % 6], v: 18 + r() * 6 }); cars.setColorAt(i, new THREE.Color(CAR[i % 6])); }
    cars.castShadow = true; w.scene.add(cars);
    // tourists at the viewpoint
    crowd = new Crowd(w, 90, 55);
    crowd.p.forEach((p) => { const a = r() * Math.PI * 2, d = 4 + r() * 20; p.bx = VP[0] - 30 + Math.cos(a) * d; p.bz = VP[1] + 30 + Math.sin(a) * d * 0.6; p.bry = Math.atan2(-(-1), -(1)) + (r() - 0.5) * 0.8; p.sit = r() < 0.15; });
    w.clouds.position.y = -90; w.clouds.scale.set(3, 0.6, 3);
  },

  update(t, w) {
    const q = quakeAmp(t);
    bridge.userData.sway(q * 6, t);
    bridge.position.set(Math.sin(t * 37) * q * 0.4, 0, Math.sin(t * 29) * q * 0.4);
    w.seaU.waveAmp.value = 0.35 + q * 1.4;
    // cars: drive, brake during the quake
    const moveT = Math.min(t, QUAKE) + Math.max(0, t - Y1906 - 2) * 0.8;
    for (let i = 0; i < cInfo.length; i++) {
      const c = cInfo[i], dir = c.lane < 0 ? 1 : -1;
      let x = c.x + dir * c.v * moveT; x = ((x + 1370) % 2740 + 2740) % 2740 - 1370;
      const [dz, dy] = deckOffset(x, q * 6, t);
      const M = new THREE.Matrix4().compose(new THREE.Vector3(x + bridge.position.x, 67 + 3.7 + dy, c.lane + dz + bridge.position.z), new THREE.Quaternion().setFromEuler(new THREE.Euler(0, dir > 0 ? 0 : Math.PI, Math.sin(t * 20 + i) * 0.04 * q)), new THREE.Vector3(1, 1, 1));
      cars.setMatrixAt(i, M);
    }
    cars.instanceMatrix.needsUpdate = true;
    // smoke (1906 flashback)
    const sk = t >= Y1906 && t < DROP ? 1 : 0;
    const M = new THREE.Matrix4();
    this.sm.forEach((s, i) => {
      const k = ((t * 0.25 + s.ph) % 1);
      M.compose(new THREE.Vector3(s.x + k * 120, 40 + k * 420, s.z), new THREE.Quaternion(), new THREE.Vector3().setScalar(sk * s.s * (0.6 + k)));
      smoke.setMatrixAt(i, M);
    });
    smoke.instanceMatrix.needsUpdate = true;
    // sky: fog rolls in at the end, sunset
    const late = smooth(prog(t, LA, END));
    w.skyU.top.value.copy(mixColor(0x3d7fc8, 0x3a5a98, late)); w.skyU.hor.value.copy(mixColor(0xe8eff3, 0xf5b98a, late));
    w.fog.color.copy(mixColor(0xd9e4ea, 0xe9c3a6, late)); w.skyU.bot.value.copy(w.fog.color);
    w.sun.color.copy(mixColor(0xfff3e0, 0xffb07a, late));

    // camera
    const vpY = vpH(VP[0] - 30, VP[1] + 30);
    let cam;
    if (t < FAULT) cam = keys(t, [[0, [-160, 140, 470, -600, 120, 0, 54]], [FAULT, [-220, 120, 430, -620, 120, 0, 54]]], (k) => k);
    else if (t < TENSION) cam = keys(t, [[FAULT, [VP[0] - 10, vpY + 22, VP[1] - 10, -400, 30, 420, 52]], [TENSION, [VP[0] - 20, vpY + 22, VP[1] + 5, -400, 30, 420, 50]]]);
    else if (t < QUAKE) cam = keys(t, [[TENSION, [-480, 8, 130, -640, 150, 0, 54]], [QUAKE, [-500, 9, 140, -640, 160, 0, 54]]]);
    else if (t < 14.2) cam = keys(t, [[QUAKE, [-260, 75, 22, 600, 72, 0, 56]], [14.2, [-240, 75, 22, 600, 72, 0, 56]]]);
    else if (t < Y1906) cam = keys(t, [[14.2, [-250, 95, 300, -520, 100, 0, 54]], [Y1906, [-230, 100, 320, -520, 100, 0, 54]]]);
    else if (t < DROP) cam = keys(t, [[Y1906, [-1300, 260, 300, -2300, 60, 1500, 50]], [DROP, [-1200, 240, 260, -2300, 60, 1500, 50]]]);
    else if (t < PROB) cam = keys(t, [[DROP, [VP[0] - 20, vpY + 3, VP[1] + 12, VP[0] - 30, vpY, VP[1] + 30, 56]], [PROB, [VP[0] - 16, vpY + 3, VP[1] + 14, VP[0] - 30, vpY, VP[1] + 30, 56]]]);
    else if (t < RETRO) cam = keys(t, [[PROB, [600, 650, 1500, -300, 0, 300, 48]], [RETRO, [400, 620, 1500, -300, 0, 300, 48]]]);
    else if (t < LA) cam = keys(t, [[RETRO, [-600, 120, 90, -640, 180, 0, 50]], [LA, [-690, 130, 95, -640, 190, 0, 50]]]);
    else cam = keys(t, [[LA, [VP[0] - 10, vpY + 24, VP[1] - 10, -300, 40, 400, 50]], [END, [VP[0] + 60, vpY + 60, VP[1] - 120, -300, 40, 400, 48]]]);
    w.shake = q * (t < 14.2 ? 1.2 : 2.5);
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // tourists: watch, crouch during quake / demonstration
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = vpH(p.bx, p.bz); p.ry = p.bry + Math.sin(t * 0.5 + p.rnd * 20) * 0.15; p.lean = 0; p.armL = p.armR = 0.03; p.rz = 0;
      if (p.sit) p.y -= 0.6;
      const photo = smooth(prog(t, FAULT + p.rnd, FAULT + 1 + p.rnd)) * (1 - smooth(prog(t, QUAKE, QUAKE + 0.5)));
      if (p.rnd2 < 0.4) p.armR = lerp(0.03, 0.88, photo);
      const duck = Math.max(q, smooth(prog(t, 27.4 + p.rnd * 0.5, 28 + p.rnd * 0.5)) * (1 - smooth(prog(t, PROB, PROB + 0.5))));
      if (duck > 0) { p.y -= 0.75 * duck; p.lean = 0.6 * duck; p.armL = p.armR = lerp(p.armL, 0.95, duck); p.rz = Math.sin(t * 30 + p.rnd * 9) * 0.05 * q; }
    }
    crowd.update();
  },
};
