# Fase 6 — Aspecto visual

Objetivo: pasar de figuras de cápsulas sobre un plano verde a un partido con aspecto de retransmisión:
jugadores con cuerpo y animación, césped con textura, estadio con público y una imagen cuidada.
Todo se genera por código; no se usa ningún recurso de terceros.

## Qué cambia

| Elemento | Antes | Ahora |
|---|---|---|
| Jugadores | Cápsula con cabeza | Cuerpo articulado (cadera, torso, cuello, cabeza con pelo, brazos con codo, piernas con rodilla, botas). Piel y pelo variados por dorsal. |
| Animación | Ninguna (se deslizaban) | Carrera con zancada y braceo según la velocidad e inclinación al esprintar; golpeo con la pierna; entrada en plancha; estirada del portero hacia el balón; tambaleo al quedar aturdido; celebración con brazos arriba y saltos; respiración en reposo. |
| Césped | Color liso | Textura de 1024 px con manchas, briznas y zonas más secas, mapa de relieve y franjas de corte. Sin costuras al repetirse. |
| Balón | Manchas | Balón clásico: 12 pentágonos negros, 20 hexágonos blancos y costuras. |
| Redes | Cajas semitransparentes | Malla de hilos de 14 cm con caída en el fondo (las cajas siguen como colisión). |
| Estadio | Nada | Cuatro gradas de 14 filas con asientos, muros, cubiertas metálicas, vallas publicitarias iluminadas (diseños inventados) y cuatro torres de focos. |
| Público | Nada | ~5.500 espectadores con los colores de cada equipo (instanciado por GPU). Se balancean y la afición del equipo que marca salta. |
| Luz e imagen | Luz básica, fondo liso | Sol de tarde con sombras suaves, cielo procedural, luz ambiental del cielo, neblina lejana. En URP: tono de cine ACES, bloom (focos y vallas brillan), ajuste de color, viñeta, SMAA alto, MSAA x4, sombras a 110 m con 4 cascadas. |

## Decisiones

- **Generado por código**: texturas (`ProceduralTextures`), mallas (`StadiumGenerator`, `NetMeshGenerator`) y cuerpos
  (`PlayerRigBuilder`). Se regeneran solos, así que cambiar las medidas del campo actualiza el estadio y las redes.
- **Animación procedural** (`PlayerAnimator`): calcula cada frame las rotaciones de las articulaciones a partir del estado
  del jugador (velocidad, golpeo, impulso, aturdimiento, gol). Solo es visual; no toca la física ni las reglas.
- **Público instanciado** (`CrowdRenderer`): sin un objeto por espectador; una llamada de dibujo por color y bloque de 1023.
- **Posprocesado en un ensamblado opcional** (`Scripts/Visuals/URP`): solo se compila si el proyecto usa URP. El perfil se
  crea al arrancar. **Aviso**: `VisualQualitySetup` cambia en el asset de URP del proyecto la distancia de sombras,
  las cascadas, el MSAA y el HDR; esos cambios se quedan guardados.
- Materiales con sufijo `_HD`: las versiones anteriores no se tocan.

## Archivos

Nuevos: `Scripts/Visuals/ProceduralTextures.cs`, `PlayerRigBuilder.cs`, `PlayerAnimator.cs`, `StadiumGenerator.cs`,
`CrowdRenderer.cs`, `NetMeshGenerator.cs`, `Scripts/Visuals/URP/VisualQualitySetup.cs` y
`Scripts/Visuals/URP/FutbolTotal.Visuals.URP.asmdef`.

Modificados: `Scripts/Utils/MeshShapes.cs` (UV en las mallas planas y `MeshBuilder3D`), `Scripts/Players/PlayerMotor.cs`
(`LungeDirection`), `Scripts/CameraRig/MenuCameraOrbit.cs` (órbita fuera del estadio), `Editor/EditorAssetUtility.cs`
(texturas PNG, materiales con relieve/emisión/doble cara, cielo) y `Editor/SceneBuilder.cs` (todo lo anterior en las escenas).

## Montaje en Unity

1. Copia `FutbolTotal/Assets/_Project` sobre la carpeta `Assets` del proyecto («Reemplazar los archivos en el destino»).
2. Espera a que compile. La consola no debe tener errores rojos.
3. **Fútbol Total ▸ Crear todas las escenas**. La primera vez tarda unos segundos más: genera las texturas
   (`Art/Textures/Grass_Albedo.png`, `Grass_Normal.png`, `Ball_HD.png`, `AdBoard.png`).
4. **▶ Play**.

Rendimiento: pensado para un PC de sobremesa. Si va lento, baja la calidad en *Edit ▸ Project Settings ▸ Quality*,
reduce `occupancy` en el componente `CrowdRenderer` (objeto `Pitch/Stadium`) o desactiva el objeto `VisualQuality`.

## Lista de pruebas

- [ ] El menú principal muestra el estadio con público, gradas, focos y vallas; la cámara gira fuera del estadio.
- [ ] El cielo tiene degradado y sol; las sombras de jugadores, porterías y gradas se ven suaves.
- [ ] El césped tiene textura (no es un color liso) y franjas; de cerca se aprecia relieve.
- [ ] Las redes se ven como hilos y el balón con pentágonos negros.
- [ ] Los jugadores mueven piernas y brazos al correr (más amplio al esprintar), dan una patada al pasar o tirar,
      hacen la plancha al entrar y el portero se estira hacia el lado.
- [ ] Al marcar, los jugadores del equipo celebran con los brazos arriba y su afición salta en las gradas.
- [ ] Los focos y las vallas brillan un poco (bloom); los bordes se ven suaves.
- [ ] El juego sigue igual de jugable (mismas reglas, IA y controles).

## Siguiente paso hacia el realismo total (Fase 7, opcional)

El límite de lo que se puede generar con código está en los cuerpos y sus movimientos. Para jugadores realistas hacen falta:

1. **Modelos humanos con esqueleto** (*humanoid*): por ejemplo, personajes gratuitos de **Mixamo** (Adobe; requiere cuenta
   gratuita y aceptar su licencia) o los de *Starter Assets* de Unity.
2. **Animaciones capturadas**: correr, esprintar, chutar, entrada, estirada del portero, celebración (también en Mixamo).
3. Un *Animator Controller* que reciba la velocidad y los eventos del juego.

El código ya separa el cuerpo (`Visual`) del resto del jugador, así que se puede sustituir por un modelo animado sin tocar
física, reglas ni IA. Esa sería la Fase 7.
