using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A factory class for constructing <see cref="DebugWindow"/> instances at runtime.
	/// </summary>
	/// <remarks>
	/// This class exists mostly for performance so <see cref="DebugWindow"/> instances are not created unless options dicticate and pre-conditions are satisfied.
	/// </remarks>
	public class DebugWindowFactory
	{
		private readonly IHostEnvironment _HostEnvironment;
		private readonly DebugWindowMessageManager _MessageManager;
		private readonly IOptionsMonitor<DebugWindowLoggerOptions> _Options;
		private readonly DebugWindowConfigureAction? _ConfigureAction;
		private readonly DebugWindowConfigureTabAction? _ConfigureTabAction;

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugWindowFactory"/> class.
		/// </summary>
		/// <param name="hostEnvironment"><see cref="IHostEnvironment"/>.</param>
		/// <param name="messageManager"><see cref="DebugWindowMessageManager"/>.</param>
		/// <param name="options"><see cref="DebugWindowLoggerOptions"/>.</param>
		/// <param name="configureAction"><see cref="DebugWindowConfigureAction"/>.</param>
		/// <param name="configureTabAction"><see cref="DebugWindowConfigureTabAction"/>.</param>
		public DebugWindowFactory(
			IHostEnvironment hostEnvironment,
			DebugWindowMessageManager messageManager,
			IOptionsMonitor<DebugWindowLoggerOptions> options,
			DebugWindowConfigureAction? configureAction = null,
			DebugWindowConfigureTabAction? configureTabAction = null)
		{
			_HostEnvironment = hostEnvironment;
			_MessageManager = messageManager;
			_Options = options;

			_ConfigureAction = configureAction;
			_ConfigureTabAction = configureTabAction;
		}

		/// <summary>
		/// Create a <see cref="DebugWindow"/> instance.
		/// </summary>
		/// <returns>Created <see cref="DebugWindow"/>.</returns>
		public DebugWindow Create() => new DebugWindow(_HostEnvironment, _MessageManager, _Options, _ConfigureAction, _ConfigureTabAction);
	}
}
