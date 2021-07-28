using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Ch9
{
	public class FromEmptyStringOrNullObjectToValueConverter : IValueConverter
	{
		public object EmptyOrNullValue { get; set; }

		public object NotEmptyOrNullValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is string str && string.IsNullOrEmpty(str))
			{
				return EmptyOrNullValue;
			}

			if (value == null || value == DependencyProperty.UnsetValue)
			{
				return EmptyOrNullValue;
			}

			return NotEmptyOrNullValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}

	public class FromTaskStatusToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if(value is Task task)
			{
				var status = Enum.Parse<TaskStatus>(parameter+"");
				return task.Status==status?Visibility.Visible : Visibility.Collapsed;
			}

			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
