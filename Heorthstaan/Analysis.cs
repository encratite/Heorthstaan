using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using NDatabase;
using NDatabase.Api;

namespace Heorthstaan
{
	class Analysis : IDisposable
	{
		IOdb Database;
		FileStream Output;

		public Analysis(string databasePath, string outputPath)
		{
			Database = OdbFactory.Open(databasePath);
			Output = File.Open(outputPath, FileMode.Truncate);
		}

		public void Dispose()
		{
			Output.Dispose();
			Database.Dispose();
		}

		public void Run()
		{
			Dictionary<int, Card> cardMap = new Dictionary<int, Card>();
			var cards = Database.QueryAndExecute<Card>();
			var decks = Database.QueryAndExecute<Deck>();
			foreach (var card in cards)
				cardMap[card.Id] = card;
			Dictionary<int, int> manaMap = new Dictionary<int, int>();
			Dictionary<Class, Dictionary<Card, int>> cardFrequency = new Dictionary<Class, Dictionary<Card, int>>();
			int cardCount = 0;
			int counter = 1;
			foreach (var deck in decks)
			{
				// Console.WriteLine("Processing deck {0}/{1}", counter, decks.Count);
				foreach (var cardId in deck.Cards)
				{
					Card card;
					if (!cardMap.TryGetValue(cardId, out card))
						throw new Exception("Invalid card ID");
					card.Name = WebUtility.HtmlDecode(card.Name);
					if (card.Name == "DEBUG")
						continue;
					int manaCost = card.ManaCost;
					if (!manaMap.ContainsKey(manaCost))
						manaMap[manaCost] = 0;
					manaMap[manaCost]++;
					Dictionary<Card, int> cardFrequencyMap;
					if(!cardFrequency.TryGetValue(card.Class, out cardFrequencyMap))
					{
						cardFrequencyMap = new Dictionary<Card, int>();
						cardFrequency[card.Class] = cardFrequencyMap;
					}
					if (!cardFrequencyMap.ContainsKey(card))
						cardFrequencyMap[card] = 0;
					cardFrequencyMap[card]++;
					cardCount++;
				}
				counter++;
			}
			const int cardsPerDeck = 30;
			const int manaLimit = 10;
			const int offset = 15;
			using (StreamWriter writer = new StreamWriter(Output))
			{
				writer.WriteLine("Analysed {0} decks, detected {1} different cards\n", decks.Count, cards.Count);
				writer.WriteLine("Mana cost distribution:");
				for(int mana = 0; mana <= manaLimit; mana++)
				{
					double unroundedCount = ((double)manaMap[mana] / cardCount) * cardsPerDeck;
					int count = (int)Math.Round(unroundedCount);
					writer.WriteLine("{0}: {1}{2} {3}", mana.ToString().PadLeft(2, ' '), new string('=', count), new string(' ', offset - count), count);
				}
				writer.WriteLine("");
				var classes = cardFrequency.Keys.ToList();
				classes.Remove(Class.Neutral);
				classes.Sort((x, y) => x.ToString().CompareTo(y.ToString()));
				classes.Insert(0, Class.Neutral);
				foreach (var cardClass in classes)
				{
					writer.WriteLine("Most common {0} cards:", cardClass);
					List<Tuple<Card, int>> pairs = new List<Tuple<Card, int>>();
					Dictionary<Card, int> cardFrequencyMap = cardFrequency[cardClass];
					foreach (var pair in cardFrequencyMap)
						pairs.Add(new Tuple<Card, int>(pair.Key, pair.Value));
					pairs.Sort((x, y) => y.Item2.CompareTo(x.Item2));
					int setSize = 0;
					foreach (var pair in pairs)
						setSize += pair.Item2;
					int cardCounter = 1;
					foreach (var pair in pairs)
					{
						double percentage = (double)pair.Item2 / setSize * 100.0;
						writer.WriteLine("{0}. {1} ({2:0.0}%)", cardCounter, pair.Item1.Name, percentage);
						cardCounter++;
					}
					writer.WriteLine("");
				}
			}
		}
	}
}
