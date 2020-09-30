using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using FreeGameIsAFreeGame.Core;
using FreeGameIsAFreeGame.Core.Models;
using FreeGameIsAFreeGame.Scraper.Steam.Details;
using FreeGameIsAFreeGame.Scraper.Steam.Overview;
using Newtonsoft.Json;
using NLog;
using SteamKit2;
using PriceOverview = FreeGameIsAFreeGame.Scraper.Steam.Overview.PriceOverview;

namespace FreeGameIsAFreeGame.Scraper.Steam
{
    public class SteamScraper : IScraper
    {
        private readonly string ENV_KEY = "STEAM_API_KEY";

        private readonly IBrowsingContext context;
        private readonly ILogger logger;

        string IScraper.Identifier => "SteamFree";
        string IScraper.DisplayName => "Steam";

        private DateTimeOffset? lastScrapeStamp = null;
        private string apiKey = "";
        private TaskCompletionSource<bool?> steamClientConnected = new TaskCompletionSource<bool?>(null);
        private bool handledLogin;

        private SteamClient client;
        private SteamApps apps;
        private SteamUser user;
        private CallbackManager manager;

        public SteamScraper()
        {
            context = BrowsingContext.New(Configuration.Default
                .WithDefaultLoader()
                .WithDefaultCookies());

            logger = LogManager.GetLogger(GetType().FullName);
        }

        Task<IEnumerable<IDeal>> IScraper.Scrape(CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        private DateTimeOffset GetTimeFromRow(IHtmlTableRowElement row)
        {
            IElement italicElement = row.Cells[1].QuerySelector(".muted");
            string text = italicElement.TextContent;
            text = text.Replace("(", string.Empty).Replace(")", string.Empty);
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(text));
        }

        private void EnsureVariables()
        {
            IDictionary variables = Environment.GetEnvironmentVariables();
            if (!variables.Contains(ENV_KEY))
                throw new Exception($"The required environment variable {ENV_KEY} is missing!");

            apiKey = (string) variables[ENV_KEY];
        }

        private async Task<List<long>> GetModifiedGames(CancellationToken token)
        {
            logger.Info("Getting modified games since {time}", lastScrapeStamp);
            long epoch = lastScrapeStamp.Value.ToUnixTimeSeconds();
            Url url = Url.Create(
                $"https://api.steampowered.com/IStoreService/GetAppList/v1/?key={apiKey}&if_modified_since={epoch}&include_games=1");
            DocumentRequest request = DocumentRequest.Get(url);
            IDocument response = await context.OpenAsync(request, cancel: token);
            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            string json = response.Body.Text();
            SteamModifiedGamesData modifiedGamesData = JsonConvert.DeserializeObject<SteamModifiedGamesData>(json);

            List<long> appIds = modifiedGamesData.Response.Apps?.Where(x => x.PriceChangeNumber > 0)
                .Select(x => x.Appid)
                .ToList() ?? new List<long>();

            return appIds;
        }

        private async Task<Dictionary<uint, int>> FilterAppIds(List<long> appIds, CancellationToken token)
        {
            logger.Info("Filtering app ids");
            Dictionary<uint, int> filteredAppIds = new Dictionary<uint, int>();

            for (int i = 0; i < appIds.Count; i += 100)
            {
                await Task.Delay(1500, token);

                int max = 0;

                if (i + 100 < appIds.Count)
                    max = 100;
                else
                    max = appIds.Count - i;

                logger.Debug("Start: {start}; Range: {range}", i, max);
                List<long> range = appIds.GetRange(i, max);
                string joined = string.Join(",", range);
                Url url = Url.Create(
                    $"https://store.steampowered.com/api/appdetails?key={apiKey}&filters=price_overview&appids={joined}");
                DocumentRequest request = DocumentRequest.Get(url);
                IDocument response = await context.OpenAsync(request, token);
                string json = response.Body.Text();
                Dictionary<string, SteamPriceOverviewData> data = SteamPriceOverviewData.FromJson(json);
                foreach (KeyValuePair<string, SteamPriceOverviewData> kvp in data)
                {
                    if (!kvp.Value.Success)
                        continue;

                    if (kvp.Value.Data?.DataClass?.PriceOverview == null)
                        continue;

                    PriceOverview priceOverview = kvp.Value.Data.Value.DataClass.PriceOverview;

                    if (priceOverview.DiscountPercent != 100)
                        continue;

                    uint parsedId = uint.Parse(kvp.Key);
                    filteredAppIds.Add(parsedId, (int) priceOverview.DiscountPercent);
                }
            }

            return filteredAppIds;
        }

        private async Task<(Dictionary<uint, Deal> idToDeal, List<uint> packages)> CreateBaseDeals(Dictionary<uint, int> filteredAppIds)
        {
            logger.Info("Creating base deal data");
            Dictionary<uint, Deal> idToDeal = new Dictionary<uint, Deal>();
            List<uint> packages = new List<uint>();

            foreach (KeyValuePair<uint, int> filteredAppId in filteredAppIds)
            {
                Dictionary<string, SteamAppDetails> steamAppDetails = await GetAppDetails(filteredAppId.Key);
                string key = filteredAppId.Key.ToString();

                if (!steamAppDetails.TryGetValue(key, out SteamAppDetails appDetails))
                    continue;
                if (!appDetails.Success)
                    continue;

                Deal deal = new Deal()
                {
                    Discount = filteredAppId.Value,
                    Image = appDetails.Data.HeaderImage,
                    Link = $"https://store.steampowered.com/app/{filteredAppId.Key}",
                    Title = appDetails.Data.Name
                };

                idToDeal.Add(filteredAppId.Key, deal);
                packages.AddRange(appDetails.Data.Packages.Select(x => (uint) x));
            }

            return (idToDeal, packages);
        }

        private async Task<Dictionary<string, SteamAppDetails>> GetAppDetails(uint appId)
        {
            Url url = Url.Create($"https://store.steampowered.com/api/appdetails?key={apiKey}&appids={appId}");
            DocumentRequest request = DocumentRequest.Get(url);
            IDocument response = await context.OpenAsync(request);
            string content = response.Body.Text();
            Dictionary<string, SteamAppDetails> steamAppDetails = SteamAppDetails.FromJson(content);
            return steamAppDetails;
        }

        private async Task<bool> ConnectToSteam()
        {
            logger.Info("Connecting to Steam");
            EnableSteamLogger();

            SteamConfiguration config = SteamConfiguration.Create(CreateSteamConfiguration);
            client = new SteamClient(config);

            apps = client.GetHandler<SteamApps>();
            user = client.GetHandler<SteamUser>();

            CreateSteamCallbacks();

            client.Connect();

            while (!handledLogin)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }

            bool? isConnected = await steamClientConnected.Task;

            return isConnected.HasValue && isConnected.Value;
        }

        private void EnableSteamLogger()
        {
            DebugLog.AddListener(SteamLogCallback);
            DebugLog.Enabled = true;
        }

        private void SteamLogCallback(string category, string message)
        {
            logger.Debug("Steam|{Category}|{Message}", category, message);
        }

        private void CreateSteamConfiguration(ISteamConfigurationBuilder builder)
        {
            builder.WithWebAPIKey(apiKey);
        }

        private void CreateSteamCallbacks()
        {
            if (manager != null)
                return;

            manager = new CallbackManager(client);
            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback obj)
        {
            logger.Warn("Steam user logged off with result {result}", obj.Result);
            logger.Warn("Client is connected: {isConnected}", client.IsConnected);
            if (client.IsConnected)
                user.LogOnAnonymous();
        }

        private void OnConnected(SteamClient.ConnectedCallback obj)
        {
            logger.Info("Client is connected, logging on as anonymous user");
            user.LogOnAnonymous();
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback obj)
        {
            handledLogin = true;

            if (obj.Result != EResult.OK)
            {
                logger.Error("Unable to log in; {status}", obj.Result);
                steamClientConnected.SetResult(false);
                return;
            }

            logger.Info("Successfully logged in");
            steamClientConnected.SetResult(true);
        }
    }
}
