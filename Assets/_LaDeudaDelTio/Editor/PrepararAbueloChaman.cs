using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Asistente no destructivo para enlazar las 9 animaciones de SpriteCook.
public static class PrepararAbueloChaman
{
    const string Clips = "Assets/SpriteCook/Animations/PixelArt";
    const string Controller = "Assets/_LaDeudaDelTio/Animations/AbueloChaman_Completo.controller";
    const string Prefab = "Assets/_LaDeudaDelTio/Prefabs/AbueloChaman_Jugable.prefab";
    const string SourcePrefab = "Assets/SpriteCook/Prefabs/Player.prefab";
    const string Extras = "Assets/_LaDeudaDelTio/Animations/GeneradasAbuelo";

    [MenuItem("Tools/La Deuda del Tio/1 - Preparar animaciones y prefab")]
    public static void Preparar()
    {
        try
        {
            AnimationClip idle = Encontrar("_idle");
            AnimationClip walk = Encontrar("_front_walk_walk");
            AnimationClip run = Encontrar("_front_run_run");
            AnimationClip jump = Encontrar("_jump");
            AnimationClip magic = Encontrar("_custom_1_");
            AnimationClip knife = Encontrar("_custom_2_");
            AnimationClip crouch = Encontrar("_custom_3_");
            AnimationClip hurt = Encontrar("_hurt");
            AnimationClip death = Encontrar("_death");
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);

            if (new[] { idle, walk, run, jump, magic, knife, crouch, hurt, death }.Any(x => x == null) || original == null)
            {
                EditorUtility.DisplayDialog("Faltan recursos de SpriteCook",
                    "Antes importa SpriteCook_Export(4).unitypackage y espera que se generen las nueve animaciones en Assets/SpriteCook/Animations/PixelArt.\n\nNo se ha modificado nada.", "Entendido");
                return;
            }

            CrearCarpeta(Extras);
            AnimationClip magiaUnica = ClonarSinBucle(magic, "Magia");
            AnimationClip cuchilloUnico = ClonarSinBucle(knife, "Cuchillo");
            AnimationClip heridoUnico = ClonarSinBucle(hurt, "Herido");
            AnimationClip muerteUnica = ClonarSinBucle(death, "Muerte");

            Sprite[] secuencia = ObtenerSprites(crouch);
            if (secuencia.Length < 6)
            {
                EditorUtility.DisplayDialog("Animacion no compatible", "Agacharse no tiene los seis fotogramas esperados. No se cambiara el prefab.", "OK");
                return;
            }
            // El clip original se levanta al final. Se separa bajar, mantener y levantar.
            AnimationClip bajar = CrearSecuencia("AgacharseBajar", secuencia.Take(6).ToArray(), crouch.frameRate, false);
            AnimationClip quieto = CrearSecuencia("AgacharseQuieto", new[] { secuencia[5] }, crouch.frameRate, true);
            AnimationClip levantar = CrearSecuencia("AgacharseLevantar", secuencia.Take(6).Reverse().ToArray(), crouch.frameRate, false);

            CrearCarpeta("Assets/_LaDeudaDelTio/Animations");
            CrearCarpeta("Assets/_LaDeudaDelTio/Prefabs");
            AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (controlador == null) controlador = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
            foreach (ChildAnimatorState estado in maquina.states.ToArray()) maquina.RemoveState(estado.state);
            AnimatorState inicial = Agregar(maquina, "Reposo", idle, new Vector3(100, 50, 0));
            Agregar(maquina, "Caminar", walk, new Vector3(330, 50, 0));
            Agregar(maquina, "Correr", run, new Vector3(560, 50, 0));
            Agregar(maquina, "Saltar", jump, new Vector3(100, 150, 0));
            Agregar(maquina, "Caer", jump, new Vector3(330, 150, 0));
            Agregar(maquina, "AgacharseBajar", bajar, new Vector3(100, 250, 0));
            Agregar(maquina, "AgacharseQuieto", quieto, new Vector3(330, 250, 0));
            Agregar(maquina, "AgacharseLevantar", levantar, new Vector3(560, 250, 0));
            Agregar(maquina, "Magia", magiaUnica, new Vector3(100, 350, 0));
            Agregar(maquina, "Cuchillo", cuchilloUnico, new Vector3(330, 350, 0));
            Agregar(maquina, "Herido", heridoUnico, new Vector3(560, 350, 0));
            Agregar(maquina, "Muerte", muerteUnica, new Vector3(100, 450, 0));
            maquina.defaultState = inicial;
            EditorUtility.SetDirty(controlador);

            GameObject nuevo = new GameObject("AbueloChaman_Jugable");
            try
            {
                SpriteRenderer originalSprite = original.GetComponent<SpriteRenderer>();
                SpriteRenderer sr = nuevo.AddComponent<SpriteRenderer>();
                if (originalSprite != null)
                {
                    sr.sprite = originalSprite.sprite;
                    sr.color = originalSprite.color;
                    sr.sortingLayerID = originalSprite.sortingLayerID;
                    sr.sortingOrder = originalSprite.sortingOrder;
                    sr.sharedMaterial = originalSprite.sharedMaterial;
                }

                Animator animator = nuevo.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                Rigidbody2D body = nuevo.AddComponent<Rigidbody2D>();
                Rigidbody2D originalBody = original.GetComponent<Rigidbody2D>();
                body.gravityScale = originalBody != null ? originalBody.gravityScale : 3f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                CapsuleCollider2D capsule = nuevo.AddComponent<CapsuleCollider2D>();
                CapsuleCollider2D originalCapsule = original.GetComponent<CapsuleCollider2D>();
                if (originalCapsule != null)
                {
                    capsule.size = originalCapsule.size;
                    capsule.offset = originalCapsule.offset;
                    capsule.direction = originalCapsule.direction;
                }
                YatiriJugador jugador = nuevo.AddComponent<YatiriJugador>();
                jugador.velocidadCaminar = 3f;
                jugador.velocidadCorrer = 6f;
                jugador.fuerzaSalto = 9.5f;
                jugador.cuchilloDesbloqueado = false;
                PrefabUtility.SaveAsPrefabAsset(nuevo, Prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(nuevo); }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Abuelo listo",
                "Se prepararon las 9 animaciones, el Animator y el prefab SIN tocar SpriteCook.\n\nAhora abre 01_Pueblo y usa:\nTools > La Deuda del Tio > 2 - Poner abuelo en escena actual", "Perfecto");
            Debug.Log("[La Deuda del Tio] Creado: " + Controller + " y " + Prefab);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Error al preparar abuelo", e.Message + "\nMira la pestaña Console.", "OK");
        }
    }

    [MenuItem("Tools/La Deuda del Tio/2 - Poner abuelo en escena actual")]
    public static void PonerEnEscena()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Deten el juego", "Presiona Stop antes de modificar la escena.", "OK");
            return;
        }
        Scene escena = SceneManager.GetActiveScene();
        if (!escena.IsValid() || escena.name != "01_Pueblo")
        {
            EditorUtility.DisplayDialog("Escena incorrecta", "Abre primero Assets/_LaDeudaDelTio/Scenes/01_Pueblo.unity y vuelve a intentarlo. No se cambiara la demo.", "OK");
            return;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Falta prefab", "Ejecuta antes la opcion 1: Preparar animaciones y prefab.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Colocar al abuelo",
            "Creare AbueloChaman_Jugable en 01_Pueblo y desactivare (SIN borrar) el Player o AbueloChaman anteriores, si existen.\n\nContinuar?", "Si, colocar", "Cancelar")) return;

        GameObject existente = GameObject.Find("AbueloChaman_Jugable");
        if (existente != null)
        {
            Selection.activeGameObject = existente;
            EditorGUIUtility.PingObject(existente);
            EditorUtility.DisplayDialog("Ya esta colocado", "AbueloChaman_Jugable ya existe en la escena. No cree un duplicado.", "OK");
            return;
        }

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if ((raiz.name == "Player" || raiz.name == "AbueloChaman" || raiz.name == "SpriteCook Player") && raiz.activeSelf)
            {
                Undo.RecordObject(raiz, "Conservar abuelo anterior desactivado");
                raiz.SetActive(false);
            }
        }

        GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
        Undo.RegisterCreatedObjectUndo(copia, "Añadir AbueloChaman jugable");
        copia.name = "AbueloChaman_Jugable";
        float posicionY = -2.9f;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.name == "PisoPrueba")
            {
                Collider2D col = raiz.GetComponent<Collider2D>();
                if (col != null) posicionY = col.bounds.max.y + 0.25f;
            }
        }
        copia.transform.position = new Vector3(-1.5f, posicionY, 0f);
        Selection.activeGameObject = copia;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorUtility.DisplayDialog("Colocado", "AbueloChaman_Jugable esta en 01_Pueblo.\n\nPresiona Ctrl+S y despues Play. El prefab original se conservo.", "Listo");
    }

    private static AnimationClip Encontrar(string parte)
    {
        if (!AssetDatabase.IsValidFolder(Clips)) return null;
        return AssetDatabase.FindAssets("t:AnimationClip", new[] { Clips })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(p => AssetDatabase.LoadAssetAtPath<AnimationClip>(p))
            .FirstOrDefault(c => c != null);
    }

    private static AnimatorState Agregar(AnimatorStateMachine m, string nombre, AnimationClip c, Vector3 pos)
    {
        AnimatorState e = m.AddState(nombre, pos);
        e.motion = c;
        return e;
    }

    private static AnimationClip ClonarSinBucle(AnimationClip origen, string nombre)
    {
        string ruta = Extras + "/" + nombre + ".anim";
        AnimationClip destino = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
        if (destino == null)
        {
            destino = new AnimationClip { name = nombre };
            AssetDatabase.CreateAsset(destino, ruta);
        }
        destino.name = nombre;
        destino.frameRate = origen.frameRate;
        foreach (EditorCurveBinding curva in AnimationUtility.GetObjectReferenceCurveBindings(destino))
            AnimationUtility.SetObjectReferenceCurve(destino, curva, null);
        foreach (EditorCurveBinding curva in AnimationUtility.GetObjectReferenceCurveBindings(origen))
            AnimationUtility.SetObjectReferenceCurve(destino, curva, AnimationUtility.GetObjectReferenceCurve(origen, curva));
        destino.wrapMode = WrapMode.Once;
        var ajustes = AnimationUtility.GetAnimationClipSettings(destino);
        ajustes.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(destino, ajustes);
        EditorUtility.SetDirty(destino);
        return destino;
    }

    private static Sprite[] ObtenerSprites(AnimationClip clip)
    {
        EditorCurveBinding curva = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(c => c.propertyName == "m_Sprite");
        if (String.IsNullOrEmpty(curva.propertyName)) return new Sprite[0];
        return AnimationUtility.GetObjectReferenceCurve(clip, curva)
            .OrderBy(k => k.time).Select(k => k.value as Sprite).Where(s => s != null).ToArray();
    }

    private static AnimationClip CrearSecuencia(string nombre, Sprite[] cuadros, float fps, bool repetir)
    {
        string ruta = Extras + "/" + nombre + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
        if (clip == null)
        {
            clip = new AnimationClip { name = nombre };
            AssetDatabase.CreateAsset(clip, ruta);
        }
        clip.name = nombre;
        clip.frameRate = Mathf.Max(1f, fps);
        EditorCurveBinding curva = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        ObjectReferenceKeyframe[] claves = cuadros.Select((s, i) => new ObjectReferenceKeyframe { time = i / clip.frameRate, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, curva, claves);
        clip.wrapMode = repetir ? WrapMode.Loop : WrapMode.Once;
        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = repetir;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CrearCarpeta(string ruta)
    {
        string[] segmentos = ruta.Split('/');
        string actual = segmentos[0];
        for (int i = 1; i < segmentos.Length; i++)
        {
            string siguiente = actual + "/" + segmentos[i];
            if (!AssetDatabase.IsValidFolder(siguiente)) AssetDatabase.CreateFolder(actual, segmentos[i]);
            actual = siguiente;
        }
    }
}
