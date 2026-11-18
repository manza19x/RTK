# ¿Y si…? — generador de videos virales

Videos verticales (1080×1920, 30 fps, ~50 s) con el mismo montaje que el video de referencia
"What if every human on Earth farted at the same time?":

- Escena 3D low‑poly con gente de bloques (Three.js).
- Dato grande arriba a la izquierda (etiqueta pequeña, número que cuenta, subtítulo).
- Frase narrativa en cursiva serif en el centro‑bajo.
- Cuenta atrás / giro final y tarjeta negra con "SIGUE PARA MÁS ¿Y SI…?" y botón de seguir.
- Audio generado: ambiente, música y efectos.

## Videos

| Archivo | Tema |
|---|---|
| `videos/saltar.js` | ¿Y si todos los humanos saltaran a la vez? |
| `videos/sol.js` | ¿Y si el Sol se apagara ahora mismo? |
| `videos/agujero.js` | ¿Y si saltaras por un agujero que atraviesa la Tierra? |
| `videos/girar.js` | ¿Y si la Tierra dejara de girar de golpe? |

Los MP4 terminados están en `final/`.

## Cómo renderizar

```bash
npm install                        # three + fuentes
node render.js saltar --still 1,12,20   # fotogramas sueltos de prueba -> out/stills
./build2.sh saltar                 # video completo -> final/saltar.mp4
```

Requiere Node, Playwright (Chromium), ffmpeg y Python 3 con numpy.

## Crear un tema nuevo

Copia un archivo de `videos/` y cambia:

- `title`, `duration`, `end.fact`
- `stats`: `{ t, label, value, sub }` — `value` puede ser texto o `count(desde, hasta, {dur, suf})`
- `captions`: `{ t, d, text }`
- `audio`: música (`curious`, `tense`, `drone`, `night`, `cold`, `epic`), ambientes y efectos
- `setup(world)` / `update(t, world)`: la escena 3D (todo en función de `t`, sin estado)
