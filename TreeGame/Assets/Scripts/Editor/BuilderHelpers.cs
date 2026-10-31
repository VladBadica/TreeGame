using System.IO;
using TreeGame.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TreeGame.EditorTools
{
    /// <summary>General building blocks shared by the scene builders: assets, serialized references and scene parts.</summary>
    internal static class BuilderHelpers
    {
        private const string MaterialsFolder = "Assets/Materials";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>Assigns private [SerializeField] references the same way the Inspector would.</summary>
        public static void SetReferences(Object target, params (string propertyName, Object value)[] references)
        {
            var serializedObject = new SerializedObject(target);
            foreach ((string propertyName, Object value) in references)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name} has no serialized field '{propertyName}'.", target);
                    continue;
                }

                if (value == null)
                {
                    Debug.LogError($"{target.GetType().Name}.{propertyName}: tried to assign a missing object.", target);
                }

                property.objectReferenceValue = value;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a private [SerializeField] array of references.</summary>
        public static void SetReferenceArray(Object target, string propertyName, params Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Starts generating a scene: asks before replacing an existing one and offers to save open changes, then
        /// opens an empty scene. Returns false if the user cancelled. Opening a scene unloads unused assets, so
        /// callers must load or create assets after this; otherwise fresh ones are gone by the time they are assigned.
        /// </summary>
        public static bool TryStartNewScene(string scenePath, string dialogTitle, out Scene scene)
        {
            scene = default;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null &&
                !EditorUtility.DisplayDialog(dialogTitle, $"{scenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }

        /// <summary>Saves a generated scene and makes sure it is in the build.</summary>
        public static void SaveGeneratedScene(Scene scene, string scenePath)
        {
            EditorSceneManager.SaveScene(scene, scenePath);
            BuildSceneList.Add(scenePath);
        }

        /// <summary>Creates a folder directly inside an existing one, unless it already exists.</summary>
        public static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
            }
        }

        public static Material GetOrCreateMaterial(string materialName, Color color, bool unlit = false)
        {
            string path = $"{MaterialsFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(MaterialsFolder);

            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("URP Lit shader not found; falling back to the render pipeline's default shader.");
                shader = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.defaultShader
                    : Shader.Find("Standard");
            }

            material = new Material(shader) { name = materialName };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f); // Matte look suits the soft, cozy style.

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Finds the reference asset Unity generates for an action inside the project's input actions.</summary>
        public static InputActionReference FindActionReference(string mapName, string actionName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
            {
                if (asset is InputActionReference reference && reference.action != null &&
                    reference.action.name == actionName && reference.action.actionMap.name == mapName)
                {
                    return reference;
                }
            }

            Debug.LogError($"No '{mapName}/{actionName}' action found in {InputActionsPath}.");
            return null;
        }

        public static Transform CreatePivot(string objectName, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(objectName).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        /// <summary>A collider-free primitive, for parts that are only visual.</summary>
        public static Transform CreatePart(PrimitiveType type, string objectName, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.transform;
        }

        /// <summary>
        /// A trigger zone the player can stand in to see a prompt and interact. It is deep enough (along Z) to reach
        /// the player's walking line wherever the owner stands, and only as wide as given.
        /// </summary>
        public static Interactable CreateInteractionZone(Transform owner, float width, float promptHeight,
            string actionDescription, string promptKeyOverride = "")
        {
            var zone = new GameObject("Interaction Zone", typeof(BoxCollider));
            zone.transform.SetParent(owner, false);
            var zoneCollider = zone.GetComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.center = new Vector3(0f, 1f, 0f);
            zoneCollider.size = new Vector3(width, 2f, 6f);

            Transform promptAnchor = CreatePivot("Prompt Anchor", owner, new Vector3(0f, promptHeight, 0f));
            var interactable = zone.AddComponent<Interactable>();
            SetReferences(interactable, ("promptAnchor", promptAnchor));
            var serializedInteractable = new SerializedObject(interactable);
            serializedInteractable.FindProperty("actionDescription").stringValue = actionDescription;
            serializedInteractable.FindProperty("promptKeyOverride").stringValue = promptKeyOverride;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();
            return interactable;
        }
    }
}
