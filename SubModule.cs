using System.IO;
using System.Reflection;

using HarmonyLib;

using Bannerlord.UIExtenderEx;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

using WindsOfTrade.Behaviours;

namespace WindsOfTrade
{
    public class SubModule : MBSubModuleBase
    {
        public static string? ModuleDirectory => Path.GetDirectoryName(typeof(SubModule).Assembly.Location);

        internal static GlobalTradeItemTrackerBehaviour? globalTradeItemTrackerBehaviour;

        internal static HighlightBetterOptions highlightBetterOptions = new HighlightBetterOptions();

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            UIExtender uiExtender = UIExtender.Create(nameof(WindsOfTrade));
            uiExtender.Register(typeof(SubModule).Assembly);
            uiExtender.Enable();

            Harmony harmony = new Harmony("winds_of_trade_1.12.2.1_harmony");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            // Super method call
            if (gameStarter is not CampaignGameStarter campaignGameStarter)
            {
                return;
            }


            // Read config
            // TODO: implement config
            //Utilities.ReadConfig();

            // Add behaviours to game starter
            campaignGameStarter.AddBehavior(globalTradeItemTrackerBehaviour = new GlobalTradeItemTrackerBehaviour());

            Utilities.Log("Winds of Trade loaded successfully", LogLevel.SUCCESS);
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
                    Utilities.Log(string.Format("Tracker radius: {0}", TrackerRadius.radius), LogLevel.LOG);

                    // Update prices
                    MobileParty mainParty = MobileParty.MainParty;
                    globalTradeItemTrackerBehaviour?.UpdatePrices(mainParty, mainParty.CurrentSettlement);
                }

                // TODO: Implement destination tracker
            }
        }

        public override void OnGameEnd(Game game)
        {
            globalTradeItemTrackerBehaviour?.Dispose();
            globalTradeItemTrackerBehaviour = null;
        }
    }
}