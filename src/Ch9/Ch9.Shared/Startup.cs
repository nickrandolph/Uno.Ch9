using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Ch9
{
	public static class Startup
	{
		//public void Initialize(Ioc ioc)
		//{
		//	//ioc.ConfigureServices(services =>
		//	//{
		//	//	InitializeHttpClient(services);
		//	//	InitializeBusinessServices(services);
		//	//});
		//}

		//public static IServiceCollection InitializeBusinessServices(this IServiceCollection serviceProvider)
		//{
		//	return serviceProvider.AddSingleton<IShowService, ShowService>();
		//}

		//public static IServiceCollection InitializeHttpClient(this IServiceCollection serviceProvider)
		//{
		//	return serviceProvider.AddTransient(s =>
		//	{
		//		var client = HttpUtility.CreateHttpClient();

		//		client.BaseAddress = new Uri("https://ch9-app.azurewebsites.net/");

		//		return client;
		//	});
		//}
	}
}
