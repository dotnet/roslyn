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

namespace BuildActionTelemetryTable
{
    class Program
    {
        private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly string[] s_excludeList = new[]
        {
            "Microsoft.CodeAnalysis.AnalyzerUtilities.dll"
        };

        static void Main(string[] args)
        {
            var assemblies = GetAssemblies(args);
            var codeActionTypes = GetCodeActionTypes(assemblies);
            var telemetryInfos = codeActionTypes.Select(type => GetTelemetryInfo(type));

            var hashes = new StringBuilder();

            hashes.AppendLine("let actions = datatable(ActionName: string, Prefix: string, Suffix: string)");

            hashes.AppendLine("[");

            foreach (var (ActionTypeName, Prefix, Suffix) in telemetryInfos)
            {
                hashes.AppendLine(@$"  ""{ActionTypeName}"", ""{Prefix}"", ""{Suffix}"",");
            }

            hashes.Append("];");

            File.WriteAllText("ActionTable.txt", hashes.ToString());
        }

        internal static ImmutableArray<Assembly> GetAssemblies(string[] paths)
        {
            if (paths.Length == 0)
            {
                // By default inspect the Roslyn assemblies
                paths = Directory.EnumerateFiles(s_executingPath, "Microsoft.CodeAnalysis*.dll")
                    .Where(path => !s_excludeList.Any(exclude => path.EndsWith(exclude)))
                    .ToArray();
            }

            return paths.Select(path => Assembly.LoadFrom(path))
                .ToImmutableArray();
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

        internal static (string ActionTypeName, string Prefix, string Suffix) GetTelemetryInfo(Type type, short scope = 0)
        {
            var telemetryId = type.GetTelemetryId(scope).ToString();
            return (type.FullName, telemetryId.Substring(0, 8), telemetryId.Substring(19));
        }
    }
}
