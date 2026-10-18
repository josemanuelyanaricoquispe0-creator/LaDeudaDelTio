# Diálogos — Fase 1

La escena aislada está en `Assets/_LaDeudaDelTio/DialogueDemo/DialogueDemo.unity`.
Ábrela y pulsa Play; da foco a la pestaña Game para usar el teclado.

- La conversación se abre automáticamente con los retratos del chamán y Doña Rosa.
- Espacio durante la escritura completa la frase actual.
- Otra pulsación avanza; después de la última frase, cierra la pantalla.
- Detén Play y vuelve a iniciarlo para repetir.

El texto está identificado como **PROVISIONAL**. No se encontró un documento de
historia accesible en el proyecto ni en el ZIP revisado y no se atribuye este texto
al guion del juego.

## Configuración

`DialogueData.cs` conserva su contenido original. Crea conversaciones desde
`Create > La Deuda del Tio > Dialogue`; configura `lines`, el nombre, el texto y
ambos retratos en cada línea. Los retratos son sub-sprites existentes: no se
modificaron sus hojas, importadores ni animaciones. Un retrato nulo oculta ese lado.

`DialogueScreen.prefab` contiene el gestor, `DialogueUI` y un Canvas de pantalla
completa. El panel se mantiene oculto hasta iniciar una conversación. La velocidad
de escritura se ajusta en `Characters Per Second` del gestor. La escritura usa
`TMP_Text.maxVisibleCharacters` y tiempo no escalado. El texto se trata como texto
plano, sin interpretar etiquetas rich text.

El componente `DialogueUI` y el gestor deben estar fuera del panel que se oculta.
El prefab ya conserva esta separación. La demo no necesita EventSystem porque
lee Espacio directamente; no contiene botones ni interacción de exploración.

## Conexión futura — Fase 2

- `TryStartDialogue(DialogueData)` devuelve false si ya hay una conversación,
  faltan referencias, no hay líneas o alguna línea es nula.
- `Advance()` completa, avanza o cierra; puede llamarlo un futuro controlador de
  entrada desmarcando `Read Space Input` para evitar lecturas duplicadas.
- `CloseDialogue()` permite cancelar y es idempotente.
- `IsOpen`, `IsRevealing` y `CurrentLineIndex` exponen el estado; el índice es -1
  cuando no hay conversación.
- `DialogueStarted`, `DialogueEnded` y `LineChanged` permiten conectar otros
  sistemas. Desactivar el gestor cierra la conversación y notifica su final.

Esta fase no modifica el movimiento, el ataque ni los sectores, no pausa
`Time.timeScale` y **no bloquea al jugador**. La demo no contiene un jugador.
No incorpores el prefab a una escena de exploración esperando un bloqueo: eso
requiere la coordinación prevista para la siguiente fase.

Los recursos mínimos de TextMeshPro y su licencia de fuente se incluyen con la
demo para evitar depender de una importación manual de TMP Essential Resources.
No se añade la escena a Build Settings ni se cambia la escena inicial del juego.

## Comprobación visual manual

Verifica los dos retratos, los cuatro turnos (Chamán / Doña Rosa), la ñ y los
acentos, el texto progresivo, la distinción entre completar y avanzar y la
desaparición de la pantalla al finalizar. Prueba además una resolución 16:9 y
otra 4:3, y comprueba que mantener Espacio no salta líneas automáticamente.

## Verificaciones realizadas

Los scripts nuevos se compilaron en un proyecto temporal con Unity 6000.5.5f1,
uGUI 2.5.0 e Input System 1.19.0, sin abrir el proyecto original. La escena y el
prefab fueron generados con ese editor y conservan referencias a los GUID de
los sprites originales.

Las pruebas automatizadas en Play Mode, sin renderizado, comprobaron apertura
automática, retratos asignados, cambio de hablante, escritura progresiva con
tiempo no escalado, Espacio mediante un teclado sintético del Input System,
ausencia de repetición al mantenerlo pulsado y cierre final. También pasaron
las comprobaciones de eventos, cierre al desactivar el gestor, rechazo de
conversaciones vacías o duplicadas, retratos nulos y caracteres españoles.
La entrada sintética se configuró para recibir eventos sin foco en Game solo
en el proyecto temporal; esa configuración no se incorpora al juego.

Se revisaron los GUID serializados y la presencia de los `.meta`; no hay
referencias GUID sin resolver ni GUID duplicados en los assets del proyecto.
Git confirmó que los archivos existentes no tienen cambios.

Quedan pendientes la inspección visual en Game, las distintas resoluciones,
la compilación del proyecto completo y una prueba de build. El editor temporal
registró una excepción interna de indexación de `UnityEditor.Search` durante
su inicio; las pruebas del diálogo terminaron correctamente. No se ha validado
visualmente la demo ni se afirma que la Console del proyecto completo esté limpia.
