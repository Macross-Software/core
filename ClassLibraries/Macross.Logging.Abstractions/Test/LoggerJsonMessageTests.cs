using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Extensions.Logging;

namespace Macross.Logging.Abstractions.Tests
{
	[TestClass]
	public class LoggerJsonMessageTests
	{
		[TestMethod]
		public void BasicMessageTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.LogInformation("Hello world.");

			Assert.AreEqual(nameof(LogLevel.Information), Logger.Message?.LogLevel);
			Assert.AreEqual("Hello world.", Logger.Message?.Content);
			Assert.IsNull(Logger.Message?.Data);
			Assert.IsNull(Logger.Message?.Scope);
		}

		[TestMethod]
		public void NullMessageTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.LogInformation(null);

			Assert.AreEqual(nameof(LogLevel.Information), Logger.Message?.LogLevel);
			Assert.AreEqual(null, Logger.Message?.Content);
			Assert.IsNull(Logger.Message?.Data);
			Assert.IsNull(Logger.Message?.Scope);
		}

		[TestMethod]
		public void AtypicalMessageTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.Log(LogLevel.Information, 0, 1000, null, (s, e) => $"{s}");

			Assert.AreEqual(nameof(LogLevel.Information), Logger.Message?.LogLevel);
			Assert.AreEqual("1000", Logger.Message?.Content);
			Assert.IsNull(Logger.Message?.Data);
			Assert.IsNull(Logger.Message?.Scope);
		}

		[TestMethod]
		public void FormattedMessageTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.LogInformation("OrderId {OrderId} CustomerId {CustomerId} Null {NullValue}", 1, 2, null);

			Assert.AreEqual(nameof(LogLevel.Information), Logger.Message?.LogLevel);
			Assert.AreEqual("OrderId 1 CustomerId 2 Null (null)", Logger.Message?.Content);

			Assert.AreEqual(3, Logger.Message?.Data?.Count);
			Assert.AreEqual(1, Logger.Message?.Data?["OrderId"]);
			Assert.AreEqual(2, Logger.Message?.Data?["CustomerId"]);
			Assert.AreEqual(null, Logger.Message?.Data?["NullValue"]);
		}

		[TestMethod]
		public void ScopeTests()
		{
			TestLogger Logger = new TestLogger();

			using IDisposable Scope1 = Logger.BeginScope(1000);
			using IDisposable Scope2 = Logger.BeginScope("Scope");
			using IDisposable Scope3 = Logger.BeginScope(null);
			using IDisposable Scope4 = Logger.BeginScope("OrderId {OrderId} CustomerId {CustomerId}", 1, 2);
			using IDisposable Scope5 = Logger.BeginScope(
				new TestProduct
				{
					ProductId = 3,
					AddressId = 4
				});

			Logger.LogInformation("Hello world.");

			Assert.AreEqual(4, Logger.Message?.Scope?.Count);
			Assert.AreEqual(1000, Logger.Message?.Scope?[0]);
			Assert.AreEqual("Scope", Logger.Message?.Scope?[1]);
			Assert.AreEqual(null, Logger.Message?.Scope?[2]);
			Assert.AreEqual("OrderId 1 CustomerId 2", Logger.Message?.Scope?[3]);

			Assert.AreEqual(4, Logger.Message?.Data?.Count);
			Assert.AreEqual(1, Logger.Message?.Data?["OrderId"]);
			Assert.AreEqual(2, Logger.Message?.Data?["CustomerId"]);
			Assert.AreEqual(3, Logger.Message?.Data?["ProductId"]);
			Assert.AreEqual(4, Logger.Message?.Data?["AddressId"]);
		}

		[TestMethod]
		public void GroupNameTest()
		{
			TestLogger Logger = new TestLogger();

			using IDisposable Group = Logger.BeginGroup("Main");

			Logger.LogInformation("Hello world.");

			Assert.AreEqual("Main", Logger.Message?.GroupName);
			Assert.AreEqual(0, Logger.Message?.Scope?.Count ?? 0);
		}

		[TestMethod]
		public void GroupNameMultipleTest()
		{
			TestLogger Logger = new TestLogger();

			using IDisposable Group1 = Logger.BeginGroup("Main");
			using IDisposable Group2 = Logger.BeginGroup("Sub1");
			using IDisposable Group3 = Logger.BeginGroup("Sub2");

			Logger.LogInformation("Hello world.");

			Assert.AreEqual("Sub2", Logger.Message?.GroupName);
			Assert.AreEqual(0, Logger.Message?.Scope?.Count ?? 0);
		}

		[TestMethod]
		public void GroupNamePriorityTest()
		{
			TestLogger Logger = new TestLogger();

			using IDisposable Group1 = Logger.BeginGroup("Main", 1);
			using IDisposable Group2 = Logger.BeginGroup("Sub1");
			using IDisposable Group3 = Logger.BeginGroup("Sub2");

			Logger.LogInformation("Hello world.");

			Assert.AreEqual("Main", Logger.Message?.GroupName);
			Assert.AreEqual(0, Logger.Message?.Scope?.Count ?? 0);
		}

		[TestMethod]
		public void WriteExtensionDataNullTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.Write(
				LogLevel.Information,
				null,
				"Data test.");

			Assert.AreEqual(0, Logger.Message?.Data?.Count);
		}

		[TestMethod]
		public void WriteExtensionDataValidTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.Write(
				LogLevel.Information,
				new TestProduct
				{
					ProductId = 3,
					AddressId = 4
				},
				"Data test.");

			Assert.AreEqual(2, Logger.Message?.Data?.Count);
			Assert.AreEqual(3, Logger.Message?.Data?["ProductId"]);
			Assert.AreEqual(4, Logger.Message?.Data?["AddressId"]);
		}

		[TestMethod]
		public void WriteExtensionEnumerableDataValidTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.Write(
				LogLevel.Information,
				new Dictionary<string, object>
				{
					["ProductId"] = 3,
					["AddressId"] = 4
				},
				"Data test.");

			Assert.AreEqual(2, Logger.Message?.Data?.Count);
			Assert.AreEqual(3, Logger.Message?.Data?["ProductId"]);
			Assert.AreEqual(4, Logger.Message?.Data?["AddressId"]);
		}

		[TestMethod]
		public void SerializeMessageToJsonTest()
		{
			TestLogger Logger = new TestLogger();

			Logger.LogInformation("Hello world.");

			string json = JsonSerializer.Serialize(
				Logger.Message,
				new JsonSerializerOptions
				{
					IgnoreNullValues = true
				});

			Assert.IsNotNull(Logger.Message);
			Assert.AreEqual(@$"{{""TimestampUtc"":{JsonSerializer.Serialize(Logger.Message.TimestampUtc)},""ThreadId"":{Logger.Message.ThreadId},""LogLevel"":""Information"",""CategoryName"":""Category"",""Content"":""Hello world.""}}", json);
		}

		private class TestProduct
		{
			public int? ProductId { get; set; }

			public int? AddressId { get; set; }
		}

		private class TestLogger : ILogger
		{
			private readonly IExternalScopeProvider _ScopeProvider;

			public LoggerJsonMessage? Message { get; private set; }

			public TestLogger()
			{
				_ScopeProvider = new LoggerExternalScopeProvider();
			}

			public IDisposable BeginScope<TState>(TState state) => _ScopeProvider.Push(state);

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
				=> Message = LoggerJsonMessage.FromLoggerData("Category", _ScopeProvider, logLevel, eventId, state, exception, formatter);
		}
	}
}
