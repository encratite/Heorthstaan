using System.Collections.Generic;

namespace Heorthstaan
{
	enum Class
	{
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
		public List<Card> Cards;

		public Deck(string path, Class deckClass)
		{
			Path = path;
			Class = deckClass;
			Cards = new List<Card>();
		}
	}
}
