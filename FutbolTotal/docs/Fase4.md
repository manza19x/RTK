# Fase 4 — Interfaz, menús y cámara

Objetivo: que el prototipo se pueda jugar de principio a fin sin tocar el editor: menú principal con opciones,
marcador y reloj claros, mensajes del partido, menú de pausa, pantalla de controles, pantalla de resultado
y una cámara que acompaña la jugada.

## Decisiones

| Tema | Decisión |
|---|---|
| Tecnología | uGUI construida **por código** al arrancar (`UIFactory`). Sin prefabs ni referencias en escena: regenerar escenas no rompe nada. |
| Texto | `UnityEngine.UI.Text` con la fuente integrada (`LegacyRuntime.ttf`). TextMeshPro queda para cuando haya arte definitivo (obliga a importar recursos). |
| Navegación | `EventSystem` + `InputSystemUIInputModule` con sus acciones por defecto: ratón, flechas/WASD + Intro y stick/cruceta + A. |
| Opciones | Duración (corta 0,6× / normal / larga 1,6× de `MatchSettings`), dificultad (fácil / normal / difícil) y asistencia de pase (baja / normal / alta). Se guardan con `PlayerPrefs`. «Normal» usa siempre los assets de ajustes. |
| Dificultad | Escala solo al **equipo rival** (copia de `AISettings`): tiempo de reacción de entradas y portero, estirada, ritmo de decisión y distancia de tiro. Sin azar. |
| Sonido | No hay audio en el prototipo, así que no hay opción de volumen. |
| Cámara | Se acerca (hasta 0,85×) con el balón cerca de una portería y más (0,65×) al celebrar un gol; siempre sin girar. |
| HUD técnico | `DebugHud` pasa a ser solo de desarrollo: F2 roles de la IA, F3 panel técnico. La ayuda y los mensajes van al HUD nuevo. |

## Pantallas

| Pantalla | Contenido |
|---|---|
| Menú principal (`MainMenu`) | Título, Jugar, Controles, Opciones, Salir. Fondo: el campo con una cámara girando. |
| Opciones | Tres botones que cambian de valor al pulsarlos (duración, dificultad, asistencia) y Volver. |
| Controles (`ControlsPanel`) | Tabla acción / teclado / mando y notas sobre cargar y golpear al primer toque. Desde el menú, la pausa y F1. |
| HUD (`MatchHud`) | Marcador con colores de equipo, reloj y parte, quién tiene el balón, ficha del jugador (dorsal, puesto, resistencia, carga del botón; en rojo si el tiro va pasado de potencia), franja de mensajes y pista de controles. |
| Pausa (`PauseMenu`) | Reanudar, Reiniciar partido, Controles, Menú principal. |
| Resultado (`ResultScreen`) | Victoria / Derrota / Empate, marcador, goles con minuto y dorsal (o «en propia puerta»), posesión y tiros. Jugar otra vez o Menú principal. |

## Archivos

Nuevos:

| Ruta | Responsabilidad |
|---|---|
| `Scripts/Core/GameOptions.cs` | Opciones del jugador guardadas en PlayerPrefs |
| `Scripts/Core/SceneNames.cs` | Nombres de escena y carga segura (`SceneLoader`) |
| `Scripts/Core/IMatchControl.cs` | Órdenes de la pausa a las reglas (reanudar, reiniciar) |
| `Scripts/UI/UIFactory.cs` | Construcción de lienzos, textos, botones, barras; colores (`UITheme`) |
| `Scripts/UI/MainMenu.cs` | Menú principal y opciones |
| `Scripts/UI/ControlsPanel.cs` | Pantalla de controles |
| `Scripts/UI/MatchHud.cs` | Interfaz durante el partido |
| `Scripts/UI/PauseMenu.cs` | Menú de pausa |
| `Scripts/UI/ResultScreen.cs` | Pantalla de resultado |
| `Scripts/Match/MatchStats.cs` | Goles, posesión y tiros |
| `Scripts/AI/DifficultyScaling.cs` | Ajuste del rival según la dificultad |
| `Scripts/CameraRig/MenuCameraOrbit.cs` | Cámara del menú principal |

Modificados:

| Ruta | Cambio |
|---|---|
| `Scripts/FutbolTotal.Runtime.asmdef` | Referencia a `UnityEngine.UI` |
| `Scripts/Match/MatchManager.cs` | Duración según Opciones, evento `MatchStarted`, `CurrentMinute`, implementa `IMatchControl`; el final ya no se reinicia con J (lo hace la pantalla de resultado) |
| `Scripts/DevTools/TrainingHarness.cs` | Implementa `IMatchControl` |
| `Scripts/AI/TeamAI.cs` | El rival usa una copia de `AISettings` ajustada a la dificultad |
| `Scripts/Players/PlayerActions.cs` | El jugador humano usa la asistencia de pase elegida |
| `Scripts/CameraRig/MatchCamera.cs`, `Scripts/Data/CameraSettings.cs` | Zoom según la jugada y al celebrar |
| `Scripts/Controls/FootballInput.cs` | F3 (panel técnico) |
| `Scripts/DevTools/DebugHud.cs` | Solo herramientas de desarrollo (F2, F3), ocultas al empezar |
| `Editor/SceneBuilder.cs` | Escena de menú principal, interfaz en las escenas de juego, «Crear todas las escenas», orden de Build Settings |

## Montaje en Unity

1. Descarga la versión de esta fase (enlace en el mensaje de entrega) y copia `FutbolTotal/Assets/_Project` sobre la
   carpeta `Assets` del proyecto, eligiendo **«Reemplazar los archivos en el destino»**.
2. Espera a que compile; la consola no debe tener errores rojos. Si sale «UnityEngine.UI no encontrado», instala el
   paquete **Unity UI** (Window ▸ Package Manager), aunque la plantilla Universal 3D ya lo trae.
3. **Fútbol Total ▸ Crear todas las escenas** (Don't Save si pregunta). Crea `MenuPrincipal`, `Partido` y
   `Entrenamiento`, las añade a Build Settings en ese orden y deja abierto el menú.
4. **▶ Play**: empieza en el menú principal.

Escenas antiguas que ya no se usan y se pueden borrar: `Fase1_Entrenamiento.unity`.

## Lista de pruebas

**Menú principal**
- [ ] Se ve el título sobre el campo y la cámara gira despacio.
- [ ] Se navega con ratón, con flechas + Intro y con el mando (stick + A).
- [ ] Opciones: cada botón cambia su valor al pulsarlo; al salir del Play y volver a entrar, los valores se recuerdan.
- [ ] Controles: se ve la tabla; Volver regresa al menú.
- [ ] Jugar carga el partido.

**HUD**
- [ ] Marcador arriba con colores de equipo, reloj y parte debajo, y «Balón: …» según quién lo tiene.
- [ ] Abajo a la izquierda: dorsal y puesto del jugador activo y su resistencia; al mantener J, L o K aparece la barra de carga (roja si el tiro va pasado).
- [ ] Los mensajes (previa, saques, gol, descanso, cambio de campo) aparecen en la franja central.
- [ ] F1 abre y cierra la pantalla de controles.

**Pausa**
- [ ] Esc / Start abre el menú de pausa; Reanudar continúa donde estaba.
- [ ] Reiniciar partido vuelve a 0-0 con el saque inicial.
- [ ] Controles y Volver funcionan; Menú principal vuelve al menú.

**Final**
- [ ] Al acabar aparece la pantalla de resultado con Victoria / Derrota / Empate, goles con minuto y dorsal, posesión y tiros.
- [ ] Jugar otra vez empieza un nuevo partido; Menú principal vuelve al menú.

**Opciones aplicadas**
- [ ] Duración corta/larga cambia el tiempo real de cada parte (el reloj siempre llega a 45' y 90').
- [ ] En difícil, el rival roba antes y su portero reacciona antes; en fácil, al revés.
- [ ] Asistencia alta: los pases van exactos al compañero; baja: cuenta más la dirección que marques.

**Cámara**
- [ ] Se acerca un poco al acercarse el balón a una portería y más al celebrar un gol.

**Herramientas de desarrollo**
- [ ] F2 muestra los roles de la IA; F3 el panel técnico.
