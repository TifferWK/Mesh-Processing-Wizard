using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//C.S. Woodward 2024
//this tool allows you to organize meshes for Unity, I was suggested to do this tool by a friend on Twitter and I made it in the 
//hopes that others may find use in it. I know that it is very annoying to see a bunch of dummy default materials in your 
//unity material quick selector... makes it not very quick... 
public class MeshProcessorAndOrganizer : EditorWindow
{
    private List<Object> selectedMeshes = new List<Object>();
    private Vector2 scrollPos;
    private Material intendedMaterial = null;
    private string newFolderName = "";
    private string newFolderPath = "Assets";
    private string existingFolderPath = "";
    private bool useNewFolder = true;
    private Texture2D headerTexture;
    private bool deleteOriginalObjects = false;
    private bool modifyOriginalMode = false;



    [MenuItem("Assets/Big Shoe Development Suite/Mesh Processor and Organizer", false, 2000)]
    public static void ShowWindowFromContext()
    {
        MeshProcessorAndOrganizer window = GetWindow<MeshProcessorAndOrganizer>("Mesh Processor Wizard");
        window.Initialize(Selection.objects);
        window.Show();
    }

    private void Initialize(Object[] selectedObjects)
    {
        selectedMeshes.Clear();
        foreach (Object obj in selectedObjects)
        {
            if (obj is Mesh || obj is GameObject)
            {
                selectedMeshes.Add(obj);
            }
        }
    }

    private void OnEnable()
    {
        string headerPath = "Assets/Editor/BigShoeTools/HeaderForMeshProcessorWizard.png";
        headerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(headerPath);
    }

    private void OnGUI()
    {
        if (headerTexture != null)
        {
            GUILayout.Label(headerTexture, GUILayout.MaxHeight(100));
        }
        else
        {
            GUILayout.Label("Mesh Processor Wizard", EditorStyles.boldLabel);
        }

        intendedMaterial = (Material)EditorGUILayout.ObjectField("Intended Material", intendedMaterial, typeof(Material), false);

        EditorGUILayout.Space();

        //get folder selection... 
        useNewFolder = GUILayout.Toggle(useNewFolder, "New Folder");
        if (useNewFolder)
        {
            newFolderPath = EditorGUILayout.TextField("New Folder Path", newFolderPath);
            newFolderName = EditorGUILayout.TextField("New Folder Name", newFolderName);
        }
        else
        {
            existingFolderPath = EditorGUILayout.TextField("Existing Folder Path", existingFolderPath);
        }

        EditorGUILayout.Space();

        bool wasModifyOriginalMode = modifyOriginalMode;
        modifyOriginalMode = EditorGUILayout.Toggle("Modify Original Mode", modifyOriginalMode);

        //disable the other box so you don't fat finger it and lose a mesh. 
        if (modifyOriginalMode)
        {
            deleteOriginalObjects = false;
            GUI.enabled = false;
            EditorGUILayout.HelpBox("Modify Original will move your original mesh and delete dummy material.", MessageType.Warning);
        }
        deleteOriginalObjects = EditorGUILayout.Toggle("Delete Original Objects", deleteOriginalObjects);
        if (modifyOriginalMode)
        {
            GUI.enabled = true;
            
        }
        if(deleteOriginalObjects)
        {
            EditorGUILayout.HelpBox("Delete Original Objects will delete the original objects permanently and only copy them to folder.", MessageType.Warning); 
        }

        EditorGUILayout.Space();

        //doing a preview so you know you didn't fat finger something... 
        float maxMeshDisplayHeight = 3 * 100; 
        float meshDisplayHeight = selectedMeshes.Count * 100; 
        float displayHeight = Mathf.Min(meshDisplayHeight, maxMeshDisplayHeight);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(displayHeight));
        foreach (var mesh in selectedMeshes)
        {
            EditorGUILayout.BeginHorizontal();
            Texture2D preview = AssetPreview.GetAssetPreview(mesh);
            if (preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            }
            EditorGUILayout.ObjectField(mesh, typeof(Object), false);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // proc and org... 
        if (GUILayout.Button("Process and Organize Meshes"))
        {
            if (modifyOriginalMode)
            {
                ModifyAndMoveMeshes();
            }
            else
            {
                if (deleteOriginalObjects)
                {
                    if (EditorUtility.DisplayDialog(
                        "Confirm Deletion",
                        "You have chosen to delete the original objects, are you sure?",
                        "Yes",
                        "No"))
                    {
                        ProcessAndOrganizeMeshes();
                    }
                }
                else
                {
                    ProcessAndOrganizeMeshes();
                }
            }
        }
    }

    private void ProcessAndOrganizeMeshes()
{
    string folderPath = useNewFolder ? $"{newFolderPath}/{newFolderName}" : existingFolderPath;
    if (useNewFolder && !AssetDatabase.IsValidFolder(folderPath))
    {
        AssetDatabase.CreateFolder(newFolderPath, newFolderName);
    }

    foreach (Object obj in selectedMeshes)
    {
        Mesh originalMesh = null;
        GameObject meshGO = obj as GameObject;

            var meshFilters = meshGO.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                
             originalMesh = meshFilter.sharedMesh;
                   
             CreateCopyAndProcessMesh(originalMesh, folderPath, meshGO.name);

             if (deleteOriginalObjects)
             {
              //learned about using AssetDatabase, cute. 
              string assetPath = AssetDatabase.GetAssetPath(meshGO);
              if (!AssetDatabase.DeleteAsset(assetPath))
               {
                Debug.LogError($"Failed to delete GameObject at {assetPath}");
               }
             }
                    
                
            }
        else if (obj is Mesh)
        {
            originalMesh = obj as Mesh;
                CreateCopyAndProcessMesh(originalMesh, folderPath, originalMesh.name);
                if (deleteOriginalObjects)
                {
                    
                    string assetPath = AssetDatabase.GetAssetPath(originalMesh);
                    if (!AssetDatabase.DeleteAsset(assetPath))
                    {
                        Debug.LogError($"Failed to delete asset at {assetPath}");
                    }
                }
        }
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
}


    private void CreateCopyAndProcessMesh(Mesh originalMesh, string folderPath, string baseName)
    {
        //new mesh and appended name. 
        Mesh newMesh = new Mesh();
        newMesh.name = baseName + "_Copy";

        //copy all your details... I have no idea how many UVs you have so I'm just going to make this look ugly. 
        newMesh.vertices = originalMesh.vertices;
        newMesh.uv = originalMesh.uv;
        newMesh.uv2 = originalMesh.uv2;
        newMesh.uv3 = originalMesh.uv3;
        newMesh.uv4 = originalMesh.uv4;
        newMesh.uv5 = originalMesh.uv5;
        newMesh.uv6 = originalMesh.uv6;
        newMesh.uv7 = originalMesh.uv7; 
        newMesh.normals = originalMesh.normals;
        newMesh.tangents = originalMesh.tangents;
        newMesh.colors = originalMesh.colors;
        newMesh.triangles = originalMesh.triangles;

        //copy blendshapes 
        for (int i = 0; i < originalMesh.blendShapeCount; i++)
        {
            string shapeName = originalMesh.GetBlendShapeName(i);
            for (int j = 0; j < originalMesh.GetBlendShapeFrameCount(i); j++)
            {
                float frameWeight = originalMesh.GetBlendShapeFrameWeight(i, j);
                Vector3[] deltaVertices = new Vector3[originalMesh.vertexCount];
                Vector3[] deltaNormals = new Vector3[originalMesh.vertexCount];
                Vector3[] deltaTangents = new Vector3[originalMesh.vertexCount];
                originalMesh.GetBlendShapeFrameVertices(i, j, deltaVertices, deltaNormals, deltaTangents);
                newMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
            }
        }

        // recalc normals and tangents
        newMesh.RecalculateNormals();
        newMesh.RecalculateTangents();

        // Create a new asset for the copied mesh
        string newMeshPath = $"{folderPath}/{newMesh.name}.asset";
        AssetDatabase.CreateAsset(newMesh, newMeshPath);

        // Ensure asset database is refreshed to apply changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Create a prefab for the new mesh if it came from a GameObject
        GameObject newGO = new GameObject(baseName + "_Copy");
        var newMeshFilter = newGO.AddComponent<MeshFilter>();
        newMeshFilter.sharedMesh = newMesh;
        var renderer = newGO.AddComponent<MeshRenderer>();

        if (intendedMaterial != null)
        {
            renderer.sharedMaterial = intendedMaterial;
        }

        string newPrefabPath = $"{folderPath}/{newGO.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(newGO, newPrefabPath);
        DestroyImmediate(newGO);
    }

    private void ModifyAndMoveMeshes()
    {
        string folderPath = useNewFolder ? $"{newFolderPath}/{newFolderName}" : existingFolderPath;
        if (useNewFolder && !AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(newFolderPath, newFolderName);
        }

        foreach (Object obj in selectedMeshes)
        {
            GameObject meshGO = obj as GameObject;

            if (meshGO != null)
            {
                //remove the material for good measure, I'm keeping this here because I am not sure if material gets removed even 
                //on importer setting modification and I'll check later... 
                var meshRenderers = meshGO.GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in meshRenderers)
                {
                    if (meshRenderer != null)
                    {
                        meshRenderer.sharedMaterials = new Material[0];
                    }
                }

                //shunt that shit... 
                string assetPath = AssetDatabase.GetAssetPath(meshGO);
                string newAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{meshGO.name}.fbx");

                if (!AssetDatabase.MoveAsset(assetPath, newAssetPath).Contains("Error"))
                {
                    Debug.Log($"Moved asset from {assetPath} to {newAssetPath}");
                }
                else
                {
                    Debug.LogError($"Failed to move asset from {assetPath} to {newAssetPath}");
                }

                
                ModelImporter importer = AssetImporter.GetAtPath(newAssetPath) as ModelImporter;
                if (importer != null)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    AssetDatabase.ImportAsset(newAssetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }



}











