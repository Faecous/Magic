using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A custom editor for the SpellData ScriptableObject.
/// It provides a dropdown list of available incantations from the VoiceSystemWrapper.
/// </summary>
[CustomEditor(typeof(SpellData))]
public class SpellDataEditor : Editor
{
    private List<string> availableIncantations;
    private int selectedIncantationIndex = -1;

    private void OnEnable()
    {
        // Find the first (and hopefully only) IncantationList asset in the project.
        var incantationListAssetGUIDs = AssetDatabase.FindAssets("t:IncantationList");
        if (incantationListAssetGUIDs.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(incantationListAssetGUIDs[0]);
            var incantationList = AssetDatabase.LoadAssetAtPath<IncantationList>(path);
            if(incantationList != null)
            {
                 availableIncantations = incantationList.incantations;
            }
        }

        if (availableIncantations == null)
        {
            availableIncantations = new List<string> { "Could not find IncantationList asset. Create one via Assets > Create > Spellcaster > Incantation List." };
        }

        // Find the index of the currently selected incantation
        SpellData spellData = (SpellData)target;
        if (!string.IsNullOrEmpty(spellData.incantation))
        {
            selectedIncantationIndex = availableIncantations.IndexOf(spellData.incantation);
        }
    }

    public override void OnInspectorGUI()
    {
        SpellData spellData = (SpellData)target;

        // --- Incantation Dropdown ---
        EditorGUILayout.LabelField("Incantation", EditorStyles.boldLabel);

        if (availableIncantations != null && availableIncantations.Count > 0 && availableIncantations[0].StartsWith("Could not find"))
        {
            EditorGUILayout.HelpBox(availableIncantations[0], MessageType.Warning);
        }
        
        int newIndex = EditorGUILayout.Popup("Select Incantation", selectedIncantationIndex, availableIncantations.ToArray());

        if (newIndex != selectedIncantationIndex)
        {
            selectedIncantationIndex = newIndex;
            if (selectedIncantationIndex >= 0 && selectedIncantationIndex < availableIncantations.Count)
            {
                spellData.incantation = availableIncantations[selectedIncantationIndex];
                EditorUtility.SetDirty(spellData);
            }
        }
        
        // Let the default inspector draw the rest of the properties
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
} 