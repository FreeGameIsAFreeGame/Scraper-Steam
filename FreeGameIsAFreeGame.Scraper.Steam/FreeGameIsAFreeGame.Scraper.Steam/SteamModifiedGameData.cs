using System.Collections.Generic;
using Newtonsoft.Json;

namespace FreeGameIsAFreeGame.Scraper.Steam
{
    public partial class SteamModifiedGamesData
    {
        [JsonProperty("response")]
        public Response Response { get; set; }
    }

    public partial class Response
    {
        [JsonProperty("apps")]
        public List<App> Apps { get; set; }
    }

    public partial class App
    {
        [JsonProperty("appid")]
        public long Appid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("last_modified")]
        public long LastModified { get; set; }

        [JsonProperty("price_change_number")]
        public long PriceChangeNumber { get; set; }
    }
}
