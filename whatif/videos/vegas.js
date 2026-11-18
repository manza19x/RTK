import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { palm } from '../engine/props.js';
import { welcomeSign, neonTexture, hotelTower, moneyRain, billTexture } from '../engine/props3.js';

const ODDS = 5.4, WIN = 9.8, SPEND = 14.2, STACK = 18.6, WEIGHT = 23.0, TWIST = 25.2, END = 40.6;
let crowd, money, tower, jets, beam, neonMats = [];
const TW = { x: 0, z: -110 };
const stackH = (t) => (t < STACK ? 0 : 2244 * smooth(prog(t, STACK, 22.2)));

export default {
  title: '¿Y si ganaras la lotería más grande de la historia?',
  duration: 45.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Premio récord', value: count(0, 2.04e9, { dur: 2.4, suf: ' $' }), sub: 'Powerball, noviembre de 2022' },
    { t: 3.2, label: 'Celebración', value: 'Las Vegas', sub: '¿Dónde si no?' },
    { t: ODDS, label: 'Probabilidad', value: '1 entre 292.201.338', sub: 'De acertar el bote' },
    { t: 7.6, label: 'Rayo en tu vida', value: '1 entre 15.300', sub: 'Según el servicio meteorológico de EE. UU.' },
    { t: WIN, label: 'Tú', value: 'Ganador', sub: 'Contra todo pronóstico', pulse: true, color: '#ffe08a' },
    { t: 12.0, label: 'Pago único', value: count(0, 997.6e6, { dur: 1.4, pre: '≈ ', suf: ' $' }), sub: 'Antes de impuestos' },
    { t: SPEND, label: '1 millón al día', value: count(0, 2040, { dur: 1.6, suf: ' días' }), sub: '≈ 5,6 años para gastarlo todo' },
    { t: STACK, label: 'Torre de billetes de 100 $', value: count(0, 2244, { dur: 3.6, suf: ' m', curve: 'smooth' }), sub: 'Más alta que 6 torres Eiffel' },
    { t: WEIGHT, label: 'Peso', value: '≈ 20 t', sub: 'Un gramo por billete' },
    { t: 27.4, label: 'Lotería en Nevada', value: 'Prohibida', sub: 'Lo dice su constitución', pulse: true, color: '#ffb4b4' },
    { t: 31.8, label: 'La más cercana', value: 'En la frontera', sub: 'A unos 65 km, ya en California' },
    { t: 36.2, label: 'Tu plan', value: '¿?', sub: 'Cuéntalo en comentarios', pulse: true },
  ],
  captions: [
    { t: 3.3, d: 2.0, text: '2.040 millones de dólares.' },
    { t: ODDS, d: 2.1, text: 'La probabilidad:\n1 entre 292 millones.' },
    { t: 7.6, d: 2.1, text: 'Es unas 19.000 veces más fácil\nque te caiga un rayo en tu vida.' },
    { t: WIN, d: 2.1, text: 'Pero bueno… has ganado.' },
    { t: 12.0, d: 2.1, text: 'Si lo cobras de golpe:\nunos 997 millones.' },
    { t: SPEND, d: 2.1, text: 'Gastando 1 millón al día…' },
    { t: 16.4, d: 2.1, text: '…tardarías más de 5 años\nen gastarlo todo.' },
    { t: STACK, d: 2.1, text: 'En billetes de 100 $…' },
    { t: 20.8, d: 2.1, text: '…una pila de más de\n2 km de altura.' },
    { t: WEIGHT, d: 2.1, text: 'Y pesaría unas 20 toneladas.' },
    { t: TWIST, d: 2.1, text: 'Pero aquí viene lo curioso…' },
    { t: 27.4, d: 2.1, text: '…en Las Vegas no puedes\ncomprar lotería.' },
    { t: 29.6, d: 2.1, text: 'Nevada prohíbe la lotería\nen su constitución.' },
    { t: 31.8, d: 2.1, text: 'Así que mucha gente conduce\nhasta California para jugar.' },
    { t: 34.0, d: 2.1, text: 'Casinos, sí. Lotería, no.' },
    { t: 36.2, d: 3.6, text: 'Y tú, ¿qué harías con\n2.040 millones?' },
  ],
  end: { t: END, fact: 'Ganar el bote de Powerball es 1 entre 292 millones' },
  fx(t) {
    return {
      flash: t > WIN - 0.05 && t < WIN + 0.2 ? 0.45 * (1 - (t - WIN) / 0.25) : 0,
      vignette: 0.12,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: ODDS, mood: 'tense' }, { t: WIN, mood: 'epic' }, { t: TWIST, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'city', from: 0, to: END, gain: 0.55 }, { type: 'crowd', from: 0, to: END, gain: 0.5 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 0.4, type: 'ding' }, { t: 3.2, type: 'shimmer' }, { t: ODDS, type: 'tick' }, { t: 7.6, type: 'thunder_far' },
      { t: WIN - 0.8, type: 'riser', d: 0.8 }, { t: WIN, type: 'crowd_cheer' }, { t: WIN, type: 'shimmer' }, { t: WIN + 0.2, type: 'ding' }, { t: 12, type: 'tick' },
      { t: SPEND, type: 'tick' }, { t: STACK, type: 'riser', d: 3.6 }, { t: 22.2, type: 'boom', gain: 0.5 }, { t: WEIGHT, type: 'thud' },
      { t: TWIST, type: 'heartbeat' }, { t: 27.4, type: 'scratch' }, { t: 31.8, type: 'whoosh' }, { t: 34, type: 'tick' }, { t: 36.2, type: 'ding', gain: 0.5 },
      { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.groundH = () => 0;
    w.groundMat.color.set(0x5c5650); w.clouds.visible = false;
    w.setFog(150, 1800);
    w.setSunDir(0.2, 0.9, 0.3);
    const r = rng(23);
    // boulevard
    const road = new THREE.Mesh(new THREE.PlaneGeometry(40, 1800).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x26272b, roughness: 0.9 }));
    road.position.set(0, 0.02, -500); road.receiveShadow = true; w.scene.add(road);
    const median = new THREE.Mesh(new THREE.BoxGeometry(4, 0.3, 1800), new THREE.MeshStandardMaterial({ color: 0x8c8574 })); median.position.set(0, 0.15, -500); w.scene.add(median);
    for (let z = 140; z > -1300; z -= 22) { const p = palm(9 + r() * 3, 500 + z); p.position.set(0, 0.3, z); w.scene.add(p); }
    for (let z = 200; z > -1300; z -= 10) for (const x of [-10, 10]) { const s = new THREE.Mesh(new THREE.PlaneGeometry(0.3, 4).rotateX(-Math.PI / 2), new THREE.MeshBasicMaterial({ color: 0xd6d0c0 })); s.position.set(x, 0.03, z); w.scene.add(s); }
    // hotels + neon
    const tints = [0x8a6d2b, 0x2b3f6b, 0xd9d4c8, 0x3b2b4f, 0x6b2b2b, 0x2b2f3a];
    for (const side of [-1, 1]) {
      let z = 40;
      while (z > -1400) {
        const wd = 50 + r() * 40, h = 90 + r() * 140, d = 40 + r() * 20;
        const b = hotelTower(wd, h, d, Math.floor(r() * 1e6), tints[Math.floor(r() * tints.length)]);
        b.position.set(side * (75 + d / 2 + r() * 30), 0, z - wd / 2); b.rotation.y = side * Math.PI / 2; w.scene.add(b);
        // podium / casino front + neon sign
        const pod = new THREE.Mesh(new THREE.BoxGeometry(wd * 1.1, 14, 26).translate(0, 7, 0), new THREE.MeshStandardMaterial({ color: 0x3a3340, roughness: 0.6 }));
        pod.position.set(side * 48, 0, z - wd / 2); pod.rotation.y = side * Math.PI / 2; w.scene.add(pod);
        const nm = new THREE.MeshBasicMaterial({ map: neonTexture(Math.floor(r() * 1e6)), toneMapped: false }); neonMats.push(nm);
        const sign = new THREE.Mesh(new THREE.PlaneGeometry(22, 11), nm); sign.position.set(side * 34.9, 20, z - wd / 2); sign.rotation.y = -side * Math.PI / 2; w.scene.add(sign);
        const nm2 = new THREE.MeshBasicMaterial({ map: neonTexture(Math.floor(r() * 1e6)), toneMapped: false }); neonMats.push(nm2);
        const pylon = new THREE.Mesh(new THREE.PlaneGeometry(14, 7), nm2); pylon.position.set(side * 26, 16, z - wd / 2 + 30); pylon.rotation.y = -side * Math.PI / 2; w.scene.add(pylon);
        const post = new THREE.Mesh(new THREE.BoxGeometry(1.2, 13, 1.2).translate(0, 6.5, 0), new THREE.MeshStandardMaterial({ color: 0x777777 })); post.position.set(side * 26, 0, z - wd / 2 + 30); w.scene.add(post);
        z -= wd + 20 + r() * 30;
      }
    }
    // black-glass pyramid with sky beam
    const pyr = new THREE.Mesh(new THREE.CylinderGeometry(0.01, 115, 110, 4).rotateY(Math.PI / 4).translate(0, 55, 0), new THREE.MeshStandardMaterial({ color: 0x15161a, roughness: 0.15, metalness: 0.8 }));
    pyr.position.set(-230, 0, -560); w.scene.add(pyr);
    beam = new THREE.Mesh(new THREE.CylinderGeometry(3, 5, 3000, 12, 1, true).translate(0, 1610, 0), new THREE.MeshBasicMaterial({ color: 0xcfe3ff, transparent: true, opacity: 0.35, blending: THREE.AdditiveBlending, depthWrite: false, fog: false, toneMapped: false }));
    beam.position.copy(pyr.position); w.scene.add(beam);
    // needle tower far away
    const needle = new THREE.Mesh(new THREE.CylinderGeometry(4, 9, 330, 10).translate(0, 165, 0), new THREE.MeshStandardMaterial({ color: 0xe8e8e8 })); needle.position.set(60, 0, -1500); w.scene.add(needle);
    const podT = new THREE.Mesh(new THREE.CylinderGeometry(22, 14, 30, 16).translate(0, 300, 0), new THREE.MeshStandardMaterial({ color: 0xdddddd, emissive: 0x334466 })); podT.position.copy(needle.position); w.scene.add(podT);
    // fountain lake with jets
    const lake = new THREE.Mesh(new THREE.PlaneGeometry(130, 60).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x0d2a3f, roughness: 0.1, metalness: 0.3 })); lake.position.set(100, 0.1, -270); w.scene.add(lake);
    jets = new THREE.InstancedMesh(new THREE.SphereGeometry(0.5, 6, 4), new THREE.MeshBasicMaterial({ color: 0xe8f6ff, toneMapped: false, transparent: true, opacity: 0.8 }), 30 * 14);
    jets.frustumCulled = false; w.scene.add(jets);
    // welcome sign at the south end
    const ws = welcomeSign(); ws.position.set(0, 0.3, 150); ws.scale.setScalar(1.3); w.scene.add(ws);
    // the cash tower
    const bt = billTexture(); bt.wrapS = bt.wrapT = THREE.RepeatWrapping;
    tower = new THREE.Mesh(new THREE.BoxGeometry(12, 1, 6).translate(0, 0.5, 0), new THREE.MeshStandardMaterial({ map: bt, roughness: 0.8, emissive: 0x1a2a18 }));
    tower.position.set(TW.x, 0.3, TW.z); tower.castShadow = true; w.scene.add(tower); this.bt = bt;
    // crowd on the sidewalks
    crowd = new Crowd(w, 700, 66);
    crowd.p.forEach((p) => { const s = r() < 0.5 ? -1 : 1; p.bx = s * (22 + r() * 9); p.bz = 120 - r() * 700; p.bry = r() < 0.5 ? 0 : Math.PI; });
    money = moneyRain(w, 600, 8);
  },

  update(t, w) {
    // night lighting
    w.skyU.top.value.set(0x090c22); w.skyU.hor.value.set(0x4a2c48); w.fog.color.set(0x251a2c); w.skyU.bot.value.copy(w.fog.color);
    w.skyU.sunGlow.value = 0; w.light(0.55, 0.9); w.hemi.color.set(0xffc6b0); w.hemi.groundColor.set(0x241c24); w.sun.color.set(0xffd2b8);
    w.starMat.opacity = 0.25;
    neonMats.forEach((m, i) => m.color.setScalar(0.85 + 0.15 * Math.sin(t * (3 + i % 5) + i)));
    beam.material.opacity = 0.3 + 0.05 * Math.sin(t * 2);
    // fountain jets dance after the win
    const M = new THREE.Matrix4(), V = new THREE.Vector3(), Q = new THREE.Quaternion(), S = new THREE.Vector3();
    for (let j = 0; j < 30; j++) {
      const h = 6 + 22 * Math.max(0, Math.sin(t * 1.6 + j * 0.45)) * (t > WIN ? 1 : 0.4);
      for (let k = 0; k < 14; k++) { V.set(45 + j * 3.8, (k / 13) * h, -270 + Math.sin(j * 0.6) * 8); S.setScalar(0.8 + (1 - k / 13) * 0.8); M.compose(V, Q, S); jets.setMatrixAt(j * 14 + k, M); }
    }
    jets.instanceMatrix.needsUpdate = true;
    // cash tower
    const sh = stackH(t);
    tower.visible = sh > 0.5; tower.scale.y = Math.max(0.01, sh); this.bt.repeat.set(1, Math.max(1, sh / 3));

    // camera
    let cam;
    if (t < 3.2) cam = keys(t, [[0, [8, 3.2, 176, 0, 9, 150, 50]], [3.2, [5, 3.4, 170, 0, 10, 140, 50]]], (k) => k);
    else if (t < WIN) cam = keys(t, [[3.2, [3, 2.2, 125, 0, 30, -400, 56]], [WIN, [3, 2.2, 70, 0, 30, -400, 56]]]);
    else if (t < SPEND) cam = keys(t, [[WIN, [19, 2.4, 40, 26, 2.4, 10, 54]], [SPEND, [18, 2.6, 34, 26, 2.6, 4, 54]]]);
    else if (t < STACK) cam = keys(t, [[SPEND, [40, 7, -200, 100, 14, -268, 54]], [STACK, [44, 8, -210, 100, 16, -268, 54]]]);
    else if (t < WEIGHT) cam = keys(t, [[STACK, [22, 2, -60, 0, 120, -110, 62]], [20.8, [24, 2, -58, 0, 600, -110, 62]], [WEIGHT, [-260, 500, 420, 0, 900, -110, 52]]]);
    else if (t < TWIST) cam = keys(t, [[WEIGHT, [-260, 500, 420, 0, 900, -110, 52]], [TWIST, [-300, 450, 460, 0, 800, -110, 52]]]);
    else if (t < 31.8) cam = keys(t, [[TWIST, [-70, 120, -280, -230, 60, -560, 54]], [31.8, [-40, 110, -330, -230, 60, -560, 54]]]);
    else cam = keys(t, [[31.8, [6, 2.4, 110, 0, 25, -400, 56]], [END, [6, 30, 160, 0, 60, -400, 52]]]);
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);
    // money rain around the camera
    const mk = smooth(prog(t, 3.2, 4.2)) * (1 - smooth(prog(t, TWIST - 0.5, TWIST + 0.5))) * (t >= STACK && t < TWIST ? 0 : 1);
    money.mesh.visible = mk > 0.01;
    const c = w.camera.position; money.update(t, c.x, c.z - 25, mk);

    // crowd
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz + (p.bry === 0 ? -1 : 1) * t * 0.9 * (p.rnd2 > 0.4 ? 1 : 0); p.y = 0; p.lean = 0; p.ry = p.bry; p.armL = p.armR = 0.05;
      p.y = Math.abs(Math.sin(t * 5 + p.rnd * 20)) * 0.03;
      const cheer = smooth(prog(t, WIN + p.rnd * 0.4, WIN + 0.6 + p.rnd * 0.4)) * (1 - smooth(prog(t, TWIST, TWIST + 1)));
      if (cheer > 0) { p.armL = p.armR = 0.9 * cheer; p.y += Math.abs(Math.sin(t * 8 + p.rnd * 9)) * 0.35 * cheer; p.ry = lerp(p.bry, Math.atan2(p.bx - 3, p.bz - 70), cheer * 0.6); }
    }
    crowd.update();
  },
};
