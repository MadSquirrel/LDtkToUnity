﻿using LDtkUnity.Data;
using UnityEditor;
using UnityEngine;

namespace LDtkUnity.Editor
{
    public class LDtkReferenceDrawerIntGridValue : LDtkAssetReferenceDrawer<LDtkDefinitionIntGridValue>
    {
        private readonly float _opacity;
        
        public LDtkReferenceDrawerIntGridValue(SerializedProperty asset, float opacity) : base(asset)
        {
            _opacity = opacity;
        }
        
        protected override void DrawInternal(Rect controlRect, LDtkDefinitionIntGridValue data)
        {
            controlRect.x += 15;
            Rect iconRect = GetLeftIconRect(controlRect);

            Color valueColor = data.Color();
            valueColor.a = _opacity;
            EditorGUI.DrawRect(iconRect, valueColor);
            
            DrawLabel(controlRect, data);
            
            controlRect.x -= 15;
            DrawFieldAndObject(controlRect, data);
        }
    }
}