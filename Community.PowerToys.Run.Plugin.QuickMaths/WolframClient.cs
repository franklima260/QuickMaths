using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.QuickMaths
{
    public class WolframClient : IWolframClient
    {
        private readonly HttpClient _httpClient;
        private string? _appId;

        public WolframClient() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }) { }

        internal WolframClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void UpdateAppId(string appId)
        {
            _appId = appId;
            PluginLogger.Debug("App ID " + (string.IsNullOrWhiteSpace(appId) ? "cleared" : "set") + ".");
        }

        /// <summary>
        /// Normalises a query before sending it to Wolfram Alpha.
        /// Prepends '0' to any bare leading decimal point (e.g. ".5*2" → "0.5*2")
        /// so Wolfram does not interpret the dot as a punctuation mark.
        /// </summary>
        internal static string NormalizeQuery(string query)
        {
            // Replace every occurrence of a decimal point that is preceded by a
            // non-digit (or is at the start of the string) with "0.".
            // Examples:  .5*2  → 0.5*2
            //            1+.5  → 1+0.5
            //            .5+.3 → 0.5+0.3
            return System.Text.RegularExpressions.Regex.Replace(
                query,
                @"(?<![0-9])\.(?=[0-9])",
                "0.");
        }

        public async Task<string> QueryAsync(string query, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_appId))
            {
                PluginLogger.Warn("QueryAsync called but App ID is not set.");
                return "Error: Wolfram Alpha App ID is not configured.";
            }

            var normalized = NormalizeQuery(query);
            if (normalized != query)
                PluginLogger.Debug($"Query normalized: '{query}' → '{normalized}'");
            query = normalized;

            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://api.wolframalpha.com/v1/result?appid={_appId}&i={encodedQuery}";
            PluginLogger.Debug("GET " + url.Replace(_appId, "***"));

            try
            {
                using var response = await _httpClient.GetAsync(url, token);
                PluginLogger.Info($"Response status: {response.StatusCode} ({(int)response.StatusCode})");

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(token);
                    PluginLogger.Debug("Response body: " + body);
                    return body;
                }

                var errorBody = await response.Content.ReadAsStringAsync(token);
                PluginLogger.Warn($"Non-success response. Status={response.StatusCode} Body={errorBody}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotImplemented ||
                    response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return "No short answer available.";
                }

                return $"API Error: {response.StatusCode}";
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                PluginLogger.Warn($"Request timed out for query={query}.");
                return "Request timed out.";
            }
            catch (OperationCanceledException)
            {
                PluginLogger.Warn($"Request cancelled for query={query}.");
                throw;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"Unexpected error for query={query}.", ex);
                return $"Request failed: {ex.Message}";
            }
        }

        public void Dispose()
        {
            PluginLogger.Debug("Disposing HttpClient.");
            _httpClient?.Dispose();
        }
    }
}
