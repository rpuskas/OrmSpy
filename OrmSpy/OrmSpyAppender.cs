using System;
using System.Text.RegularExpressions;
using log4net.Appender;
using log4net.Core;

namespace OrmSpy
{
    public class OrmSpyAppender : AppenderSkeleton
    {
        private SearchState _state = SearchState.InitialState;
        protected override void Append(LoggingEvent loggingEvent)
        {
            Console.Write(RenderLoggingEvent(loggingEvent));
            //_state = _state.Evaluate(RenderLoggingEvent(loggingEvent));
        }
    }

    //TODO: Update properties via event listener on search state?
    public static class OrmSpyResult
    {
        public static int Time;
        public static int Queries;
        public static int Rows;

        public static void Reset()
        {
            Time = 0;
            Queries = 0;
            Rows = 0;
        }

        public delegate void PrintAction(string s, params object[] args);
        public static void Print(PrintAction print)
        {
            print("Batch Result:");
            print("  Queries = {0}", Queries);
            print("  Rows = {0}", Rows);
            print("  Time = {0}", Time);
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

        private static readonly SearchState Sql = new SearchState(@"Building an IDbCommand object.*", (mg, mt) =>
        {
            OrmSpyResult.Queries++;
            return ParamerizedSql;
        });

        private static readonly SearchState ParamerizedSql = new SearchState(@"(?i)^select.*from.*;.*", (mg, mt) =>
        {
            Console.Write("Unformatted: {0}", mg);
            Console.WriteLine("Formatted:   {0}", ReplaceQueryParametersWithValues(mg));
            return ExecutionTime;
        });

        private static readonly SearchState ExecutionTime = new SearchState(@"ExecuteReader took (\d*) ms", (mg,mt) =>
        {
            OrmSpyResult.Time += int.Parse(mt.Groups[1].Value);
            Console.Write(mg);
            return RowCount;
        });

        private static readonly SearchState RowCount = new SearchState(@"done processing result set \((\d*) rows\)", (mg,mt) =>
        {
            OrmSpyResult.Rows += int.Parse(mt.Groups[1].Value);
            Console.Write(mg);
            Console.WriteLine();
            return Sql;
        });

        private static string ReplaceQueryParametersWithValues(string query)
        {
            var result = Regex.Replace(query, @"@p\d(?=[,);\s])(?!\s*=)", match =>
            {
                var parameterValueRegex = new Regex(string.Format(@".*{0}\s*=\s*(.*?)\s*[\[].*", match));
                return parameterValueRegex.Match(query).Groups[1].ToString();
            });
            return new Regex(".*;").Match(result).Value;
        }
    }
}