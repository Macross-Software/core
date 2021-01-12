using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Macross.Logging.Abstractions;

namespace Macross.Logging
{
	/// <summary>
	/// A class for flattening Logger data into a Json representation.
	/// </summary>
	public sealed class LoggerJsonMessage
	{
		private static readonly Action<object, LoggerJsonMessage> s_ParseScopeItem = ParseScopeItem;
		private static readonly Dictionary<Type, List<PropertyGetter>> s_TypePropertyCache = new Dictionary<Type, List<PropertyGetter>>();
		private static readonly ConcurrentBag<List<object?>> s_ScopeListPool = new ConcurrentBag<List<object?>>();
		private static readonly ConcurrentBag<Dictionary<string, object?>> s_DataDictionaryPool = new ConcurrentBag<Dictionary<string, object?>>();

		/// <summary>
		/// Create a <see cref="LoggerJsonMessage"/> instance from Logger data.
		/// </summary>
		/// <param name="categoryName">Category name associated with this entry.</param>
		/// <param name="scopeProvider"><see cref="IExternalScopeProvider"/>.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">Id of the event.</param>
		/// <param name="state">The entry to be written. Can be also an object.</param>
		/// <param name="exception">The exception related to this entry.</param>
		/// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
		/// <typeparam name="TState">The type of the object to be written.</typeparam>
		/// <returns>Created <see cref="LoggerJsonMessage"/> instance.</returns>
		public static LoggerJsonMessage FromLoggerData<TState>(
			string categoryName,
			IExternalScopeProvider? scopeProvider,
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			Debug.Assert(formatter != null);

			LoggerJsonMessage Message = new LoggerJsonMessage()
			{
				TimestampUtc = DateTime.UtcNow,
				ThreadId = Thread.CurrentThread.ManagedThreadId,
				EventId = eventId.Id != 0 ? eventId.Id : null,
				CategoryName = categoryName,
				LogLevel = logLevel switch
				{
					Microsoft.Extensions.Logging.LogLevel.Information => "Information",
					Microsoft.Extensions.Logging.LogLevel.Warning => "Warning",
					Microsoft.Extensions.Logging.LogLevel.Error => "Error",
					Microsoft.Extensions.Logging.LogLevel.Critical => "Critical",
					Microsoft.Extensions.Logging.LogLevel.Trace => "Trace",
					Microsoft.Extensions.Logging.LogLevel.Debug => "Debug",
					Microsoft.Extensions.Logging.LogLevel.None => "None",
					_ => throw new NotSupportedException($"LogLevel [{logLevel}] is not supported."),
				}
			};

			scopeProvider?.ForEachScope(s_ParseScopeItem, Message);

			if (exception != null)
				Message.Exception = LoggerJsonMessageException.FromException(exception);

			if (state is FormattedLogValues formattedLogValues)
			{
				foreach (KeyValuePair<string, object?> Item in formattedLogValues)
				{
					AddStateItemToMessage(state, Message, Item);
				}
			}
			else if (state is IReadOnlyList<KeyValuePair<string, object?>> stateList)
			{
				for (int i = 0; i < stateList.Count; i++)
				{
					AddStateItemToMessage(state, Message, stateList[i]);
				}
			}
			else if (state is IEnumerable<KeyValuePair<string, object?>> stateValues)
			{
				foreach (KeyValuePair<string, object?> Item in stateValues)
				{
					AddStateItemToMessage(state, Message, Item);
				}
			}

			if (string.IsNullOrEmpty(Message.Content))
			{
				string FormattedMessage = formatter(state, null);
				if (FormattedMessage != "[null]")
					Message.Content = FormattedMessage;
			}

			return Message;
		}

		private static void ParseScopeItem(object item, LoggerJsonMessage message)
		{
			if (item is LoggerGroup LoggerGroup)
			{
				if (message._Group == null || LoggerGroup.Priority >= message._Group.Priority)
					message._Group = LoggerGroup;
			}
			else if (item is string)
			{
				if (message.Scope == null)
					message.Scope = RentScopeList();
				message.Scope.Add(item);
			}
			else if (item is IReadOnlyList<KeyValuePair<string, object?>> scopeList)
			{
				for (int i = 0; i < scopeList.Count; i++)
				{
					AddScopeItemToMessage(item, message, scopeList[i]);
				}
			}
			else if (item is IEnumerable<KeyValuePair<string, object?>> scopeValues)
			{
				foreach (KeyValuePair<string, object?> SubItem in scopeValues)
				{
					AddScopeItemToMessage(item, message, SubItem);
				}
			}
			else if (item.GetType().IsValueType)
			{
				if (message.Scope == null)
					message.Scope = RentScopeList();
				message.Scope.Add(item);
			}
			else
			{
				if (message.Data == null)
					message.Data = RentDataDictionary();

				AddObjectPropertiesToMessageData(message.Data, item);
			}
		}

		private static void AddStateItemToMessage<TState>(TState state, LoggerJsonMessage message, KeyValuePair<string, object?> item)
		{
			if (item.Key == "{OriginalFormat}")
			{
				// state should be a FormattedLogValues instance here, which formats when ToString is called.
				string FormattedMessage = state!.ToString();
				if (FormattedMessage != "[null]")
					message.Content = FormattedMessage;
				return;
			}

			if (message.Data == null)
				message.Data = RentDataDictionary();

			if (item.Key == "{Data}")
			{
				AddDataToMessage(message.Data, item.Value);
				return;
			}

			message.Data[item.Key] = item.Value;
		}

		private static void AddScopeItemToMessage(object scopeItem, LoggerJsonMessage message, KeyValuePair<string, object?> scopeSubItem)
		{
			if (scopeSubItem.Key == "{OriginalFormat}")
			{
				if (message.Scope == null)
					message.Scope = RentScopeList();

				// Item should be a FormattedLogValues instance here, which formats when ToString is called.
				string FormattedMessage = scopeItem.ToString();
				if (FormattedMessage == "[null]")
					message.Scope.Add(null);
				else
					message.Scope.Add(FormattedMessage);
				return;
			}

			if (message.Data == null)
				message.Data = RentDataDictionary();

			message.Data[scopeSubItem.Key] = scopeSubItem.Value;
		}

		private static void AddDataToMessage(IDictionary<string, object?> messageData, object? data)
		{
			if (data is IEnumerable<KeyValuePair<string, object?>> DataValues)
			{
				foreach (KeyValuePair<string, object?> DataItem in DataValues)
				{
					messageData[DataItem.Key] = DataItem.Value;
				}
			}
			else if (data != null)
			{
				AddObjectPropertiesToMessageData(messageData, data);
			}
		}

		private static void AddObjectPropertiesToMessageData(IDictionary<string, object?> data, object value)
		{
			Type type = value.GetType();
			if (!s_TypePropertyCache.TryGetValue(type, out List<PropertyGetter> propertyGetters))
			{
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				propertyGetters = new List<PropertyGetter>(properties.Length);

				foreach (PropertyInfo propertyInfo in properties)
				{
					if (propertyInfo.CanRead)
						propertyGetters.Add(new PropertyGetter(type, propertyInfo));
				}

				s_TypePropertyCache[type] = propertyGetters;
			}

			foreach (PropertyGetter propertyGetter in propertyGetters)
			{
				data[propertyGetter.PropertyName] = propertyGetter.GetPropertyFunc(value);
			}
		}

		private static List<object?> RentScopeList()
		{
			return !s_ScopeListPool.TryTake(out List<object?> scope)
				? new List<object?>(16)
				: scope;
		}

		private static Dictionary<string, object?> RentDataDictionary()
		{
			return !s_DataDictionaryPool.TryTake(out Dictionary<string, object?> data)
				? new Dictionary<string, object?>(16)
				: data;
		}

		/// <summary>
		/// Returns any rented resources for the <see cref="LoggerJsonMessage"/> back to their parent pools.
		/// </summary>
		/// <param name="message"><see cref="LoggerJsonMessage"/>.</param>
		public static void Return(LoggerJsonMessage message)
		{
			Debug.Assert(message != null);

			if (message.Scope != null && s_ScopeListPool.Count < 1024)
			{
				message.Scope.Clear();
				s_ScopeListPool.Add(message.Scope);
				message.Scope = null;
			}

			if (message.Data != null && s_DataDictionaryPool.Count < 1024)
			{
				message.Data.Clear();
				s_DataDictionaryPool.Add(message.Data);
				message.Data = null;
			}

			if (message.Exception != null)
				LoggerJsonMessageException.Return(message.Exception);
		}

		private LoggerGroup? _Group;

		/// <summary>
		/// Gets or sets the message timestamp in UTC.
		/// </summary>
		public DateTime? TimestampUtc { get; set; }

		/// <summary>
		/// Gets or sets the Id of the thread that wrote the message.
		/// </summary>
		public int? ThreadId { get; set; }

		/// <summary>
		/// Gets or sets the Id of the event corresponding to the message.
		/// </summary>
		public int? EventId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Microsoft.Extensions.Logging.LogLevel"/> of the message.
		/// </summary>
		public string? LogLevel { get; set; }

		/// <summary>
		/// Gets or sets the group name of the message.
		/// </summary>
		public string? GroupName { get; set; }

		/// <summary>
		/// Gets or sets the category name of the message.
		/// </summary>
		public string? CategoryName { get; set; }

		/// <summary>
		/// Gets or sets the content of the message.
		/// </summary>
		public string? Content { get; set; }

		/// <summary>
		/// Gets or sets the exception details.
		/// </summary>
		public LoggerJsonMessageException? Exception { get; set; }

		/// <summary>
		/// Gets or sets the scope data values associated with the message.
		/// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
		public List<object?>? Scope { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

		/// <summary>
		/// Gets or sets the data properties associated with the message.
		/// </summary>
		[JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
		public Dictionary<string, object?>? Data { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
	}
}
