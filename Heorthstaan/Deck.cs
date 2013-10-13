using System.Collections.Generic;

namespace Heorthstaan
{
	enum Class
	{
		// For neutral cards only, cannot apply to a deck
		Neutral,
		Druid,
		Hunter,
		Mage,
		Paladin,
		Priest,
		Rogue,
		Shaman,
		Warlock,
		Warrior,
	}

	class Deck
	{
		public string Path;
		public Class Class;
		// Card IDs
		public List<int> Cards;

		public Deck(string path, Class deckClass)
		{
			Path = path;
			Class = deckClass;
			Cards = new List<int>();
		}
	}
}
