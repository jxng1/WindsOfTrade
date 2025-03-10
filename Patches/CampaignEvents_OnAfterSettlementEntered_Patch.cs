using HarmonyLib;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(CampaignEvents), "OnAfterSettlementEntered")]
    internal class CampaignEvents_OnAfterSettlementEntered_Patch
    {
        public static void Postfix(MobileParty party, Settlement settlement)
        {
            if (party == MobileParty.MainParty)
            {
                SubModule.globalTradeItemTrackerBehaviour?.UpdatePrices(party, settlement);
            }
        }
    }
}