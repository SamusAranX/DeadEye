using System.Collections.Generic;

namespace DeadEye.Helpers {
	public class TabItemWrapper {
		public TabItemWrapper(string header, IEnumerable<ColorWrapper> colors) {
			this.TabHeader = header;
			this.Colors = new List<ColorWrapper>(colors);
		}

		public string TabHeader { get; }
		public List<ColorWrapper> Colors { get; }
	}
}