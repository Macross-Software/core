using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class Utf8JsonStreamReaderTests
	{
		[TestMethod]
		public async Task DeserializeFromStreamTest()
		{
			int rentCalls = 0;
			int returnCalls = 0;

			Utf8JsonStreamReader.RentBufferFunc = (size) =>
			{
				rentCalls++;
				return new byte[size];
			};

			Utf8JsonStreamReader.ReturnBufferAction = (buffer) => returnCalls++;

			using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"PropertyName\":1, \"PropertyName2\":\"value\"}"));

			TestClass instance = new();

			await Utf8JsonStreamReader.DeserializeAsync(stream, instance, ReadMethod, bufferSize: 8192).ConfigureAwait(false);

			Assert.AreEqual(1, instance.PropertyName);
			Assert.AreEqual("value", instance.PropertyName2);
			Assert.AreEqual(1, rentCalls);
			Assert.AreEqual(1, returnCalls);
		}

		[TestMethod]
		[DataRow("value123456", 16, 2, nameof(GetBufferDataBlocksFullBlocksTest))]
		[DataRow("value1234567890_1234", 16, 3, nameof(GetBufferDataBlocksPartialBlocksTest))]
		[DataRow("value1234567890_1234", 8192, 1, nameof(GetBufferDataBlocksPartialBlocksTest))]
		[DataRow("012345678901234567890123456789012", 16, 4, nameof(GetBufferDataBlocksLongTextTest))]
		public async Task DeserializeFromStreamSequencingTest(
			string value,
			int bufferSize,
			int expectedRentCalls,
			string getBuffersMethodName)
		{
			MethodInfo? getBuffersMethod = typeof(Utf8JsonStreamReaderTests).GetMethod(getBuffersMethodName, BindingFlags.Static | BindingFlags.NonPublic);
			if (getBuffersMethod == null)
				throw new InvalidOperationException($"getBuffersMethodName [{getBuffersMethodName}] could not be found reflectively.");

			int rentCalls = 0;
			int returnCalls = 0;

			Utf8JsonStreamReader.RentBufferFunc = (size) =>
			{
				rentCalls++;
				return new byte[size];
			};

			Utf8JsonStreamReader.ReturnBufferAction = (buffer) => returnCalls++;

			using TestStream stream = new((Memory<byte>[])getBuffersMethod.Invoke(null, null)!);

			TestClass instance = new();

			await Utf8JsonStreamReader.DeserializeAsync(stream, instance, ReadMethod, bufferSize).ConfigureAwait(false);

			Assert.AreEqual(1, instance.PropertyName);
			Assert.AreEqual(value, instance.PropertyName2);
			Assert.AreEqual(expectedRentCalls, rentCalls);
			Assert.AreEqual(expectedRentCalls, returnCalls);
		}

		private static Memory<byte>[] GetBufferDataBlocksFullBlocksTest()
		{
			/* 0123456789012345678901234567890123456789012345678
			 * {"PropertyName":1, "PropertyName2":"value123456"}
			 */
			return new Memory<byte>[]
			{
				Encoding.UTF8.GetBytes("{\"PropertyName\":"), // 16 bytes - should drain
				Encoding.UTF8.GetBytes("1, \"PropertyName"), // 15 bytes
				Encoding.UTF8.GetBytes("2"), // 1 byte - should cause sequencing
				Encoding.UTF8.GetBytes("\":\"value123456\"}"), // 16 bytes - should drain
			};
		}

		private static Memory<byte>[] GetBufferDataBlocksPartialBlocksTest()
		{
			/* 0123456789012345678901234567890123456789012345678901234567
			 * {"PropertyName":1, "PropertyName2":"value1234567890_1234"}
			 */
			return new Memory<byte>[]
			{
				Encoding.UTF8.GetBytes("{\"PropertyName\":"), // 16 bytes - should drain
				Encoding.UTF8.GetBytes("1,"), // 2 bytes - leaves 1 left over
				Encoding.UTF8.GetBytes("\"PropertyName2\""), // 15 bytes - should cause sequencing
				Encoding.UTF8.GetBytes(":"), // first extra allocation but should drain and go back to single buffer
				Encoding.UTF8.GetBytes("\"value1234567890"), // 16 bytes - should cause sequencing again
				Encoding.UTF8.GetBytes("_1234"), // 5 bytes - second extra allocation
				Encoding.UTF8.GetBytes("\"}"), // 2 bytes - should fit in the second extra allocation
			};
		}

		private static Memory<byte>[] GetBufferDataBlocksLongTextTest()
		{
			/* 01234567890123456789012345678901234567890123456789012345678901234567890
			 * {"PropertyName":1, "PropertyName2":"012345678901234567890123456789012"}
			 */
			return new Memory<byte>[]
			{
				Encoding.UTF8.GetBytes("{\"PropertyName\":"), // 16 bytes - should drain
				Encoding.UTF8.GetBytes("1,"), // 2 bytes - leaves 1 left over
				Encoding.UTF8.GetBytes("\"PropertyName2\""), // 15 bytes - should cause sequencing
				Encoding.UTF8.GetBytes(":"), // first extra allocation but should drain and go back to single buffer
				Encoding.UTF8.GetBytes("\"012345678901234"), // 16 bytes - should cause sequencing again
				Encoding.UTF8.GetBytes("5678901234567890"), // 16 bytes - second extra allocation
				Encoding.UTF8.GetBytes("12\"}"), // 4 bytes - third extra allocation
			};
		}

		private static bool ReadMethod(TestClass instance, ref Utf8JsonReader reader, ref int state)
		{
			while (true)
			{
				if (!reader.Read())
					return false;

				switch (state)
				{
					case 0:
						if (reader.TokenType != JsonTokenType.StartObject)
							throw new JsonException();
						break;
					case 1:
						if (reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();
						break;
					case 2:
						if (reader.TokenType != JsonTokenType.Number)
							throw new JsonException();
						instance.PropertyName = reader.GetInt32();
						break;
					case 3:
						if (reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();
						break;
					case 4:
						instance.PropertyName2 = reader.GetString();
						break;
					case 5:
						if (reader.TokenType != JsonTokenType.EndObject)
							throw new JsonException();
						return true;
					default:
						throw new JsonException();
				}

				state++;
			}
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class TestClass
#pragma warning restore CA1812 // Remove class never instantiated
		{
			public int PropertyName { get; set; }

			public string? PropertyName2 { get; set; }
		}

		private class TestStream : Stream
		{
			private readonly IReadOnlyList<Memory<byte>> _Buffers;
			private int _Index;

			public override bool CanRead => throw new NotImplementedException();

			public override bool CanSeek => throw new NotImplementedException();

			public override bool CanWrite => throw new NotImplementedException();

			public override long Length => throw new NotImplementedException();

			public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public TestStream(IReadOnlyList<Memory<byte>> buffers)
			{
				_Buffers = buffers;
			}

			public override void Flush() => throw new NotImplementedException();

			public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

			public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
				=> ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

#if NETFRAMEWORK
			public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#else
			public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#endif
			{
				if (_Index > _Buffers.Count)
					return new ValueTask<int>(0);

				Memory<byte> testBuffer = _Buffers[_Index++];
				if (testBuffer.Length > buffer.Length)
					throw new InvalidOperationException("Test buffer cannot be written into supplied buffer.");

				testBuffer.CopyTo(buffer);
				return new ValueTask<int>(testBuffer.Length);
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

			public override void SetLength(long value) => throw new NotImplementedException();

			public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
		}
	}
}
