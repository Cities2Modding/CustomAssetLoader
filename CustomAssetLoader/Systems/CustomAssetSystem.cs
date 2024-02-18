using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using CustomAssetLoader.Helpers;
using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace CustomAssetLoader.Systems
{
    public class CustomAssetSystem : GameSystemBase
    {
        public static readonly string MOD_PATH = Path.Combine( Application.persistentDataPath, "Mods", "CustomAssetLoader" );

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private PrefabSystem _prefabSystem;
        private PrefabBuilder _prefabBuilder;
        private List<PrefabBase> _prefabs;

        protected override void OnCreate( )
        {
            base.OnCreate( );

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _prefabBuilder = new PrefabBuilder( _prefabSystem );
        }

        protected override void OnUpdate( )
        {
        }

        protected override void OnGameLoadingComplete( Purpose purpose, GameMode mode )
        {
            if ( mode != GameMode.MainMenu )
                return;

            CustomAssetImporter.Import( MOD_PATH );

            // This is manual code so just trying out the CO methods first to see if we can get it working ^


            // Grab prefabs on load as it's the most reliable time to
            // evaluate prefabs to use as a base.
            //_prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );

            //var templatePrefab = _prefabs.FirstOrDefault( p => p is StaticObjectPrefab && p.name.ToLowerInvariant( ).Contains( "fence" ) );

            //if ( templatePrefab != null && templatePrefab is StaticObjectPrefab staticObjectPrefab )
            //{
            //    var meshes = staticObjectPrefab.m_Meshes;

            //    var allUnityMeshes = new List<Mesh>( );
            //    var allTextures = new List<Texture2D>( );
            //    var surfaceTemplate = "";
            //    var bounds = new Bounds3( new float3( 10f * -0.5f, 10f * -0.5f, 10f * -0.5f ), new float3( 10f * 0.5f, 10f * 0.5f, 10f * 0.5f ) );

            //    //var materialLibrary = AssetDatabase.global.resources.materialLibrary;

            //    //var materials = materialLibrary.m_Materials.Select( m => m.m_Material.name ).ToList();

            //    //foreach ( var material in materials )
            //    //{
            //    //    UnityEngine.Debug.Log("MAT: "+ material);
            //    //}
            //    //foreach ( var meshInfo in meshes )
            //    //{
            //    //    var mesh = ( RenderPrefab ) meshInfo.m_Mesh;

            //    //    UnityEngine.Debug.Log( $"Mesh: {mesh.name};" );
            //    //    UnityEngine.Debug.Log( $"            pos = {meshInfo.m_Position}, rot = {meshInfo.m_Rotation}, reqState = {meshInfo.m_RequireState}" );

            //    //    var surfaceAssets = mesh.surfaceAssets;

            //    //    foreach ( var surfaceAsset in surfaceAssets )
            //    //    {
            //    //        //if ( materialLibrary != null )
            //    //        //     surfaceTemplate = materialLibrary.Contains( surfaceAsset.materialTemplateHash ) ? materialLibrary.m_Materials.FirstOrDefault( m => m.m_Hash == surfaceAsset.materialTemplateHash ).m_Material.name : "";
            //    //        var material = surfaceAsset.Load( useVT: false );

            //    //        if ( material == null )
            //    //            continue;


            //    //        //foreach ( var kvp in surfaceAsset.textures )
            //    //        //{
            //    //        //    var textureAsset = kvp.Value;
            //    //        //    textureAsset.name = "Wonga";

            //    //        //    var texture = textureAsset.Load( keepOnCPU: TextureAsset.KeepOnCPU.Both );

            //    //        //    if ( texture != null )
            //    //        //    {
            //    //        //        allTextures.Add( CloneTexture( texture ) );
            //    //        //        UnityEngine.Debug.Log( $"            surfaceAsset.texture = {kvp.Key}" );
            //    //        //    }
            //    //        //}

            //    //        surfaceTemplate = material.shader.name;

            //    //        UnityEngine.Debug.Log( $"            surfaceAsset = {surfaceAsset.name}" );
            //    //    }

            //    //    var unityMeshes = mesh.geometryAsset.ObtainMeshes( true );

            //    //    bounds = new Bounds3( unityMeshes[0].bounds.min, unityMeshes[0].bounds.max );

            //    //    UnityEngine.Debug.Log( $"            geometryAsset = {mesh.geometryAsset.name}, filename = {mesh.geometryAsset.GetMeta( ).fileName}" );

            //    //    foreach ( var unityMesh in unityMeshes )
            //    //    {
            //    //        allUnityMeshes.Add( unityMesh );

            //    //        UnityEngine.Debug.Log( $"            unityMesh = {unityMesh.name}, verts = {unityMesh.vertices.Length}, indices = {unityMesh.triangles.Length}" );
            //    //    }

            //    //}

            //    //UnityEngine.Debug.Log( $"{surfaceTemplate} Textures: {allTextures.Count} Meshes: {allUnityMeshes.Count}" );

            //    //allTextures.Add( GenerateDummyTexture( ) );
            //    //allUnityMeshes.Add( GenerateDummyMesh( ) );


            //    //bounds = new Bounds3( allUnityMeshes[0].bounds.min / 10f, allUnityMeshes[0].bounds.max / 10f );

            //    //_prefabBuilder.BuildProp( "optimus-code", "TestAsset01", allUnityMeshes.ToArray( ), allTextures.ToArray( ), surfaceTemplate );

            //}
        }

        private Mesh GenerateDummyMesh( )
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

            triangles = triangles.Reverse( ).ToArray();
            // Assign arrays to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;

            // Recalculate normals for proper lighting
            mesh.Optimize( );
            mesh.RecalculateNormals( );
            return mesh;
        }

        private Texture2D GenerateDummyTexture( )
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

        private Texture2D CloneTexture( Texture src )
        {
            var tmp = RenderTexture.GetTemporary(
                                src.width,
                                src.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Default );

            Graphics.Blit( src, tmp );

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D readableTexture = new Texture2D( src.width, src.height, TextureFormat.RGBA32, false );
            readableTexture.ReadPixels( new Rect( 0, 0, src.width, src.height ), 0, 0 );
            readableTexture.Apply( );
            readableTexture.name = src.name;

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary( tmp );

            return readableTexture;
        }
    }
}
