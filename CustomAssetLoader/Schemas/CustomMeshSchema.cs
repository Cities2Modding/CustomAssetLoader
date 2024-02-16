using System.Collections.Generic;

namespace CustomAssetLoader.Schemas
{
    /// <summary>
    /// Represents a custom asset's mesh file and it's LOD levels
    /// </summary>
    public class CustomMeshSchema
    {
        /// <summary>
        /// List of meshes, representing LOD levels potentially?
        /// </summary>
        public List<string> Meshes
        {
            get;
            set;
        }
    }
}
