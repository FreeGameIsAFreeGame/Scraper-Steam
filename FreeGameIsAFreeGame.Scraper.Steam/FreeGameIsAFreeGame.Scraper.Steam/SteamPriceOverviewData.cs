using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FreeGameIsAFreeGame.Scraper.Steam.Overview
{
    using J = JsonPropertyAttribute;
    using N = NullValueHandling;

    public partial class SteamPriceOverviewData
    {
        [J("success")] public bool Success { get; set; }

        [J("data", NullValueHandling = N.Ignore)]
        public DataUnion? Data { get; set; }
    }

    public class DataClass
    {
        [J("price_overview")] public PriceOverview PriceOverview { get; set; }
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

    public struct DataUnion
    {
        public List<object> AnythingArray;
        public DataClass DataClass;

        public static implicit operator DataUnion(List<object> AnythingArray)
        {
            return new DataUnion {AnythingArray = AnythingArray};
        }

        public static implicit operator DataUnion(DataClass DataClass)
        {
            return new DataUnion {DataClass = DataClass};
        }
    }

    public partial class SteamPriceOverviewData
    {
        public static Dictionary<string, SteamPriceOverviewData> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, SteamPriceOverviewData>>(json,
                Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this Dictionary<string, SteamPriceOverviewData> self)
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
                DataUnionConverter.Singleton,
                new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
            }
        };
    }

    internal class DataUnionConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(DataUnion) || t == typeof(DataUnion?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    DataClass objectValue = serializer.Deserialize<DataClass>(reader);
                    return new DataUnion {DataClass = objectValue};
                case JsonToken.StartArray:
                    List<object> arrayValue = serializer.Deserialize<List<object>>(reader);
                    return new DataUnion {AnythingArray = arrayValue};
            }

            throw new Exception("Cannot unmarshal type DataUnion");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            DataUnion value = (DataUnion) untypedValue;
            if (value.AnythingArray != null)
            {
                serializer.Serialize(writer, value.AnythingArray);
                return;
            }

            if (value.DataClass != null)
            {
                serializer.Serialize(writer, value.DataClass);
                return;
            }

            throw new Exception("Cannot marshal type DataUnion");
        }

        public static readonly DataUnionConverter Singleton = new DataUnionConverter();
    }
}
