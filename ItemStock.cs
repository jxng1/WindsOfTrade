namespace WindsOfTrade
{
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