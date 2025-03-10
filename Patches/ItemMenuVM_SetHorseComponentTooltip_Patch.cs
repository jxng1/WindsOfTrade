using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(ItemMenuVM), "SetHorseComponentTooltip")]
    internal static class ItemMenuVM_SetHorseComponentTooltip_Patch
    {
        public static void Postfix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);
        }
    }
}