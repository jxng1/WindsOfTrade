using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(ItemMenuVM), "SetMerchandiseComponentTooltip")]
    internal static class ItemMenuVM_SetMerchandiseComponentTooltip_Patch
    {
        public static bool Prefix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);

            // Don't run original code so return false, skipping everything
            return false;
        }
    }
}