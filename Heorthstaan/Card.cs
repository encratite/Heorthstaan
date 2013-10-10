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

		Card(string name, Class? cardClass, CardRarity rarity, CardType type, int manaCost, int? attack, int? hitPoints)
		{
			Name = name;
			Class = cardClass;
			Rarity = rarity;
			Type = type;
			ManaCost = manaCost;
			Attack = attack;
			HitPoints = hitPoints;
		}

		public static Card Minion(string name, Class? cardClass, CardRarity rarity, int manaCost, int attack, int hitPoints)
		{
			return new Card(name, cardClass, rarity, CardType.Minion, manaCost, attack, hitPoints);
		}

		public static Card Ability(string name, Class? cardClass, CardRarity rarity, int manaCost)
		{
			return new Card(name, cardClass, rarity, CardType.Ability, manaCost, null, null);
		}
	}
}
