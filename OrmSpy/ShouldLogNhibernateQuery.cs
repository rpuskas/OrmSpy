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
    public static class QueryResults
    {
        public static int ExecutionTime;
        public static int Queries;
        public static int TotalRows { get; set; }
    }

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
                session.SaveOrUpdate(parent);
                session.Flush();
                var foo = session.Query<Parent>().Single(x => x.Id == 1);
                Assert.AreEqual(foo.Id, 1);

                Console.WriteLine("TOTAL QUERIES = {0}", QueryResults.Queries);
                Console.WriteLine("EXECUTION TIME = {0}", QueryResults.ExecutionTime);
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
        public enum State
        {
            SearchingForSql,
            SearchingForParamerizedSql,
            SearchingForExecutionTime,
            SearchingForRowCount
        }

        private State state = State.SearchingForSql;

        protected override void Append(LoggingEvent loggingEvent)
        {
            var message = RenderLoggingEvent(loggingEvent);
            switch (state)
            {
                case State.SearchingForSql:
                    if (Regex.Match(message, @"SQL: (.*)$").Success)
                    {
                        QueryResults.Queries++;
                        state = State.SearchingForParamerizedSql;
                    }
                    break;
                case State.SearchingForParamerizedSql:
                    if (Regex.Match(message, @"^select.*from.*").Success)
                    {
                        state = Regex.Match(message, @"^select.*from.*").Success ? State.SearchingForExecutionTime : state;
                        Console.Write(message);    
                    }
                    break;
                case State.SearchingForExecutionTime:
                    var match = Regex.Match(message, @"ExecuteReader took (\d*) ms");
                    if (match.Success)
                    {
                        state = State.SearchingForExecutionTime;
                        QueryResults.ExecutionTime += int.Parse(match.Groups[1].Value);
                        Console.Write(message);
                    }
                    break;
                case State.SearchingForRowCount:
                    var match2 = Regex.Match(message, @"done processing result set \((\d*) rows\)");
                    if (match2.Success)
                    {
                        state = State.SearchingForSql;
                        QueryResults.TotalRows += int.Parse(match2.Groups[1].Value);
                        Console.Write(message);
                    }
                    break;
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
