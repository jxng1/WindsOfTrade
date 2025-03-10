using Bannerlord.UIExtenderEx.Attributes;

using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Library;

using WindsOfTrade.Behaviours;

using InventorySide = TaleWorlds.CampaignSystem.Inventory.InventoryLogic.InventorySide;

namespace WindsOfTrade.Patches
{
    // Credits to jzebedee for the original code
    [ViewModelMixin(nameof(SPItemVM.RefreshValues))]
    internal class SPItemVMMixin : TwoWayViewModelMixin<SPItemVM>
    {
        private bool _shouldHighlightItem;

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

            ShouldHighlightItem = GlobalTradeItemTrackerBehaviour.ItemDictionary.TryGetValue(ViewModel.StringId, out var itemData) &&
                (itemData.buyRumourList.Count > 0 || itemData.sellRumourList.Count > 0);
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
    }
}
