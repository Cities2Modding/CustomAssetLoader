using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Diagnostic;
using Colossal.IO.AssetDatabase;
using CustomAssetLoader.Helpers;
using Game.AssetPipeline;
using Game.Prefabs;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using static Colossal.AssetPipeline.Collectors.SourceAssetCollector;


namespace CustomAssetLoader.Patches
{
    //[HarmonyPatch( typeof( AssetImportPipeline ), "CreateGeometriesAndSurfaces" )]
    //public class AssetImportPipeline_CreateGeometriesAndSurfacesPatch
    //{
    //    static CustomAssetSystem _customAssetSystem;
    //    static MethodInfo _disposeLODs = typeof( AssetImportPipeline ).GetMethods( ).FirstOrDefault( m => m.Name == "DisposeLODs" && m.GetParameters( )[0].ParameterType == typeof( IReadOnlyList<IReadOnlyList<Colossal.AssetPipeline.LOD>> ) );

    //    static void Postfix( SourceAssetCollector.AssetGroup<IAsset> assetGroup, (Report parent, Report.Asset asset) report, ref Action<string, ImportMode, Report, HashSet<SurfaceAsset>, IPrefabFactory> postImportOperations )
    //    {
    //        if ( _customAssetSystem == null )
    //            _customAssetSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CustomAssetSystem>( );
    //        // Your custom logic here
    //        postImportOperations = ( string relativeRootPath, ImportMode importMode, Report r, HashSet<SurfaceAsset> surfaceAssets, IPrefabFactory prefabFactory ) =>
    //        {
    //            var assets = assetGroup.ToArray( );
    //            _customAssetSystem?.OnPostImportProcess( assetGroup.name, relativeRootPath, assets );

    //            var geometry = report.parent.assetDatas.FirstOrDefault( ad => ad.files..assetData.type == typeof( Geometry ) );
    //            UnityEngine.Debug.Log( "Ignored create render prefabs! " + ( geometry != null ? geometry.name : " no geometry" ) );
    //        };
    //    }
    //}

    [HarmonyPatch( typeof( AssetImportPipeline ), "ImportAssetGroup" )]
    public static class AssetImportPipeline_ImportAssetGroup
    {
        public static ConcurrentQueue<string> _queue = [];
        public static void Postfix( string projectRootPath, string relativeRootPath )
        {
            _queue.Enqueue( projectRootPath );
        }
    }

    [HarmonyPatch( typeof( AssetDatabase ), "UnloadAllAssets" )]
    public static class AssetDatabase_UnloadAllAssets
    {
        public static bool overrideUnload = false;

        public static bool Prefix( )
        {
            if ( !overrideUnload )
                return true;

            return false;
        }
    }

    [HarmonyPatch( typeof( AssetImportPipeline ) )]
    public static class AssetImportPipeline_CreateRenderPrefabPatch
    {
        // Specify the method name and the full method signature including the return type and all parameter types
        [HarmonyPatch( "CreateRenderPrefab", new Type[] { typeof( Colossal.AssetPipeline.Settings ), typeof( string ), typeof( IReadOnlyList<Colossal.AssetPipeline.LOD> ), typeof( ImportMode ), typeof( Report ), typeof( HashSet<SurfaceAsset> ), typeof( IPrefabFactory ) } )]
        // Specify the return type explicitly if necessary
        [HarmonyPatch( MethodType.Normal )]
        public static bool Prefix( ref IReadOnlyList<(RenderPrefab prefab, Report.Prefab report)> __result )
        {
            // Initialize __result with an empty list or a dummy value as needed
            __result = new List<(RenderPrefab, Report.Prefab)>( );

            // Log or perform any other necessary actions before skipping the original method
            UnityEngine.Debug.Log( "Skipping execution of CreateRenderPrefab" );

            // Return false to skip the execution of the original method
            return false;
        }
    }

    [HarmonyPatch( typeof( AssetImportPipeline ), "CreateRenderPrefabs", [typeof( Settings ),
        typeof( string ),
        typeof( IReadOnlyList<List<LOD>> ),
        typeof( ImportMode ),
        typeof( Report ),
        typeof( HashSet<SurfaceAsset> ),
        typeof( IPrefabFactory )] )]
    public class AssetImportPipeline_CreateRenderPrefabsPatch
    {
        static bool Prefix( Settings settings,
          string sourcePath,
          IReadOnlyList<List<LOD>> assets,
          ImportMode importMode,
          Report report,
          HashSet<SurfaceAsset> VTMaterials,
          IPrefabFactory prefabFactory )
        {
            if ( AssetImportPipeline_ImportAssetGroup._queue.TryDequeue( out var fullPath ) )
            {
                var parentPath = Path.GetFullPath( Path.Combine( fullPath, sourcePath ) );

                // It's valid
                if ( Directory.Exists( parentPath ) )
                {
                    UnityEngine.Debug.Log( $"ZE FULL PATHS: {fullPath}" );

                    UnityEngine.Debug.Log( "Ignored create render prefabs! " + assets.Count );
                    CustomAssetImporter.CreatePrefab( parentPath, sourcePath, assets );
                }
            }

            return false;
        }
    }

    //[HarmonyPatch( typeof( AssetImportPipeline ), "CreateRenderPrefab", [typeof(string), typeof(string), typeof(int), typeof(IPrefabFactory)] )]
    //public class AssetImportPipeline_CreateRenderPrefabPatch
    //{
    //    static bool Prefix( )
    //    {
    //        UnityEngine.Debug.Log( "Ignored create render prefab!" );
    //        return false;
    //    }
    //}

    //[HarmonyPatch( typeof( AssetImportPipeline ), "CreateRenderPrefab", [typeof( Settings ), typeof( string ), typeof( IReadOnlyList<Colossal.AssetPipeline.LOD> ), typeof( ImportMode ), typeof( Report ), typeof( HashSet<SurfaceAsset> ), typeof( IPrefabFactory )] )]
    //public class AssetImportPipeline_CreateRenderPrefabPatch2
    //{
    //    static bool Prefix( )
    //    {
    //        UnityEngine.Debug.Log( "Ignored create render prefab!" );
    //        return false;
    //    }
    //}
}
