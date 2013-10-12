using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;

namespace EntityFrameworkTest
{
	class Sample
	{
		[Key]
		public int Id { get; set; }
		public int IntegerField { get; set; }
		public string StringField { get; set; }
		public bool BooleanField { get; set; }

		public Sample()
		{ }

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
				var samples = from x in context.Samples
							  where x.StringField == "string"
							  select x;
				Console.WriteLine("Samples:");
				foreach (var x in samples)
					Console.WriteLine("Id: {0}, IntegerField: {1}, StringField: {2}, BooleanField: {3}", x.Id, x.IntegerField, x.StringField, x.BooleanField);
			}
		}
	}
}
