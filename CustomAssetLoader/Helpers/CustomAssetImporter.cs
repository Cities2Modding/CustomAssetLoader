using Colossal.AssetPipeline.Importers;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Game.AssetPipeline;
using Game.Prefabs;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using CustomAssetLoader.Patches;
using System.Collections.Concurrent;
using System;
using CustomAssetLoader.Schemas;
using System.Threading;

namespace CustomAssetLoader.Helpers
{
    public class CustomAssetImporter
    {
        private static int _expectedCount;
        private static int _completedCount;

        public static void Import( CustomAssetCollection collection, string assetSourcePath )
        {
            var prefabFactory = new CustomPrefabFactory( );
            AssetImportPipeline.useParallelImport = true;
            AssetImportPipeline.targetDatabase = AssetDatabase.user;
            TextureImporter.overrideCompressionEffort = -1;

            if ( !AssetImportPipeline.IsArtRootPath( assetSourcePath, [Path.Combine( assetSourcePath, "ProjectFiles" )], out var artProjectPath, out var artProjectRelativePaths ) )
            {
                Debug.Log( "CAL: IsArtRootPath returned false" );
                return;
            }

            _completedCount = 0;
            _expectedCount = collection.Assets.Count;

            AssetDatabase_UnloadAllAssets.overrideUnload = true;
            AssetImportPipeline.ImportPath( artProjectPath, artProjectRelativePaths, ImportMode.All, false, ReportProgress, prefabFactory );
            AssetDatabase_UnloadAllAssets.overrideUnload = false;
        }

        public static void CreatePrefab( string absolutePath, string sourcePath, IReadOnlyList<List<Colossal.AssetPipeline.LOD>> assets )
        {
            // For now just use the highest detail
            var lod = assets.FirstOrDefault( )?.OrderBy( lod => lod.level ).FirstOrDefault( );

            if ( lod != null )
            {
                var geometry = lod.geometry;
                var surface = lod.surfaces.FirstOrDefault();
                var assetName = Path.GetFileNameWithoutExtension( absolutePath );
                var collectionName = Path.GetFileNameWithoutExtension( Directory.GetParent( Directory.GetParent( absolutePath ).FullName ).FullName );
                
                PrefabBuilder.BuildProp( collectionName, assetName, geometry, surface );

                Interlocked.Increment( ref _completedCount );

                Debug.Log( $"Asset '{collectionName}_{assetName}' has been imported." );
                
                if ( _expectedCount == _completedCount )
                    Systems.CustomAssetSystem._collectionBufferQueue.Enqueue( true );
            }
        }

        private static bool ReportProgress( string title, string info, float progress )
        {
            Debug.Log( ( title + " " + info + " " + progress.ToString( ) ) );
            return false;
        }
    }

    public class CustomPrefabFactory : IPrefabFactory
    {
        private readonly List<(PrefabBase Prefab, string Source)> _rootPrefabs = new List<(PrefabBase, string)>( );
        private readonly List<PrefabBase> _createdPrefabs = new List<PrefabBase>( );

        public IReadOnlyList<(PrefabBase Prefab, string Source)> rootPrefabs =>  _rootPrefabs;
        public IReadOnlyList<PrefabBase> Prefabs => _createdPrefabs;

        public T CreatePrefab<T>( string sourcePath, string rootMeshName, int lodLevel ) where T : PrefabBase
        {
            UnityEngine.Debug.LogWarning( "Shouldn't do this" );
            T instance = ScriptableObject.CreateInstance<T>( );

            instance.name = rootMeshName;

            if ( lodLevel == 0 )
                _rootPrefabs.Add( (instance, sourcePath) );

            _createdPrefabs.Add( instance );

            return instance;
        }
    }
}
