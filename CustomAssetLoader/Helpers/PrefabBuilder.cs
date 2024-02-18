using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using System;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace CustomAssetLoader.Helpers
{
    public class PrefabBuilder( PrefabSystem prefabSystem )
    {
        private readonly PrefabSystem _prefabSystem = prefabSystem;

        public void BuildProp( string collectionName, string assetName, Mesh[] meshes, Texture2D[] textures, string surfaceTemplate = "Default" )
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

        private Colossal.AssetPipeline.Surface BuildSurface( string collectionName, string assetName, Texture2D[] textures, string surfaceTemplate = "Default" )
        {
            var surface = new Colossal.AssetPipeline.Surface( $"{collectionName}_{assetName}", surfaceTemplate );

            if ( textures?.Length > 0 )
            {
                var rootPath = Path.Combine( Installer.ASSETS_PATH, collectionName, assetName );

                var baseColorMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_BaseColorMap", Path.Combine( rootPath, "BaseColorMap.png" ), textures[0] );
                surface.AddProperty( "_BaseColorMap", baseColorMap );

                var normalMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_NormalMap", Path.Combine( rootPath, "NormalMap.png" ), textures[0] );
                surface.AddProperty( "_NormalMap", normalMap );

                var maskMap = new TextureImporter.Texture( $"{collectionName}_{assetName}_MaskMap", Path.Combine( rootPath, "MaskMap.png" ), textures[0] );
                surface.AddProperty( "_MaskMap", maskMap );
            }

            return surface;
        }

        private SurfaceAsset BuildSurfaceAsset( string collectionName, string assetName, Colossal.AssetPipeline.Surface surface )
        {
            var surfaceAsset = new SurfaceAsset( )
            {
                guid = Guid.NewGuid( ),
                database = AssetDatabase.user
            };

            var assetPath = AssetDataPath.Create( $"Mods/CustomAssetLoader/Cache/{collectionName}/{assetName}", "SurfaceAsset" );
            surfaceAsset.database.AddAsset<SurfaceAsset>( assetPath, surfaceAsset.guid );
            surfaceAsset.SetData( surface );
            surfaceAsset.Save( true );

            return surfaceAsset;
        }

        private GeometryAsset BuildGeometryAsset( string collectionName, string assetName, Mesh[] meshes )
        {
            var geometryAsset = new GeometryAsset( )
            {
                guid = Guid.NewGuid( ),
                database = AssetDatabase.user
            };

            var assetPath = AssetDataPath.Create( $"Mods/CustomAssetLoader/Cache/{collectionName}/{assetName}", "geometryAsset" );
            geometryAsset.database.AddAsset<GeometryAsset>( assetPath, geometryAsset.guid );
            geometryAsset.SetData( meshes );
            geometryAsset.Save( true );

            return geometryAsset;
        }

        private RenderPrefab BuildRenderPrefab( string collectionName, string assetName, GeometryAsset geometryAsset, SurfaceAsset surfaceAsset, Bounds3 bounds )
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

        private ObjectMeshInfo BuildObjectMeshInfo( RenderPrefab renderPrefab )
        {
            return new ObjectMeshInfo( )
            {
                m_Mesh = renderPrefab,
                m_Position = float3.zero,
                m_RequireState = Game.Objects.ObjectState.None
            };
        }

        private StaticObjectPrefab BuildStaticObjectPrefab( string collectionName, string assetName, ObjectMeshInfo objectMeshInfo )
        {
            var staticObjectPrefab = ( StaticObjectPrefab ) ScriptableObject.CreateInstance( "StaticObjectPrefab" );
            staticObjectPrefab.name = $"{collectionName}_{assetName}";
            staticObjectPrefab.m_Meshes = [objectMeshInfo];

            var placeholder = ( StaticObjectPrefab ) ScriptableObject.CreateInstance( "StaticObjectPrefab" );
            placeholder.name = $"{collectionName}_{assetName}_Placeholder";
            placeholder.m_Meshes = [objectMeshInfo];
            placeholder.AddComponent<PlaceholderObject>( );

            var spawnableObject = staticObjectPrefab.AddComponent<SpawnableObject>( );
            spawnableObject.m_Placeholders = [placeholder];

            return staticObjectPrefab;
        }

        private UIObject BuildUIObject( StaticObjectPrefab staticObjectPrefab )
        {
            var uiObject = staticObjectPrefab.AddComponent<UIObject>( );
            uiObject.m_IsDebugObject = false;
            uiObject.m_Icon = "Media/Placeholder.svg";
            uiObject.m_Priority = -1;
            return uiObject;
        }
    }
}
