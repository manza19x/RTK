# Fútbol Total — prototipo de fútbol 3D (Unity 6 · C#)

Prototipo jugable y ampliable de fútbol en 3D: un partido entre dos equipos ficticios, con control
de teclado o mando, compañeros y rivales con IA, marcador, cronómetro y menús.

> **Estado actual: Fase 1 de 5 (movimiento, balón y controles).**
> El código compila sin errores ni avisos contra las DLL de referencia de Unity, pero **no se ha ejecutado
> dentro del editor de Unity** (en el entorno de desarrollo no había Unity disponible). Hasta que se ejecute
> la lista de pruebas de [`docs/Fase1.md`](docs/Fase1.md) no hay que dar por hecho que funciona.

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
| Arte | Formas primitivas y materiales de color liso con nombres claros (`M_Home_Kit`, `M_Ball`...) | Se sustituyen fácilmente por modelos y materiales definitivos. |
| Identidad | Equipos ficticios **Club Aurora** (rojo) y **Deportivo Marea** (azul) | Sin marcas ni identidades reales. |
| Espacio de nombres | Todo el código de juego en `FutbolTotal`; el de editor en `FutbolTotal.EditorTools` | Evita choques de nombres (p. ej. una carpeta `Ball` y la clase `Ball`). Las carpetas separan responsabilidades. |

---

## 2. Arquitectura

```
                ┌──────────────────┐
  Teclado/Mando │  FootballInput   │  acciones del Input System + botones con carga
                └────────┬─────────┘
                         ▼
┌──────────────────────────────────┐        ┌─────────────────────────┐
│ HumanTeamController (1 por humano)│        │  IA (Fase 2): un cerebro │
│ jugador activo, cambios, órdenes  │        │  por futbolista          │
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
│   ├── Core/          MatchContext, SettingsProvider
│   ├── Data/          FootballEnums, PlayerMovementSettings, BallSettings, ActionSettings,
│   │                  CameraSettings, TeamSettings, FormationSettings
│   ├── Controls/      FootballInput, ChargeButton, HumanTeamController
│   ├── Players/       FootballPlayer, PlayerMotor, PlayerStamina, PlayerActions
│   ├── Ball/          Ball, BallPossession, BallVisual
│   ├── Teams/         Team
│   ├── Pitch/         Pitch, PitchMeshGenerator, GoalZone
│   ├── Gameplay/      KickMath, PassTargetSelector, ShotCalculator, PlayerSwitchSelector
│   ├── CameraRig/     MatchCamera
│   ├── Presentation/  SelectionIndicator
│   ├── Utils/         MeshShapes (FlatMeshBuilder)
│   └── DevTools/      TrainingHarness, DebugHud      (provisionales de la Fase 1)
├── Editor/                       (FutbolTotal.Editor.asmdef)
│   ├── EditorAssetUtility.cs
│   └── Phase1SceneBuilder.cs     menú «Fútbol Total ▸ Crear escena de entrenamiento (Fase 1)»
├── Resources/FutbolTotal/ (generado) assets de ajustes, equipos y formación
├── Art/        (generado)  Materials/, Textures/, Physics/
└── Scenes/     (generado)  Fase1_Entrenamiento.unity
```

Carpetas previstas para fases siguientes: `Scripts/Match/` (reglas del partido), `Scripts/AI/`, `Scripts/UI/`,
`Scenes/MainMenu.unity`, `Scenes/Match.unity`.

---

## 4. Objetos de escena (Fase 1)

El generador los crea todos; esta tabla sirve para montarlos a mano o entender qué hay.

| Objeto | Componentes | Notas |
|---|---|---|
| `Match` | `MatchContext`, `TrainingHarness`, `DebugHud` | `MatchContext` referencia balón, campo y equipos. |
| `Pitch` | `Pitch` | Centrado en el origen, sin rotar. Porterías en ±X. |
| ├ `GroundCollider` | `BoxCollider` (PM_Grass) | Cara superior en y = 0. Invisible. |
| ├ `Surround`, `PlayingSurface` | `MeshRenderer` | Solo visuales. |
| ├ `Stripes`, `Lines` | `MeshFilter`, `MeshRenderer`, `PitchMeshGenerator` | Franjas del césped y marcas, generadas desde `Pitch`. |
| ├ `Boundaries` | 4 `BoxCollider` (PM_Wall) | Muros invisibles a 7 m de las líneas (en la Fase 2 habrá saques). |
| └ `Goal_West`, `Goal_East` | Postes y larguero (`CapsuleCollider`), red (`BoxCollider`, PM_Net), `GoalZone` (trigger) | |
| `Ball` | `Rigidbody`, `SphereCollider` (PM_Ball), `Ball`, `BallPossession`, `BallVisual` | Hijo `Visual` con la esfera texturizada. |
| `Team_Home` | `Team`, `FootballInput`, `HumanTeamController` | Equipo humano; ataca hacia +X. |
| `Team_Away` | `Team` | Ataca hacia −X. En la Fase 1 sus jugadores están quietos. |
| └ cada jugador (`AUR_09_Forward`...) | `Rigidbody`, `CapsuleCollider`, `PlayerMotor`, `PlayerStamina`, `PlayerActions`, `FootballPlayer` | Hijo `Visual` con torso, pantalón, piernas, cabeza y marca frontal. |
| `Main Camera` | `Camera`, `MatchCamera` | |
| `SelectionIndicator` | `MeshFilter`, `MeshRenderer`, `SelectionIndicator` | Anillo y flecha bajo el jugador activo. |
| `Directional Light` | `Light` | Sombras suaves. |

---

## 5. Controles

| Acción | Teclado | Mando (Xbox / PlayStation) |
|---|---|---|
| Mover | WASD | Stick izquierdo |
| Sprint (gasta resistencia) | Shift izquierdo | RT / R2 |
| Pase corto · sin balón: **entrada** | J | A / ✕ |
| Pase largo / centro | L | X / □ |
| Tiro | K | B / ○ |
| Cambiar de jugador | Q | LB / L1 |
| Pausa | Esc | Start / Options |
| Ayuda de controles | F1 | Pulsar stick derecho |
| Recolocar balón (pruebas) | R | Select / Share |

- **Mantener** pase o tiro carga la potencia; la acción ocurre **al soltar**. Toque corto = pase cercano o tiro suave.
- Pulsar pase/tiro **antes de recibir** guarda la orden 0,35 s: golpeo al primer toque.
- En teclado, la mano derecha queda sobre **J K L** (pase, tiro, pase largo) y la izquierda en WASD + Shift + Q,
  sin cruzar manos. Las flechas no se usan para mover porque chocarían con esa mano.
- En mando se sigue la convención más extendida en juegos de fútbol (A pase, B tiro, X pase alto, LB cambio, RT sprint).

---

## 6. Plan por fases

| Fase | Contenido | Estado |
|---|---|---|
| **1. Movimiento, balón y controles** | Campo y porterías, jugadores, movimiento con aceleración/giro, sprint con resistencia, conducción, pase corto/largo con asistencia, tiro con potencia y error, entrada, cambio de jugador, detección de gol, cámara básica, HUD de pruebas, generador de escena | **Hecha (pendiente de prueba en Unity)** |
| 2. Reglas del partido | `MatchManager` con estados (presentación, saque inicial, en juego, gol, descanso, final), cronómetro configurable, cambio de campo, saques de banda/puerta/córner simplificados, reposicionamiento tras gol | Pendiente |
| 3. IA y cámara | Cerebro por futbolista con comportamientos separados (con balón, apoyo, desmarque, presión, cobertura de zona, portero), coordinación por equipo para que solo 1-2 jugadores presionen, formación dinámica según balón; cámara con zoom según la jugada y plano de gol | Pendiente |
| 4. Interfaz de partido | Marcador y tiempo (uGUI + TextMeshPro), posesión, mensajes (gol, descanso, final), barras de potencia y resistencia, nombre/dorsal del jugador activo | Pendiente |
| 5. Menús y ajustes | Menú principal, pausa (reanudar, reiniciar, salir), opciones (duración, dificultad, volumen, reasignación de controles), pantalla de resultado | Pendiente |

> Correspondencia con el proceso de trabajo pedido: los pasos 1-3 (arquitectura, estructura, movimiento/balón/controles)
> son esta entrega; el paso 4 (lógica del partido, IA, cámara) se reparte en las Fases 2 y 3; el paso 5 (interfaz,
> menú y ajustes) en las Fases 4 y 5. La cámara básica se adelanta a la Fase 1 porque sin ella no se puede probar
> el movimiento, que además es relativo a la cámara.

---

## 7. Limitaciones conocidas (Fase 1)

- **No probado dentro de Unity.** Se ha comprobado que compila (ver más abajo) y se ha simulado numéricamente
  la conducción en giros bruscos, pero el ajuste fino (velocidades, rigidez del balón, encuadre) necesita jugar.
- Los jugadores no controlados **están quietos**: no hay IA hasta la Fase 3 (el portero rival no se mueve).
- No hay saques: el balón rebota en muros invisibles alrededor del campo.
- El HUD es provisional (IMGUI) y el marcador solo cuenta goles de entrenamiento.
- Sin animaciones: los cuerpos son primitivas que giran y se desplazan.
- Las entradas no tienen faltas: si fallas, tu jugador queda aturdido un instante.
- El tiro incluye un margen de error aleatorio; el tamaño del margen se explica siempre por distancia, presión,
  perfil del cuerpo y sprint (ver `ShotCalculator`).

### Cómo se verificó el código

Sin Unity disponible, se compiló todo el código (juego y editor) con el SDK de .NET contra las DLL de referencia
`UnityEngine.dll`/`UnityEditor.dll` de Unity 2021, renombrando automáticamente las APIs que cambiaron de nombre en
Unity 6 (`linearVelocity`→`velocity`, `PhysicsMaterial`→`PhysicMaterial`...) y con stubs mínimos del Input System.
Resultado: 0 errores, 0 avisos. Esto detecta errores de sintaxis y de tipos, **no** errores de comportamiento.

---

## 8. Puesta en marcha

Instrucciones detalladas y lista de pruebas: **[`docs/Fase1.md`](docs/Fase1.md)**.

Resumen:

1. Crea un proyecto nuevo en Unity 6 con la plantilla **Universal 3D** (incluye el Input System).
2. Copia la carpeta `Assets/_Project` de este repositorio dentro de `Assets/` de tu proyecto.
3. Menú **Fútbol Total ▸ Crear escena de entrenamiento (Fase 1)** y pulsa **Play**.
