import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count, clock } from '../engine/hud.js';

const OFF = 15.0, MOONOFF = 16.3, COLD = 26.6, ICE = 31.2, DEEP = 40.8, END = 46.0;
let crowd, rocks;
const SUN = new THREE.Vector3(0.2, 0.13, -1).normalize();
const MOON = new THREE.Vector3(-0.2, 0.3, -1).normalize();

export default {
  title: '¿Y si el Sol se apagara ahora mismo?',
  duration: 51,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Distancia al Sol', value: count(0, 150e6, { dur: 2.4, suf: ' km' }), sub: 'Un día de playa cualquiera' },
    { t: 3.2, label: 'La oscuridad llega en', value: (dt) => clock(500, 0, OFF - 3.2)(Math.pow(dt / (OFF - 3.2), 1.6) * (OFF - 3.2)), sub: 'La luz tarda en llegar' },
    { t: OFF, label: 'Sol', value: 'Apagado', sub: 'Por fin nos enteramos', pulse: true },
    { t: MOONOFF + 0.9, label: 'Luna', value: 'Apagada', sub: 'Ya no refleja nada', pulse: true },
    { t: 19.6, label: 'Estrellas visibles', value: count(0, 4500, { dur: 1.8, pre: '≈ ' }), sub: 'Sin luz, a simple vista' },
    { t: 22.0, label: 'Velocidad de la Tierra', value: count(0, 107000, { dur: 1.5, suf: ' km/h' }), sub: 'En línea recta, hacia el vacío' },
    { t: COLD, label: 'Temperatura', value: count(15, -18, { dur: 3.6, suf: ' °C', curve: 'lin' }), sub: 'Primera semana' },
    { t: ICE, label: 'Superficie del mar', value: 'Hielo', sub: 'Empieza a congelarse' },
    { t: 33.6, label: 'Temperatura', value: count(-18, -73, { dur: 2.4, suf: ' °C' }), sub: 'Después de un año', color: '#cfe8ff' },
    { t: 36.0, label: 'Fotosíntesis', value: '0 %', sub: 'Las plantas mueren' },
    { t: 38.4, label: 'Océano profundo', value: 'Líquido', sub: 'Durante miles de años' },
    { t: DEEP + 2.4, label: 'Vida', value: 'Sigue', sub: 'Junto a fuentes hidrotermales' },
  ],
  captions: [
    { t: 3.3, d: 2.4, text: 'El Sol acaba de apagarse.' },
    { t: 5.8, d: 2.3, text: 'Pero aquí nadie lo sabe.' },
    { t: 8.2, d: 2.3, text: 'Su última luz sigue viajando\nhacia nosotros.' },
    { t: 10.6, d: 2.1, text: '8 minutos y 20 segundos\nde viaje.' },
    { t: 12.8, d: 2.1, text: 'Disfrutad de la playa…\nmientras dure.' },
    { t: OFF + 0.3, d: 1.9, text: 'Y de golpe… noche.' },
    { t: 17.4, d: 2.1, text: 'La Luna también se apaga.' },
    { t: 19.6, d: 2.3, text: 'Solo quedan las estrellas.' },
    { t: 22.0, d: 2.3, text: 'Sin la gravedad del Sol…' },
    { t: 24.3, d: 2.2, text: '…la Tierra sale disparada\nen línea recta.' },
    { t: COLD + 0.1, d: 2.3, text: 'Empieza el frío.' },
    { t: 28.8, d: 2.3, text: 'En una semana, −18 °C.' },
    { t: ICE + 0.1, d: 2.3, text: 'El mar empieza a congelarse.' },
    { t: 33.6, d: 2.3, text: 'En un año, −73 °C.' },
    { t: 36.0, d: 2.3, text: 'Sin luz, no hay plantas.\nSin plantas, no hay comida.' },
    { t: 38.4, d: 2.3, text: 'Pero bajo el hielo…' },
    { t: DEEP, d: 2.3, text: '…el océano profundo\nsigue líquido.' },
    { t: DEEP + 2.4, d: 2.7, text: 'Y ahí abajo,\nla vida aguanta.' },
  ],
  end: { t: END, fact: 'La luz del Sol tarda 8 minutos y 20 segundos en llegar a la Tierra' },
  fx(t) {
    const cold = smooth(prog(t, COLD + 1, 36));
    const deep = smooth(prog(t, DEEP - 0.4, DEEP + 0.6));
    return {
      snow: smooth(prog(t, 28, 30)) * (1 - deep),
      snowWind: 0.4,
      frost: cold * 0.8 * (1 - deep),
      underwater: deep,
      tint: deep ? `rgba(0,8,14,${0.55 * deep})` : null,
      flash: 0,
      dark: t > OFF && t < OFF + 0.25 ? 0.6 : 0,
      vignette: 0.15 * smooth(prog(t, OFF, OFF + 2)),
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: 8.2, mood: 'tense' }, { t: OFF + 0.4, mood: 'drone' }, { t: COLD, mood: 'cold' }, { t: END - 0.3, mood: 'none' }],
    amb: [
      { type: 'waves', from: 0, to: ICE + 2, gain: 0.7, fout: 2 }, { type: 'crowd', from: 0, to: OFF + 1.5, gain: 0.45, fout: 0.8 },
      { type: 'wind', from: OFF, to: DEEP, gain: 0.35 }, { type: 'storm', from: COLD + 1, to: DEEP, gain: 0.3, fin: 3 },
      { type: 'underwater', from: DEEP - 0.3, to: END, gain: 0.7 },
    ],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: 8.2, type: 'heartbeat' }, { t: 10.2, type: 'heartbeat' }, { t: 12.2, type: 'heartbeat' }, { t: 13.6, type: 'heartbeat' },
      { t: OFF - 1.4, type: 'riser', d: 1.4 }, { t: OFF - 0.02, type: 'mute', d: 0.02 }, { t: OFF, type: 'power_down' }, { t: OFF, type: 'sub_drop' }, { t: OFF + 0.4, type: 'gasp' },
      { t: MOONOFF, type: 'power_down', gain: 0.5 }, { t: 19.6, type: 'shimmer' }, { t: 22, type: 'whoosh' }, { t: COLD, type: 'tick' },
      { t: ICE, type: 'crack' }, { t: ICE + 1.2, type: 'crack', gain: 0.7 }, { t: 33.6, type: 'crack' }, { t: 36, type: 'tick' }, { t: 38.4, type: 'tick' },
      { t: DEEP - 0.3, type: 'splash' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0.03, 0.01);
    w.setFog(60, 520);
    crowd = new Crowd(w, 900, 11).scatter(-14, 14, 1.5, 40, Math.PI);
    for (const p of crowd.p) {
      if (Math.abs(p.x) < 1.5 && p.z > 31) p.vis = 0;
      p.lying = p.rnd3 < 0.18; p.sitting = !p.lying && p.rnd3 < 0.3;
      if (p.lying) { p.bry = Math.PI / 2 + (p.rnd - 0.5); }
    }
    // a few dark rocks + boats
    const r = rng(4);
    rocks = new THREE.InstancedMesh(new THREE.DodecahedronGeometry(1, 0), new THREE.MeshStandardMaterial({ color: 0x2b2b30, roughness: 1, flatShading: true }), 14);
    const m = new THREE.Matrix4();
    for (let i = 0; i < 14; i++) {
      m.compose(new THREE.Vector3((r() - 0.5) * 90, -1.4, -20 - r() * 70), new THREE.Quaternion().setFromEuler(new THREE.Euler(r(), r() * 3, r())), new THREE.Vector3(1 + r() * 2, 0.7 + r(), 1 + r() * 2));
      rocks.setMatrixAt(i, m);
    }
    rocks.castShadow = true; w.scene.add(rocks);
    const boat = new THREE.Group();
    const hull = new THREE.Mesh(new THREE.BoxGeometry(2.2, 0.9, 7), new THREE.MeshStandardMaterial({ color: 0xffffff }));
    const cab = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1.0, 2.4), new THREE.MeshStandardMaterial({ color: 0xdfe6ea }));
    cab.position.set(0, 0.9, -0.5); boat.add(hull, cab); boat.position.set(18, 0.2, -120); boat.rotation.y = 0.6;
    w.scene.add(boat); this.boat = boat;
    w.moon.visible = true; w.sunDisc.visible = true;
  },

  update(t, w) {
    const cam = keys(t, [
      [0, [1, 3.4, 40, 0, 1.2, 0, 54]],
      [3.2, [0.5, 3.0, 37, 0, 1.4, -5, 54]],
      [12.6, [-0.5, 2.6, 32, 0, 4, -20, 52]],
      [OFF, [-0.5, 2.4, 31, 0, 6, -30, 50]],
      [19.4, [0, 2.2, 30, 0, 14, -30, 54]],
      [22, [0, 2.5, 33, 0, 6, -20, 56]],
      [COLD, [2, 7, 40, 0, 1, 0, 56]],
      [ICE, [0, 9, 34, 0, 0, -20, 54]],
      [DEEP - 0.5, [0, 5, 20, 0, 0, -40, 56]],
      [END, [0, 3, 10, 0, -2, -60, 58]],
    ]);
    w.shake = t > OFF && t < OFF + 1.5 ? 0.06 * (1 - (t - OFF) / 1.5) : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    const dark = t >= OFF ? 1 : 0;
    const snowK = smooth(prog(t, 28, 38));
    // sky
    if (!dark) {
      w.setSky(0x3a7cc2, 0xdcecf3, 0xd3e5ee);
      w.setSunDir(SUN.x, SUN.y, SUN.z);
      w.light(1, 1); w.skyU.sunGlow.value = 1;
      w.hemi.color.set(0xdfefff);
    } else {
      const k = smooth(prog(t, OFF, OFF + 0.5));
      w.skyU.top.value.copy(mixColor(0x3a7cc2, 0x02040c, k));
      w.skyU.hor.value.copy(mixColor(0xdcecf3, 0x0a1224, k));
      w.fog.color.copy(mixColor(0xd3e5ee, 0x070b16, k)); w.skyU.bot.value.copy(w.fog.color);
      w.skyU.sunGlow.value = 1 - k;
      w.light(lerp(1, 0.05, k), lerp(1, 0.8, k));
      w.hemi.color.copy(mixColor(0xdfefff, 0x6f86c9, k));
      w.hemi.groundColor.set(0x252a33);
    }
    w.starMat.opacity = smooth(prog(t, MOONOFF + 0.5, 21));
    w.cloudMat.opacity = dark ? 0.95 * (1 - smooth(prog(t, OFF, OFF + 0.4))) * 0.15 : 0.95;
    w.clouds.visible = w.cloudMat.opacity > 0.02;
    // bodies
    const cp = w.camera.position;
    w.sunDisc.position.copy(cp).addScaledVector(SUN, 1000);
    w.sunDisc.material.opacity = dark ? 0 : 1;
    w.moon.position.copy(cp).addScaledVector(MOON, 1000); w.moon.lookAt(cp);
    w.moon.material.opacity = t < OFF ? 0.55 : t < MOONOFF ? 0.85 : 0;
    // ground & sea freezing
    w.groundMat.color.copy(mixColor(0xcdbb9c, 0xe9eef3, snowK));
    w.seaU.iceAmt.value = smooth(prog(t, ICE - 0.5, ICE + 4));
    w.seaU.waveAmp.value = lerp(0.12, 0.0, smooth(prog(t, ICE - 0.5, ICE + 3)));
    w.seaU.foamAmt.value = 1 - w.seaU.iceAmt.value;
    // boat drifting
    this.boat.position.y = 0.2 + Math.sin(t * 1.3) * 0.1 * (1 - w.seaU.iceAmt.value);

    // people
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz; p.ry = p.bry + Math.sin(t * 0.4 + p.rnd * 30) * 0.1; p.y = 0; p.lean = 0; p.rz = 0;
      p.armL = p.armR = 0.03;
      if (p.lying) { p.lean = -Math.PI / 2 + 0.02; p.y = 0.18; }
      if (p.sitting) { p.y = -0.62; p.lean = -0.1; }
      // reaction to darkness: stand up, look up, point
      const react = smooth(prog(t, OFF + 0.2 + p.rnd * 1.2, OFF + 1 + p.rnd * 1.2));
      if (react > 0) {
        if (p.lying || p.sitting) { p.lean = lerp(p.lean, -0.15, react); p.y = lerp(p.y, 0, react); }
        p.lean = lerp(p.lean, -0.25, react);
        p.ry = lerp(p.ry, Math.PI + (p.rnd2 - 0.5) * 0.6, react);
        p.armR = lerp(p.armR, p.rnd2 < 0.4 ? 0.85 : 0.05, react);
      }
      // cold: huddle, arms in, gather to clusters, shiver
      const cold = smooth(prog(t, COLD + p.rnd, COLD + 2 + p.rnd));
      if (cold > 0) {
        const gx = Math.round(p.bx / 6) * 6, gz = Math.round(p.bz / 6) * 6;
        p.x = lerp(p.bx, gx + (p.bx - gx) * 0.35, cold); p.z = lerp(p.bz, gz + (p.bz - gz) * 0.35, cold);
        p.lean = lerp(p.lean, 0.18, cold); p.armR = lerp(p.armR, 0.0, cold); p.armL = 0;
        p.rz = Math.sin(t * 40 + p.rnd * 50) * 0.02 * cold;
      }
    }
    crowd.update();
  },
};
