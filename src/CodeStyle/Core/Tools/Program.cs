// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeStylePackagePropsFileGenerator
{
    class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine($"Excepted 2 arguments, found {args.Length}: {string.Join(";", args)}");
                return 1;
            }

            var assemblyList = args[0].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            string propsFile = args[1];

            var ruleIds = GetAllRuleIds(assemblyList);
            Directory.CreateDirectory(Path.GetDirectoryName(propsFile));
            File.WriteAllText(propsFile, GetPropsFileContents(ruleIds));

            return 0;

            static SortedSet<string> GetAllRuleIds(ImmutableArray<string> assemblyList)
            {
                var ruleIds = new SortedSet<string>();
                foreach (var assemblyPath in assemblyList)
                {
                    if (!File.Exists(assemblyPath))
                    {
                        throw new Exception($"'{assemblyPath}' does not exist");
                    }

                    var analyzerFileReference = new AnalyzerFileReference(assemblyPath, AnalyzerAssemblyLoader.Instance);
                    var analyzers = analyzerFileReference.GetAnalyzersForAllLanguages();

                    foreach (var analyzer in analyzers)
                    {
                        foreach (var rule in analyzer.SupportedDiagnostics)
                        {
                            if (rule.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                            {
                                // Skip non-configurable diagnostics.
                                continue;
                            }

                            ruleIds.Add(rule.Id);
                        }
                    }
                }

                return ruleIds;
            }

            static string GetPropsFileContents(IEnumerable<string> ruleIds)
            {
                var allRuleIds = string.Join(";", ruleIds);
                return
$@"<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <!-- This property group prevents the rule ids implemented in this package from being executed by default in CI -->
  <PropertyGroup Condition=""'$(DesignTimeBuild)' != 'true' and '$(BuildingProject)' != 'false'"">
    <NoWarn>$(NoWarn);{allRuleIds}</NoWarn>
  </PropertyGroup>
</Project>";
            }
        }

        private sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            public static IAnalyzerAssemblyLoader Instance = new AnalyzerAssemblyLoader();

            private AnalyzerAssemblyLoader() { }
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
