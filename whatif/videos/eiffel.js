import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { eiffelTower, haussmann } from '../engine/props2.js';

const HEAT = 5.4, MELT = 9.8, POOL = 14.2, CUBE = 20.8, BACK = 23.0, AIR = 25.2, SUMMER = 31.8, END = 43.6;
let tower, pool, cube, airCyl, crowd;

const melt = (t) => (t < MELT ? 0 : t < BACK ? smooth(prog(t, MELT, POOL)) : 1 - smooth(prog(t, BACK, BACK + 1.6)));
const heat = (t) => (t < HEAT ? 0 : t < POOL + 4 ? smooth(prog(t, HEAT, MELT)) * (1 - smooth(prog(t, POOL + 1, POOL + 5))) : 0);
const HEAT_COL = [0x000000, 0x5a0a00, 0xd23a00, 0xff8a1a, 0xffe08a];
function heatColor(k) {
  const i = Math.min(3, Math.floor(k * 4)), f = k * 4 - i;
  return new THREE.Color(HEAT_COL[i]).lerp(new THREE.Color(HEAT_COL[i + 1]), f);
}

export default {
  title: '¿Y si fundieras la Torre Eiffel?',
  duration: 48.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Torre Eiffel, París', value: count(0, 330, { dur: 2, suf: ' m' }), sub: 'De altura' },
    { t: 3.2, label: 'Hierro', value: count(0, 7300, { dur: 1.6, suf: ' t' }), sub: '18.000 piezas, 2,5 millones de remaches' },
    { t: HEAT, label: 'Temperatura', value: count(20, 1538, { dur: MELT - HEAT, suf: ' °C', curve: 'smooth' }), sub: 'Punto de fusión del hierro', color: '#ffc9a0' },
    { t: MELT, label: 'Altura', value: count(330, 0, { dur: POOL - MELT, suf: ' m', curve: 'smooth' }), sub: 'Bajando…' },
    { t: 16.4, label: 'Charco', value: '≈ 6 cm', sub: 'Sobre 125 × 125 m', pulse: true },
    { t: CUBE, label: 'En un cubo', value: '≈ 10 m', sub: 'De lado' },
    { t: BACK, label: 'Torre Eiffel', value: 'Restaurada', sub: 'Aquí no ha pasado nada' },
    { t: 27.4, label: 'Aire a su alrededor', value: '> 7.300 t', sub: 'En un cilindro de su tamaño' },
    { t: SUMMER, label: 'En verano', value: '+15 cm', sub: 'El calor dilata el hierro' },
    { t: 34.0, label: 'Debía durar', value: '20 años', sub: 'Para la Exposición de 1889' },
    { t: 36.2, label: 'La salvó', value: 'La radio', sub: 'Servía de antena' },
    { t: 38.4, label: 'Pintura', value: '≈ 60 t', sub: 'Cada 7 años' },
  ],
  captions: [
    { t: 3.3, d: 2.0, text: '330 metros de hierro.' },
    { t: HEAT, d: 2.1, text: 'Ahora, imagina\nque la calentamos…' },
    { t: 7.6, d: 2.1, text: '…hasta 1.538 °C.' },
    { t: MELT, d: 2.1, text: 'Y se derrite.' },
    { t: 12.0, d: 2.1, text: 'Los turistas,\nalgo preocupados.' },
    { t: POOL, d: 2.1, text: '¿Cuánto metal queda?' },
    { t: 16.4, d: 2.1, text: 'Repartido sobre su base…' },
    { t: 18.6, d: 2.1, text: '…un charco de solo\n6 centímetros.' },
    { t: CUBE, d: 2.1, text: 'O un cubo de unos\n10 metros de lado.' },
    { t: BACK, d: 2.1, text: 'Mejor la dejamos\ncomo estaba.' },
    { t: AIR, d: 2.1, text: 'Y un dato loco:' },
    { t: 27.4, d: 2.1, text: 'El aire que la rodea…' },
    { t: 29.6, d: 2.1, text: '…pesa más que\nla propia torre.' },
    { t: SUMMER, d: 2.1, text: 'En verano, crece\nhasta 15 cm.' },
    { t: 34.0, d: 2.1, text: 'Y debía desmontarse\na los 20 años.' },
    { t: 36.2, d: 2.1, text: 'La salvó ser una\ngran antena de radio.' },
    { t: 38.4, d: 2.1, text: 'Hoy se pinta cada 7 años.\nCon unas 60 toneladas.' },
    { t: 40.6, d: 2.9, text: '¿La has visitado?' },
  ],
  end: { t: END, fact: 'Si fundieras la Torre Eiffel, quedaría un charco de unos 6 cm sobre su base' },
  fx(t) {
    const h = heat(t);
    return {
      tint: h > 0 ? `rgba(255,120,30,${0.12 * h})` : null,
      flash: t > BACK - 0.05 && t < BACK + 0.25 ? 0.3 : 0,
      vignette: h * 0.2,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: HEAT, mood: 'tense' }, { t: MELT, mood: 'epic' }, { t: POOL, mood: 'drone' }, { t: BACK, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'city', from: 0, to: END, gain: 0.35 }, { type: 'crowd', from: 0, to: END, gain: 0.45 }, { type: 'wind', from: 0, to: END, gain: 0.15 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: HEAT, type: 'riser', d: 4.3 }, { t: MELT, type: 'rumble', d: 4 }, { t: MELT + 0.3, type: 'gasp' },
      { t: MELT + 1, type: 'crack' }, { t: 12, type: 'scream', gain: 0.5 }, { t: POOL, type: 'sub_drop' }, { t: 16.4, type: 'tick' }, { t: CUBE, type: 'thud' },
      { t: BACK - 0.5, type: 'riser', d: 0.5 }, { t: BACK, type: 'shimmer' }, { t: BACK + 0.2, type: 'crowd_cheer' }, { t: AIR, type: 'tick' }, { t: 29.6, type: 'shimmer' },
      { t: SUMMER, type: 'tick' }, { t: 34, type: 'tick' }, { t: 36.2, type: 'beep' }, { t: 38.4, type: 'tick' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.groundH = () => 0;
    w.groundMat.color.set(0xcdbf9f);
    w.setSky(0x4a86c8, 0xe8eef2, 0xdfe6ea);
    w.setFog(250, 1500);
    w.setSunDir(-0.45, 0.6, 0.55);
    tower = eiffelTower(); w.scene.add(tower);
    // Champ de Mars lawns + tree rows
    const lawn = new THREE.MeshStandardMaterial({ color: 0x6fa24f, roughness: 1 });
    for (const x of [-30, 30]) { const l = new THREE.Mesh(new THREE.PlaneGeometry(46, 520).rotateX(-Math.PI / 2), lawn); l.position.set(x, 0.03, 350); l.receiveShadow = true; w.scene.add(l); }
    const treeM = new THREE.MeshStandardMaterial({ color: 0x3f6f35, flatShading: true });
    const trees = new THREE.InstancedMesh(new THREE.BoxGeometry(6, 7, 6).translate(0, 7, 0), treeM, 160);
    const trunks = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.3, 0.4, 4).translate(0, 2, 0), new THREE.MeshStandardMaterial({ color: 0x5b4636 }), 160);
    const M = new THREE.Matrix4();
    let n = 0;
    for (const x of [-64, -74, 64, 74]) for (let z = 110; z < 700 && n < 160; z += 15) { M.makeTranslation(x, 0, z); trees.setMatrixAt(n, M); trunks.setMatrixAt(n, M); n++; }
    trees.castShadow = trunks.castShadow = true; w.scene.add(trees, trunks);
    // Haussmann blocks around
    const r = rng(21);
    for (const side of [-1, 1]) for (let z = 80; z < 900; z += 70) { const b = haussmann(60, 40, 20 + r() * 6, Math.floor(r() * 1000)); b.position.set(side * 120, 0, z); b.rotation.y = side * Math.PI / 2; w.scene.add(b); }
    for (let x = -260; x <= 260; x += 70) { const b = haussmann(64, 40, 22, Math.floor(r() * 1000)); b.position.set(x, 0, 950); w.scene.add(b); }
    // the Seine behind
    const river = new THREE.Mesh(new THREE.PlaneGeometry(3000, 90).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x4f7f8f, roughness: 0.3, metalness: 0.2 }));
    river.position.set(0, 0.05, -170); w.scene.add(river);
    // molten pool, cube, air cylinder
    pool = new THREE.Mesh(new THREE.BoxGeometry(125, 0.06, 125).translate(0, 0.03, 0), new THREE.MeshStandardMaterial({ color: 0xc4c9ce, metalness: 0.35, roughness: 0.3, emissive: 0xff6a00 }));
    pool.visible = false; w.scene.add(pool);
    cube = new THREE.Mesh(new THREE.BoxGeometry(9.75, 9.75, 9.75).translate(0, 4.875, 0), new THREE.MeshStandardMaterial({ color: 0xb8bec4, metalness: 0.35, roughness: 0.35 }));
    cube.castShadow = true; cube.visible = false; w.scene.add(cube);
    airCyl = new THREE.Mesh(new THREE.CylinderGeometry(88, 88, 330, 64, 1, true).translate(0, 165, 0), new THREE.MeshBasicMaterial({ color: 0x9fd8ff, transparent: true, opacity: 0, side: THREE.DoubleSide, depthWrite: false }));
    w.scene.add(airCyl);
    const ring = new THREE.Mesh(new THREE.TorusGeometry(88, 0.6, 6, 96).rotateX(Math.PI / 2), new THREE.MeshBasicMaterial({ color: 0xd8f1ff, transparent: true, opacity: 0 }));
    const ring2 = ring.clone(); ring2.material = ring.material; ring2.position.y = 330; airCyl.add(ring, ring2); this.ringMat = ring.material;
    crowd = new Crowd(w, 700, 61).scatter(-52, 52, 75, 330, Math.PI);
    for (const p of crowd.p) { p.sit = p.rnd3 < 0.3; if (Math.abs(p.bx) < 6 && p.bz > 200) p.vis = 0; }
  },

  update(t, w) {
    const m = melt(t), h = heat(t);
    // tower heat + melt (squash down, spread a bit)
    const hc = heatColor(h * 0.72);
    tower.userData.mats.forEach((mat) => { mat.emissive.copy(hc); mat.emissiveIntensity = 1.4; });
    tower.scale.set(1 + m * 0.15, Math.max(0.001, 1 - m) * (1 + (t > SUMMER && t < 34 ? 0.0005 : 0)), 1 + m * 0.15);
    tower.visible = m < 0.995;
    tower.rotation.z = Math.sin(t * 2) * 0.01 * m;
    // pool & cube
    const poolK = t < BACK ? smooth(prog(t, MELT + 1.5, POOL)) * (1 - smooth(prog(t, CUBE - 0.2, CUBE + 0.8))) : 0;
    pool.visible = poolK > 0.01; pool.scale.set(poolK, 1, poolK);
    pool.material.emissive.copy(heatColor(clamp(1 - prog(t, POOL, CUBE) * 1.2) * 0.7)); pool.material.emissiveIntensity = 1.2;
    const cubeK = t < BACK ? smooth(prog(t, CUBE - 0.2, CUBE + 0.8)) : 1 - smooth(prog(t, BACK, BACK + 0.6));
    cube.visible = cubeK > 0.01; cube.scale.setScalar(Math.max(0.001, cubeK));
    // air cylinder
    const air = smooth(prog(t, 27.2, 28)) * (1 - smooth(prog(t, SUMMER - 0.3, SUMMER + 0.3)));
    airCyl.material.opacity = 0.16 * air; this.ringMat.opacity = 0.9 * air;
    // late-afternoon light at the end
    const late = smooth(prog(t, 36, END));
    w.skyU.top.value.copy(mixColor(0x4a86c8, 0x3c5fa0, late)); w.skyU.hor.value.copy(mixColor(0xe8eef2, 0xf3c08e, late));
    w.fog.color.copy(mixColor(0xdfe6ea, 0xe7c3a2, late)); w.skyU.bot.value.copy(w.fog.color);
    w.sun.color.copy(mixColor(0xfff3e0, 0xffbf80, late));

    // camera
    let cam;
    if (t < 3.2) cam = keys(t, [[0, [0, 4, 430, 0, 150, 0, 46]], [3.2, [0, 4, 410, 0, 150, 0, 46]]], (k) => k);
    else if (t < MELT) cam = keys(t, [[3.2, [45, 3, 240, 0, 140, 0, 56]], [MELT, [35, 3, 225, 0, 150, 0, 56]]]);
    else if (t < POOL) cam = keys(t, [[MELT, [0, 20, 430, 0, 150, 0, 46]], [POOL, [0, 30, 380, 0, 10, 0, 46]]]);
    else if (t < CUBE) cam = keys(t, [[POOL, [110, 90, 190, 0, 0, 0, 50]], [CUBE, [90, 70, 150, 0, 0, 0, 50]]]);
    else if (t < BACK) cam = keys(t, [[CUBE, [24, 7, 36, 0, 5, 0, 50]], [BACK, [28, 8, 40, 0, 5, 0, 50]]]);
    else if (t < AIR) cam = keys(t, [[BACK, [0, 10, 470, 0, 150, 0, 46]], [AIR, [0, 10, 450, 0, 150, 0, 46]]]);
    else if (t < SUMMER) cam = keys(t, [[AIR, [260, 70, 470, 0, 150, 0, 46]], [SUMMER, [300, 60, 420, 0, 150, 0, 46]]]);
    else if (t < 36.2) cam = keys(t, [[SUMMER, [62, 2, 105, 0, 150, 0, 60]], [36.2, [70, 2, 95, 0, 160, 0, 60]]]);
    else if (t < 38.4) cam = keys(t, [[36.2, [60, 290, 110, 0, 300, 0, 50]], [38.4, [80, 280, 90, 0, 300, 0, 50]]]);
    else cam = keys(t, [[38.4, [-80, 40, 330, 0, 150, 0, 46]], [END, [-40, 6, 470, 0, 150, 0, 46]]]);
    w.shake = t > MELT && t < POOL ? 0.08 : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // tourists: watch, back away during the melt, cheer when it returns
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = p.sit ? -0.62 : 0; p.lean = p.sit ? -0.1 : 0; p.ry = p.bry + Math.sin(t * 0.4 + p.rnd * 20) * 0.15; p.armL = p.armR = 0.03;
      const scare = smooth(prog(t, MELT + p.rnd, MELT + 1 + p.rnd)) * (1 - smooth(prog(t, BACK, BACK + 1)));
      if (scare > 0) { p.y = lerp(p.y, 0, scare); p.lean = -0.2 * scare; p.z = p.bz + scare * (6 + p.rnd2 * 10); p.armL = p.armR = 0.5 * scare * (p.rnd3 > 0.5 ? 1 : 0.2); }
      const cheer = smooth(prog(t, BACK + 0.2, BACK + 0.8)) * (1 - smooth(prog(t, AIR, AIR + 1)));
      if (cheer > 0) { p.armL = p.armR = 0.9 * cheer; p.y = Math.abs(Math.sin(t * 8 + p.rnd * 9)) * 0.25 * cheer; }
      const photo = smooth(prog(t, 3.2 + p.rnd * 2, 4 + p.rnd * 2)) * (1 - smooth(prog(t, HEAT, HEAT + 1)));
      if (photo > 0 && p.rnd2 < 0.35) p.armR = 0.88 * photo;
    }
    crowd.update();
  },
};
