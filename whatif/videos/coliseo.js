import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { colosseum, tierPoint, stonePine, trex } from '../engine/props2.js';

const IN = 3.2, ENTER = 5.4, ROAR = 12.2, LOOK = 18.8, RUN = 25.4, WALL = 29.8, BITE = 32.0, EXT = 34.2, END = 44.0;
let col, rex, crowd;

// T-Rex position/heading as pure functions of time
function rexState(t) {
  const s = { x: -46, z: 0, yaw: -Math.PI / 2, walk: 0, phase: 0, open: 0, look: 0, lift: 0, vis: t >= ENTER - 0.5 };
  const walkTo = (t0, t1, x0, z0, x1, z1) => { const k = prog(t, t0, t1); s.x = lerp(x0, x1, k); s.z = lerp(z0, z1, k); s.walk = k > 0 && k < 1 ? 1 : 0; };
  if (t < 10.4) walkTo(ENTER, 10.4, -46, 0, -8, 0);
  else if (t < LOOK + 0.8) { s.x = -8; s.z = 0; }
  else if (t < WALL) { walkTo(LOOK + 0.8, 25.6, -8, 0, -2, 16.5); s.yaw = lerp(-Math.PI / 2, -Math.PI * 0.98, smooth(prog(t, LOOK, LOOK + 1.6))); }
  else if (t < EXT + 0.4) { s.x = -2; s.z = 16.5; s.yaw = -Math.PI * 0.98; }
  else { walkTo(EXT + 0.4, END, -2, 16.5, 8, -6); s.yaw = lerp(-Math.PI * 0.98, -0.6, smooth(prog(t, EXT + 0.4, EXT + 2))); }
  s.phase = t * 3.2;
  // roar (cut short), looking around, biting
  if (t > ROAR && t < 14.4) s.open = smooth(prog(t, ROAR, ROAR + 0.5)) * (1 - smooth(prog(t, 14.1, 14.4))), s.lift = 0.35 * s.open;
  if (t > 15.2 && t < 18.0) s.open = 0.25 * smooth(prog(t, 15.2, 15.6)) * (1 - smooth(prog(t, 17.4, 18)));
  if (t > 10.4 && t < LOOK) s.look = Math.sin((t - 10.4) * 0.9) * 0.6 * (t > 14.4 ? 1 : 0.3);
  if (t > WALL && t < EXT) { s.lift = 0.45 * smooth(prog(t, WALL, WALL + 0.8)); s.open = t > BITE ? Math.abs(Math.sin((t - BITE) * 5)) * 0.9 : 0.2; }
  return s;
}

export default {
  title: '¿Y si un T-Rex apareciera en el Coliseo de Roma?',
  duration: 49,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Coliseo, Roma', value: count(0, 50000, { dur: 2.2 }), sub: 'Espectadores que cabían' },
    { t: ENTER, label: 'Altura', value: count(0, 4, { dur: 1.2, pre: '≈ ', suf: ' m' }), sub: 'Solo hasta la cadera' },
    { t: 7.8, label: 'Longitud', value: count(0, 12, { dur: 1.2, pre: '≈ ', suf: ' m' }), sub: 'Como un autobús', pulse: true },
    { t: 10.0, label: 'Peso', value: count(0, 8, { dur: 1.2, pre: '≈ ', suf: ' t' }), sub: 'Más que un elefante' },
    { t: 14.4, label: 'Sonido', value: 'Grave, boca cerrada', sub: 'Como algunas aves y cocodrilos' },
    { t: LOOK, label: 'Vista', value: 'Hasta 13×', sub: 'Mejor que la tuya, según un estudio' },
    { t: RUN, label: 'Velocidad máxima', value: '≈ 20 km/h', sub: 'Según estimaciones' },
    { t: WALL, label: 'Muro de la arena', value: '≈ 4 m', sub: 'Su cabeza llega al borde', pulse: true },
    { t: BITE, label: 'Mordida', value: count(0, 35000, { dur: 1.2, pre: '≈ ', suf: ' N' }), sub: 'La más fuerte de un animal terrestre', color: '#ffd1c4' },
    { t: EXT, label: 'Extinguido hace', value: count(0, 66e6, { dur: 1.8, suf: ' años' }), sub: 'Junto a casi todos los dinosaurios' },
    { t: 38.6, label: 'Estegosaurio', value: '≈ 150 M años', sub: 'T-Rex: hace unos 66 M años' },
  ],
  captions: [
    { t: IN + 0.1, d: 2.1, text: 'Un día de turismo en Roma.' },
    { t: ENTER + 0.1, d: 2.3, text: 'Hasta que algo entra por\nla puerta de los gladiadores.' },
    { t: 7.8, d: 2.1, text: 'Un Tyrannosaurus rex.\n12 metros.' },
    { t: 10.0, d: 2.1, text: 'Pesa más que un elefante.' },
    { t: ROAR, d: 2.1, text: 'Y ahora… el rugido.' },
    { t: 14.4, d: 2.1, text: 'Bueno… en realidad,\nprobablemente no rugía.' },
    { t: 16.6, d: 2.1, text: 'Hacía un retumbo grave,\ncon la boca cerrada.' },
    { t: LOOK, d: 2.1, text: '¿Quedarse quieto? No sirve.' },
    { t: 21.0, d: 2.1, text: 'Eso solo funciona\nen las películas.' },
    { t: 23.2, d: 2.1, text: 'Su vista podía ser hasta\n13 veces mejor que la tuya.' },
    { t: RUN, d: 2.1, text: 'Corre a unos 20 km/h…' },
    { t: 27.6, d: 2.1, text: '…más o menos como\ntú esprintando.' },
    { t: WALL, d: 2.1, text: 'El muro de la arena\nmide unos 4 metros.' },
    { t: BITE, d: 2.1, text: 'Y su mordida\npartía huesos.' },
    { t: EXT, d: 2.2, text: 'Por suerte, se extinguió\nhace 66 millones de años.' },
    { t: 36.4, d: 2.1, text: 'Y un dato loco:' },
    { t: 38.6, d: 2.2, text: 'El T-Rex vivió más cerca\nde nosotros…' },
    { t: 40.8, d: 3.1, text: '…que del Estegosaurio.' },
  ],
  end: { t: END, fact: 'El T-Rex vivió más cerca de nuestra época que de la del Estegosaurio' },
  fx(t) {
    return {
      dust: (t > ENTER && t < 10.4 ? 0.25 : 0) + (t > RUN && t < WALL ? 0.2 : 0),
      vignette: t > ROAR && t < 14.4 ? 0.25 : t > WALL && t < EXT ? 0.2 : 0,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: ENTER, mood: 'tense' }, { t: 14.4, mood: 'curious' }, { t: LOOK, mood: 'tense' }, { t: EXT, mood: 'drone' }, { t: 36.4, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'crowd', from: 0, to: END, gain: 0.5 }, { type: 'wind', from: 0, to: END, gain: 0.2 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: IN, type: 'whoosh' }, { t: ENTER - 0.4, type: 'rumble', d: 1.5 },
      ...[...Array(8).keys()].map((i) => ({ t: ENTER + 0.3 + i * 0.62, type: 'thud', gain: 0.5 + i * 0.05 })),
      { t: ENTER + 0.6, type: 'gasp' }, { t: 7.8, type: 'tick' }, { t: 10, type: 'tick' }, { t: ROAR + 0.3, type: 'roar', d: 1.8 }, { t: 14.2, type: 'scratch' },
      { t: 16.4, type: 'rumble', d: 2 }, { t: LOOK, type: 'heartbeat' }, { t: 20, type: 'heartbeat' }, { t: RUN, type: 'scream' },
      ...[...Array(8).keys()].map((i) => ({ t: LOOK + 1 + i * 0.8, type: 'thud', gain: 0.6 })),
      { t: WALL, type: 'heartbeat' }, { t: BITE, type: 'chomp' }, { t: BITE + 0.6, type: 'chomp' }, { t: BITE + 1.2, type: 'chomp' }, { t: EXT, type: 'whoosh' },
      { t: 36.4, type: 'tick' }, { t: 40.8, type: 'shimmer' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false;
    w.groundH = () => 0;
    w.groundMat.color.set(0xb8ab92);
    w.setSky(0x3d82cf, 0xe8eef0, 0xdfe6ea);
    w.setFog(120, 900);
    w.setSunDir(0.45, 0.7, 0.3);
    col = colosseum(w);
    const r = rng(9);
    for (let i = 0; i < 26; i++) { const p = stonePine(); const a = r() * Math.PI * 2, d = 130 + r() * 120; p.position.set(Math.cos(a) * d, 0, Math.sin(a) * d * 0.9); p.scale.setScalar(0.9 + r() * 0.4); w.scene.add(p); }
    // tourists on the stands (mostly on the lower tiers, near-side and far-side)
    crowd = new Crowd(w, 1100, 19);
    const tiers = col.userData.tiers;
    crowd.p.forEach((p) => {
      const tier = tiers[[2, 2, 2, 4, 4, 6][Math.floor(r() * 6)]];
      let th; do { th = r() * Math.PI * 2; } while (Math.abs(Math.sin(th) - 1) < 0.06 && tier === tiers[2] && r() < 0.9);
      const s = 0.05 + r() * 0.9;
      const [x, y, z] = tierPoint(tier, s, th);
      p.bx = x; p.bz = z; p.by = y; p.bth = th; p.tier = tier; p.bs = s;
      p.bry = Math.atan2(x, z) + Math.PI + (r() - 0.5) * 0.5;
      p.sit = r() < 0.35;
    });
    // keep a gap where the camera sits on the near stands
    crowd.p.forEach((p) => { if (Math.abs(p.bx - 4) < 7 && p.bz > 34 && p.by < 24) p.vis = 0; });
    rex = trex(); rex.scale.set(0.8, 0.95, 0.72); w.scene.add(rex);
  },

  update(t, w) {
    const s = rexState(t);
    rex.visible = s.vis;
    rex.position.set(s.x, 0.15 + (s.walk ? Math.abs(Math.sin(s.phase)) * 0.12 : 0), s.z);
    rex.rotation.y = s.yaw;
    rex.userData.pose(s.phase, s.walk, s.open, s.look, s.lift);

    const head = new THREE.Vector3(); rex.userData.head.getWorldPosition(head);
    let cam;
    if (t < IN) cam = keys(t, [[0, [210, 120, 250, 0, 5, 0, 46]], [IN, [180, 100, 215, 0, 5, 0, 46]]], (k) => k);
    else cam = keys(t, [
      [IN, [2, 16, 40, 0, 3, 0, 55]], [ENTER, [2, 16, 40, -20, 3, 0, 52]], [7.7, [-6, 15.5, 39, s.x, 4, 0, 48]],
      [7.8, [-30, 1.4, 14, s.x, 4.5, s.z, 50]], [10.0, [-26, 1.2, 12, -8, 5, 0, 52]], [ROAR, [-18, 1.4, 9, head.x, head.y, head.z, 50]],
      [14.4, [-17, 1.5, 9, -8, 5.5, 0, 50]], [LOOK, [2, 16, 40, -8, 4, 2, 52]], [RUN, [3, 16, 40, s.x, 4, s.z, 52]],
      [WALL, [8, 17, 38, -2, 4.5, 16, 58]], [EXT, [9, 17, 38, -2, 5, 16, 58]], [36.4, [25, 36, 42, 0, 3, 0, 52]], [END, [30, 46, 48, 0, 4, 0, 50]],
    ]);
    w.shake = (s.walk && t < 26 ? 0.05 : 0) + (t > BITE && t < EXT ? 0.08 : 0);
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // tourists
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry + Math.sin(t * 0.5 + p.rnd * 20) * 0.12; p.lean = 0; p.armL = p.armR = 0.03;
      p.y = p.sit ? p.by - 0.6 : p.by;
      const shock = smooth(prog(t, ENTER + 0.4 + p.rnd * 1.5, ENTER + 1 + p.rnd * 1.5));
      if (shock > 0) { p.y = p.by; p.lean = -0.15 * shock; p.armR = lerp(0.03, p.rnd3 < 0.55 ? 0.88 : 0.2, shock); p.ry = lerp(p.ry, Math.atan2(p.bx - s.x, p.bz - s.z) + Math.PI, shock * 0.8); }
      const run = Math.max(0, t - RUN - p.rnd * 0.8);
      if (run > 0 && p.by < 24) {
        const dir = p.bx > 0 ? -1 : 1, th = p.bth + dir * run * 0.03;
        const [x, y, z] = tierPoint(p.tier, Math.min(0.95, p.bs + run * 0.05), th);
        p.x = x; p.z = z; p.y = y + Math.abs(Math.sin(t * 10 + p.rnd * 9)) * 0.1;
        p.ry = Math.atan2(-z * dir, x * dir) + Math.PI / 2; p.armL = p.armR = 0.4 + 0.3 * Math.sin(t * 11 + p.rnd * 7);
      }
    }
    crowd.update();
  },
};
