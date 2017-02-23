using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NetworkableAssets {

    public static List<Object> FindNetworkableAssets(List<NetworkableSettings.PersistentTypeId> networkableTypes)
    {
        List<string> networkableAssets = new List<string>();

        if (networkableTypes.Count != 0)
        {
            string searchString = "";

            foreach (NetworkableSettings.PersistentTypeId persistentTypeId in networkableTypes)
                searchString += " t:" + persistentTypeId.TypeName;

            string[] assetGuids = AssetDatabase.FindAssets(searchString);

            List<Object> assets = new List<Object>();

            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                assets.Add(asset);
            }

            return assets;
        }
        else
            return new List<Object>();
    }
    
}
