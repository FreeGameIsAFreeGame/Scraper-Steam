using System.Collections.Generic;
using FreeGameIsAFreeGame.Core.Models;

namespace FreeGameIsAFreeGame.Scraper.Steam
{
    public partial class SteamScraper
    {
        private class BaseDeals
        {
            public BaseDeals(Dictionary<uint, Deal> idToDeal, List<PackageKeys> packages)
            {
                IdToDeal = idToDeal;
                Packages = packages;
            }

            public Dictionary<uint, Deal> IdToDeal { get; }
            public List<PackageKeys> Packages { get; }
        }
    }
}
