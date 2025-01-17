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
        /*if (SaveManager.IsValid(selectedFlag))
        {
            SaveManager.SetFlag(selectedFlag);
        }*/
    }

    void Update()
    {
    }
}

public class FlagSelectorAttribute : PropertyAttribute
{
    // Leere Klasse, dient nur als Markierung
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(FlagSelectorAttribute))]
public class FlagSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            // Hole eine Referenz zum SaveManager-Script im Editor
            SaveManager saveManager = GameObject.FindObjectOfType<SaveManager>();
            
            if (saveManager != null)
            {
                // Verwende Reflection, um das StageFlags-Dictionary zu erhalten
                FieldInfo stageFlagsField = typeof(SaveManager).GetField("StageFlags", BindingFlags.Instance | BindingFlags.Public);
                
                if (stageFlagsField != null)
                {
                    // Cast des Wertes zu einem Dictionary
                    var stageFlags = stageFlagsField.GetValue(saveManager) as Dictionary<string, List<string>>;
                    
                    if (stageFlags != null)
                    {
                        // Liste der Keys im Dictionary
                        List<string> keys = new List<string>(stageFlags.Keys);

                        // Finde das GameObject mit dem Tag "StageObjects"
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

                        // Finde den aktuellen Index des ausgewählten Keys im Dropdown
                        /*int selectedIndex = keys.IndexOf(property.stringValue);
                        if (selectedIndex == -1) selectedIndex = 0;

                        // Dropdown im Inspector zeichnen
                        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, keys.ToArray());
                        property.stringValue = keys[selectedIndex];*/
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
