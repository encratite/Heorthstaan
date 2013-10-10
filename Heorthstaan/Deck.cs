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
		public string Identifier;
		public Class Class;
		public List<Card> Cards;
	}
}
