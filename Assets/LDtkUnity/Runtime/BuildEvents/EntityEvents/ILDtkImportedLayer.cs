﻿namespace LDtkUnity
{
    /// <summary>
    /// Use this interface on a entity's prefab components to access the layer of this entity instance during the import process.
    /// </summary>
    public interface ILDtkImportedLayer
    {
        /// <summary>
        /// Triggers on an all entity components that implements this interface during the import process.
        /// </summary>
        /// <param name="layerInstance">
        /// The layer instance that this entity is in.
        /// </param>
        void OnLDtkImportLayer(LayerInstance layerInstance);
    }
}