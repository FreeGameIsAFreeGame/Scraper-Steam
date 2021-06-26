using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FreeGameIsAFreeGame.Scraper.Steam.Details
{
    using J = JsonPropertyAttribute;

    public partial class SteamAppDetails
    {
        [J("success")] public bool Success { get; set; }
        [J("data")] public Data Data { get; set; }
    }

    public class Data
    {
        [J("type")] public string Type { get; set; }
        [J("name")] public string Name { get; set; }
        [J("steam_appid")] public long SteamAppid { get; set; }
        [J("header_image")] public string HeaderImage { get; set; }
        [J("price_overview")] public PriceOverview PriceOverview { get; set; }
        [J("packages")] public List<long> Packages { get; set; }
        [J("package_groups")] public List<PackageGroup> PackageGroups { get; set; }
        [J("platforms")] public Platforms Platforms { get; set; }
    }

    public class Achievements
    {
        [J("total")] public long Total { get; set; }
        [J("highlighted")] public List<Highlighted> Highlighted { get; set; }
    }

    public class Highlighted
    {
        [J("name")] public string Name { get; set; }
        [J("path")] public Uri Path { get; set; }
    }

    public class Category
    {
        [J("id")] public long Id { get; set; }
        [J("description")] public string Description { get; set; }
    }

    public class ContentDescriptors
    {
        [J("ids")] public List<object> Ids { get; set; }
        [J("notes")] public object Notes { get; set; }
    }

    public class Genre
    {
        [J("id"), JsonConverter(typeof(PurpleParseStringConverter))]
        public long Id { get; set; }

        [J("description")] public string Description { get; set; }
    }

    public class CRequirements
    {
        [J("minimum")] public string Minimum { get; set; }
        [J("recommended")] public string Recommended { get; set; }
    }

    public class Metacritic
    {
        [J("score")] public long Score { get; set; }
        [J("url")] public Uri Url { get; set; }
    }

    public class Movie
    {
        [J("id")] public long Id { get; set; }
        [J("name")] public string Name { get; set; }
        [J("thumbnail")] public Uri Thumbnail { get; set; }
        [J("webm")] public Webm Webm { get; set; }
        [J("highlight")] public bool Highlight { get; set; }
    }

    public class Webm
    {
        [J("480")] public Uri The480 { get; set; }
        [J("max")] public Uri Max { get; set; }
    }

    public class PackageGroup
    {
        [J("name")] public string Name { get; set; }
        [J("title")] public string Title { get; set; }
        [J("description")] public string Description { get; set; }
        [J("selection_text")] public string SelectionText { get; set; }
        [J("save_text")] public string SaveText { get; set; }
        [J("display_type")] public long DisplayType { get; set; }

        [J("is_recurring_subscription"), JsonConverter(typeof(FluffyParseStringConverter))]
        public bool IsRecurringSubscription { get; set; }

        [J("subs")] public List<Sub> Subs { get; set; }
    }

    public class Sub
    {
        [J("packageid")] public long Packageid { get; set; }
        [J("percent_savings_text")] public string PercentSavingsText { get; set; }
        [J("percent_savings")] public long PercentSavings { get; set; }
        [J("option_text")] public string OptionText { get; set; }
        [J("option_description")] public string OptionDescription { get; set; }

        [J("can_get_free_license"), JsonConverter(typeof(PurpleParseStringConverter))]
        public long CanGetFreeLicense { get; set; }

        [J("is_free_license")] public bool IsFreeLicense { get; set; }
        [J("price_in_cents_with_discount")] public long PriceInCentsWithDiscount { get; set; }
    }

    public class Platforms
    {
        [J("windows")] public bool Windows { get; set; }
        [J("mac")] public bool Mac { get; set; }
        [J("linux")] public bool Linux { get; set; }
    }

    public class PriceOverview
    {
        [J("currency")] public string Currency { get; set; }
        [J("initial")] public long Initial { get; set; }
        [J("final")] public long Final { get; set; }
        [J("discount_percent")] public long DiscountPercent { get; set; }
        [J("initial_formatted")] public string InitialFormatted { get; set; }
        [J("final_formatted")] public string FinalFormatted { get; set; }
    }

    public class Recommendations
    {
        [J("total")] public long Total { get; set; }
    }

    public class ReleaseDate
    {
        [J("coming_soon")] public bool ComingSoon { get; set; }
        [J("date")] public string Date { get; set; }
    }

    public class Screenshot
    {
        [J("id")] public long Id { get; set; }
        [J("path_thumbnail")] public Uri PathThumbnail { get; set; }
        [J("path_full")] public Uri PathFull { get; set; }
    }

    public class SupportInfo
    {
        [J("url")] public Uri Url { get; set; }
        [J("email")] public string Email { get; set; }
    }

    public struct MacRequirements
    {
        public List<object> AnythingArray;
        public CRequirements CRequirements;

        public static implicit operator MacRequirements(List<object> AnythingArray)
        {
            return new MacRequirements {AnythingArray = AnythingArray};
        }

        public static implicit operator MacRequirements(CRequirements CRequirements)
        {
            return new MacRequirements {CRequirements = CRequirements};
        }
    }

    public partial class SteamAppDetails
    {
        public static Dictionary<string, SteamAppDetails> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, SteamAppDetails>>(json,
                Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this Dictionary<string, SteamAppDetails> self)
        {
            return JsonConvert.SerializeObject(self, Converter.Settings);
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                MacRequirementsConverter.Singleton,
                new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
            }
        };
    }

    internal class PurpleParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(long) || t == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            string value = serializer.Deserialize<string>(reader);
            long l;
            if (long.TryParse(value, out l))
            {
                return l;
            }

            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            long value = (long) untypedValue;
            serializer.Serialize(writer, value.ToString());
        }

        public static readonly PurpleParseStringConverter Singleton = new PurpleParseStringConverter();
    }

    internal class MacRequirementsConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(MacRequirements) || t == typeof(MacRequirements?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    CRequirements objectValue = serializer.Deserialize<CRequirements>(reader);
                    return new MacRequirements {CRequirements = objectValue};
                case JsonToken.StartArray:
                    List<object> arrayValue = serializer.Deserialize<List<object>>(reader);
                    return new MacRequirements {AnythingArray = arrayValue};
            }

            throw new Exception("Cannot unmarshal type MacRequirements");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            MacRequirements value = (MacRequirements) untypedValue;
            if (value.AnythingArray != null)
            {
                serializer.Serialize(writer, value.AnythingArray);
                return;
            }

            if (value.CRequirements != null)
            {
                serializer.Serialize(writer, value.CRequirements);
                return;
            }

            throw new Exception("Cannot marshal type MacRequirements");
        }

        public static readonly MacRequirementsConverter Singleton = new MacRequirementsConverter();
    }

    internal class FluffyParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(bool) || t == typeof(bool?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            string value = serializer.Deserialize<string>(reader);
            bool b;
            if (bool.TryParse(value, out b))
            {
                return b;
            }

            throw new Exception("Cannot unmarshal type bool");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            bool value = (bool) untypedValue;
            string boolString = value ? "true" : "false";
            serializer.Serialize(writer, boolString);
        }

        public static readonly FluffyParseStringConverter Singleton = new FluffyParseStringConverter();
    }

    internal class DecodingChoiceConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(long) || t == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                    long integerValue = serializer.Deserialize<long>(reader);
                    return integerValue;
                case JsonToken.String:
                case JsonToken.Date:
                    string stringValue = serializer.Deserialize<string>(reader);
                    long l;
                    if (long.TryParse(stringValue, out l))
                    {
                        return l;
                    }

                    break;
            }

            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            long value = (long) untypedValue;
            serializer.Serialize(writer, value);
        }

        public static readonly DecodingChoiceConverter Singleton = new DecodingChoiceConverter();
    }
}
