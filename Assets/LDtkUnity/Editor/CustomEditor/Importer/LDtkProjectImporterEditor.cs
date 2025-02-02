﻿using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace LDtkUnity.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LDtkProjectImporter))]
    internal class LDtkProjectImporterEditor : LDtkImporterEditor
    {
        private LDtkJsonEditorCache _cache;

        private ILDtkSectionDrawer[] _sectionDrawers;
        private LDtkSectionMain _sectionMain;
        private LDtkSectionIntGrids _sectionIntGrids;
        private LDtkSectionEntities _sectionEntities;
        private LDtkSectionEnums _sectionEnums;
        private bool _shouldApply = true;
        
        
        private static readonly GUIContent ExportButtonContent = new GUIContent()
        {
            text = "Export",
            tooltip = "Export Native Prefab"
        };

        public override void OnEnable()
        {
            base.OnEnable();

            ConstructCache();
            LDtkUidBank.CacheUidData(_cache.Json);
            
            _sectionMain = new LDtkSectionMain(serializedObject);
            _sectionIntGrids = new LDtkSectionIntGrids(serializedObject);
            _sectionEntities = new LDtkSectionEntities(serializedObject);
            _sectionEnums = new LDtkSectionEnums(serializedObject);

            _sectionDrawers = new[]
            {
                (ILDtkSectionDrawer)_sectionMain,
                _sectionIntGrids,
                _sectionEntities,
                _sectionEnums,
            };

            foreach (ILDtkSectionDrawer drawer in _sectionDrawers)
            {
                drawer.Init();
            }
            LDtkUidBank.ReleaseDefinitions();
        }

        public override void OnDisable()
        {

            
            if (_sectionDrawers != null)
            {
                foreach (ILDtkSectionDrawer drawer in _sectionDrawers)
                {
                    drawer?.Dispose();
                }
            }

            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            try
            {
                serializedObject.Update();
                
                TryReconstructCache();
                
                Profiler.BeginSample("CacheUidData");
                LDtkUidBank.CacheUidData(_cache.Json);
                Profiler.EndSample();

                Profiler.BeginSample("ShowGUI");
                ShowGUI();
                Profiler.EndSample();
                
                serializedObject.ApplyModifiedProperties();
                
                if (_shouldApply)
                {
                    ApplyIfArraySizesChanged();
                    _shouldApply = false;
                }
                DrawPotentialProblem();
            }
            finally
            {
                ApplyRevertGUI();
                LDtkUidBank.ReleaseDefinitions();
            }
        }

        private void TryReconstructCache()
        {
            if (_cache == null)
            {
                Debug.LogError("LDtk: bug, cache is null, but its expected to never be null");
                return;
            }
            
            if (_cache.ShouldForceReconstruct())
            {
                ConstructCache();
                _shouldApply = true;
            }
        }
        
        private void ConstructCache()
        {
            _cache = new LDtkJsonEditorCache((LDtkProjectImporter)target);
        }

        private void ShowGUI()
        {
            Profiler.BeginSample("JsonSetup");
            EditorGUIUtility.SetIconSize(Vector2.one * 16);

            LdtkJson data = GetJson();
            if (data == null)
            {
                DrawBreakingError();
                return;
            }
            
            DrawExportButton();
            _sectionMain.SetJson(data);

            Definitions defs = data.Defs;
            Profiler.EndSample();

            Profiler.BeginSample("MainSection");
            _sectionMain.Draw();
            Profiler.EndSample();
            
            Profiler.BeginSample("IntGridSection");
            _sectionIntGrids.Draw(defs.IntGridLayers);
            Profiler.EndSample();
            
            Profiler.BeginSample("EntitiesSection");
            _sectionEntities.Draw(defs.Entities);
            Profiler.EndSample();
            
            Profiler.BeginSample("EnumsSection");
            _sectionEnums.Draw(defs.Enums);
            Profiler.EndSample();
            
            LDtkEditorGUIUtility.DrawDivider();
        }

        private void DrawExportButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool button = GUILayout.Button(ExportButtonContent, GUILayout.Width(45));
            GUILayout.EndHorizontal();

            if (button)
            {
                GameObject gameObject = (GameObject)assetTarget;
                LDtkNativeExportWindow.CreateWindowWithContext(gameObject);
            }
            
            LDtkEditorGUIUtility.DrawDivider();
        }

        private LdtkJson GetJson()
        {
            if (_cache == null)
            {
                Debug.LogError("LDtk: Cache was null");
                return null;
            }
            
            LdtkJson cachedJson = _cache.Json;
            if (cachedJson != null)
            {
                return cachedJson;
            }

            return null;
        }
        
        private void ApplyIfArraySizesChanged()
        {
            //IMPORTANT: if there are any new/removed array elements via this setup of automatically resizing arrays as LDtk definitions change,
            //then Unity's going to notice and make the apply/revert buttons appear active which normally gives us trouble when we try clicking out.
            //So, try applying right now when this specific case happens; whenever there is an array resize.
            
            if (_sectionDrawers.Any(drawer => drawer.HasResizedArrayPropThisUpdate))
            {
                Apply();
                //Debug.Log("Applied an array resize and reimported as a result");
            }
        }
        
        private void DrawPotentialProblem()
        {
            bool problem = _sectionDrawers.Any(drawer => drawer.HasProblem);

            if (problem)
            {
                EditorGUIUtility.SetIconSize(Vector2.one * 32);
                EditorGUILayout.HelpBox(
                    "LDtk Project asset configuration has unresolved issues, mouse over them to see the problem",
                    MessageType.Warning);
            }
        }
    }
}