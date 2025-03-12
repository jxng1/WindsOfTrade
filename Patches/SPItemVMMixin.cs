using Bannerlord.UIExtenderEx.Attributes;
using System.Linq;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;

using WindsOfTrade.Behaviours;

using InventorySide = TaleWorlds.CampaignSystem.Inventory.InventoryLogic.InventorySide;

namespace WindsOfTrade.Patches
{
    // Credits to jzebedee for the original code
    [ViewModelMixin(nameof(SPItemVM.RefreshValues))]
    internal class SPItemVMMixin : TwoWayViewModelMixin<SPItemVM>
    {
        private static InventoryLogic _inventoryLogic => InventoryManager.InventoryLogic;

        private bool _shouldHighlightItem;
        private bool _isItemBadPrice;

        public SPItemVMMixin(SPItemVM vm) : base(vm) {}

        public override void OnRefresh()
        {
            Refresh();
            base.OnRefresh();
        }

        public void Refresh() {
            if (!IsValid() || !ShouldHighlightSide())
            {
                return;
            }

            if (GlobalTradeItemTrackerBehaviour.ItemDictionary.TryGetValue(ViewModel.StringId, out var itemData) && (itemData.itemObject.IsAnimal || itemData.itemObject.IsTradeGood)) 
            {
                ShouldHighlightItem = true;

                if (ViewModel.InventorySide == InventorySide.PlayerInventory) // Compares the market data to the player's inventory
                {
                    int buyPrice = _inventoryLogic.GetItemPrice(ViewModel.ItemRosterElement.EquipmentElement, true);
                    IsItemBadPrice = (ViewModel.ItemCost < buyPrice);
                }
                // TODO: Market data doesn't work as you're not able to check the player's inventory, so need to rework the logic on PriceData or somewhere else to accomodate this
                //} else if (ViewModel.InventorySide == InventorySide.OtherInventory) // Check market data
                //{
                //    int sell = _inventoryLogic.GetItemPrice(ViewModel.ItemRosterElement.EquipmentElement, false);
                //    if (ViewModel.ItemCost > sell)
                //    {
                //        IsItemBadPrice = true;
                //    } else
                //    {
                //        IsItemBadPrice = false;
                //    }
                //}
            } else
            {
                ShouldHighlightItem = false;
            }

            return;
        }

        private bool IsValid() => ViewModel
            switch
            {
                null => false,
                { IsEquipableItem: true } => false,
                { IsArtifact: true } => false,
                { InventorySide: not InventorySide.PlayerInventory and not InventorySide.OtherInventory } => false,
                _ => true
            };

        bool ShouldHighlightSide()
        {
            if (SubModule.highlightBetterOptions is not HighlightBetterOptions options || options.HighlightBetterItems == false)
            {
                return false;
            }

            if (ViewModel is not { InventorySide: var side })
            {
                return false;
            }

            return side switch
            {
                InventorySide.PlayerInventory => options.HighlightFromInventory,
                InventorySide.OtherInventory when InventoryManager.Instance is { CurrentMode: var mode } => ShouldHighlightOtherSide(options, mode),
                _ => true
            };

            static bool ShouldHighlightOtherSide(HighlightBetterOptions options, InventoryMode mode)
                => (options, mode) switch
                {
                    ({ HighlightFromDiscard: true }, InventoryMode.Default) => true,
                    ({ HighlightFromLoot: true }, InventoryMode.Loot) => true,
                    ({ HighlightFromStash: true }, InventoryMode.Stash) => true,
                    ({ HighlightFromTrade: true }, InventoryMode.Trade) => true,
                    _ => false
                };
        }

        [DataSourceProperty]
        public bool ShouldHighlightItem
        {
            get => _shouldHighlightItem;
            set
            {
                if (_shouldHighlightItem != value)
                {
                    _shouldHighlightItem = value;
                    OnPropertyChangedWithValue(value, "ShouldHighlightItem");
                }
            }
        }

        [DataSourceProperty]
        public bool IsItemBadPrice
        {
            get => _isItemBadPrice;
            set
            {
                if (_isItemBadPrice != value)
                {
                    _isItemBadPrice = value;
                    OnPropertyChangedWithValue(value, "IsItemBadPrice");
                }
            }
        }
    }
}
