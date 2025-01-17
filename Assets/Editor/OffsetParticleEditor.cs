using UnityEditor;
using UnityEngine;

public class OffsetParticleEditor : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty baseMap = FindProperty("_BaseMap", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        MaterialProperty tiling = FindProperty("_MainTex_ST", properties);

        materialEditor.ShaderProperty(baseMap, "Base Map");
        materialEditor.ShaderProperty(baseColor, "Base Color");

        EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(tiling, "Texture Tiling");
    }
}