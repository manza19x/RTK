import { Crowd, keys, prog, smooth, ease, clamp, lerp, mixColor, easeOut } from '../engine/world.js';
import { count } from '../engine/hud.js';

const JUMP = 11.2, LAND = 15.9, LEAVE = 24.0, NIGHT = 35.6, END = 46.0;
let crowd;

export default {
  title: '¿Y si todos los humanos saltaran a la vez?',
  duration: 51,
  titleCard: { t0: 0, t1: 3.1 },
  stats: [
    { t: 0, label: 'Personas en la Tierra', value: count(0, 8.2e9, { dur: 2.6 }), sub: 'Todas en un mismo sitio' },
    { t: 3.2, label: 'Espacio necesario', value: count(0, 4100, { dur: 1.6, suf: ' km²' }), sub: '≈ el tamaño de Mallorca' },
    { t: 8.2, label: 'Salto en', value: '3', pulse: true },
    { t: 9.2, label: 'Salto en', value: '2', pulse: true },
    { t: 10.2, label: 'Salto en', value: '1', pulse: true },
    { t: JUMP, label: 'Masa en el aire', value: count(0, 5e8, { dur: 1.4, suf: ' t' }), sub: '8.200 millones × ~60 kg' },
    { t: 13.4, label: 'La Tierra se aparta', value: '0,00000000000004 m', sub: 'Menos que un átomo', pulse: true },
    { t: LAND, label: 'Energía del impacto', value: count(0, 2.5e12, { dur: 0.9, suf: ' J' }), sub: 'Como un terremoto pequeño', pulse: true },
    { t: 19.6, label: 'Órbita de la Tierra', value: 'Sin cambios', sub: 'Ni un milímetro' },
    { t: LEAVE, label: 'Quieren irse a casa', value: count(0, 8.2e9, { dur: 1.8 }), sub: 'Todos a la vez' },
    { t: 26.6, label: 'Saliendo 1 por segundo', value: count(0, 260, { dur: 2.2, suf: ' años' }), sub: 'Para vaciar el lugar' },
    { t: 31.2, label: 'Agua al día', value: count(0, 16.4e9, { dur: 2, suf: ' L' }), sub: '≈ 6.500 piscinas olímpicas' },
    { t: NIGHT, label: 'Noche', value: '1', sub: 'Nadie ha conseguido salir' },
    { t: 38.4, label: 'Duración del salto', value: '0,5 s', sub: 'Volver a casa: generaciones' },
  ],
  captions: [
    { t: 3.3, d: 2.6, text: 'Los juntamos a todos\nen un solo lugar.' },
    { t: 6.0, d: 2.1, text: 'Hombro con hombro.' },
    { t: 8.2, d: 2.9, text: 'A la de tres…\nsaltan todos.' },
    { t: JUMP + 0.2, d: 2.1, text: '500.000 millones de kilos,\nen el aire.' },
    { t: 13.5, d: 2.3, text: 'La Tierra se aparta…\nmenos que un átomo.' },
    { t: LAND + 0.25, d: 1.5, text: 'BOOM.' },
    { t: 17.8, d: 1.8, text: 'Los sismógrafos lo notan.' },
    { t: 19.7, d: 2.2, text: 'Y la Tierra…\nni se inmuta.' },
    { t: 22.0, d: 2.0, text: 'El problema no es el salto.' },
    { t: LEAVE + 0.1, d: 2.4, text: 'Es volver a casa.' },
    { t: 26.7, d: 2.2, text: 'Saliendo de uno en uno…' },
    { t: 28.9, d: 2.2, text: '…tardarían 260 años.' },
    { t: 31.3, d: 2.2, text: 'Y todos tienen sed.' },
    { t: 33.5, d: 2.0, text: '16.400 millones de litros.\nCada día.' },
    { t: NIGHT + 0.2, d: 2.6, text: 'Llega la noche.\nNadie se ha movido.' },
    { t: 38.5, d: 2.1, text: 'El salto duró medio segundo.' },
    { t: 40.7, d: 2.3, text: 'La salida…\nvarias generaciones.' },
    { t: 43.1, d: 2.8, text: 'Mejor que cada uno\nsalte en su casa.' },
  ],
  end: { t: END, fact: 'Un salto humano normal dura menos de un segundo en el aire' },
  fx(t) {
    const land = t > LAND ? Math.exp(-(t - LAND) * 2.2) : 0;
    return {
      dust: land * 0.9,
      flash: t > LAND ? Math.exp(-(t - LAND) * 12) * 0.25 : 0,
      vignette: 0.1 * smooth(prog(t, 8.2, 11)) * (1 - smooth(prog(t, LAND, LAND + 2))),
    };
  },
  audio: {
    music: [{ t: 0, mood: 'curious' }, { t: 8.2, mood: 'tense' }, { t: LAND + 0.2, mood: 'drone' }, { t: NIGHT, mood: 'night' }, { t: END - 0.3, mood: 'none' }],
    amb: [{ type: 'crowd', from: 0, to: END, gain: 0.55 }, { type: 'wind', from: 0, to: END, gain: 0.25 }, { type: 'night', from: NIGHT, to: END, gain: 0.4 }],
    sfx: [
      { t: 0, type: 'whoosh' }, { t: 3.2, type: 'tick' }, { t: 8.2, type: 'beep' }, { t: 9.2, type: 'beep' }, { t: 10.2, type: 'beep' },
      { t: JUMP - 0.6, type: 'riser', d: 0.6 }, { t: JUMP, type: 'whoosh_up' }, { t: JUMP + 0.1, type: 'crowd_cheer' }, { t: 13.4, type: 'tick' },
      { t: LAND - 0.05, type: 'mute', d: 0.05 }, { t: LAND, type: 'boom' }, { t: LAND, type: 'thud' }, { t: 17.8, type: 'rumble', d: 2 },
      { t: 19.6, type: 'tick' }, { t: 22, type: 'tick' }, { t: LEAVE, type: 'whoosh' }, { t: 26.6, type: 'tick' }, { t: 31.2, type: 'tick' },
      { t: NIGHT, type: 'whoosh' }, { t: 38.4, type: 'tick' }, { t: END, type: 'pop' }, { t: END + 1.9, type: 'ding' },
    ],
  },

  setup(w) {
    w.reshapeGround(0, 0);
    w.sea.visible = false;
    w.groundMat.color.set(0xc9b48a);
    w.setSky(0x4a86c5, 0xe6eef0, 0xd7e2e6);
    w.setFog(30, 210);
    w.setSunDir(0.55, 0.55, 0.35);
    crowd = new Crowd(w, 3000, 3).scatter(-16, 16, -40, 34, Math.PI);
    // nobody right on top of the camera at start
    for (const p of crowd.p) if (Math.abs(p.x) < 1.2 && p.z > 21.5) p.vis = 0;
    for (const p of crowd.p) if (Math.abs(p.x) < 2.0 && p.z > 23.5) p.vis = 0;
    w.crowdCarpet(-16, 16, -40, 34);
  },

  update(t, w) {
    // ---- camera
    const cam = keys(t, [
      [0, [0.6, 2.4, 23.5, 0, 1.6, 0, 52]],
      [3.2, [0.3, 3.4, 24.5, 0, 1.4, -2, 52]],
      [8.0, [-1.5, 6.5, 30, 0, 1.0, 0, 50]],
      [10.8, [-0.3, 1.5, 27, 0, 2.2, 8, 50]],
      [LAND, [-0.1, 1.1, 26, 0, 2.3, 8, 52]],
      [19.5, [2, 4, 27, 0, 1.2, 4, 56]],
      [LEAVE - 0.2, [2, 6, 30, 0, 1, 0, 56]],
      [30, [0, 55, 70, 0, 0, -10, 52]],
      [NIGHT, [0, 80, 95, 0, 0, -30, 52]],
      [END, [0, 16, 40, 0, 1, 0, 50]],
    ]);
    w.shake = t > LAND ? Math.exp(-(t - LAND) * 3) * 0.45 : 0;
    w.cam([cam[0], cam[1], cam[2]], [cam[3], cam[4], cam[5]], cam[6]);

    // ---- sky: day -> dusk -> night
    const n = smooth(prog(t, NIGHT - 3, NIGHT + 1.5));
    const dusk = smooth(prog(t, NIGHT - 5, NIGHT - 1.5)) * (1 - n);
    w.skyU.top.value.copy(mixColor(mixColor(0x4a86c5, 0x3b4f8a, dusk), 0x050b1f, n));
    w.skyU.hor.value.copy(mixColor(mixColor(0xe6eef0, 0xf0a46a, dusk), 0x1b2747, n));
    const fogc = mixColor(mixColor(0xd7e2e6, 0xc98f6e, dusk), 0x0c1328, n);
    w.fog.color.copy(fogc); w.skyU.bot.value.copy(fogc);
    w.setSunDir(0.55, lerp(0.55, 0.08, dusk + n), 0.35);
    w.light(lerp(1, 0.18, n) * lerp(1, 0.7, dusk), lerp(1, 0.5, n));
    w.hemi.color.copy(mixColor(0xdfefff, 0x8fa6ff, n));
    w.sun.color.copy(mixColor(0xfff3e0, 0xffb27a, dusk));
    w.skyU.sunGlow.value = 1 - n;
    w.starMat.opacity = n;
    w.cloudMat.opacity = 0.95 * (1 - n * 0.8);

    // ---- people
    for (const p of crowd.p) {
      const off = p.rnd * 0.18;
      p.x = p.bx; p.z = p.bz; p.y = 0; p.lean = 0; p.ry = p.bry;
      // idle: look around a bit
      p.ry += Math.sin(t * 0.5 + p.rnd * 20) * 0.15;
      p.armL = p.armR = 0.02 + Math.sin(t * 0.8 + p.rnd2 * 9) * 0.02;
      // crouch before jump
      const crouch = smooth(prog(t, 10.2 + off, JUMP + off - 0.05)) * (1 - smooth(prog(t, JUMP + off - 0.05, JUMP + off + 0.1)));
      p.lean = crouch * 0.35;
      p.armL = p.armR = lerp(p.armL, 0.08, crouch);
      // jump (slow motion)
      const air = prog(t, JUMP + off, LAND + off * 0.2);
      if (air > 0 && air < 1) {
        const k = air;
        p.y = 4 * 0.6 * k * (1 - k) * (0.8 + p.rnd2 * 0.4);
        p.armL = p.armR = 0.75 + 0.2 * Math.sin(k * Math.PI) + p.rnd3 * 0.1;
      }
      const after = t - (LAND + off * 0.2);
      if (after > 0 && after < 0.8) p.lean = Math.sin(after / 0.8 * Math.PI) * 0.25;
      // leave: everyone turns away and shuffles
      if (t > LEAVE) {
        const k = smooth(prog(t, LEAVE + p.rnd * 1.5, LEAVE + 1.5 + p.rnd * 1.5));
        p.ry = lerp(p.ry, Math.PI + (p.x > 0 ? 0.4 : -0.4) * p.rnd2, k);
        const walk = Math.max(0, t - LEAVE - 1.5 - p.rnd * 1.5) * 0.25;
        p.z = p.bz - walk; p.x = p.bx + (p.rnd2 - 0.5) * walk * 0.4;
        p.y = Math.abs(Math.sin(t * 6 + p.rnd * 30)) * 0.04 * k;
      }
      if (t > NIGHT + 1) {
        const sit = smooth(prog(t, NIGHT + 1 + p.rnd * 4, NIGHT + 2 + p.rnd * 4)) * (p.rnd3 < 0.35 ? 1 : 0);
        p.y = lerp(p.y, -0.7, sit); p.lean = lerp(p.lean, -0.15, sit);
      }
    }
    crowd.update();
  },
};
