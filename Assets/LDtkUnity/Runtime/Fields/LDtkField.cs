﻿using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LDtkUnity
{
    /// <summary>
    /// A class that contains a single piece of data for a field, whether single or array.
    /// </summary>
    [Serializable] 
    internal class LDtkField
    {
        public const string PROPERTY_IDENTIFIER = nameof(_identifier);
        public const string PROPERTY_DATA = nameof(_data);
        public const string PROPERTY_SINGLE = nameof(_isSingle);

        [SerializeField] private string _identifier;
        [SerializeField] private bool _isSingle;
        [SerializeField] private LDtkFieldType _type;
        [SerializeField] private LDtkFieldElement[] _data;

        public string Identifier => _identifier;
        public bool IsArray => !_isSingle;
        public LDtkFieldType Type => _type;

        public LDtkField(string identifier, LDtkFieldElement[] instances, bool isArray)
        {
            _identifier = identifier;
            _data = instances;
            _isSingle = !isArray;
            _type = _data != null && _data.Length > 0 ? _data.First().Type : LDtkFieldType.None;
        }
        
        public bool GetFieldElementByType(LDtkFieldType type, out LDtkFieldElement element)
        {
            element = _data.FirstOrDefault(e => e.Type == type);
            return element != null;
        }

        public FieldsResult<LDtkFieldElement> GetSingle()
        {
            FieldsResult<LDtkFieldElement> result = FieldsResult<LDtkFieldElement>.Null();
            
            if (!ValidateSingle())
            {
                return result;
            }
            
            if (_data.IsNullOrEmpty())
            {
                Debug.LogError("LDtk: Error getting single");
                return result;
            }

            if (_data.Length != 1)
            {
                Debug.LogError("LDtk: Unexpected length when getting single");
                return result;
            }

            result.Success = true;
            result.Value = _data[0];
            return result;
        }
        
        public FieldsResult<LDtkFieldElement[]> GetArray()
        {
            FieldsResult<LDtkFieldElement[]> result = FieldsResult<LDtkFieldElement[]>.Null();
            
            if (!ValidateArray())
            {
                return result;
            }
            
            if (_data == null)
            {
                Debug.LogError("LDtk: Error getting array");
                return result;
            }

            result.Success = true;
            result.Value = _data;
            return result;
        }
        
        public FieldsResult<string> GetValueAsString()
        {
            FieldsResult<string> result = FieldsResult<string>.Null();
            if (!ValidateSingle())
            {
                return result;
            }
            
            FieldsResult<LDtkFieldElement> elementResult = GetSingle();
            if (!elementResult.Success)
            {
                return result;
            }

            LDtkFieldElement element = elementResult.Value;
            result.Success = true;
            result.Value = element.GetValueAsString();
            return result;
        }
        public FieldsResult<string[]> GetValuesAsStrings()
        {
            FieldsResult<string[]> result = FieldsResult<string[]>.Null();
            
            if (!ValidateArray())
            {
                return result;
            }

            FieldsResult<LDtkFieldElement[]> resultElements = GetArray();
            if (resultElements.Success)
            {
                LDtkFieldElement[] elements = resultElements.Value; 
                result.Value = elements.Select(p => p.GetValueAsString()).ToArray();
                result.Success = true;
            }

            return result;
        }

        public bool IsSingleNull()
        {
            if (!ValidateSingle())
            {
                return true;
            }

            FieldsResult<LDtkFieldElement> result = GetSingle();
            if (!result.Success)
            {
                return true;
            }

            LDtkFieldElement element = result.Value;
            return element.IsNull();
        }
        
        public bool IsArrayElementNull(int index)
        {
            if (!ValidateArray())
            {
                return true;
            }

            FieldsResult<LDtkFieldElement[]> result = GetArray();
            if (!result.Success)
            {
                return true;
            }
            
            LDtkFieldElement[] elements = result.Value; 
            if (elements.IsNullOrEmpty())
            {
                return true;
            }
            
            bool outOfBounds = index < 0 || index >= elements.Length;
            if (outOfBounds)
            {
                Debug.LogError($"LDtk: Out of range when checking if an array's element index {index} was null for \"{_identifier}\"");
                return true;
            }

            LDtkFieldElement element = elements[index];
            return element == null || element.IsNull();
        }
        
        private bool ValidateSingle()
        {
            if (_isSingle)
            {
                return true;
            }
            
            Debug.LogError($"LDtk: Tried accessing a single value when \"{_identifier}\" is an array");
            return false;

        }
        private bool ValidateArray()
        {
            if (!IsArray)
            {
                Debug.LogError($"LDtk: Tried accessing an array when \"{_identifier}\" is a single value");
                return false;
            }

            return true;
        }

        public bool ValidateElementTypes(LDtkFieldType type, Object ctx)
        {
            if (_type == LDtkFieldType.None)
            {
                return false;
            }
            
            if (type != _type)
            {
                Debug.LogError($"LDtk: Tried getting a field \"{_identifier}\" as type \"{type}\" but the field was a \"{_type}\" type instead", ctx);
                return false;
            }
            
            for (int i = 0; i < _data.Length; i++)
            {
                LDtkFieldElement element = _data[i];
                if (element == null)
                {
                    Debug.LogError("An array element in LDtkField was null", ctx);
                    continue;
                }
                
                if (!element.IsOfType(_type))
                {
                    return false;
                }
            }
            return true;
        }
    }
}