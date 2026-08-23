using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneNoteMomtion.Server.Commands;

namespace OneNoteMomtion.Server.Ui
{
    internal static class PaletteMessages
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static PaletteRequest? ParseRequest(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<PaletteRequest>(json, Options);
            }
            catch (JsonException ex)
            {
                EngineLog.Warn($"The palette sent an unreadable message: {ex.Message}");
                return null;
            }
        }

        public static string SerializeResponse(string? requestId, CommandResult result)
        {
            PaletteResponse response = new()
            {
                RequestId = requestId,
                Success = result.Success,
                Changed = result.Changed,
                Message = result.Message,
                Data = result.Data is IReadOnlyList<string> list ? list : result.Data,
            };

            return JsonSerializer.Serialize(response, Options);
        }

        public static string SerializeError(string? requestId, string message)
        {
            return SerializeResponse(requestId, CommandResult.Failed(message));
        }
    }
}
