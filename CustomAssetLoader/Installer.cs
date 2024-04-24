using BepInEx.Logging;
using CustomAssetLoader.Schemas;
using Game.Areas;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomAssetLoader
{
    /// <summary>
    /// Automatically searches BepInEx directory and copies all changed maps
    /// </summary>
    internal class Installer
    {
        static char _S = Path.DirectorySeparatorChar;
        static string GAME_PATH = Path.GetDirectoryName( UnityEngine.Application.dataPath );
        static string BEPINEX_PATH = Path.Combine( GAME_PATH, $"BepInEx{_S}plugins" );

        public static readonly string MOD_PATH = Path.Combine( Application.persistentDataPath, "Mods", "CustomAssetLoader" );
        public static readonly string ASSETS_PATH = Path.Combine( MOD_PATH, "Source" );

        static string THUNDERSTORE_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}Roaming{_S}Thunderstore Mod Manager{_S}DataFolder{_S}CitiesSkylines2{_S}profiles" );
        static string RMODMAN_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}Roaming{_S}r2modmanPlus-local{_S}CitiesSkylines2{_S}profiles" );
        static List<Action> _currentActions = new List<Action>( );

        private static ManualLogSource _logger;
        private static bool _hasErrors = false;

        internal Installer( ManualLogSource logger )
        {
            _logger = logger;
        }

        /// <summary>
        /// Scan all plugin folders for maps directories
        /// </summary>
        private void ScanDirectory( )
        {
            try
            {
                if ( Directory.Exists( BEPINEX_PATH ) )
                {
                    _logger.LogInfo( "Scanning BepInEx folder..." );
                    ProcessSource( BEPINEX_PATH );
                }

                var thunderStorePath = GetActiveThunderstoreProfile( );

                if ( !string.IsNullOrEmpty( thunderStorePath ) )
                {
                    _logger.LogInfo( $"Scanning Thunderstore folder '{thunderStorePath}'..." );
                    ProcessSource( thunderStorePath );
                }

                var rModManPath = GetActiveRModManProfile( );

                if ( !string.IsNullOrEmpty( rModManPath ) )
                {
                    _logger.LogInfo( $"Scanning rModMan folder '{rModManPath}'..." );
                    ProcessSource( rModManPath );
                }

                // If no actions were queued there's no changes
                if ( _currentActions.Count == 0 )
                {
                    OnComplete( );
                    _logger.LogInfo( "No changes detected!" );
                }
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Check for mod manager active path (TODO: Use hashing!)
        /// </summary>
        /// <returns></returns>
        private string GetActiveProfile( string path )
        {
            if ( !Directory.Exists( path ) )
                return null;

            DateTime mostRecent = DateTime.MinValue;
            var mostRecentProfile = string.Empty;

            foreach ( var profileDirectory in Directory.GetDirectories( path ) )
            {
                var bepInExPath = Path.Combine( profileDirectory, $"BepInEx{_S}plugins" );

                if ( Directory.Exists( bepInExPath ) )
                {
                    var mostRecentModified = GetMostRecentModifiedDate( bepInExPath );

                    if ( mostRecentModified > mostRecent )
                    {
                        mostRecent = mostRecentModified;
                        mostRecentProfile = bepInExPath;
                    }
                }
            }

            return mostRecentProfile;
        }

        /// <summary>
        /// Get the active Thunderstore profile
        /// </summary>
        /// <returns></returns>
        private string GetActiveThunderstoreProfile( )
        {
            return GetActiveProfile( THUNDERSTORE_PATH );
        }

        /// <summary>
        /// Get the active RModMan profile
        /// </summary>
        /// <returns></returns>
        private string GetActiveRModManProfile( )
        {

            if ( Directory.Exists( RMODMAN_PATH ) )
            {
                return GetActiveProfile( RMODMAN_PATH );
            }
            else
            {
                String envVar = Environment.GetEnvironmentVariable( "DOORSTOP_INVOKE_DLL_PATH" );
                String dllPath = Path.GetDirectoryName( envVar );
                String profilesPath = Path.GetFullPath( Path.Combine( dllPath, "..", "..", ".." ) );

                return GetActiveProfile( profilesPath );
            }
        }

        /// <summary>
        /// Gets the most recent date modified date
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public DateTime GetMostRecentModifiedDate( string directory )
        {
            return Directory.GetFiles( directory, "*", SearchOption.AllDirectories )
                                          .Select( file => new FileInfo( file ).LastWriteTime )
                                          .OrderByDescending( date => date )
                                          .FirstOrDefault( );
        }

        /// <summary>
        /// Process a source directory
        /// </summary>
        /// <param name="sourceDirectory"></param>
        private void ProcessSource( string sourceDirectory )
        {
            var assetDirectories = Directory.GetFiles( sourceDirectory, "*.json", SearchOption.AllDirectories )?
                .Where( f => Path.GetFileName( f ).ToLowerInvariant( ) == "assets.json" )
                .Select( Path.GetDirectoryName )
                .Distinct();

            if ( !assetDirectories.Any( ) )
                return;

            foreach ( var assetDirectory in assetDirectories )
            {
                if ( !CustomAssetCollection.HasAssets( assetDirectory ) )
                    continue;

                var collection = CustomAssetCollection.Load( assetDirectory );

                ProcessZipFile( collection, sourceDirectory, Path.Combine( assetDirectory, "assets.zip" ) );
            }
        }

        /// <summary>
        /// Ensure the local mod folder exists
        /// </summary>
        /// <remarks>
        /// (No need to do an exists check as this does nothing if
        /// it already exists.)
        /// </remarks>
        private void EnsureModFolder( )
        {
            try
            {
                Directory.CreateDirectory( ASSETS_PATH );
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Search a ZIP file for any maps
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="sourceDirectory"></param>
        /// <param name="zipFilePath"></param>
        private void ProcessZipFile( CustomAssetCollection collection, string sourceDirectory, string zipFilePath )
        {
            var readableString = Path.GetRelativePath( sourceDirectory, zipFilePath );

            if ( ZipFileHasChanges( collection, zipFilePath, Path.GetFullPath( Path.Combine( ASSETS_PATH, collection.Name, "ProjectFiles" ) ) ) )
            {
                _logger.LogInfo( $"Detected changes in assets ZIP '{readableString}', queuing for copy..." );
                _currentActions.Add( GenerateZipCopyTask( collection, sourceDirectory, zipFilePath ) );
            }
        }

        /// <summary>
        /// Check if a ZIP file has any changes if it has map files
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="zipFilePath"></param>
        /// <param name="targetFolder"></param>
        /// <returns></returns>
        private bool ZipFileHasChanges( CustomAssetCollection collection, string zipFilePath, string targetFolder )
        {
            var targetLocation = $"{collection.Name}/";

            using ( var archive = ZipFile.OpenRead( zipFilePath ) )
            {
                foreach ( var entry in archive.Entries )
                {
                    if ( entry.FullName.EndsWith( "/" ) || entry.FullName.ToLowerInvariant( ).Contains( targetLocation.ToLowerInvariant() ) ||
                        ( !entry.FullName.ToLowerInvariant( ).EndsWith( ".png" ) && !entry.FullName.ToLowerInvariant( ).EndsWith( ".fbx" ) ))
                        continue;

                    _logger.LogInfo( targetFolder );
                    // Extract the relative path of the file within the 'Maps' directory in the ZIP archive
                    var targetFilePath = SanitiseZipEntryPath( Path.GetFileName( entry.FullName ), targetFolder );

                    // If the file does not exist in the target folder, it's considered a change
                    if ( !File.Exists( targetFilePath ) )
                        return true;

                    // Check if the file has changed
                    if ( GetZipEntryHash( entry ) != GetFileHash( targetFilePath ) )
                        return true;
                }
            }
            return false; // No changes detected in zip file compared to target folder
        }

        /// <summary>
        /// Gets an MD5 hash for a file
        /// </summary>
        /// <remarks>
        /// (Used to detect changes)
        /// </remarks>
        /// <param name="stream"></param>
        /// <returns></returns>
        private string GetHash( Stream stream )
        {
            using ( var md5 = MD5.Create( ) )
            {
                var hash = md5.ComputeHash( stream );
                return BitConverter.ToString( hash ).Replace( "-", "" ).ToLowerInvariant( );
            }
        }

        /// <summary>
        /// Get a local file system file MD5 hash
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetFileHash( string file )
        {
            using ( var stream = File.OpenRead( file ) )
            {
                return GetHash( stream );
            }
        }

        /// <summary>
        /// Gets a zip entry file MD5 hash
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private string GetZipEntryHash( ZipArchiveEntry entry )
        {
            using ( var stream = entry.Open( ) )
            {
                return GetHash( stream );
            }
        }

        /// <summary>
        /// Sanitise a ZIP entry path to ensure no dodgey exploits!
        /// </summary>
        /// <param name="entryFullName"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException"></exception>
        private string SanitiseZipEntryPath( string entryFullName, string targetDirectory )
        {
            // Normalize the zip entry path to prevent directory traversal
            var sanitisedPath = Path.GetFullPath( Path.Combine( targetDirectory, entryFullName ) );

            // Ensure the sanitized path still resides within the target directory
            if ( !sanitisedPath.StartsWith( targetDirectory, StringComparison.OrdinalIgnoreCase ) )
                throw new SecurityException( "Attempted to extract a file outside of the target directory." );

            return sanitisedPath;
        }

        /// <summary>
        /// Generate copy tasks for maps located in ZIP files
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="zipFilePath"></param>
        /// <returns></returns>
        private Action GenerateZipCopyTask( CustomAssetCollection collection, string sourceDirectory, string zipFilePath )
        {
            return ( ) =>
            {
                try
                {
                    var readableString = Path.GetRelativePath( sourceDirectory, zipFilePath );

                    _logger.LogInfo( $"Processing zip file '{readableString}'." );

                    var jsonSavePath = Path.Combine( ASSETS_PATH, collection.Name );
                    collection.Save( jsonSavePath );

                    var saveRootPath = Path.GetFullPath( Path.Combine( jsonSavePath, "ProjectFiles" ) );

                    Directory.CreateDirectory( saveRootPath );

                    using ( var archive = ZipFile.OpenRead( zipFilePath ) )
                    {
                        foreach ( var asset in collection.Assets )
                        {
                            var path = $"{asset.Name}/";
                            var entries = archive.Entries.Where( e => !e.FullName.EndsWith( "/" ) &&
                                e.FullName.ToLowerInvariant( ).Contains( path.ToLowerInvariant( ) ) &&
                                ( e.FullName.ToLowerInvariant( ).EndsWith( ".png" ) || e.FullName.ToLowerInvariant( ).EndsWith( ".fbx" ) ) ).ToArray( );

                            if ( !entries.Any( ) )
                                continue;

                            var savePath = Path.GetFullPath( Path.Combine( saveRootPath, asset.Name ) );

                            var totalEntries = entries.Length;
                            _logger.LogInfo( $"Extracting '{totalEntries}' files from '{readableString}'." );

                            var complete = 0;

                            foreach ( var entry in entries )
                            {
                                var entryName = Path.GetFileName( entry.FullName );
                                var directoryName = Path.GetDirectoryName( entry.FullName );

                                var targetPath = SanitiseZipEntryPath( entryName, savePath );

                                // Ensure the target directory exists
                                Directory.CreateDirectory( Path.GetDirectoryName( targetPath ) );

                                // Extract the file to the target path
                                entry.ExtractToFile( targetPath, true ); // Overwrite if exists

                                complete++;
                                var progress = ( int ) ( ( complete / ( decimal ) totalEntries ) * 100 );

                                if ( progress % 10 == 0 )
                                    _logger.LogInfo( $"Extracting file {complete + 1}/{totalEntries}..." );
                            }
                        }                        
                    }

                    _logger.LogInfo( $"Finished processing zip file '{readableString}'." );
                }
                catch ( Exception ex )
                {
                    HandleException( ex );
                }
            };
        }

        /// <summary>
        /// Run all of the copy actions in a serial manner so we don't hammer
        /// HDD/SSD drives.
        /// </summary>
        private void RunActions( )
        {
            if ( _currentActions.Count == 0 )
            {
                OnComplete( );
                return;
            }

            Task.Run( ( ) =>
            {
                foreach ( var action in _currentActions )
                    action( );

                OnComplete( );
            } );
        }

        /// <summary>
        /// Clear the list to let GC clear memory
        /// </summary>
        private void Clear( )
        {
            _currentActions.Clear( );
        }

        /// <summary>
        /// Handle exceptions, use best practice of only handling
        /// exceptions we expect within our context.
        /// </summary>
        /// <param name="ex"></param>
        private void HandleException( Exception ex )
        {
            if ( ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is InvalidDataException /* For handling invalid or corrupt zip files */ ||
                ex is FileNotFoundException /* If a zip file is not found */)
            {
                _logger.LogError( ex );
                _hasErrors = true;
            }
            else
                throw ex; // Rethrow if it's not an expected exception
        }

        /// <summary>
        /// Check for errors and advise the user if necessary
        /// </summary>
        private void CheckForErrors( )
        {
            if ( !_hasErrors )
                return;

            _logger.LogInfo( @"CustomAssetLoader encountered errors trying to copy assets, " +
                "for support please visit the Cities2Modding discord referencing the error." );
            _logger.LogInfo( @"See BepInEx log file at: 'BepInEx\plugins' folder.");
        }

        /// <summary>
        /// Executed when the installer actions are complete
        /// </summary>
        private void OnComplete( )
        {
            CheckForErrors( );
            Clear( );
        }

        /// <summary>
        /// Run the installer tasks
        /// </summary>
        public void Run( )
        {
            EnsureModFolder( );
            ScanDirectory( );
            RunActions( );
        }
    }
}