# Fase 5 — Sonido y faltas

Objetivo: que el partido suene y se arbitre como un partido: golpeos, botes, postes, red, silbato y público,
y que las entradas mal hechas se castiguen con tiros libres y penaltis.

## Decisiones

| Tema | Decisión |
|---|---|
| Origen del sonido | **Generado por código** al arrancar (`ProceduralAudio`): síntesis sencilla de cada sonido. Sin archivos de terceros. Para usar audio grabado, se sustituye el clip correspondiente. |
| Mezcla | Volumen general en **Opciones** (0-100 %, se guarda); volumen relativo de cada sonido en `Resources/FutbolTotal/SoundSettings`. |
| Qué suena | Golpeo (más fuerte y grave cuanto más rápido sale el balón), bote, poste/larguero (metálico), red, silbato (saque inicial, faltas, descanso; tres pitidos al final), público de fondo que se anima cerca de las porterías, celebración de gol y clic de los botones. |
| Falta | Una entrada es falta si **llega por la espalda** del rival (aunque toque el balón) o si **choca con el rival sin alcanzar el balón**. Quien la comete queda aturdido un momento. |
| Sanción | Dentro del área del equipo que la comete: **penalti**. Fuera: **tiro libre** en el lugar de la falta, sacado por quien la recibió; los rivales se apartan 6 m. |
| Penalti | Balón en el punto de penalti, portero en su línea y el resto fuera del área por detrás del balón. Si lo tiras tú, apuntas con la dirección y tiras con K. Si lo tira la IA, chuta fuerte al lado contrario del portero; tu portero lo para la IA. |
| IA | No entra nunca por detrás. Puede cometer falta si choca con el rival sin llegar al balón (por ejemplo, cuando protege el balón con el cuerpo). En tiros libres chuta si está a tiro y con ángulo; si no, pasa. |
| No incluido | Tarjetas, ley de la ventaja, barrera y fuera de juego. |

## Archivos

Nuevos:

| Ruta | Responsabilidad |
|---|---|
| `Scripts/Core/FoulEvent.cs` | Datos de una falta (quién, a quién, dónde, por qué) |
| `Scripts/Data/SoundSettings.cs` | Volumen relativo de cada sonido |
| `Scripts/Audio/ProceduralAudio.cs` | Generación de todos los sonidos |
| `Scripts/Audio/MatchAudio.cs` | Sonido del partido a partir de los eventos del juego |
| `Scripts/Audio/UISound.cs` | Clic de los botones |

Modificados:

| Ruta | Cambio |
|---|---|
| `Scripts/Players/PlayerActions.cs` | Detecta las faltas en las entradas (`IsBehind`, contacto sin balón) |
| `Scripts/Data/ActionSettings.cs` | Parámetros de falta (`foulContactDistance`, `fromBehindDot`) |
| `Scripts/Core/MatchContext.cs` | Evento `FoulCommitted` |
| `Scripts/Match/MatchManager.cs` | Sanciona faltas con tiro libre o penalti y coloca a los jugadores en el penalti |
| `Scripts/Match/MatchState.cs`, `RestartRules.cs`, `AutoRestart.cs` | Saques de tiro libre y penalti (también los de la IA) |
| `Scripts/AI/Behaviors/PressBehavior.cs` | La IA no entra por detrás |
| `Scripts/Ball/Ball.cs` | Evento `Bounced` para el sonido de los choques |
| `Scripts/Match/MatchStats.cs`, `Scripts/UI/ResultScreen.cs` | Faltas en las estadísticas |
| `Scripts/Core/GameOptions.cs`, `Scripts/UI/MainMenu.cs` | Opción de volumen |
| `Scripts/UI/UIFactory.cs` | Clic al pulsar botones |
| `Scripts/DevTools/TrainingHarness.cs` | En entrenamiento, las faltas solo se avisan |
| `Scripts/Core/SettingsProvider.cs`, `Editor/SceneBuilder.cs` | Asset `SoundSettings` y objeto `Audio` en las escenas de juego |

## Montaje en Unity

1. Descarga la versión de esta fase (enlace en el mensaje de entrega) y copia `FutbolTotal/Assets/_Project` sobre la
   carpeta `Assets` del proyecto, eligiendo **«Reemplazar los archivos en el destino»**.
2. Espera a que compile; la consola no debe tener errores rojos.
3. **Fútbol Total ▸ Crear todas las escenas** (Don't Save si pregunta). Es necesario: las escenas nuevas llevan el sonido.
4. **▶ Play** desde el menú principal. Sube el volumen del ordenador.

## Lista de pruebas

**Sonido**
- [ ] En el menú, los botones hacen clic; Opciones ▸ Volumen cambia 0 / 25 / 50 / 75 / 100 % y se recuerda.
- [ ] En el partido se oye el público de fondo, que sube cuando el balón se acerca a una portería.
- [ ] Silbato al empezar cada saque inicial, en cada falta, en el descanso (largo) y al final (tres pitidos).
- [ ] Los golpeos suenan más fuertes cuanto más fuerte sale el balón; los botes se oyen.
- [ ] Un balón al poste suena metálico; uno a la red, a red.
- [ ] Al marcar, el público celebra.
- [ ] En pausa el público baja.

**Faltas**
- [ ] Entrar (J sin balón) por la espalda de un rival es falta aunque toques el balón: silbato, mensaje «Falta de AUR n (entrada por detrás)».
- [ ] Entrar de frente y llegar al balón roba sin falta.
- [ ] Chocar con un rival sin llegar al balón es falta («no tocó el balón»).
- [ ] Falta fuera del área: tiro libre en ese punto; si es para ti, apuntas y pasas o tiras; si es para la IA, saca sola.
- [ ] Falta dentro de tu área: penalti para el rival; los jugadores salen del área, tu portero está en la línea y la IA tira.
- [ ] Falta sobre ti dentro del área rival: penalti a tu favor; apuntas y tiras con K.
- [ ] La IA casi nunca hace faltas (nunca por detrás).
- [ ] La pantalla de resultado muestra las faltas de cada equipo.
