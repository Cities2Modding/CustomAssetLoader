using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.City;
using Game.Prefabs;
using Game.UI.InGame;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CustomAssetLoader.Helpers
{
    public static class PrefabBuilder
    {
        public static PrefabSystem _prefabSystem;
        private static readonly ConcurrentQueue<(string collectionName, string assetName, GeometryInfo geometryInfo, Colossal.Hash128 geometryAsset, Colossal.Hash128 surfaceAsset)> _creationQueue = [];

        static PrefabBuilder( )
        {
            _prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>( );
        }

        private static Texture2D GenerateDummyTexture( )
        {
            var width = 64;
            var height = 64;
            var texture = new Texture2D( width, height, TextureFormat.RGBA32, true );

            var yellow = Color.yellow;
            var black = Color.black;

            for ( var y = 0; y < height; y++ )
            {
                for ( var x = 0; x < width; x++ )
                {
                    var isYellow = ( x / 8 + y / 8 ) % 2 == 0;
                    texture.SetPixel( x, y, isYellow ? yellow : black );
                }
            }

            texture.Apply( );

            return texture;
        }

        private static Mesh GenerateDummyMesh( )
        {
            Mesh mesh = new Mesh( );

            var scale = 100f;

            // Define the vertices of the cube
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f * scale, -0.5f * scale, 0.5f * scale), // Front Bottom Left 0
                new Vector3(0.5f * scale, -0.5f * scale, 0.5f * scale), // Front Bottom Right 1
                new Vector3(0.5f * scale, 0.5f * scale, 0.5f * scale), // Front Top Right 2
                new Vector3(-0.5f * scale, 0.5f * scale, 0.5f * scale), // Front Top Left 3
                new Vector3(-0.5f * scale, -0.5f * scale, -0.5f * scale), // Back Bottom Left 4
                new Vector3(0.5f * scale, -0.5f * scale, -0.5f * scale), // Back Bottom Right 5
                new Vector3(0.5f * scale, 0.5f * scale, -0.5f * scale), // Back Top Right 6
                new Vector3(-0.5f * scale, 0.5f * scale, -0.5f * scale) // Back Top Left 7
            };

            // Define the triangles (3 vertices per triangle)
            int[] triangles = new int[]
            {
                0, 2, 1, // Front
                0, 3, 2,
                2, 3, 6, // Top
                3, 7, 6,
                0, 1, 5, // Bottom
                0, 5, 4,
                1, 2, 6, // Right
                1, 6, 5,
                0, 4, 7, // Left
                0, 7, 3,
                5, 6, 7, // Back
                5, 7, 4
            };

            // Define UVs for texture mapping
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            triangles = triangles.Reverse( ).ToArray( );
            // Assign arrays to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;

            // Recalculate normals for proper lighting
            mesh.Optimize( );
            mesh.RecalculateNormals( );
            return mesh;
        }

        private static Texture2D GenerateBlackTexture( )
        {
            var width = 64;
            var height = 64;
            var texture = new Texture2D( width, height, TextureFormat.RGBA32, true );

            var black = Color.black;

            for ( var y = 0; y < height; y++ )
            {
                for ( var x = 0; x < width; x++ )
                {
                    texture.SetPixel( x, y, black );
                }
            }

            texture.Apply( );

            return texture;
        }

        public static void BuildProp( string collectionName, string assetName, Geometry geometry, Surface surface )
        {
            var meshes = geometry.ToUnityMeshes( true );

            foreach ( var mesh in meshes )
            {
                mesh.RecalculateNormals( );
                mesh.RecalculateTangents( );
            }
            //Mesh[] meshes = [GenerateDummyMesh( )];
            //Texture2D[] textures = [GenerateDummyTexture( )];

            //BuildProp( collectionName, assetName, meshes, textures );
            //return;
            //var newSurface = BuildSurface( collectionName, assetName, textures );
            var newSurface = BuildSurface( collectionName, assetName, surface.textures );
            var surfaceAsset = BuildSurfaceAsset( collectionName, assetName, newSurface );
            var geometryAsset = BuildGeometryAsset( collectionName, assetName, meshes );

            //var b = geometry.CalcBounds( );

            //UnityEngine.Debug.Log( $"Asset bounds: {b.min} {b.max}" );

            foreach ( var m in meshes )
            {
                UnityEngine.Debug.Log($"v: {m.vertices.Length} i: {m.triangles.Length}");

            }

            UnityEngine.Debug.Log( $"Prepared prefab {collectionName}_{assetName}" );

            var b = new Bounds3( meshes[0].bounds.min * 0.5f, meshes[0].bounds.max * 0.5f );
            _creationQueue.Enqueue( (collectionName, assetName, /*new GeometryInfo { Bounds = b, SurfaceArea = 1f, VertexCount = meshes[0].vertexCount }*/ new GeometryInfo( geometry ), geometryAsset.guid, surfaceAsset.guid) );
            
        }

        public static void BuildResults( )
        {
            if ( !_creationQueue.Any( ) )
                return;

            while ( _creationQueue.TryDequeue( out var result ) )
            {
                var renderPrefab = BuildRenderPrefab( result.collectionName, result.assetName, result.geometryInfo, result.geometryAsset, result.surfaceAsset );

                var objectMeshInfo = BuildObjectMeshInfo( renderPrefab );

                var staticObjectPrefab = BuildBuildingPrefab( result.collectionName, result.assetName, objectMeshInfo );
                MakePoliceStation( staticObjectPrefab );
                AddServiceConnections( staticObjectPrefab );
                MakePlaceableObject( staticObjectPrefab );

                var uiObject = BuildUIObject( staticObjectPrefab );
                _prefabSystem.AddPrefab( staticObjectPrefab );
                UnityEngine.Debug.Log( $"Added prefab {result.collectionName}_{result.assetName}" );
            }
        }

        public static void BuildProp( string collectionName, string assetName, Mesh[] meshes, Texture2D[] textures, string surfaceTemplate = "Default" )
        {
            var surface = BuildSurface( collectionName, assetName, textures, surfaceTemplate );
            var surfaceAsset = BuildSurfaceAsset( collectionName, assetName, surface );
            var geometryAsset = BuildGeometryAsset( collectionName, assetName, meshes );

            var b = new Bounds3( meshes[0].bounds.min * 0.5f, meshes[0].bounds.max * 0.5f );

            var renderPrefab = BuildRenderPrefab( collectionName, assetName, geometryAsset, surfaceAsset, b );

            var objectMeshInfo = BuildObjectMeshInfo( renderPrefab );
            var staticObjectPrefab = BuildStaticObjectPrefab( collectionName, assetName, objectMeshInfo );
            var uiObject = BuildUIObject( staticObjectPrefab );
            _prefabSystem.AddPrefab( staticObjectPrefab );
            UnityEngine.Debug.Log( $"Added prefab {collectionName}_{assetName}" );
        }

        private static Colossal.AssetPipeline.Surface BuildSurface( string collectionName, string assetName, Texture2D[] textures, string surfaceTemplate = "Default" )
        {
            var surface = new Colossal.AssetPipeline.Surface( $"{collectionName}_{assetName}", surfaceTemplate );

            if ( textures?.Length > 0 )
            {
                var rootPath = Path.Combine( Installer.ASSETS_PATH, collectionName, assetName );

                var baseColorMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_BaseColorMap", Path.Combine( rootPath, $"{assetName}_BaseColorMap.png" ), textures[0] );
                surface.AddProperty( "_BaseColorMap", baseColorMap );

                //var controlMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_ControlMask", Path.Combine( rootPath, $"{assetName}_ControlMask.png" ), textures[1] );
                //surface.AddProperty( "_ControlMask", controlMap );

                //var emissive = new TextureImporter.Texture( $"{collectionName}_{assetName}_Emissive", Path.Combine( rootPath, $"{assetName}_Emissive.png" ), textures[2] );
                //surface.AddProperty( "_EmissiveColorMap", emissive );

                //var maskMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_MaskMap", Path.Combine( rootPath, $"{assetName}_MaskMap.png" ), textures[3] );
                //surface.AddProperty( "_MaskMap", maskMap );

                //var normalMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_NormalMap", Path.Combine( rootPath, $"{assetName}_NormalMap.png" ), textures[4] );
                //surface.AddProperty( "_NormalMap", normalMap );
            }

            return surface;
        }

        private static Colossal.AssetPipeline.Surface BuildSurface( string collectionName, string assetName, Dictionary<string, TextureImporter.ITexture> textures, string surfaceTemplate = "Default" )
        {
            var surface = new Colossal.AssetPipeline.Surface( $"{collectionName}_{assetName}", surfaceTemplate );

            if ( textures?.Count > 0 )
            {
                foreach ( var texture in textures )
                {
                    UnityEngine.Debug.Log( $"Added surface texture {texture.Key}" );
                    surface.AddProperty( texture.Key, texture.Value );
                }
            }
            else
                UnityEngine.Debug.Log( $"NO surface textures" );

            return surface;
        }

        private static SurfaceAsset BuildSurfaceAsset( string collectionName, string assetName, Colossal.AssetPipeline.Surface surface )
        {
            var surfaceAsset = new SurfaceAsset( )
            {
                guid = Guid.NewGuid( ),
                database = AssetDatabase.user
            };

            var assetPath = AssetDataPath.Create( $"Mods/CustomAssetLoader/Cache/{collectionName}/{assetName}", $"{assetName}_SurfaceAsset" );
            surfaceAsset.database.AddAsset<SurfaceAsset>( assetPath, surfaceAsset.guid );
            surfaceAsset.SetData( surface );
            surfaceAsset.Save( true );

            return surfaceAsset;
        }

        private static SurfaceAsset LoadSurfaceAsset( Colossal.Hash128 surfaceAssetGUID )
        {
            return AssetDatabase.user.GetAsset<SurfaceAsset>( surfaceAssetGUID );
        }

        private static GeometryAsset BuildGeometryAsset( string collectionName, string assetName, Mesh[] meshes )
        {
            var geometryAsset = new GeometryAsset( )
            {
                guid = Guid.NewGuid( ),
                database = AssetDatabase.user
            };

            var assetPath = AssetDataPath.Create( $"Mods/CustomAssetLoader/Cache/{collectionName}/{assetName}", $"{assetName}_GeometryAsset" );
            geometryAsset.database.AddAsset<GeometryAsset>( assetPath, geometryAsset.guid );
            geometryAsset.SetData( meshes );
            geometryAsset.Save( true );

            return geometryAsset;
        }

        private static GeometryAsset LoadGeometryAsset( Colossal.Hash128 geometryAssetGUID )
        {
            return AssetDatabase.user.GetAsset<GeometryAsset>( geometryAssetGUID );
        }

        private static RenderPrefab BuildRenderPrefab( string collectionName, string assetName, GeometryAsset geometryAsset, SurfaceAsset surfaceAsset, Bounds3 bounds )
        {
            var renderPrefab = ( RenderPrefab ) ScriptableObject.CreateInstance( "RenderPrefab" );
            renderPrefab.name = $"{collectionName}_{assetName}_RenderPrefab";
            renderPrefab.geometryAsset = new AssetReference<GeometryAsset>( geometryAsset.guid );
            renderPrefab.surfaceAssets = [surfaceAsset];
            renderPrefab.bounds = bounds;
            renderPrefab.meshCount = 1;
            renderPrefab.vertexCount = geometryAsset.GetVertexCount( 0 );
            renderPrefab.indexCount = 1;
            renderPrefab.manualVTRequired = false;
            return renderPrefab;
        }

        private static RenderPrefab BuildRenderPrefab( string collectionName, string assetName, GeometryInfo geometryInfo, Colossal.Hash128 geometryAssetGUID, Colossal.Hash128 surfaceAssetGUID )
        {
            var renderPrefab = ( RenderPrefab ) ScriptableObject.CreateInstance( "RenderPrefab" );
            renderPrefab.name = $"{collectionName}_{assetName}_RenderPrefab";
            renderPrefab.geometryAsset = new AssetReference<GeometryAsset>( geometryAssetGUID );
            renderPrefab.surfaceAssets = [new AssetReference<SurfaceAsset>( surfaceAssetGUID )];
            renderPrefab.manualVTRequired = false;

            renderPrefab.bounds = geometryInfo.Bounds;
            renderPrefab.surfaceArea = geometryInfo.SurfaceArea;
            renderPrefab.indexCount = 1;
            renderPrefab.vertexCount = geometryInfo.VertexCount;
            renderPrefab.meshCount = 1;//geometryInfo.MeshCount;
            return renderPrefab;
        }

        private static ObjectMeshInfo BuildObjectMeshInfo( RenderPrefab renderPrefab )
        {
            return new ObjectMeshInfo( )
            {
                m_Mesh = renderPrefab,
                m_Position = float3.zero,
                m_RequireState = Game.Objects.ObjectState.None
            };
        }

        private static StaticObjectPrefab BuildPlaceholder( string collectionName, string assetName, ObjectMeshInfo objectMeshInfo )
        {
            var placeholder = ScriptableObject.CreateInstance<StaticObjectPrefab>( );
            placeholder.name = $"{collectionName}_{assetName}_Placeholder";
            placeholder.m_Meshes = [objectMeshInfo];
            placeholder.AddComponent<PlaceholderObject>( );
            return placeholder;
        }

        private static void CreateSpawnableObject( StaticObjectPrefab staticObjectPrefab, StaticObjectPrefab placeholderPrefab )
        {
            var spawnableObject = staticObjectPrefab.AddComponent<SpawnableObject>( );
            spawnableObject.m_Placeholders = [placeholderPrefab];
        }

        private static StaticObjectPrefab BuildStaticObjectPrefab( string collectionName, string assetName, ObjectMeshInfo objectMeshInfo )
        {
            var staticObjectPrefab = ScriptableObject.CreateInstance<StaticObjectPrefab>( );
            staticObjectPrefab.name = $"{collectionName}_{assetName}";
            staticObjectPrefab.m_Meshes = [objectMeshInfo];

            var placeholderPrefab = BuildPlaceholder( collectionName, assetName, objectMeshInfo );

            CreateSpawnableObject( staticObjectPrefab, placeholderPrefab );

            return staticObjectPrefab;
        }

        private static BuildingPrefab BuildBuildingPrefab( string collectionName, string assetName, ObjectMeshInfo objectMeshInfo )
        {
            var buildingPrefab = ScriptableObject.CreateInstance<BuildingPrefab>( );
            buildingPrefab.name = $"{collectionName}_{assetName}";
            buildingPrefab.m_Meshes = [objectMeshInfo];
            buildingPrefab.m_LotWidth = 2;
            buildingPrefab.m_LotDepth = 4;

            return buildingPrefab;
        }

        static FieldInfo _servicePrefabService = typeof( ServicePrefab ).GetField( "m_Service", BindingFlags.Instance | BindingFlags.NonPublic );
        static FieldInfo _servicePrefabCityResources = typeof( ServicePrefab ).GetField( "m_CityResources", BindingFlags.Instance | BindingFlags.NonPublic );

        static FieldInfo _prefabs = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private static void MakePoliceStation( BuildingPrefab buildingPrefab )
        {
            //buildingPrefab.AddComponent<PoliceStation>();

            var prefabs = ( List<PrefabBase> ) _prefabs.GetValue( _prefabSystem );

            var prefab = ( BuildingPrefab ) prefabs
                .Where( p => p is BuildingPrefab && p.name.ToLowerInvariant( ).Contains( "policestation" ) && 
                p.components?.Count( c => c is CityServiceBuilding ) > 0 )
                .FirstOrDefault( );

            var policeStation = prefab.components.FirstOrDefault( c => c is PoliceStation );
            var cityServiceBuilding = prefab.components.FirstOrDefault( c => c is CityServiceBuilding );
            var workPlace = prefab.components.FirstOrDefault( c => c is Workplace );
            var serviceCoverage = prefab.components.FirstOrDefault( c => c is ServiceCoverage );
            var pollution = prefab.components.FirstOrDefault( c => c is Pollution );
            var serviceConsumption = prefab.components.FirstOrDefault( c => c is ServiceConsumption );
            var serviceObject = prefab.components.FirstOrDefault( c => c is ServiceObject );

            //var componentNames = prefab.components.Select( c => c.GetType( ).FullName );

            //foreach ( var component in componentNames )
            //{
            //    UnityEngine.Debug.Log( "PoliceStation Comp: " + component );
            //}

            buildingPrefab.components.Add( cityServiceBuilding );
            buildingPrefab.components.Add( policeStation );
            buildingPrefab.components.Add( workPlace );
            buildingPrefab.components.Add( serviceCoverage );
            buildingPrefab.components.Add( pollution );
            buildingPrefab.components.Add( serviceConsumption );
            buildingPrefab.components.Add( serviceObject );
            buildingPrefab.Reset( );
        }

        private static void MakePlaceableObject( PrefabBase prefab )
        {
            var placeableObject = ScriptableObject.CreateInstance<PlaceableObject>( );
            placeableObject.m_ConstructionCost = 100_000;
            placeableObject.m_XPReward = 250;

            prefab.components.Add( placeableObject );
            prefab.Reset( );
        }

        private static void AddServiceConnections( BuildingPrefab buildingPrefab )
        {
            //buildingPrefab.AddComponent<WaterPipeConnection>( );
        }

        private static UIObject BuildUIObject( StaticObjectPrefab staticObjectPrefab )
        {
            UIGroupPrefab group = null;

            if ( staticObjectPrefab is BuildingPrefab )
            {
                var prefabs = ( List<PrefabBase> ) _prefabs.GetValue( _prefabSystem );

                if ( staticObjectPrefab.components.Count( c => c is PoliceStation ) > 0 )
                {
                    var policeStationUIObject = ( UIObject ) prefabs
                        .Where( p => p.name.ToLowerInvariant().Contains( "policestation" ) && p.components?.Count( c => c is UIObject ) > 0 )
                        .Select( p => p.components.FirstOrDefault( c => c is UIObject ) )
                        .FirstOrDefault();

                    if ( policeStationUIObject != null )
                    {
                        group = policeStationUIObject.m_Group;
                        UnityEngine.Debug.Log( "APPLIED GROUP PREFAB SETTING TO UI OBJECT" );
                    }
                }
            }

            var uiObject = staticObjectPrefab.AddComponent<UIObject>( );
            uiObject.m_IsDebugObject = false;
            uiObject.m_Icon = "Media/Placeholder.svg";
            uiObject.m_Priority = -1;
            uiObject.m_Group = group;
            return uiObject;
        }
    }
    /*
     
            renderPrefab.bounds = geometry.CalcBounds( );
            renderPrefab.surfaceArea = geometry.CalcSurfaceArea( );
            renderPrefab.indexCount = geometry.CalcTotalIndices( );
            renderPrefab.vertexCount = geometry.CalcTotalVertices( );
            renderPrefab.meshCount = geometry.models.Length;
     */
    public struct GeometryInfo
    {
        public Bounds3 Bounds
        {
            get;
            set;
        }

        public float SurfaceArea
        {
            get;
            set;
        }

        public int VertexCount
        {
            get;
            set;
        }

        public GeometryInfo( Geometry geometry )
        {
            Bounds = geometry.CalcBounds( );
            SurfaceArea = geometry.CalcSurfaceArea( );
            VertexCount = geometry.CalcTotalVertices( );
        }
    }
}
