# ⚽ Patea el Balón (juego de Roblox)

Patea el balón lo más lejos que puedas. Mientras más lejos cae, mejor es la **zona** y más **suerte** tienes.
Donde cae el balón aparece un **futbolista**: los raros (como Messi, 1 en 250.000) solo salen con mucha suerte.
Gana monedas y mejora tu **fuerza** y tu **balón** en la tienda.

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
| Patear | Botón **⚽ PATEAR** o tecla **F**. Suelta cuando la barra esté en la parte dorada (★) para una **patada perfecta** (+15%). |
| Zonas | Son 30, de 100 m cada una. La zona 1 tiene suerte x1, la 10 x7,5, la 20 x69 y la 30 x646. |
| Futbolistas | Al caer el balón se sortea uno. La suerte divide la dificultad: con suerte x10, "1 en 1.000" se vuelve "1 en 100". |
| Monedas | Ganas más mientras más lejos pateas, con un bono por zona. Los futbolistas raros dan monedas extra. |
| Tienda 🛒 | **Fuerza**: +15 de potencia por nivel. **Balones**: 12 balones, del Trapo (x1) al Galáctico (x8). |
| Colección 📖 | Muestra los futbolistas que ya sacaste y cuántas veces. |

Según una simulación, se llega a la zona 10 en unas 200 patadas, a la 20 en unas 800 y a la 30 en unas 1.300.

## Cómo cambiar cosas

Casi todo está en **`src/shared/Config.luau`** (en Studio: *ReplicatedStorage > Shared > Config*):

- `Config.Footballers`: nombres, "1 en X", rareza y colores de la camiseta. Puedes agregar más.
- `Config.Balls`: balones, precios y multiplicadores.
- `Config.NUM_ZONES`, `Config.ZONE_LENGTH`, `Config.LUCK_GROWTH`: zonas y suerte.
- `GetStrengthCost`, `GetCoinsForKick`, `GetFootballerReward`: precios y monedas.

> **Ojo con los nombres reales:** los futbolistas usan nombres reales de jugadores. Para jugar con amigos no hay problema, pero si vas a publicar el juego con ganancias (Robux), Roblox puede moderarlo por usar nombres de personas reales sin permiso. Se cambian en `Config.Footballers`.

## Archivos

```
src/shared/Config.luau          -> ReplicatedStorage.Shared.Config (configuración)
src/server/Main.server.luau     -> ServerScriptService.Server.Main (patada, sorteo, tienda)
src/server/MapBuilder.luau      -> construye el campo, las 30 zonas y la portería
src/server/DataManager.luau     -> guarda monedas, fuerza, balón y colección
src/client/Main.client.luau     -> StarterPlayerScripts.Client.Main (interfaz y cámara)
```

El mapa se construye solo al iniciar el juego, así que no hay que armar nada a mano.

**Si copias los scripts a tu propio lugar:** desactiva *Workspace > StreamingEnabled*. Si no, el balón y los futbolistas lejanos no se cargan en tu pantalla.

## Para programadores (Rojo)

```bash
rojo serve        # sincroniza en vivo con Studio (plugin de Rojo)
rojo build -o PateaElBalon.rbxlx
```
