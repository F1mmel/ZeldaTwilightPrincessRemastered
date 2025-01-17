using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MeshBundleBuilder
{
    [MenuItem("Custom/Build Mesh Asset Bundle")]
    public static void BuildMeshAssetBundle()
    {
        string folderPath = "Assets/Meshes/";
        string bundlePath = "Assets/Meshes/mesh_bundle";

        // Holen Sie sich alle Mesh-Assets im angegebenen Ordner
        string[] assetPaths = AssetDatabase.FindAssets("t:Mesh", new[] { folderPath });

        foreach (string a in assetPaths)
        {
            Debug.LogWarning(a);
        }

        AssetBundleBuild[] builds = new AssetBundleBuild[1];
        builds[0].assetBundleName = "mesh_bundle"; // Gib dem Asset-Bundle einen eindeutigen Namen
        builds[0].assetNames = assetPaths;

        // Erstellen des Asset-Bundles
        BuildPipeline.BuildAssetBundles(folderPath, builds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        Debug.Log("Mesh Asset Bundle created at: " + bundlePath);
    }
}