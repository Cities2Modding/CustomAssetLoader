using BepInEx;
using System.Reflection;
using System.Linq;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace CustomAssetLoader
{
    [BepInPlugin( MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION )]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake( )
        {
            new Installer( Logger ).Run( );
        }
    }
}
