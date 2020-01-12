using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class StringExtensionTests
	{
		[TestMethod]
		public void StringSplitPredicateTest()
		{
			string[] Expected = new string[]
			{
				"value1",
				"value2"
			};
			IEnumerable<string> Actual = "value1|value2".Split(c => c == '|');

			Assert.IsTrue(Expected.SequenceEqual(Actual));
		}

		[TestMethod]
		public void StringSplitPredicateWithEmptyTest()
		{
			string[] Expected = new string[]
			{
				"value1",
				string.Empty,
				" ",
				"value2"
			};
			IEnumerable<string> Actual = "value1|| |value2".Split(c => c == '|');

			Assert.IsTrue(Expected.SequenceEqual(Actual));
		}

		[TestMethod]
		public void StringSplitPredicateWithEmptyRemovedTest()
		{
			string[] Expected = new string[]
			{
				"value1",
				" ",
				"value2"
			};
			IEnumerable<string> Actual = "value1|| |value2".Split(c => c == '|', StringSplitOptions.RemoveEmptyEntries);

			Assert.IsTrue(Expected.SequenceEqual(Actual));
		}
	}
}
