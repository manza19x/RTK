# Fase 9 — Interfaz profesional

Objetivo: que menús y marcador tengan el aspecto de un juego deportivo comercial (estilo retransmisión),
sin recursos externos: todo se genera por código.

## Estilo

| Elemento | Decisión |
|---|---|
| Tipografía | **Bahnschrift** (condensada, de marcador deportivo; incluida en Windows 10/11). Si no existe, Segoe UI o Arial; en último caso la de Unity. |
| Paleta | Paneles azul noche translúcidos, acento **turquesa**, dorado para potencia y goles, rojo para «en directo» y avisos. Todo en `UITheme`. |
| Formas | Rectángulos redondeados escalables (9-slice), píldoras, círculos, anillos, triángulos y degradados generados (`UISprites`). |
| Escudos | Escudo inventado por equipo (forma clásica, borde, banda diagonal y rombo) con sus colores. Sin marcas reales. |
| Botones | Al pasar el ratón o seleccionarlos con mando: se rellenan de turquesa, crecen un poco y muestran una barra de acento (`UIButtonFx`). |

## Pantallas

| Pantalla | Contenido |
|---|---|
| Menú principal | Logotipo «FÚTBOL TOTAL» con entrada animada, botones grandes (Jugar partido, Entrenamiento, Controles, Opciones, Salir), tarjeta del partido con escudos, «VS» y ajustes elegidos, barra de ayuda. Esc / B vuelve atrás. |
| Opciones | Duración, dificultad, ayuda al pasar y volumen con el formato «< valor >». |
| Marcador | Arriba a la izquierda: escudos, abreviaturas, resultado (salta y se ilumina al marcar), reloj sobre fondo turquesa, barra bajo el equipo con balón y etiqueta de la parte con punto rojo parpadeante cuando el balón está en juego. |
| Avisos | Saques y faltas: píldora breve arriba. Previa, descanso, cambio de campo y final: tarjeta central. Gol: banda a todo lo ancho con el color del equipo, «¡GOL!» grande y equipo, dorsal y minuto. Repetición: tarjeta «● REPETICIÓN». |
| Jugador | Abajo a la izquierda: dorsal grande, puesto, resistencia y barra de potencia (roja si el tiro va pasado). Etiqueta con el dorsal sobre el jugador controlado. |
| Radar | Abajo en el centro: minicampo con todos los jugadores (colores de equipo, porteros aparte), el balón y un anillo en tu jugador. |
| Pausa | Panel lateral con «PAUSA», marcador actual y botones. |
| Resultado | Cinta con «¡VICTORIA!», «DERROTA» o «EMPATE», escudos, marcador grande, goles con minuto y barras comparadas de posesión, tiros y faltas. |
| Controles | Tabla con las teclas y botones dentro de «píldoras». |

## Archivos

Nuevos: `Scripts/UI/UISprites.cs`, `Scripts/UI/UIButtonFx.cs`.
Reescritos: `Scripts/UI/MatchHud.cs`, `MainMenu.cs`, `PauseMenu.cs`, `ResultScreen.cs`, `ControlsPanel.cs`.
Modificados: `Scripts/UI/UIFactory.cs` (tema, fuente, sprites, botones), `Scripts/Core/IMatchStatus.cs` (`MessageStyle`),
`Scripts/Match/MatchManager.cs` y `Scripts/DevTools/TrainingHarness.cs` (estilo de cada mensaje).

No hace falta regenerar escenas para ver la nueva interfaz (se construye al arrancar), pero sí para que el menú tenga el
botón «Entrenamiento» funcionando (la escena debe estar en Build Settings: «Crear todas las escenas»).

## Pruebas

- [ ] El menú usa la fuente condensada, se ve el logo, la tarjeta del partido con escudos y los botones se iluminan en turquesa.
- [ ] Opciones cambia los valores y Esc / B vuelve.
- [ ] En el partido: marcador arriba a la izquierda, radar abajo, ficha del jugador y etiqueta con su dorsal.
- [ ] Los saques salen como aviso pequeño; el descanso y el final como tarjeta; los goles con la banda de color.
- [ ] Pausa y resultado con el nuevo diseño.
