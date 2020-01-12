using System;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class ToStringArrayExtensionTests
	{
		[TestMethod]
		public void TestToStringArrayExtensionBasicSuccess()
		{
			ArraySegment<byte>[] Data = new ArraySegment<byte>[]
			{
				new ArraySegment<byte>(Encoding.ASCII.GetBytes("Hello")),
				new ArraySegment<byte>(Encoding.ASCII.GetBytes("World")),
			};

			string[] Strings = Data.ToStringArray(Encoding.ASCII);

			Assert.IsNotNull(Strings);
			Assert.AreEqual(2, Strings.Length);
			Assert.AreEqual("Hello", Strings[0]);
			Assert.AreEqual("World", Strings[1]);
		}

		[TestMethod]
		public void TestToStringArrayExtensionNullRecordSuccess()
		{
			ArraySegment<byte>[] Data = new ArraySegment<byte>[]
			{
				new ArraySegment<byte>(Encoding.ASCII.GetBytes("Hello"), 1, 3)
			};

			string[] Strings = Data.ToStringArray(Encoding.ASCII);

			Assert.IsNotNull(Strings);
			Assert.AreEqual(1, Strings.Length);
			Assert.AreEqual("ell", Strings[0]);
		}

		[TestMethod]
		public void TestToStringArrayExtensionInvalidCharactersSuccess()
		{
			byte[] One = new byte[] { 0x00, 0x00, 0x00 };
			byte[] Two = new byte[] { (byte)'A', 0x2F, (byte)'z' };
			byte[] Three = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

			ArraySegment<byte>[] Data = new ArraySegment<byte>[]
			{
				new ArraySegment<byte>(One),
				new ArraySegment<byte>(Two),
				new ArraySegment<byte>(Three)
			};

			string[] Strings = Data.ToStringArray(Encoding.ASCII);

			Assert.IsNotNull(Strings);
			Assert.AreEqual(3, Strings.Length);
			Assert.AreEqual(Encoding.ASCII.GetString(One), Strings[0]);
			Assert.AreEqual(Encoding.ASCII.GetString(Two), Strings[1]);
			Assert.AreEqual(Encoding.ASCII.GetString(Three), Strings[2]);
		}
	}
}