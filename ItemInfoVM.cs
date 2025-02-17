using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WindsOfTrade
{
    internal static class ItemInfoVM
    {
        internal static void ShowTooltips(ItemMenuVM instance)
        {
            try
            {
                if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
                {
                    ItemMenuVMFields itemMenuVMFields = new ItemMenuVMFields(instance);
                    InventoryLogic inventoryLogic = (InventoryLogic)itemMenuVMFields.GetValue("_inventoryLogic");
                    IMarketData marketData = inventoryLogic.MarketData;

                    bool isLeftPanel = !(bool)itemMenuVMFields.GetValue("_isPlayerItem");

                    Settlement? currentSettlement = (inventoryLogic.OtherParty != null) ?
                        inventoryLogic.OtherParty.Settlement :
                        null;
                    ItemVM targetItemVM = (ItemVM)itemMenuVMFields.GetValue("_targetItem");
                    EquipmentElement element = targetItemVM.ItemRosterElement.EquipmentElement;

                    int currentSellPrice = marketData.GetPrice(element.Item, MobileParty.MainParty, true, inventoryLogic.OtherParty);
                    int currentBuyPrice = marketData.GetPrice(element.Item, MobileParty.MainParty, false, inventoryLogic.OtherParty);

                    if (isLeftPanel && (element.Item.IsTradeGood || element.Item.IsAnimal))
                    {
                        ItemRosterElement? item = inventoryLogic.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, element);
                        int itemAmount = (item != null) ? item.GetValueOrDefault().Amount : 0;
                        ShowNewLine(instance);

                        MBTextManager.SetTextVariable("AMOUNT", itemAmount);

                        ShowRumourText(instance, new TextObject("{=VKkiPo9W}You have {AMOUNT}").ToString(), Color.ConvertStringToColor(Colour.ALABASTER));
                    }

                    string itemId = element.Item.StringId;
                    if (!isLeftPanel)
                    {
                        float stockUnitValue = CampaignEventDispatcher_OnPlayerInventoryExchange.GetStockUnitValue(itemId);

                        // If item is in stock
                        if (stockUnitValue > 0.0f)
                        {
                            string sellHereText = new TextObject("{=hve9LACb}Sell here to break even").ToString();
                            Color textColor = Colors.Yellow;

                            // If selling here is at least at profit threshold
                            if (currentSellPrice > stockUnitValue)
                            {
                                float profit = ((float)currentSellPrice - stockUnitValue) / (float)currentSellPrice * 100.0f;

                                if (profit >= 0.5f) // TODO: pull profit threshold(%) from config
                                {
                                    MBTextManager.SetTextVariable("PROFIT", profit);
                                    sellHereText = new TextObject("{=caaVIYnm}Sell here for {PROFIT}% profit").ToString();
                                    textColor = Color.FromUint(3745513216U);
                                }
                            }
                            else if (currentSellPrice < stockUnitValue) // If selling here nets in loss
                            {
                                float loss = (stockUnitValue - (float)currentSellPrice) / stockUnitValue * 100.0f;

                                if (loss >= 0.1f) // TODO: pull loss threshold(%) from config
                                {
                                    MBTextManager.SetTextVariable("LOSS", loss);
                                    sellHereText = new TextObject("{=PA8Hj4bM}Sell here for a {LOSS}% loss").ToString();
                                    textColor = Color.FromUint(4026482495U);
                                }
                            }

                            ShowNewLine(instance);
                            ShowRumourText(instance, sellHereText, textColor);
                        }
                    }
                    else
                    {
                        float averagePurchasePrice = CampaignEventDispatcher_OnPlayerInventoryExchange.GetAveragePurchasePrice(itemId);

                        if (averagePurchasePrice > 0.0f)
                        {
                            string buyHereText = new TextObject("{=oVZwPLij}About the usual price").ToString();
                            Color textColor = Colors.Yellow;

                            if (currentBuyPrice > averagePurchasePrice)
                            {
                                float extra = (currentBuyPrice - averagePurchasePrice) / (currentBuyPrice * 100.0f);
                                if (extra >= 1.0f) // TODO: pull extra cost(from average) threshold(%) from config
                                {
                                    MBTextManager.SetTextVariable("MORE_EXPENSIVE", extra);
                                    buyHereText = new TextObject("{=AbPnZRIw}{MORE_EXPENSIVE}% more expensive than usual").ToString();
                                    textColor = Colors.Red;
                                }
                            }
                            else if (currentBuyPrice < averagePurchasePrice)
                            {
                                float less = (averagePurchasePrice - currentBuyPrice) / (averagePurchasePrice * 100.0f);
                                if (less >= 1.0f) // TODO: pull less cost(from average) threshold(%) from config
                                {
                                    MBTextManager.SetTextVariable("LESS_EXPENSIVE", less);
                                    buyHereText = new TextObject("{=4DjKMJ2l}{LESS_EXPENSIVE}% cheaper than usual").ToString();
                                    textColor = Colors.Green;
                                }
                            }

                            ShowNewLine(instance);
                            CreateProperty(instance.TargetItemProperties, "", buyHereText, 0, textColor, null, TooltipProperty.TooltipPropertyFlags.None);
                        }
                    }

                    ItemData itemData;
                    // May be null here, but doesn't matter as the try get would prevent a null reference exception
                    // Only do items that are trade goods, ignore armours, weapons, etc, uniques
                    if (PriceTrackBehaviour.itemDictionary.TryGetValue(itemId, out itemData) && itemData.IsValid() && (itemData.itemObject.IsTradeGood || itemData.itemObject.IsAnimal))
                    {
                        List<RumourInfo> betterSellRumours = new List<RumourInfo>();
                        List<RumourInfo> betterBuyRumours = new List<RumourInfo>();

                        foreach (PriceData priceData in itemData.priceInfoList)
                        {
                            if (priceData.settlement != currentSettlement)
                            {
                                RumourInfo rumourInfo = new RumourInfo();

                                try
                                {
                                    if (isLeftPanel)
                                    {
                                        if (priceData.sellPrice > currentBuyPrice)
                                        {
                                            rumourInfo = CalculatePotententialSellProfit(priceData, currentBuyPrice);
                                            betterSellRumours.Add(rumourInfo);
                                        }
                                        else if (priceData.buyPrice < currentBuyPrice)
                                        {
                                            rumourInfo = CalculateBetterPurchasePrice(priceData, currentBuyPrice);
                                            betterBuyRumours.Add(rumourInfo);
                                        }
                                    }
                                    else if (priceData.sellPrice > currentSellPrice)
                                    {
                                        rumourInfo = CalculateBetterSellPrice(priceData, currentSellPrice);
                                        betterSellRumours.Add(rumourInfo);
                                    }
                                    else if (MobileParty.MainParty.CurrentSettlement != null && currentSellPrice > priceData.buyPrice)
                                    {
                                        rumourInfo = CalculateBuyHereSellThereProfit(priceData, currentSellPrice);
                                        betterBuyRumours.Add(rumourInfo);
                                    }
                                    else if (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt) && priceData.count > 0)
                                    {
                                        rumourInfo = CalculateBuyTherePrice(priceData);
                                        betterBuyRumours.Add(rumourInfo);
                                    }
                                }
                                catch (Exception e1)
                                {
                                    Utilities.Log(e1.Message, LogLevel.ERROR);
                                    Utilities.Log(e1.StackTrace, LogLevel.LOG);
                                }
                            }
                        }

                        ShowNewLine(instance);
                        CreateProperty(instance.TargetItemProperties,
                            "",
                            new TextObject("{=ii72WyNL}PRICE DATA").ToString(),
                            1,
                            Color.FromUint(4293446041U),
                            null,
                            TooltipProperty.TooltipPropertyFlags.None);

                        if (instance.IsComparing)
                        {
                            CreateProperty(instance.ComparedItemProperties, "", "", 0, Colors.Black, null, TooltipProperty.TooltipPropertyFlags.None);
                            CreateProperty(instance.ComparedItemProperties, "", "", 0, Colors.Black, null, TooltipProperty.TooltipPropertyFlags.None);
                        }

                        ShowNewLine(instance);
                        ShowSellRumours(instance, isLeftPanel, betterSellRumours);
                        ShowBuyRumours(instance, isLeftPanel, betterBuyRumours);
                    }
                }
            }
            catch (Exception e2)
            {
                Utilities.Log("Failed to show tooltips: " + e2.Message, LogLevel.ERROR);
            }
        }

        private static void ShowBuyRumours(ItemMenuVM instance, bool isLeftPanel, List<RumourInfo> betterBuyRumours)
        {
            if (betterBuyRumours.Count > 0)
            {
                betterBuyRumours = FilterBuyRumours(betterBuyRumours);

                foreach (RumourInfo rumour in betterBuyRumours)
                {
                    ShowRumourTooltip(instance, rumour);
                }
            }
            else if (isLeftPanel)
            {
                ShowRumourText(instance, new TextObject("{=fusk5kOb}This is the best place to buy this item").ToString(), Color.FromUint(4278255360U));
            }
        }

        private static void ShowSellRumours(ItemMenuVM instance, bool isLeftPanel, List<RumourInfo> betterSellRumours)
        {
            if (betterSellRumours.Count > 0)
            {
                int count = 0;

                foreach (RumourInfo rumourInfo in betterSellRumours)
                {
                    count++;

                    if (count <= 8 || rumourInfo.isTradeDestination)
                    {
                        if (count > 8 && rumourInfo.isTradeDestination)
                        {
                            ShowNewLine(instance);
                        }
                        ShowRumourTooltip(instance, rumourInfo);
                    }
                }
                ShowNewLine(instance);
            }
            else if (!isLeftPanel)
            {
                ShowRumourText(instance, new TextObject("{=whqxpBwA}This is the best place to sell this item").ToString(), Color.FromUint(4278255360U));
                ShowNewLine(instance);
            }
        }

        private static List<RumourInfo> FilterBuyRumours(List<RumourInfo> rumours)
        {
            List<RumourInfo> result = new List<RumourInfo>(rumours.Count);
            rumours.Reverse();

            int count = 0;

            foreach (RumourInfo rumour in rumours)
            {
                count++;

                if (count <= 8 || rumour.isTradeDestination)
                {
                    result.Add(rumour);
                }
            }
            result.Reverse();

            return result;
        }

        private static void ShowRumourTooltip(ItemMenuVM instance, RumourInfo rumourInfo)
        {
            bool isTradeDestination = rumourInfo.isTradeDestination;
            Color color;

            if (isTradeDestination)
            {
                color = rumourInfo.isBestSell ? Color.FromUint(4294918143U) : Color.FromUint(2147418367U);
            }
            else
            {
                color = GetTooltipColour(rumourInfo);
            }

            CreateProperty(instance.TargetItemProperties,
                "",
                rumourInfo.text,
                0,
                color,
                null,
                TooltipProperty.TooltipPropertyFlags.None);

            if (instance.IsComparing)
            {
                CreateProperty(instance.ComparedItemProperties, "", "", 0, Colors.Black, null, TooltipProperty.TooltipPropertyFlags.None);
            }
        }

        private static Color GetTooltipColour(RumourInfo rumourInfo)
        {
            return Utilities.colourStyle == ColourStyle.PROFIT_PER_MILE ? GetProfitPerMileColour(rumourInfo) :
                Utilities.colourStyle == ColourStyle.PERCENTAGE_DIFFERENCE ? GetProfitPercentageDifferenceColour(rumourInfo) :
                Colors.White;
        }

        private static Color GetProfitPercentageDifferenceColour(RumourInfo rumourInfo)
        {
            uint colourCode = uint.MaxValue;
            uint intensity = Convert.ToUInt32(rumourInfo.percentageDifference) * 8U;
            uint b = Math.Min(intensity, 255U);

            colourCode -= b;
            if (intensity > 255U)
            {
                intensity -= 255U;
                uint g = Math.Min(intensity, 255U) << 16;
                colourCode -= g;
            }

            if (intensity > 255U)
            {
                intensity -= 255U;
                b = Math.Min(intensity, 255U);
                colourCode += b;
            }

            return Color.FromUint(colourCode);
        }

        private static Color GetProfitPerMileColour(RumourInfo rumourInfo)
        {
            uint colourCode = 805306367U;
            uint intensity = Convert.ToUInt32(rumourInfo.profitPerMile * 400f);
            uint b = Math.Min(intensity, 255U);

            colourCode -= b;
            uint opacity = Math.Min(intensity, 208U);
            colourCode += opacity << 24;

            if (intensity > 255U)
            {
                intensity -= 255U;
                uint g = Math.Min(intensity, 255U) << 16;
                colourCode -= g;
            }

            if (intensity > 255U)
            {
                intensity -= 255U;
                b = Math.Min(intensity, 255U);
                colourCode += b;
            }

            return Color.FromUint(colourCode);
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
                    rumourInfo.profitPerMile = ((float)priceData.sellPrice - currentSellPrice) / (float)distance;
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

        private static void ShowRumourText(ItemMenuVM instance, string text, Color color)
        {
            CreateProperty(instance.TargetItemProperties, "", text, 0, color, null, TooltipProperty.TooltipPropertyFlags.None);
        }

        private static void ShowNewLine(ItemMenuVM instance)
        {
            CreateProperty(instance.TargetItemProperties, "", "", 0, Colors.Black, null, TooltipProperty.TooltipPropertyFlags.None);
        }

        private static ItemMenuTooltipPropertyVM CreateProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetItemProperties,
            string def,
            string value,
            int textHeight,
            Color colour,
            HintViewModel? hintViewModel = null,
            TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
        {
            ItemMenuTooltipPropertyVM tooltipPropertyVM = new ItemMenuTooltipPropertyVM(def, value, textHeight, colour, false, hintViewModel, propertyFlags);
            targetItemProperties.Add(tooltipPropertyVM);

            return tooltipPropertyVM;
        }
    }
}
