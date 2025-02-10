using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;


namespace WindsOfTrade
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Harmony Harmony = new Harmony("winds_of_trade_1.12.2.1");
            Harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            // Super method call
            base.OnGameStart(game, gameStarterObject);

            // Read config
            // TODO: implement config
            //Utilities.ReadConfig();

            // Add behaviour to game starter
            try
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                if (campaignGameStarter != null)
                {
                    campaignGameStarter.AddBehavior(new PriceTrackBehaviour());
                    Utilities.Log("Winds of Trade loaded successfully", LogLevel.SUCCESS);
                }
                else
                {
                    Utilities.Log("Failed to load Winds of Trade: GameStarterObject is null", LogLevel.ERROR);
                }
            }
            catch (Exception e)
            {
                Utilities.Log("Failed to load Winds of Trade: " + e.Message, LogLevel.ERROR);
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            // Super method call
            base.OnApplicationTick(dt);

            // TODO: allow keybind changes
            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
            {
                bool radiusChanged = false;
                if (Input.IsKeyDown(InputKey.Equals))
                {
                    TrackerRadius.Increase();
                    radiusChanged = true;
                }
                else if (Input.IsKeyDown(InputKey.Minus))
                {
                    TrackerRadius.Decrease();
                    radiusChanged = true;
                }

                if (radiusChanged)
                {
                    Utilities.Log(String.Format("Tracker radius: {0}", TrackerRadius.radius), LogLevel.LOG);

                    // Update prices
                    MobileParty mainParty = MobileParty.MainParty;
                    PriceTrackBehaviour.UpdatePrices(mainParty, mainParty.CurrentSettlement);
                }

                // TODO: Implement destination tracker
            }
        }
    }

    [HarmonyPatch(typeof(CampaignEvents), "OnAfterSettlementEntered")]
    internal class CampaignEvents_OnAfterSettlementEntered
    {
        public static void Postfix(MobileParty party, Settlement settlement)
        {
            if (party == MobileParty.MainParty)
            {
                PriceTrackBehaviour.UpdatePrices(party, settlement);
            }
        }
    }

    [HarmonyPatch(typeof(Campaign), "OnDataLoadFinished")]
    internal class CampaignEvents_OnDataLoadFinished
    {
        public static void Postfix()
        {
            PriceTrackBehaviour.UpdatePrices(); // TODO: figure out a better way to do this
        }
    }

    internal static class TrackerRadius
    {
        // TODO: read from config
        internal static float radius { get; set; } = 250.0f;

        internal static void Increase()
        {
            if (radius < 250.0f)
            {
                radius += 25.0f;
            }
        }

        internal static void Decrease()
        {
            if (radius > 25.0f)
            {
                radius -= 25.0f;
            }
        }
    }

    [HarmonyPatch(typeof(ItemMenuVM), "SetMerchandiseComponentTooltip")]
    internal static class ItemMenuVM_SetMerchandiseComponentTooltip
    {
        public static bool Prefix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);

            // Don't run original code so return false, skipping everything
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemMenuVM), "SetHorseComponentTooltip")]
    internal static class ItemMenuVM_SetHorseComponentTooltip
    {
        public static void Postfix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemMenuVM), "SetWeaponComponentTooltip")]
    internal static class ItemMenuVM_SetWeaponComponentTooltip
    {
        public static void Postfix(ItemMenuVM __instance)
        {
            ItemInfoVM.ShowTooltips(__instance);
        }
    }

    internal struct ItemStock
    {
        internal int currentAmountInStock { get; set; }
        internal int currentStockValue { get; set; }
        internal int totalAmountHeld { get; set; }
        internal int historicalTotalValue { get; set; }

        internal ItemStock(int quantity, int value)
        {
            currentAmountInStock = quantity;
            currentStockValue = value;
            totalAmountHeld = 0;
            historicalTotalValue = 0;
        }

        internal ItemStock(int quantity, int value, int totalAmount, int historicalValue)
        {
            currentAmountInStock = quantity;
            currentStockValue = value;
            totalAmountHeld = totalAmount;
            historicalTotalValue = historicalValue;
        }
    }
}