// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using TelemetryInfo = System.Tuple<string, string, string>;

namespace BuildActionTelemetryTable
{
    public class Program
    {
        private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void Main(string[] args)
        {
            Console.WriteLine("Loading assemblies and finding CodeActions ...");

            var assemblies = GetAssemblies(args);
            var codeActionTypes = GetCodeActionTypes(assemblies);

            Console.WriteLine($"Generating Kusto datatable of {codeActionTypes.Length} CodeAction hashes ...");

            var telemetryInfos = GetTelemetryInfos(codeActionTypes);
            var datatable = GenerateKustoDatatable(telemetryInfos);

            var filepath = Path.GetFullPath(".\\ActionTable.txt");

            Console.WriteLine($"Writing datatable to {filepath} ...");

            File.WriteAllText(filepath, datatable);

            Console.WriteLine("Complete.");
        }

        internal static ImmutableArray<Assembly> GetAssemblies(string[] paths)
        {
            if (paths.Length == 0)
            {
                // By default inspect the Roslyn assemblies
                paths = Directory.EnumerateFiles(s_executingPath, "Microsoft.CodeAnalysis*.dll")
                    .ToArray();
            }

            var currentDirectory = new Uri(Environment.CurrentDirectory + "\\");
            return paths.Select(path =>
            {
                Console.WriteLine($"Loading assembly from {GetRelativePath(path, currentDirectory)}.");
                return Assembly.LoadFrom(path);
            }).ToImmutableArray();

            static string GetRelativePath(string path, Uri baseUri)
            {
                var rootedPath = Path.IsPathRooted(path)
                    ? path
                    : Path.GetFullPath(path);
                var relativePath = baseUri.MakeRelativeUri(new Uri(rootedPath));
                return relativePath.ToString();
            }
        }

        internal static ImmutableArray<Type> GetCodeActionTypes(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(
                assembly => assembly.GetTypes().Where(
                    type => !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract));

            return types
                .Where(t => typeof(CodeAction).IsAssignableFrom(t))
                .ToImmutableArray();
        }

        internal static ImmutableArray<TelemetryInfo> GetTelemetryInfos(ImmutableArray<Type> codeActionTypes)
        {
            return codeActionTypes.Select(GetTelemetryInfo)
                .ToImmutableArray();

            static TelemetryInfo GetTelemetryInfo(Type type)
            {
                var telemetryId = type.GetTelemetryId().ToString();
                return Tuple.Create(type.FullName, telemetryId.Substring(0, 8), telemetryId.Substring(19));
            }
        }

        internal static string GenerateKustoDatatable(ImmutableArray<TelemetryInfo> telemetryInfos)
        {
            var table = new StringBuilder();

            table.AppendLine("let actions = datatable(ActionName: string, Prefix: string, Suffix: string)");
            table.AppendLine("[");

            foreach (var (actionTypeName, prefix, suffix) in telemetryInfos)
            {
                table.AppendLine(@$"  ""{actionTypeName}"", ""{prefix}"", ""{suffix}"",");
            }

            table.Append("];");

            return table.ToString();
        }
    }
}
