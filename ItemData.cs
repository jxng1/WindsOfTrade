using System.Collections.Generic;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.Core;

namespace WindsOfTrade
{
    internal class ItemData
    {
        internal ItemObject itemObject { get; set; }

        internal List<PriceData> priceDataList { get; set; }

        internal List<RumourInfo> buyRumourList { get; set; }

        internal List<RumourInfo> sellRumourList { get; set; }


        internal ItemData(ItemObject itemObject, List<PriceData> priceDataList)
        {
            this.itemObject = itemObject;
            this.priceDataList = priceDataList;
            buyRumourList = new List<RumourInfo>();
            sellRumourList = new List<RumourInfo>();
        }

        internal ItemData(ItemObject itemObject, List<PriceData> priceDataList, List<RumourInfo> buyRumourList, List<RumourInfo> sellRumourList)
        {
            this.itemObject = itemObject;
            this.priceDataList = priceDataList;
            this.buyRumourList = buyRumourList;
            this.sellRumourList = sellRumourList;
        }

        internal bool IsValid()
        {
            return itemObject != null && priceDataList != null;
        }
    }
}