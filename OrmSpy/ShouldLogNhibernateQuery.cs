using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using log4net.Config;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;
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
                var men = new[] { new Man(), new Man(), new Man() };
                var women = new[] { new Woman(), new Woman(), new Woman() };
                var marriage = new Marriage() {Man = men.First(), Woman = women.First()};

                men.ForEach(m => session.Save(m));
                women.ForEach(w => session.Save(w));
                session.Save(marriage);

                session.Flush();
                session.Clear();

                var foo = session.Query<Marriage>().Fetch(x => x.Man).ThenFetch(x => x.Properties).ToList();
                var marriedMen = foo.Select(x => x.Man);
                var detachedCriteria = marriedMen.Select(x => x.Id);
                var singleMen = session.QueryOver<Man>().WhereRestrictionOn(m => m.Id).Not.IsIn(detachedCriteria.ToList());

                Assert.AreEqual(singleMen.List().Count,2);
                Assert.AreEqual(foo.Single().Man.Properties.Age, 21);
                OrmSpyResult.Print(Console.WriteLine);
   
            }
        }
    }
}
