using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ch9.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using Uno.Extensions.Navigation;

namespace Ch9.ViewModels
{
	[Windows.UI.Xaml.Data.Bindable]
	public class ShowPageViewModel : ObservableObject, IInitialise
    {
        public SourceFeed SourceFeed { get; set; }

        private ShowViewModel _show;
        public ShowViewModel Show
        {
            get => _show;
            set => SetProperty(ref _show, value);
        }

        private bool _isNarrowAndSelected;
        public bool IsNarrowAndSelected
        {
            get => _isNarrowAndSelected;
            set => SetProperty(ref _isNarrowAndSelected, value);
        }

        //public void OnNavigatedTo(SourceFeed sourceFeed)
        //{
        //    if (Show == null)
        //    {
        //        SourceFeed = sourceFeed;

        //        Show = new ShowViewModel(SourceFeed);
        //    }
        //}

		public bool TryHandleBackRequested()
		{
			if (Show.SelectedEpisode != null && IsNarrowAndSelected)
			{
				if (Show.IsVideoFullWindow)
				{
					Show.IsVideoFullWindow = false;
				}
				else
				{
					Show.DismissSelectedEpisode.Execute(null);
				}

				return true;
			}

			return false;
		}

		public Task Initialize(IDictionary<string, object> args)
		{
			var sourceFeed = args[TabRouter.ShowRouteParameter] as SourceFeed;
			if (Show == null)
			{
				SourceFeed = sourceFeed;

				Show = new ShowViewModel(SourceFeed);
			}

			return Task.CompletedTask;
		}
	}
}
