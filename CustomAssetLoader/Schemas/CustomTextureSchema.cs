namespace CustomAssetLoader.Schemas
{
    /// <summary>
    /// Stores the texture files for a custom asset
    /// </summary>
    public class CustomTextureSchema
    {
        /// <summary>
        /// The base color/diffuse map file
        /// </summary>
        public string BaseColor
        {
            get;
            set;
        }

        /// <summary>
        /// The normal map file
        /// </summary>
        public string Normal
        {
            get;
            set;
        }

        /// <summary>
        /// The emissive file
        /// </summary>
        public string Emissive
        {
            get;
            set;
        }
    }
}
