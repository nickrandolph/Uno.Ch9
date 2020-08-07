﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Ch9.ViewModels;
using Ch9.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Ch9
{
	public sealed partial class Shell : UserControl
	{
		private enum TabType
		{
			Recent,
			Shows,
			About
		}

		private readonly Dictionary<TabType, Frame> _frames = new Dictionary<TabType, Frame>();

		private readonly Dictionary<TabType, Type> _tabs = new Dictionary<TabType, Type>
		{
			{ TabType.Recent, typeof(RecentEpisodesPage) },
			{ TabType.Shows, typeof(ShowsPage) },
			{ TabType.About, typeof(AboutPage) },
		};

		private NavigationViewItem _activeTab;
		private Frame _activeFrame;

		public Shell()
		{
			this.InitializeComponent();

			Instance = this;

			InitializeSafeArea();

			this.Loaded += OnLoaded;

			SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
		}

		public static Shell Instance { get; private set; }

		public NavigationView NavigationView => this.RootNavigationView;

		public EventHandler<Frame> Navigated;

		public void NavigateTo(string tabKey, Type pageType = null, object parameter = null)
		{
			var item = NavigationView
				.MenuItems
				.Select(s => s as NavigationViewItem)
				.Single(s => s.Tag.ToString() == tabKey);

			NavigateTo(item, pageType, parameter);
		}

		public void NavigateTo(Type pageType, object parameter)
		{
			NavigateTo(_activeTab, pageType, parameter);
		}

		public bool TryGetActiveViewModel<TViewModel>(out TViewModel viewModel)
		{
			var dataContext = (_activeFrame?.Content as FrameworkElement)?.DataContext;

			if (dataContext is TViewModel model)
			{
				viewModel = model;

				return true;
			}

			viewModel = default(TViewModel);

			return false;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			InitializeTabs();
		}

		private void InitializeSafeArea()
		{
			var full = Windows.UI.Xaml.Window.Current.Bounds;
			var bounds = ApplicationView.GetForCurrentView().VisibleBounds;

			var topPadding = Math.Abs(full.Top - bounds.Top);

			if (topPadding > 0)
			{
				TopPaddingRow.Height = new GridLength(topPadding);
			}
		}

		private void InitializeTabs()
		{
			if (NavigationView.MenuItems.Count == 0)
			{
				foreach (var tab in _tabs)
				{
					NavigationView.MenuItems.Add(ConstructNavigationViewItem(tab));
				}

				NavigationView.ItemInvoked += OnNavigationViewItemInvoked;
				NavigationView.BackRequested += OnNavigationViewBackRequested;

				var initialSelection = (NavigationViewItem)NavigationView.MenuItems.First();
				NavigateTo(initialSelection);
			}
		}

		private NavigationViewItem ConstructNavigationViewItem(KeyValuePair<TabType, Type> tab)
		{
			switch (tab.Key)
			{
				case TabType.Recent:
					return new NavigationViewItem { Content = tab.Key, Tag = tab.Key, Icon = new SymbolIcon(Symbol.Video) };
				case TabType.Shows:
					return new NavigationViewItem { Content = tab.Key, Tag = tab.Key, Icon = new SymbolIcon(Symbol.VideoChat) };
				case TabType.About:
					return new NavigationViewItem { Content = tab.Key, Tag = tab.Key, Icon = new SymbolIcon(Symbol.Important) };
				default:
					throw new ArgumentException("This tab does not exist");
			}
		}

		private void NavigateTo(NavigationViewItem item, Type pageType = null, object parameter = null)
		{
			var tabKey = item.Tag is TabType ? (TabType) item.Tag : TabType.Recent;

			if (item == _activeTab && pageType == null)
			{
				return;
			}

			if (_tabs.TryGetValue(tabKey, out var rootPageType))
			{
				var targetPageType = pageType ?? rootPageType;

				if (!_frames.TryGetValue(tabKey, out var frame))
				{
					frame = new Frame();
					this.RootContent.Children.Add(frame);
					_frames.Add(tabKey, frame);
				}

				if (frame.Content == null)
				{
					frame.Navigate(targetPageType, parameter);
				}
				else if (frame.Content.GetType() != targetPageType)
				{
					frame.Navigate(targetPageType, parameter);

					if (targetPageType == rootPageType)
					{
						// Go back to the root page.
						var backStack = frame.BackStackDepth;

						for (var i = 0; i < backStack; i++)
						{
							frame.GoBack();
						}
					}
				}

				foreach (var f in _frames.Where(x => x.Value != frame).Select(x => x.Value))
				{
					f.Visibility = Visibility.Collapsed;
				}

				frame.Visibility = Visibility.Visible;

				NavigationView.SelectedItem = item;

				UpdateBackButtonVisibility();

				_activeTab = item;
				_activeFrame = frame;

				Navigated?.Invoke(this, frame);
			}
		}

		private void OnNavigationViewBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{

			if (_activeFrame?.CanGoBack ?? false)
			{
				_activeFrame.GoBack();

				UpdateBackButtonVisibility();

				Navigated?.Invoke(this, _activeFrame);
			}
		}

		private void OnNavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (args.InvokedItemContainer is NavigationViewItem item)
			{
				NavigateTo(item);
			}
		}

		private void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			if (TryGetActiveViewModel<ShowPageViewModel>(out var showPage) && showPage.TryHandleBackRequested())
			{
				e.Handled = true;

				return;
			}

			if (TryGetActiveViewModel<RecentEpisodesPageViewModel>(out var recentEpisodesPage) && recentEpisodesPage.TryHandleBackRequested())
			{
				e.Handled = true;

				return;
			}

			if (_activeFrame?.CanGoBack ?? false)
			{
				e.Handled = true;

				_activeFrame.GoBack();

				UpdateBackButtonVisibility();

				Navigated?.Invoke(this, _activeFrame);
			}
		}

		private void UpdateBackButtonVisibility()
		{
			if (_activeFrame != null)
			{
				NavigationView.IsBackButtonVisible = _activeFrame.CanGoBack
						   ? NavigationViewBackButtonVisible.Visible
						   : NavigationViewBackButtonVisible.Collapsed;

#if __WASM__
				// This is needed on WASM to intercept the system back.
				SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = _activeFrame.CanGoBack
					? AppViewBackButtonVisibility.Visible
					: AppViewBackButtonVisibility.Collapsed;
#endif
			}
		}
	}
}