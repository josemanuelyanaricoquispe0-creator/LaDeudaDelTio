# Fase 2 — Pueblo con diálogos

## Abrir la prueba

1. Abre `Assets/_LaDeudaDelTio/DialogueDemo/Pueblo_Dialogos_Test.unity`.
2. Pulsa Play y da foco a la pestaña Game.
3. Camina hacia la derecha con A/D o las flechas. Shift permite correr.
4. Al acercarte al primer NPC, el minero, aparecerá `[E] HABLAR`.
5. Pulsa E para abrir la conversación. Espacio completa la frase que se está
   escribiendo; otra pulsación avanza. La última pulsación cierra la pantalla.
6. Comprueba que A/D, Shift, Espacio y J no ejecutan acciones del jugador durante
   la conversación. Al terminar puedes moverte, saltar, atacar y hablar otra vez.

Los NPC conservan sus posiciones de la escena original. De izquierda a derecha
están el minero, la vendedora, Doña Rosa, la curandera y el habitante misterioso.
No hace falta añadir la prueba a Build Settings para abrirla y usar Play.

Las cinco conversaciones de `Conversations/` son **PROVISIONALES**. No se encontró
`GameJam.pdf` en el proyecto ni en el ZIP disponible; no son diálogos del guion.
La conversación y la escena de la Fase 1 se conservan por separado.

## Configuración desde el Inspector

Cada NPC tiene `NPCDialogueInteractable`:

- `Dialogue`: asset `DialogueData` independiente, editable desde el Inspector.
- `Interaction Radius`: distancia en unidades del mundo, inicialmente 1.3.
- `Interaction Origin`: Transform opcional para ubicar el centro de interacción.
- `Interaction Offset`: desplazamiento en unidades del mundo. Sin origen explícito
  se utiliza el centro del SpriteRenderer.

Al seleccionar el NPC, el gizmo muestra el radio. No se necesitan colliders nuevos
ni un tag Player. El interactor utiliza el centro del collider del jugador y elige
un único NPC cercano; en un empate conserva el orden de registro de los NPC.

El objeto `Player` de esta copia incorpora `PlayerControlLock` y
`PlayerDialogueInteractor`. El interactor referencia al `DialogueManager` del
prefab existente y a la indicación del HUD. El sistema no abre diálogos al iniciar
esta escena: requiere acercarse y pulsar E.

Se reutilizan `DialogueScreen.prefab`, `DialogueUI`, `DialogueManager`,
`DialogueData` y los sprites existentes. No se duplicó el sistema de conversación.
La velocidad de escritura sigue configurándose en el gestor.

## Coordinación de controles

`PlayerControlLock.Acquire(owner)` añade un bloqueo por propietario y
`Release(owner)` retira únicamente el de ese propietario. Repetir Acquire con el
mismo propietario no acumula bloqueos. Un propietario destruido se limpia en
LateUpdate. Cada sistema debe liberar su bloqueo al finalizar o desactivarse.

El interactor escucha los eventos de inicio y cierre del gestor, también si otra
funcionalidad abre una conversación directamente. El ataque utiliza su propio
bloqueo cuando existe este componente. Movimiento consulta el bloqueo en Update
y FixedUpdate, y limpia desplazamiento, sprint y salto pendientes cuando cambia
el estado. La liberación conserva el bloqueo de entrada hasta terminar ese frame,
de modo que el Espacio de cierre no se reutilice como salto.

No se cambia `Time.timeScale`, la gravedad, las constraints ni `Rigidbody2D.simulated`.
Se detiene la velocidad horizontal; la física vertical sigue funcionando. Ningún
bloqueo activa un componente de movimiento desactivado por otro sistema.

Durante ataques no se ofrecen nuevas interacciones. Si un sistema externo ha
desactivado movimiento, E y J se ignoran. Desactivar un NPC durante una conversación
retira su proximidad, pero el jugador conserva sus referencias y puede terminar
el diálogo normalmente. Desactivar el interactor cierra la conversación y libera
su bloqueo; desactivar el gestor también notifica el cierre.

`PuebloSectoresRuntime` no se modificó. La escena de prueba usa la cámara continua.
La integración real con los sectores debe comprobarse por separado antes de
incorporar estos componentes a la escena por sectores.

Los jugadores sin `PlayerControlLock` mantienen el camino de funcionamiento
anterior del ataque. No se modificó el prefab Player compartido ni se añadieron
componentes a las escenas oficiales.

## Alcance de los cambios

Archivos nuevos: los tres componentes de integración, esta guía, la escena de
prueba y los cinco assets de conversación, con sus metadatos.

Archivos existentes modificados: `SpriteCookPlayerMovement.cs` y
`AbueloAtaqueCuchillo.cs`, para consultar y coordinar los bloqueos. Los archivos
de la Fase 1, las escenas oficiales, los sprites, importadores, animaciones,
controladores y ajustes compartidos quedan fuera de estos cambios.

Antes de comenzar ya había modificaciones en animaciones de SpriteCook,
`Character.controller`, `Player.prefab`, `SpriteCookPlatformerDemo.unity`,
`UniversalRenderPipelineGlobalSettings.asset`, `EditorBuildSettings.asset` y
`ProjectSettings.asset`. No se descartan ni se incluyen como cambios de esta fase.

## Generador automático preexistente de SpriteCook

`Assets/SpriteCook/Editor/SpriteCookCharacterSetup.cs` tiene un inicializador
automático. Si falta su firma de importación en las preferencias del editor,
puede regenerar el controlador, el prefab Player y su escena de demostración.
En la copia temporal se observó además un intento de generar esa escena durante
Play Mode, que produjo una excepción de `EditorSceneManager.NewScene`.

Ese generador no se modificó. Para validar los assets actuales, el comprobador
temporal configura exclusivamente la firma correspondiente a su propia ruta de
proyecto y utiliza copias de los assets existentes. Esa configuración y el
comprobador no se incorporan al proyecto compartido. Conviene revisar este
comportamiento del generador con el equipo antes de automatizar importaciones
desde cero; no forma parte del sistema de diálogos.

## Resultado de las verificaciones

Compilación completa y comprobaciones automatizadas de Play Mode aprobadas en una
copia temporal del proyecto con Unity 6000.5.5f1. Se utilizaron los assets actuales
del equipo, sin regenerar el controlador ni el prefab durante la ejecución final.
El comprobador utilizó un teclado sintético del Input System nuevo; sus ajustes
para recibir entrada sin foco no se trasladaron al proyecto compartido.

Pasaron las siguientes comprobaciones:

- Cinco conversaciones independientes, jugador Untagged, ausencia de apertura
  automática y ocultación del indicador fuera del radio.
- Caminar, correr y saltar antes de conversar, conservando la fricción existente.
- Selección del NPC más cercano y una sola apertura con E, incluso al pulsar
  simultáneamente E, J, Espacio y movimiento.
- Bloqueo de movimiento, sprint, salto y ataque durante la conversación;
  limpieza del salto pendiente y ausencia de velocidad horizontal residual.
- Retratos y nombres correctos de los cinco NPC; texto progresivo; Espacio para
  completar y avanzar, sin repetición automática al mantenerlo pulsado.
- Cierre final sin salto o ataque por la misma pulsación, recuperación de los
  controles y posibilidad de volver a conversar.
- NPC desactivado durante el diálogo sin perder referencias ni dejar bloqueos;
  limpieza al desactivar y reactivar el interactor o el gestor.
- Bloqueos superpuestos por distintos propietarios: finalizar un ataque o un
  diálogo conserva el bloqueo ajeno. Se simuló una transición desactivando el
  movimiento; el ataque no lo reactivó y E/J siguieron bloqueadas.
- Ataque sin PlayerControlLock en la copia intacta de la escena original y
  regresión de la demostración de Fase 1: apertura y cierre correctos.

La comparación SHA-256 de todos los archivos existentes al comenzar identificó
únicamente los dos scripts autorizados como modificados. Las escenas oficiales,
los archivos de Fase 1 y los cambios previos del equipo permanecen idénticos.
Los 21 archivos nuevos incluyen metadatos; no hay GUID duplicados y las referencias
serializadas se resuelven en los assets del proyecto o sus paquetes existentes.

Las pruebas se ejecutaron sin renderizado. Quedan pendientes la revisión visual
en Game, diferentes resoluciones, una compilación de build y la transición real
del pueblo por sectores. El entorno temporal registró errores de hojas de estilo
de herramientas 2D durante la importación y una excepción interna de
`UnityEditor.Search.SearchDatabase` al iniciar el editor; no fueron errores de
compilación de los diálogos y las comprobaciones funcionales finales pasaron.

## Archivos nuevos de esta fase

En `Assets/_LaDeudaDelTio/Scripts/Dialogue/`:

- `NPCDialogueInteractable.cs`
- `PlayerDialogueInteractor.cs`
- `PlayerControlLock.cs`

En `Assets/_LaDeudaDelTio/DialogueDemo/`:

- `Pueblo_Dialogos_Test.unity`
- `FASE2_LEEME.md`
- `Conversations/Dialogue_Vendedora_PROVISIONAL.asset`
- `Conversations/Dialogue_DonaRosa_PROVISIONAL.asset`
- `Conversations/Dialogue_Curandera_PROVISIONAL.asset`
- `Conversations/Dialogue_Minero_PROVISIONAL.asset`
- `Conversations/Dialogue_HabitanteMisterioso_PROVISIONAL.asset`

Cada archivo tiene su `.meta`; también se añadió `Conversations.meta` para la
carpeta nueva. No se ejecutaron commits, push ni cambios de rama.
