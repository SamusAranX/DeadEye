using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DeadEye.Helpers;

namespace DeadEye.Windows {
	public partial class ColorBrowserWindow: INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName = null) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public List<ColorWrapper> SystemColors { get; set; }
		public List<ColorWrapper> SystemParameters { get; set; }
		public List<ColorWrapper> BrushColors { get; set; }

		public ColorBrowserWindow() {
			this.InitializeComponent();

			this.SystemColors = new List<ColorWrapper>(this.GetBrushes(typeof(SystemColors)));
			this.SystemParameters = new List<ColorWrapper>(this.GetBrushes(typeof(SystemParameters)));
			this.BrushColors = new List<ColorWrapper>(this.GetBrushes(typeof(Brushes)));

			this.OnPropertyChanged("SystemColors");
			this.OnPropertyChanged("SystemParameters");
			this.OnPropertyChanged("BrushColors");
		}

		private void ColorBrowser_OnLoaded(object sender, RoutedEventArgs e) {
			//this.ColorList.SelectedIndex = 0;
		}

		private IEnumerable<ColorWrapper> GetBrushes(Type type) {
			var properties = type.GetProperties().OrderBy(p => p.Name);

			var brushList = new List<ColorWrapper>();
			foreach (var propInfo in properties) {
				if (!propInfo.Name.EndsWith("Brush") && !propInfo.PropertyType.IsSubclassOf(typeof(Brush)))
					continue;

				//if (!propInfo.PropertyType.IsSubclassOf(typeof(Brush)))
				//	continue;

				var resName = propInfo.Name;
				if (resName.EndsWith("Brush"))
					resName = resName.Remove(propInfo.Name.Length - 5);

				var brush = (Brush)propInfo.GetValue(null);

				brushList.Add(new ColorWrapper(resName, brush));
			}

			return brushList.ToArray();
		}
	}
}
