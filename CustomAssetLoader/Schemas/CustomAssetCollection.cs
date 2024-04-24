using CustomAssetLoader.Helpers;
using CustomAssetLoader.Systems;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

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

        public static bool HasAssets( string assetsSourcePath )
        {
            var path = Path.Combine( assetsSourcePath, "assets.json" );
            var zipPath = Path.Combine( assetsSourcePath, "assets.zip" );

            return File.Exists( path ) && File.Exists( zipPath );
        }

        public static CustomAssetCollection Load( string assetsSourcePath )
        {
            var path = Path.Combine( assetsSourcePath, "assets.json" );

            if ( !File.Exists( path ) )
                return null;

            return JsonConvert.DeserializeObject<CustomAssetCollection>( File.ReadAllText( path ) );
        }

        public void Save( string assetsSourcePath )
        {
            var path = Path.Combine( assetsSourcePath, "assets.json" );
            var json = JsonConvert.SerializeObject( this );

            Directory.CreateDirectory( assetsSourcePath );
            File.WriteAllText( path, json );
        }
    }
}
