namespace WindsOfTrade
{
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
}