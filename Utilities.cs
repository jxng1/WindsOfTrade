using System;
using System.IO;
using System.Xml;

using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

using MathF = TaleWorlds.Library.MathF;

namespace WindsOfTrade
{
    internal static class Utilities
    {
        internal static ColourStyle colourStyle = ColourStyle.PERCENTAGE_DIFFERENCE;

        internal static void ReadConfig()
        {
            try
            {
                string fileName = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\Modules\WindsOfTrade", "config.xml");
                bool flag = !File.Exists(fileName);

                if (!flag)
                {
                    // Create new config file
                    XmlDocument document = new XmlDocument();
                    document.Load(fileName);
                    XmlNode root = document.DocumentElement;

                    if (root == null || root.Name != "winds_of_trade")
                    {
                        Log("Winds of Trade config.xml is invalid", LogLevel.ERROR);
                    }
                    else
                    {
                        // TODO: Imeplemnt config
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.Message, LogLevel.ERROR);
            }
        }

        public static void Log(string s, LogLevel level)
        {
            LogToGame(s, level);
            // TODO: unimplemented
            //LogToFile(s);
        }

        public static void LogToGame(string s, LogLevel level)
        {
            InformationManager.DisplayMessage(new InformationMessage(s,
                level == LogLevel.LOG ? Colors.Blue :
                level == LogLevel.SUCCESS ? Colors.Green :
                Colors.Red));
        }

        public static void LogToFile(string s)
        {
            // TODO: unimplemented
        }

        public static int CalculateIntDistanceBetweenMainPartyAndSettlement(Settlement settlement)
        {
            return MathF.Round(MobileParty.MainParty.GetPosition2D.Distance(settlement.GetPosition2D));
        }

        public static float CalculateFloatDistanceBetweenPartyAndSettlement(Settlement settlement, MobileParty party)
        {
            return party.GetPosition2D.Distance(settlement.GetPosition2D);
        }
    }

    internal enum LogLevel
    {
        LOG,
        SUCCESS,
        ERROR
    }

    internal enum ColourStyle
    {
        PROFIT_PER_MILE,
        PERCENTAGE_DIFFERENCE,
        POTENTIAL_PROFIT
    }
}
