﻿using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Text;
using Ch9.Services;

namespace Ch9
{
	public class Startup
	{
		public void Initialize(ISimpleIoc serviceProvider)
		{
			InitializeNavigationService(serviceProvider);
			InitializePostsService(serviceProvider);
		}

		private void InitializeNavigationService(ISimpleIoc serviceProvider)
		{
			serviceProvider.Register<IStackNavigationService>(() =>
			{
				var navigationService = new NavigationService();

				navigationService.Configure(nameof(MainPage), typeof(MainPage));
				navigationService.Configure(nameof(AboutPage), typeof(AboutPage));

				return new StackNavigationService(navigationService);
			});
		}

		private void InitializePostsService(ISimpleIoc serviceProvider)
		{
			serviceProvider.Register<IEpisodeService>(() => new EpisodeService());
            serviceProvider.Register<IShowService>(() => new ShowService());
		}
	}
}
