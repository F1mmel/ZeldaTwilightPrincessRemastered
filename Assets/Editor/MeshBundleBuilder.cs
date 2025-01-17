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

        string[] assetPaths = AssetDatabase.FindAssets("t:Mesh", new[] { folderPath });

        foreach (string a in assetPaths)
        {
            Debug.LogWarning(a);
        }

        AssetBundleBuild[] builds = new AssetBundleBuild[1];
        builds[0].assetBundleName = "mesh_bundle";
        builds[0].assetNames = assetPaths;

        BuildPipeline.BuildAssetBundles(folderPath, builds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        Debug.Log("Mesh Asset Bundle created at: " + bundlePath);
    }
}