using System.Text;

namespace PicoCompanion.Core.Protocol;

public static class PicoResponse
{
    public static bool IsOk(string response) =>
        response.StartsWith("OK", StringComparison.OrdinalIgnoreCase);

    public static string DecodePayload(string response)
    {
        if (!IsOk(response))
        {
            return response;
        }

        var separator = response.IndexOf(' ', StringComparison.Ordinal);
        if (separator < 0 || separator == response.Length - 1)
        {
            return string.Empty;
        }

        var payload = response[(separator + 1)..].Trim();
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }
}
