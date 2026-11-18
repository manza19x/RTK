import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count, clock } from '../engine/hud.js';
import { mountainMesh, prayerFlags } from '../engine/props2.js';

const APPEAR = 3.2, CLOCK = 9.8, FAINT = 18.6, SAVE = 20.8, WIDE = 23.0, QUEUE = 29.6, END = 42.0;
let you, climbers, flags;
const hFn = (x, z) => {
  const ax = Math.abs(x), az = Math.abs(z);
  let h = -(Math.pow(ax, 1.12) * 0.95 + az * 0.24 + Math.max(0, -z) * 0.5);
  h += Math.sin(z * 0.07) * 1.2 * clamp(az / 30) + Math.sin(x * 0.11 + z * 0.05) * 2.5 * clamp((ax + az) / 60);
  h -= Math.max(0, Math.hypot(x, z) - 160) * 0.7;
  return Math.max(h, -420);
};

export default {
  title: '¿Y si te teletransportaras a la cima del Everest?',
  duration: 47,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Altitud', value: count(0, 8849, { dur: 2.4, suf: ' m' }), sub: 'El punto más alto de la Tierra' },
    { t: 5.4, label: 'Temperatura', value: count(15, -30, { dur: 1.4, pre: '≈ ', suf: ' °C' }), sub: 'Con viento, mucho peor', color: '#cfe8ff' },
    { t: 7.6, label: 'Oxígeno', value: '33 %', sub: 'Del que hay a nivel del mar', pulse: true },
    { t: CLOCK, label: 'Consciencia útil', value: (dt) => clock(120, 0, FAINT - CLOCK)(dt), sub: 'Sin aclimatar: uno o dos minutos', color: '#ffd1c4' },
    { t: SAVE, label: 'Oxígeno embotellado', value: 'A salvo', sub: 'Y ahora, a bajar cuanto antes' },
    { t: WIDE, label: 'Expedición normal', value: '≈ 2 meses', sub: 'Para aclimatarse' },
    { t: 27.4, label: 'Zona de la muerte', value: '> 8.000 m', sub: 'El cuerpo no se recupera' },
    { t: QUEUE, label: 'Atasco de 2019', value: 'Horas de cola', sub: 'En la zona de la muerte' },
    { t: 31.8, label: 'Cumbres desde 1953', value: count(0, 10000, { dur: 1.6, pre: '> ' }), sub: 'Hillary y Tenzing fueron los primeros' },
    { t: 34.0, label: 'El Everest', value: 'Sigue creciendo', sub: 'Unos milímetros al año' },
  ],
  captions: [
    { t: APPEAR + 0.1, d: 2.1, text: 'Te teletransportas.\nEn camiseta.' },
    { t: 5.4, d: 2.1, text: 'Hace unos −30 °C.' },
    { t: 7.6, d: 2.1, text: 'Y cada respiración trae\nun tercio del oxígeno.' },
    { t: CLOCK, d: 2.1, text: 'Tienes uno o dos minutos\nde pensar con claridad.' },
    { t: 12.0, d: 2.1, text: 'Primero: mareo\ny dolor de cabeza.' },
    { t: 14.2, d: 2.1, text: 'Luego: confusión. Torpeza.' },
    { t: 16.4, d: 2.1, text: 'Tus labios se vuelven azules.' },
    { t: FAINT, d: 2.1, text: 'Y te desmayas.' },
    { t: SAVE + 0.2, d: 2.0, text: 'Por suerte, un sherpa\nte pone oxígeno.' },
    { t: WIDE, d: 2.2, text: 'Por eso los escaladores tardan\nunos dos meses en subir.' },
    { t: 25.2, d: 2.1, text: 'Subiendo y bajando\npara acostumbrarse.' },
    { t: 27.4, d: 2.1, text: 'Por encima de 8.000 m,\nel cuerpo no se recupera.' },
    { t: QUEUE, d: 2.1, text: 'Y a veces… hay cola\npara llegar a la cima.' },
    { t: 31.8, d: 2.1, text: 'Desde 1953, más de\n10.000 cumbres.' },
    { t: 34.0, d: 2.1, text: 'Y el Everest\nsigue creciendo.' },
    { t: 36.2, d: 2.1, text: 'La India sigue empujando\ncontra Asia.' },
    { t: 38.4, d: 3.4, text: '¿Tú subirías?' },
  ],
  end: { t: END, fact: 'En la cima del Everest hay un tercio del oxígeno que a nivel del mar' },
  fx(t) {
    const faint = smooth(prog(t, FAINT, FAINT + 1.2)) * (1 - smooth(prog(t, SAVE - 0.3, SAVE + 0.3)));
    const hyp = smooth(prog(t, 12, FAINT)) * (1 - smooth(prog(t, SAVE - 0.3, SAVE + 0.3)));
    return {
      snow: 0.55, snowWind: 2.2,
      wind: 0.25,
      flash: t > APPEAR - 0.05 && t < APPEAR + 0.35 ? 0.9 * (1 - (t - APPEAR) / 0.4) : 0,
      dark: faint * 0.92,
      vignette: hyp * 0.55,
      tint: hyp > 0 ? `rgba(40,70,140,${0.18 * hyp})` : null,
      frost: 0.25,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'epic' }, { t: APPEAR, mood: 'cold' }, { t: CLOCK, mood: 'tense' }, { t: SAVE, mood: 'cold' }, { t: WIDE, mood: 'epic' }, { t: 38.4, mood: 'cold' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'storm', from: 0, to: END, gain: 0.45 }, { type: 'wind', from: 0, to: END, gain: 0.4 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: APPEAR - 0.6, type: 'riser', d: 0.6 }, { t: APPEAR, type: 'shimmer' }, { t: APPEAR, type: 'pop' }, { t: 5.4, type: 'tick' }, { t: 7.6, type: 'tick' },
      ...[...Array(9).keys()].map((i) => ({ t: CLOCK + i * 1.0, type: 'heartbeat', gain: 0.5 + i * 0.06 })), { t: FAINT, type: 'sub_drop' }, { t: FAINT + 0.2, type: 'mute', d: 1.8 },
      { t: SAVE, type: 'shimmer' }, { t: WIDE, type: 'whoosh' }, { t: 27.4, type: 'tick' }, { t: QUEUE, type: 'tick' }, { t: 31.8, type: 'tick' }, { t: 34, type: 'rumble', d: 2 },
      { t: 38.4, type: 'ding', gain: 0.5 }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.ground.visible = false;
    w.groundH = () => 0;
    w.setSky(0x0b2f78, 0xd6e8f6, 0xe4eef6);
    w.setFog(250, 1600);
    w.setSunDir(0.55, 0.45, 0.25);
    const near = mountainMesh(hFn, 520, 260, 3); w.scene.add(near);
    const far = mountainMesh((x, z) => hFn(x, z) - 2.5, 3200, 150, 4); w.scene.add(far);
    // neighbouring peaks
    const r = rng(6);
    for (let i = 0; i < 14; i++) {
      const a = r() * Math.PI * 2, d = 900 + r() * 900, ph = -120 - r() * 250;
      const pk = mountainMesh((x, z) => Math.max(-600, -(Math.hypot(x, z * 1.3) * 0.9) + Math.sin(x * 0.05) * 6), 900, 60, 10 + i);
      pk.position.set(Math.cos(a) * d, ph, Math.sin(a) * d); pk.rotation.y = r() * 6; w.scene.add(pk);
    }
    // sea of clouds below
    const cf = new THREE.Mesh(new THREE.PlaneGeometry(8000, 8000).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 1, emissive: 0x8899aa, emissiveIntensity: 0.4 }));
    cf.position.y = -330; w.scene.add(cf);
    w.clouds.position.y = -430; w.clouds.scale.set(1.6, 1, 1.6);
    // summit marker + prayer flags
    const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 1.6), new THREE.MeshStandardMaterial({ color: 0x777777 })); pole.position.set(-0.6, 0.8, -0.8); w.scene.add(pole);
    flags = new THREE.Group();
    for (const [x, z] of [[-9, -6], [7, -9], [-11, 1], [10, -2]]) flags.add(prayerFlags([-0.6, 1.5, -0.8], [x, hFn(x, z) + 0.6, z], 22, 0.9));
    w.scene.add(flags);
    // climbers queue on the ridge (down-suits)
    climbers = new Crowd(w, 70, 41);
    const SUITS = [0xf2711c, 0xd62828, 0xffc300, 0x2b6cd6, 0x2bb673, 0xe9e9e9, 0x111111];
    climbers.p.forEach((p, i) => {
      const c = new THREE.Color(SUITS[i % SUITS.length]);
      climbers.torso.setColorAt(i, c); climbers.arms.setColorAt(i * 2, c); climbers.arms.setColorAt(i * 2 + 1, c);
      climbers.legs.setColorAt(i, new THREE.Color(i % 3 ? 0x1d1d22 : SUITS[(i + 2) % SUITS.length]));
      climbers.hair.setColorAt(i, new THREE.Color([0x111111, 0xd62828, 0x2b6cd6, 0xffc300][i % 4]));
      p.bz = 7 + i * 2.2 + r() * 0.6; p.bx = Math.sin(p.bz * 0.05) * 0.8 + (r() - 0.5) * 0.4; p.s = 1.05;
    });
    you = new Crowd(w, 1, 2);
    you.torso.setColorAt(0, new THREE.Color(0xf4f4f4)); you.arms.setColorAt(0, new THREE.Color(0xe0ac82)); you.arms.setColorAt(1, new THREE.Color(0xe0ac82));
    you.legs.setColorAt(0, new THREE.Color(0x3b6fd0)); you.head.setColorAt(0, new THREE.Color(0xe0ac82));
  },

  update(t, w) {
    flags.children.forEach((f) => f.userData.flutter(t));
    // you
    const y = you.p[0];
    y.x = 0.4; y.z = 0.6; y.ry = 0.25; y.armL = y.armR = 0.05; y.lean = 0; y.rz = 0; y.rx = 0;
    y.vis = t >= APPEAR ? 1 : 0;
    y.y = hFn(y.x, y.z) + 0.05;
    if (t > CLOCK) {
      const wob = smooth(prog(t, 12, FAINT));
      y.rz = Math.sin(t * 2.3) * 0.12 * wob; y.lean = Math.sin(t * 1.7) * 0.1 * wob;
      y.armL = 0.05 + 0.4 * wob * (Math.sin(t * 1.3) * 0.5 + 0.5);
    }
    if (t > FAINT) {
      const k = smooth(prog(t, FAINT, FAINT + 0.8));
      y.lean = lerp(y.lean, -Math.PI / 2 + 0.05, k); y.y += lerp(0, 0.18, k); y.rz = lerp(y.rz, 0, k);
    }
    if (t > SAVE + 1.4) {
      const k = smooth(prog(t, SAVE + 1.4, SAVE + 2.6));
      y.lean = lerp(-Math.PI / 2 + 0.05, 0.25, k); y.y = hFn(y.x, y.z) + lerp(0.23, -0.6, k);
    }
    you.update();
    // climbers: slowly shuffling up the ridge, one comes to help
    for (const [i, p] of climbers.p.entries()) {
      p.x = p.bx; p.z = p.bz - Math.max(0, Math.sin(t * 0.4 + i)) * 0.3; p.ry = Math.PI + Math.sin(t * 0.5 + i) * 0.1;
      p.lean = 0.3; p.armL = 0.15; p.armR = 0.15 + 0.1 * Math.sin(t + i); p.rz = 0;
      if (i === 0) {
        const k = smooth(prog(t, FAINT + 0.6, SAVE + 0.8));
        p.x = lerp(p.bx, 1.4, k); p.z = lerp(p.bz, 1.4, k); p.ry = lerp(Math.PI, Math.PI + 0.8, k);
        p.lean = lerp(0.3, 0.6, smooth(prog(t, SAVE + 0.6, SAVE + 1.2)) * (1 - smooth(prog(t, WIDE, WIDE + 0.5))));
        p.armR = lerp(0.15, 0.6, smooth(prog(t, SAVE, SAVE + 0.8)));
      }
      p.y = hFn(p.x, p.z);
    }
    climbers.update();

    // camera
    let cam;
    if (t < APPEAR) cam = keys(t, [[0, [62, 16, 70, 0, -8, 12, 50]], [APPEAR, [52, 12, 58, 0, -4, 6, 50]]]);
    else if (t < CLOCK) cam = keys(t, [[APPEAR, [3.2, 1.0, 6.5, 0.4, 1.0, 0.4, 46]], [CLOCK, [2.6, 1.2, 5.6, 0.4, 1.1, 0.5, 44]]]);
    else if (t < SAVE) cam = keys(t, [[CLOCK, [2.6, 1.2, 5.6, 0.4, 1.1, 0.5, 44]], [FAINT, [1.6, 1.3, 3.6, 0.4, 1.0, 0.5, 40]], [SAVE, [2.2, 2.4, 3.2, 0.4, 0.2, 0.5, 46]]]);
    else if (t < WIDE) cam = keys(t, [[SAVE, [5, 1.0, 7.5, 0.8, 0.4, 0.8, 46]], [WIDE, [5.5, 1.4, 8, 0.8, 0.5, 0.8, 46]]]);
    else if (t < QUEUE) cam = keys(t, [[WIDE, [130, 45, 170, 0, -25, 40, 50]], [QUEUE, [-90, 40, 190, 0, -25, 40, 50]]]);
    else if (t < 31.8) cam = keys(t, [[QUEUE, [7, -27, 182, 0, -12, 70, 28]], [31.8, [6, -26, 178, 0, -10, 60, 28]]]);
    else cam = keys(t, [[31.8, [-110, 70, 140, 0, -30, 20, 50]], [38.4, [-40, 90, 220, 0, -40, 30, 50]], [END, [20, 160, 300, 0, -60, 0, 50]]]);
    w.roll = t > CLOCK && t < SAVE ? Math.sin(t * 1.3) * 0.06 * smooth(prog(t, 12, FAINT)) : 0;
    w.shake = 0.03;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);
  },
};
