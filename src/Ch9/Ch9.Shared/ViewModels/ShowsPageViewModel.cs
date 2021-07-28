using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Ch9.Domain;
using Ch9.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
using static Ch9.Shell;

namespace Ch9.ViewModels
{
	[Windows.UI.Xaml.Data.Bindable]
	public class ShowsPageViewModel : ObservableObject
	{
		private IShowService _showService;
		public ShowsPageViewModel(IShowService showService, IRouteMessenger messenger)
		{
			_showService = showService;
			DisplayShow = new RelayCommand<SourceFeed>(showFeed =>
			{
				messenger.Send(new RoutingMessage(this, typeof(ShowPageViewModel).AsRoute(),
					new Dictionary<string, object> {
						{ TabRouter.TabTypeRouteParameter, TabType.Shows },
						{TabRouter.ShowRouteParameter, showFeed }
					}));
				//Shell.Instance.NavigateTo(typeof(ShowPage), showFeed);
			});

			ReloadShowsList = new RelayCommand(LoadShowFeeds);

			//LoadShowFeeds();
		}

		public ICommand ReloadShowsList { get; }

		public ICommand DisplayShow { get; set; }

		private TaskNotifier<IEnumerable<ShowItemViewModel>> _shows;
		public Task<IEnumerable<ShowItemViewModel>> Shows
		{
			get => _shows;
			set
			{
				SetPropertyAndNotifyOnCompletion(ref _shows, value, task =>
				{
					OnPropertyChanged(nameof(ShowsResult));
				});
			}
		}

		public IEnumerable<ShowItemViewModel> ShowsResult =>
	((Task)_shows)?.Status == TaskStatus.RanToCompletion
	? ((Task<IEnumerable<ShowItemViewModel>>)_shows).Result
	: null;

		public void LoadShowFeeds()
		{
			if (Shows != null) { return; }
			async Task<IEnumerable<ShowItemViewModel>> GetShowFeeds()
			{
				return await Task.Run(async () =>
				{
					var showFeeds = await _showService.GetShowFeeds();
					return showFeeds
						.OrderBy(s => s.Name)
						.Select(s => new ShowItemViewModel(this, s))
						.ToArray();
				});

			}

			Shows = GetShowFeeds();
		}
	}

	public class ShowItemViewModel
	{
		public ObservableObject Parent { get; set; }

		public SourceFeed Show { get; set; }

		public ShowItemViewModel(ObservableObject parent, SourceFeed show)
		{
			Parent = parent;
			Show = show;
		}
	}
}
