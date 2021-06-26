namespace FreeGameIsAFreeGame.Scraper.Steam
{
    public partial class SteamScraper
    {
        private class PackageKeys
        {
            public PackageKeys(uint packageId, uint filteredAppId)
            {
                PackageId = packageId;
                FilteredAppId = filteredAppId;
            }

            public uint PackageId { get; }
            public uint FilteredAppId { get; }
        }
    }
}
