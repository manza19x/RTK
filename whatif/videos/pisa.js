import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';

const SOIL = 5.4, WAR = 9.8, Y1990 = 14.2, LEAN = 18.6, FALL = 20.2, REW = 23.0, FIX = 25.2, LOCO = 31.8, POSE = 38.4, END = 42.6;
const DEG = Math.PI / 180;
let tower, crowd, dust;
const std = (o) => new THREE.MeshStandardMaterial({ roughness: 0.75, ...o });

// tilt angle (radians) as a pure function of time
function tilt(t) {
  if (t < Y1990) return 3.97 * DEG;
  if (t < LEAN) return lerp(3.97, 5.5, smooth(prog(t, Y1990, Y1990 + 1.5))) * DEG;
  if (t < FALL) return lerp(5.5, 7.5, smooth(prog(t, LEAN, FALL))) * DEG;
  if (t < REW) { const k = prog(t, FALL, FALL + 2.6); return lerp(7.5 * DEG, Math.PI / 2 - 0.02, k * k); }
  if (t < FIX) return lerp(Math.PI / 2 - 0.02, 5.5 * DEG, smooth(prog(t, REW, REW + 1.6)));
  return lerp(5.5, 3.97, smooth(prog(t, FIX, FIX + 2))) * DEG;
}

function buildTower() {
  const g = new THREE.Group(), marble = std({ color: 0xf1ede3 }), grey = std({ color: 0xc9c3b6 });
  const R = 7.75;
  // ground storey with blind arcade
  g.add(new THREE.Mesh(new THREE.CylinderGeometry(R, R + 0.3, 11, 32).translate(0, 5.5, 0), marble));
  const levels = [11, 18.3, 25.6, 32.9, 40.2, 47.5];
  const colGeo = new THREE.CylinderGeometry(0.28, 0.32, 6.0, 6);
  const cols = new THREE.InstancedMesh(colGeo, marble, levels.length * 30);
  const M = new THREE.Matrix4(); let k = 0;
  for (const y of levels) {
    g.add(new THREE.Mesh(new THREE.CylinderGeometry(R - 1.6, R - 1.6, 7.3, 24).translate(0, y + 3.65, 0), grey));         // inner wall (shadowed gallery)
    g.add(new THREE.Mesh(new THREE.CylinderGeometry(R + 0.35, R + 0.35, 0.7, 32).translate(0, y + 0.35, 0), marble));     // floor cornice
    g.add(new THREE.Mesh(new THREE.CylinderGeometry(R + 0.25, R + 0.25, 0.9, 32).translate(0, y + 6.85, 0), marble));     // arch band
    for (let i = 0; i < 30; i++) { const a = i / 30 * Math.PI * 2; M.makeTranslation(Math.cos(a) * (R - 0.1), y + 3.7, Math.sin(a) * (R - 0.1)); cols.setMatrixAt(k++, M); }
  }
  g.add(cols);
  // belfry
  g.add(new THREE.Mesh(new THREE.CylinderGeometry(R * 0.62, R * 0.62, 6.5, 24).translate(0, 54.8 + 3.25 - 6.5 + 4, 0), marble));
  g.add(new THREE.Mesh(new THREE.CylinderGeometry(R * 0.66, R * 0.66, 0.6, 24).translate(0, 58.3, 0), marble));
  g.traverse((o) => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
  return g;
}

export default {
  title: '¿Y si la Torre de Pisa se cayera?',
  duration: 47.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Torre de Pisa, Italia', value: count(0, 56, { dur: 1.4, suf: ' m' }), sub: 'Y 14.500 toneladas de mármol' },
    { t: 3.2, label: 'Inclinación', value: '3,97°', sub: 'Hoy' },
    { t: SOIL, label: 'Se inclina desde', value: '1178', sub: 'Al construir el segundo piso' },
    { t: WAR, label: 'Obra parada', value: '≈ 100 años', sub: 'Por las guerras' },
    { t: Y1990, label: '1990', value: count(3.97, 5.5, { dur: 1.5, dec: 2, suf: '°' }), sub: 'Cerrada al público 11 años', color: '#ffd1c4' },
    { t: LEAN, label: 'Inclinación', value: (dt) => (dt < FALL - LEAN ? (5.5 + 2 * dt / (FALL - LEAN)).toFixed(1).replace('.', ',') + '°' : '90°'), sub: 'Imaginemos que sigue…', color: '#ffb4b4' },
    { t: FALL + 0.4, label: 'Al suelo', value: '14.500 t', sub: 'De mármol y piedra', pulse: true },
    { t: FIX, label: 'Corrección', value: '≈ 45 cm', sub: 'Entre 1990 y 2001' },
    { t: 27.4, label: 'Tierra extraída', value: '≈ 38 m³', sub: 'Del lado contrario' },
    { t: 29.6, label: 'Estable', value: '≥ 200 años', sub: 'Según los ingenieros' },
    { t: 34.0, label: 'Terremotos', value: '≥ 4 fuertes', sub: 'Desde 1280, y sigue en pie' },
    { t: POSE, label: 'Fotos empujándola', value: 'Millones', sub: 'Cada año' },
  ],
  captions: [
    { t: 3.3, d: 2.0, text: 'Lleva inclinada más\nde 800 años.' },
    { t: SOIL, d: 2.1, text: 'Empezó a torcerse en 1178,\nmientras la construían…' },
    { t: 7.6, d: 2.1, text: '…porque el suelo es blando:\narcilla y arena.' },
    { t: WAR, d: 2.1, text: 'Las guerras pararon la obra\ncasi 100 años…' },
    { t: 12.0, d: 2.1, text: '…y eso la salvó:\nel suelo se asentó.' },
    { t: Y1990, d: 2.1, text: 'En 1990 llegó a 5,5°.' },
    { t: 16.4, d: 2.1, text: 'Y la cerraron 11 años.' },
    { t: LEAN, d: 1.6, text: 'Ahora imagina que sigue…' },
    { t: FALL + 0.5, d: 2.2, text: '14.500 toneladas,\ncontra el suelo.' },
    { t: REW, d: 2.1, text: 'Tranquilo: no va a pasar.' },
    { t: FIX, d: 2.1, text: 'Entre 1990 y 2001\nla enderezaron unos 45 cm.' },
    { t: 27.4, d: 2.1, text: '¿Cómo? Sacando tierra…\ndel lado contrario.' },
    { t: 29.6, d: 2.1, text: 'Ahora es estable durante\nal menos 200 años.' },
    { t: LOCO, d: 2.1, text: 'Y un dato loco:' },
    { t: 34.0, d: 2.1, text: 'El suelo blando que la inclina…' },
    { t: 36.2, d: 2.1, text: '…es lo que la ha salvado\nde los terremotos.' },
    { t: POSE, d: 3.9, text: '¿Tú también harías la foto\nempujándola?' },
  ],
  end: { t: END, fact: 'La Torre de Pisa empezó a inclinarse en 1178, mientras se construía' },
  fx(t) {
    const hit = t - (FALL + 2.6);
    const rew = t > REW && t < REW + 1.6;
    return {
      dust: hit > 0 && hit < 4 ? Math.exp(-hit * 0.8) * 0.9 : 0,
      flash: hit > 0 && hit < 0.15 ? 0.4 : 0,
      tint: rew ? `rgba(80,120,200,${0.18})` : null,
      vignette: t > LEAN && t < REW + 1.6 ? 0.25 : 0,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: Y1990, mood: 'tense' }, { t: FALL, mood: 'epic' }, { t: REW, mood: 'curious' }, { t: LOCO, mood: 'night' }, { t: POSE, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'crowd', from: 0, to: END, gain: 0.5 }, { type: 'wind', from: 0, to: END, gain: 0.2 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: SOIL, type: 'crack' }, { t: WAR, type: 'tick' }, { t: Y1990, type: 'crack' }, { t: Y1990 + 0.5, type: 'rumble', d: 1.5 },
      { t: LEAN, type: 'riser', d: 1.6 }, { t: FALL, type: 'crack' }, { t: FALL + 0.3, type: 'scream' }, { t: FALL + 2.55, type: 'mute', d: 0.05 }, { t: FALL + 2.6, type: 'boom' }, { t: FALL + 2.6, type: 'rumble', d: 3 },
      { t: REW, type: 'scratch' }, { t: REW + 0.1, type: 'whoosh_up' }, { t: FIX, type: 'tick' }, { t: 27.4, type: 'tick' }, { t: 29.6, type: 'shimmer' }, { t: LOCO, type: 'tick' },
      { t: 36.2, type: 'shimmer' }, { t: POSE, type: 'pop', gain: 0.5 }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.groundH = () => 0;
    w.groundMat.color.set(0x7fae58);
    w.setSky(0x4689d2, 0xe8f0f3, 0xdbe6ec);
    w.setFog(200, 1400);
    w.setSunDir(0.45, 0.65, 0.35);
    tower = new THREE.Group(); tower.add(buildTower()); w.scene.add(tower);
    // base plinth
    const plinth = new THREE.Mesh(new THREE.CylinderGeometry(10, 10.5, 0.8, 32).translate(0, 0.4, 0), std({ color: 0xe6e0d2 })); w.scene.add(plinth);
    // cathedral (striped marble) + baptistery
    const stripeCv = document.createElement('canvas'); stripeCv.width = 16; stripeCv.height = 64; const sg = stripeCv.getContext('2d');
    for (let i = 0; i < 8; i++) { sg.fillStyle = i % 2 ? '#f1ede3' : '#c7c1b4'; sg.fillRect(0, i * 8, 16, 8); }
    const st = new THREE.CanvasTexture(stripeCv); st.wrapS = st.wrapT = THREE.RepeatWrapping; st.repeat.set(1, 3); st.colorSpace = THREE.SRGBColorSpace;
    const duomo = new THREE.Group();
    duomo.add(new THREE.Mesh(new THREE.BoxGeometry(26, 32, 95).translate(0, 16, 0), std({ map: st })));
    duomo.add(new THREE.Mesh(new THREE.BoxGeometry(72, 26, 22).translate(0, 13, 10), std({ map: st })));
    const roof = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 14, 6, 4, 1).rotateY(Math.PI / 4), std({ color: 0x9a6b4f })); roof.scale.set(1, 1, 3.6); roof.position.y = 35; duomo.add(roof);
    duomo.add(new THREE.Mesh(new THREE.SphereGeometry(9, 20, 10, 0, Math.PI * 2, 0, Math.PI / 2).translate(0, 32, 10), std({ color: 0xb98b6a })));
    const facade = new THREE.Mesh(new THREE.BoxGeometry(30, 36, 2).translate(0, 18, -48), std({ map: st })); duomo.add(facade);
    duomo.position.set(-25, 0, -95); duomo.rotation.y = -Math.PI / 2 + 0.15; w.scene.add(duomo);
    const bapt = new THREE.Group();
    bapt.add(new THREE.Mesh(new THREE.CylinderGeometry(17, 17, 30, 32).translate(0, 15, 0), std({ color: 0xf1ede3 })));
    bapt.add(new THREE.Mesh(new THREE.SphereGeometry(17, 24, 12, 0, Math.PI * 2, 0, Math.PI / 2).translate(0, 30, 0), std({ color: 0xc9a787 })));
    bapt.add(new THREE.Mesh(new THREE.CylinderGeometry(3, 3, 6, 12).translate(0, 49, 0), std({ color: 0xf1ede3 })));
    bapt.position.set(-170, 0, -40); w.scene.add(bapt);
    // city walls and houses
    const wall = new THREE.Mesh(new THREE.BoxGeometry(600, 11, 3).translate(0, 5.5, 0), std({ color: 0xb58a5e })); wall.position.set(-80, 0, -175); w.scene.add(wall);
    const r = rng(9);
    const houses = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1).translate(0, 0.5, 0), std({ color: 0xffffff }), 160);
    const HC = [0xe9c99a, 0xd99a6c, 0xf1dcb5, 0xc98a5e, 0xe8b88a];
    const MM = new THREE.Matrix4();
    for (let i = 0; i < 160; i++) {
      let x, z; do { x = -400 + r() * 700; z = -150 + r() * 500; } while (Math.abs(x + 60) < 190 && z < 150);
      MM.compose(new THREE.Vector3(x, 0, z), new THREE.Quaternion(), new THREE.Vector3(12 + r() * 14, 9 + r() * 12, 12 + r() * 14)); houses.setMatrixAt(i, MM); houses.setColorAt(i, new THREE.Color(HC[i % HC.length]));
    }
    houses.castShadow = true; w.scene.add(houses);
    const path = new THREE.Mesh(new THREE.PlaneGeometry(14, 300).rotateX(-Math.PI / 2), std({ color: 0xd8d2c4 })); path.position.set(-60, 0.03, 30); path.rotation.y = 1.2; w.scene.add(path);
    // dust cloud for the impact
    dust = new THREE.InstancedMesh(new THREE.SphereGeometry(1, 8, 6), new THREE.MeshStandardMaterial({ color: 0xe8e0cf, transparent: true, opacity: 0.6, depthWrite: false }), 90);
    dust.frustumCulled = false; w.scene.add(dust); this.dp = [...Array(90)].map(() => ({ z: 5 + r() * 55, x: (r() - 0.5) * 18, s: 2 + r() * 5, v: 2 + r() * 5 }));
    // tourists (many doing the "holding it up" pose)
    crowd = new Crowd(w, 260, 12);
    crowd.p.forEach((p) => {
      let x, z; do { x = (r() - 0.5) * 120; z = -10 - r() * 70; } while (Math.hypot(x, z) < 14);
      p.bx = x; p.bz = z; p.pose = r() < 0.45; p.bry = p.pose ? Math.atan2(x, z) + Math.PI * 0.5 : Math.atan2(x, z);
    });
  },

  update(t, w) {
    const a = tilt(t);
    tower.rotation.set(a, 0, 0); // leans toward +z (south), away from the crowd
    // dust after impact
    const hit = t - (FALL + 2.6);
    const M = new THREE.Matrix4();
    this.dp.forEach((d, i) => {
      const k = hit > 0 && hit < 5 && t < REW ? hit : -1;
      M.compose(new THREE.Vector3(d.x * (1 + k * 0.4), 1 + k * d.v * 0.4, d.z), new THREE.Quaternion(), new THREE.Vector3().setScalar(k > 0 ? d.s * (1 + k * 0.6) : 0));
      dust.setMatrixAt(i, M);
    });
    dust.instanceMatrix.needsUpdate = true;
    w.shake = hit > 0 && hit < 1.5 ? (1.5 - hit) * 0.4 : 0;

    // camera
    let cam;
    if (t < 3.2) cam = keys(t, [[0, [70, 22, -70, 0, 26, 8, 50]], [3.2, [58, 18, -82, 0, 26, 8, 50]]], (k) => k);
    else if (t < Y1990) cam = keys(t, [[3.2, [-6, 1.8, -44, 0, 24, 4, 56]], [WAR, [8, 1.8, -42, 0, 24, 4, 56]], [Y1990, [64, 6, -6, 0, 26, 6, 52]]]);
    else if (t < LEAN) cam = keys(t, [[Y1990, [64, 6, -6, 0, 26, 6, 52]], [LEAN, [66, 6, 0, 0, 26, 8, 52]]]);
    else if (t < REW) cam = keys(t, [[LEAN, [90, 10, 4, 0, 22, 14, 52]], [FALL + 2.6, [100, 12, 20, 0, 6, 30, 54]], [REW, [104, 14, 26, 0, 4, 32, 54]]]);
    else if (t < FIX) cam = keys(t, [[REW, [104, 14, 26, 0, 10, 30, 54]], [FIX, [70, 18, -60, 0, 24, 6, 50]]]);
    else if (t < LOCO) cam = keys(t, [[FIX, [70, 18, -60, 0, 24, 6, 50]], [LOCO, [-60, 30, -70, 0, 26, 6, 50]]]);
    else if (t < POSE) cam = keys(t, [[LOCO, [-120, 70, -160, 0, 20, 0, 48]], [POSE, [-90, 50, -150, 0, 22, 0, 48]]]);
    else cam = keys(t, [[POSE, [10, 1.6, -58, 2, 14, 0, 56]], [END, [4, 1.6, -64, 2, 18, 0, 56]]]);
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // tourists
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.ry = p.bry; p.armL = p.armR = 0.03;
      if (p.pose) { p.armR = 0.55; p.armL = 0.25; p.lean = -0.08; }
      const panic = smooth(prog(t, FALL + p.rnd * 0.4, FALL + 0.7 + p.rnd * 0.4)) * (1 - smooth(prog(t, REW + 0.5, REW + 1.5)));
      if (panic > 0) { p.armL = p.armR = 0.9 * panic; p.lean = -0.2 * panic; p.z = p.bz - panic * 10; p.ry = lerp(p.bry, Math.atan2(p.bx, p.bz), panic); }
    }
    crowd.update();
  },
};
