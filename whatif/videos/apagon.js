import { Crowd, keys, prog, smooth, clamp, lerp, mixColor, rng, THREE } from '../engine/world.js';
import { count } from '../engine/hud.js';
import { timesSquare, taxi, milkyWay } from '../engine/props.js';

const OFF = 7.0, PHONES = 11.2, STARS = 15.6, BACK = 40.2, END = 45.5;
let crowd, city, taxis = [], mw, phones;
const tInfo = [];

export default {
  title: '¿Y si se fuera la luz en todo el planeta a la vez?',
  duration: 50.5,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Times Square, Nueva York', value: count(0, 330000, { dur: 2.4 }), sub: 'Personas que pasan cada día' },
    { t: 3.2, label: 'Luz eléctrica', value: '100 %', sub: 'En todo el planeta' },
    { t: OFF, label: 'Luz eléctrica', value: '0 %', sub: 'En todo el planeta', pulse: true, color: '#ffb4b4' },
    { t: PHONES, label: 'Linternas de móvil', value: count(0, 4000, { dur: 1.6, pre: '≈ ' }), sub: 'Solo en esta plaza' },
    { t: 13.4, label: 'Batería del móvil', value: '≈ 1 día', sub: 'Y sin forma de cargarlo' },
    { t: STARS, label: 'Estrellas visibles', value: count(20, 2500, { dur: 2.4, pre: '≈ ' }), sub: 'Antes: unas pocas' },
    { t: 20.0, label: 'Los Ángeles, 1994', value: 'Vía Láctea', sub: 'Mucha gente la vio por primera vez' },
    { t: 24.6, label: 'Agua en rascacielos', value: 'Cortada', sub: 'Sin bombas, no sube' },
    { t: 27.0, label: 'Ascensores', value: 'Parados', sub: 'Toca subir a pie' },
    { t: 29.2, label: 'Hospitales', value: 'Generadores', sub: 'Aguantan días, no semanas' },
    { t: 31.4, label: 'Apagón de 2003', value: count(0, 55e6, { dur: 1.8 }), sub: 'Personas sin luz en Norteamérica' },
    { t: BACK, label: 'Luz eléctrica', value: count(0, 100, { dur: 1.2, suf: ' %' }), sub: 'Vuelve la normalidad' },
  ],
  captions: [
    { t: 3.3, d: 1.9, text: 'Ahora mismo,\nen todo el planeta…' },
    { t: 5.3, d: 1.6, text: '…se va la luz.\nA la vez.' },
    { t: OFF + 0.2, d: 1.9, text: 'Times Square, a oscuras.' },
    { t: 9.2, d: 1.9, text: 'Solo quedan los faros\nde los coches.' },
    { t: PHONES + 0.1, d: 2.1, text: 'Y miles de linternas\nde móvil.' },
    { t: 13.4, d: 2.1, text: 'Que duran… un día,\ncomo mucho.' },
    { t: STARS + 0.1, d: 2.1, text: 'Entonces, alguien mira arriba.' },
    { t: 17.8, d: 2.1, text: 'La Vía Láctea,\nsobre Nueva York.' },
    { t: 20.0, d: 2.4, text: 'Tras un apagón en Los Ángeles\nen 1994…' },
    { t: 22.4, d: 2.2, text: '…hubo quien llamó asustado\npor la "nube plateada" del cielo.' },
    { t: 24.6, d: 2.3, text: 'Pero sin luz, el agua no sube\na los rascacielos.' },
    { t: 27.0, d: 2.1, text: 'Los ascensores, parados.' },
    { t: 29.2, d: 2.1, text: 'Los hospitales tiran\nde generadores.' },
    { t: 31.4, d: 2.2, text: 'En 2003, Nueva York\nya vivió algo así.' },
    { t: 33.6, d: 2.2, text: '55 millones de personas\nsin luz.' },
    { t: 35.8, d: 2.1, text: 'Y la ciudad que nunca duerme…' },
    { t: 38.0, d: 2.1, text: '…por fin vio las estrellas.' },
    { t: BACK + 0.1, d: 2.2, text: 'Hasta que vuelve la luz…' },
    { t: 42.4, d: 2.8, text: '…y las estrellas\ndesaparecen otra vez.' },
  ],
  end: { t: END, fact: 'En 2003 un apagón dejó sin luz a 55 millones de personas en EE. UU. y Canadá' },
  fx(t) {
    return {
      flash: t > OFF - 0.05 && t < OFF + 0.12 ? 0.25 : 0,
      dark: t > OFF && t < OFF + 0.3 ? 0.4 * (1 - (t - OFF) / 0.3) : 0,
      vignette: 0.2 * smooth(prog(t, OFF, OFF + 1)) * (1 - smooth(prog(t, BACK, BACK + 1))),
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: 3.2, mood: 'tense' }, { t: OFF + 0.4, mood: 'drone' }, { t: STARS, mood: 'night' }, { t: BACK, mood: 'curious' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'city', from: 0, to: OFF, gain: 0.7, fout: 0.1 }, { type: 'crowd', from: 0, to: END, gain: 0.5 }, { type: 'night', from: STARS, to: BACK, gain: 0.25 }, { type: 'city', from: BACK + 0.5, to: END, gain: 0.5 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: 5.3, type: 'riser', d: 1.6 }, { t: OFF - 0.02, type: 'mute', d: 0.02 }, { t: OFF, type: 'power_down' }, { t: OFF, type: 'sub_drop' },
      { t: OFF + 0.5, type: 'gasp' }, { t: PHONES, type: 'tick' }, { t: 13.4, type: 'tick' }, { t: STARS, type: 'shimmer' }, { t: 17.8, type: 'shimmer' }, { t: 20, type: 'whoosh' },
      { t: 24.6, type: 'tick' }, { t: 27, type: 'tick' }, { t: 29.2, type: 'heartbeat' }, { t: 31.4, type: 'tick' }, { t: 38, type: 'shimmer' },
      { t: BACK - 0.5, type: 'riser', d: 0.5 }, { t: BACK, type: 'crowd_cheer' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0); w.sea.visible = false;
    w.groundMat.color.set(0x55565c);
    w.clouds.visible = false;
    w.setFog(80, 420);
    w.setSunDir(0.2, 0.9, 0.25);
    city = timesSquare(w);
    // taxis in lanes
    const r = rng(5);
    for (let i = 0; i < 16; i++) {
      const tx = taxi(); w.scene.add(tx); taxis.push(tx);
      tInfo.push({ x: [-9.5, -5.5, 5.5, 9.5][i % 4], z: 40 - Math.floor(i / 4) * 24 - r() * 10, v: 2 + r() * 2, dir: i % 4 < 2 ? -1 : 1 });
    }
    crowd = new Crowd(w, 900, 52).scatter(-11.5, 11.5, -56, 32, Math.PI);
    for (const p of crowd.p) { if (Math.abs(p.x) < 1.6 && p.z > 22) p.vis = 0; p.phone = p.rnd3 < 0.38; }
    mw = milkyWay(w);
    // phone flashlights
    phones = new THREE.InstancedMesh(new THREE.SphereGeometry(0.07, 6, 4), new THREE.MeshBasicMaterial({ color: 0xffffff, toneMapped: false }), 900);
    phones.frustumCulled = false; w.scene.add(phones);
    this.glowTex = (() => { const cv = document.createElement('canvas'); cv.width = cv.height = 64; const g = cv.getContext('2d'); const gr = g.createRadialGradient(32, 32, 0, 32, 32, 32); gr.addColorStop(0, 'rgba(255,255,255,1)'); gr.addColorStop(0.25, 'rgba(255,250,235,0.5)'); gr.addColorStop(1, 'rgba(255,250,235,0)'); g.fillStyle = gr; g.fillRect(0, 0, 64, 64); return new THREE.CanvasTexture(cv); })();
    const halo = new THREE.InstancedMesh(new THREE.PlaneGeometry(0.9, 0.9), new THREE.MeshBasicMaterial({ map: this.glowTex, transparent: true, opacity: 0.45, depthWrite: false, blending: THREE.AdditiveBlending, toneMapped: false }), 900);
    halo.frustumCulled = false; w.scene.add(halo); this.halo = halo;
  },

  update(t, w) {
    const off = t >= OFF && t < BACK;
    // camera
    const cam = keys(t, [
      [0, [0.5, 2.0, 30, 0, 9, -40, 56]], [OFF - 0.2, [0.2, 2.2, 27, 0, 8, -40, 56]], [PHONES, [1, 2.4, 24, 0, 3, -30, 56]],
      [STARS - 0.2, [1, 2.4, 24, 0, 3, -30, 56]], [19.6, [0.5, 2.0, 22, 0, 55, -40, 62]], [24.4, [0.5, 2.0, 22, 0, 60, -38, 62]],
      [26.8, [4, 9, 34, 0, 2, -20, 56]], [33.4, [-3, 14, 40, 0, 2, -25, 56]], [35.6, [0.5, 2.2, 24, 0, 30, -40, 60]], [BACK - 0.1, [0.5, 2.0, 22, 0, 60, -40, 62]],
      [43, [0.5, 2.6, 30, 0, 10, -40, 56]], [END, [0.5, 3, 34, 0, 10, -40, 54]],
    ]);
    w.shake = 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // ---- lights on/off with flicker
    const flick = (k) => (t > OFF - 0.35 && t < OFF ? (Math.sin(t * 90 + k) > 0 ? 1 : 0.2) : 1);
    const backK = (k) => smooth(prog(t, BACK + k * 1.4, BACK + k * 1.4 + 0.1)) * (Math.sin(t * 70 + k * 9) > -0.6 || t > BACK + k * 1.4 + 0.5 ? 1 : 0.1);
    city.emiss.forEach((m, i) => {
      const base = m.color.r < 0.5 && m.emissive.r > 0.9 && m.emissiveMap ? 1 : 1.6;
      m.emissiveIntensity = (off ? (t > BACK ? 0 : 0) : (t >= BACK ? backK((i * 0.37) % 1) : flick(i))) * base;
    });
    city.boards.forEach((m, i) => {
      const k = off ? 0.02 : t >= BACK ? backK((i * 0.61) % 1) : flick(i * 3);
      m.color.setScalar(Math.max(0.02, k));
    });
    const glow = off ? 0 : 1;
    // sky: city glow vs. dark sky
    const dk = smooth(prog(t, OFF, OFF + 0.4)) * (1 - smooth(prog(t, BACK, BACK + 1.5)));
    w.skyU.top.value.copy(mixColor(0x0b1028, 0x02030a, dk));
    w.skyU.hor.value.copy(mixColor(0x5a3b45, 0x070a18, dk));
    w.fog.color.copy(mixColor(0x2a2230, 0x05070f, dk)); w.skyU.bot.value.copy(w.fog.color);
    w.skyU.sunGlow.value = 0;
    w.light(lerp(0.55, 0.05, dk), lerp(0.9, 0.32, dk));
    w.hemi.color.copy(mixColor(0xffc9a8, 0x7d8fd6, dk)); w.hemi.groundColor.set(0x1a1a22);
    w.sun.color.copy(mixColor(0xffd1b0, 0x9fb0ff, dk));
    const st = smooth(prog(t, STARS - 1, STARS + 2)) * (1 - smooth(prog(t, 42.4, 43.6)));
    w.starMat.opacity = st; mw.set(st); mw.follow(w.camera.position);

    // taxis: drive, then stop at blackout (headlights stay on)
    taxis.forEach((tx, i) => {
      const ti = tInfo[i];
      const tt = Math.min(t, OFF + 0.6) + Math.max(0, t - BACK - 1.5);
      let z = ti.z + ti.dir * -1 * ti.v * tt;
      z = ((z + 260) % 330 + 330) % 330 - 260;
      tx.position.set(ti.x, 0, z); tx.rotation.y = ti.dir < 0 ? 0 : Math.PI;
      tx.userData.lights.forEach((m) => {});
    });

    // people
    const M = new THREE.Matrix4(), V = new THREE.Vector3(), Q = new THREE.Quaternion(), S = new THREE.Vector3();
    let n = 0;
    const camQ = w.camera.quaternion;
    for (const p of crowd.p) {
      p.x = p.bx; p.z = p.bz + Math.sin(t * 0.25 + p.rnd * 10) * 0.8 * (off ? 0.2 : 1); p.y = 0; p.lean = 0;
      p.ry = p.bry + Math.sin(t * 0.4 + p.rnd * 20) * 0.25;
      p.armL = p.armR = 0.03;
      if (!off) p.y = Math.abs(Math.sin(t * 5 + p.rnd * 30)) * 0.03;
      const sh = smooth(prog(t, OFF + 0.2 + p.rnd * 0.6, OFF + 0.8 + p.rnd * 0.6)) * (1 - smooth(prog(t, BACK, BACK + 1)));
      p.lean = -0.1 * sh;
      const phoneUp = p.phone ? smooth(prog(t, PHONES - 0.5 + p.rnd * 1.5, PHONES + p.rnd * 1.5)) * (1 - smooth(prog(t, BACK + 0.5, BACK + 1.5))) : 0;
      const lookUp = smooth(prog(t, STARS + p.rnd * 2, STARS + 1 + p.rnd * 2)) * (1 - smooth(prog(t, BACK, BACK + 1)));
      p.lean = lerp(p.lean, -0.35, lookUp * (p.rnd2 > 0.3 ? 1 : 0.3));
      p.armR = lerp(p.armR, 0.88, phoneUp);
      if (p.vis && phoneUp > 0.6) {
        // approximate hand position (right arm raised)
        const lx = -0.42 * p.s, ly = 2.05 * p.s;
        const c = Math.cos(p.ry), s = Math.sin(p.ry);
        V.set(p.x + lx * c, ly + p.y, p.z - lx * s);
        S.setScalar(1); M.compose(V, Q, S); phones.setMatrixAt(n, M);
        M.compose(V, camQ, S.setScalar(0.5 + p.rnd * 0.4)); this.halo.setMatrixAt(n, M);
        n++;
      }
    }
    phones.count = n; this.halo.count = n;
    phones.instanceMatrix.needsUpdate = true; this.halo.instanceMatrix.needsUpdate = true;
    crowd.update();
  },
};
