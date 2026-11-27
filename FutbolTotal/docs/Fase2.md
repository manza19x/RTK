# Fase 2 — Reglas del partido

Objetivo: jugar un partido completo de principio a fin: previa, saque inicial, dos partes con cronómetro,
goles, saques de banda, de puerta y córners, descanso con cambio de campo, pausa y final con opción de repetir.
La IA de los jugadores todavía no existe (Fase 3): los rivales solo se mueven para sacar.

## Decisiones de esta fase

| Tema | Decisión |
|---|---|
| Duración | 2 partes de **150 s reales**; el marcador muestra 0-45 y 45-90 minutos (`MatchSettings`). |
| Reloj | Solo corre con el balón en juego (estilo arcade). No hay tiempo añadido. |
| Fuera | El balón está fuera cuando cruza la línea **por completo**. El que no lo tocó el último saca. |
| Saques | Banda (con el pie), puerta (desde el borde del área pequeña, lo saca el portero) y córner. Los rivales se apartan 6 m (9,15 m es demasiado en fútbol 7). |
| Durante un saque | Nadie se desplaza; el lanzador puede **girar para apuntar**. El juego se reanuda cuando golpea el balón. |
| Saques de la IA | Decisiones fijas y previsibles: saque inicial atrás, banda corto hacia delante, puerta en largo, córner al punto de penalti. |
| Saque inicial | Lo hace el local en la 1ª parte, el visitante en la 2ª y, tras un gol, el que lo ha encajado. |
| Descanso | 3 s, cambio de campo y aviso de hacia dónde atacas. |
| Final | Mensaje con el resultado; **J / A** para jugar otra vez. La pantalla de resultado definitiva llega en la Fase 4. |

## Archivos

Nuevos:

| Ruta | Responsabilidad |
|---|---|
| `Assets/_Project/Scripts/Data/MatchSettings.cs` | Duración, pausas y distancia de saques |
| `Assets/_Project/Scripts/Core/IMatchStatus.cs` | Lo que el HUD necesita saber del partido (marcador, reloj, mensajes) |
| `Assets/_Project/Scripts/Match/MatchState.cs` | Estados del partido y tipos de saque |
| `Assets/_Project/Scripts/Match/MatchManager.cs` | **Sistema central de reglas**: estados, reloj, goles, saques, descanso, final, pausa |
| `Assets/_Project/Scripts/Match/RestartRules.cs` | Cuándo sale el balón, qué saque toca, dónde y quién saca |
| `Assets/_Project/Scripts/Match/AutoRestart.cs` | Cómo saca un equipo controlado por la IA |
| `Assets/_Project/Editor/SceneBuilder.cs` | Generador de escenas (partido y entrenamiento); sustituye a `Phase1SceneBuilder.cs` |

Modificados:

| Ruta | Cambio |
|---|---|
| `Scripts/Players/PlayerMotor.cs` | `MovementLocked`: el jugador no se desplaza pero puede girar (saques) |
| `Scripts/Teams/Team.cs` | `SetMovementLocked` para bloquear a todo el equipo |
| `Scripts/Controls/HumanTeamController.cs` | `SwitchingEnabled`: impide cambiar de jugador mientras sacas |
| `Scripts/Core/SettingsProvider.cs` | Nombre del asset `MatchSettings` |
| `Scripts/DevTools/DebugHud.cs` | Lee cualquier `IMatchStatus`: muestra reloj, parte y mensajes de varias líneas |
| `Scripts/DevTools/TrainingHarness.cs` | Implementa `IMatchStatus` (para la escena de entrenamiento) |
| `Editor/Phase1SceneBuilder.cs` | Vaciado (obsoleto). Se puede borrar |

## Montaje en Unity

1. Descarga la versión de esta fase (enlace en el mensaje de entrega) y copia `FutbolTotal/Assets/_Project` sobre la
   carpeta `Assets` del proyecto, eligiendo **«Reemplazar los archivos en el destino»**.
2. Espera a que Unity compile. La consola no debe mostrar errores rojos.
3. Menú **Fútbol Total ▸ Crear escena de partido** (si pregunta, **Don't Save**). Se crea
   `Assets/_Project/Scenes/Partido.unity` y el asset `Resources/FutbolTotal/MatchSettings`.
4. Pulsa **▶ Play** y haz clic dentro de la ventana *Game*.

La escena antigua `Fase1_Entrenamiento.unity` ya no se usa y se puede borrar. Si quieres probar controles sin reloj
ni saques, usa **Fútbol Total ▸ Crear escena de entrenamiento**.

Para probar el final sin esperar 5 minutos: durante el Play abre `Resources/FutbolTotal/MatchSettings` y pon
**Half Duration Seconds** en 20. Vuelve a dejarlo en 150 después (los cambios hechos en Play se guardan).

## Lista de pruebas

**Inicio**
- [ ] Al pulsar Play aparece «Club Aurora vs Deportivo Marea · Pulsa J / A para empezar» y nadie se mueve.
- [ ] Al pulsar J empieza el saque inicial: tu jugador está junto al balón en el centro, los rivales fuera del círculo.
- [ ] Durante el saque no puedes desplazarte, pero sí girar con WASD para apuntar; con J o L sacas y empieza el juego.

**Reloj y marcador**
- [ ] El panel muestra «AUR 0 - 0 MAR   00:00  1ª parte» y el reloj avanza solo con el balón en juego.

**Saques**
- [ ] Balón por la banda: «Saque de banda para …». Si te toca, tu jugador aparece en la línea y apuntas y pasas.
- [ ] Si toca sacar al rival, a los ~1,2 s saca solo hacia un compañero.
- [ ] Balón por la línea de fondo rival tocado por ti: saque de puerta del portero rival.
- [ ] Balón por tu línea de fondo tocado por ti: córner para el rival, que centra al área.
- [ ] Balón por encima del larguero: también es saque de puerta o córner, no gol.
- [ ] En los saques, los rivales quedan a unos 6 m del balón.

**Goles**
- [ ] Al marcar: «¡GOL de Club Aurora!» con el resultado, el juego se detiene 3 s y saca el rival desde el centro.
- [ ] Un gol en propia puerta suma al rival.

**Descanso y final**
- [ ] Al llegar a 45:00: «Descanso», cambio de campo y aviso «Ahora atacas hacia la izquierda»; saca el visitante.
- [ ] Al llegar a 90:00: «FINAL · AUR x - y MAR». J / A empieza otro partido desde 0-0.

**Pausa**
- [ ] Esc / Start pausa y reanuda en cualquier momento del partido (no en la previa ni en el final).
- [ ] Mantener J durante la pausa y soltarlo al reanudar no hace nada.
