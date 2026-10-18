using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaDeudaDelTio.Bicicleta.Editor
{
    public static class LDLT_BicicletaValidacion
    {
        private const string Raiz = "Assets/_LaDeudaDelTio/Bicicleta/";

        [MenuItem("Tools/La Deuda del Tio/Bicicleta/Validar paquete")]
        public static void Validar()
        {
            GameObject prueba = null;
            try
            {
                string[] nombres = { "Reposo", "Pedalear", "Saltar" };
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    Raiz + "Controllers/LDLT_AbueloBicicleta.controller");
                Exigir(controller != null && controller.layers.Length == 1, "Animator Controller no disponible.");
                AnimatorStateMachine maquina = controller.layers[0].stateMachine;
                Exigir(maquina.states.Length == 3 && maquina.defaultState.name == "Reposo", "Estados incorrectos.");
                Exigir(controller.parameters.Any(p => p.name == "Velocidad" && p.type == AnimatorControllerParameterType.Float), "Falta Velocidad.");
                Exigir(controller.parameters.Any(p => p.name == "EnSuelo" && p.type == AnimatorControllerParameterType.Bool), "Falta EnSuelo.");
                prueba = new GameObject("LDLT_ValidacionTemporal");
                prueba.hideFlags = HideFlags.HideAndDontSave;
                SpriteRenderer renderer = prueba.AddComponent<SpriteRenderer>();
                for (int n = 0; n < nombres.Length; n++)
                {
                    string nombre = nombres[n];
                    string textura = Raiz + "Textures/LDLT_Bici_" + nombre + ".png";
                    Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(textura).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
                    Exigir(sprites.Length == 8, nombre + ": se esperaban 8 sprites.");
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Raiz + "Animations/LDLT_AbueloBicicleta_" + nombre + ".anim");
                    Exigir(clip != null && Mathf.Approximately(clip.frameRate, 8f), nombre + ": clip o FPS incorrectos.");
                    Exigir(AnimationUtility.GetAnimationClipSettings(clip).loopTime, nombre + ": falta el bucle original.");
                    EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    Exigir(bindings.Length == 1 && bindings[0].path == "" && bindings[0].propertyName == "m_Sprite", nombre + ": curva incorrecta.");
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                    Exigir(keys.Length == 8, nombre + ": cuadros incorrectos.");
                    for (int i = 0; i < 8; i++)
                    {
                        Exigir(keys[i].value == sprites[i] && Mathf.Approximately(keys[i].time, i / 8f), nombre + ": referencia o tiempo incorrecto en " + i);
                        Exigir(sprites[i].rect == new Rect(i * 640, 0, 640, 640), nombre + ": recorte incorrecto.");
                        Exigir(sprites[i].pivot == new Vector2(320, 0) && Mathf.Approximately(sprites[i].pixelsPerUnit, 100f), nombre + ": pivote o escala incorrectos.");
                        // SampleAnimation comprueba que la curva realmente anima el SpriteRenderer.
                        // El ultimo keyframe se comprueba por referencia para evitar el wrap del bucle.
                        if (i < 7)
                        {
                            clip.SampleAnimation(prueba, i / 8f + 0.001f);
                            Exigir(renderer.sprite == sprites[i], nombre + ": fallo al reproducir el cuadro " + i);
                        }
                    }
                    AnimatorState state = maquina.states.Select(s => s.state).Single(s => s.name == nombre);
                    Exigir(state.motion == clip && state.transitions.Length == 2, nombre + ": motion o transiciones incorrectas.");
                    foreach (AnimatorStateTransition transition in state.transitions)
                        Exigir(transition.destinationState != null && !transition.hasExitTime && transition.duration == 0f, nombre + ": transicion incompleta.");
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "Prefabs/LDLT_AbueloBicicleta.prefab");
                Exigir(prefab != null && prefab.name == "LDLT_AbueloBicicleta", "Prefab no disponible.");
                Exigir(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) == 0, "Hay scripts faltantes.");
                Exigir(prefab.GetComponent<Animator>().runtimeAnimatorController == controller, "Controller del prefab incorrecto.");
                Exigir(prefab.GetComponent<SpriteRenderer>().sprite != null, "Sprite inicial faltante.");
                Exigir(prefab.GetComponent<LDLT_BicicletaMovimiento>() != null, "Movimiento independiente faltante.");
                Debug.Log("[LDLT Bicicleta] OK: 3 clips, 24 sprites, 8 FPS, curvas, 6 transiciones y prefab verificados.", prefab);
                Selection.activeObject = prefab;
            }
            catch (Exception error)
            {
                Debug.LogError("[LDLT Bicicleta] " + error.Message);
                throw;
            }
            finally
            {
                if (prueba != null) UnityEngine.Object.DestroyImmediate(prueba);
            }
        }

        private static void Exigir(bool condicion, string mensaje)
        {
            if (!condicion) throw new InvalidOperationException(mensaje);
        }
    }
}
