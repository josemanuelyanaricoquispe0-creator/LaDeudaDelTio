#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// SOLO Editor: configura las 5 escenas sin borrar Main Camera, audio, TEMITA
// ni otros objetos del proyecto. Se puede ejecutar mas de una vez.
public static class LDLT_ConfigurarCinematicas
{
    private const string RutaEscenas = "Assets/_LaDeudaDelTio/Scenes";
    private const string RutaImagenes = "Assets/_LaDeudaDelTio/Escenas";
    private const string NombreSistema = "LDLT_CinematicaAuto";

    private class Datos
    {
        public string Codigo;
        public string Escena;
        public int Cantidad;
        public string Siguiente;
        public bool EsFinal;
        public string RutaArchivoEscena;
        public Sprite[] Sprites;
    }

    [MenuItem("La Deuda del Tio/Cinematicas/Configurar las 5 cinematicas")]
    public static void ConfigurarTodas()
    {
        Datos[] todas =
        {
            new Datos { Codigo = "00", Escena = "00_Cinematica_Introduccion", Cantidad = 6, Siguiente = "01_Pueblo_Dialogos_Integracion" },
            new Datos { Codigo = "02", Escena = "02_Cinematica_Cerro", Cantidad = 6, Siguiente = "03_PersecucionBicicleta" },
            new Datos { Codigo = "04", Escena = "04_Cinematica_EncuentroHija", Cantidad = 3, Siguiente = "05_Pueblo_Regreso_Hordas" },
            new Datos { Codigo = "07", Escena = "07_Cinematica_SecretoDiablo", Cantidad = 2, Siguiente = "08_Mina_JefeFinal" },
            new Datos { Codigo = "09", Escena = "09_Cinematica_RescateFinal", Cantidad = 2, EsFinal = true }
        };

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(RutaEscenas) || !AssetDatabase.IsValidFolder(RutaImagenes))
        {
            EditorUtility.DisplayDialog("Cinematicas", "No encuentro las carpetas:\n" +
                RutaEscenas + "\n" + RutaImagenes + "\nRevisa sus nombres en Project.", "Entendido");
            return;
        }

        // Primero validar TODAS las imagenes y escenas, antes de tocar ninguna escena.
        var errores = new List<string>();
        foreach (Datos d in todas)
        {
            d.RutaArchivoEscena = BuscarEscena(d.Escena);
            if (d.RutaArchivoEscena == null)
                errores.Add("No encuentro la escena " + d.Escena);

            string carpeta = BuscarCarpeta(d.Codigo);
            if (carpeta == null)
            {
                errores.Add("No encuentro la carpeta de imagenes que empiece por " + d.Codigo + "_");
                continue;
            }

            d.Sprites = new Sprite[d.Cantidad];
            for (int i = 1; i <= d.Cantidad; i++)
            {
                string archivo = BuscarImagenNumerada(carpeta, i);
                if (archivo == null)
                {
                    errores.Add("Falta " + i + ".png/.jpg en " + carpeta);
                    continue;
                }

                d.Sprites[i - 1] = ObtenerSprite(archivo);
                if (d.Sprites[i - 1] == null)
                    errores.Add("No pude importar como Sprite: " + archivo);
            }
        }

        if (errores.Count > 0)
        {
            string mensaje = string.Join("\n", errores);
            Debug.LogError("LDLT CINEMATICAS: " + mensaje);
            EditorUtility.DisplayDialog("Revisa las imagenes", mensaje, "Entendido");
            return;
        }

        try
        {
            for (int i = 0; i < todas.Length; i++)
            {
                Datos d = todas[i];
                EditorUtility.DisplayProgressBar("Configurando cinematicas", d.Escena, (float)i / todas.Length);
                Scene escena = EditorSceneManager.OpenScene(d.RutaArchivoEscena, OpenSceneMode.Single);
                CrearInterfaz(escena, d);
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                Debug.Log("CINEMATICA preparada: " + d.Escena + " (" + d.Cantidad + " imagenes)");
            }

            // Agrega solo escenas ausentes; NO reordena ni quita el menu del companero.
            List<EditorBuildSettingsScene> lista = EditorBuildSettings.scenes.ToList();
            foreach (Datos d in todas)
            {
                AsegurarEscenaEnBuild(lista, d.RutaArchivoEscena);
                if (!d.EsFinal)
                {
                    string rutaDestino = BuscarEscena(d.Siguiente);
                    if (rutaDestino != null) AsegurarEscenaEnBuild(lista, rutaDestino);
                }
            }
            EditorBuildSettings.scenes = lista.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(todas[0].RutaArchivoEscena, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("¡LISTO!", "Las 5 cinematicas estan configuradas (19 imagenes).\n\n" +
                "Ya esta abierta la escena 00. Pulsa PLAY y ENTER para probar.\n" +
                "No se modificaron las escenas jugables ni el objeto TEMITA.", "¡Probar!");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error al configurar", ex.Message + "\nConsulta la Console.", "Entendido");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string BuscarEscena(string nombre)
    {
        string directa = RutaEscenas + "/" + nombre + ".unity";
        if (File.Exists(directa)) return directa;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { RutaEscenas }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre) return ruta;
        }
        return null;
    }

    private static string BuscarCarpeta(string codigo)
    {
        string[] carpetas = AssetDatabase.GetSubFolders(RutaImagenes);
        return carpetas.FirstOrDefault(ruta =>
            Path.GetFileName(ruta).StartsWith(codigo + "_", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuscarImagenNumerada(string carpeta, int numero)
    {
        string[] extensiones = { ".png", ".jpg", ".jpeg" };
        foreach (string guid in AssetDatabase.FindAssets("", new[] { carpeta }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(ruta)?.Replace('\\', '/') != carpeta) continue;
            if (Path.GetFileNameWithoutExtension(ruta) != numero.ToString()) continue;
            if (extensiones.Contains(Path.GetExtension(ruta).ToLowerInvariant())) return ruta;
        }
        return null;
    }

    private static Sprite ObtenerSprite(string ruta)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite != null) return sprite;
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador != null)
        {
            // Las 19 ilustraciones son imagenes completas, NO hojas de animacion.
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.maxTextureSize = 4096;
            importador.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
        return sprite;
    }

    private static void CrearInterfaz(Scene escena, Datos d)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.name == NombreSistema)
            {
                // Solo actualiza el sistema autogenerado, NO los demas objetos.
                UnityEngine.Object.DestroyImmediate(raiz);
                break;
            }
        }

        GameObject sistema = new GameObject(NombreSistema,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(LDLT_CinematicaImagenes));
        Canvas canvas = sistema.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = sistema.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image fondo = CrearImagen(sistema.transform, "FondoNegro", Color.black);
        fondo.raycastTarget = false;

        Image ilustracion = CrearImagen(sistema.transform, "ImagenNarrativa", Color.white);
        ilustracion.preserveAspect = true;
        ilustracion.raycastTarget = false;
        ilustracion.sprite = d.Sprites[0];
        CanvasGroup grupoIlustracion = ilustracion.gameObject.AddComponent<CanvasGroup>();

        GameObject pantallaFin = null;
        CanvasGroup grupoFin = null;
        if (d.EsFinal)
        {
            Image panel = CrearImagen(sistema.transform, "PantallaFIN", new Color(0.035f, 0.015f, 0.02f, 1f));
            panel.raycastTarget = false;
            pantallaFin = panel.gameObject;
            grupoFin = pantallaFin.AddComponent<CanvasGroup>();
            grupoFin.alpha = 0f;

            CrearTexto(pantallaFin.transform, "TituloFinal", "LA DEUDA DEL TÍO", 100,
                new Vector2(0.5f, 0.57f), new Vector2(0.95f, 0.21f));
            CrearTexto(pantallaFin.transform, "Fin", "FIN", 90,
                new Vector2(0.5f, 0.37f), new Vector2(0.9f, 0.18f));
            CrearTexto(pantallaFin.transform, "Mensaje", "El amor de una familia vale más que todo el oro de una montaña.", 36,
                new Vector2(0.5f, 0.20f), new Vector2(0.95f, 0.12f));
            pantallaFin.SetActive(false);
        }

        sistema.GetComponent<LDLT_CinematicaImagenes>().Preparar(
            d.Sprites, d.Siguiente, d.EsFinal, ilustracion, grupoIlustracion, pantallaFin, grupoFin);
    }

    private static Image CrearImagen(Transform padre, string nombre, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        return imagen;
    }

    private static void CrearTexto(Transform padre, string nombre, string contenido,
        int tamano, Vector2 centro, Vector2 proporcion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = centro;
        rect.anchorMax = centro;
        rect.sizeDelta = Vector2.Scale(proporcion, new Vector2(1920, 1080));
        rect.anchoredPosition = Vector2.zero;
        Text texto = objeto.GetComponent<Text>();
        texto.text = contenido;
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = tamano;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 18;
        texto.resizeTextMaxSize = tamano;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(1f, 0.88f, 0.67f);
        texto.raycastTarget = false;
    }

    private static void AsegurarEscenaEnBuild(List<EditorBuildSettingsScene> lista, string ruta)
    {
        EditorBuildSettingsScene existente = lista.FirstOrDefault(e => e.path == ruta);
        if (existente == null) lista.Add(new EditorBuildSettingsScene(ruta, true));
        else existente.enabled = true;
    }
}
#endif
