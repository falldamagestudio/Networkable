﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

/// <summary>
/// Inspector-view for NetworkableSettings assets. Use this to view and manipulate any data that is persisted in NetworkableSettings.
/// </summary>
[CustomEditor(typeof(NetworkableSettings))]
public class NetworkableSettingsInspector : Editor {

    SerializedObject serializedObj;
    NetworkableSettings networkableSettings;

    private void OnEnable()
    {
        networkableSettings = (NetworkableSettings)target;
        serializedObj = new SerializedObject(networkableSettings);
    }

    public override void OnInspectorGUI()
    {

        GUIStyle warningStyle = new GUIStyle(GUI.skin.button);
        warningStyle.normal.textColor = Color.red;

        /////////////// TypeIds //////////////////

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Update TypeIds"))
        {
            Undo.RecordObject(networkableSettings, "Update NetworkableSettings PersistentTypeIds");
            if (!networkableSettings.AddNewPersistentTypeIds(NetworkableInitializer.FindNetworkableTypesInAssembly()))
            {
                EditorUtility.DisplayDialog("Not enough type IDs available", "There are not enough IDs available for adding the new types to the PersistentTypeIds list.\n\nPerhaps it is time for you to break backwards compatibility by removing unused TypeIds, and then try again?", "Ok");
            }
            EditorUtility.SetDirty(networkableSettings);
            Repaint();
        }
        if (GUILayout.Button("Remove all TypeIds", warningStyle))
        {
            if (EditorUtility.DisplayDialog("Remove all persistent TypeIds", "Removing old Type IDs will break compatibility with older builds. Are you sure you want to do this?", "Yes", "No"))
            {
                Undo.RecordObject(networkableSettings, "Remove all NetworkableSettings PersistentTypeIds");
                networkableSettings.RemoveAllPersistentTypeIds();
                EditorUtility.SetDirty(networkableSettings);
                Repaint();
            }
        }
        if (GUILayout.Button("Remove unused TypeIds", warningStyle))
        {
            if (EditorUtility.DisplayDialog("Remove all persistent TypeIds", "Removing old Type IDs will break compatibility with older builds. Are you sure you want to do this?", "Yes", "No"))
            {
                Undo.RecordObject(networkableSettings, "Remove unused NetworkableSettings PersistentTypeIds");
                networkableSettings.RemoveUnusedPersistentTypeIds(NetworkableInitializer.FindNetworkableTypesInAssembly());
                EditorUtility.SetDirty(networkableSettings);
                Repaint();
            }
        }
        serializedObj.Update();
        SerializedProperty typeIds = serializedObj.FindProperty("PersistentTypeIds");
        Assert.IsNotNull(typeIds);
        EditorGUILayout.PropertyField(typeIds, true);
        serializedObj.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();

        /////////////// AssetIds //////////////////

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Update AssetIds"))
        {
            Undo.RecordObject(networkableSettings, "Update NetworkableSettings PersistentAssetIds");
            if (!networkableSettings.AddNewPersistentTypeIds(NetworkableInitializer.FindNetworkableTypesInAssembly()))
                EditorUtility.DisplayDialog("Not enough type IDs available", "There are not enough IDs available for adding the new types to the PersistentTypeIds list.\n\nPerhaps it is time for you to break backwards compatibility by removing unused TypeIds, and then try again?", "Ok");
            else
            {
                if (!networkableSettings.AddNewPersistentAssetIds(NetworkableAssets.FindNetworkableAssets(networkableSettings.PersistentTypeIds)))
                    EditorUtility.DisplayDialog("Not enough asset IDs available", "There are not enough IDs available for adding the new assets to the PersistentAssetIds list.\n\nPerhaps it is time for you to break backwards compatibility by removing unused Asset IDs, and then try again?", "Ok");
            }
            EditorUtility.SetDirty(networkableSettings);
            Repaint();
        }
        if (GUILayout.Button("Remove all AssetIds", warningStyle))
        {
            if (EditorUtility.DisplayDialog("Remove all persistent AssetIds", "Removing old Asset IDs will break compatibility with older builds. Are you sure you want to do this?", "Yes", "No"))
            {
                Undo.RecordObject(networkableSettings, "Remove all NetworkableSettings PersistentAssetIds");
                networkableSettings.RemoveAllPersistentAssetIds();
                EditorUtility.SetDirty(networkableSettings);
                Repaint();
            }
        }
        if (GUILayout.Button("Remove unused AssetIds", warningStyle))
        {
            if (EditorUtility.DisplayDialog("Remove all persistent AssetIds", "Removing old Asset IDs will break compatibility with older builds. Are you sure you want to do this?", "Yes", "No"))
            {
                Undo.RecordObject(networkableSettings, "Remove unused NetworkableSettings PersistentAssetIds");
                if (!networkableSettings.AddNewPersistentTypeIds(NetworkableInitializer.FindNetworkableTypesInAssembly()))
                    EditorUtility.DisplayDialog("Not enough type IDs available", "There are not enough IDs available for adding the new types to the PersistentTypeIds list.\n\nPerhaps it is time for you to break backwards compatibility by removing unused TypeIds, and then try again?", "Ok");
                else
                    networkableSettings.RemoveUnusedPersistentAssetIds(NetworkableAssets.FindNetworkableAssets(networkableSettings.PersistentTypeIds));
                EditorUtility.SetDirty(networkableSettings);
                Repaint();
            }
        }

        serializedObj.Update();
        SerializedProperty assetIds = serializedObj.FindProperty("PersistentAssetIds");
        Assert.IsNotNull(assetIds);
        EditorGUILayout.PropertyField(assetIds, true);
        serializedObj.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }

}
