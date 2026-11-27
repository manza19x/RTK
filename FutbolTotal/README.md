# Fútbol Total — prototipo de fútbol 3D (Unity 6 · C#)

Prototipo jugable y ampliable de fútbol en 3D: un partido entre dos equipos ficticios, con control
de teclado o mando, compañeros y rivales con IA, marcador, cronómetro y menús.

> **Estado actual: Fase 9 (interfaz profesional) implementada.** Las Fases 1, 2 y 3 se han probado en Unity y funcionan.
> Las Fases 4 a 9 compilan sin errores, pero **no se han ejecutado dentro del editor de Unity** (en el entorno de
> desarrollo no había Unity disponible). Hasta que se ejecuten las listas de pruebas de [`docs/Fase4.md`](docs/Fase4.md),
> [`docs/Fase5.md`](docs/Fase5.md), [`docs/Fase6.md`](docs/Fase6.md) y [`docs/Fase7.md`](docs/Fase7.md) no hay que dar por hecho que funcionan.

---

## 1. Decisiones principales

Estas son las decisiones tomadas donde el encargo no lo concretaba. Todas se pueden cambiar.

| Tema | Decisión | Motivo |
|---|---|---|
| Formato | **Fútbol 7** (7 contra 7) en un campo de 72 × 46 m | Más legible con cámara lateral, IA más sencilla de afinar y partidos más dinámicos. La formación y el tamaño del campo son datos: pasar a 11 contra 11 es cambiar assets, no código. |
| Entrada | **Nuevo Input System**, con las acciones definidas **en código** (`FootballInput`) | No hace falta generar clases ni configurar assets; todas las asignaciones están en un solo sitio. Siguen siendo `InputAction` normales, así que se podrán reasignar desde Opciones. |
| Movimiento | `Rigidbody` con velocidad controlada por código | Los jugadores chocan entre sí con naturalidad y comparten paso de física con el balón. |
| Balón ↔ jugador | **Sin colisión física** entre balón y futbolistas; la posesión la deciden reglas (`BallPossession`) | Los choques cápsula-esfera dan rebotes imprevisibles. Con reglas, conducir, recibir y robar es estable y ajustable. |
| Física del balón | Rodadura con desaceleración constante propia, sin rozamiento del aire | Los pases se calculan de forma exacta (`KickMath`), y la IA puede usar las mismas fórmulas. |
| Cámara | Retransmisión lateral y elevada que **se desplaza sin girar** | La dirección "arriba" del stick siempre es la misma: no desorienta. |
| Botones de acción | Se **cargan manteniendo** y actúan **al soltar**, una sola vez | Da profundidad (potencia/distancia) y evita repeticiones por mantener pulsado. |
| Render | URP (plantilla *Universal 3D*) o pipeline integrado | El generador de escena detecta el pipeline y elige los shaders. HDRP no está contemplado. |
| Arte | Todo generado por código: jugadores articulados con animación procedural, césped y balón con texturas generadas, estadio con público instanciado; posprocesado de URP | Sin recursos de terceros. Para realismo total hacen falta modelos humanos y animaciones capturadas (ver `docs/Fase6.md`, «Siguiente paso»). |
| Identidad | Equipos ficticios **Club Aurora** (rojo) y **Deportivo Marea** (azul) | Sin marcas ni identidades reales. |
| Interfaz | uGUI **construida por código** al arrancar, con el texto clásico y la fuente integrada | Sin prefabs ni referencias guardadas que se rompan al regenerar escenas, y sin tener que importar los recursos de TextMeshPro. |
| Sonido | **Generado por código** al arrancar (`ProceduralAudio`) | Sin archivos de audio de terceros; cada clip se puede sustituir por uno grabado. |
| Espacio de nombres | Todo el código de juego en `FutbolTotal`; el de editor en `FutbolTotal.EditorTools` | Evita choques de nombres (p. ej. una carpeta `Ball` y la clase `Ball`). Las carpetas separan responsabilidades. |

---

## 2. Arquitectura

```
                ┌──────────────────┐
  Teclado/Mando │  FootballInput   │  acciones del Input System + botones con carga
                └────────┬─────────┘
                         ▼
┌──────────────────────────────────┐        ┌─────────────────────────┐
│ HumanTeamController (1 por humano)│        │  TeamAI + PlayerBrain:   │
│ jugador activo, cambios, órdenes  │        │  un cerebro por jugador  │
└────────────────┬─────────────────┘        └────────────┬────────────┘
                 │  misma API para humano e IA            │
                 ▼                                        ▼
        ┌──────────────── FootballPlayer ────────────────────┐
        │ PlayerMotor (moverse) · PlayerStamina (sprint)      │
        │ PlayerActions (pasar, tirar, entrar)                │
        └───────────────┬────────────────────────────────────┘
                        │ usa
        ┌───────────────▼─────────────┐    ┌──────────────────────────┐
        │ Gameplay (funciones puras):  │    │ Ball + BallPossession    │
        │ PassTargetSelector,          │───▶│ física, conducción,      │
        │ ShotCalculator, KickMath,    │    │ quién controla el balón  │
        │ PlayerSwitchSelector         │    └──────────────────────────┘
        └──────────────────────────────┘
  MatchContext: acceso único a balón, campo y equipos.   Pitch / GoalZone: geometría y detección de gol.
  MatchCamera, SelectionIndicator: presentación.          Data/*.cs: ScriptableObjects con todos los parámetros.
  MatchManager: reglas del partido (estados, reloj, saques). UI: MainMenu, MatchHud, PauseMenu, ResultScreen.
```

Principios:

- **Humano e IA usan la misma API** (`PlayerMotor.SetMoveInput`, `PlayerActions.ShortPass/LongPass/Shoot/TryTackle`).
  Las reglas son idénticas para los dos; la IA solo decide *qué* pedir.
- **Lógica de decisión en funciones puras** (`Gameplay/`): reciben datos y devuelven resultados, sin buscar
  objetos en la escena. Son deterministas (salvo el margen de error del tiro, que siempre se explica por factores visibles).
- **Eventos en lugar de sondeo**: `Ball.OwnerChanged`, `Ball.Kicked`, `GoalZone.Scored`. Las reglas del partido,
  la cámara y la interfaz se suscriben sin que el balón sepa que existen.
- **Parámetros en ScriptableObjects** (`Assets/_Project/Resources/FutbolTotal`), editables en el Inspector incluso en Play. `SettingsProvider` los carga por nombre si un componente no tiene uno asignado.

---

## 3. Estructura de carpetas

```
Assets/_Project/
├── Scripts/                      (FutbolTotal.Runtime.asmdef)
│   ├── Core/          MatchContext, SettingsProvider, GameOptions, SceneNames, IMatchStatus, IMatchControl, FoulEvent
│   ├── Data/          FootballEnums, PlayerMovementSettings, BallSettings, ActionSettings, CameraSettings,
│   │                  TeamSettings, FormationSettings, MatchSettings, AISettings, SoundSettings
│   ├── Audio/         ProceduralAudio, MatchAudio, UISound
│   ├── Visuals/       ProceduralTextures, PlayerRigBuilder, PlayerAnimator, StadiumGenerator, CrowdRenderer,
│   │                  NetMeshGenerator; URP/ VisualQualitySetup (ensamblado opcional, solo con URP)
│   ├── Controls/      FootballInput, ChargeButton, HumanTeamController
│   ├── Players/       FootballPlayer, PlayerMotor, PlayerStamina, PlayerActions
│   ├── Ball/          Ball, BallPossession, BallVisual
│   ├── Teams/         Team
│   ├── Pitch/         Pitch, PitchMeshGenerator, GoalZone
│   ├── Gameplay/      KickMath, PassTargetSelector, ShotCalculator, PlayerSwitchSelector
│   ├── CameraRig/     MatchCamera, MenuCameraOrbit
│   ├── Presentation/  SelectionIndicator
│   ├── Utils/         MeshShapes (FlatMeshBuilder)
│   ├── Match/         MatchManager, MatchState, RestartRules, AutoRestart, MatchStats
│   ├── AI/            TeamAI, PlayerBrain, FormationShape, PassEvaluator, AIQueries, AIRole, DifficultyScaling
│   │   └── Behaviors/ BallCarrier, Support, Press (presión/contención/búsqueda), ZoneCover, Goalkeeper
│   ├── UI/            UIFactory, UISprites, UIButtonFx, MainMenu, MatchHud, PauseMenu, ResultScreen, ControlsPanel
│   └── DevTools/      TrainingHarness (escena de entrenamiento), DebugHud (F2 roles, F3 depuración)
├── Editor/                       (FutbolTotal.Editor.asmdef)
│   ├── EditorAssetUtility.cs
│   ├── SceneBuilder.cs           menú «Fútbol Total ▸ Crear todas las escenas» (y una a una)
│   └── Phase1SceneBuilder.cs     obsoleto y vacío (se puede borrar)
├── Resources/FutbolTotal/ (generado) assets de ajustes, equipos y formación
├── Art/        (generado)  Materials/, Textures/, Physics/
└── Scenes/     (generado)  MenuPrincipal.unity, Partido.unity, Entrenamiento.unity
```

---

## 4. Objetos de escena (escena de partido)

El generador los crea todos; esta tabla sirve para montarlos a mano o entender qué hay.

| Objeto | Componentes | Notas |
|---|---|---|
| `Match` | `MatchContext`, `MatchManager`, `MatchStats`, `DebugHud` | `MatchContext` referencia balón, campo y equipos; `MatchManager` aplica las reglas. En la escena de entrenamiento, `TrainingHarness` sustituye a `MatchManager`. |
| `Pitch` | `Pitch` | Centrado en el origen, sin rotar. Porterías en ±X. |
| ├ `GroundCollider` | `BoxCollider` (PM_Grass) | Cara superior en y = 0. Invisible. |
| ├ `Surround`, `PlayingSurface` | `MeshRenderer` | Solo visuales. |
| ├ `Stripes`, `Lines` | `MeshFilter`, `MeshRenderer`, `PitchMeshGenerator` | Franjas del césped y marcas, generadas desde `Pitch`. |
| ├ `Boundaries` | 4 `BoxCollider` (PM_Wall) | Muros invisibles a 7 m de las líneas (en la Fase 2 habrá saques). |
| └ `Goal_West`, `Goal_East` | Postes y larguero (`CapsuleCollider`), red (`BoxCollider`, PM_Net), `GoalZone` (trigger) | |
| `Ball` | `Rigidbody`, `SphereCollider` (PM_Ball), `Ball`, `BallPossession`, `BallVisual` | Hijo `Visual` con la esfera texturizada. |
| `Team_Home` | `Team`, `TeamAI`, `FootballInput`, `HumanTeamController` | Equipo humano; ataca hacia +X en la 1ª parte. La IA mueve a todos menos al jugador activo. |
| `Team_Away` | `Team`, `TeamAI` | Equipo de la IA; ataca hacia −X en la 1ª parte. |
| └ cada jugador (`AUR_09_Forward`...) | `Rigidbody`, `CapsuleCollider`, `PlayerMotor`, `PlayerStamina`, `PlayerActions`, `FootballPlayer`, `PlayerBrain` | Hijo `Visual` con torso, pantalón, piernas, cabeza y marca frontal. |
| `Main Camera` | `Camera`, `MatchCamera` | |
| `SelectionIndicator` | `MeshFilter`, `MeshRenderer`, `SelectionIndicator` | Anillo y flecha bajo el jugador activo. |
| `Directional Light` | `Light` | Sombras suaves. |
| `Audio` | `MatchAudio` | Sonido del partido: golpeos, botes, postes, red, silbato y público. |
| `UI` | `MatchHud`, `PauseMenu`, `ResultScreen` | Cada uno construye su lienzo al arrancar. Crean también el `EventSystem` (con `InputSystemUIInputModule`). |

Escena `MenuPrincipal`: el campo (sin jugadores), `Main Camera` con `MenuCameraOrbit` y un objeto `MainMenu`.

---

## 5. Controles

| Acción | Teclado | Mando (Xbox / PlayStation) |
|---|---|---|
| Mover | WASD | Stick izquierdo |
| Sprint (gasta resistencia) | Shift izquierdo | RT / R2 |
| Pase corto · sin balón: **entrada** | J | A / Cruz |
| Pase largo / centro | L | X / Cuadrado |
| Tiro | K | B / Círculo |
| Cambiar de jugador | Q | LB / L1 |
| Pausa | Esc | Start / Options |
| Pantalla de controles | F1 | Pulsar stick derecho |
| Ver el rol de la IA de cada jugador | F2 | Pulsar stick izquierdo |
| Panel de depuración | F3 | — |
| Recolocar balón (solo entrenamiento) | R | Select / Share |
| Menús | Flechas o WASD + Intro, o ratón | Stick o cruceta + A |

- **Mantener** pase o tiro carga la potencia; la acción ocurre **al soltar**. Toque corto = pase cercano o tiro suave.
- Pulsar pase/tiro **antes de recibir** guarda la orden 0,35 s: golpeo al primer toque.
- En teclado, la mano derecha queda sobre **J K L** (pase, tiro, pase largo) y la izquierda en WASD + Shift + Q,
  sin cruzar manos. Las flechas no se usan para mover porque chocarían con esa mano.
- En mando se sigue la convención más extendida en juegos de fútbol (A pase, B tiro, X pase alto, LB cambio, RT sprint).

---

## 6. Plan por fases

| Fase | Contenido | Estado |
|---|---|---|
| **1. Movimiento, balón y controles** | Campo y porterías, jugadores, movimiento con aceleración/giro, sprint con resistencia, conducción, pase corto/largo con asistencia, tiro con potencia y error, entrada, cambio de jugador, detección de gol, cámara básica, HUD de pruebas, generador de escena | **Hecha y probada en Unity** |
| **2. Reglas del partido** | `MatchManager` con estados (previa, saque inicial, en juego, saque, gol, descanso, final), cronómetro, cambio de campo, saques de banda/puerta/córner simplificados, pausa, repetir partido | **Hecha y probada en Unity** |
| **3. IA y porteros** | Cerebro por futbolista con comportamientos separados (con balón, apoyo/desmarque, presión, cobertura de zona, portero), coordinación para que solo uno presione, formación dinámica según el balón, etiquetas de rol (F2) | **Hecha y probada en Unity** |
| **4. Interfaz, menús y cámara** | Marcador, reloj y posesión (uGUI), ficha del jugador con resistencia y carga, mensajes, menú principal con opciones (duración, dificultad, asistencia), pantalla de controles, menú de pausa, pantalla de resultado con estadísticas, cámara con zoom según la jugada | **Hecha (pendiente de prueba en Unity)** |
| **5. Sonido y faltas** | Sonidos generados por código (golpeos, botes, postes, red, silbato, público y celebración), volumen en Opciones; faltas por entrada por detrás o sin tocar el balón, tiros libres y penaltis, IA que evita entrar por detrás, faltas en las estadísticas | **Hecha (pendiente de prueba en Unity)** |
| **6. Aspecto visual** | Jugadores articulados con animación (carrera, golpeo, entrada, estirada, celebración), césped con textura y relieve, estadio con gradas, cubiertas, vallas, focos y miles de espectadores animados, redes de hilos, balón clásico, cielo, sol y posprocesado (ACES, bloom, SMAA) | **Hecha (pendiente de prueba en Unity)** |
| **7. Personaje de Mixamo** | Importación automática de un personaje humanoide y 8 animaciones de Mixamo (puestas por el jugador), Animator con mezcla de locomoción y acciones, equipación teñida por equipo; si no hay modelo, se usa el cuerpo de la Fase 6 | **Hecha (pendiente de prueba en Unity)** |
| **8. Pulido** | Pasos sincronizados con la velocidad (sin patinar), dorsales en la espalda, repetición de los goles a cámara lenta con cámara a pie de campo | **Hecha (pendiente de prueba en Unity)** |
| **9. Interfaz profesional** | Estilo de retransmisión: fuente deportiva, paneles redondeados, escudos generados, marcador de TV, radar, avisos/tarjetas/celebración de gol, menú principal, pausa y resultado rediseñados | **Hecha (pendiente de prueba en Unity)** |

> La cámara básica se adelanta a la Fase 1 porque sin ella no se puede probar el movimiento, que además es
> relativo a la cámara. La IA va después de las reglas porque necesita saber en qué estado está el partido
> (saques, posesión, descanso) para decidir.

---

## 7. Limitaciones conocidas

- **Las Fases 4, 5 y 6 no se han probado dentro de Unity.** Compilan (contra stubs de uGUI y URP, ver más abajo);
  las Fases 1-3 sí se han probado.
- Aspecto: los jugadores son figuras articuladas hechas con formas simples, no modelos humanos realistas.
- El equilibrio de la IA necesita ajustarse jugando (`Resources/FutbolTotal/AISettings`); la dificultad del menú
  solo escala unos pocos parámetros del rival.
- Reglas simplificadas: el reloj se para cuando el balón no está en juego; no hay tiempo añadido, fuera de juego,
  tarjetas ni ley de la ventaja; el saque de banda se hace con el pie; en los penaltis contra ti para tu portero la IA.
- Los sonidos son sintéticos y sencillos (no hay música ni narración); sirven de referencia hasta tener audio grabado.
- Los controles no se pueden reasignar desde el menú todavía (las acciones ya lo permiten con
  `PerformInteractiveRebinding`).
- Las opciones solo están en el menú principal; durante el partido, la pausa permite reiniciar, ver controles o salir.
- Sin animaciones: los cuerpos son primitivas que giran y se desplazan.
- Las entradas no tienen faltas: si fallas, tu jugador queda aturdido un instante.
- El tiro incluye un margen de error aleatorio; el tamaño del margen se explica siempre por distancia, presión,
  perfil del cuerpo y sprint (ver `ShotCalculator`).

### Cómo se verificó el código

Sin Unity disponible, se compiló todo el código (juego y editor) con el SDK de .NET contra las DLL de referencia
`UnityEngine.dll`/`UnityEditor.dll` de Unity 2021, renombrando automáticamente las APIs que cambiaron de nombre en
Unity 6 (`linearVelocity`→`velocity`, `PhysicsMaterial`→`PhysicMaterial`...) y con stubs mínimos del Input System
y de uGUI y URP (que son paquetes y no están en esas DLL). Las texturas generadas se revisaron con una réplica
en Python del mismo algoritmo.
Resultado: 0 errores, 0 avisos. Esto detecta errores de sintaxis y de tipos, **no** errores de comportamiento.

---

## 8. Puesta en marcha

Instrucciones detalladas y listas de pruebas: [`docs/Fase1.md`](docs/Fase1.md), [`docs/Fase2.md`](docs/Fase2.md),
[`docs/Fase3.md`](docs/Fase3.md), [`docs/Fase4.md`](docs/Fase4.md), [`docs/Fase5.md`](docs/Fase5.md) y [`docs/Fase6.md`](docs/Fase6.md).

Resumen:

1. Crea un proyecto nuevo en Unity 6 con la plantilla **Universal 3D** (incluye el Input System).
2. Copia la carpeta `Assets/_Project` de este repositorio dentro de `Assets/` de tu proyecto.
3. Menú **Fútbol Total ▸ Crear todas las escenas** y pulsa **Play** (empieza en el menú principal).

## 9. Criterios de aceptación

| Criterio | Dónde se cumple |
|---|---|
| Se puede iniciar y terminar un partido | Menú principal ▸ Jugar; `MatchManager` (previa, dos partes, final) y `ResultScreen` |
| Mover, conducir, pasar, correr y tirar | `PlayerMotor`, `Ball` (conducción), `PlayerActions`, `PlayerStamina` |
| Balón estable y goles detectados | `Ball`, `BallPossession`, `GoalZone` (balón entero dentro) |
| La IA participa sin quedarse quieta ni amontonarse | `TeamAI` (un solo jugador al balón) y comportamientos de `Scripts/AI/Behaviors` |
| Marcador y cronómetro se actualizan | `MatchManager` + `MatchHud` |
| Pausar y reanudar sin romper el estado | `MatchManager.SetPaused` (escala de tiempo, entradas descartadas) + `PauseMenu` |
| Controles explicados en el juego | `ControlsPanel` (menú, pausa y F1) |
| Implementación y limitaciones documentadas | Este README y `docs/Fase1-6.md` |
