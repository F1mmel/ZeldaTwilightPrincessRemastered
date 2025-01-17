using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#endif

public class FlagEvent : MonoBehaviour
{
    [FlagSelector]
    public string selectedFlag;
    
    void Start()
    {
        SaveManager.HandleChilds(selectedFlag, transform);
    }

    void Update()
    {
    }
}

public class FlagSelectorAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(FlagSelectorAttribute))]
public class FlagSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            SaveManager saveManager = GameObject.FindObjectOfType<SaveManager>();
            
            if (saveManager != null)
            {
                FieldInfo stageFlagsField = typeof(SaveManager).GetField("StageFlags", BindingFlags.Instance | BindingFlags.Public);
                
                if (stageFlagsField != null)
                {
                    var stageFlags = stageFlagsField.GetValue(saveManager) as Dictionary<string, List<string>>;
                    
                    if (stageFlags != null)
                    {
                        List<string> keys = new List<string>(stageFlags.Keys);

                        GameObject stageObject = GameObject.FindWithTag("StageObjects");

                        string stageName = "";
                        foreach(Transform child in stageObject.transform)
                        {                            
                            if(!child.gameObject.activeSelf) continue;
                            if(child.name.Contains("___")) continue;
                            
                            if (keys.Contains(child.name.Split("_")[2]))
                            {
                                stageName = child.name.Split("_")[2];
                            }
                        }

                        int selectedIndex = 0;
                        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, stageFlags[stageName].ToArray());
                        property.stringValue = stageFlags[stageName][selectedIndex];

                    }
                    else
                    {
                        EditorGUI.LabelField(position, label.text, "StageFlags dictionary is null.");
                    }
                }
                else
                {
                    EditorGUI.LabelField(position, label.text, "StageFlags field not found.");
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "SaveManager not found in scene.");
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [FlagSelector] with a string.");
        }
    }
}
#endif
