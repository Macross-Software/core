using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Extensions.Tests
{
	[TestClass]
	public class BitCompactorTests
	{
		[TestMethod]
		public void TestBitCompactorConstructorFromTwoBitsSuccess()
		{
			BitCompactor Compactor = new BitCompactor(true, false);

			Assert.IsNotNull(Compactor);
			Assert.IsNotNull(Compactor.Value);
			Assert.AreEqual(1, Compactor.Value.Length);
			Assert.AreEqual(0x01, Compactor.Value[0]);
		}

		[TestMethod]
		public void TestBitCompactorConstructorFromFourBitsSuccess()
		{
			BitCompactor Compactor = new BitCompactor(true, false, true, true);

			Assert.IsNotNull(Compactor);
			Assert.IsNotNull(Compactor.Value);
			Assert.AreEqual(1, Compactor.Value.Length);
			Assert.AreEqual(0x0D, Compactor.Value[0]);
		}

		[TestMethod]
		public void TestBitCompactorConstructorFromNineBitsSuccess()
		{
			BitCompactor Compactor = new BitCompactor(
				new bool[]
				{
					true, false, true, true, // 1101 = D
					true, true, false, true, // 1011 = B
					true // 0001 = 1
				});

			Assert.IsNotNull(Compactor);
			Assert.IsNotNull(Compactor.Value);
			Assert.AreEqual(2, Compactor.Value.Length);
			Assert.AreEqual(0xBD, Compactor.Value[0]);
			Assert.AreEqual(0x01, Compactor.Value[1]);
		}

		[TestMethod]
		public void TestBitCompactorConstructorToBitsSuccess()
		{
			BitCompactor Compactor = new BitCompactor(0xBD, 0x01);

			Assert.IsNotNull(Compactor);
			Assert.IsNotNull(Compactor.Value);
			Assert.AreEqual(2, Compactor.Value.Length);

			bool[] Bits = Compactor.ToBits();

			Assert.IsNotNull(Bits);
			Assert.AreEqual(16, Bits.Length);

			Assert.IsTrue(
				Bits
				.SequenceEqual(
					new bool[]
					{
						true, false, true, true, // 1101 = D
						true, true, false, true, // 1011 = B
						true, false, false, false, // 0001 = 1
						false, false, false, false // 0000 = 0
					}));
		}

		[TestMethod]
		public void TestBitCompactorIndexerSingleByteSuccess()
		{
			BitCompactor Compactor = new BitCompactor(true, false);

			Assert.IsTrue(Compactor[0]);
			Assert.IsFalse(Compactor[1]);
			Assert.IsFalse(Compactor[2]); // Not defined case
		}

		[TestMethod]
		public void TestBitCompactorIndexerMultipleByteSuccess()
		{
			BitCompactor Compactor = new BitCompactor(
				new bool[]
				{
					true, false, true, true, // 1101 = D
					true, true, false, true, // 1011 = B
					true // 0001 = 1
				});

			Assert.IsTrue(Compactor[0]);
			Assert.IsFalse(Compactor[1]);
			Assert.IsTrue(Compactor[7]);
			Assert.IsTrue(Compactor[8]);
		}
	}
}