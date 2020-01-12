using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class HexStringExtensionTests
	{
		[TestMethod]
		public void ByteArrayToHexStringTestSuccess()
		{
			byte[] Value = new byte[] { 0xFF, 0x01, 0x0A, 0x15, 0xBD };

			string Actual = Value.ToHexString();

			Assert.AreEqual("FF010A15BD", Actual);
		}

		[TestMethod]
		public void ByteToHexStringTestSuccess()
		{
			string Actual = ((byte)0x1A).ToHexString();

			Assert.AreEqual("1A", Actual);
		}

		[TestMethod]
		public void HexStringToByteArrayTestSuccess()
		{
			string Value = "FF010A15BD";

			byte[] Actual = Value.ToByteArray();

			byte[] Expected = new byte[] { 0xFF, 0x01, 0x0A, 0x15, 0xBD };

			Assert.IsTrue(Expected.SequenceEqual(Actual));
		}

		[TestMethod]
		public void HexStringMixedCaseToByteArrayTestSuccess()
		{
			string Value = "Ff010a15bdFF";

			byte[] Actual = Value.ToByteArray();

			byte[] Expected = new byte[] { 0xFF, 0x01, 0x0A, 0x15, 0xBD, 0xFF };

			Assert.IsTrue(Expected.SequenceEqual(Actual));
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void InvalidLengthHexStringToByteArrayTestFailure()
		{
			string Value = "Ff0";

			Value.ToByteArray();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InvalidContentHexStringToByteArrayTestFailure()
		{
			string Value = "hello world!";

			Value.ToByteArray();
		}
	}
}