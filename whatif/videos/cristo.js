import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { mountainMesh } from '../engine/props2.js';
import { cristo, bolt, rainbow, island } from '../engine/props3.js';

const STORM = 5.4, HIT1 = 9.8, HIT2 = 18.6, RODS = 23.0, PEOPLE = 25.2, CLEAR = 34.0, TRICK = 38.4, END = 43.6;
const BAY = -700;
let statue, crowd, bolts = [], rb, chip;
const hFn = (x, z) => {
  const r = Math.hypot(x, z);
  if (r < 27) return -2.2;
  let h = -2.2 - Math.pow(r - 27, 1.22) * 1.15 + Math.sin(x * 0.03) * 8 * clamp((r - 40) / 80) + Math.cos(z * 0.025 + 1) * 10 * clamp((r - 60) / 100);
  return Math.max(h, BAY - 5);
};
// strikes: [time, from, to, seed, branches, near]
const STRIKES = [
  [7.0, [-420, 380, -900], [-380, -300, -1100], 5, 4],
  [HIT1, [30, 420, -120], [0, 37.5, 0], 7, 3],
  [13.4, [380, 400, -700], [420, -250, -900], 11, 4],
  [HIT2, [-160, 420, -60], [-14.3, 31.8, 0], 13, 3],
  [27.0, [520, 380, 300], [560, -300, 420], 17, 4],
  [30.6, [-600, 420, -300], [-640, -400, -350], 19, 3],
];
const boltLevel = (t, t0) => { const d = t - t0; if (d < 0 || d > 0.55) return 0; if (d < 0.07) return 1; if (d < 0.12) return 0.15; if (d < 0.3) return 1 - (d - 0.12) * 1.5; return Math.max(0, 0.7 - (d - 0.3) * 2.8); };

export default {
  title: '¿Y si un rayo cayera sobre el Cristo Redentor?',
  duration: 48.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Cristo Redentor, Río', value: count(0, 30, { dur: 1.6, suf: ' m' }), sub: 'Sobre una montaña de 700 m' },
    { t: 3.2, label: 'Brazos abiertos', value: '28 m', sub: 'De punta a punta' },
    { t: STORM, label: 'Rayos en Brasil', value: count(0, 78e6, { dur: 2, pre: '≈ ' }), sub: 'Al año, uno de los países con más rayos' },
    { t: HIT1, label: 'Temperatura del rayo', value: count(0, 30000, { dur: 0.9, pre: '≈ ', suf: ' °C' }), sub: '5 veces la superficie del Sol', pulse: true, color: '#e6ecff' },
    { t: 14.2, label: 'Energía de un rayo', value: '≈ 1.000 MJ', sub: 'Lo que gasta una casa en un mes' },
    { t: HIT2, label: 'Rayos al año', value: '3 a 6', sub: 'Los que recibe el Cristo', pulse: true },
    { t: 20.8, label: 'Enero de 2014', value: 'Dedo dañado', sub: 'Lo restauraron' },
    { t: RODS, label: 'Pararrayos', value: 'Cabeza y brazos', sub: 'Llevan la descarga a tierra' },
    { t: PEOPLE, label: 'Sobreviven', value: '≈ 90 %', sub: 'De las personas alcanzadas por un rayo' },
    { t: 31.8, label: 'Distancia', value: 'hasta 16 km', sub: 'Puede caerte un rayo de lejos' },
    { t: CLEAR + 2.2, label: 'Inaugurado', value: '1931', sub: 'Una de las 7 maravillas modernas' },
    { t: TRICK, label: 'Truco', value: 'segundos ÷ 3', sub: '= kilómetros hasta el rayo' },
  ],
  captions: [
    { t: 3.3, d: 2.0, text: 'Brazos abiertos de 28 metros.' },
    { t: STORM, d: 2.1, text: 'Pero se acerca una tormenta.' },
    { t: 7.6, d: 2.1, text: 'Brasil recibe unos 78 millones\nde rayos al año.' },
    { t: HIT1 + 0.15, d: 1.9, text: '¡Zas!' },
    { t: 12.0, d: 2.1, text: '30.000 °C. Cinco veces la\nsuperficie del Sol.' },
    { t: 14.2, d: 2.1, text: 'Dura menos de un segundo…' },
    { t: 16.4, d: 2.1, text: '…pero lleva la energía que\ngasta una casa en un mes.' },
    { t: HIT2 + 0.15, d: 2.0, text: 'Al Cristo le caen entre\n3 y 6 rayos al año.' },
    { t: 20.8, d: 2.1, text: 'En 2014, uno le dañó\nun dedo de la mano.' },
    { t: RODS, d: 2.1, text: 'Por eso lleva pararrayos.' },
    { t: PEOPLE, d: 2.1, text: '¿Y si te cae a ti?' },
    { t: 27.4, d: 2.1, text: 'Unas 9 de cada 10 personas\nsobreviven.' },
    { t: 29.6, d: 2.1, text: 'Aléjate de lo más alto.\nNunca bajo un árbol.' },
    { t: 31.8, d: 2.1, text: 'Si oyes truenos, el rayo\npuede alcanzarte.' },
    { t: CLEAR, d: 2.1, text: 'Y cuando pasa la tormenta…' },
    { t: 36.2, d: 2.1, text: '…él sigue ahí.\nDesde 1931.' },
    { t: TRICK, d: 2.1, text: 'Truco: cuenta los segundos\nentre el rayo y el trueno…' },
    { t: 40.6, d: 2.9, text: '…divide entre 3, y sabrás\na cuántos km está.' },
  ],
  end: { t: END, fact: 'Al Cristo Redentor le caen entre 3 y 6 rayos al año' },
  fx(t) {
    let fl = 0; for (const [t0, , , , , ] of STRIKES) fl = Math.max(fl, boltLevel(t, t0));
    const near = Math.max(boltLevel(t, HIT1), boltLevel(t, HIT2));
    const storm = smooth(prog(t, STORM, STORM + 2)) * (1 - smooth(prog(t, CLEAR - 0.5, CLEAR + 1.5)));
    return { rain: storm, flash: near * 0.75 + (fl - near) * 0.18, vignette: storm * 0.2 };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: STORM, mood: 'tense' }, { t: HIT1 + 0.3, mood: 'drone' }, { t: PEOPLE, mood: 'tense' }, { t: CLEAR, mood: 'epic' }, { t: TRICK, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'wind', from: 0, to: END, gain: 0.3 }, { type: 'crowd', from: 0, to: STORM + 2, gain: 0.4 }, { type: 'storm', from: STORM, to: CLEAR + 1, gain: 0.55, fin: 2, fout: 1.5 }, { type: 'rain', from: STORM + 0.5, to: CLEAR + 0.5, gain: 0.6, fin: 2 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: STORM, type: 'rumble', d: 2.5 },
      ...STRIKES.map(([t0]) => ({ t: t0, type: t0 === HIT1 || t0 === HIT2 ? 'thunder_near' : 'thunder_far' })),
      { t: HIT1 + 0.1, type: 'gasp' }, { t: 14.2, type: 'tick' }, { t: 20.8, type: 'crack' }, { t: RODS, type: 'tick' }, { t: PEOPLE, type: 'heartbeat' },
      { t: 31.8, type: 'tick' }, { t: CLEAR, type: 'shimmer' }, { t: 36.2, type: 'shimmer' }, { t: TRICK, type: 'tick' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.ground.visible = false; w.groundH = () => 0;
    w.setSky(0x3f87d4, 0xe4eef3, 0xd6e5ee);
    w.setFog(500, 3200);
    w.setSunDir(0.5, 0.65, -0.4);
    const mtn = mountainMesh(hFn, 1500, 240, 8, 0x3e7a3a, 0x6b6255, 0.5); w.scene.add(mtn);
    const water = new THREE.Mesh(new THREE.PlaneGeometry(12000, 12000).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x2a6e8c, roughness: 0.25, metalness: 0.1 }));
    water.position.y = BAY; w.scene.add(water);
    // city along the bay, Sugarloaf and surrounding hills
    const r = rng(31);
    const city = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1).translate(0, 0.5, 0), new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.8 }), 900);
    const M = new THREE.Matrix4(), CC = [0xf4f1ea, 0xe8e2d4, 0xdfe7ec, 0xf2e3c8, 0xd9d9d9];
    let n = 0;
    while (n < 900) {
      const a = r() * Math.PI * 2, d = 760 + r() * 1100; const x = Math.cos(a) * d, z = Math.sin(a) * d;
      if (z < -1350 && Math.abs(x + 350) < 500) continue;
      const h = 12 + r() * 55, s = 14 + r() * 18;
      M.compose(new THREE.Vector3(x, BAY, z), new THREE.Quaternion().setFromEuler(new THREE.Euler(0, r() * 3, 0)), new THREE.Vector3(s, h, s)); city.setMatrixAt(n, M);
      city.setColorAt(n, new THREE.Color(CC[n % CC.length])); n++;
    }
    w.scene.add(city);
    const sugar = island(120, 270, 3, 0x7e7a6c, 0x4f7d45); sugar.position.set(-350, BAY, -1750); w.scene.add(sugar);
    const sugar2 = island(70, 120, 4, 0x7e7a6c, 0x4f7d45); sugar2.position.set(-520, BAY, -1620); w.scene.add(sugar2);
    for (let i = 0; i < 8; i++) { const hh = island(150 + r() * 150, 220 + r() * 220, 10 + i, 0x5d6b4a, 0x3e7a3a); const a = r() * Math.PI * 2, d = 1500 + r() * 900; hh.position.set(Math.cos(a) * d, BAY, Math.sin(a) * d * 0.6 + 600); w.scene.add(hh); }
    const beach = new THREE.Mesh(new THREE.TorusGeometry(900, 18, 3, 40, Math.PI * 0.6).rotateX(Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0xf1e3bf }));
    beach.position.set(400, BAY + 1, -900); beach.scale.set(1, 0.05, 1); w.scene.add(beach);
    statue = cristo(); statue.rotation.y = Math.PI; w.scene.add(statue);
    // lightning bolts
    for (const [, from, to, seed, br] of STRIKES) { const b = bolt(from, to, seed, br); w.scene.add(b); bolts.push(b); }
    rb = rainbow(1100); rb.position.set(150, BAY, -2300); w.scene.add(rb);
    chip = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.4, 0.4), new THREE.MeshStandardMaterial({ color: 0xdedfd6 })); chip.visible = false; w.scene.add(chip);
    // tourists on the terrace (some doing the pose)
    crowd = new Crowd(w, 170, 77);
    crowd.p.forEach((p) => {
      let x, z; do { x = (r() - 0.5) * 44; z = (r() - 0.5) * 44; } while (Math.hypot(x, z) < 8 || Math.hypot(x, z) > 21.5);
      p.bx = x; p.bz = z; p.pose = r() < 0.35; p.bry = p.pose ? Math.atan2(x, z) : Math.atan2(-x, -z);
    });
  },

  update(t, w) {
    // weather
    const storm = smooth(prog(t, STORM, STORM + 2.5)) * (1 - smooth(prog(t, CLEAR - 0.5, CLEAR + 2)));
    const sunset = smooth(prog(t, CLEAR + 2, END));
    let flash = 0; STRIKES.forEach(([t0], i) => { const k = boltLevel(t, t0); bolts[i].userData.set(k); flash = Math.max(flash, k * (t0 === HIT1 || t0 === HIT2 ? 1 : 0.4)); });
    w.skyU.top.value.copy(mixColor(mixColor(0x3f87d4, 0x37507f, sunset), 0x252c36, storm));
    w.skyU.hor.value.copy(mixColor(mixColor(0xe4eef3, 0xf6b37a, sunset), 0x5b6470, storm));
    w.fog.color.copy(mixColor(mixColor(0xd6e5ee, 0xe9b894, sunset), 0x4d5560, storm)); w.skyU.bot.value.copy(w.fog.color);
    w.setFog(lerp(500, 220, storm), lerp(3200, 1700, storm));
    w.light(lerp(1, 0.32, storm) + flash * 1.4, lerp(1, 0.55, storm) + flash * 1.2);
    w.hemi.color.copy(mixColor(0xdfefff, 0xb7c6e8, storm)); w.sun.color.copy(mixColor(0xfff3e0, 0xffb37a, sunset));
    w.skyU.sunGlow.value = 1 - storm;
    w.cloudMat.color.copy(mixColor(0xffffff, 0x3e4651, storm)); w.cloudMat.emissiveIntensity = lerp(0.35, 0.05, storm) + flash * 0.8;
    w.clouds.position.y = lerp(0, -70, storm);
    rb.userData.set(smooth(prog(t, CLEAR + 0.5, CLEAR + 2.5)) * (1 - smooth(prog(t, TRICK + 2, END))));
    // chipped finger falling
    const dt = t - (HIT2 + 0.05);
    chip.visible = dt > 0 && dt < 3.5;
    if (chip.visible) { chip.position.set(-14.6 - dt * 1.5, Math.max(-60, 31.4 + dt * 2 - 4.9 * dt * dt), -0.3 - dt * 0.6); chip.rotation.set(dt * 7, dt * 5, 0); }

    // camera
    let cam;
    if (t < 3.2) cam = keys(t, [[0, [-70, 52, 120, 0, 22, 0, 50]], [3.2, [-30, 46, 110, 0, 24, 0, 50]]], (k) => k);
    else if (t < STORM) cam = keys(t, [[3.2, [3, 1.8, 21, 0, 28, 0, 58]], [STORM, [-2, 1.8, 21, 0, 30, 0, 58]]]);
    else if (t < HIT1) cam = keys(t, [[STORM, [150, 30, 190, 0, 10, -250, 52]], [HIT1 - 0.01, [130, 26, 170, 0, 15, -200, 52]]]);
    else if (t < 14.2) cam = keys(t, [[HIT1, [-10, 2.2, -46, 0, 28, 0, 58]], [14.2, [-8, 2.0, -42, 0, 29, 0, 58]]]);
    else if (t < HIT2) cam = keys(t, [[14.2, [95, 32, -70, 0, 26, 0, 52]], [HIT2 - 0.01, [85, 30, -60, 0, 26, 0, 52]]]);
    else if (t < RODS) cam = keys(t, [[HIT2, [-40, 30, -42, -10, 30, 0, 52]], [RODS, [-44, 26, -46, -10, 26, 0, 52]]]);
    else if (t < PEOPLE) cam = keys(t, [[RODS, [-16, 42, -26, 0, 33, 0, 50]], [PEOPLE, [12, 43, -26, 0, 33, 0, 50]]]);
    else if (t < CLEAR) cam = keys(t, [[PEOPLE, [14, 2.0, -26, 0, 6, 0, 56]], [29.6, [-16, 2.4, -24, 0, 8, 0, 56]], [CLEAR, [-24, 6, -30, 0, 18, 0, 54]]]);
    else cam = keys(t, [[CLEAR, [-50, 32, 110, 0, 10, -400, 52]], [TRICK, [30, 30, 105, 0, 14, -400, 52]], [END, [70, 50, 120, 0, 22, 0, 50]]]);
    w.shake = Math.max(boltLevel(t, HIT1), boltLevel(t, HIT2)) * 0.25;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // tourists
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.ry = p.bry + Math.sin(t * 0.5 + p.rnd * 20) * 0.1;
      p.armL = p.armR = p.pose ? 0.5 : 0.03;
      const sc = smooth(prog(t, HIT1 + p.rnd * 0.5, HIT1 + 0.6 + p.rnd * 0.5)) * (1 - smooth(prog(t, CLEAR, CLEAR + 1)));
      if (sc > 0) { p.armL = p.armR = lerp(p.armL, 0.95, sc); p.lean = 0.35 * sc; p.y = -0.4 * sc; p.ry = lerp(p.ry, Math.atan2(p.bx, p.bz), sc); p.x = p.bx * (1 + 0.08 * sc); p.z = p.bz * (1 + 0.08 * sc); }
      const cheer = smooth(prog(t, CLEAR + 0.5, CLEAR + 1.5));
      if (cheer > 0) { p.armL = p.armR = lerp(p.armL, p.pose ? 0.5 : 0.85, cheer); p.y = Math.abs(Math.sin(t * 7 + p.rnd * 9)) * 0.15 * cheer * (1 - sunset); }
    }
    crowd.update();
  },
};
