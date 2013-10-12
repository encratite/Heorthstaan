using System.Data.Entity;

namespace EntityFrameworkTest
{
	class Sample
	{
		public int Id { get; set; }
		public int IntegerField { get; set; }
		public string StringField { get; set; } 
		public bool BooleanField { get; set; }

		public Sample(int integerField, string stringField, bool booleanField)
		{
			IntegerField = integerField;
			StringField = stringField;
			BooleanField = booleanField;
		}
	}

	class Context : DbContext
	{
		public DbSet<Sample> Samples { get; set; }

		public Context(string connectionString)
		{
			Database.Connection.ConnectionString = connectionString;
		}
	}

	class Program
	{
		static void Main(string[] arguments)
		{
			using (Context context = new Context(@"Data Source = .\SQLEXPRESS; Database = test; Integrated Security = True"))
			{
				context.Database.CreateIfNotExists();
				Sample sample = new Sample(123, "string", true);
				context.Samples.Add(sample);
				context.SaveChanges();
			}
		}
	}
}
