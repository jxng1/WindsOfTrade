using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace WindsOfTrade
{
    internal class PriceTrackBehaviour : CampaignBehaviorBase
    {
        internal static Dictionary<string, ItemData> itemDictionary { get; set; } = new Dictionary<string, ItemData>();

        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                SaveGame(dataStore);
            }
            else if (dataStore.IsLoading)
            {
                LoadGame(dataStore);
            }
        }

        private static void LoadGame(IDataStore dataStore)
        {
            try
            {
                float trackerRadius = 0;
                dataStore.SyncData<float>("trade_tracker_radius", ref trackerRadius);
                TrackerRadius.radius = trackerRadius;

                // TODO: trade destination tracker
                //string tradeDestination = string.Empty;
                //dataStore.SyncData<string>("trade_tracker_trade_destination", ref TradeDestination.Name);
                //TradeDestination.Name = tradeDestination;

                string stockValues = CampaignEventDispatcher_OnPlayerInventoryExchange.Serialise();
                dataStore.SyncData<string>("trade_tracker_stock_values", ref stockValues);
                CampaignEventDispatcher_OnPlayerInventoryExchange.Deserialise(stockValues);
            }
            catch (Exception e)
            {
                Utilities.Log(e.Message, LogLevel.ERROR);
            }
        }

        private static void SaveGame(IDataStore dataStore)
        {
            try
            {
                float trackerRadius = TrackerRadius.radius;
                dataStore.SyncData<float>("trade_tracker_radius", ref trackerRadius);

                // TODO: trade destination tracker
                //string tradeDestination = TradeDestination.Name;
                //dataStore.SyncData<string>("trade_tracker_trade_destination", ref tradeDestination);

                string stockValues = CampaignEventDispatcher_OnPlayerInventoryExchange.Serialise();
                dataStore.SyncData<string>("trade_tracker_stock_values", ref stockValues);
                CampaignEventDispatcher_OnPlayerInventoryExchange.Deserialise(stockValues);
            }
            catch (Exception e)
            {
                Utilities.Log(e.Message, LogLevel.ERROR);
            }
        }

        public static void UpdatePrices()
        {
            UpdatePrices(MobileParty.MainParty, MobileParty.MainParty.CurrentSettlement);
        }

        public static void UpdatePrices(MobileParty mainParty, Settlement settlement)
        {
            UpdatePricesForItemRoster(mainParty.ItemRoster, mainParty);

            if (settlement != null)
            {
                UpdatePricesForItemRoster(settlement.ItemRoster, mainParty);
            }
        }

        private static void UpdatePricesForItemRoster(ItemRoster itemRoster, MobileParty party)
        {
            HashSet<string> itemIds = new HashSet<string>(256);

            foreach (ItemRosterElement itemRosterElement in itemRoster)
            {
                ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
                string itemId = itemObject.StringId;

                if (!itemIds.Contains(itemId))
                {
                    List<PriceData> prices = GetPricesForItemFromParty(itemObject, party);

                    itemDictionary[itemId] = new ItemData(itemObject, prices);
                    itemIds.Add(itemId);
                }
            }
        }

        private static List<PriceData> GetPricesForItemFromParty(ItemObject itemObject, MobileParty party)
        {
            List<PriceData> prices = new List<PriceData>();

            // TODO: implement destination tracker too rather than just tracker radius
            foreach (Settlement settlement in Settlement.FindAll(delegate (Settlement s) { return Utilities.CalculateFloatDistanceBetweenPartyAndSettlement(s, party) <= TrackerRadius.radius; }))
            {
                if (settlement.Town != null)
                {
                    TownMarketData townMarketData = settlement.Town.MarketData;
                    ItemRoster itemRoster = settlement.ItemRoster;
                    int stock = itemRoster.GetItemNumber(itemObject);
                    int buyPrice = stock > 0 ? townMarketData.GetPrice(itemObject, party, false, null) : int.MaxValue;
                    int sellPrice = townMarketData.GetPrice(itemObject, party, true, null);
                    PriceData priceInfo = new PriceData(settlement, sellPrice, buyPrice, stock);

                    prices.Add(priceInfo);
                }
            }

            prices.Sort((PriceData a, PriceData b) => a.sellPrice.CompareTo(b.sellPrice));
            prices.Sort((PriceData a, PriceData b) => a.buyPrice.CompareTo(b.buyPrice));

            return prices;
        }
    }
}
