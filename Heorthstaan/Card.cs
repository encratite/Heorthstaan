namespace Heorthstaan
{
	enum CardRarity
	{
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary,
	}

	enum CardType
	{
		Minion,
		Ability,
	}

	class Card
	{
		public string Name;
		// Class is null for neutral cards
		public Class? Class;
		public CardRarity Rarity;
		public CardType Type;
		public int ManaCost;
		// Attack and hit points are only specified for minion cards
		public int? Attack;
		public int? HitPoints;
	}
}
