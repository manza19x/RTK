import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { mountainMesh } from '../engine/props2.js';
import { greatWall, island } from '../engine/props3.js';

const WALK = 7.6, LAPSE = 14.2, SPACE = 25.2, RUIN = 31.8, SEA = 34.0, END = 41.6;
const X0 = 6000; // the sea end ("Old Dragon's Head") is built far away
const crestY = (x) => 40 + 55 * Math.sin(x / 150) + 25 * Math.sin(x / 57 + 1);
const crestZ = (x) => 60 * Math.sin(x / 190) + 20 * Math.sin(x / 70);
const path = (x) => { const z = crestZ(x), z2 = crestZ(x + 1); return [crestY(x) - 1.2, z, Math.atan2(z2 - z, 1)]; };
const ruinF = (x) => (x > 380 && x < 560 ? 0.35 + 0.65 * Math.abs(Math.sin(x * 0.21)) * Math.abs(Math.sin(x * 0.05)) : 1);
const hFn = (x, z) => {
  const d = Math.abs(z - crestZ(x));
  let h = crestY(x) - 0.7 - Math.pow(d, 1.18) * 0.55 + Math.sin(x * 0.07 + z * 0.05) * 4 * clamp(d / 40);
  return Math.max(h, -120);
};
let hiker, crowd, pack, seaWall;
// hiker position along the wall (x coordinate), time-warped during the timelapse
const hx = (t) => -20 + 1.2 * Math.min(t, LAPSE) + 55 * clamp(t - LAPSE, 0, SPACE - LAPSE);

export default {
  title: '¿Y si recorrieras toda la Gran Muralla China a pie?',
  duration: 46.6,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Gran Muralla China', value: count(0, 21196, { dur: 2.2, suf: ' km' }), sub: 'Con todas sus ramas (medición de 2012)' },
    { t: 3.2, label: 'Comparación', value: '> 50 %', sub: 'De la vuelta a la Tierra' },
    { t: WALK, label: 'Ritmo', value: '20 km/día', sub: 'Sin descansar ni un día' },
    { t: 12.0, label: 'Tiempo', value: '≈ 3 años', sub: 'Unos 1.060 días', pulse: true },
    { t: LAPSE, label: 'Pasos', value: count(0, 28e6, { dur: 2, pre: '≈ ' }), sub: 'Más o menos' },
    { t: 16.4, label: 'Día', value: (dt) => String(Math.min(1060, 1 + Math.floor(Math.pow(dt / (SPACE - 16.4), 1.3) * 1060))), sub: 'Caminando' },
    { t: 18.6, label: 'Construcción', value: '≈ 2.000 años', sub: 'Del siglo VII a. C. al XVII' },
    { t: 20.8, label: 'Ingrediente secreto', value: 'Arroz glutinoso', sub: 'Mezclado con cal' },
    { t: 27.4, label: 'Vista desde el espacio', value: 'Mito', sub: 'A simple vista no se ve' },
    { t: RUIN, label: 'Muralla Ming', value: '≈ 30 %', sub: 'Ya ha desaparecido' },
    { t: 36.2, label: 'Cabeza del Viejo Dragón', value: 'Fin del camino', sub: 'Donde la muralla toca el mar' },
  ],
  captions: [
    { t: 3.3, d: 2.0, text: '21.196 kilómetros.' },
    { t: 5.4, d: 2.1, text: 'Más de la mitad de\nla vuelta a la Tierra.' },
    { t: WALK, d: 2.1, text: 'Empiezas a caminar.' },
    { t: 9.8, d: 2.1, text: 'A 20 km al día…' },
    { t: 12.0, d: 2.1, text: '…tardarías casi 3 años.' },
    { t: LAPSE, d: 2.1, text: 'Unos 28 millones de pasos.' },
    { t: 16.4, d: 2.1, text: 'Por montañas, desiertos\ny valles.' },
    { t: 18.6, d: 2.1, text: 'Construida durante\nunos 2.000 años.' },
    { t: 20.8, d: 2.1, text: 'Y en algunos tramos, la argamasa\nllevaba… arroz.' },
    { t: 23.0, d: 2.1, text: 'Arroz glutinoso con cal:\nmás fuerte y resistente.' },
    { t: SPACE, d: 2.1, text: '¿Y se ve desde el espacio?' },
    { t: 27.4, d: 2.1, text: 'No. A simple vista, no.' },
    { t: 29.6, d: 2.1, text: 'Es demasiado estrecha y\ndel color del terreno.' },
    { t: RUIN, d: 2.1, text: 'Y cerca de un tercio de la\nmuralla Ming ya ha desaparecido.' },
    { t: SEA, d: 2.1, text: 'Y al final de tu viaje…' },
    { t: 36.2, d: 2.1, text: '…la muralla entra en el mar.' },
    { t: 38.4, d: 3.1, text: '¿Te atreverías?' },
  ],
  end: { t: END, fact: 'La Gran Muralla mide más de la mitad de la vuelta a la Tierra' },
  fx(t) {
    return {
      vignette: 0.08,
      flash: (t > SEA - 0.05 && t < SEA + 0.2 ? 0.3 : 0),
      dark: t > SEA - 0.25 && t < SEA ? (t - (SEA - 0.25)) * 4 : 0,
    };
  },
  audio: {
    music: [{ t: 0, mood: 'epic' }, { t: WALK, mood: 'curious' }, { t: LAPSE, mood: 'epic' }, { t: SPACE, mood: 'drone' }, { t: RUIN, mood: 'night' }, { t: SEA, mood: 'cold' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'wind', from: 0, to: SEA, gain: 0.45 }, { type: 'crowd', from: 0, to: LAPSE, gain: 0.3 }, { type: 'waves', from: SEA, to: END, gain: 0.7 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: WALK, type: 'tick' }, { t: 12, type: 'tick' }, { t: LAPSE - 0.5, type: 'riser', d: 0.5 }, { t: LAPSE, type: 'whoosh' },
      ...[...Array(10).keys()].map((i) => ({ t: 16.4 + i * 0.85, type: 'tick', gain: 0.45 })), { t: 20.8, type: 'pop', gain: 0.5 }, { t: SPACE, type: 'riser', d: 2 },
      { t: 27.4, type: 'shimmer' }, { t: RUIN, type: 'crack' }, { t: SEA, type: 'whoosh' }, { t: 36.2, type: 'shimmer' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.ground.visible = false; w.groundH = () => 0;
    w.setSky(0x5b8fc9, 0xe6eef0, 0xc9d6de);
    w.setFog(250, 1700);
    const mtn = mountainMesh(hFn, 1800, 300, 12, 0x6f8f44, 0x7d6c52, 0.55); w.scene.add(mtn);
    // misty layered ridges in the distance
    const r = rng(17);
    for (let i = 0; i < 26; i++) { const h = island(160 + r() * 220, 140 + r() * 180, 30 + i, 0x5f7a52, 0x6f8f44); const a = r() * Math.PI * 2, d = 950 + r() * 900; h.position.set(Math.cos(a) * d, -60, Math.sin(a) * d); w.scene.add(h); }
    w.scene.add(greatWall(path, -820, 820, 3.2, 38, ruinF));
    // tourists near the start (popular section)
    crowd = new Crowd(w, 140, 28);
    crowd.p.forEach((p) => { p.bx = r() < 0.6 ? 6 + r() * 48 : -120 + r() * 50; p.off = (r() - 0.5) * 4.4; p.bry = r() < 0.5 ? Math.PI / 2 : -Math.PI / 2; });
    // the hiker (red jacket + backpack)
    hiker = new Crowd(w, 1, 3);
    hiker.torso.setColorAt(0, new THREE.Color(0xd62828)); hiker.arms.setColorAt(0, new THREE.Color(0xd62828)); hiker.arms.setColorAt(1, new THREE.Color(0xd62828));
    hiker.legs.setColorAt(0, new THREE.Color(0x2b3a55));
    pack = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.55, 0.25), new THREE.MeshStandardMaterial({ color: 0x2f6f3a })); pack.castShadow = true; w.scene.add(pack);
    // sea end set
    const beach = new THREE.Mesh(new THREE.PlaneGeometry(900, 500, 10, 50).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0xd9c9a0, roughness: 1 }));
    const bp = beach.geometry.attributes.position; for (let i = 0; i < bp.count; i++) bp.setY(i, bp.getZ(i) * 0.03); beach.geometry.computeVertexNormals();
    beach.position.set(X0, 0, 0); beach.receiveShadow = true; w.scene.add(beach);
    seaWall = new THREE.Group(); seaWall.position.x = X0; w.scene.add(seaWall);
    seaWall.add(greatWall((x) => [x * 0.03 - 1.2, 0, 0], -60, 220, 3.2, 1000));
    seaWall.rotation.y = -Math.PI / 2;
    const head = new THREE.Group();
    head.add(new THREE.Mesh(new THREE.BoxGeometry(18, 9, 18).translate(0, 2, 0), new THREE.MeshStandardMaterial({ color: 0x9c8d77, flatShading: true })));
    const pav = new THREE.Mesh(new THREE.BoxGeometry(9, 5, 9).translate(0, 9, 0), new THREE.MeshStandardMaterial({ color: 0xb23a2a })); head.add(pav);
    const roof = new THREE.Mesh(new THREE.ConeGeometry(8, 4, 4).rotateY(Math.PI / 4).translate(0, 13.5, 0), new THREE.MeshStandardMaterial({ color: 0x3f5a6b, flatShading: true })); head.add(roof);
    head.position.set(X0, 0, -66); head.traverse((o) => { if (o.isMesh) o.castShadow = true; }); w.scene.add(head);
  },

  update(t, w) {
    const atSea = t >= SEA;
    w.sea.visible = atSea; w.sea.position.set(X0, 0, -500); w.seaU.slopeSea.value = 0.03; w.seaU.slopeLand.value = 0.03;
    // day/night timelapse + dawn at the sea
    let sunA = 0.95;
    if (t > LAPSE && t < SPACE) sunA = 0.95 + (t - LAPSE) * Math.PI * 0.8;
    const el = Math.sin(sunA);
    const night = 0.65 * smooth(prog(-el, -0.05, 0.35));
    const dawn = atSea ? 1 - smooth(prog(t, SEA, END)) * 0.6 : 0;
    w.setSunDir(Math.cos(sunA) * 0.8, Math.max(0.06, el) * (atSea ? 0.4 : 1), atSea ? -0.9 : 0.5);
    w.skyU.top.value.copy(mixColor(mixColor(0x5b8fc9, 0x4f6fa8, dawn), 0x0a1430, night));
    w.skyU.hor.value.copy(mixColor(mixColor(0xe6eef0, 0xf6c08c, dawn), 0x1b2443, night));
    w.fog.color.copy(mixColor(mixColor(0xc9d6de, 0xe8c4a2, dawn), 0x141a30, night)); w.skyU.bot.value.copy(w.fog.color);
    w.light(lerp(1, 0.1, night), lerp(1, 0.4, night)); w.sun.color.copy(mixColor(0xfff3e0, 0xffb27a, dawn));
    w.starMat.opacity = night;
    const space = smooth(prog(t, SPACE, 29.0)) * (1 - (t >= RUIN ? 1 : 0));
    w.setFog(lerp(250, 1800, space), lerp(1700, 9000, space));

    // hiker
    const h = hiker.p[0];
    if (!atSea) {
      const x = hx(t), [y, z, yaw] = path(x);
      h.x = x; h.z = z; h.y = y + 6.62; h.ry = -Math.PI / 2 - yaw; h.lean = 0.12;
      h.armL = 0.1 + 0.08 * Math.sin(t * 6); h.armR = 0.1 - 0.08 * Math.sin(t * 6);
      h.y += Math.abs(Math.sin(t * 6)) * 0.05;
    } else {
      const k = smooth(prog(t, SEA, 37.5));
      h.x = X0; h.z = lerp(40, -44, k); h.y = h.z * 0.03 - 1.2 + 6.62; h.ry = Math.PI; h.lean = 0.1;
      h.armL = h.armR = t > 37.6 ? 0.92 : 0.12;
    }
    h.vis = 1;
    hiker.update();
    const hp = new THREE.Vector3(h.x, h.y, h.z);
    pack.position.set(h.x + Math.sin(h.ry) * -0.25, h.y + 1.15, h.z + Math.cos(h.ry) * -0.25); pack.rotation.y = h.ry;
    // tourists
    for (const p of crowd.p) {
      const x = p.bx + Math.sin(t * 0.2 + p.rnd * 9) * 2, [y, z, yaw] = path(x);
      p.x = x; p.z = z + p.off; p.y = y + 6.62; p.ry = p.bry - yaw; p.lean = 0.05; p.armL = p.armR = 0.05;
      p.vis = t < LAPSE + 1 ? 1 : 0;
    }
    crowd.update();

    // camera
    let cam;
    const fwd = (dx, up, side, lookAhead) => { const [y, z] = path(h.x + dx); const [ly, lz] = path(h.x + lookAhead); return [h.x + dx, y + 6.6 + up, z + side, h.x + lookAhead, ly + 7, lz]; };
    if (t < WALK) { const x = -260 + t * 22; const [y, z] = path(x); const [ly, lz] = path(x + 160); cam = [x - 40, y + 70, z + 90, x + 160, ly, lz, 52]; }
    else if (t < LAPSE) { const c = fwd(-8, 4.2, 0.6, 18); cam = [...c, 54]; }
    else if (t < SPACE) { const c = fwd(-90, 110, 130, 60); cam = [...c, 52]; }
    else if (t < RUIN) { const k = smooth(prog(t, SPACE, 29.6)); const c = fwd(-90, lerp(110, 3800, k), lerp(130, 300, k), 60); c[4] = lerp(c[4], -200, k); cam = [...c, 52]; }
    else if (!atSea) { const x = 420 + (t - RUIN) * 15; const [y, z] = path(x); const [ly, lz] = path(x + 70); cam = [x - 20, y + 18, z + 30, x + 70, ly + 5, lz, 52]; }
    else cam = keys(t, [[SEA, [X0 + 40, 9, 70, X0, 5, -60, 52]], [37.5, [X0 + 25, 12, 30, X0, 6, -60, 52]], [END, [X0 + 60, 25, -10, X0, 6, -66, 50]]]);
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);
  },
};
