﻿namespace LDtkUnity
{
    /// <summary>
    /// Use this interface on entity/level components to access the field instances of the entity/level.
    /// </summary>
    public interface ILDtkImportedFields
    {
        /// <summary>
        /// Triggers on an all entity/level prefab components that implements this interface during the import process.
        /// </summary>
        /// <param name="fields">
        /// The entity instance.
        /// </param>
        void OnLDtkImportFields(LDtkFields fields);
    }
}