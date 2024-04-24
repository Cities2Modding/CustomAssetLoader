using Colossal.AssetPipeline.Importers;
using Colossal.Serialization.Entities;
using CustomAssetLoader.Helpers;
using CustomAssetLoader.Schemas;
using Game;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomAssetLoader.Systems
{
    public class CustomAssetSystem : MonoBehaviour
    {
        public static readonly string MOD_PATH = Path.Combine( Application.persistentDataPath, "Mods", "CustomAssetLoader" );
        public static readonly string SOURCE_PATH = Path.Combine( MOD_PATH, "Source" );

        static bool hasPreparedPrefabs = false;
        static bool canBuild = false;
        static bool hasImported = false;

        public static ConcurrentQueue<bool> _collectionBufferQueue = [];
        private ConcurrentQueue<string> _collectionBuildQueue = [];

        public static CustomAssetSystem Instance
        {
            get;
            private set;
        }

        static CustomAssetSystem( )
        {
            var existing = GameObject.Find( "CustomAssetSystem" );

            if ( existing != null )
                Destroy( existing );

            var gameObject = new GameObject( "CustomAssetSystem" );
            Instance = gameObject.AddComponent<CustomAssetSystem>();            
        }

        private void Start( )
        {
            DontDestroyOnLoad( this );

            var collectionDirectories = Directory.GetDirectories( SOURCE_PATH );

            if ( collectionDirectories?.Length > 0 )
            {
                foreach ( var collectionDirectory in collectionDirectories )
                    _collectionBuildQueue.Enqueue( collectionDirectory );

                _collectionBufferQueue.Enqueue( true );
            }
        }

        private void Update( )
        {
            if ( !hasPreparedPrefabs && _collectionBuildQueue.Count == _collectionBufferQueue.Count )
                hasPreparedPrefabs = true;

            if ( canBuild && _collectionBuildQueue.TryDequeue( out var collectionDirectory ) )
            {
                Debug.Log( $"Importing collection {collectionDirectory}..." );
                var collection = CustomAssetCollection.Load( collectionDirectory );

                CustomAssetImporter.Import( collection, collectionDirectory );
                canBuild = false;
            }
            else if ( hasPreparedPrefabs && !canBuild && _collectionBufferQueue.Any( ) &&
                _collectionBufferQueue.TryDequeue( out _ ) )
            {
                PrefabBuilder.BuildResults( );
                canBuild = true;
            }
        }
    }
}
