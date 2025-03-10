using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

using WindsOfTrade.Patches;

namespace WindsOfTrade
{
    internal static class SPItemVMExtensions
    {
        internal static SPItemVMMixin? GetMixinForVM(this SPItemVM vm)
        {
            if (vm is null) { return null; }

            var mixinRef = TwoWayViewModelMixin<SPItemVM>.GetVmMixin(vm);
            if (!mixinRef.TryGetTarget(out var mixinBase) || mixinBase is not SPItemVMMixin mixinVM)
            {
                return null;
            }

            return mixinVM;
        }
    }
}
