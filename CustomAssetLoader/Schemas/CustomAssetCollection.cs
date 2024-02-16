using System.Collections.Generic;

namespace CustomAssetLoader.Schemas
{
    /// <summary>
    /// A collection of assets (for a whole mod)
    /// </summary>
    public class CustomAssetCollection
    {
        /// <summary>
        /// The name of the asset collection
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// List of assets included in the mod
        /// </summary>
        public List<CustomAssetSchema> Assets
        {
            get;
            set;
        }
    }
}
