# Fase 10 — Equipos

20 clubes ficticios para elegir en el menú principal (nombres, colores, escudos y estadios inventados).

## Equipos

| # | Equipo | Abr. | Estadio | ATQ | MED | DEF | Media |
|---|---|---|---|---|---|---|---|
| 1 | Club Aurora | AUR | Estadio Aurora | 78 | 80 | 76 | 78 |
| 2 | Deportivo Marea | MAR | Estadio del Puerto | 77 | 75 | 80 | 77 |
| 3 | Atlético Volcán | VOL | Cráter Arena | 84 | 76 | 70 | 77 |
| 4 | Unión Cóndor | CON | Estadio Altiplano | 74 | 82 | 79 | 78 |
| 5 | Racing Brisa | BRI | Campo de la Brisa | 72 | 78 | 74 | 75 |
| 6 | Sporting Pinar | PIN | Estadio El Pinar | 76 | 77 | 77 | 77 |
| 7 | Ciudad Ámbar | AMB | Estadio Ámbar | 80 | 72 | 73 | 75 |
| 8 | Estrella Polar | EST | Estadio Boreal | 73 | 79 | 83 | 78 |
| 9 | Halcones del Sur | HAL | Nido del Sur | 82 | 80 | 75 | 79 |
| 10 | Tormenta FC | TOR | Estadio Relámpago | 79 | 74 | 71 | 75 |
| 11 | Montaña Azul | MON | Estadio La Cumbre | 75 | 76 | 84 | 78 |
| 12 | Leones del Valle | LEO | Estadio del Valle | 83 | 78 | 72 | 78 |
| 13 | Arena Dorada | ARE | Estadio Dorado | 70 | 73 | 75 | 73 |
| 14 | Selva Verde | SEL | Estadio Jungla | 77 | 81 | 74 | 77 |
| 15 | Faro del Norte | FAR | Estadio El Faro | 74 | 72 | 78 | 75 |
| 16 | Celeste FC | CEL | Estadio Celeste | 81 | 83 | 76 | 80 |
| 17 | Bahía Coral | BAH | Arena de la Bahía | 73 | 75 | 70 | 73 |
| 18 | Titanes FC | TIT | Coliseo Titán | 78 | 71 | 82 | 77 |
| 19 | Glaciar CF | GLA | Estadio Glaciar | 71 | 77 | 80 | 76 |
| 20 | Cometa Rosa | COM | Órbita Arena | 76 | 79 | 73 | 76 |

Para añadir o cambiar equipos: `Scripts/Data/TeamCatalog.cs` (una línea por equipo).

## Cómo funciona

- **Menú**: la tarjeta del partido tiene `<` y `>` bajo cada escudo para elegir tu equipo (local) y el rival. Muestra
  escudo, nombre, media y barras de ataque, medio y defensa; abajo, el estadio del local. La elección se guarda.
- **Al empezar**: cada `Team` crea sus ajustes desde el catálogo. Si el rival es el mismo equipo pasa al siguiente; si las
  camisetas se parecen, el visitante viste su **segunda equipación** (su color secundario, o blanco/negro).
- **Equipaciones** (`KitPainter`): camiseta y medias del color principal, pantalón del secundario, portero con su color y
  dorsal blanco o casi negro según la camiseta. Funciona con Shannon (materiales de equipación) y con el cuerpo generado.
  También usan los colores el público, el marcador, el radar, los escudos y la pantalla de resultado.
- **Valoraciones** (`TeamStrength`, efecto moderado):

  | Valoración | Efecto (de 60 a 90) |
  |---|---|
  | Ataque | Error del tiro ×1,2 → ×0,8; la IA tira desde un 8 % menos → 8 % más lejos |
  | Medio | Velocidad −4 % → +4 % |
  | Defensa | Alcance de las entradas −8 % → +8 %; reflejos del portero y de las entradas de la IA un 15 % más lentos → 12 % más rápidos |

- `Team` tiene la casilla **Use Catalog**: desactivada, usa el asset `TeamSettings` como antes.

## Pruebas

- [ ] En el menú, `<` y `>` cambian de equipo; el escudo, el nombre, la media y las barras se actualizan; no deja elegir el mismo equipo en los dos lados.
- [ ] Al jugar, las camisetas, el marcador, el radar y el público tienen los colores elegidos.
- [ ] Con dos equipos de colores parecidos (por ejemplo, Club Aurora y Faro del Norte, los dos rojos), el visitante sale con la segunda equipación.
- [ ] Los dorsales se leen en todas las camisetas (oscuros en las claras).
- [ ] La elección se recuerda al volver a abrir el juego.
