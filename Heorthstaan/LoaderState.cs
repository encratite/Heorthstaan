using System.Collections.Generic;

namespace Heorthstaan
{
	class LoaderState
	{
		public HashSet<int> ProcessedPages;

		public LoaderState()
		{
			ProcessedPages = new HashSet<int>();
		}
	}
}
