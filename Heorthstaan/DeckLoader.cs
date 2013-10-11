using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

using HtmlAgilityPack;
using NDatabase;
using NDatabase.Api;

namespace Heorthstaan
{
	class DeckLoader: IDisposable
	{
		const string DatabasePath = "Heorthstaan.ndb";
		const string WebSite = "http://www.hearthpwn.com";

		IOdb Database;

		LoaderState LoaderState;

		int ThreadCount;
		int PageCount;

		HashSet<int> UnprocessedPages;

		public DeckLoader(int threadCount)
		{
			Database = OdbFactory.Open(DatabasePath);
			ThreadCount = threadCount;
		}

		public void Dispose()
		{
			Database.Dispose();
		}

		public void Load()
		{
			PageCount = GetPageCount();
			LoaderState = Database.QueryAndExecute<LoaderState>().GetFirst();
			if (LoaderState == null)
				LoaderState = new LoaderState();
			UnprocessedPages = new HashSet<int>();
			for(int page = 1; page <= PageCount; page++)
			{
				if (!LoaderState.ProcessedPages.Contains(page))
					UnprocessedPages.Add(page);
			}
			List<Thread> threads = new List<Thread>();
			for (int i = 1; i <= ThreadCount; i++)
			{
				Thread thread = new Thread(RunWorker);
				thread.Name = string.Format("Worker {0}", i);
				threads.Add(thread);
				thread.Start();
			}
			foreach(Thread thread in threads)
				thread.Join();
		}

		HtmlDocument GetDocument(string path)
		{
			using (WebClient client = new WebClient())
			{
				string content = client.DownloadString(WebSite + path);
				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(content);
				return document;
			}
		}

		bool Matches(string pattern, string input)
		{
			Regex regex = new Regex(pattern);
			return regex.IsMatch(input);
		}

		int GetInteger(string input)
		{
			try
			{
				return Int32.Parse(input);
			}
			catch(Exception exception)
			{
				throw new DeckLoaderException("Unable to parse integer", exception);
			}
		}

		int GetPageCount()
		{
			HtmlDocument document = GetDocument("/decks");
			var links = document.DocumentNode.SelectNodes("//a[@class = 'b-pagination-item' and @href]");
			var pageLinks = links.Where(x => Matches("^\\/decks\\?.*?page=\\d+$", x.Attributes["href"].Value));
			if (pageLinks.Count() == 0)
				throw new DeckLoaderException("Unable to locate page links");
			HtmlNode lastLink = pageLinks.Last();
			int pageCount = GetInteger(lastLink.InnerText);
			return pageCount;
		}

		void Print(string message, params object[] arguments)
		{
			string formattedMessage = string.Format(message, arguments);
			lock (Console.Out)
			{
				Console.WriteLine("[{0}] {1}", Thread.CurrentThread.Name, formattedMessage);
			}
		}

		void RunWorker()
		{
			Print("Launched worker");
			while(true)
			{
				int? page = null;
				if(!GetJobPage(ref page))
				{
					Print("No pages left to be processed, terminating");
					return;
				}
				ProcessPage(page.Value);
			}
		}

		bool GetJobPage(ref int? output)
		{
			lock(UnprocessedPages)
			{
				if (UnprocessedPages.Count == 0)
					return false;
				for (int page = 1; page <= PageCount; page++)
				{
					if(UnprocessedPages.Contains(page))
					{
						output = page;
						UnprocessedPages.Remove(page);
						return true;
					}
				}
				throw new DeckLoaderException("Unable to get job");
			}
		}

		void ProcessPage(int page)
		{
			string path = string.Format("/decks?page={0}", page);
			Print("Processing page {0}", path);
			HtmlDocument document = GetDocument(path);
			var links = document.DocumentNode.SelectNodes("//a[starts-with(@href, '/decks/') and starts-with(@class, 'hsdeck hsdeck-')]");
			foreach (var link in links)
			{
				string deckPath = link.Attributes["href"].Value;
				ProcessDeck(deckPath);
			}
			/*
			lock(LoaderState)
			{
				LoaderState.ProcessedPages.Add(page);
				lock (Database)
				{
					Database.Store(LoaderState);
					Database.Commit();
				}
			}
			*/
		}

		Class GetClass(string classString)
		{
			try
			{
				return (Class)Enum.Parse(typeof(Class), classString);
			}
			catch (ArgumentException exception)
			{
				throw new DeckLoaderException("Unable to parse deck class", exception);
			}
		}

		bool DeckIsInDatabase(string path)
		{
			var deck = from x in Database.AsQueryable<Deck>()
					   where x.Path == path
					   select x;
			return deck.Count() > 0;
		}

		CardType GetCardType(string input)
		{ 
			switch(input)
			{
				case "Minion":
					return CardType.Minion;

				case "Ability":
					return CardType.Ability;

				default:
					throw new DeckLoaderException("Unable to parse card type");
			}
		}

		void ProcessDeck(string path)
		{
			lock (Database)
			{
				if (DeckIsInDatabase(path))
				{
					Print("Skipping deck {0}", path);
					return;
				}
			}
			Print("Processing deck {0}", path);
			HtmlDocument document = GetDocument(path);
			var selection = document.DocumentNode.SelectSingleNode("//li[@class = 'b-breadcrumb-item'][2]//span");
			if (selection == null)
				throw new DeckLoaderException("Unable to determine class of deck");
			Class deckClass = GetClass(selection.InnerText);
			var rows = document.DocumentNode.SelectNodes("//tr[@class = 'even' or @class = 'odd']");
			if(rows.Count == 0)
				throw new DeckLoaderException("Unable to detect cards in deck");
			Deck deck = new Deck(path, deckClass);
			foreach (var row in rows)
			{
				Card card = ParseCard(row);
				deck.Cards.Add(card);
			}
		}

		Card ParseCard(HtmlNode row)
		{
			var link = row.SelectSingleNode(".//a[@href and @class]");
			if (link == null)
				throw new DeckLoaderException("Unable to retrieve ID and name of card");
			Regex idPattern = new Regex("\\/cards\\/(\\d+)-.+?");
			Match idMatch = idPattern.Match(link.OuterHtml);
			if (!idMatch.Success)
				throw new DeckLoaderException("Unable to extract card ID");
			int id = Convert.ToInt32(idMatch.Groups[1].Value);
			string name = link.InnerText;
			string rarityString = link.Attributes["class"].Value;
			Regex rarityPattern = new Regex("\\d+");
			Match rarityMatch = rarityPattern.Match(rarityString);
			if (!rarityMatch.Success)
				throw new DeckLoaderException("Unable to extract card rarity");
			int rarityIndex = Convert.ToInt32(rarityMatch.Groups[0].Value) - 1;
			var rarityValues = Enum.GetValues(typeof(CardRarity));
			if (rarityIndex >= rarityValues.Length)
				throw new DeckLoaderException("Invalid card rarity specified");
			CardRarity rarity = (CardRarity)rarityValues.GetValue(rarityIndex);
			var paragraph = row.SelectSingleNode(".//p");
			if (paragraph == null)
				throw new DeckLoaderException("Unable to extract card description");
			string description = paragraph.InnerText.Trim();
			Class? cardClass = null;
			var classCell = row.SelectSingleNode(".//span[starts-with(@class, 'class-')]");
			if (classCell != null)
			{
				string lowerCaseString = classCell.Attributes["class"].Value.Substring("class-".Length);
				if (lowerCaseString.Length == 0)
					throw new DeckLoaderException("Invalid card class string");
				string classString = lowerCaseString.Substring(0, 1).ToUpper() + lowerCaseString.Substring(1);
				cardClass = GetClass(classString);
			}
			var descriptionCell = row.SelectSingleNode(".//td[@class = 'col-type']");
			if (descriptionCell == null)
				throw new DeckLoaderException("Unable to extract card type");
			CardType type = GetCardType(descriptionCell.InnerText);
			var manaCell = row.SelectSingleNode(".//td[@class = 'col-cost']");
			if (manaCell == null)
				throw new DeckLoaderException("Unable to extract mana cost");
			int manaCost;
			try
			{
				manaCost = Convert.ToInt32(manaCell.InnerText);
			}
			catch (Exception exception)
			{
				throw new DeckLoaderException("Unable to parse mana cost", exception);
			}
			var attackCell = row.SelectSingleNode(".//td[@class = 'col-attack']");
			if (attackCell == null)
				throw new DeckLoaderException("Unable to extract attack");
			int attack;
			try
			{
				attack = Convert.ToInt32(attackCell.InnerText);
			}
			catch (Exception exception)
			{
				throw new DeckLoaderException("Unable to parse attack", exception);
			}
			var hitPointsCell = row.SelectSingleNode(".//td[@class = 'col-hp']");
			if (hitPointsCell == null)
				throw new DeckLoaderException("Unable to extract hit points");
			int hitPoints;
			try
			{
				hitPoints = Convert.ToInt32(hitPointsCell.InnerText);
			}
			catch (Exception exception)
			{
				throw new DeckLoaderException("Unable to parse hit points", exception);
			}
			if (type == CardType.Minion)
				return Card.Minion(id, name, description, cardClass, rarity, manaCost, attack, hitPoints);
			else
				return Card.Ability(id, name, description, cardClass, rarity, manaCost);
			// Count still missing
		}
	}
}
