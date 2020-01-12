using System;
using System.Runtime.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class EnumExtensionTests
	{
		private enum TestEnum : int
		{
			Default = 0,
			Definition1 = 1,
			[EnumMember(Value = "B")]
			Definition2 = 2,
		}

		[TestMethod]
		public void EnumParseStringMatchesDefinitionTest()
		{
			TestEnum ExptectedValue = TestEnum.Definition1;
			TestEnum ActualValue = "Definition1".ToEnum<TestEnum>();

			Assert.AreEqual(ExptectedValue, ActualValue);
		}

		[TestMethod]
		public void EnumParseStringMatchesAttributeTest()
		{
			TestEnum ExptectedValue = TestEnum.Definition2;
			TestEnum ActualValue = "B".ToEnum<TestEnum>();

			Assert.AreEqual(ExptectedValue, ActualValue);
		}

		[TestMethod]
		public void EnumParseFallbackTest()
		{
			TestEnum ExptectedValue = TestEnum.Default;
			TestEnum ActualValue = "Definition3".ToEnum(TestEnum.Default);

			Assert.AreEqual(ExptectedValue, ActualValue);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void EnumParseInvalidMatchTest() => "Definition3".ToEnum<TestEnum>();

		[TestMethod]
		public void EnumFromValueTest()
		{
			TestEnum ExptectedValue = TestEnum.Definition2;
			TestEnum ActualValue = 2.ToEnum<int, TestEnum>();

			Assert.AreEqual(ExptectedValue, ActualValue);
		}

		[TestMethod]
		public void EnumFromValueFallbackTest()
		{
			TestEnum ExptectedValue = TestEnum.Default;
			TestEnum ActualValue = 99.ToEnum(TestEnum.Default);

			Assert.AreEqual(ExptectedValue, ActualValue);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void EnumFromValueInvalidMatchTest() => 99.ToEnum<int, TestEnum>();
	}
}
