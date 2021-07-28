using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Ch9.ViewModels;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
#if !__MACOS__
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
#endif
using Uno.Extensions;
using Uno.Logging;
#if !__WASM__ && !__MACOS__
using Xamarin.Essentials;
#endif
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Ch9.Views;
using Uno.Extensions.Navigation.Messages;
using CommunityToolkit.Mvvm.Messaging;
using static Ch9.Shell;
using System.Threading.Tasks;

namespace Ch9
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		public static App Instance { get; private set; }


		private Shell _shell;
		private bool _isActivityBackgroundCleared;

		private IHost Host { get; }

		public App()
		{
			Instance = this;

			Host = UnoHost
#if __WASM__
                .CreateDefaultBuilderForWASM()
#else
			   .CreateDefaultBuilder()
#endif
			   //.UseConfigurationSectionInApp<CustomIntroduction>(nameof(CustomIntroduction))
			   .UseUnoLogging(logBuilder =>
			   {
				   logBuilder
						.SetMinimumLevel(LogLevel.Debug)
						.XamlLogLevel(LogLevel.Information)
						.XamlLayoutLogLevel(LogLevel.Information);
			   }
#if __WASM__
                    , new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider()
#endif
			   )
			   //.UseSerilog(true)
			   //.UseLocalization()
			   .UseRouting<RouterConfiguration, LaunchMessage>(() => _shell.ActiveFrame)
			   .ConfigureServices(services =>
			   {
				   services
					.AddSingleton<ITabManager>(sp => _shell)
					.AddSingleton<IRouter, TabRouter>(); // Override the default Router
			   }) 
			   .AddConfigurationSectionFromEntity(new EndpointOptions()
			   {
				   Url = "https://ch9-app.azurewebsites.net/",
				   Features = new Dictionary<string, bool>
				   {
					   { nameof(EndpointOptions.UseNativeHandler), true }
				   }
			   }, typeof(IShowService).Name)
			   .ConfigureServices((context, services) =>
			   {
				   _ = services
				   .AddNativeHandler(
#if __WASM__
				() => new Uno.UI.Wasm.WasmHttpHandler()
#endif
				)
				   .AddClient<IShowService, ShowService>(context, typeof(IShowService).Name);
			   })
			   .Build()
			   .EnableUnoLogging();
			Ioc.Default.ConfigureServices(Host.Services);

			// Uncomment this if you want to set a default theme.
			// this.RequestedTheme = ApplicationTheme.Dark;

			ConfigureFilters(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

			this.InitializeComponent();
			ConfigureSuspension();

#if !DEBUG
			AnalyticsService.Initialize();
#endif
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			this.Resources.MergedDictionaries.Add(new Uno.Material.MaterialColorPalette());
			this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("ms-appx:///Views/Styles/Application/Colors.xaml") });
			this.Resources.MergedDictionaries.Add(new Uno.Material.MaterialResources());
			this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("ms-appx:///Views/Styles/Styles.xaml") });

#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif
			_shell = Windows.UI.Xaml.Window.Current.Content as Shell;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (_shell == null)
			{
				//_startup.Initialize(Ioc.Default);

				ConfigureViewSize();
				ConfigureStatusBar();

				_shell = new Shell();

				ConfigureOrientation();
				ConfigureEscapeKey();

				// Place the frame in the current Window
				Windows.UI.Xaml.Window.Current.Content = _shell;
			}

			if (e.PrelaunchActivated == false)
			{
				// Ensure the current window is active
				Windows.UI.Xaml.Window.Current.Activate();
			}

			_ = Task.Run(() =>
			{
				//Startup.Start();
				Host.Run();
			});
		}

		#region Application configuration
		private void ConfigureSuspension()
		{
			this.Suspending += OnSuspending;

			void OnSuspending(object sender, SuspendingEventArgs e)
			{
				var deferral = e.SuspendingOperation.GetDeferral();

				deferral.Complete();
			}
		}

		private void ConfigureViewSize()
		{
#if WINDOWS_UWP
			ApplicationView.PreferredLaunchViewSize = new Size(1330, 768);
			ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 480));
#endif
		}

		private void ConfigureEscapeKey()
		{
#if WINDOWS_UWP
			Window.Current.CoreWindow.CharacterReceived += CoreWindowCharacterReceived;

			void CoreWindowCharacterReceived(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.CharacterReceivedEventArgs args)
			{
				if (args.KeyCode == 27) // Escape key
				{
					if (_shell.TryGetActiveViewModel<ShowPageViewModel>(out var showPage) && showPage.Show.IsVideoFullWindow)
					{
						showPage.Show.IsVideoFullWindow = false;
					}
					else if (_shell.TryGetActiveViewModel<RecentEpisodesPageViewModel>(out var recentEpisodesPage) && recentEpisodesPage.Show.IsVideoFullWindow)
					{
						recentEpisodesPage.Show.IsVideoFullWindow = false;
					}
				}
			}
#endif
		}

		private void ConfigureStatusBar()
		{
			var resources = Windows.UI.Xaml.Application.Current.Resources;

#if WINDOWS_UWP || __WASM__ || __MACOS__
			var hasStatusBar = false;
#else
			var hasStatusBar = true;

			StatusBar.GetForCurrentView().ForegroundColor = this.RequestedTheme == ApplicationTheme.Dark
				? Windows.UI.Colors.White
				: Windows.UI.Colors.Black;
#endif

			var statusBarHeight = hasStatusBar ? Windows.UI.ViewManagement.StatusBar.GetForCurrentView().OccludedRect.Height : 0;

			resources.Add("StatusBarDouble", (double)statusBarHeight);
			resources.Add("StatusBarThickness", new Thickness(0, statusBarHeight, 0, 0));
			resources.Add("StatusBarGridLength", new GridLength(statusBarHeight, GridUnitType.Pixel));

#if __IOS__
			// This is the actual height of the CommandBar on iOS: https://platform.uno/docs/articles/controls/CommandBar.html
			var commandBarHeight = 44;

			// This is just below the status bar.
			var bounds = ApplicationView.GetForCurrentView().VisibleBounds;

			// We've seen cases where the Top is reported as 0 when it's actually 20.
			var top = Math.Max((double)bounds.Top, 20);

			resources.Add("CommandBarHeight", top + commandBarHeight);
#endif
		}

		private void ConfigureOrientation()
		{
#if !__WASM__ && !__MACOS__
			if (DeviceInfo.Idiom == DeviceIdiom.Phone)
			{
				DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
			}
#endif

			var simpleOrientationSensor = SimpleOrientationSensor.GetDefault();

			if (simpleOrientationSensor != null)
			{
				simpleOrientationSensor.OrientationChanged += OrientationChanged;
			}

			void OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
			{
				try
				{
					var isLandscape = args.Orientation.IsOneOf(
						SimpleOrientation.Rotated270DegreesCounterclockwise,
						SimpleOrientation.Rotated90DegreesCounterclockwise
					);

					if (_shell.TryGetActiveViewModel<ShowPageViewModel>(out var showPage) && showPage.Show.SelectedEpisode != null)
					{
						ToVideoFullWindow(showPage.Show, isLandscape);
					}
					else if (_shell.TryGetActiveViewModel<RecentEpisodesPageViewModel>(out var recentEpisodesPage) && recentEpisodesPage.Show.SelectedEpisode != null)
					{
						ToVideoFullWindow(recentEpisodesPage.Show, isLandscape);
					}
				}
				catch (Exception ex)
				{
					this.Log().ErrorIfEnabled(() => $"Error in OrientationChanged subscription: {ex}");
				}
			}
		}

		private void ConfigureFilters(ILoggerFactory factory)
		{
			//			factory
			//				.WithFilter(new FilterLoggerSettings
			//					{
			//						{ "Uno", Microsoft.Extensions.Logging.LogLevel.Warning },
			//						{ "Windows", Microsoft.Extensions.Logging.LogLevel.Warning },

			//						// Debug JS interop
			//						// { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

			//						// Generic Xaml events
			//						// { "Windows.UI.Xaml", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

			//						// Layouter specific messages
			//						// { "Windows.UI.Xaml.Controls", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
			//						// { "Windows.Storage", LogLevel.Debug },

			//						// Binding related messages
			//						// { "Windows.UI.Xaml.Data", LogLevel.Debug },

			//						// DependencyObject memory references tracking
			//						// { "ReferenceHolder", LogLevel.Debug },

			//						// ListView-related messages
			//						// { "Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.ListView", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.GridView", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
			//						// { "Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
			//						// { "Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
			//						// { "Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
			//						// { "Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
			//						// { "Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
			//					}
			//				)
			//#if DEBUG
			//				.AddConsole(Microsoft.Extensions.Logging.LogLevel.Debug);
			//#else
			//				.AddConsole(Microsoft.Extensions.Logging.LogLevel.Information);
			//#endif
		}
		#endregion

		public void OnFullscreenChanged(bool isFullscreen)
		{
#if __ANDROID__
			// This will reset the window background from the splashscreen to a black background.
			if (isFullscreen && !_isActivityBackgroundCleared)
			{
				(ContextHelper.Current as Android.App.Activity).Window.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Black));
				_isActivityBackgroundCleared = true;
			}
#endif

#if !__WASM__ && !__MACOS__
			if (DeviceInfo.Idiom == DeviceIdiom.Phone)
			{
				if (isFullscreen)
				{
					if (DisplayInformation.AutoRotationPreferences == DisplayOrientations.None)
					{
						return;
					}

					DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
				}
				else
				{
					DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
				}
			}
#endif
		}

		private void ToVideoFullWindow(ShowViewModel viewModel, bool isLandscape)
		{
			if (viewModel != null)
			{
				// Set display orientation to none, thus screen can handle LandscapeFlipped
				DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
				viewModel.IsVideoFullWindow = isLandscape;
			}
		}
	}



	/// <summary>
	/// This class is used for navigation configuration.
	/// - Configures the navigator.
	/// </summary>
	public class RouterConfiguration : IRouteDefinitions
	{
		public IReadOnlyDictionary<string, (Type, Type)> Routes { get; } = new Dictionary<string, (Type, Type)>()
						.RegisterPage<AboutPageViewModel, AboutPage>()
						.RegisterPage<EpisodeViewModel,EpisodeContent >()
			.RegisterPage<RecentEpisodesPageViewModel,RecentEpisodesPage>()
			.RegisterPage<ShowPageViewModel,ShowPage>()
			.RegisterPage<ShowsPageViewModel,ShowsPage>("");

	}


	//public class RouterRedirection : IRouteRedirection
	//{
	//	private IServiceProvider Services { get; }
	//	public RouterRedirection(IServiceProvider services)
	//	{
	//		Services = services;
	//		Redirection =
	//			(stack, route, args) => {
	//				if (route != "" && route != "/") return route;
	//				var onboarding = Services.GetService<IWritableOptions<ApplicationSettings>>();
	//				if (!(onboarding?.Value.IsOnboardingCompleted ?? false))
	//				{
	//					return typeof(OnboardingPageViewModel).AsRoute();
	//				}
	//				else
	//				{
	//					return typeof(WelcomePageViewModel).AsRoute();
	//				}
	//			};
	//	}


	//	public Func<
	//			string[],                       // navigation stack
	//			string,                         // new path
	//			IDictionary<string, object>,    // args
	//			string                          // relative path
	//			> Redirection
	//	{ get; }


	//}

	public interface ITabManager
	{
		event EventHandler TabsBuilt;
		event EventHandler ActiveTabChanged;
	}

	public class TabRouter : Router
	{
		public const string TabTypeRouteParameter = "TabType";
		public const string ShowRouteParameter = "Show";
		private Shell _shell;
		public TabRouter(
			ITabManager tabManager,
		   ILogger<Router> logger,
		   INavigator navigator,
		   IMessenger messenger,
		   IRouteDefinitions routeDefinitions,
		   IServiceProvider services,
		   IRouteRedirection redirection = default
		   ) : base(logger, navigator, messenger, routeDefinitions, services, redirection)
		{
			tabManager.TabsBuilt += TabBuilder_TabsBuilt;
			tabManager.ActiveTabChanged += TabManager_ActiveTabChanged;

			_shell = tabManager as Shell;
			foreach (var tab in _shell.Tabs)
			{
				TabNavigationStack[tab.Key] = new Stack<string>();
				TabNavigationViewModelInstances[tab.Key] = new Stack<object>();
			}
		}

		private void TabManager_ActiveTabChanged(object sender, EventArgs e)
		{
			NavigationStack = TabNavigationStack[_shell.ActiveTabType];
			NavigationViewModelInstances = TabNavigationViewModelInstances[_shell.ActiveTabType];
		}

		private IDictionary<TabType, Stack<string>> TabNavigationStack { get; } = new Dictionary<TabType, Stack<string>>();

		private IDictionary<TabType, Stack<object>> TabNavigationViewModelInstances { get; } = new Dictionary<TabType, Stack<object>>();

		private void TabBuilder_TabsBuilt(object sender, EventArgs e)
		{
			_shell = sender as Shell;
			foreach (var tab in _shell.Tabs)
			{
				TabNavigationStack[tab.Key] = new Stack<string>();
				TabNavigationViewModelInstances[tab.Key] = new Stack<object>();
			}
		}

		protected override async Task<RoutingMessage> Preprocess(RoutingMessage message)
		{
			if(message is LaunchMessage)
			{
				message = message with { Args = new Dictionary<string, object> { { TabTypeRouteParameter, TabType.Shows } } };
			}

			if(message.Args.TryGetValue(TabTypeRouteParameter,out var tabTypeArg))
			{
				var tabType = (TabType)tabTypeArg;

				var taskComplete = new TaskCompletionSource<bool>();
				_ = _shell.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
				{
					_shell.UpdateActiveTab(tabType);
					taskComplete.SetResult(true);	
				});

				await taskComplete.Task;
			}

			return await base.Preprocess(message);
		}


	}
}
