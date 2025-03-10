using System;

using HarmonyLib;

using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(SPInventoryVM), "InitializeInventory")]
    internal class SPInventoryVM_InitializeInventory_Patch
    {
        internal static event Action<SPInventoryVM>? InitializeInventory;

        public static void Postfix(SPInventoryVM __instance)
        {
            if (__instance.LeftItemListVM == null || __instance.RightItemListVM == null)
            {
                return;
            }


            InitializeInventory?.Invoke(__instance);
        }
    }
}
