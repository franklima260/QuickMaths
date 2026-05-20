using Community.PowerToys.Run.Plugin.QuickMaths;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuickMaths.Tests
{
    /// <summary>
    /// MathDetector uses a permissive regex pre-filter: any digit, math operator,
    /// or recognised keyword is enough to forward to Wolfram Alpha. Wolfram's own
    /// NLP handles false-positives; the detector only blocks obviously non-math input.
    /// </summary>
    [TestClass]
    public class MathDetectorTests
    {
        // --- Queries that should be forwarded to Wolfram ---

        [DataTestMethod]
        [DataRow("2+2")]
        [DataRow("sin(45)")]
        [DataRow("10 * 5 / 2")]
        [DataRow("e^pi")]
        [DataRow("sqrt(16) + 4")]
        [DataRow("100%")]
        [DataRow("log(100) =")]
        [DataRow("(-5)^2")]            // negative base in parens
        [DataRow("2pi")]               // digit + keyword
        [DataRow("1e10 + 2")]          // scientific notation with operator
        [DataRow("3.14 * 2")]          // decimal with operator
        [DataRow("cos(0) + sin(0)")]   // multiple functions with digit
        [DataRow("ln(1)")]             // natural log with digit
        [DataRow("1,000 + 2,000")]     // thousands-separated numbers
        [DataRow("12345")]             // bare number has digit → forwarded
        [DataRow("3.14")]              // decimal has digit → forwarded
        [DataRow("test+test")]         // operator present → forwarded
        [DataRow("foo * bar")]         // operator present → forwarded
        [DataRow("-5 + 3")]            // negative number with operator
        public void IsMathQuery_ValidMath_ReturnsTrue(string query)
        {
            Assert.IsTrue(MathDetector.IsMathQuery(query));
        }

        // --- Queries that have no digit, operator, or recognised keyword ---

        [DataTestMethod]
        [DataRow("hello world")]
        [DataRow("what is the weather")]
        [DataRow("open chrome")]
        [DataRow("  ")]               // whitespace only
        [DataRow(null)]               // null
        [DataRow("")]                 // empty string
        [DataRow("sincos")]           // not a valid keyword word-boundary match
        public void IsMathQuery_NotMath_ReturnsFalse(string query)
        {
            Assert.IsFalse(MathDetector.IsMathQuery(query));
        }

        // --- Boundary / keyword-specific cases ---

        [TestMethod]
        public void IsMathQuery_PiAlone_ReturnsTrue()
        {
            // "pi" is a recognised keyword matched at word boundary
            Assert.IsTrue(MathDetector.IsMathQuery("pi"));
        }

        [TestMethod]
        public void IsMathQuery_EulerInExpression_ReturnsTrue()
        {
            // "^" is in the operator set; "e" alone is not a keyword
            Assert.IsTrue(MathDetector.IsMathQuery("e^2"));
        }

        [TestMethod]
        public void IsMathQuery_PlainTextNoDigitOrSymbol_ReturnsFalse()
        {
            Assert.IsFalse(MathDetector.IsMathQuery("the quick brown fox"));
        }
    }
}
