using System.Diagnostics;

using Macross.OpenTelemetry.Extensions;

namespace OpenTelemetry.Trace
{
	/// <summary>
	/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
	/// </summary>
	public static class TracerProviderBuilderExtensions
	{
		/// <summary>
		/// Adds a <see cref="BaseProcessor{T}"/> which will enrich <see cref="Activity"/> objects created while inside an <see cref="ActivityEnrichmentScope"/>.
		/// </summary>
		/// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
		/// <returns>Returns the supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
		public static TracerProviderBuilder AddActivityEnrichmentScopeProcessor(this TracerProviderBuilder tracerProviderBuilder)
#pragma warning disable CA2000 // Dispose objects before losing scope
			=> tracerProviderBuilder.AddProcessor(new ActivityEnrichmentScopeProcessor());
#pragma warning restore CA2000 // Dispose objects before losing scope
	}
}
