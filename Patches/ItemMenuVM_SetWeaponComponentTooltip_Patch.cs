using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(ItemMenuVM), "SetWeaponComponentTooltip")]
    internal static class ItemMenuVM_SetWeaponComponentTooltip_Patch
    {
        public static void Postfix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);
        }
    }
}