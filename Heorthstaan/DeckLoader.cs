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

		public DeckLoader(int threadCount = 10)
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
			for (int i = 0; i < ThreadCount; i++)
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
			var selection = document.DocumentNode.SelectNodes("//li[@class = 'b-breadcrumb-item'][2]//span");
			if (selection.Count == 0)
				throw new DeckLoaderException("Unable to determine class of deck");
			string classString = selection.First().InnerText;
			Class deckClass = GetClass(classString);
		}
	}
}
