using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceFlow.IntegrationTests.Api;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
