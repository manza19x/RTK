import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';

const STOP = 15.4, WAVE = 26.6, POLE = 31.4, DAY = 35.8, END = 46.0;
let crowd, town, cars, palms, buildings, pole, researchers;
const bInfo = [], cInfo = [], pInfo = [];
const M = new THREE.Matrix4(), Q = new THREE.Quaternion(), V = new THREE.Vector3(), S = new THREE.Vector3(), E = new THREE.Euler();

const fly = (t, delay) => Math.max(0, t - STOP - delay);
// slow-motion time warp for the first seconds after the stop
const warp = (d) => (d < 2 ? d * 0.35 : 0.7 + (d - 2) * 1.0);

export default {
  title: '¿Y si la Tierra dejara de girar de golpe?',
  duration: 51,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Velocidad en el ecuador', value: count(0, 1670, { dur: 2.2, suf: ' km/h' }), sub: 'Ahora mismo, sin notarlo' },
    { t: 5.6, label: 'Avión comercial', value: '≈ 900 km/h', sub: 'Vas casi al doble' },
    { t: 7.8, label: 'Velocidad del sonido', value: '1.235 km/h', sub: 'También la superas' },
    { t: 12.4, label: 'La Tierra frena en', value: '3', pulse: true },
    { t: 13.4, label: 'La Tierra frena en', value: '2', pulse: true },
    { t: 14.4, label: 'La Tierra frena en', value: '1', pulse: true },
    { t: STOP, label: 'Rotación', value: '0 km/h', sub: 'Frenazo total', pulse: true, color: '#ffd1c4' },
    { t: 17.6, label: 'Tu velocidad', value: '1.670 km/h', sub: 'Hacia el este', pulse: true },
    { t: 19.8, label: 'Viento', value: count(0, 1670, { dur: 1.2, suf: ' km/h' }), sub: 'La atmósfera sigue girando' },
    { t: 22.0, label: 'Huracán más fuerte medido', value: '345 km/h', sub: 'Patricia, 2015' },
    { t: WAVE, label: 'Océanos', value: 'En marcha', sub: 'Siguen moviéndose' },
    { t: POLE, label: 'Velocidad en los polos', value: '0 km/h', sub: 'Aquí no pasa casi nada' },
    { t: DAY, label: 'Duración de un día', value: (dt) => (dt < 1.2 ? count(24, 8760, { dur: 1.2, suf: ' h' })(dt) : '1 año'), sub: '6 meses de luz, 6 de noche' },
    { t: 42.6, label: 'Probabilidad', value: '≈ 0 %', sub: 'Puedes dormir tranquilo' },
  ],
  captions: [
    { t: 3.2, d: 2.3, text: 'Ahora mismo, en el ecuador,\nvas a 1.670 km/h.' },
    { t: 5.6, d: 2.1, text: 'Casi el doble que un avión.' },
    { t: 7.8, d: 2.1, text: 'Más rápido que el sonido.' },
    { t: 10.0, d: 2.3, text: 'Y no lo notas… porque\ntodo se mueve contigo.' },
    { t: 12.4, d: 2.9, text: 'Ahora la Tierra se para.\nEn un segundo.' },
    { t: STOP + 0.3, d: 1.9, text: 'Pero tú no te paras.' },
    { t: 17.6, d: 2.1, text: 'Sales disparado hacia el este.' },
    { t: 19.8, d: 2.1, text: 'El aire tampoco frena.' },
    { t: 22.0, d: 2.3, text: 'El huracán más fuerte\njamás medido: 345 km/h.' },
    { t: 24.3, d: 2.2, text: 'Esto es casi 5 veces más.' },
    { t: WAVE + 0.1, d: 2.3, text: 'Los océanos tampoco frenan…' },
    { t: 28.9, d: 2.3, text: '…y saltan sobre la costa.' },
    { t: POLE + 0.2, d: 2.1, text: 'Excepto en los polos.' },
    { t: 33.5, d: 2.2, text: 'Allí la Tierra casi\nno se movía.' },
    { t: DAY, d: 2.3, text: 'Y desde ahora,\nun día dura un año.' },
    { t: 38.2, d: 2.1, text: '6 meses de luz…' },
    { t: 40.4, d: 2.1, text: '…y 6 meses de oscuridad.' },
    { t: 42.6, d: 3.1, text: 'Tranquilo: frenar la Tierra\nde golpe es imposible.' },
  ],
  end: { t: END, fact: 'La Tierra gira tan suave que no notamos sus 1.670 km/h' },
  fx(t) {
    const d = t - STOP;
    const chaos = t > STOP && t < WAVE + 4 ? 1 : 0;
    const under = smooth(prog(t, 29.9, 30.3)) * (1 - smooth(prog(t, POLE - 0.25, POLE)));
    const night = smooth(prog(t, 39.6, 41)) * (1 - smooth(prog(t, 42.4, 42.9)));
    return {
      wind: chaos * smooth(prog(t, STOP + 0.1, STOP + 0.8)) * (1 - under),
      dust: (t < WAVE ? 1 : 0.3) * chaos * (0.35 + 0.25 * Math.sin(t * 3)) * smooth(prog(t, STOP, STOP + 1)) * (1 - under),
      flash: d > 0 && d < 0.4 ? (1 - d / 0.4) * 0.35 : 0,
      underwater: under,
      snow: t > POLE ? 0.35 : 0, snowWind: 0.2,
      vignette: chaos * 0.15,
      dark: t > POLE - 0.2 && t < POLE ? 1 : 0,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: 12.4, mood: 'tense' }, { t: STOP, mood: 'epic' }, { t: POLE, mood: 'cold' }, { t: 40.4, mood: 'night' }, { t: END - 0.3, mood: 'none' }],
    amb: [
      { type: 'city', from: 0, to: STOP, gain: 0.5 }, { type: 'crowd', from: 0, to: STOP, gain: 0.35 }, { type: 'waves', from: 0, to: POLE, gain: 0.3 },
      { type: 'storm', from: STOP, to: 30, gain: 0.9, fin: 0.2 }, { type: 'underwater', from: 30, to: POLE, gain: 0.7 }, { type: 'wind', from: POLE, to: END, gain: 0.4 },
      { type: 'night', from: 40.4, to: END, gain: 0.25 },
    ],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 5.6, type: 'tick' }, { t: 7.8, type: 'tick' }, { t: 12.4, type: 'beep' }, { t: 13.4, type: 'beep' }, { t: 14.4, type: 'beep' },
      { t: STOP - 0.9, type: 'riser', d: 0.9 }, { t: STOP - 0.03, type: 'mute', d: 0.03 }, { t: STOP, type: 'boom' }, { t: STOP, type: 'sub_drop' }, { t: STOP + 0.2, type: 'crowd_cheer', gain: 0.6 },
      { t: 17.6, type: 'whoosh' }, { t: 19.8, type: 'whoosh' }, { t: 22, type: 'tick' }, { t: WAVE, type: 'rumble', d: 3.4 }, { t: 29.6, type: 'boom', gain: 0.7 }, { t: 29.9, type: 'splash' },
      { t: POLE, type: 'whoosh' }, { t: DAY, type: 'tick' }, { t: 38.2, type: 'shimmer' }, { t: 40.4, type: 'power_down', gain: 0.4 }, { t: 42.6, type: 'tick' },
      { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0.03, 0.01);
    w.setFog(60, 480);
    w.setSunDir(0.3, 0.75, 0.35);
    w.groundMat.color.set(0xb7a77f);
    const r = rng(12);
    town = new THREE.Group(); w.scene.add(town);
    // street
    const road = new THREE.Mesh(new THREE.PlaneGeometry(9, 70).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x4d4f55, roughness: 1 }));
    road.position.set(0, 0.32, 36); road.rotation.x = -Math.atan(0.01); road.receiveShadow = true; town.add(road);
    // buildings
    const BCOL = [0xf2d7a7, 0xe8a87c, 0xd9e4dd, 0xf6f0e3, 0x9fc5c9, 0xe58f7a, 0xf3c969, 0xb7d3a8];
    buildings = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1).translate(0, 0.5, 0), new THREE.MeshStandardMaterial({ roughness: 0.9 }), 40);
    buildings.castShadow = buildings.receiveShadow = true;
    for (let i = 0; i < 40; i++) {
      const side = i % 2 ? 1 : -1, row = Math.floor(i / 2);
      const wd = 5 + r() * 4, h = 4 + r() * 9, dp = 5 + r() * 2;
      const x = side * (8 + wd / 2 + r() * 2), z = 4 + row * 3.4 + r();
      bInfo.push({ x, z, wd, h, dp, rnd: r() });
      buildings.setColorAt(i, new THREE.Color(BCOL[Math.floor(r() * BCOL.length)]));
    }
    town.add(buildings);
    // cars
    const CC = [0xd23b3b, 0x2f6fd0, 0xf2f2f2, 0x2b2b2b, 0xf2c13b, 0x3fae6a];
    cars = new THREE.InstancedMesh(new THREE.BoxGeometry(1.8, 1.3, 4).translate(0, 0.65, 0), new THREE.MeshStandardMaterial({ roughness: 0.4, metalness: 0.2 }), 14);
    cars.castShadow = true;
    for (let i = 0; i < 14; i++) { cInfo.push({ x: (i % 2 ? 2.2 : -2.2), z: 5 + i * 4.5 + r() * 2, rnd: r() }); cars.setColorAt(i, new THREE.Color(CC[i % CC.length])); }
    town.add(cars);
    // palms
    palms = new THREE.Group(); town.add(palms);
    const trunkM = new THREE.MeshStandardMaterial({ color: 0x8b6b45 }), leafM = new THREE.MeshStandardMaterial({ color: 0x3e8e41, flatShading: true, side: THREE.DoubleSide });
    for (let i = 0; i < 12; i++) {
      const p = new THREE.Group();
      const tr = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.28, 6, 6).translate(0, 3, 0), trunkM); tr.castShadow = true; p.add(tr);
      for (let k = 0; k < 6; k++) {
        const lf = new THREE.Mesh(new THREE.ConeGeometry(0.5, 3.2, 4).rotateZ(Math.PI / 2).translate(1.5, 0, 0), leafM);
        lf.position.y = 6; lf.rotation.y = k / 6 * Math.PI * 2; lf.rotation.z = -0.35; lf.castShadow = true; p.add(lf);
      }
      const x = (i % 2 ? 1 : -1) * 6.2, z = 3 + i * 4;
      p.position.set(x, 0, z); pInfo.push({ x, z, rnd: r(), g: p }); palms.add(p);
    }
    crowd = new Crowd(w, 520, 17).scatter(-5.6, 5.6, 2, 62, Math.PI);
    for (const p of crowd.p) if (Math.abs(p.x) < 1.6 && p.z > 52) p.vis = 0;
    // pole set
    pole = new THREE.Group(); pole.visible = false; pole.position.z = 20; w.scene.add(pole);
    const hut = new THREE.Mesh(new THREE.BoxGeometry(6, 3, 4).translate(0, 1.5, 0), new THREE.MeshStandardMaterial({ color: 0xd6402f }));
    hut.position.set(-6, 0, -6); hut.castShadow = true; pole.add(hut);
    const roof = new THREE.Mesh(new THREE.BoxGeometry(6.4, 0.3, 4.4), new THREE.MeshStandardMaterial({ color: 0xeeeeee })); roof.position.set(-6, 3.1, -6); pole.add(roof);
    const mast = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 4), new THREE.MeshStandardMaterial({ color: 0xcccccc })); mast.position.set(3, 2, -4); pole.add(mast);
    const sphere = new THREE.Mesh(new THREE.SphereGeometry(0.35, 16, 8), new THREE.MeshStandardMaterial({ color: 0xdddddd, metalness: 0.8, roughness: 0.2 })); sphere.position.set(3, 4.2, -4); pole.add(sphere);
    researchers = new Crowd(w, 7, 44);
    researchers.p.forEach((p, i) => {
      researchers.torso.setColorAt(i, new THREE.Color(0xf2711c)); researchers.arms.setColorAt(i * 2, new THREE.Color(0xf2711c)); researchers.arms.setColorAt(i * 2 + 1, new THREE.Color(0xf2711c));
      researchers.legs.setColorAt(i, new THREE.Color(0x222831));
      p.bx = -2 + (i % 4) * 1.6 + r(); p.bz = 19 + Math.floor(i / 4) * 2 + r(); p.bry = Math.PI + (r() - 0.5);
      p.vis = 0;
    });
  },

  update(t, w) {
    const atPole = t >= POLE;
    const waveShot = t >= WAVE && t < POLE;
    // ---------- camera
    let cam;
    if (t < WAVE) cam = keys(t, [
      [0, [0.5, 2.6, 58, 0, 1.6, 20, 54]], [5.5, [1.2, 3.6, 60, 0, 1.6, 20, 54]], [12.2, [-1, 5, 66, 0, 2, 30, 54]],
      [STOP - 0.2, [-3, 4.4, 62, 2, 2, 34, 54]], [STOP + 5, [-6, 4.4, 60, 6, 2.5, 36, 56]], [WAVE, [-6, 5, 60, 8, 3, 36, 56]],
    ]);
    else if (!atPole) cam = keys(t, [[WAVE, [0, 7, 30, 0, 6, -60, 56]], [POLE, [0, 7, 30, 0, 9, -60, 56]]]);
    else cam = keys(t, [[POLE, [4, 2.2, 30, -1, 1.5, 16, 54]], [DAY, [7, 3, 34, -2, 2, 16, 54]], [END, [10, 6, 40, -2, 3, 14, 56]]]);
    const d = t - STOP;
    w.shake = d > 0 && t < WAVE ? 0.35 * Math.exp(-d * 0.5) + 0.06 : waveShot ? 0.05 + 0.25 * smooth(prog(t, 28, 29.9)) : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // ---------- sets
    town.visible = !atPole;
    crowd.p.forEach((p) => (p.vis = atPole ? 0 : p.vis));
    pole.visible = atPole;
    researchers.p.forEach((p) => (p.vis = atPole ? 1 : 0));
    if (!atPole) {
      const storm = smooth(prog(t, STOP, STOP + 1.5));
      w.skyU.top.value.copy(mixColor(0x3b7fcc, 0x5d6c7a, storm));
      w.skyU.hor.value.copy(mixColor(0xe3eef2, 0xb9aa8f, storm));
      w.fog.color.copy(mixColor(0xd8e6ec, 0xb8a98c, storm)); w.skyU.bot.value.copy(w.fog.color);
      if (waveShot) w.setFog(50, 520); else w.setFog(lerp(60, 15, storm), lerp(480, 230, storm));
      w.light(lerp(1, 0.6, storm)); w.groundMat.color.set(0xb7a77f);
      w.clouds.rotation.y = t * 0.004 + storm * fly(t, 0) * 0.08;
      w.seaU.iceAmt.value = 0; w.seaU.waveAmp.value = lerp(0.12, 0.6, storm);
      // tsunami
      w.seaU.tsuH.value = waveShot ? 26 : 0;
      w.seaU.tsuW.value = 40;
      w.seaU.tsuZ.value = lerp(-420, 60, prog(t, WAVE, 30.4));
    } else {
      w.seaU.tsuH.value = 0;
      // polar: low sun; day -> night time lapse
      const night = smooth(prog(t, 39.6, 41)) * (1 - smooth(prog(t, 42.4, 42.9)));
      w.skyU.top.value.copy(mixColor(0x6f9fd6, 0x040817, night));
      w.skyU.hor.value.copy(mixColor(0xf2f5f7, 0x101a30, night));
      w.fog.color.copy(mixColor(0xe8eef3, 0x0b1222, night)); w.skyU.bot.value.copy(w.fog.color);
      w.setFog(40, 300);
      const sa = prog(t, DAY, 39.6) * Math.PI * 0.8;
      w.setSunDir(Math.cos(sa + 2.6), lerp(0.25, 0.08, night), Math.sin(sa + 2.6));
      w.light(lerp(0.9, 0.06, night), lerp(1, 0.4, night));
      w.skyU.sunGlow.value = 1 - night; w.starMat.opacity = night;
      w.groundMat.color.set(0xeef2f6); w.seaU.iceAmt.value = 1; w.seaU.waveAmp.value = 0; w.seaU.foamAmt.value = 0;
      w.clouds.visible = false;
    }

    // ---------- buildings: shake, then tilt east
    for (let i = 0; i < bInfo.length; i++) {
      const b = bInfo[i];
      const k = smooth(prog(t, STOP + b.rnd * 0.8, STOP + 2.5 + b.rnd * 2));
      const tilt = -k * (0.15 + b.rnd * 0.5);
      E.set(0, 0, tilt + (d > 0 && t < WAVE ? Math.sin(t * 30 + i) * 0.01 : 0));
      Q.setFromEuler(E);
      const gy = w.groundH(b.z);
      M.compose(V.set(b.x, gy, b.z), Q, S.set(b.wd, b.h, b.dp));
      buildings.setMatrixAt(i, M);
    }
    buildings.instanceMatrix.needsUpdate = true;
    // ---------- cars fly east
    for (let i = 0; i < cInfo.length; i++) {
      const c = cInfo[i], f = warp(fly(t, c.rnd * 0.3));
      const x = c.x + f * (22 + c.rnd * 18), y = w.groundH(c.z) + (f > 0 ? 3 * f * (1 + c.rnd) : 0);
      E.set(f * (1 + c.rnd) * 1.5, 0, -f * 3 * (0.5 + c.rnd));
      Q.setFromEuler(E);
      M.compose(V.set(x, y, c.z), Q, S.set(1, 1, 1)); cars.setMatrixAt(i, M);
    }
    cars.instanceMatrix.needsUpdate = true;
    // ---------- palms bend then rip out
    for (const p of pInfo) {
      const f = warp(fly(t, p.rnd * 0.4));
      p.g.rotation.z = -Math.min(1.3, f * 2.5) - (f > 0.6 ? (f - 0.6) * 4 : 0);
      p.g.position.x = p.x + Math.max(0, f - 0.6) * 30; p.g.position.y = w.groundH(p.z) + Math.max(0, f - 0.6) * 6;
      if (t < STOP) p.g.rotation.z = Math.sin(t * 1.3 + p.rnd * 6) * 0.03;
    }
    // ---------- people
    for (const p of crowd.p) {
      if (atPole) break;
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.rx = 0; p.rz = 0; p.ry = p.bry + Math.sin(t * 0.5 + p.rnd * 10) * 0.1;
      p.armL = p.armR = 0.03;
      // walking idle along the street
      if (t < STOP) { p.z = p.bz + Math.sin(t * 0.3 + p.rnd * 10) * 0.6; p.y = Math.abs(Math.sin(t * 5 + p.rnd * 20)) * 0.03; }
      if (t >= 12.4 && t < STOP) { p.ry = lerp(p.ry, Math.PI, 0.5); }
      const f = warp(fly(t, p.rnd * 0.25));
      if (f > 0) {
        const v = 18 + p.rnd2 * 22;
        p.x = p.bx + f * v; p.y = f * (2 + p.rnd3 * 6) + f * f * 2;
        p.rz = -f * (2 + p.rnd * 5); p.rx = f * (p.rnd2 - 0.5) * 6;
        p.armL = 0.8 + Math.sin(t * 13 + p.rnd * 9) * 0.2; p.armR = 0.6 + Math.sin(t * 11 + p.rnd) * 0.3;
        if (p.x > 80) p.vis = 0; else if (!atPole) p.vis = 1;
      } else if (!atPole) p.vis = (Math.abs(p.bx) < 1.6 && p.bz > 52) ? 0 : 1;
    }
    crowd.update();
    for (const p of researchers.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry + Math.sin(t * 0.7 + p.rnd * 9) * 0.3; p.y = 0; p.lean = 0;
      p.armL = 0.05; p.armR = p.rnd > 0.5 ? 0.05 + 0.4 * smooth(prog(t, POLE + 0.6, POLE + 1.2)) : 0.05;
    }
    researchers.update();
  },
};
