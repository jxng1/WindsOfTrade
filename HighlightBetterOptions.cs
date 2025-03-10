namespace WindsOfTrade
{
    // Credits to jzebedee for the original code
    public record class HighlightBetterOptions
    {
        public bool HighlightBetterItems { get; set; } = true;
        public bool HighlightFromLoot { get; set; } = true;
        public bool HighlightFromStash { get; set; } = true;
        public bool HighlightFromInventory { get; set; } = true;
        public bool HighlightFromDiscard { get; set; } = true;
        public bool HighlightFromTrade { get; set; } = true;
    }
}
