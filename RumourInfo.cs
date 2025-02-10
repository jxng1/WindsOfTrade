namespace WindsOfTrade
{
    internal class RumourInfo
    {
        internal static RumourInfo Empty
        {
            get
            {
                return new RumourInfo();
            }
        }

        internal string text { get; set; }

        internal float profitPerMile { get; set; }

        internal float percentageDifference { get; set; }

        internal bool isBestSell { get; set; }

        internal bool isBestBuy { get; set; }

        // TODO: implement destination tracker too rather than just tracker radius
        internal bool isTradeDestination { get; set; }

        internal bool isEmpty
        {
            get
            {
                return string.IsNullOrEmpty(text);
            }
        }
    }
}