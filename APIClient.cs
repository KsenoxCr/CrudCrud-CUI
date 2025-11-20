using System.Text;

namespace CrudCrudCUI;

class APIClient
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "DELETE"
    };

    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> HTTPRequest(string url, string method, string? resourceID = null, string? payload = null)
    {
        if (!AllowedMethods.Contains(method))
            throw new ArgumentOutOfRangeException($"Virheellinen HTTP metodi: {method}\n(Sallitut metodit: {string.Join(", ", AllowedMethods)})");

        if ((string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase)) && payload != null)
            throw new InvalidOperationException("GET ja DELETE -pyynnöissä ei voi olla sisältöä (payload)");

        if ((string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)) && resourceID != null)
            throw new InvalidOperationException("GET ja POST -pyynnöissä ei voi olla resurssitunnusta (resourceID)");

        if ((string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(resourceID))
            throw new InvalidOperationException("PUT ja DELETE -pyynnöissä on oltava resurssitunnus (resourceID)");

        if ((string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("POST ja PUT -pyynnöissä on oltava sisältö (payload)");

        if (resourceID != null)
            url += $"/{resourceID}";

        HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), url);

        if (payload != null)
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            throw new HttpRequestException($"Pyyntö osoitteeseen {url} epäonnistui: {e.StatusCode}");
        }

        if (response.Content == null)
            throw new InvalidOperationException("Vastauksella ei ollut sisältöä");

        string responseBody = await response.Content.ReadAsStringAsync();

        // TODO: Ask user to paste endpoint and resource name before other actions and throw clarifying exception when endpoint has reached its request limit

        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(responseBody))
            throw new InvalidOperationException("Vastauksen sisältö on tyhjä");

        return responseBody;
    }
}
