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
        private SearchState _state = SearchState.InitialState;
        protected override void Append(LoggingEvent loggingEvent)
        {
            _state = _state.Evaluate(RenderLoggingEvent(loggingEvent));
        }
    }

    public class SearchState
    {
        private readonly Regex _pattern;
        private readonly Func<string,Match,SearchState> _action;

        private SearchState(string pattern, Func<string,Match,SearchState> action)
        {
            _pattern = new Regex(pattern);
            _action = action;
        }

        public static SearchState InitialState { get { return Sql; }}

        public SearchState Evaluate(string message)
        {
            return !_pattern.IsMatch(message) ? this : _action.Invoke(message, _pattern.Match(message));
        }

        private static readonly SearchState Sql = new SearchState(@"SQL: (.*)$", (mg, mt) =>
        {
            QueryResults.Queries++;
            return ParamerizedSql;
        });

        private static readonly SearchState ParamerizedSql = new SearchState(@"^select.*from.*", (mg, mt) =>
        {
            Console.Write(mg);
            return ExecutionTime;
        });

        private static readonly SearchState ExecutionTime = new SearchState(@"ExecuteReader took (\d*) ms", (mg,mt) =>
        {
            QueryResults.ExecutionTime += int.Parse(mt.Groups[1].Value);
            Console.Write(mg);
            return RowCount;
        });

        private static readonly SearchState RowCount = new SearchState(@"done processing result set \((\d*) rows\)", (mg,mt) =>
        {
            QueryResults.TotalRows += int.Parse(mt.Groups[1].Value);
            Console.Write(mg);
            return Sql;
        });
    }
}
