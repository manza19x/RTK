# ⚽ Patea el Balón (juego de Roblox)

Patea el balón por un cañón de 30 zonas y 6 biomas. Mientras más lejos cae, más **suerte** tienes.
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
| Patear | Botón **⚽ PATEAR** o tecla **F**. Suelta cuando la barra vertical esté arriba, en la parte dorada (★), para una patada **¡PERFECTO!**: va 15% más lejos y da el doble de poder. |
| Poder 💪 | Cada patada te da de +1 a +10 de poder. Más poder hace que el balón llegue más lejos. |
| Zonas y biomas | 30 zonas de 100 m en 6 biomas: Barrio 🏘️, Bosque 🌳, Playa 🏖️, Desierto 🌵, Nieve ❄️ y Volcán 🌋. La suerte sube mientras el balón vuela: x1,25 a 100 m, x9 a 1.000 m, x87 a 2.000 m y x807 a 3.000 m. |
| Futbolistas | Al caer el balón se sortea uno. Llega caminando, su nombre aparece letra por letra y ves si es **¡NUEVO!** y cuánto gana por segundo. La suerte divide la dificultad: con suerte x10, "1 en 1.000" se vuelve "1 en 100". |
| Plantilla ⚽ | Tus futbolistas te dan monedas **cada segundo**. Empiezas con 8 espacios y puedes comprar más. Si está llena, te quedas con los mejores y el peor se vende solo. También puedes vender a mano. |
| Tu corral 🐾 | Cada jugador tiene un corral en el lobby (hay 6) con su nombre. Ahí caminan sus futbolistas. El dinero se junta en el **plato dorado** de la entrada y se cobra al pisarlo. En el **cartel de madera** presionas **E** para comprar más espacios. |
| Tienda 🛒 | 12 balones, del Trapo (x1 de distancia) al Galáctico (x8). |
| Índice 📖 | Los futbolistas que ya descubriste y cuántas veces te salió cada uno. |

Según una simulación, se llega a la zona 10 en unos 45 minutos, a la 20 en unos 85 y a la 30 en unas 2 horas.

## Cómo cambiar cosas

Casi todo está en **`src/shared/Config.luau`** (en Studio: *ReplicatedStorage > Shared > Config*):

- `Config.Footballers`: nombres, "1 en X", rareza, monedas por segundo (`Income`) y colores. Puedes agregar más.
- `Config.Balls`: balones, precios y multiplicadores de distancia.
- `Config.Biomes`: nombre, emoji, colores y decoración de cada bioma.
- `Config.NUM_ZONES`, `Config.ZONE_LENGTH`, `Config.LUCK_GROWTH`: zonas y suerte.
- `START_POWER`, `DISTANCE_PER_POWER`, `GetPowerGain`: poder y distancia.
- `START_SLOTS`, `GetSlotCost`, `SELL_SECONDS`: plantilla y precios de venta.

> **Ojo con los nombres reales:** los futbolistas usan nombres reales de jugadores. Para jugar con amigos no hay problema, pero si vas a publicar el juego con ganancias (Robux), Roblox puede moderarlo por usar nombres de personas reales sin permiso. Se cambian en `Config.Footballers`.

## Archivos

```
src/shared/Config.luau          -> ReplicatedStorage.Shared.Config (configuración)
src/server/Main.server.luau     -> ServerScriptService.Server.Main (patada, poder, sorteo, plantilla, tienda)
src/server/MapBuilder.luau      -> construye el lobby, los corrales, el cañón, los biomas y el cielo
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
