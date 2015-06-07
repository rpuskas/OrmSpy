using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;
using NUnit.Framework;

namespace OrmSpy
{
    public class SqlStatementInterceptor : EmptyInterceptor
    {
        public override SqlString OnPrepareStatement(SqlString sql)
        {
            var parameters = sql.GetParameters();
            parameters.ForEach(p => Trace.WriteLine(p)); 
            Trace.WriteLine(sql.ToString());
            return sql;
        }
    }

    [TestFixture]
    public class ShouldLogNhibernateQuery
    {
        private static Configuration _configuration;

        private static ISessionFactory CreateSessionFactory()
        {
            XmlConfigurator.Configure();
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory)
                .ExposeConfiguration(c =>
                {
                    _configuration = c;
                    //c.SetInterceptor(new SqlStatementInterceptor());
                })

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
                var foo = session.Query<Parent>().Single(x => x.Id == 1);
                Assert.AreEqual(foo.Id, 1);
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

    public class AwesomeAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            var message = RenderLoggingEvent(loggingEvent);
            var match = Regex.Match(message, @"done processing result set \((\d*) rows\)");
            if (match.Success)
            {  
                Console.WriteLine("ROWS: {0}",match.Groups[1]);
            }

            var sqlMatch = Regex.Match(message, @"SQL: (.*)$");
            if (sqlMatch.Success)
            {
                Console.WriteLine(sqlMatch.Groups[1]);
            }

            var executionTime = Regex.Match(message, @"ExecuteReader took (\d*) ms");
            if (executionTime.Success)
            {
                Console.WriteLine(executionTime.Groups[1]);
            }


            switch (loggingEvent.Level.Name)
            {
                case "DEBUG":
                    //Console.WriteLine(RenderLoggingEvent(loggingEvent));
                    break;
            }
        }
    } 
}
