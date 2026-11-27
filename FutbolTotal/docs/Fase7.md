# Fase 7 — Personaje de Mixamo

Objetivo: sustituir los cuerpos generados por código por un personaje humano con animaciones capturadas de Mixamo
(Adobe, gratuito con cuenta). El modelo elegido es **Shannon**, un futbolista con equipación blanca, que el juego tiñe
con los colores de cada equipo.

## Archivos que pone el jugador (no están en el repositorio)

En `Assets/_Project/Art/Mixamo/`:

| Archivo | Contenido | Opciones de descarga |
|---|---|---|
| `Jugador.fbx` | Personaje (Shannon) | FBX for Unity, T-Pose |
| `Idle.fbx`, `Correr.fbx`, `Sprint.fbx` | Quieto, trote, sprint | FBX for Unity, Without Skin, 30 fps, **In Place** |
| `Pase.fbx`, `Tiro.fbx`, `Entrada.fbx`, `Estirada.fbx`, `Celebracion.fbx` | Acciones | FBX for Unity, Without Skin, 30 fps |

Los archivos de Mixamo no se suben al repositorio: su licencia permite usarlos en juegos, no redistribuirlos sueltos.

## Qué hace el generador de escenas

1. `Jugador.fbx` → rig **Humanoid** con Avatar propio, sin animaciones.
2. Cada animación → Humanoid copiando el Avatar del personaje; en bucle Idle/Correr/Sprint; raíz bloqueada
   (sin desplazamiento ni giro propios: el jugador lo mueve `PlayerMotor`).
3. `FutbolTotal_Animator.controller`: mezcla 1D quieto (0) → trote (0,6) → sprint (1) según `Speed` (velocidad / velocidad
   de sprint) y estados Pase, Tiro, Entrada, Estirada (en espejo con `MirrorDive`) y Celebracion, que vuelven a la locomoción.
4. Materiales de la equipación teñidos por equipo y portero en `Art/Mixamo/Materials`. En la consola salen los nombres
   de los materiales del modelo; si se tiñe algo que no debe (por ejemplo la piel), se ajustan las palabras clave en
   `Resources/FutbolTotal/CharacterSettings`.
5. Cada jugador lleva el modelo escalado a 1,80 m y `HumanoidAnimatorBridge`, que lanza las animaciones según lo que pasa
   en el juego. Si falta `Jugador.fbx` (o `Idle` y `Correr`), se usa el cuerpo generado de la Fase 6.

## Archivos de código

Nuevos: `Scripts/Data/CharacterSettings.cs`, `Scripts/Visuals/HumanoidAnimatorBridge.cs`, `Editor/MixamoSetup.cs`.
Modificados: `Editor/SceneBuilder.cs` (usa el modelo si existe), `Editor/EditorAssetUtility.cs` (`SetBool`),
`Scripts/Core/SettingsProvider.cs` (`CharacterSettings`).

## Lista de pruebas

- [ ] Al crear las escenas, la consola dice «Personaje de Mixamo preparado» y lista los materiales.
- [ ] Los jugadores son Shannon, rojos (Club Aurora) y azules (Deportivo Marea); los porteros, amarillo y verde.
- [ ] Quietos respiran; al moverse trotan y al esprintar corren, sin deslizarse ni dar saltos atrás.
- [ ] Al pasar y al tirar hacen su animación; al entrar, la plancha; el portero se estira hacia el lado correcto.
- [ ] Al marcar, el equipo celebra.
- [ ] Si el portero se estira hacia el lado contrario: en `CharacterSettings` cambia `diveClipGoesRight` y vuelve a crear las escenas.
