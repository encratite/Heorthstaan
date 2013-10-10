using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using NDatabase;
using NDatabase.Api;

namespace Heorthstaan
{
	class DeckLoader: IDisposable
	{
		const string DatabasePath = "Heorthstaan.ndb";
		const string WebSite = "http://www.hearthpwn.com";

		WebClient Client;
		IOdb Database;

		public DeckLoader()
		{
			Client = new WebClient();
			Database = OdbFactory.Open(DatabasePath);
		}

		public void Dispose()
		{
			Database.Dispose();
			Client.Dispose();
		}

		public void Load()
		{
			int pageCount = GetPageCount();
			LoaderState loaderState = Database.QueryAndExecute<LoaderState>().GetFirst();
			if (loaderState == null)
				loaderState = new LoaderState();
		}

		HtmlDocument GetDocument(string path)
		{
			string content = Client.DownloadString(WebSite + path);
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			return document;
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
	}
}
