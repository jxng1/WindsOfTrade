using HarmonyLib;

using TaleWorlds.CampaignSystem;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(Campaign), "OnDataLoadFinished")]
    internal class CampaignEvents_OnDataLoadFinished_Patch
    {
        public static void Postfix()
        {
            SubModule.globalTradeItemTrackerBehaviour?.UpdatePrices(); // TODO: figure out a better way to do this
        }
    }
}