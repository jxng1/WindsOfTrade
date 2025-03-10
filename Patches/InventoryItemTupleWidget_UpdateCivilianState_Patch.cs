using HarmonyLib;

using System;

using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(InventoryItemTupleWidget), "UpdateCivilianState")]
    internal class InventoryItemTupleWidget_UpdateCivilianState_Patch
    {
        internal static event Action<InventoryItemTupleWidget>? UpdateCivilianState;

        public static void Postfix(InventoryItemTupleWidget __instance)
        {
            if (__instance.ScreenWidget == null)
            {
                return;
            }

            UpdateCivilianState?.Invoke(__instance);
        }
    }
}
