import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { palm, lifeguardTower, artDeco, shark, splash } from '../engine/props.js';

const FIN = 5.4, PANIC = 12.0, UNDER = 18.6, BREACH = 23.0, AWAY = 34.4, TEASE = 38.8, END = 41.6;
const SZ = -160; // shark lane (deep enough)
let crowd, swimmers, meg, spl1, spl2;

// shark state as a pure function of time
function sharkState(t) {
  const s = { x: 0, y: -2.0, z: SZ, yaw: -Math.PI / 2, pitch: 0, roll: 0, vis: true };
  if (t < FIN) { s.vis = false; return s; }
  if (t < BREACH) { s.x = -150 + (t - FIN) * 8.5; }            // cruising along the coast
  else if (t < AWAY) {
    const k = t - BREACH; s.x = -150 + (BREACH - FIN) * 8.5 + k * 8.5;
    if (k < 2.6) {                                                // breach (slow motion)
      const u = k / 2.6;
      s.y = -2.0 + Math.sin(u * Math.PI) * 9.5;
      s.pitch = lerp(0.9, -0.9, u);
      s.roll = Math.sin(u * Math.PI) * 0.4;
    }
  } else if (t < TEASE) {
    const k = t - AWAY; s.x = -150 + (AWAY - FIN) * 8.5 + k * 4; s.z = SZ - k * 14; s.yaw = -Math.PI / 2 + Math.min(1, k / 2) * 0.9;
  } else { s.x = 4 - (t - TEASE) * 3; s.z = -38; s.y = -2.2; s.yaw = Math.PI / 2; } // only the fin, close to shore
  return s;
}

export default {
  title: '¿Y si un megalodón apareciera en Miami Beach?',
  duration: 46.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'South Beach, Miami', value: count(0, 31, { dur: 1.5, suf: ' °C' }), sub: 'Agua perfecta para bañarse' },
    { t: FIN, label: 'Aleta dorsal', value: count(0, 1.6, { dur: 1.2, dec: 1, pre: '≈ ', suf: ' m' }), sub: 'Más alta que muchas personas' },
    { t: 7.6, label: 'Longitud', value: count(0, 16, { dur: 1.4, pre: '≈ ', suf: ' m' }), sub: 'Más largo que un autobús', pulse: true },
    { t: 9.8, label: 'Peso', value: count(0, 61, { dur: 1.3, pre: '≈ ', suf: ' t' }), sub: 'Como 10 elefantes' },
    { t: PANIC, label: 'Bañistas en el agua', value: (dt) => String(Math.max(0, Math.round(120 - dt * 40))), sub: 'Saliendo a toda prisa' },
    { t: 14.2, label: 'Velocidad de crucero', value: '≈ 5 km/h', sub: 'Como tú caminando' },
    { t: UNDER, label: 'Fuerza de mordida', value: count(0, 180000, { dur: 1.6, pre: 'hasta ', suf: ' N' }), sub: '10 veces la de un tiburón blanco', color: '#ffd1c4' },
    { t: BREACH, label: 'Dientes', value: '≈ 276', sub: 'Algunos de 18 cm', pulse: true },
    { t: 27.8, label: 'En la orilla', value: 'A salvo', sub: 'Es demasiado grande para entrar' },
    { t: 30.0, label: 'Móviles grabando', value: 'Todos', sub: 'Viral en 3… 2… 1…' },
    { t: AWAY, label: 'Extinguido hace', value: count(0, 3.6e6, { dur: 1.8, pre: '≈ ', suf: ' años' }), sub: 'Según los fósiles' },
    { t: TEASE, label: '???', value: '¿?', sub: '', pulse: true },
  ],
  captions: [
    { t: 3.2, d: 2.1, text: 'Un día normal en Miami Beach.' },
    { t: FIN + 0.1, d: 2.1, text: 'Hasta que aparece esto.' },
    { t: 7.6, d: 2.1, text: 'Un megalodón.\nDe 16 metros.' },
    { t: 9.8, d: 2.1, text: 'Pesa como 10 elefantes.' },
    { t: PANIC + 0.1, d: 2.1, text: 'Los socorristas\nlo ven primero.' },
    { t: 14.2, d: 2.1, text: 'Nada a unos 5 km/h…' },
    { t: 16.4, d: 2.1, text: '…tranquilo.\nComo si paseara.' },
    { t: UNDER + 0.1, d: 2.2, text: 'Su mordida: la más fuerte\nque se conoce.' },
    { t: 20.8, d: 2.1, text: '10 veces la de\nun tiburón blanco.' },
    { t: BREACH + 0.4, d: 2.1, text: '276 dientes.\nAlgunos de 18 cm.' },
    { t: 25.6, d: 2.1, text: 'Pero aquí viene lo bueno…' },
    { t: 27.8, d: 2.1, text: 'Con 16 metros, en la orilla\nse quedaría varado.' },
    { t: 30.0, d: 2.2, text: 'En la arena estás a salvo.' },
    { t: 32.2, d: 2.1, text: 'Y en minutos, sería el video\nmás visto del planeta.' },
    { t: AWAY + 0.1, d: 2.1, text: 'Por suerte, se extinguió…' },
    { t: 36.5, d: 2.2, text: '…hace unos 3,6 millones\nde años.' },
    { t: TEASE + 0.2, d: 2.6, text: '¿O no?' },
  ],
  end: { t: END, fact: 'Un diente de megalodón podía medir tanto como tu mano' },
  fx(t) {
    const under = t >= UNDER && t < BREACH ? smooth(prog(t, UNDER - 0.15, UNDER + 0.2)) : 0;
    return {
      underwater: under * 0.55,
      flash: (t > UNDER - 0.15 && t < UNDER + 0.15) || (t > BREACH - 0.1 && t < BREACH + 0.1) ? 0.3 : 0,
      vignette: smooth(prog(t, FIN, FIN + 1)) * 0.15 * (t < AWAY ? 1 : 0) + (t > TEASE ? 0.2 : 0),
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: FIN, mood: 'tense' }, { t: UNDER, mood: 'drone' }, { t: BREACH, mood: 'epic' }, { t: 27.8, mood: 'curious' }, { t: AWAY, mood: 'night' }, { t: TEASE, mood: 'tense' }, { t: END - 0.3, mood: 'none' }],
    amb: [
      { type: 'waves', from: 0, to: UNDER, gain: 0.75 }, { type: 'crowd', from: 0, to: UNDER, gain: 0.5 }, { type: 'underwater', from: UNDER - 0.1, to: BREACH, gain: 0.8 },
      { type: 'waves', from: BREACH, to: END, gain: 0.7 }, { type: 'crowd', from: BREACH + 0.5, to: END, gain: 0.45 },
    ],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: FIN - 0.3, type: 'sub_drop' }, { t: FIN + 0.5, type: 'heartbeat' }, { t: 6.6, type: 'heartbeat' }, { t: 7.6, type: 'boom', gain: 0.5 },
      { t: 9.8, type: 'tick' }, { t: PANIC, type: 'whistle' }, { t: PANIC + 0.3, type: 'scream' }, { t: 14.2, type: 'tick' }, { t: UNDER - 0.15, type: 'splash' },
      { t: 20.8, type: 'tick' }, { t: BREACH - 0.8, type: 'riser', d: 0.8 }, { t: BREACH + 0.15, type: 'splash' }, { t: BREACH + 0.3, type: 'scream' },
      { t: BREACH + 2.5, type: 'boom', gain: 0.8 }, { t: BREACH + 2.5, type: 'splash' }, { t: 27.8, type: 'tick' }, { t: 30, type: 'crowd_cheer' }, { t: AWAY, type: 'tick' },
      { t: TEASE - 0.3, type: 'sub_drop' }, { t: TEASE + 0.2, type: 'heartbeat' }, { t: TEASE + 1.2, type: 'heartbeat' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0.03, 0.02);
    w.setSunDir(0.35, 0.65, -0.55);
    w.groundMat.color.set(0xf1e2c2);
    w.setSky(0x2f8be0, 0xe3f3f8, 0xd8ecf3);
    w.setFog(90, 650);
    w.seaU.shallow.value.set(0x45e0cf); w.seaU.deep.value.set(0x168fae);
    const r = rng(88);
    // Ocean Drive: palms + art deco hotels
    for (let i = 0; i < 26; i++) { const p = palm(8 + r() * 5, 200 + i); p.position.set(-130 + i * 10 + r() * 4, w.groundH(88), 86 + r() * 6); w.scene.add(p); }
    let x = -140;
    while (x < 140) { const b = artDeco(Math.floor(r() * 1e5)); const bw = b.userData.w; b.position.set(x + bw / 2, w.groundH(108), 108); w.scene.add(b); x += bw + 1.5; }
    const road = new THREE.Mesh(new THREE.PlaneGeometry(400, 10).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x3a3b40 }));
    road.position.set(0, w.groundH(97) + 0.03, 97); w.scene.add(road);
    [[0xff9ec4, -36], [0xffd84d, 0], [0x5fd6c8, 36], [0xb79cff, 72]].forEach(([c, xx], i) => { const lg = lifeguardTower(c, i); lg.position.set(xx, w.groundH(14), 14); lg.rotation.y = Math.PI; w.scene.add(lg); });
    // umbrellas
    const um = [0xff5e5e, 0x2ec4c4, 0xffd23f, 0xff8ad8];
    for (let i = 0; i < 40; i++) {
      const g = new THREE.Group();
      const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 2.4), new THREE.MeshStandardMaterial({ color: 0xffffff })); pole.position.y = 1.2;
      const top = new THREE.Mesh(new THREE.ConeGeometry(1.4, 0.6, 8), new THREE.MeshStandardMaterial({ color: um[i % 4], flatShading: true })); top.position.y = 2.4;
      g.add(pole, top); g.traverse((o) => { o.castShadow = true; });
      const zz = 20 + r() * 50; g.position.set(-60 + r() * 120, w.groundH(zz), zz); w.scene.add(g);
    }
    crowd = new Crowd(w, 520, 91).scatter(-55, 55, 4, 70, Math.PI);
    for (const p of crowd.p) { if ((Math.abs(p.x - 18) < 3 && p.z > 24 && p.z < 42) || (Math.abs(p.x + 4) < 3.5 && p.z > 4 && p.z < 46)) p.vis = 0; p.lying = p.rnd3 < 0.22; if (p.lying) p.bry = Math.PI / 2 + (p.rnd - 0.5); }
    swimmers = new Crowd(w, 120, 92).scatter(-50, 50, -34, -3, 0);
    meg = shark(); w.scene.add(meg);
    spl1 = splash(w, 170, 3); spl2 = splash(w, 200, 4);
  },

  update(t, w) {
    const s = sharkState(t);
    meg.visible = s.vis;
    meg.position.set(s.x, s.y, s.z);
    meg.rotation.set(0, 0, 0); meg.rotateY(s.yaw); meg.rotateX(s.pitch); meg.rotateZ(s.roll);
    meg.userData.tail.rotation.y = Math.sin(t * 2.4) * 0.35;
    const bx = -150 + (BREACH - FIN) * 8.5;
    spl1.update(t - BREACH - 0.15, bx + 2, 0, SZ, 1.0);
    spl2.update(t - BREACH - 2.45, bx + 20, 0, SZ, 1.3);
    const under = t >= UNDER && t < BREACH;

    // camera
    let cam;
    const g = (z) => w.groundH(z);
    if (t < FIN) cam = keys(t, [[0, [0, 3.2, -42, 0, 7, 80, 52]], [3.1, [2, 3.4, -36, 0, 6, 80, 52]], [3.2, [18, g(32) + 1.9, 32, -10, 1.5, -150, 46]], [FIN, [18, g(32) + 1.9, 32, -40, 1.0, -150, 40]]], (k) => k);
    else if (t < PANIC) cam = keys(t, [[FIN, [18, g(32) + 1.9, 32, s.x, 0.6, SZ, 30]], [7.6, [18, g(32) + 2.6, 31, s.x, 0.6, SZ, 13]], [9.79, [18, g(32) + 2.6, 30, s.x, 0.6, SZ, 11]], [9.8, [14, 1.6, -30, s.x + 4, 0.4, SZ, 16]], [PANIC, [14, 1.6, -32, s.x + 6, 0.4, SZ, 14]]]);
    else if (t < UNDER) cam = keys(t, [[PANIC, [0, g(14) + 6.6, 15.5, -10, 0, -30, 52]], [14.2, [0, g(14) + 6.8, 16, -10, 0, -30, 52]], [14.21, [10, 1.4, -40, s.x, 0.2, SZ, 34]], [UNDER, [14, 1.4, -44, s.x, 0.2, SZ, 34]]]);
    else if (t < BREACH) cam = [s.x + 14, -2.4, SZ + 16, s.x - 2, -2.0, SZ, 60];
    else if (t < 27.8) cam = keys(t, [[BREACH, [bx + 6, 2.2, SZ + 70, bx + 4, 4, SZ, 38]], [BREACH + 2.6, [bx + 14, 2.4, SZ + 72, bx + 20, 2, SZ, 40]], [27.8, [bx + 18, 3, SZ + 80, bx + 24, 1, SZ, 42]]]);
    else if (t < TEASE) cam = keys(t, [[27.8, [-4, g(10) + 3.6, 10, 4, 1.2, -120, 50]], [30.0, [-4, g(26) + 3.8, 26, 4, 2.5, -60, 52]], [AWAY, [0, g(40) + 8, 40, 0, 0, -80, 52]], [TEASE, [0, g(40) + 9, 42, 0, 0, -90, 52]]]);
    else cam = keys(t, [[TEASE, [-2, 1.5, -8, 6, 0.2, -40, 42]], [END, [-2, 1.4, -10, 0, 0.2, -40, 38]]]);
    w.shake = (t > BREACH + 2.4 && t < BREACH + 3.4) ? 0.15 : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // underwater look
    w.sky.visible = w.clouds.visible = !under;
    w.scene.background = under ? new THREE.Color(0x0d6c80) : null;
    if (under) { w.fog.color.set(0x0d6c80); w.setFog(3, 48); w.light(0.6, 0.8); w.hemi.color.set(0x8ff0ea); }
    else { w.setSky(0x2f8be0, 0xe3f3f8, 0xd8ecf3); w.setFog(90, 650); w.light(1); w.hemi.color.set(0xdfefff); }

    // swimmers flee to the sand
    for (const p of swimmers.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry + Math.sin(t * 0.6 + p.rnd * 9) * 0.3; p.lean = 0; p.y = Math.sin(t * 1.4 + p.rnd * 9) * 0.06;
      p.armL = p.armR = 0.15 + 0.15 * Math.sin(t * 1.7 + p.rnd * 7);
      const run = Math.max(0, t - PANIC - p.rnd * 0.8);
      if (run > 0) { p.ry = Math.PI; p.z = p.bz + run * (3.2 + p.rnd2 * 1.5); p.armL = p.armR = 0.6 + 0.3 * Math.sin(t * 12 + p.rnd * 9); p.lean = 0.25; p.y = Math.abs(Math.sin(t * 10 + p.rnd * 9)) * 0.12; }
      if (p.z > 3) { p.z = Math.min(p.z, 3 + (p.z - 3) * 0.3 + p.rnd * 6); }
    }
    swimmers.update();
    // beach crowd: alarm, stand up, point & film
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.ry = p.bry + Math.sin(t * 0.4 + p.rnd * 10) * 0.15; p.armL = p.armR = 0.03;
      if (p.lying) { p.lean = -Math.PI / 2 + 0.02; p.y = 0.18; }
      const alarm = smooth(prog(t, PANIC + 0.5 + p.rnd * 1.5, PANIC + 1.2 + p.rnd * 1.5));
      if (alarm > 0) {
        if (p.lying) { p.lean = lerp(p.lean, 0, alarm); p.y = lerp(p.y, 0, alarm); }
        p.ry = lerp(p.ry, Math.PI + (p.rnd2 - 0.5) * 0.4, alarm);
        p.z = p.bz + alarm * 4 * (p.rnd2 > 0.6 ? 1 : 0);
        p.armR = lerp(0.03, p.rnd3 < 0.6 ? 0.88 : 0.3, smooth(prog(t, 27.8, 29)) * alarm);
      }
    }
    crowd.update();
  },
};
