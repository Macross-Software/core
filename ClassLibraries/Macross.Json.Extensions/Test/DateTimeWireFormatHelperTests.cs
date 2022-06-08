using System.Text;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class DateTimeWireFormatHelperTests
	{
		[TestMethod]
		[DataRow("/Date(1)/", 1)]
		[DataRow("/Date(-1)/", -1)]
		[DataRow("/Date(18+1234)/", 18, 1, 12, 34)]
		[DataRow("/Date(-20-9876)/", -20, -1, 98, 76)]
		[DataRow(@"\/Date(1580803200000)\/", 1580803200000)]
		[DataRow(@"\/Date(1580803200000+0800)\/", 1580803200000, 1, 8, 0)]
		public void ValidTryParseTests(string json, long ticks, int multiplier = 0, int hours = 0, int minutes = 0)
		{
			bool result = DateTimeWireFormatHelper.TryParse(Encoding.UTF8.GetBytes(json), out DateTimeWireFormatHelper.DateTimeOffsetParseResult parseResult);

			Assert.IsTrue(result);
			Assert.AreEqual(ticks, parseResult.UnixEpochMilliseconds);
			Assert.AreEqual(multiplier, parseResult.OffsetMultiplier);
			Assert.AreEqual(hours, parseResult.OffsetHours);
			Assert.AreEqual(minutes, parseResult.OffsetMinutes);
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("/Date()/")]
		[DataRow("/Date000/")]
		[DataRow("/Date(0)0")]
		[DataRow("0Date(0)/")]
		[DataRow("/Date(012/")]
		[DataRow("/date(0)/")]
		[DataRow(@"\/Date(0)/")]
		[DataRow(@"/Date(0)\/")]
		[DataRow("/Date(+0000)/")]
		[DataRow("/Date(1234a+0000)/")]
		[DataRow("/Date(1234-000a)/")]
		[DataRow("/Date(1234-00z0)/")]
		[DataRow("/Date(1234-0a00)/")]
		[DataRow("/Date(1234-z000)/")]
		[DataRow("/Date(1234+000)/")]
		[DataRow("/Date(1234+00000)/")]
		[DataRow("/Date(-)/")]
		[DataRow("/Date(0+)/")]
		public void InvalidTryParseTests(string json)
		{
			bool result = DateTimeWireFormatHelper.TryParse(Encoding.UTF8.GetBytes(json), out DateTimeWireFormatHelper.DateTimeOffsetParseResult parseResult);

			Assert.IsFalse(result);
		}
	}
}
