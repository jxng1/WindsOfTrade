using TaleWorlds.CampaignSystem.Settlements;

namespace WindsOfTrade
{
    internal class PriceData
    {
        internal Settlement settlement { get; set; }
        internal int sellPrice { get; set; }
        internal int buyPrice { get; set; }
        internal int count { get; set; }

        internal PriceData(Settlement settlement, int sellPrice, int buyPrice, int count)
        {
            this.settlement = settlement;
            this.sellPrice = sellPrice;
            this.buyPrice = buyPrice;
            this.count = count;
        }
    }
}