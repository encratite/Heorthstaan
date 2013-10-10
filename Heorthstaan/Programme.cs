namespace Heorthstaan
{
	class Programme
	{
		static void Main(string[] arguments)
		{
			DeckLoader loader = new DeckLoader(1);
			loader.Load();
		}
	}
}
