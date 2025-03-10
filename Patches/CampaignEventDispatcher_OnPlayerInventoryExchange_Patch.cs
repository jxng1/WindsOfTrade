using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;


namespace WindsOfTrade.Patches
{
    [HarmonyPatch(typeof(CampaignEventDispatcher), "OnPlayerInventoryExchange")]
    internal class CampaignEventDispatcher_OnPlayerInventoryExchange_Patch
    {
        private static Dictionary<string, ItemStock> _dictionary = new Dictionary<string, ItemStock>();

        public static void Postfix(List<ValueTuple<ItemRosterElement, int>> purchasedItems,
            List<ValueTuple<ItemRosterElement, int>> soldItems,
            bool isTrading)
        {
            if (isTrading)
            {
                foreach (ValueTuple<ItemRosterElement, int> item in purchasedItems)
                {
                    ItemRosterElement itemRosterElement = item.Item1;
                    string id = itemRosterElement.EquipmentElement.Item.StringId;
                    int quantity = itemRosterElement.Amount;
                    int value = item.Item2;

                    ItemStock itemStock = GetStock(id);
                    itemStock.currentAmountInStock += quantity;
                    itemStock.currentStockValue += value;
                    itemStock.totalAmountHeld += quantity;
                    itemStock.historicalTotalValue += value;
                    UpdateStock(id, itemStock);
                }

                foreach (ValueTuple<ItemRosterElement, int> item in soldItems)
                {
                    ItemRosterElement itemRosterElement = item.Item1;
                    string id = itemRosterElement.EquipmentElement.Item.StringId;
                    int quantity = itemRosterElement.Amount;
                    int value = item.Item2;

                    ItemStock itemStock = GetStock(id);
                    itemStock.currentAmountInStock -= quantity;
                    itemStock.currentStockValue -= value;

                    if (itemStock.currentAmountInStock <= 0 || itemStock.currentStockValue <= 0)
                    {
                        itemStock = default;
                    }
                    UpdateStock(id, itemStock);
                }
            }
        }

        public static string Serialise()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, ItemStock> keyValuePair in _dictionary)
            {
                ItemStock itemStock = keyValuePair.Value;
                stringBuilder.Append(string.Format("{0}, {1}, {2}, {3}, {4}", new object[]
                {
                    keyValuePair.Key,
                    itemStock.currentAmountInStock,
                    itemStock.currentStockValue,
                    itemStock.totalAmountHeld,
                    itemStock.historicalTotalValue
                }));
            }

            return stringBuilder.ToString();
        }
        public static void Deserialise(string values)
        {
            _dictionary.Clear();

            string[] strings = values.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in strings)
            {
                string[] subValues = s.Split(new char[] { ',' });

                if (subValues.Length == 3 || subValues.Length == 5)
                {
                    string id = subValues[0];
                    ItemStock itemStock = new ItemStock(Convert.ToInt32(subValues[1]), Convert.ToInt32(subValues[2]));

                    if (subValues.Length == 5)
                    {
                        itemStock.totalAmountHeld = Convert.ToInt32(subValues[3]); ;
                        itemStock.historicalTotalValue = Convert.ToInt32(subValues[4]); ;
                    }

                    _dictionary.Add(id, itemStock);
                }
            }
        }

        internal static void UpdateStock(string id, ItemStock itemStock)
        {
            _dictionary[id] = itemStock;
        }

        internal static ItemStock GetStock(string id)
        {
            ItemStock itemStock;
            ItemStock result;

            if (_dictionary.TryGetValue(id, out itemStock))
            {
                result = itemStock;
            }
            else
            {
                ItemStock newStock = default;
                _dictionary.Add(id, newStock);
                result = newStock;
            }

            return result;
        }

        internal static float GetStockUnitValue(string id)
        {
            ItemStock itemStock;

            if (_dictionary.TryGetValue(id, out itemStock))
            {
                if (itemStock.currentAmountInStock > 0)
                {
                    return (float)itemStock.currentStockValue / itemStock.currentAmountInStock;
                }
            }

            return -1.0f;
        }

        internal static float GetAveragePurchasePrice(string itemId)
        {
            ItemStock itemStock;

            if (_dictionary.TryGetValue(itemId, out itemStock))
            {
                if (itemStock.currentAmountInStock > 0)
                {
                    return (float)itemStock.historicalTotalValue / itemStock.totalAmountHeld;
                }
            }

            return -1.0f;
        }
    }
}