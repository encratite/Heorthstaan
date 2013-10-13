using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

namespace FluentNHibernateTest
{
	class Sample
	{
		public int Id { get; set; }
		public int IntegerField { get; set; }
		public string StringField { get; set; }
		public bool BooleanField { get; set; }
	}

	class Program
	{
		static void Main(string[] arguments)
		{
			var configuration = PostgreSQLConfiguration
				.PostgreSQL82
				.ConnectionString(x =>
					x
					.Host("127.0.0.1")
					.Username("void")
					.Database("test"));
			var sessionFactory = Fluently
				.Configure()
				.Database(configuration)
				.Mappings(x =>
					x.AutoMappings
					.Add(AutoMap.AssemblyOf<Sample>()))
				.BuildSessionFactory();

			using(var session = sessionFactory.OpenSession())
			{
				using(var transaction = session.BeginTransaction())
				{
					Sample sample = new Sample { IntegerField = 123, StringField = "string", BooleanField = true };
					session.Save(sample);
					transaction.Commit();
				}
			}
		}
	}
}
