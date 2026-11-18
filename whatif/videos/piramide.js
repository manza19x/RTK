import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { pyramid, sphinx, datePalm, limestoneCanvas } from '../engine/props2.js';

const REW = 3.2, BUILD = 5.4, FAST = 12.0, CAP = 27.4, CASE = 29.6, FACT = 36.2, END = 43.6;
const BASE = 230, HEIGHT = 146, HALF = BASE / 2;
let great, gMat, ramp, cap, sph, crowd, blocks;
const SLED = 30, PER = 8;

function heightAt(t) {
  if (t < REW) return HEIGHT;
  if (t < BUILD) return HEIGHT * (1 - smooth(prog(t, REW, BUILD - 0.3)));
  if (t < FAST) return lerp(0, 8, prog(t, BUILD, FAST));
  if (t < CAP) return lerp(8, HEIGHT - 4, prog(t, FAST, CAP - 0.4));
  return HEIGHT - 4;
}
const halfTop = (h) => HALF * (1 - h / HEIGHT);
// sledge travel distance (time-warped during the timelapse)
const travel = (t) => 1.3 * t + 30 * clamp(t - FAST, 0, CAP - FAST);

export default {
  title: '¿Y si tuvieras que construir la Gran Pirámide?',
  duration: 48.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Altura original', value: count(0, 146, { dur: 2, suf: ' m' }), sub: 'Giza, Egipto' },
    { t: REW, label: 'Bloques necesarios', value: count(0, 2.3e6, { dur: 2 }), sub: 'De piedra' },
    { t: 7.6, label: 'Peso medio', value: '≈ 2,5 t', sub: 'Como un coche grande' },
    { t: 9.8, label: 'Plazo', value: '≈ 20 años', sub: 'Lo que tardaron los egipcios' },
    { t: FAST, label: 'Ritmo', value: '1 bloque / 5 min', sub: '24 horas al día, sin parar', pulse: true },
    { t: 14.2, label: 'Trabajadores', value: count(0, 20000, { dur: 1.4, pre: '≈ ' }), sub: 'Con salario y comida' },
    { t: 18.6, label: 'Herramientas', value: 'Cobre y piedra', sub: 'Sin poleas' },
    { t: 20.8, label: 'El truco', value: 'Arena mojada', sub: 'Reduce la fricción a la mitad' },
    { t: 23.0, label: 'Año', value: (dt) => String(Math.min(20, 1 + Math.floor(dt / (CAP - 23) * 20))), sub: 'De construcción' },
    { t: CAP, label: 'Altura', value: '146 m', sub: 'Terminada', pulse: true },
    { t: CASE, label: 'Revestimiento', value: 'Caliza blanca', sub: 'Hoy casi ha desaparecido' },
    { t: 31.8, label: 'Récord de altura', value: '≈ 3.800 años', sub: 'Hasta la catedral de Lincoln (1311)' },
    { t: 38.4, label: 'Cleopatra', value: '69–30 a. C.', sub: 'Pirámide: hacia el 2560 a. C.' },
  ],
  captions: [
    { t: REW + 0.1, d: 2.1, text: 'Te dan el trabajo.\nDesde cero.' },
    { t: BUILD + 0.1, d: 2.1, text: '2,3 millones de bloques.' },
    { t: 7.6, d: 2.1, text: 'Cada uno pesa como\nun coche grande.' },
    { t: 9.8, d: 2.1, text: 'Tienes unos 20 años.' },
    { t: FAST, d: 2.1, text: 'Un bloque cada 5 minutos.\nDía y noche. 20 años.' },
    { t: 14.2, d: 2.1, text: 'Unos 20.000 trabajadores…' },
    { t: 16.4, d: 2.1, text: '…y no, no eran esclavos.' },
    { t: 18.6, d: 2.1, text: 'Con herramientas de\ncobre y piedra.' },
    { t: 20.8, d: 2.1, text: 'Arrastrando trineos\nsobre arena mojada.' },
    { t: 23.0, d: 2.1, text: 'Capa a capa…' },
    { t: 25.2, d: 2.1, text: '…año tras año…' },
    { t: CAP + 0.1, d: 2.1, text: 'Terminada.' },
    { t: CASE, d: 2.1, text: 'Recubierta de piedra blanca\npulida. Brillaba al sol.' },
    { t: 31.8, d: 2.1, text: 'Fue lo más alto construido\npor el ser humano…' },
    { t: 34.0, d: 2.1, text: '…durante unos 3.800 años.' },
    { t: FACT, d: 2.1, text: 'Y un dato loco:' },
    { t: 38.4, d: 2.2, text: 'Cleopatra vivió más cerca de\nla llegada a la Luna…' },
    { t: 40.6, d: 2.9, text: '…que de la construcción\nde esta pirámide.' },
  ],
  end: { t: END, fact: 'La Gran Pirámide de Giza tiene unos 4.500 años' },
  fx(t) {
    return {
      dust: t > BUILD && t < FAST ? 0.15 : 0,
      flash: (t > CAP - 0.05 && t < CAP + 0.25 ? 0.4 * (1 - (t - CAP) / 0.3) : 0) + (t > CASE - 0.05 && t < CASE + 0.3 ? 0.35 : 0),
      vignette: 0.05,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: FAST, mood: 'epic' }, { t: CAP, mood: 'curious' }, { t: FACT, mood: 'night' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'wind', from: 0, to: END, gain: 0.35 }, { type: 'crowd', from: BUILD, to: CAP, gain: 0.35 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: REW, type: 'riser', d: 2 }, { t: BUILD - 0.3, type: 'thud' }, { t: 7.6, type: 'thud' }, { t: 9.8, type: 'tick' },
      { t: FAST - 0.5, type: 'riser', d: 0.5 }, { t: FAST, type: 'whoosh' }, ...[...Array(15).keys()].map((i) => ({ t: FAST + 1 + i, type: 'tick', gain: 0.5 })),
      { t: CAP, type: 'shimmer' }, { t: CAP, type: 'crowd_cheer' }, { t: CASE, type: 'shimmer' }, { t: 31.8, type: 'tick' }, { t: FACT, type: 'tick' },
      { t: 40.6, type: 'shimmer' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false; w.groundH = () => 0;
    w.groundMat.color.set(0xdcc08c);
    w.setSky(0x3f7fc6, 0xf3e6cf, 0xeadcc0);
    w.setFog(500, 3200);
    const r = rng(14);
    // great pyramid (rebuilt every frame while growing)
    const lt = new THREE.CanvasTexture(limestoneCanvas()); lt.colorSpace = THREE.SRGBColorSpace; lt.wrapS = lt.wrapT = THREE.RepeatWrapping;
    gMat = new THREE.MeshStandardMaterial({ map: lt, roughness: 0.95, flatShading: true }); gMat.userData.map = lt;
    great = new THREE.Mesh(new THREE.BufferGeometry(), gMat); great.castShadow = great.receiveShadow = true; w.scene.add(great);
    cap = new THREE.Mesh(new THREE.CylinderGeometry(0.01, 5.6, 4, 4).rotateY(Math.PI / 4).translate(0, 2, 0), new THREE.MeshStandardMaterial({ color: 0xffcf4a, metalness: 0.9, roughness: 0.25, emissive: 0x6b4a00 }));
    w.scene.add(cap);
    ramp = new THREE.Mesh(new THREE.BoxGeometry(16, 2, 1), new THREE.MeshStandardMaterial({ color: 0xb69a6c, roughness: 1 })); ramp.castShadow = ramp.receiveShadow = true; w.scene.add(ramp);
    const khafre = pyramid(215, 136); khafre.position.set(-300, 6, -290); w.scene.add(khafre);
    const menk = pyramid(103, 65); menk.position.set(-520, 3, -560); w.scene.add(menk);
    this.others = [khafre, menk];
    sph = sphinx(); sph.position.set(150, 0, 300); sph.rotation.y = Math.PI + 0.45; w.scene.add(sph);
    for (let i = 0; i < 30; i++) { const p = datePalm(9 + r() * 6); p.position.set(400 + r() * 300, 0, 300 + r() * 400); w.scene.add(p); }
    const duneM = new THREE.MeshStandardMaterial({ color: 0xd7b680, flatShading: true });
    for (let i = 0; i < 16; i++) { const d = new THREE.Mesh(new THREE.SphereGeometry(1, 10, 6), duneM); const a = -Math.PI * 0.2 - r() * Math.PI * 1.1; d.position.set(Math.cos(a) * (700 + r() * 400), -10, Math.sin(a) * (700 + r() * 400)); d.scale.set(120 + r() * 120, 30 + r() * 25, 90 + r() * 60); w.scene.add(d); }
    // blocks on sledges + workers
    blocks = new THREE.InstancedMesh(new THREE.BoxGeometry(1.4, 1.1, 1.8).translate(0, 0.85, 0), new THREE.MeshStandardMaterial({ color: 0xd2b98a, roughness: 1 }), SLED);
    blocks.castShadow = true; blocks.frustumCulled = false; w.scene.add(blocks);
    crowd = new Crowd(w, SLED * PER + 160, 33);
    crowd.p.forEach((p, i) => {
      // workers wear white linen kilts and simple tunics
      const c = new THREE.Color([0xf2ead8, 0xe8dcc0, 0xd9c7a0, 0xf5f0e6][i % 4]);
      crowd.torso.setColorAt(i, r() < 0.6 ? new THREE.Color([0xb07a4f, 0x8a5a3a, 0x9c6a43][i % 3]) : c);
      crowd.legs.setColorAt(i, c);
      if (i >= SLED * PER) { p.bx = (r() - 0.5) * 200; p.bz = HALF + 10 + r() * 60; p.bry = r() * 6.28; }
    });
  },

  update(t, w) {
    const h = heightAt(t);
    great.geometry.dispose();
    const rb = BASE / Math.SQRT2, rt = rb * (1 - h / HEIGHT);
    great.geometry = new THREE.CylinderGeometry(Math.max(rt, 0.01), rb, Math.max(h, 0.05), 4, 1).rotateY(Math.PI / 4).translate(0, Math.max(h, 0.05) / 2, 0);
    gMat.userData.map.repeat.set(BASE / 6, Math.max(h, 1) / 6);
    const cased = smooth(prog(t, CASE - 0.1, CASE + 0.3));
    const want = cased > 0.5 ? null : gMat.userData.map;
    if (gMat.map !== want) { gMat.map = want; gMat.needsUpdate = true; }
    gMat.color.copy(mixColor(0xffffff, 0xf6f1e4, cased));
    gMat.roughness = lerp(0.95, 0.45, cased);
    cap.visible = t < REW || t >= CAP;
    this.others.forEach((o) => (o.visible = t < REW || t >= CAP));
    cap.position.y = t < REW ? HEIGHT - 4 : lerp(HEIGHT + 6, HEIGHT - 4, smooth(prog(t, CAP, CAP + 0.6)));
    // ramp up the south face
    const ht = halfTop(h), zs = ht + 3.6 * h + 8;
    ramp.visible = t > BUILD - 0.3 && t < CAP + 0.5 && h > 0.5;
    const dz = zs - ht, L = Math.hypot(dz, h);
    ramp.scale.set(1, 1, L); ramp.position.set(0, h / 2 - 0.9, (zs + ht) / 2); ramp.rotation.set(Math.atan2(h, dz), 0, 0);

    // day / night timelapse
    let sunA = 0.9;
    if (t > FAST && t < CAP) sunA = 0.9 + (t - FAST) * Math.PI * 0.9;
    if (t >= FACT) sunA = 0.9 + Math.PI * 0.32 * smooth(prog(t, FACT, END));
    const el = Math.sin(sunA), az = Math.cos(sunA);
    w.setSunDir(az * 0.8, Math.max(0.05, el) , 0.55);
    const night = 0.7 * smooth(prog(-el, -0.05, 0.35));
    const sunset = t >= FACT ? smooth(prog(t, FACT, END)) : 0;
    w.skyU.top.value.copy(mixColor(mixColor(0x3f7fc6, 0x2d3f78, sunset), 0x081028, night));
    w.skyU.hor.value.copy(mixColor(mixColor(0xf3e6cf, 0xf4a35c, sunset), 0x1b2240, night));
    w.fog.color.copy(mixColor(mixColor(0xeadcc0, 0xdf9a62, sunset), 0x141a30, night)); w.skyU.bot.value.copy(w.fog.color);
    w.light(lerp(1, 0.08, night) * lerp(1, 0.75, sunset), lerp(1, 0.35, night));
    w.sun.color.copy(mixColor(0xfff1d6, 0xffa25a, sunset));
    w.starMat.opacity = night * 0.9;

    // camera
    let cam;
    if (t < BUILD) cam = keys(t, [[0, [250, 16, 470, 40, 55, 0, 48]], [BUILD, [235, 18, 450, 30, 40, 0, 48]]]);
    else if (t < FAST) cam = keys(t, [[BUILD, [28, 5, 236, 0, 3, 170, 52]], [FAST, [22, 6, 228, 0, 5, 160, 52]]]);
    else if (t < CAP) cam = keys(t, [[FAST, [420, 140, 560, 0, 45, 0, 46]], [CAP - 0.1, [460, 170, 520, 0, 70, 0, 46]]]);
    else if (t < CASE) cam = keys(t, [[CAP, [38, 158, 52, 0, 146, 0, 52]], [CASE, [44, 160, 58, 0, 144, 0, 52]]]);
    else if (t < FACT) cam = keys(t, [[CASE, [330, 50, 430, 0, 60, 0, 46]], [FACT, [290, 30, 470, 0, 60, 0, 46]]]);
    else { const face = sph.localToWorld(new THREE.Vector3(0, 16, -24)); const eye = sph.localToWorld(new THREE.Vector3(10, 14, -70)); cam = keys(t, [[FACT, [eye.x, eye.y, eye.z, face.x, face.y + 6, face.z, 50]], [END, [eye.x + 15, eye.y + 4, eye.z + 10, 0, 70, 0, 50]]]); }
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // sledges and workers
    const Lg = 160, total = Lg + L;
    const pathPos = (d, out) => {
      d = ((d % total) + total) % total;
      if (d < Lg) { out.set(0, 0, zs + Lg - d); out.dir = 0; }
      else { const k = (d - Lg) / L; out.set(0, k * h, zs - k * dz); out.dir = Math.atan2(h, dz); }
      return out;
    };
    const M = new THREE.Matrix4(), Q = new THREE.Quaternion(), V = new THREE.Vector3(), S = new THREE.Vector3(1, 1, 1), E = new THREE.Euler();
    const working = t > BUILD - 0.3 && t < CAP + 0.5;
    for (let i = 0; i < SLED; i++) {
      const d0 = i * total / SLED, xo = (i % 3 - 1) * 4.5;
      pathPos(travel(t) + d0, V); const pitch = V.dir; V.x += xo;
      E.set(-pitch, 0, 0); Q.setFromEuler(E);
      S.setScalar(working ? 1 : 0); M.compose(V.clone().setY(V.y + 0.2), Q, S); blocks.setMatrixAt(i, M);
      for (let k = 0; k < PER; k++) {
        const p = crowd.p[i * PER + k], P = new THREE.Vector3();
        pathPos(travel(t) + d0 + 2.4 + Math.floor(k / 2) * 1.2, P);
        p.x = P.x + xo + (k % 2 ? 0.55 : -0.55); p.z = P.z; p.y = P.y + Math.abs(Math.sin(t * 6 + k)) * 0.05;
        p.ry = Math.PI; p.lean = 0.45 - P.dir; p.armL = p.armR = 0.15; p.vis = working ? 1 : 0;
      }
    }
    blocks.instanceMatrix.needsUpdate = true;
    for (let i = SLED * PER; i < crowd.n; i++) {
      const p = crowd.p[i];
      p.x = p.bx; p.z = Math.max(p.bz, zs + 6 + (i % 7) * 3); p.y = 0; p.ry = p.bry + Math.sin(t + i) * 0.3; p.lean = 0.1; p.armL = p.armR = 0.1;
      p.vis = working ? 1 : 0;
    }
    crowd.update();
  },
};
