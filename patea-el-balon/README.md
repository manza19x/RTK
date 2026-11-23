# ⚽ Patea el Balón (juego de Roblox)

Patea el balón a través de 30 zonas, cada una con su propio escenario a los lados. Mientras más lejos cae, más **suerte** tienes.
Donde cae el balón llega caminando un **futbolista**. Los raros (como Messi, 1 en 250.000) solo salen con mucha suerte.
Cada patada **entrena tu poder**, y los futbolistas de tu **plantilla** te dan monedas cada segundo.

## Cómo jugarlo en Roblox Studio (la forma fácil)

1. Descarga el archivo **`PateaElBalon.rbxlx`** de esta carpeta.
2. Ábrelo con **Roblox Studio** (doble clic, o en Studio: *File > Open from File*).
3. Presiona **Play** (F5).

Para que **se guarde el progreso**:
1. Publica el juego (*File > Publish to Roblox*).
2. Activa *Home > Game Settings > Security > Enable Studio Access to API Services*.

## Cómo se juega

| Qué | Cómo |
| --- | --- |
| Patear | Camina hasta el arco y **cruza la línea blanca**: aparece la barra de poder. Haz **clic** (o tecla **F**, o el botón) cuando la barra esté arriba, en la parte dorada (★), para una patada **¡PERFECTO!**: va 15% más lejos y da el doble de poder. Después de patear vuelves atrás de la línea. |
| Gimnasio 🏋️ | Al fondo del lobby, detrás del monumento. Párate en el tapete de un aparato y presiona **E**. Cada **clic** te da poder (una repetición cada 0,8 s); con **🔁 Auto** entrena solo. Hay 6 aparatos: Mancuernas (+1), Sentadillas (+2, requiere 200), Prensa de piernas (+3, requiere 800), Saco de patadas (+5, requiere 2.000), Llantas gigantes (+8, requiere 4.000) y Máquina dorada (+12, requiere 7.000). Sales con **Q**. |
| Poder 💪 | Cada patada te da de +1 a +10 de poder, y el gimnasio te da mucho más. Más poder hace que el balón llegue más lejos. |
| Zonas 🗺️ | 30 zonas de 100 m. Cada una tiene un escenario distinto a los lados, y van mejorando mientras más lejos llegas: **Barrios** (de tierra, humilde, colorido, con canchita, colonia), **Ciudad** (calle comercial, avenida, parque, centro, rascacielos), **Playa** (playa, malecón, hoteles, puerto de yates, resort), **Fútbol** (escuela, estadio de barrio, de ciudad, profesional y gran estadio), **Lujo** (mansiones, autos de lujo, castillos, ciudad dorada, salón de trofeos) y **Leyenda** (sobre las nubes, arcoíris, la Luna, galaxia y el Salón de la Fama). La suerte sube mientras el balón vuela: x1,25 a 100 m, x9 a 1.000 m, x87 a 2.000 m y x807 a 3.000 m. |
| Futbolistas | Al caer el balón se sortea uno. Llega caminando, su nombre aparece letra por letra y ves si es **¡NUEVO!** y cuánto gana por segundo. La suerte divide la dificultad: con suerte x10, "1 en 1.000" se vuelve "1 en 100". |
| Plantilla ⚽ | Tus futbolistas te dan monedas **cada segundo**. Empiezas con 8 espacios y puedes comprar más. Si está llena, te quedas con los mejores y el peor se vende solo. También puedes vender a mano. |
| Tu corral 🐾 | Cada jugador tiene un corral en el lobby (hay 6) con su nombre. Ahí caminan sus futbolistas. El dinero se junta en el **plato dorado** de la entrada y se cobra al pisarlo. En el **cartel de madera** presionas **E** para comprar más espacios. |
| Tienda 🛒 | 12 balones, del Trapo (x1 de distancia) al Galáctico (x8). Se abre con el botón o en el puesto rojo del lobby (tecla **E**). El puesto azul abre el Índice. |
| Índice 📖 | Los futbolistas que ya descubriste y cuántas veces te salió cada uno. |

Según una simulación (pateando y entrenando), se llega a la zona 10 en unos 47 minutos, a la 20 en unos 80 y a la 30 en 1 hora 40. Si solo pateas sin entrenar, es mucho más lento.

## Cómo cambiar cosas

Casi todo está en **`src/shared/Config.luau`** (en Studio: *ReplicatedStorage > Shared > Config*):

- `Config.Footballers`: nombres, "1 en X", rareza, monedas por segundo (`Income`) y colores. Puedes agregar más.
- `Config.Balls`: balones, precios y multiplicadores de distancia.
- `Config.Zones`: nombre, emoji, colores y escenario de cada una de las 30 zonas.
- `Config.NUM_ZONES`, `Config.ZONE_LENGTH`, `Config.LUCK_GROWTH`: zonas y suerte.
- `START_POWER`, `DISTANCE_PER_POWER`, `GetPowerGain`: poder y distancia.
- `Config.TrainingStations`, `TRAIN_COOLDOWN`: aparatos del gimnasio, cuánto poder dan y cuánto piden.
- `KICK_LINE_Z`, `RETURN_POSITION`: dónde está la línea de patada y a dónde vuelves.
- `START_SLOTS`, `GetSlotCost`, `SELL_SECONDS`: plantilla y precios de venta.

> **Ojo con los nombres reales:** los futbolistas usan nombres reales de jugadores. Para jugar con amigos no hay problema, pero si vas a publicar el juego con ganancias (Robux), Roblox puede moderarlo por usar nombres de personas reales sin permiso. Se cambian en `Config.Footballers`.

## Archivos

```
src/shared/Config.luau          -> ReplicatedStorage.Shared.Config (configuración)
src/server/Main.server.luau     -> ServerScriptService.Server.Main (patada, poder, sorteo, plantilla, tienda)
src/server/MapBuilder.luau      -> construye el lobby, los corrales, el gimnasio, el campo y el cielo
src/server/Scenery.luau         -> los 30 escenarios de los lados (barrios, ciudad, playa, estadios...)
src/server/PlotManager.luau     -> corrales: futbolistas, plato para cobrar y cartel de mejora
src/server/DataManager.luau     -> guarda monedas, poder, balón, plantilla e índice
src/client/Main.client.luau     -> StarterPlayerScripts.Client.Main (interfaz, cámara y revelación)
```

El mapa se construye solo cuando le das **Play**. En el editor no se ve, y es normal. No pongas cosas dentro de la carpeta `Mapa` porque se vuelve a crear al iniciar; si quieres decorar, hazlo en otra carpeta del Workspace.

**Si copias los scripts a tu propio lugar:** desactiva *Workspace > StreamingEnabled*. Si no, el balón y los futbolistas lejanos no se cargan en tu pantalla.

## Para programadores (Rojo)

```bash
rojo serve        # sincroniza en vivo con Studio (plugin de Rojo)
rojo build -o PateaElBalon.rbxlx
```
