#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Crea una COPIA independiente de 01_Pueblo. No cambia los assets de SpriteCook.
public static class ConstruirPueblo10Sectores
{
    private const string EscenaOriginal = "Assets/_LaDeudaDelTio/Scenes/01_Pueblo.unity";
    private const string CarpetaFondos = "Assets/_LaDeudaDelTio/Sprites/Escenarios/Pueblo";
    private static readonly string[] Nombres =
    {
        "01_CasaDelYatiri", "02_BarrioDeViviendas", "03_PlazaEIglesia",
        "04_MercadoNocturno", "05_CalleDeLaCurandera", "06_BarrioDeLosMineros",
        "07_CalleMisteriosa", "08_BarrioAntiguo", "09_CasaDeLaHija",
        "10_AfuerasDelPueblo"
    };

    [MenuItem("Tools/La Deuda del Tio/ARMAR PUEBLO - 10 sectores (copia segura)")]
    public static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Primero detener Play", "Deten el juego antes de armar el pueblo.", "Aceptar");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaOriginal) == null)
        {
            EditorUtility.DisplayDialog("No se encuentra 01_Pueblo", "Falta la escena original: " + EscenaOriginal, "Aceptar");
            return;
        }

        for (int i = 0; i < Nombres.Length; i++)
        {
            string ruta = CarpetaFondos + "/" + Nombres[i] + ".png";
            if (!File.Exists(ruta))
            {
                EditorUtility.DisplayDialog("Falta una imagen", "No se encuentra: " + ruta, "Aceptar");
                return;
            }
        }

        if (!EditorUtility.DisplayDialog("Armar pueblo", "Se creara una copia NUEVA de 01_Pueblo con los 10 fondos, camara y suelo invisible. La escena original y el Player original no se sobrescribiran. ¿Continuar?", "Armar copia", "Cancelar")) return;

        try
        {
            // Ajustar los importadores de los nuevos PNG (sin tocar PNG antiguos).
            for (int i = 0; i < Nombres.Length; i++)
            {
                string ruta = CarpetaFondos + "/" + Nombres[i] + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (importer == null) throw new Exception("No se puede importar PNG: " + ruta);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }

            // Copia el archivo antes de modificar la escena: nunca edita 01_Pueblo.unity.
            string nuevaRuta = AssetDatabase.GenerateUniqueAssetPath("Assets/_LaDeudaDelTio/Scenes/01_Pueblo_Armado.unity");
            if (!AssetDatabase.CopyAsset(EscenaOriginal, nuevaRuta))
                throw new Exception("No se pudo crear la copia de seguridad de la escena.");
            AssetDatabase.Refresh();
            Scene escena = EditorSceneManager.OpenScene(nuevaRuta, OpenSceneMode.Single);

            GameObject jugador = EncontrarEnRaiz(escena, "Player");
            if (jugador == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SpriteCook/Prefabs/Player.prefab");
                if (prefab == null) throw new Exception("No se encuentra el Player de SpriteCook ni su prefab.");
                jugador = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            }
            jugador.SetActive(true);
            // Solo en esta copia, el abuelo se escala para que encaje con puertas y calles.
            jugador.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
            jugador.transform.position = new Vector3(-8.4f, -0.70f, 0f);
            SpriteRenderer jugadorSprite = jugador.GetComponent<SpriteRenderer>();
            if (jugadorSprite != null) jugadorSprite.sortingOrder = 10;
            SpriteCookPlayerMovement movement = jugador.GetComponent<SpriteCookPlayerMovement>();
            if (movement != null)
            {
                movement.walkSpeed = 3f;
                movement.runSpeed = 6f;
                movement.jumpForce = 9.5f;
            }

            GameObject antiguo = EncontrarEnRaiz(escena, "AbueloChaman");
            if (antiguo != null && antiguo != jugador) antiguo.SetActive(false);

            GameObject piso = EncontrarEnRaiz(escena, "PisoPrueba");
            if (piso == null)
            {
                piso = new GameObject("PisoInvisible_Pueblo");
                piso.AddComponent<BoxCollider2D>();
            }
            piso.transform.position = new Vector3(0f, -0.98f, 0f);
            piso.transform.localScale = new Vector3(21.2f, 0.50f, 1f);
            SpriteRenderer pisoSprite = piso.GetComponent<SpriteRenderer>();
            if (pisoSprite != null) pisoSprite.enabled = false;
            BoxCollider2D pisoCollider = piso.GetComponent<BoxCollider2D>();
            if (pisoCollider == null) pisoCollider = piso.AddComponent<BoxCollider2D>();
            pisoCollider.enabled = true;
            pisoCollider.isTrigger = false;
            piso.SetActive(true);

            GameObject mainCam = EncontrarEnRaiz(escena, "Main Camera");
            Camera camara = mainCam != null ? mainCam.GetComponent<Camera>() : Camera.main;
            if (camara == null) throw new Exception("No se encuentra Main Camera.");
            camara.orthographic = true;
            camara.orthographicSize = 3.42f;
            camara.transform.position = new Vector3(-3.5f, 0f, -10f);

            GameObject raiz = new GameObject("PUEBLO_10_SECTORES");
            var sectores = new List<GameObject>();
            for (int i = 0; i < Nombres.Length; i++)
            {
                GameObject sector = new GameObject("Sector_" + Nombres[i]);
                sector.transform.SetParent(raiz.transform, false);
                SpriteRenderer fondo = sector.AddComponent<SpriteRenderer>();
                fondo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaFondos + "/" + Nombres[i] + ".png");
                if (fondo.sprite == null) throw new Exception("Fallo al cargar el Sprite: " + Nombres[i]);
                fondo.sortingOrder = -100;
                sector.SetActive(i == 0);
                sectores.Add(sector);
            }

            PuebloSectoresRuntime controlador = raiz.AddComponent<PuebloSectoresRuntime>();
            controlador.jugador = jugador.transform;
            controlador.camara = camara;
            controlador.sectores = sectores.ToArray();
            controlador.anchoSector = 21.72f;
            controlador.limitePaso = 9.45f;
            controlador.reaparecerX = 8.55f;
            controlador.alturaCamara = 0f;
            controlador.duracionFundido = 0.18f;

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            // El nuevo pueblo queda como primera escena de la compilacion.
            var escenasCompilacion = new List<EditorBuildSettingsScene>();
            escenasCompilacion.Add(new EditorBuildSettingsScene(nuevaRuta, true));
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
                if (e.path != nuevaRuta) escenasCompilacion.Add(e);
            EditorBuildSettings.scenes = escenasCompilacion.ToArray();

            Selection.activeObject = raiz;
            EditorGUIUtility.PingObject(raiz);
            EditorUtility.DisplayDialog("¡Pueblo armado!", "Se abrio una COPIA nueva: " + nuevaRuta + "\n\n10 sectores preparados con transicion oscura, suelo invisible y camara. Pulsa Play y recorre con A/D. No se modifico 01_Pueblo.unity.", "¡A probar!");
            Debug.Log("[LaDeudaDelTio] Pueblo armado correctamente: " + nuevaRuta);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error armando el pueblo", ex.Message + "\n\nNo borres la escena original. Envia el mensaje rojo de Console.", "Aceptar");
        }
    }

    private static GameObject EncontrarEnRaiz(Scene escena, string nombre)
    {
        foreach (GameObject go in escena.GetRootGameObjects())
            if (go.name == nombre) return go;
        return null;
    }
}
#endif
