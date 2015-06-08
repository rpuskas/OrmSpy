using System;
using System.Linq;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using log4net.Config;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace OrmSpy
{
    [TestFixture]
    public class ShouldLogNhibernateQuery
    {
        private static Configuration _configuration;

        private static ISessionFactory CreateSessionFactory()
        {
            XmlConfigurator.Configure();
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory)
                .ExposeConfiguration(c => { _configuration = c; })
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ShouldLogNhibernateQuery>())
                .BuildSessionFactory();
        }

        [Test]
        public void ShouldLog()
        {
            var sessionFactory = CreateSessionFactory();
            var schemaExport = new SchemaExport(_configuration);
            using (var session = sessionFactory.OpenSession())
            {
                schemaExport.Execute(true, true, false, session.Connection, null);
                var parent = new Parent();
                var parent2 = new Parent();
                session.SaveOrUpdate(parent);
                session.SaveOrUpdate(parent2);
                session.Flush();

                var foo = session.Query<Parent>().Single(x => x.Id == 1);
                var foo2 = session.Query<Parent>().Single(x => x.Id == 2);
                
                Assert.AreEqual(foo.Id, 1);
                Assert.AreEqual(foo2.Id, 2);
                Assert.AreEqual(OrmSpyResult.Queries,2);
                Assert.AreEqual(OrmSpyResult.Rows, 2);

                OrmSpyResult.Print(Console.WriteLine);
                OrmSpyResult.Reset();
            }
        }
    }
}
