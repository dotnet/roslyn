// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roslyn.Test.Performance.Utilities.ConsumptionParser
{
    internal class Program
    {
        public static void Main()
        {
            const string prefix = "PerfResults-";
            const string share = @"\\mlangfs1\public\basoundr\PerfTraces\";

            var runs = new List<Tuple<string, ConsumptionParse, RunInfo>>();

            // Collect run information from the share
            foreach (var directory in Directory.GetDirectories(share))
            {
                var lastPath = directory.Substring(share.Length);
                if (!lastPath.StartsWith(prefix))
                {
                    continue;
                }

                var date = lastPath.Substring(prefix.Length);
                var consumptionXml = Path.Combine(directory, "ConsumptionTempResults.xml");
                var resultJson = Directory.EnumerateFiles(directory, "Roslyn*.json").Single();

                var parse = ConsumptionParse.Parse(File.ReadAllText(consumptionXml));
                var runInfo = RunInfo.Parse(File.ReadAllText(resultJson));
                runs.Add(Tuple.Create(date, parse, runInfo));

            }

            // Sort by date
            runs.Sort((run1, run2) => run1.Item1.CompareTo(run2.Item1));

            // Collect all of the metric names first 
            var metrics = new HashSet<string>(
                from run in runs
                from scenario in run.Item2.Scenarios
                from metric in scenario.Counters
                select metric.Name);

            // Write the column titles
            Console.Write("build, username, branch, scenario");
            foreach (var metric in metrics)
            {
                // Prepend a 'z_' so that in PowerBI all of the metrics
                // are grouped together in the alphabetically sorted list of fields
                Console.Write($", z_{metric}");
            }
            Console.WriteLine();

            foreach (var run in runs)
            {
                var date = run.Item1;
                var consumption = run.Item2;
                var runInfo = run.Item3;

                foreach (var scenario in consumption.Scenarios)
                {
                    Console.Write($"{NormalizeDate(date)}, {NormalizeUserName(runInfo.UserName)}, {NormalizeBranch(runInfo.Branch)}, {NormalizeScenario(scenario.Name)}");
                    foreach (var metric in metrics)
                    {
                        var m = scenario[metric];
                        if (m != null)
                        {
                            var mm = m.Value;
                            Console.Write($", {mm.Value}");
                        }
                        else
                        {
                            Console.Write(",");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Removes a prefix from a string.  RemovePrefix("aaa-bbb", "aaa-") gives you "bbb".
        /// </summary>
        private static string RemovePrefix(string target, string prefix)
        {
            if (target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                target = target.Substring(prefix.Length);
            }
            return target;
        }

        private static string NormalizeBranch(string s)
        {
            s = RemovePrefix(s, "Roslyn-");
            if (s == "HEAD")
            {
                s = "master";
            }
            return s;
        }

        private static string NormalizeUserName(string s)
        {
            return RemovePrefix(s, @"redmond\");
        }

        private static string NormalizeScenario(string s)
        {
            while (s.Length != 0 && char.IsNumber(s.Last()))
            {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }

        private static string NormalizeDate(string s)
        {
            var dateTimeSplit = s.Split('_');
            var date = dateTimeSplit[0];
            var time = dateTimeSplit[1];

            var hourMinuteSecondSplit = time.Split('-');
            var hour = hourMinuteSecondSplit[0];
            var min = hourMinuteSecondSplit[1];
            var sec = hourMinuteSecondSplit[2];

            return $"{date} {hour}:{min}:{sec}";
        }
    }
}
