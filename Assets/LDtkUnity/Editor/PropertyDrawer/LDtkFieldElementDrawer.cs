﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace LDtkUnity.Editor
{
    [CustomPropertyDrawer(typeof(LDtkFieldElement))]
    internal class LDtkFieldElementDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Sprite, Texture2D> Icons = new Dictionary<Sprite, Texture2D>();
        private static Texture2D _blankSquareTex; 

        private SerializedProperty _canBeNullProp;
        private SerializedProperty _isNotNullProp;
        private SerializedProperty _valueProp;
        private Rect _position;
        private Rect _labelRect;
        private Rect _fieldRect;

        private static readonly GUIContent NullToggle = new GUIContent()
        {
            tooltip = "Is null"
        };
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            LDtkFieldType type = GetFieldType(property);
            _valueProp = GetPropertyToDraw(property, type);
            if (_valueProp == null)
            {
                Debug.LogError($"LDtk: Error drawing in the scene for field: {label.text}, serialized property was null");
                return 0;
            }
            
            float propertyHeight = EditorGUI.GetPropertyHeight(_valueProp, label);

            _isNotNullProp = property.FindPropertyRelative(LDtkFieldElement.PROPERTY_NULL);
            if (type == LDtkFieldType.Multiline && (!_canBeNullProp.boolValue || _isNotNullProp.boolValue))
            {
                //if we cannot be null, always have be tall even if we are considered null in the serialized value.
                //only be tall if we are not null
                propertyHeight *= 3;
            }

            return propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Profiler.BeginSample("LDtkFieldElementDrawer.OnGUI");
            using (new EditorGUIUtility.IconSizeScope(Vector2.one * 14))
            {
                Draw(position, property, label);
            }
            Profiler.EndSample();
        }

        private void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            Profiler.BeginSample("LDtkFieldElementDrawer.Draw");
            
            TryInitTex();

            label.image = _blankSquareTex;

            LDtkFieldType type = GetFieldType(property);
            _isNotNullProp = property.FindPropertyRelative(LDtkFieldElement.PROPERTY_NULL);
            _canBeNullProp = property.FindPropertyRelative(LDtkFieldElement.PROPERTY_CAN_NULL);
            
            _position = position;
            _labelRect = GetLabelRect(position);
            _fieldRect = GetFieldRect(position, _labelRect);
            //_labelRect.xMin += EditorGUIUtility.singleLineHeight;


            _valueProp = GetPropertyToDraw(property, type);
            Profiler.EndSample();
            
            if (_valueProp == null)
            {
                Debug.LogError($"LDtk: Error drawing in the scene for field: {label.text}, serialized property was null");
                return;
            }

            
            if (TryDrawNullable(label, type))
            {
                return;
            }

            if (TryDrawAlternateType(type, label))
            {
                return;
            }

            Profiler.BeginSample("LDtkFieldElementDrawer.PropertyField");
            EditorGUI.PropertyField(_position, _valueProp, label);
            Profiler.EndSample();
        }
        
        private static void TryInitTex()
        {
            if (_blankSquareTex != null)
            {
                return;
            }
            
            _blankSquareTex = new Texture2D(1, 1);
            _blankSquareTex.SetPixel(0, 0, Color.clear);
            _blankSquareTex.Apply();
        }

        private bool TryDrawNullable(GUIContent label, LDtkFieldType type)
        {
            Profiler.BeginSample("LDtkFieldElementDrawer.TryDrawNullable");
            if (!CanBeNull(type))
            {
                
                Profiler.EndSample();
                return false;
            }
            
            Rect boolRect = _position;
            //boolRect.xMin += labelRect.width - position.height;
            boolRect.width = EditorGUIUtility.singleLineHeight;
            boolRect.height = EditorGUIUtility.singleLineHeight;
            
            if (!IsNull(type))
            {
                EditorGUI.PropertyField(boolRect, _isNotNullProp, NullToggle);
                
                Profiler.EndSample();
                return false;
            }

            GUIContent nullContent = new GUIContent($"(Null {type})");

            EditorGUI.PropertyField(boolRect, _isNotNullProp, NullToggle);
            EditorGUI.LabelField(_labelRect, label);
            EditorGUI.LabelField(_fieldRect, nullContent);

            Profiler.EndSample();
            return true;
        }

        private bool TryDrawAlternateType(LDtkFieldType type, GUIContent label)
        {
            switch (type)
            {
                case LDtkFieldType.Multiline:
                {
                    return DrawMultiline(label);
                }
                
                case LDtkFieldType.EntityRef:
                {
                    return DrawEntityRef(label);
                }
                
                case LDtkFieldType.Tile:
                {
                    DrawTileField(label);
                    return false;
                }
                
                default:
                    return false;
            }
        }

        private bool DrawMultiline(GUIContent label)
        {
            Profiler.BeginSample("LDtkFieldElementDrawer.DrawMultiline");
            
            GUIStyle labelStyle = _valueProp.prefabOverride ? EditorStyles.boldLabel : EditorStyles.label;
            
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.fontStyle = _valueProp.prefabOverride ? FontStyle.Bold : FontStyle.Normal;
            
            _labelRect.height *= 0.33f;
            
            EditorGUI.LabelField(_labelRect, label, labelStyle);

            _valueProp.stringValue = EditorGUI.TextArea(_fieldRect, _valueProp.stringValue, textAreaStyle);
            
            Profiler.EndSample();
            return true;
        }

        private bool DrawEntityRef(GUIContent label)
        {
            Profiler.BeginSample("LDtkFieldElementDrawer.DrawEntityRef");
            
            string iid = _valueProp.stringValue;

            if (string.IsNullOrEmpty(iid))
            {
                Profiler.EndSample();
                return false;
            }

            LDtkIid component = LDtkIidComponentBank.FindObjectOfIid(iid);
            if (component == null)
            {
                Profiler.EndSample();
                return false;
            }

            float desiredObjectWidth = 175;

            float objectWidth = Mathf.Min(desiredObjectWidth, _position.width - desiredObjectWidth * 0.83f);
            float fieldWidth = Mathf.Max(_position.width - objectWidth);

            Rect fieldRect = new Rect(_position.x, _position.y, fieldWidth - 2, _position.height);
            Rect gameObjectRect = new Rect(_position.x + fieldWidth, _position.y, Mathf.Max(desiredObjectWidth, objectWidth), _position.height);

            fieldRect.xMin = _labelRect.xMin;

            _labelRect = fieldRect;
            _fieldRect.width -= gameObjectRect.width;
            //DrawField(label);

            EditorGUI.PropertyField(fieldRect, _valueProp, label);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(gameObjectRect, component.gameObject, typeof(GameObject), true); //todo figure out this object field's width
            }

            Profiler.EndSample();
            return true;
        }

        private void DrawTileField(GUIContent label)
        {
            if (_canBeNullProp.boolValue)
            {
                //don't draw if we already have the toggle overlaying
                return;
            }
            
            Sprite spr = (Sprite)_valueProp.objectReferenceValue;
            if (spr == null)
            {
                return;
            }

            Texture2D tex = GetTileTexture(spr);
            if (tex == null)
            {
                return;
            }

            Rect imgRect = new Rect(_position);
            imgRect.width = 16;
            imgRect.height = 16;
            imgRect.x -= 2;
            imgRect.y += 1;

            GUI.DrawTexture(imgRect, tex);
        }

        private static Texture2D GetTileTexture(Sprite spr)
        {
            return AssetPreview.GetAssetPreview(spr);
            /*Texture2D tex = null;
            if (Icons.ContainsKey(spr))
            {
                tex = Icons[spr];
                if (tex == null)
                {
                    return tex;
                }
            }
            else
            {
                tex = AssetPreview.GetAssetPreview(spr);
                Icons.Add(spr, tex);
            }

            if (tex == null)
            {
                Debug.LogError("Texture was null, never expected");
                return tex;
            }*/

            //return tex;
        }

        private static Rect GetFieldRect(Rect position, Rect labelRect)
        {
            Rect fieldRect = new Rect(position);
            fieldRect.x = labelRect.xMax;
            fieldRect.width = Mathf.Max(EditorGUIUtility.fieldWidth, position.width - labelRect.width);
            return fieldRect;
        }

        private static Rect GetLabelRect(Rect position)
        {
            Rect labelRect = new Rect(position);
            labelRect.width = EditorGUIUtility.labelWidth + 2;
            return labelRect;
        }

        private SerializedProperty GetPropertyToDraw(SerializedProperty property, LDtkFieldType type)
        {
            string propName = GetPropertyNameForType(type);
            return property.FindPropertyRelative(propName);
        }

        private LDtkFieldType GetFieldType(SerializedProperty property)
        {
            SerializedProperty typeProp = property.FindPropertyRelative(LDtkFieldElement.PROPERTY_TYPE);
            Array values = Enum.GetValues(typeof(LDtkFieldType));
            return (LDtkFieldType)values.GetValue(typeProp.enumValueIndex);
        }

        private string GetPropertyNameForType(LDtkFieldType type)
        {
            switch (type)
            {
                case LDtkFieldType.Int:
                    return LDtkFieldElement.PROPERTY_INT;
                
                case LDtkFieldType.Float:
                    return LDtkFieldElement.PROPERTY_FLOAT;
                
                case LDtkFieldType.Bool:
                    return LDtkFieldElement.PROPERTY_BOOL;
                
                case LDtkFieldType.String:
                case LDtkFieldType.Multiline:
                case LDtkFieldType.FilePath:
                case LDtkFieldType.Enum:
                case LDtkFieldType.EntityRef:
                    return LDtkFieldElement.PROPERTY_STRING;
                
                case LDtkFieldType.Color:
                    return LDtkFieldElement.PROPERTY_COLOR;
                
                case LDtkFieldType.Point:
                    return LDtkFieldElement.PROPERTY_VECTOR2;

                case LDtkFieldType.Tile:
                    return LDtkFieldElement.PROPERTY_SPRITE;
            }

            return null;
        }

        private bool CanBeNull(LDtkFieldType type)
        {
            if (!_canBeNullProp.boolValue)
            {
                return false;
            }
            
            switch (type)
            {
                case LDtkFieldType.None:
                case LDtkFieldType.Int:
                case LDtkFieldType.Float:
                case LDtkFieldType.String:
                case LDtkFieldType.Multiline:
                case LDtkFieldType.FilePath:
                case LDtkFieldType.Enum:
                case LDtkFieldType.Point:
                case LDtkFieldType.EntityRef:
                case LDtkFieldType.Tile: //for any unity engine object references, it will check if the value is actually null instead 
                    return true;

                case LDtkFieldType.Bool:
                case LDtkFieldType.Color:
                default:
                    return false;
            }
        }
        public bool IsNull(LDtkFieldType type)
        {
            switch (type)
            {
                case LDtkFieldType.None:
                    return true;
                    
                case LDtkFieldType.Bool: //these are values that are never able to be null from ldtk
                case LDtkFieldType.Color:
                    return false;

                //for our use cases, it's null when the value was set as null from the parsed data types.
                case LDtkFieldType.Int:
                case LDtkFieldType.Float:
                case LDtkFieldType.String:
                case LDtkFieldType.Multiline:
                case LDtkFieldType.FilePath:
                case LDtkFieldType.Enum:
                case LDtkFieldType.Point:
                case LDtkFieldType.EntityRef:
                case LDtkFieldType.Tile:
                    return !_isNotNullProp.boolValue;

                default:
                    return false;
            }
        }
    }
}