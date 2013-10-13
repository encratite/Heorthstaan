namespace Heorthstaan
{
	class Programme
	{
		static string DatabasePath = "Heorthstaan.ndb";
		static string OutputPath = "Heorthstaan.log";

		static void Scrape()
		{
			using (DeckLoader loader = new DeckLoader(DatabasePath, 3))
			{
				loader.Load();
			}
		}

		static void Analyse()
		{
			using (Analysis analysis = new Analysis(DatabasePath, OutputPath))
			{
				analysis.Run();
			}
		}

		static void Main(string[] arguments)
		{
			// Scrape();
			Analyse();
		}
	}
}
