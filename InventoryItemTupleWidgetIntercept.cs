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

        private Brush? _betterItemHighlightBrush;
        private bool _shouldHighlightItem;

        public InventoryItemTupleWidgetIntercept(UIContext context) : base(context) { }

        [Editor(false)]
        public Brush? BetterItemHighlightBrush
        {
            get
            {
                if (_betterItemHighlightBrush == null)
                {
                    _betterItemHighlightBrush = DefaultBrush.Clone();
                    _betterItemHighlightBrush.DefaultLayer.Color = Color.FromUint(4286578559U);
                }

                return _betterItemHighlightBrush;
            }
            set
            {
                if (_betterItemHighlightBrush != value)
                {
                    _betterItemHighlightBrush = value;
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
    }
}
