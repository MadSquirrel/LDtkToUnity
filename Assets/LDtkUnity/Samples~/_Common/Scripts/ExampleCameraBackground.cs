﻿using UnityEngine;

namespace Samples
{
    public class ExampleCameraBackground : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        
        public void UpdateBackgroundColor(Color bgColor)
        {
            _camera.backgroundColor = bgColor;
        }
    }
}