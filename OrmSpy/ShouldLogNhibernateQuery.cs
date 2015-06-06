using System.Linq;
using System.Text.RegularExpressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
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
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory)
                .ExposeConfiguration(c => _configuration = c)
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
                session.SaveOrUpdate(parent);
                session.Flush();
                var foo = session.Query<Parent>().ToList();
                Assert.AreEqual(foo.First().Id, 1);
            }
        }
    }

    public class Parent
    {
        public virtual int Id { get; set; }
    }

    public class ParentMap : ClassMap<Parent>
    {
        public ParentMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
        }
    }
}
