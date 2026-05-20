using System.Threading;
using Community.PowerToys.Run.Plugin.QuickMaths;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Wox.Plugin;

namespace QuickMaths.Tests
{
    /// <summary>
    /// Tests for Main.Query() paths. Init() is intentionally not called — IcoPath
    /// will be null but all Result content fields are still verifiable.
    ///
    /// UpdateSettings tests are omitted: PowerLauncherPluginSettings requires
    /// WinRT.Runtime which is unavailable in the headless test host.
    /// </summary>
    [TestClass]
    public class MainTests
    {
        private Mock<IWolframClient> _mockClient = null!;
        private Main _main = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockClient = new Mock<IWolframClient>();
            _main = new Main(_mockClient.Object);
        }

        [TestCleanup]
        public void Cleanup() => _main.Dispose();

        // --- Immediate (non-delayed) Query always returns empty ---

        [TestMethod]
        public void Query_ImmediatePath_AlwaysReturnsEmpty()
        {
            var results = _main.Query(MakeQuery("2+2"));

            Assert.AreEqual(0, results.Count);
            _mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // --- Delayed Query, non-math input ---

        [TestMethod]
        public void Query_Delayed_ReturnsEmpty_WhenNotMath()
        {
            var results = _main.Query(MakeQuery("hello world"), delayedExecution: true);

            Assert.AreEqual(0, results.Count);
            _mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // --- Delayed Query, non-delayed flag is false ---

        [TestMethod]
        public void Query_Delayed_ReturnsEmpty_WhenDelayedFlagIsFalse()
        {
            var results = _main.Query(MakeQuery("2+2"), delayedExecution: false);

            Assert.AreEqual(0, results.Count);
            _mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // --- Delayed Query, math input → Wolfram called and result returned ---

        [TestMethod]
        public void Query_Delayed_ReturnsSingleResult_WhenMathDetected()
        {
            _mockClient
                .Setup(c => c.QueryAsync("2+2", It.IsAny<CancellationToken>()))
                .ReturnsAsync("4");

            var results = _main.Query(MakeQuery("2+2"), delayedExecution: true);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("4", results[0].Title);
            Assert.AreEqual("QuickMaths via Wolfram Alpha", results[0].SubTitle);
        }

        [TestMethod]
        public void Query_Delayed_PassesSearchTermToWolfram()
        {
            const string expression = "sin(45)";
            _mockClient
                .Setup(c => c.QueryAsync(expression, It.IsAny<CancellationToken>()))
                .ReturnsAsync("0.7071");

            _main.Query(MakeQuery(expression), delayedExecution: true);

            _mockClient.Verify(c => c.QueryAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- Wolfram returns an error string (not an exception) ---

        [TestMethod]
        public void Query_Delayed_ReturnsSingleResult_WhenWolframReturnsErrorString()
        {
            _mockClient
                .Setup(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("No short answer available.");

            var results = _main.Query(MakeQuery("2+2"), delayedExecution: true);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("No short answer available.", results[0].Title);
        }

        // --- Wolfram throws → graceful empty list ---

        [TestMethod]
        public void Query_Delayed_ReturnsEmpty_WhenWolframThrows()
        {
            _mockClient
                .Setup(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Net.Http.HttpRequestException("network error"));

            var results = _main.Query(MakeQuery("2+2"), delayedExecution: true);

            Assert.AreEqual(0, results.Count);
        }

        // --- Explicit invocation via '=' action keyword bypasses MathDetector ---

        [TestMethod]
        public void Query_Delayed_ForwardsToWolfram_WhenExplicitKeyword()
        {
            // When the user types "= hello world", rawQuery="= hello world", actionKeyword="=".
            // Query.Search strips the keyword length: "= hello world".Substring(1).Trim() = "hello world".
            _mockClient
                .Setup(c => c.QueryAsync("hello world", It.IsAny<CancellationToken>()))
                .ReturnsAsync("42");

            var results = _main.Query(MakeQuery("= hello world", actionKeyword: "="), delayedExecution: true);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("42", results[0].Title);
        }

        // --- Helper ---

        // Query(rawQuery, actionKeyword): Search = rawQuery.Substring(actionKeyword.Length).Trim()
        // Empty string action keyword → Search == rawQuery (global/wildcard plugin mode).
        private static Query MakeQuery(string search, string actionKeyword = "")
            => new Query(search, actionKeyword);
    }
}
