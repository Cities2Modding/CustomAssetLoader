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
        /// The name to add to localization
        /// </summary>
        public string DisplayName
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
        } = CustomAssetType.Prop;
    }
}
