using HarmonyLib.BUTR.Extensions;
using System;

using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

namespace WindsOfTrade
{
    internal class InventoryItemTupleWidgetIntercept : InventoryItemTupleWidget
    {
        private static readonly Action<InventoryItemTupleWidget>? UpdateCivilianState =
            AccessTools2.GetDelegate<Action<InventoryItemTupleWidget>>(typeof(InventoryItemTupleWidget), nameof(UpdateCivilianState));

        private Brush? _itemHighlightBrush;
        private bool _shouldHighlightItem;
        private bool _isItemBadPrice;

        public InventoryItemTupleWidgetIntercept(UIContext context) : base(context) { }

        [Editor(false)]
        public Brush? ItemHighlightBrush
        {
            get
            {
                return _itemHighlightBrush;
            }
            set
            {
                if (_itemHighlightBrush != value)
                {
                    _itemHighlightBrush = value;
                    OnPropertyChanged(value);
                }
            }
        }

        [Editor(false)]
        public bool ShouldHighlightItem
        {
            get => _shouldHighlightItem;
            set
            {
                if (_shouldHighlightItem != value)
                {
                    _shouldHighlightItem = value;
                    OnPropertyChanged(value);
                    UpdateCivilianState?.Invoke(this);
                }
            }
        }

        [Editor(false)]
        public bool IsItemBadPrice
        {
            get => _isItemBadPrice;
            set
            {
                if (_isItemBadPrice != value)
                {
                    _isItemBadPrice = value;
                    OnPropertyChanged(value);
                    UpdateCivilianState?.Invoke(this);
                }
            }
        }
    }
}
