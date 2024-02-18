using Colossal.AssetPipeline.Importers;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Game.AssetPipeline;
using Game.Prefabs;
using Game.UI.Editor;
using System.Collections.Generic;
using System;
using UnityEngine;
using Game.Tools;
using Unity.Entities;
using System.IO;

namespace CustomAssetLoader.Helpers
{
    public class CustomAssetImporter
    {
        public static void Import( string assetSourcePath )
        {
            var prefabFactory = new CustomPrefabFactory( );
            AssetImportPipeline.useParallelImport = true;
            AssetImportPipeline.targetDatabase = AssetDatabase.user;
            TextureImporter.overrideCompressionEffort = -1;

            //var pfilesRoot = AssetImportPanel.FindProjectFilesRoot( assetSourcePath );
            //UnityEngine.Debug.Log( pfilesRoot );

            if ( !AssetImportPipeline.IsArtRootPath( assetSourcePath, [Path.Combine( assetSourcePath, "ProjectFiles" )], out var artProjectPath, out var artProjectRelativePaths ) )
            {
                UnityEngine.Debug.Log( "CAL: IsArtRootPath returned false" );
                return;
            }

            UnityEngine.Debug.Log( $"CAL: artProjectPath {artProjectPath}" );

            if ( artProjectRelativePaths?.Count > 0 )
            {
                foreach ( var path in artProjectRelativePaths )
                {
                    UnityEngine.Debug.Log( $"CAL: artProjectRelativePaths {path}" );
                }
            }
            AssetImportPipeline.ImportPath( artProjectPath, artProjectRelativePaths, ImportMode.All, false, ReportProgress, prefabFactory );
            
            foreach ( var prefab in prefabFactory.Prefabs )
            {
                UnityEngine.Debug.Log( "Creating prefab: " + prefab.name );
                AssetImportPipeline.targetDatabase.AddAsset( ( AssetDataPath ) string.Format( "{0}_{1}", prefab.name, prefab.GetType( ) ), prefab ).Save( false );
            }

            var world = World.DefaultGameObjectInjectionWorld;

            PrefabSystem systemManaged1 = world.GetOrCreateSystemManaged<PrefabSystem>( );
            ToolSystem systemManaged2 = world.GetOrCreateSystemManaged<ToolSystem>( );

            foreach ( var rootPrefab in prefabFactory.rootPrefabs )
            {
                UnityEngine.Debug.Log( $"Root prefab: {rootPrefab.Prefab.name} ({rootPrefab.Source})" );

                var instance = ScriptableObject.CreateInstance<BuildingPrefab>( );
                instance.name = rootPrefab.Prefab.name;
                instance.m_Meshes =
                [
                      new ObjectMeshInfo()
                      {
                        m_Mesh = rootPrefab.Prefab as RenderPrefabBase
                      }
                ];
                instance.m_LotWidth = 10;
                instance.m_LotDepth = 8;

                AssetImportPipeline.targetDatabase.AddAsset( ( AssetDataPath ) string.Format( "{0}_{1}", instance.name, instance.GetType( ) ), instance ).Save( false );
                
                systemManaged1.AddPrefab( rootPrefab.Prefab );
                systemManaged2.ActivatePrefabTool( rootPrefab.Prefab );
            }
        }

        private static bool ReportProgress( string title, string info, float progress )
        {
            UnityEngine.Debug.Log( ( title + " " + info + " " + progress.ToString( ) ) );
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
            T instance = ScriptableObject.CreateInstance<T>( );

            instance.name = rootMeshName;

            if ( lodLevel == 0 )
                _rootPrefabs.Add( (instance, sourcePath) );

            _createdPrefabs.Add( instance );

            return instance;
        }
    }
}
