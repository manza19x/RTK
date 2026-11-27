# Fase 3 — IA de jugadores y porteros

Objetivo: que compañeros y rivales jueguen solos de forma comprensible: el equipo mantiene su forma, solo uno
va a por el balón, los demás apoyan o defienden su zona, el jugador con balón decide entre tirar, pasar o
conducir, y el portero se coloca e intenta parar.

## Arquitectura

```
TeamAI (uno por equipo)                     PlayerBrain (uno por jugador)
 └ cada 0,25 s o al cambiar la posesión      └ ejecuta el comportamiento de su rol
   reparte UN rol a cada jugador:              (nunca si lo controla el humano
   ┌──────────────┬───────────────────────┐     ni con el balón fuera de juego)
   │ Situación     │ Roles                 │
   ├──────────────┼───────────────────────┤   Comportamientos (Scripts/AI/Behaviors):
   │ Tenemos balón │ BALÓN + APOYO         │    BallCarrierBehavior  jugador con balón
   │ Balón suelto  │ 1 BUSCA + resto forma │    SupportBehavior      apoyo / desmarque
   │ Lo tiene el   │ 1 PRESIÓN (o CUBRE)   │    PressBehavior        presión / contención / búsqueda
   │ rival         │ + ZONA con marcas     │    ZoneCoverBehavior    defensa de zona
   │ Siempre       │ POR (portero)         │    GoalkeeperBehavior   portero
   └──────────────┴───────────────────────┘
FormationShape: puesto de cada jugador según dónde está el balón y quién lo tiene.
PassEvaluator / AIQueries: valoraciones compartidas (líneas de pase, ángulo de tiro, predicción del balón).
```

Decisiones:

| Tema | Decisión |
|---|---|
| Quién va al balón | Solo **uno** por equipo: el que antes llega (balón suelto), el receptor de un pase, o el más cercano al conductor rival. |
| Compañeros del humano | No hacen entradas (rol «CUBRE»): se colocan entre el rival y la portería. Las entradas las haces tú. Si estás cerca del balón, no ayudan para no estorbar. |
| Forma del equipo | El bloque sigue al balón a lo largo del campo, se adelanta con balón, se repliega sin él y se cierra hacia el lado del balón. Los defensas nunca quedan por delante del balón al defender. |
| Jugador con balón | Decide cada 0,35 s (antes si le presionan): **tirar** si está a menos de 22 m con ángulo y línea libre; **pasar** si está presionado o el pase avanza al menos 6 m; si no, **conducir** esquivando. Despeja si le acosan en su tercio sin opción. |
| Entradas de la IA | Solo tras mantener el balón a su alcance 0,35 s: si conduces y cambias de dirección, puedes superarle. |
| Portero | Se coloca entre balón y portería; reacciona a los tiros 0,25 s después del golpeo, da un paso o se estira; sale a por balones sueltos en el área si llega antes y al encuentro del delantero que entra solo. Con el balón en las manos espera 0,8 s y saca. Nadie puede robárselo en su área. |
| Aleatoriedad | Ninguna en las decisiones. Lo único aleatorio sigue siendo el margen de error del tiro, que se explica por distancia, presión, perfil y sprint. |
| Visibilidad | **F2** (o L3) muestra sobre cada jugador su rol: POR, BALÓN, APOYO, BUSCA, PRESIÓN, CUBRE, ZONA o TÚ. |

## Archivos

Nuevos:

| Ruta | Responsabilidad |
|---|---|
| `Scripts/Data/AISettings.cs` | Todos los parámetros de la IA |
| `Scripts/AI/AIRole.cs` | Roles y sus etiquetas |
| `Scripts/AI/TeamAI.cs` | Reparto de roles y marcas por equipo |
| `Scripts/AI/PlayerBrain.cs` | Ejecuta el rol de cada jugador y guarda su memoria |
| `Scripts/AI/FormationShape.cs` | Posición en la formación según la jugada |
| `Scripts/AI/PassEvaluator.cs` | Valoración y ejecución de pases de la IA |
| `Scripts/AI/AIQueries.cs` | Consultas espaciales compartidas |
| `Scripts/AI/Behaviors/IPlayerBehavior.cs` | Interfaz y registro de comportamientos |
| `Scripts/AI/Behaviors/BallCarrierBehavior.cs` | Jugador con balón |
| `Scripts/AI/Behaviors/SupportBehavior.cs` | Apoyo y desmarque |
| `Scripts/AI/Behaviors/PressBehavior.cs` | Presión, contención y búsqueda del balón |
| `Scripts/AI/Behaviors/ZoneCoverBehavior.cs` | Defensa de zona con marca |
| `Scripts/AI/Behaviors/GoalkeeperBehavior.cs` | Portero |

Modificados:

| Ruta | Cambio |
|---|---|
| `Scripts/Core/IMatchStatus.cs` | `IsBallInPlay`: la IA solo actúa con el balón en juego |
| `Scripts/Core/MatchContext.cs` | Registro de las reglas activas (`Rules`) |
| `Scripts/Match/MatchManager.cs`, `Scripts/DevTools/TrainingHarness.cs` | Se registran como reglas e implementan `IsBallInPlay` |
| `Scripts/Players/PlayerMotor.cs` | `SetFacing`: orientarse sin moverse (mirar al balón) |
| `Scripts/Players/PlayerActions.cs` | No se puede robar al portero con el balón en su área |
| `Scripts/Controls/FootballInput.cs` | Acción F2 / L3 para ver roles |
| `Scripts/DevTools/DebugHud.cs` | Etiquetas de rol sobre los jugadores y ayuda actualizada |
| `Scripts/Core/SettingsProvider.cs` | Nombre del asset `AISettings` |
| `Editor/SceneBuilder.cs` | Añade `TeamAI` a los equipos, `PlayerBrain` a los jugadores y crea `AISettings` |

## Montaje en Unity

1. Descarga la versión de esta fase (enlace en el mensaje de entrega) y copia `FutbolTotal/Assets/_Project` sobre la
   carpeta `Assets` del proyecto, eligiendo **«Reemplazar los archivos en el destino»**.
2. Espera a que compile y comprueba que la consola no tiene errores rojos.
3. **Fútbol Total ▸ Crear escena de partido** (es necesario: la escena nueva lleva la IA). **Don't Save** si pregunta.
4. **▶ Play**, clic dentro de la ventana *Game*, **J** para empezar y **F2** para ver los roles.

Si alguna escena antigua no tuviera `PlayerBrain` en los jugadores, `TeamAI` lo añade al arrancar.

## Lista de pruebas

**Forma y reparto**
- [ ] Con F2 activo, en cada equipo hay como mucho **un** jugador con PRESIÓN/CUBRE o BUSCA; los demás APOYO o ZONA.
- [ ] Cuando tu equipo tiene el balón, tus compañeros se abren y avanzan; al perderlo, se repliegan.
- [ ] Tras una jugada lejana, los jugadores vuelven a ocupar su zona.
- [ ] Nadie se queda quieto mucho tiempo con el balón en juego, y no se forman montones alrededor del balón.

**Rival con balón**
- [ ] El rival con balón avanza, esquiva y pasa cuando le presionas.
- [ ] Tira cuando llega cerca del área con ángulo; desde lejos o sin ángulo, no.
- [ ] Si conduces y un rival se te pega, intenta robarte tras un instante; cambiando de dirección puedes superarle.

**Compañeros**
- [ ] Tus compañeros buscan huecos libres para recibir (sobre todo los delanteros).
- [ ] Al pasar, el receptor va hacia el balón.

**Porteros**
- [ ] El portero rival se mueve con el balón, cubriendo el centro de la portería.
- [ ] Ante tus tiros, se estira hacia el lado del balón; para unos y otros entran (sobre todo los ajustados al palo).
- [ ] Sale a por balones sueltos dentro de su área cuando llega antes.
- [ ] Con el balón en las manos espera un momento y lo saca; no puedes robárselo.
- [ ] Tu portero hace lo mismo cuando tira el rival.

## Ajustes útiles

Todos en `Assets/_Project/Resources/FutbolTotal/AISettings` (se pueden cambiar durante el Play):

| Si... | Cambia |
|---|---|
| El rival roba demasiado fácil | `tackleReactionDelay` ↑ (0,5) o `tackleRange` ↓ |
| El rival tira poco / demasiado | `shootRange`, `minShootAngle` |
| El rival pasa demasiado / poco | `passScoreFree`, `minProgressWhenFree` |
| Los porteros paran demasiado | `keeperReaction` ↑, `diveSpeed` ↓ |
| Los porteros paran poco | `keeperReaction` ↓, `diveSpeed` ↑ |
| El equipo se estira o se junta demasiado | `blockShift`, `lateralCompression` |
