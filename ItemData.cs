using System.Collections.Generic;

using TaleWorlds.Core;

namespace WindsOfTrade
{
    internal class ItemData
    {
        internal ItemObject itemObject { get; set; }
        internal List<PriceData> priceInfoList { get; set; }

        internal ItemData(ItemObject item, List<PriceData> list)
        {
            itemObject = item;
            priceInfoList = list;
        }

        internal bool IsValid()
        {
            return itemObject != null && priceInfoList != null;
        }
    }
}