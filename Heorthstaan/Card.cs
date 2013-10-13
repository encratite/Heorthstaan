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
		Weapon,
		Hero,
	}

	class Card
	{
		public int Id;
		public string Name;
		public string Description;
		// Class is null for neutral cards
		public Class Class;
		public CardRarity Rarity;
		public CardType Type;
		public int ManaCost;
		// Attack and hit points are only specified for minion cards
		public int Attack;
		public int HitPoints;

		public Card(int id, string name, string description, Class cardClass, CardRarity rarity, CardType type, int manaCost, int attack, int hitPoints)
		{
			Id = id;
			Name = name;
			Description = description;
			Class = cardClass;
			Rarity = rarity;
			Type = type;
			ManaCost = manaCost;
			Attack = attack;
			HitPoints = hitPoints;
		}
	}
}
