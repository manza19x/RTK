# Fase 1 — Movimiento, balón y controles

Objetivo: poder mover a un futbolista, conducir, esprintar, pasar, tirar, robar y cambiar de jugador,
con un balón estable y goles detectados. Todavía no hay IA ni reglas de partido.

## Archivos de esta fase

Todos nuevos. Rutas relativas a la raíz del proyecto de Unity.

| Ruta | Responsabilidad |
|---|---|
| `Assets/_Project/Scripts/FutbolTotal.Runtime.asmdef` | Ensamblado del juego (referencia `Unity.InputSystem`) |
| `Assets/_Project/Scripts/Core/MatchContext.cs` | Acceso único a balón, campo, equipos y lista de jugadores |
| `Assets/_Project/Scripts/Data/FootballEnums.cs` | `PlayerRole`, `TeamSide`, `KickType` |
| `Assets/_Project/Scripts/Data/PlayerMovementSettings.cs` | Velocidades, aceleración, giro, resistencia |
| `Assets/_Project/Scripts/Data/BallSettings.cs` | Física del balón, conducción, recepción, portero |
| `Assets/_Project/Scripts/Data/ActionSettings.cs` | Pases, asistencia, tiro, precisión, entrada |
| `Assets/_Project/Scripts/Data/CameraSettings.cs` | Encuadre de la cámara |
| `Assets/_Project/Scripts/Data/TeamSettings.cs` | Nombre y colores de un equipo |
| `Assets/_Project/Scripts/Data/FormationSettings.cs` | Formación normalizada (por defecto 2-3-1 de fútbol 7) |
| `Assets/_Project/Scripts/Controls/FootballInput.cs` | Acciones del Input System (teclado y mando) |
| `Assets/_Project/Scripts/Controls/ChargeButton.cs` | Botón con carga que actúa una vez al soltar |
| `Assets/_Project/Scripts/Controls/HumanTeamController.cs` | Jugador activo, órdenes, cambios de jugador, golpeo al primer toque |
| `Assets/_Project/Scripts/Players/FootballPlayer.cs` | Identidad del futbolista y acceso a sus componentes |
| `Assets/_Project/Scripts/Players/PlayerMotor.cs` | Movimiento con aceleración, frenada y giro |
| `Assets/_Project/Scripts/Players/PlayerStamina.cs` | Resistencia del sprint |
| `Assets/_Project/Scripts/Players/PlayerActions.cs` | Pase corto, pase largo, tiro y entrada |
| `Assets/_Project/Scripts/Ball/Ball.cs` | Física, conducción y estado de posesión del balón |
| `Assets/_Project/Scripts/Ball/BallPossession.cs` | Quién se hace con un balón suelto; rebotes |
| `Assets/_Project/Scripts/Ball/BallVisual.cs` | Giro visual del balón |
| `Assets/_Project/Scripts/Teams/Team.cs` | Equipo: jugadores, dirección de ataque, formación |
| `Assets/_Project/Scripts/Pitch/Pitch.cs` | Medidas y geometría del campo |
| `Assets/_Project/Scripts/Pitch/PitchMeshGenerator.cs` | Líneas y franjas del césped generadas |
| `Assets/_Project/Scripts/Pitch/GoalZone.cs` | Detección de gol (balón entero dentro) y frenada de la red |
| `Assets/_Project/Scripts/Gameplay/KickMath.cs` | Fórmulas de trayectoria |
| `Assets/_Project/Scripts/Gameplay/PassTargetSelector.cs` | Elección de receptor |
| `Assets/_Project/Scripts/Gameplay/ShotCalculator.cs` | Dirección, potencia y error del tiro |
| `Assets/_Project/Scripts/Gameplay/PlayerSwitchSelector.cs` | Elección del jugador al cambiar |
| `Assets/_Project/Scripts/CameraRig/MatchCamera.cs` | Cámara de retransmisión |
| `Assets/_Project/Scripts/Presentation/SelectionIndicator.cs` | Anillo bajo el jugador activo |
| `Assets/_Project/Scripts/Utils/MeshShapes.cs` | `FlatMeshBuilder`: rectángulos, arcos y discos planos |
| `Assets/_Project/Scripts/DevTools/TrainingHarness.cs` | Pruebas: colocación, goles, pausa, recolocar balón |
| `Assets/_Project/Scripts/DevTools/DebugHud.cs` | HUD provisional (resistencia, carga, posesión, ayuda) |
| `Assets/_Project/Editor/FutbolTotal.Editor.asmdef` | Ensamblado de editor |
| `Assets/_Project/Editor/EditorAssetUtility.cs` | Creación de materiales, físicas y ajustes |
| `Assets/_Project/Editor/Phase1SceneBuilder.cs` | Genera la escena de pruebas |

## Montaje en Unity

1. **Unity Hub ▸ New project ▸ Unity 6 ▸ plantilla «Universal 3D»**. (El pipeline integrado también vale.)
2. Comprueba que el Input System está instalado: **Window ▸ Package Manager ▸ In Project ▸ Input System**.
   Si no está, instálalo desde *Unity Registry* y acepta reiniciar el editor.
3. **Edit ▸ Project Settings ▸ Player ▸ Other Settings ▸ Active Input Handling**: debe ser
   *Input System Package (New)* o *Both*. Si está en *Input Manager (Old)*, el juego no responderá a los controles.
4. Copia la carpeta `Assets/_Project` de este repositorio dentro de la carpeta `Assets/` de tu proyecto.
   Espera a que Unity compile; la consola no debe mostrar errores.
5. Menú superior **Fútbol Total ▸ Crear escena de entrenamiento (Fase 1)**. Se crean:
   - `Assets/_Project/Resources/FutbolTotal/` con los ajustes, equipos y formación;
   - `Assets/_Project/Art/` con materiales, textura del balón y materiales físicos;
   - `Assets/_Project/Scenes/Fase1_Entrenamiento.unity` (abierta y añadida a *Build Settings*).
6. Pulsa **Play**. Para jugar con mando, conéctalo antes o durante la partida.

Volver a ejecutar el menú regenera la escena pero **reutiliza** los assets de ajustes y materiales existentes,
así que los cambios hechos en el Inspector se conservan. Para volver a los valores por defecto, borra el asset.

### Montaje manual (alternativa)

Si prefieres no usar el generador, crea los objetos de la tabla del apartado 4 del [README](../README.md),
crea los assets con **Create ▸ Fútbol Total ▸ ...** y asigna en el Inspector todas las referencias
(`settings`, `ball`, `pitch`, equipos, `input`, `cameraTransform`...). Los jugadores deben ser hijos de su `Team`.

## Lista de pruebas

Marca cada punto tras comprobarlo. Si algo falla, anota qué hiciste y qué ocurrió.

**Escena y cámara**
- [ ] Al pulsar Play no hay errores en la consola.
- [ ] Se ven el campo con franjas, líneas, áreas, círculo central, puntos de penalti y arcos de córner.
- [ ] Se ven dos porterías con red semitransparente, 7 jugadores rojos (portero amarillo) y 7 azules (portero verde).
- [ ] Un anillo amarillo con flecha marca al jugador controlado.
- [ ] La cámara es lateral y sigue al balón sin girar.

**Movimiento**
- [ ] WASD/stick mueve al jugador en la dirección de la pantalla (arriba = hacia el fondo del campo).
- [ ] Arranca y frena con suavidad; al soltar se detiene en menos de medio segundo, sin patinar.
- [ ] Un cambio de sentido de 180° frena primero y luego acelera.
- [ ] Con sprint corre más rápido; la barra de resistencia baja y se recupera al dejar de esprintar.
- [ ] Al agotarla no se puede esprintar hasta recuperar aproximadamente un 30 %.

**Balón y conducción**
- [ ] El balón está quieto en el centro al empezar; al llegar a él, el jugador lo controla («Balón: AUR n»).
- [ ] Conduciendo al trote el balón va pegado; esprintando va más adelantado y se retrasa en los giros.
- [ ] El balón no tiembla, no se hunde en el suelo y no atraviesa postes ni red.

**Pases**
- [ ] J (toque) con un compañero en la dirección apuntada: pase raso hacia él y el control pasa al receptor.
- [ ] J mantenido elige compañeros más lejanos en esa dirección.
- [ ] Sin compañero en esa dirección: pase al espacio, más largo cuanto más se carga.
- [ ] L: pase por alto que cae algo por delante del receptor.
- [ ] Mantener un botón pulsado no repite la acción.
- [ ] Pulsar J o K justo antes de recibir un pase ejecuta la acción al primer toque.

**Tiro y gol**
- [ ] K cargado poco: tiro raso y suave; K cargado del todo: tiro fuerte y alto (puede irse por encima).
- [ ] Con la dirección arriba/abajo pulsada, el tiro va hacia el palo correspondiente.
- [ ] Un balón que cruza la línea por completo entre los postes muestra «¡GOL de ...!» y a los 2 s se recoloca todo.
- [ ] Un balón que da en el poste y sale, o que no cruza entera la línea, no es gol.
- [ ] Un tiro fuerte al portero rival lo rechaza; uno flojo lo bloca.

**Defensa y cambios**
- [ ] Pasa el balón al rival (o déjalo cerca de un azul): ese jugador lo controla y se queda quieto.
- [ ] J sin balón cerca de él: entrada; si alcanzas el balón lo robas y el rival queda aturdido.
- [ ] Una entrada fallida deja a tu jugador aturdido un instante.
- [ ] Q / LB sin balón cambia al compañero mejor situado respecto al balón.

**Pausa**
- [ ] Esc / Start congela el juego y muestra «PAUSA»; al reanudar todo sigue igual.
- [ ] Mantener J durante la pausa y soltarlo después de reanudar no hace ningún pase.
- [ ] R / Select coloca el balón delante del jugador activo.

## Ajustes rápidos

Todos los valores están en `Assets/_Project/Resources/FutbolTotal`. Se pueden cambiar durante el Play (los cambios en
ScriptableObjects se conservan al salir del Play).

| Si... | Cambia |
|---|---|
| El jugador patina | `PlayerMovementSettings ▸ deceleration` ↑ |
| Arranca demasiado lento | `acceleration` / `sprintAcceleration` ↑ |
| El balón va demasiado suelto al conducir | `BallSettings ▸ dribbleStiffnessSprint` ↑, `dribbleDistanceSprint` ↓ |
| Los pases se quedan cortos o llegan muy fuertes | `ActionSettings ▸ shortPassArrivalSpeed`, `BallSettings ▸ rollingDeceleration` |
| Los pases van "solos" | `ActionSettings ▸ passAssist` ↓ |
| Los tiros fallan demasiado | `shotBaseErrorDegrees`, `shotErrorPerMeter` ↓ |
| Se ve poco campo | `CameraSettings ▸ height`, `distance` o `fieldOfView` ↑ |

## Problemas frecuentes

- **Los controles no hacen nada**: revisa *Active Input Handling* (paso 3) y que la ventana *Game* tenga el foco.
- **Materiales rosas**: el proyecto usa un pipeline distinto de URP o el integrado (p. ej. HDRP). Asigna
  materiales compatibles a los de `Art/Materials` o usa la plantilla Universal 3D.
- **«Campo 'x' no encontrado» al generar la escena**: indica que un script se ha renombrado; vuelve a copiar `_Project`.
- **Error de ensamblado `Unity.InputSystem` no encontrado**: falta el paquete Input System (paso 2).
