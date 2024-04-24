using Game.Common;
using Game;
using HarmonyLib;
using CustomAssetLoader.Systems;

namespace CustomAssetLoader.Patches
{
    [HarmonyPatch( typeof( SystemOrder ) )]
    internal class SystemOrderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch( typeof( SystemOrder ), nameof( SystemOrder.Initialize ) )]
        public static void GetSystemOrder( UpdateSystem updateSystem )
        {
            //updateSystem?.UpdateAt<CustomAssetSystem>( SystemUpdatePhase.PrefabUpdate );
            var insance = CustomAssetSystem.Instance;
        }
    }
}
