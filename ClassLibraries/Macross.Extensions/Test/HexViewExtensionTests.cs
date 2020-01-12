using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class HexViewExtensionTests
	{
		[TestMethod]
		public void SingleLineHexViewTest()
		{
			byte[] Bytes = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F };

			string Actual = Bytes.ToHexView();

			Assert.AreEqual("0000  30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F  0123456789:;<=>?", Actual);
		}

		[TestMethod]
		public void MaskedRegionLineHexViewTest()
		{
			byte[] Bytes = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F };

			string Actual = Bytes.ToHexView(new MaskedRegion(4, 4));

			Assert.AreEqual("0000  30 31 32 33 ** ** ** ** 38 39 3A 3B 3C 3D 3E 3F  0123****89:;<=>?", Actual);
		}

		[TestMethod]
		public void MultipleSeparateMaskedRegionsLineHexViewTest()
		{
			byte[] Bytes = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F };

			string Actual = Bytes.ToHexView(new MaskedRegion(4, 4), new MaskedRegion(9, 2));

			Assert.AreEqual("0000  30 31 32 33 ** ** ** ** 38 ** ** 3B 3C 3D 3E 3F  0123****8**;<=>?", Actual);
		}

		[TestMethod]
		public void MultipleContinuousMaskedRegionsLineHexViewTest()
		{
			byte[] Bytes = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F };

			string Actual = Bytes.ToHexView(new MaskedRegion(4, 4), new MaskedRegion(8, 2));

			Assert.AreEqual("0000  30 31 32 33 ** ** ** ** ** ** 3A 3B 3C 3D 3E 3F  0123******:;<=>?", Actual);
		}

		[TestMethod]
		public void PartialLineHexViewTest()
		{
			byte[] Bytes = new byte[] { 0x30, 0x31, 0x32, 0x33 };

			string Actual = Bytes.ToHexView();

			Assert.AreEqual("0000  30 31 32 33                                      0123            ", Actual);
		}

		[TestMethod]
		public void DoubleLineHexViewTest()
		{
			byte[] Bytes = new byte[]
			{
				0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
				0x00, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0xFF, 0xFF
			};

			string[] Actual = Bytes.ToHexView().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			Assert.AreEqual("0000  30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D 3E 3F  0123456789:;<=>?", Actual[0]);
			Assert.AreEqual("0010  00 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D FF FF  .123456789:;<=..", Actual[1]);
		}

		[TestMethod]
		public void SpanningMaskedRegionDoubleLineHexViewTest()
		{
			byte[] Bytes = new byte[]
			{
				0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
				0x00, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0xFF, 0xFF
			};

			string[] Actual = Bytes.ToHexView(new MaskedRegion(14, 4)).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			Assert.AreEqual("0000  30 31 32 33 34 35 36 37 38 39 3A 3B 3C 3D ** **  0123456789:;<=**", Actual[0]);
			Assert.AreEqual("0010  ** ** 32 33 34 35 36 37 38 39 3A 3B 3C 3D FF FF  **23456789:;<=..", Actual[1]);
		}
	}
}