import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { pier, ferrisWheel, palm, lifeguardTower, fishSchool } from '../engine/props.js';

const GREEN = 5.6, ANT = 12.8, FULL = 20.0, TURN = 21.8, DIVE = 30.6, END = 41.5;
const CLIFF_Z = 70, CLIFF_H = 26;
let crowd, swimmers, wheel, fish, skyline;

function seaLevel(t) {
  return lerp(0, 7, smooth(prog(t, GREEN, GREEN + 4))) + lerp(0, 63, smooth(prog(t, ANT, FULL)));
}

export default {
  title: '¿Y si se derritiera todo el hielo de la Tierra?',
  duration: 46.5,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Nivel del mar', value: '+0 m', sub: 'Santa Mónica, California' },
    { t: 3.2, label: 'Hielo en la Tierra', value: count(0, 30e6, { dur: 2, pre: '≈ ', suf: ' km³' }), sub: 'Polos y glaciares' },
    { t: GREEN, label: 'Groenlandia', value: count(0, 7, { dur: 4, pre: '+', suf: ' m', curve: 'smooth' }), sub: 'Nivel del mar' },
    { t: 10.4, label: 'Miami', value: 'Bajo el agua', sub: 'Altitud media: unos 2 m' },
    { t: ANT, label: 'Antártida', value: '+58 m', sub: 'El 90 % del hielo del planeta', pulse: true },
    { t: 17.2, label: 'Nivel del mar', value: count(30, 70, { dur: 2.8, pre: '+', suf: ' m' }), sub: 'Todo el hielo, fuera' },
    { t: TURN, label: 'Ciudades bajo el mar', value: 'Cientos', sub: 'Venecia, Ámsterdam, Buenos Aires…' },
    { t: 26.4, label: 'Tiempo necesario', value: 'Miles de años', sub: 'No pasaría de golpe' },
    { t: 28.6, label: 'Subida desde 1880', value: count(0, 21, { dur: 1.2, pre: '+', suf: ' cm' }), sub: 'Y cada vez más rápido' },
    { t: DIVE + 0.2, label: 'Profundidad aquí', value: count(0, 65, { dur: 1.6, pre: '≈ ', suf: ' m' }), sub: 'Muelle de Santa Mónica' },
    { t: 37.4, label: 'Tu casa', value: '¿?', sub: 'Cuéntalo en comentarios', pulse: true },
  ],
  captions: [
    { t: 3.3, d: 2.2, text: 'Todo el hielo del planeta…\nse derrite.' },
    { t: GREEN + 0.1, d: 2.2, text: 'Primero, Groenlandia.' },
    { t: 8.0, d: 2.2, text: 'Adiós a la playa.' },
    { t: 10.4, d: 2.3, text: 'Solo con eso, Miami\nya estaría bajo el agua.' },
    { t: ANT + 0.1, d: 2.1, text: 'Ahora, la Antártida.' },
    { t: 15.0, d: 2.1, text: 'El muelle, bajo el agua.' },
    { t: 17.2, d: 2.2, text: 'La noria, también.' },
    { t: 19.5, d: 2.2, text: 'Y los acantilados.' },
    { t: TURN + 0.1, d: 2.3, text: 'Venecia, Ámsterdam,\nBuenos Aires… desaparecen.' },
    { t: 24.2, d: 2.1, text: 'Pero hay un detalle…' },
    { t: 26.4, d: 2.2, text: '…derretirlo todo tardaría\nmiles de años.' },
    { t: 28.6, d: 2.0, text: 'Aunque el mar ya ha subido\nmás de 20 cm desde 1880.' },
    { t: DIVE + 0.3, d: 2.2, text: 'Mientras tanto, aquí abajo…' },
    { t: 33.0, d: 2.2, text: '…la noria sería un arrecife.' },
    { t: 35.2, d: 2.1, text: 'Los peces, encantados.' },
    { t: 37.4, d: 3.9, text: 'Y tú…\n¿a qué altura vives?' },
  ],
  end: { t: END, fact: 'Si todo el hielo se derritiera, el mar subiría unos 70 metros' },
  fx(t) {
    const under = smooth(prog(t, DIVE - 0.15, DIVE + 0.25));
    return {
      underwater: under * 0.55,
      flash: t > DIVE - 0.15 && t < DIVE + 0.2 ? 0.35 : 0,
      vignette: under * 0.2,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: GREEN, mood: 'tense' }, { t: ANT, mood: 'epic' }, { t: TURN, mood: 'drone' }, { t: DIVE, mood: 'cold' }, { t: END - 0.3, mood: 'none' }],
    amb: [
      { type: 'waves', from: 0, to: DIVE, gain: 0.8 }, { type: 'crowd', from: 0, to: 11, gain: 0.45, fout: 2 }, { type: 'wind', from: 9, to: DIVE, gain: 0.35 },
      { type: 'underwater', from: DIVE - 0.2, to: END, gain: 0.8 },
    ],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: GREEN, type: 'rumble', d: 4 }, { t: 8, type: 'gasp' }, { t: 10.4, type: 'tick' },
      { t: ANT - 0.6, type: 'riser', d: 0.6 }, { t: ANT, type: 'boom', gain: 0.6 }, { t: ANT, type: 'rumble', d: 7 }, { t: 17.2, type: 'tick' }, { t: TURN, type: 'whoosh' },
      { t: 26.4, type: 'tick' }, { t: 28.6, type: 'tick' }, { t: DIVE - 0.2, type: 'splash' }, { t: 35.2, type: 'shimmer' }, { t: 37.4, type: 'ding', gain: 0.5 },
      { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0.03, 0.06);
    w.setSunDir(-0.5, 0.55, -0.4);
    w.groundMat.color.set(0xe3d2ae);
    w.setSky(0x3f86d1, 0xe6f0f2, 0xd9e7ee);
    w.setFog(80, 700);
    const baseH = w.groundH.bind(w);
    w.groundH = (z) => (z >= CLIFF_Z ? CLIFF_H : baseH(z));
    const r = rng(77);
    // bluffs (Palisades) with grass top and palms
    const cliff = new THREE.Mesh(new THREE.BoxGeometry(1600, CLIFF_H + 10, 400), new THREE.MeshStandardMaterial({ color: 0xc9a77c, roughness: 1, flatShading: true }));
    cliff.position.set(0, (CLIFF_H - 10) / 2, CLIFF_Z + 200); cliff.receiveShadow = cliff.castShadow = true; w.scene.add(cliff);
    const grass = new THREE.Mesh(new THREE.PlaneGeometry(1600, 400).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x6ea64f, roughness: 1 }));
    grass.position.set(0, CLIFF_H + 0.02, CLIFF_Z + 200); grass.receiveShadow = true; w.scene.add(grass);
    for (let i = 0; i < 40; i++) { const p = palm(9 + r() * 6, i + 1); p.position.set(-240 + i * 12 + r() * 3, CLIFF_H, CLIFF_Z + 4 + r() * 4); w.scene.add(p); }
    for (let i = 0; i < 18; i++) {
      const h = 8 + r() * 30, b = new THREE.Mesh(new THREE.BoxGeometry(14, h, 14), new THREE.MeshStandardMaterial({ color: [0xf4efe6, 0xe9dcc5, 0xd7e3e8, 0xf2d7c4][i % 4] }));
      b.position.set(-200 + i * 24 + r() * 6, CLIFF_H + h / 2, CLIFF_Z + 30 + r() * 30); b.castShadow = true; w.scene.add(b);
    }
    // far downtown skyline (visible once everything else is flooded)
    skyline = new THREE.Group();
    for (let i = 0; i < 26; i++) {
      const h = 120 + r() * 200, b = new THREE.Mesh(new THREE.BoxGeometry(18 + r() * 14, h, 18 + r() * 14), new THREE.MeshStandardMaterial({ color: 0x8aa0b4, roughness: 0.3, metalness: 0.4 }));
      b.position.set(-90 + r() * 180, h / 2 + CLIFF_H, 620 + r() * 160); skyline.add(b);
    }
    w.scene.add(skyline);
    // pier + wheel
    const pr = pier(300, 6.4); pr.position.set(-35, 0, CLIFF_Z); w.scene.add(pr);
    wheel = ferrisWheel(13); wheel.position.set(-30, 9.4, -95); w.scene.add(wheel);
    const lg = lifeguardTower(0x5fb7e5); lg.position.set(10, w.groundH(14), 14); lg.rotation.y = 0.2; w.scene.add(lg);
    const lg2 = lifeguardTower(0xf2d03b); lg2.position.set(48, w.groundH(18), 18); w.scene.add(lg2);
    crowd = new Crowd(w, 600, 64).scatter(-24, 60, 3, 62, Math.PI);
    for (const p of crowd.p) { if (p.x > 2 && p.x < 14 && p.z > 44) p.vis = 0; p.lying = p.rnd3 < 0.2; if (p.lying) p.bry = Math.PI / 2 + (p.rnd - 0.5); }
    swimmers = new Crowd(w, 70, 65).scatter(-20, 60, -26, -3, Math.PI);
    fish = fishSchool(w, 70, 4); fish.mesh.visible = false;
  },

  update(t, w) {
    const lvl = seaLevel(t);
    w.seaU.level.value = lvl;
    w.seaU.waveAmp.value = lerp(0.14, 0.45, smooth(prog(t, ANT, FULL)));
    w.seaU.foamAmt.value = 1 - 0.75 * smooth(prog(t, ANT, FULL));
    const under = t >= DIVE;
    wheel.userData.spin(t < GREEN + 3 ? t : GREEN + 3 + (t - GREEN - 3) * 0.2);

    // camera
    let cam;
    if (!under) cam = keys(t, [
      [0, [8, 5.0, 52, -26, 10, -70, 55]], [GREEN, [6, 5.2, 48, -26, 9, -70, 55]], [9.6, [12, 11, 70, -28, 9, -80, 55]],
      [ANT, [12, 12, 74, -28, 12, -80, 55]], [FULL, [25, 78, 140, -28, 40, -110, 56]], [TURN - 0.2, [25, 80, 140, -28, 40, -110, 56]],
      [TURN + 2.2, [10, 76, 120, 0, 140, 700, 52]], [28.4, [12, 76, 118, 0, 150, 700, 52]], [DIVE - 0.2, [-20, 73, 0, -30, 60, -95, 56]],
    ]);
    else cam = keys(t, [[DIVE, [-12, 34, -50, -30, 24, -95, 58]], [35.2, [-4, 26, -66, -30, 24, -95, 58]], [END, [-14, 20, -72, -30, 26, -95, 60]]]);
    if (!under) { cam[1] = Math.max(cam[1], lvl + 4); cam[4] = Math.max(cam[4], lvl + 2); }
    w.shake = t > ANT && t < FULL ? 0.06 : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // environment above / below water
    w.sky.visible = !under; w.clouds.visible = !under;
    w.scene.background = under ? new THREE.Color(0x0e4552) : null;
    if (under) {
      w.fog.color.set(0x0e4552); w.setFog(4, 80);
      w.light(0.45, 0.7); w.hemi.color.set(0x7fd6d6); w.sun.color.set(0x9fe8e0);
      fish.mesh.visible = true; fish.update(t, -30, 24, -93);
    } else {
      const storm = smooth(prog(t, ANT, FULL));
      w.skyU.top.value.copy(mixColor(0x3f86d1, 0x5b7896, storm));
      w.skyU.hor.value.copy(mixColor(0xe6f0f2, 0xc7d3da, storm));
      w.fog.color.copy(mixColor(0xd9e7ee, 0xc4d1d8, storm)); w.skyU.bot.value.copy(w.fog.color);
      w.setFog(80, t > TURN ? 1100 : 700);
      w.light(lerp(1, 0.75, storm)); w.hemi.color.set(0xdfefff); w.sun.color.set(0xfff3e0);
      fish.mesh.visible = false;
    }

    // beach crowd walks inland as the water comes
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.ry = p.bry + Math.sin(t * 0.4 + p.rnd * 10) * 0.12; p.armL = p.armR = 0.03;
      if (p.lying) { p.lean = -Math.PI / 2 + 0.02; p.y = 0.18; }
      const go = Math.max(0, t - GREEN - 0.3 - p.rnd * 1.2);
      if (go > 0) {
        if (p.lying) { p.lean = -0.1; p.y = 0; }
        p.ry = 0.0 + (p.rnd2 - 0.5) * 0.3; p.z = p.bz + go * (2.2 + p.rnd2 * 1.5);
        p.y = Math.abs(Math.sin(t * 9 + p.rnd * 20)) * 0.08; p.armL = p.armR = 0.15 + 0.1 * Math.sin(t * 9 + p.rnd);
        if (p.z > CLIFF_Z - 1) p.vis = 0; else p.vis = (p.bx > 2 && p.bx < 14 && p.bz > 44) ? 0 : 1;
      }
    }
    crowd.update();
    for (const p of swimmers.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry; p.lean = 0; p.armL = p.armR = 0.2 + 0.2 * Math.sin(t * 2 + p.rnd * 9);
      p.y = Math.sin(t * 1.5 + p.rnd * 9) * 0.05 + Math.max(0, lvl - 0.8 - w.groundH(p.bz)) * 0; // they bob; rising water swallows them
      const go = Math.max(0, t - GREEN - p.rnd);
      p.z = p.bz + go * 2.4; p.ry = lerp(p.bry, 0, clamp(go));
      p.vis = p.z > CLIFF_Z - 1 ? 0 : 1;
    }
    swimmers.update();
  },
};
