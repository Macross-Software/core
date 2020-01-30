using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Threading;
using System.ComponentModel;

using Microsoft.Extensions.Logging;

namespace Macross.Logging
{
	/// <summary>
	/// A class for flattening Logger data into a Json representation.
	/// </summary>
	public class LoggerJsonMessage
	{
		/// <summary>
		/// Create a <see cref="LoggerJsonMessage"/> instance from Logger data.
		/// </summary>
		/// <param name="groupName">Group name associated with this entry.</param>
		/// <param name="categoryName">Category name associated with this entry.</param>
		/// <param name="scope">Scope data associated with this entry.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">Id of the event.</param>
		/// <param name="state">The entry to be written. Can be also an object.</param>
		/// <param name="exception">The exception related to this entry.</param>
		/// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
		/// <typeparam name="TState">The type of the object to be written.</typeparam>
		/// <returns>Created <see cref="LoggerJsonMessage"/> instance.</returns>
		public static LoggerJsonMessage FromLoggerData<TState>(
			string? groupName,
			string categoryName,
			IEnumerable<object>? scope,
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			LoggerJsonMessage Message = new LoggerJsonMessage()
			{
				TimestampUtc = DateTime.UtcNow,
				ThreadId = Thread.CurrentThread.ManagedThreadId,
				EventId = eventId.Id != 0 ? (int?)eventId.Id : null,
				LogLevel = logLevel,
				GroupName = groupName,
				CategoryName = categoryName
			};

			if (scope != null)
				(Message.Data, Message.Scope) = ParseScope(scope);

			if (exception != null)
				Message.Exception = LoggerJsonMessageException.FromException(exception);

			if (state is IEnumerable<KeyValuePair<string, object?>> StateValues)
			{
				foreach (KeyValuePair<string, object?> Item in StateValues)
				{
					if (Item.Key == "{OriginalFormat}")
					{
						// state should be a FormattedLogValues instance here, which formats when ToString is called.
						string FormattedMessage = state.ToString();
						if (FormattedMessage != "[null]")
							Message.Content = FormattedMessage;
						continue;
					}

					if (Message.Data == null)
						Message.Data = new Dictionary<string, object?>();

					if (Item.Key == "{Data}")
					{
						AddDataToMessage(Message.Data, Item.Value);
						continue;
					}

					Message.Data[Item.Key] = Item.Value;
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

		private static (IDictionary<string, object?>? Data, IList<object?>? Scope) ParseScope(IEnumerable<object> scope)
		{
			IDictionary<string, object?>? Data = null;
			IList<object?>? Scope = null;

			foreach (object Item in scope)
			{
				if (Item is string)
				{
					if (Scope == null)
						Scope = new Collection<object?>();
					Scope.Add(Item);
				}
				else if (Item is IEnumerable<KeyValuePair<string, object?>> Dictionary)
				{
					if (Data == null)
						Data = new Dictionary<string, object?>();
					foreach (KeyValuePair<string, object?> SubItem in Dictionary)
					{
						if (SubItem.Key == "{OriginalFormat}")
						{
							if (Scope == null)
								Scope = new Collection<object?>();

							// Item should be a FormattedLogValues instance here, which formats when ToString is called.
							string FormattedMessage = Item.ToString();
							if (FormattedMessage == "[null]")
								Scope.Add(null);
							else
								Scope.Add(FormattedMessage);
							continue;
						}
						Data[SubItem.Key] = SubItem.Value;
					}
				}
				else if (Item.GetType().IsValueType)
				{
					if (Scope == null)
						Scope = new Collection<object?>();
					Scope.Add(Item);
				}
				else
				{
					if (Data == null)
						Data = new Dictionary<string, object?>();

					AddObjectPropertiesToMessageData(Data, Item);
				}
			}

			return (Data, Scope);
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
			PropertyDescriptorCollection ItemProperties = TypeDescriptor.GetProperties(value);

			foreach (PropertyDescriptor? ItemProperty in ItemProperties)
			{
				if (ItemProperty == null)
					continue;
				data[ItemProperty.Name] = ItemProperty.GetValue(value);
			}
		}

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
		/// Gets or sets the <see cref="LogLevel"/> of the message.
		/// </summary>
		[JsonConverter(typeof(JsonStringEnumMemberConverter))]
		public LogLevel? LogLevel { get; set; }

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
		public IList<object?>? Scope { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

		/// <summary>
		/// Gets or sets the data properties associated with the message.
		/// </summary>
		[JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only
		public IDictionary<string, object?>? Data { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
	}
}
