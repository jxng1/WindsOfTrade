using System;
using System.Collections.Generic;

using HarmonyLib.BUTR.Extensions;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

using WindsOfTrade.Patches;
using static TaleWorlds.Core.ItemObject;

namespace WindsOfTrade.Behaviours
{
    internal class GlobalTradeItemTrackerBehaviour : CampaignBehaviorBase, IDisposable
    { 
        private static InventoryLogic _inventoryLogic => InventoryManager.InventoryLogic;
        private static Dictionary<string, ItemData> _itemDictionary = new Dictionary<string, ItemData>();

        private bool _eventsRegistered;
        private bool _disposed;

        public static Dictionary<string, ItemData> ItemDictionary
        {
            get => _itemDictionary;
        }

        public override void RegisterEvents()
        {
            if (_eventsRegistered)
            {
                return;
            }

            InventoryItemTupleWidget_UpdateCivilianState_Patch.UpdateCivilianState += InventoryItemTupleWidget_UpdateCivilianState_UpdateCivilianState;
            SPInventoryVM_InitializeInventory_Patch.InitializeInventory += SPInventoryVM_InitializeInventory_InitializeInventory;

            _eventsRegistered = true;
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_eventsRegistered)
            {
                InventoryItemTupleWidget_UpdateCivilianState_Patch.UpdateCivilianState-= InventoryItemTupleWidget_UpdateCivilianState_UpdateCivilianState;
                SPInventoryVM_InitializeInventory_Patch.InitializeInventory -= SPInventoryVM_InitializeInventory_InitializeInventory;
            }

            _disposed = true;
        }

        public void UpdatePrices()
        {
            UpdatePrices(MobileParty.MainParty, MobileParty.MainParty.CurrentSettlement);
        }

        public void UpdatePrices(MobileParty mainParty, Settlement settlement)
        {
            UpdatePricesForItemRoster(mainParty.ItemRoster, mainParty, true);

            if (settlement != null)
            {
                UpdatePricesForItemRoster(settlement.ItemRoster, mainParty, false);
            }
        }

        internal static bool CalculateTradeGoodsRumoursForId(string itemId, int marketBuyPrice, int marketSellPrice, Settlement currentSettlement, bool isLeftPanel)
        {
            if (_itemDictionary.TryGetValue(itemId, out var data) && data.IsValid() && (data.itemObject.IsAnimal || data.itemObject.IsTradeGood))
            {
                data.buyRumourList.Clear();
                data.sellRumourList.Clear();

                foreach (PriceData priceData in data.priceDataList)
                {
                    RumourInfo rumour = new RumourInfo();

                    if (isLeftPanel)
                    {
                        if (priceData.sellPrice > marketBuyPrice)
                        {
                            rumour = CalculatePotententialSellProfit(priceData, marketBuyPrice);
                            data.sellRumourList.Add(rumour);
                        }
                        else if (priceData.buyPrice < marketBuyPrice)
                        {
                            rumour = CalculateBetterPurchasePrice(priceData, marketBuyPrice);
                            data.buyRumourList.Add(rumour);
                        }
                    }
                    else if (priceData.sellPrice > marketSellPrice)
                    {
                        rumour = CalculateBetterSellPrice(priceData, marketSellPrice);
                        data.sellRumourList.Add(rumour);
                    }
                    else if (MobileParty.MainParty.CurrentSettlement != null && marketSellPrice > priceData.buyPrice)
                    {
                        rumour = CalculateBuyHereSellThereProfit(priceData, marketSellPrice);
                        data.buyRumourList.Add(rumour);
                    }
                    else if (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt) && priceData.count > 0)
                    {
                        rumour = CalculateBuyTherePrice(priceData);
                        data.buyRumourList.Add(rumour);
                    }
                }
            }

            return data?.buyRumourList?.Count > 0 || data?.sellRumourList?.Count > 0;
        }

        internal static void CalculateTradeGoodsRumours(SPInventoryVM instance)
        {
            foreach (SPItemVM spItemVM in instance.LeftItemListVM) // Market inventory
            {
                if (!CalculateTradeGoodsRumoursForId(spItemVM.StringId,
                    _inventoryLogic.GetItemPrice(spItemVM.ItemRosterElement.EquipmentElement, false),
                    _inventoryLogic.GetItemPrice(spItemVM.ItemRosterElement.EquipmentElement, true),
                    _inventoryLogic.OtherParty.Settlement,
                    true))
                {
                    // TODO: Make this a debug listener
                    Utilities.Log("Failed to calculate market trade good rumours for item: " + spItemVM.StringId, LogLevel.LOG);
                }
            }

            if (_inventoryLogic.OtherParty != null)
            {
                foreach (SPItemVM spItemVM in instance.RightItemListVM) // Player inventory
                {
                    if (!CalculateTradeGoodsRumoursForId(spItemVM.StringId,
                        _inventoryLogic.GetItemPrice(spItemVM.ItemRosterElement.EquipmentElement, false),
                        _inventoryLogic.GetItemPrice(spItemVM.ItemRosterElement.EquipmentElement, true),
                        _inventoryLogic.OtherParty.Settlement,
                        true))
                    {
                        // TODO: Make this a debug listener
                        Utilities.Log("Failed to calculate player trade good rumours for item: " + spItemVM.StringId, LogLevel.LOG);
                    }
                }
            }
        }

        private static RumourInfo CalculateBuyTherePrice(PriceData priceData)
        {
            RumourInfo rumourInfo = new RumourInfo();

            // TODO: implement trade destination tracker
            //rumourInfo.isTradeDestination = Equals(priceData.settlement.Id == TradeDestination.settlementId);
            int distance = Utilities.CalculateIntDistanceBetweenMainPartyAndSettlement(priceData.settlement);

            if (distance > 0)
            {
                MBTextManager.SetTextVariable("COUNT", priceData.count.ToString("#,0"));
                MBTextManager.SetTextVariable("WHERE", priceData.settlement.Name);
                MBTextManager.SetTextVariable("DISTANCE", distance);
                MBTextManager.SetTextVariable("PRICE", priceData.sellPrice.ToString("'$'#,0"));
                rumourInfo.text = new TextObject("{=CTw6t9SU}Buy {COUNT} more at {WHERE} ({DISTANCE} miles) for {PRICE}").ToString();
            }
            rumourInfo.isBestBuy = true;

            return rumourInfo;
        }

        private static RumourInfo CalculateBuyHereSellThereProfit(PriceData priceData, int currentSellPrice)
        {
            RumourInfo rumourInfo = new RumourInfo();

            // TODO: implement trade destination tracker
            //rumourInfo.isTradeDestination = Equals(priceData.settlement.Id == TradeDestination.settlementId);
            float more = (currentSellPrice - (float)priceData.buyPrice) / currentSellPrice * 100.0f;

            if (more >= 1.0f) // TODO: // pull better price sell threshold(%) from config
            {
                int distance = Utilities.CalculateIntDistanceBetweenMainPartyAndSettlement(priceData.settlement);

                if (distance > 0)
                {
                    MBTextManager.SetTextVariable("WHERE", priceData.settlement.Name);
                    MBTextManager.SetTextVariable("DISTANCE", distance);
                    MBTextManager.SetTextVariable("MORE", more.ToString("0"));
                    rumourInfo.text = new TextObject("{=jeKcVBlz}Buy at {WHERE} ({DISTANCE} miles) sell here for {MORE}% profit").ToString();
                    rumourInfo.percentageDifference = more;
                    rumourInfo.profitPerMile = (currentSellPrice - (float)priceData.buyPrice) / distance;
                }
                rumourInfo.isBestBuy = true;
            }

            return rumourInfo;
        }

        private static RumourInfo CalculateBetterSellPrice(PriceData priceData, int currentSellPrice)
        {
            RumourInfo rumourInfo = new RumourInfo();

            // TODO: implement trade destination tracker
            // rumourInfo.isTradeDestination = Equals(priceData.settlement.Id == TradeDestination.settlementId);
            float more = ((float)priceData.sellPrice - currentSellPrice) / priceData.sellPrice * 100.0f;

            if (more >= 1.0f) // TODO: pull better price sell threshold(%) from config
            {
                int distance = Utilities.CalculateIntDistanceBetweenMainPartyAndSettlement(priceData.settlement);

                if (distance > 0)
                {
                    MBTextManager.SetTextVariable("WHERE", priceData.settlement.Name);
                    MBTextManager.SetTextVariable("DISTANCE", distance);
                    MBTextManager.SetTextVariable("MORE", more.ToString("0"));
                    rumourInfo.text = new TextObject("{=sd6cN2Dj}Sell at {WHERE} ({DISTANCE} miles) for {MORE}% more").ToString();
                    rumourInfo.percentageDifference = more;
                    rumourInfo.profitPerMile = ((float)priceData.sellPrice - currentSellPrice) / distance;
                }
                rumourInfo.isBestSell = true;
            }

            return rumourInfo;
        }

        private static RumourInfo CalculateBetterPurchasePrice(PriceData priceData, int currentBuyPrice)
        {
            RumourInfo rumourInfo = new RumourInfo();

            // TODO: implement trade destination tracker
            //rumourInfo.isTradeDestination = Equals(priceData.settlement.Id == TradeDestination.settlementId);
            float cheaper = (currentBuyPrice - (float)priceData.buyPrice) / currentBuyPrice * 100.0f;

            if (cheaper >= 1.0f) // TODO: pull better price buy threshold(%) from config
            {
                int distance = Utilities.CalculateIntDistanceBetweenMainPartyAndSettlement(priceData.settlement);

                if (distance > 0)
                {
                    MBTextManager.SetTextVariable("CHEAPER", cheaper.ToString("0"));
                    MBTextManager.SetTextVariable("WHERE", priceData.settlement.Name);
                    MBTextManager.SetTextVariable("DISTANCE", distance);
                    rumourInfo.text = string.Format("{0} {1}",
                        new TextObject("{=1TTAJSpB}Buy {CHEAPER}% cheaper at {WHERE} ({DISTANCE} miles)").ToString(),
                        ShowStockAtSettlement(priceData).TrimEnd(Array.Empty<char>()));
                    rumourInfo.profitPerMile = (currentBuyPrice - (float)priceData.buyPrice) / distance;
                }
                rumourInfo.isBestBuy = true;
            }

            return rumourInfo;
        }

        private static string ShowStockAtSettlement(PriceData priceData)
        {
            string result = string.Empty;

            if (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt))
            {
                MBTextManager.SetTextVariable("COUNT", priceData.count);
                result = new TextObject("{=TBqKTzSv}{COUNT} units").ToString();
            }

            return result;
        }

        private static RumourInfo CalculatePotententialSellProfit(PriceData priceData, float currentBuyPrice)
        {
            RumourInfo rumourInfo = new RumourInfo();

            // TODO: implement trade destination tracker
            //rumourInfo.isTradeDestination = Equals(priceData.settlement.Id == TradeDestination.settlementId);
            float profit = (priceData.sellPrice - currentBuyPrice) / priceData.sellPrice * 100.0f;

            if (profit >= 0.5f) // TODO: pull profit threshold(%) from config
            {
                int distance = Utilities.CalculateIntDistanceBetweenMainPartyAndSettlement(priceData.settlement);

                if (distance > 0)
                {
                    MBTextManager.SetTextVariable("WHERE", priceData.settlement.Name);
                    MBTextManager.SetTextVariable("DISTANCE", distance);
                    MBTextManager.SetTextVariable("PROFIT", profit.ToString("0"));
                    rumourInfo.text = new TextObject("{=mKle3hW9}Sell at {WHERE} ({DISTANCE} miles) for {PROFIT}% profit").ToString();
                    rumourInfo.percentageDifference = profit;
                    rumourInfo.profitPerMile = (priceData.sellPrice - currentBuyPrice) / distance;
                }
                rumourInfo.isBestSell = true;
            }

            return rumourInfo;
        }

        private static void LoadGame(IDataStore dataStore)
        {
            try
            {
                float trackerRadius = 0;
                dataStore.SyncData("trade_tracker_radius", ref trackerRadius);
                TrackerRadius.radius = trackerRadius;

                // TODO: trade destination tracker
                //string tradeDestination = string.Empty;
                //dataStore.SyncData<string>("trade_tracker_trade_destination", ref TradeDestination.Name);
                //TradeDestination.Name = tradeDestination;

                string stockValues = CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.Serialise();
                dataStore.SyncData("trade_tracker_stock_values", ref stockValues);
                CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.Deserialise(stockValues);
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
                dataStore.SyncData("trade_tracker_radius", ref trackerRadius);

                // TODO: trade destination tracker
                //string tradeDestination = TradeDestination.Name;
                //dataStore.SyncData<string>("trade_tracker_trade_destination", ref tradeDestination);

                string stockValues = CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.Serialise();
                dataStore.SyncData("trade_tracker_stock_values", ref stockValues);
                CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.Deserialise(stockValues);
            }
            catch (Exception e)
            {
                Utilities.Log(e.Message, LogLevel.ERROR);
            }
        }

        private static void UpdatePricesForItemRoster(ItemRoster itemRoster, MobileParty party, bool isPlayerItem)
        {
            HashSet<string> itemIds = new HashSet<string>();

            foreach (ItemRosterElement itemRosterElement in itemRoster)
            {
                ItemObject itemObject = itemRosterElement.EquipmentElement.Item;
                string itemId = itemObject.StringId;

                if (!itemIds.Contains(itemId))
                {
                    _itemDictionary[itemId] = new ItemData(itemObject, GetPricesForItemFromParty(itemObject, party));
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
                    PriceData priceData = new PriceData(settlement, sellPrice, buyPrice, stock);

                    prices.Add(priceData);
                }
            }

            prices.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            prices.Sort((a, b) => a.buyPrice.CompareTo(b.buyPrice));

            return prices;
        }

        private void InventoryItemTupleWidget_UpdateCivilianState_UpdateCivilianState(InventoryItemTupleWidget widget)
        {
            if (widget is not InventoryItemTupleWidgetIntercept widgetIntercept)
            {
                return;
            }

            if (!widget.MainContainer.Brush.IsCloneRelated(widget.DefaultBrush))
            {
                return;
            }

            widget.MainContainer.Brush = widgetIntercept.ShouldHighlightItem ? widgetIntercept.BetterItemHighlightBrush : widget.DefaultBrush;
        }

        private void SPInventoryVM_InitializeInventory_InitializeInventory(SPInventoryVM spInventoryVM)
        {
            CalculateTradeGoodsRumours(spInventoryVM);

            spInventoryVM.LeftItemListVM.ApplyActionOnAllItems(vm => vm.GetMixinForVM()?.Refresh());
            spInventoryVM.RightItemListVM.ApplyActionOnAllItems(vm => vm.GetMixinForVM()?.Refresh());
        }
    }
}
