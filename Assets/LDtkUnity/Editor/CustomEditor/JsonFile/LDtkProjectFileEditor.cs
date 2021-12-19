﻿using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

namespace LDtkUnity.Editor
{
    [ExcludeFromDocs]
    [CustomEditor(typeof(LDtkProjectFile))]
    public class LDtkProjectFileEditor : LDtkJsonFileEditor<LdtkJson>
    {
        private void OnEnable()
        {
            TryCacheJson();
            Tree = new LDtkTreeViewWrapper(JsonData);
        }

        protected override void DrawInspectorGUI()
        {
            DrawVersion(JsonData);

            Level[] levels = JsonData.Levels;
            
            if (levels == null)
            {
                return;
            }
            
            DrawCountOfItems(levels.Length, "Level", "Levels");
            
            DrawDefinitions(JsonData.Defs);
            LDtkEditorGUIUtility.DrawDivider();
            Tree?.OnGUI();
        }
        
        private void DrawDefinitions(Definitions defs)
        {
            DrawCountOfItems(defs.Layers.Length, 
                "Layer", "Layers");
            DrawCountOfItems(defs.LevelFields.Length, 
                "Level Fields", "Level Fields");
            
            DrawCountOfItems(defs.Entities.Length, 
                "Entity", "Entities");
            DrawCountOfItems(defs.Entities.SelectMany(p => p.FieldDefs).Count(), 
                "Entity Field", "Entity Fields");
            
            DrawCountOfItems(defs.Enums.Length, 
                "Enum", "Enums");
            DrawCountOfItems(defs.Enums.SelectMany(p => p.Values).Count(), 
                "Enum Value", "Enum Values");
            
            DrawCountOfItems(defs.Tilesets.Length, 
                "Tileset", "Tilesets");
        }

        private static void DrawVersion(LdtkJson project)
        {
            string version = $"Json Version: {project.JsonVersion}";
            EditorGUILayout.LabelField(version);
        }
        
        private void DrawCountOfItems(int count, string single, string plural)
        {
            string naming = count == 1 ? single : plural;
            EditorGUILayout.LabelField($"{count} {naming}");
        }
    }
}