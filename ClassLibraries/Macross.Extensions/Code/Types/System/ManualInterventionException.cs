using System.Runtime.Serialization;

namespace System
{
	/// <summary>
	/// Thrown when an issue is encountered requiring the attention of an admin.
	/// </summary>
	[Serializable]
	public class ManualInterventionException : Exception
	{
		/// <inheritdoc cref="Exception()" />
		public ManualInterventionException()
			: base()
		{
		}

		/// <inheritdoc cref="Exception(string)" />
		public ManualInterventionException(string message)
			: base(message)
		{
		}

		/// <inheritdoc cref="Exception(string, Exception)" />
		public ManualInterventionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <inheritdoc cref="Exception(SerializationInfo, StreamingContext)" />
		protected ManualInterventionException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
		}
	}
}
