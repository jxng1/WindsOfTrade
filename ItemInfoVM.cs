using HarmonyLib.BUTR.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using WindsOfTrade.Behaviours;
using WindsOfTrade.Patches;

namespace WindsOfTrade
{
    internal static class ItemInfoVM
    {
        private static InventoryLogic _inventoryLogic => InventoryManager.InventoryLogic;

        internal static void ShowTooltips(ItemMenuVM instance)
        {
            if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
            {
                IMarketData? marketData = _inventoryLogic?.MarketData;

                if (marketData == null)
                {
                    Utilities.Log("Failed to show tooltips: marketData is null", LogLevel.ERROR);
                    return;
                }

                bool isLeftPanel = instance.IsPlayerItem;
                Settlement? currentSettlement = _inventoryLogic?.OtherParty == null ? null : _inventoryLogic.OtherParty.Settlement;
                ItemVM? targetItemVM = AccessTools2.Field(typeof(ItemMenuVM), "_targetItem")?.GetValue(instance) as ItemVM;

                if (targetItemVM == null)
                {
                    Utilities.Log("Failed to show tooltips: targetItemVM is null", LogLevel.ERROR);
                    return;
                }

                EquipmentElement element = targetItemVM.ItemRosterElement.EquipmentElement;

                int marketBuyPrice = marketData.GetPrice(element.Item, MobileParty.MainParty, false, _inventoryLogic?.OtherParty);
                int marketSellPrice = marketData.GetPrice(element.Item, MobileParty.MainParty, true, _inventoryLogic?.OtherParty);

                if (isLeftPanel && (element.Item.IsTradeGood || element.Item.IsAnimal))
                {
                    ItemRosterElement? item = _inventoryLogic?.FindItemFromSide(InventoryLogic.InventorySide.PlayerInventory, element);
                    int itemAmount = item != null ? item.GetValueOrDefault().Amount : 0;
                    ShowNewLine(instance);

                    MBTextManager.SetTextVariable("AMOUNT", itemAmount);

                    ShowRumourText(instance, new TextObject("{=VKkiPo9W}You have {AMOUNT}").ToString(), Color.ConvertStringToColor("#FAFAFAFF"));
                }

                string itemId = element.Item.StringId;
                if (!isLeftPanel)
                {
                    float stockUnitValue = CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.GetStockUnitValue(itemId);

                    // If item is in stock
                    if (stockUnitValue > 0.0f)
                    {
                        string sellHereText = new TextObject("{=hve9LACb}Sell here to break even").ToString();
                        Color textColor = Colors.Yellow;

                        // If selling here is at least at profit threshold
                        if (marketSellPrice > stockUnitValue)
                        {
                            float profit = (marketSellPrice - stockUnitValue) / marketSellPrice * 100.0f;

                            if (profit >= 0.5f) // TODO: pull profit threshold(%) from config
                            {
                                MBTextManager.SetTextVariable("PROFIT", profit);
                                sellHereText = new TextObject("{=caaVIYnm}Sell here for {PROFIT}% profit").ToString();
                                textColor = Color.FromUint(3745513216U);
                            }
                        }
                        else if (marketSellPrice < stockUnitValue) // If selling here nets in loss
                        {
                            float loss = (stockUnitValue - marketSellPrice) / stockUnitValue * 100.0f;

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
                    float averagePurchasePrice = CampaignEventDispatcher_OnPlayerInventoryExchange_Patch.GetAveragePurchasePrice(itemId);

                    if (averagePurchasePrice > 0.0f)
                    {
                        string buyHereText = new TextObject("{=oVZwPLij}About the usual price").ToString();
                        Color textColor = Colors.Yellow;

                        if (marketBuyPrice > averagePurchasePrice)
                        {
                            float extra = (marketBuyPrice - averagePurchasePrice) / (marketBuyPrice * 100.0f);
                            if (extra >= 1.0f) // TODO: pull extra cost(from average) threshold(%) from config
                            {
                                MBTextManager.SetTextVariable("MORE_EXPENSIVE", extra);
                                buyHereText = new TextObject("{=AbPnZRIw}{MORE_EXPENSIVE}% more expensive than usual").ToString();
                                textColor = Colors.Red;
                            }
                        }
                        else if (marketBuyPrice < averagePurchasePrice)
                        {
                            float less = (averagePurchasePrice - marketBuyPrice) / (averagePurchasePrice * 100.0f);
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

                if (GlobalTradeItemTrackerBehaviour.ItemDictionary.TryGetValue(itemId, out var itemData)
                    && (GlobalTradeItemTrackerBehaviour.ItemDictionary[itemId].buyRumourList.Count > 0 || GlobalTradeItemTrackerBehaviour.ItemDictionary[itemId].sellRumourList.Count > 0))
                {
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
                    ShowSellRumours(instance, isLeftPanel, itemData.sellRumourList);
                    ShowBuyRumours(instance, isLeftPanel, itemData.buyRumourList);
                }
            }
        }

        private static void ShowBuyRumours(ItemMenuVM instance, bool isLeftPanel, List<RumourInfo> betterBuyRumours)
        {
            if (betterBuyRumours.Count > 0)
            {
                betterBuyRumours = FilterRumours(betterBuyRumours);

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

                betterSellRumours.Reverse();

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

        private static List<RumourInfo> FilterRumours(List<RumourInfo> rumours)
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
                rumourInfo.text != null ? rumourInfo.text : "",
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
