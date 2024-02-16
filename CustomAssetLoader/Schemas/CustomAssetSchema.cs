namespace CustomAssetLoader.Schemas
{
    /// <summary>
    /// Schema for an individual asset
    /// </summary>
    public class CustomAssetSchema
    {
        /// <summary>
        /// The asset name, used to infer folder etc to load from.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The type of asset
        /// </summary>
        public CustomAssetType Type
        {
            get;
            set;
        }

        /// <summary>
        /// The texture configuration for the asset
        /// </summary>
        public CustomTextureSchema Texture
        {
            get;
            set;
        }

        /// <summary>
        /// The mesh configuration for the asset
        /// </summary>
        public CustomMeshSchema Mesh
        {
            get;
            set;
        }
    }
}
