using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;

using MySql.Data.MySqlClient;
using System.Data.Common;

namespace MySQLTest
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
	}

	class Program
	{
		static void RunVersionTest(string connectionString)
		{
			var factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
			using (var connection = (MySqlConnection)factory.CreateConnection())
			{
				connection.ConnectionString = connectionString;
				connection.Open();
				MySqlCommand command = new MySqlCommand("select version()", connection);
				String version = (String)command.ExecuteScalar();
			}
		}

		static void Main(string[] arguments)
		{
			string connectionString = "Server = 127.0.0.1; User = void; Password =; Database = test";
			// RunVersionTest(connectionString);
			using (Context context = new Context())
			{
				// context.Database.Connection.ConnectionString = connectionString;
				// context.Database.Connection.Open();
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
