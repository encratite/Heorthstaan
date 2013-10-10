using System;

namespace Heorthstaan
{
	class DeckLoaderException: Exception
	{
		public DeckLoaderException(string message) :
			base(message)
		{ }

		public DeckLoaderException(string message, Exception exception) :
			base(message, exception)
		{ }
	}
}
