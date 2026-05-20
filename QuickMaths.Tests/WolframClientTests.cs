using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.QuickMaths;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickMaths.Tests
{
    [TestClass]
    public class WolframClientTests
    {
        // --- Helper to build a WolframClient backed by a fake HTTP handler ---

        private static WolframClient MakeClient(HttpStatusCode status, string body)
        {
            var handler = new FakeHttpHandler(status, body);
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var client = new WolframClient(http);
            client.UpdateAppId("TEST_APP_ID");
            return client;
        }

        // --- App ID guard ---

        [TestMethod]
        public async Task QueryAsync_ReturnsConfigError_WhenAppIdNotSet()
        {
            var handler = new FakeHttpHandler(HttpStatusCode.OK, "4");
            var client = new WolframClient(new HttpClient(handler));
            // AppId deliberately not set

            var result = await client.QueryAsync("2+2", CancellationToken.None);

            Assert.IsTrue(result.Contains("not configured"), $"Unexpected: {result}");
        }

        // --- Success path ---

        [TestMethod]
        public async Task QueryAsync_ReturnsBody_WhenResponseIsOk()
        {
            using var client = MakeClient(HttpStatusCode.OK, "4");

            var result = await client.QueryAsync("2+2", CancellationToken.None);

            Assert.AreEqual("4", result);
        }

        // --- Wolfram-specific error codes ---

        [TestMethod]
        public async Task QueryAsync_ReturnsNoShortAnswer_On501()
        {
            using var client = MakeClient(HttpStatusCode.NotImplemented, "No short answer");

            var result = await client.QueryAsync("who is president", CancellationToken.None);

            Assert.AreEqual("No short answer available.", result);
        }

        [TestMethod]
        public async Task QueryAsync_ReturnsNoShortAnswer_On400()
        {
            using var client = MakeClient(HttpStatusCode.BadRequest, "Error");

            var result = await client.QueryAsync("!!!invalid", CancellationToken.None);

            Assert.AreEqual("No short answer available.", result);
        }

        [TestMethod]
        public async Task QueryAsync_ReturnsApiError_OnOtherNonSuccessStatus()
        {
            using var client = MakeClient(HttpStatusCode.InternalServerError, "Server error");

            var result = await client.QueryAsync("2+2", CancellationToken.None);

            Assert.IsTrue(result.StartsWith("API Error:"), $"Unexpected: {result}");
        }

        // --- Timeout ---

        [TestMethod]
        public async Task QueryAsync_ReturnsTimedOut_WhenRequestTimesOut()
        {
            var handler = new TimeoutHttpHandler();
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1) };
            using var client = new WolframClient(http);
            client.UpdateAppId("TEST_APP_ID");

            var result = await client.QueryAsync("2+2", CancellationToken.None);

            Assert.IsTrue(result.Contains("timed out") || result.Contains("Request failed"),
                $"Unexpected: {result}");
        }

        // --- Cancellation ---

        [TestMethod]
        public async Task QueryAsync_Throws_WhenCancelled()
        {
            var handler = new BlockingHttpHandler();
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            using var client = new WolframClient(http);
            client.UpdateAppId("TEST_APP_ID");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // HttpClient wraps cancellation as TaskCanceledException (subclass of OperationCanceledException)
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                () => client.QueryAsync("2+2", cts.Token));
        }

        // --- Fake HTTP handlers ---

        private sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
                => Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                });
        }

        private sealed class TimeoutHttpHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private sealed class BlockingHttpHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
