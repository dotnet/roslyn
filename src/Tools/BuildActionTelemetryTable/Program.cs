// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static BuildActionTelemetryTable.CodeActionDescriptions;

namespace BuildActionTelemetryTable;

public static partial class Program
{
    private static readonly string s_executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private static ImmutableHashSet<string> IgnoredCodeActions { get; } = new HashSet<string>()
    {
        "Microsoft.CodeAnalysis.CodeActions.CodeAction+CodeActionWithNestedActions",
        "Microsoft.CodeAnalysis.CodeActions.CodeAction+DocumentChangeAction",
        "Microsoft.CodeAnalysis.CodeActions.CodeAction+NoChangeAction",
        "Microsoft.CodeAnalysis.CodeActions.CodeAction+SolutionChangeAction",
        "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+DocumentChangeAction",
        "Microsoft.CodeAnalysis.CodeActions.CustomCodeActions+SolutionChangeAction",
        "Microsoft.CodeAnalysis.CodeFixes.DocumentBasedFixAllProvider+PostProcessCodeAction",
    }.ToImmutableHashSet();

    public static void Main(string[] args)
    {
        Console.WriteLine("Loading assemblies and finding CodeActions ...");

        var assemblies = GetAssemblies(args);
        var codeActionAndProviderTypes = GetCodeActionAndProviderTypes(assemblies);

        var telemetryInfos = GetTelemetryInfos(codeActionAndProviderTypes);

        if (!IsDescriptionMapComplete(telemetryInfos))
        {
            WriteCodeActionDescriptionMap(telemetryInfos);

            Console.WriteLine("Please update the CodeActionDescriptions.cs file and re-run the tool.");
        }
        else
        {
            WriteKustoDatatable(codeActionAndProviderTypes, telemetryInfos);
        }

        Console.WriteLine("Complete.");

        static void WriteKustoDatatable(ImmutableArray<Type> codeActionAndProviderTypes, ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
        {
            Console.WriteLine($"Generating Kusto datatable of {codeActionAndProviderTypes.Length} CodeAction and provider hashes ...");

            var datatable = GenerateKustoDatatable(telemetryInfos);

            var filepath = Path.GetFullPath("ActionTable.txt");

            Console.WriteLine($"Writing datatable to {filepath} ...");

            File.WriteAllText(filepath, datatable);
        }

        static void WriteCodeActionDescriptionMap(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
        {
            Console.WriteLine($"Generating new CodeAction Description Map ...");

            var descriptionMap = GenerateCodeActionsDescriptionMap(telemetryInfos);

            var filepath = Path.GetFullPath("CodeActionDescriptions.Review.cs");

            Console.WriteLine($"Writing code file to {filepath} ...");

            File.WriteAllText(filepath, descriptionMap);
        }
    }

    internal static ImmutableArray<Assembly> GetAssemblies(string[] paths)
    {
        if (paths.Length == 0)
        {
            // By default inspect the Roslyn assemblies
            paths = [.. Directory.EnumerateFiles(s_executingPath, "Microsoft.CodeAnalysis*.dll")];
        }

        var currentDirectory = new Uri(Environment.CurrentDirectory + "\\");
        return [.. paths.Select(path =>
        {
            Console.WriteLine($"Loading assembly from {GetRelativePath(path, currentDirectory)}.");
            return Assembly.LoadFrom(path);
        })];

        static string GetRelativePath(string path, Uri baseUri)
        {
            var rootedPath = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(path);
            var relativePath = baseUri.MakeRelativeUri(new Uri(rootedPath));
            return relativePath.ToString();
        }
    }

    internal static ImmutableArray<Type> GetCodeActionAndProviderTypes(IEnumerable<Assembly> assemblies)
    {
        var types = assemblies.SelectMany(
            assembly => assembly.GetTypes().Where(
                type => !type.GetTypeInfo().IsInterface && !type.GetTypeInfo().IsAbstract));

        return [.. types.Where(t => IsCodeActionType(t) || IsCodeActionProviderType(t))];

        static bool IsCodeActionType(Type t) => typeof(CodeAction).IsAssignableFrom(t);

        static bool IsCodeActionProviderType(Type t) => typeof(CodeFixProvider).IsAssignableFrom(t)
            || typeof(CodeRefactoringProvider).IsAssignableFrom(t);
    }

    internal static ImmutableArray<(string TypeName, string Hash)> GetTelemetryInfos(ImmutableArray<Type> codeActionAndProviderTypes)
    {
        return [.. codeActionAndProviderTypes
            .Distinct(FullNameTypeComparer.Instance)
            .Select(GetTelemetryInfo)
            .OrderBy(info => info.TypeName)];

        static (string TypeName, string Hash) GetTelemetryInfo(Type type)
        {
            // Generate dev17 telemetry hash
            var telemetryId = type.GetTelemetryId().ToString();
            var fnvHash = telemetryId.Substring(19);

            return (type.FullName!, fnvHash);
        }
    }

    internal static bool IsDescriptionMapComplete(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
    {
        var missingDescriptions = new List<string>();

        foreach (var (actionTypeName, _) in telemetryInfos)
        {
            if (IgnoredCodeActions.Contains(actionTypeName))
            {
                continue;
            }

            if (!CodeActionDescriptionMap.TryGetValue(actionTypeName, out var description))
            {
                missingDescriptions.Add(actionTypeName);
            }
        }

        if (missingDescriptions.Count == 0)
        {
            return true;
        }

        Console.WriteLine($"The following Actions are new and need their description reviewed:{Environment.NewLine}{string.Join(Environment.NewLine, missingDescriptions)}");

        return false;
    }

    internal static string GenerateKustoDatatable(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
    {
        var table = new StringBuilder();

        table.AppendLine("let actions = datatable(Description: string, ActionName: string, FnvHash: string)");
        table.AppendLine("[");

        foreach (var (actionTypeName, fnvHash) in telemetryInfos)
        {
            if (IgnoredCodeActions.Contains(actionTypeName))
            {
                continue;
            }

            if (!CodeActionDescriptionMap.TryGetValue(actionTypeName, out var description))
            {
                description = $"**NEEDS REVIEW** {GenerateCodeActionDescription(actionTypeName)}";
            }

            table.AppendLine(@$"  ""{description}"", ""{actionTypeName}"", ""{fnvHash}"",");
        }

        table.Append("];");

        return table.ToString();
    }

    internal static string GenerateCodeActionsDescriptionMap(ImmutableArray<(string TypeName, string Hash)> telemetryInfos)
    {
        var builder = new StringBuilder();

        builder.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
        builder.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
        builder.AppendLine("// See the LICENSE file in the project root for more information.");
        builder.AppendLine();
        builder.AppendLine("using System.Collections.Immutable;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("namespace BuildActionTelemetryTable;");
        builder.AppendLine();
        builder.AppendLine("internal static class CodeActionDescriptions");
        builder.AppendLine("{");

        builder.AppendLine("    public static ImmutableDictionary<string, string> CodeActionDescriptionMap { get; } = new Dictionary<string, string>()");
        builder.AppendLine("    {");

        foreach (var (actionOrProviderTypeName, _) in telemetryInfos)
        {
            if (IgnoredCodeActions.Contains(actionOrProviderTypeName))
            {
                continue;
            }

            if (!CodeActionDescriptionMap.TryGetValue(actionOrProviderTypeName, out var description))
            {
                description = $"**NEEDS REVIEW** {GenerateCodeActionDescription(actionOrProviderTypeName)}";
            }

            builder.AppendLine(@$"        {{ ""{actionOrProviderTypeName}"", ""{description}"" }},");
        }

        builder.AppendLine("    }.ToImmutableDictionary();");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateCodeActionDescription(string actionOrProviderTypeName)
    {
        // Regex to split where letter capitalization changes. Try not to split up interface names such as IEnumerable.
        var regex = ChangeOfCaseRegex();

        // Prefixes and suffixes to trim out.
        var prefixStrings = new[]
        {
            "Abstract",
            "CSharp",
            "VisualBasic",
            "CodeFixes",
            "CodeStyle",
            "TypeStyle",
        };

        var suffixStrings = new[]
        {
            "CodeFixProvider",
            "CodeRefactoringProvider",
            "RefactoringProvider",
            "TaggerProvider",
            "CustomCodeAction",
            "CodeAction",
            "CodeActionWithOption",
            "CodeActionProvider",
            "Action",
            "FeatureService",
            "Service",
            "ProviderHelpers",
            "Provider",
        };

        // We create the description string from the namespace and type name after omitting the well-known prefixes and suffixes.
        var descriptionParts = actionOrProviderTypeName.Replace('.', '+').Split('+');

        // When there is deep nesting, construct the descriptions from the inner two names.
        var startIndex = Math.Max(0, descriptionParts.Length - 2);

        var description = string.Empty;
        var isRefactoring = false;

        for (var index = startIndex; index < descriptionParts.Length; index++)
        {
            var part = descriptionParts[index];

            // Remove TypeParameter count
            if (part.Contains('`'))
            {
                part = part.Split('`')[0];
            }

            foreach (var prefix in prefixStrings)
            {
                if (part.StartsWith(prefix))
                {
                    part = part.Substring(prefix.Length);
                    break;
                }
            }

            foreach (var suffix in suffixStrings)
            {
                if (part.EndsWith(suffix))
                {
                    part = part.Substring(0, part.LastIndexOf(suffix));

                    if (suffix == "CodeActionWithOption")
                    {
                        part += "WithOption";
                    }
                    else if (suffix == "CodeRefactoringProvider" || suffix == "RefactoringProvider")
                    {
                        isRefactoring = true;
                    }

                    break;
                }
            }

            if (part.Length == 0)
            {
                continue;
            }

            // Split type name into words
            part = regex.Replace(part, " ");

            if (description.Length == 0)
            {
                description = part;
                continue;
            }

            if (description == part)
            {
                // Don't repeat the containing type name.
                continue;
            }

            if (part.StartsWith(description))
            {
                // Don't repeat the containing type name.
                part = part.Substring(description.Length).TrimStart();
            }

            description = $"{description}: {part}";
        }

        if (isRefactoring)
        {
            // Ensure we differentiate refactorings from similar named fixes.
            description += " (Refactoring)";
        }

        return description;
    }

    private class FullNameTypeComparer : IEqualityComparer<Type>
    {
        public static FullNameTypeComparer Instance { get; } = new FullNameTypeComparer();

        public bool Equals(Type? x, Type? y)
            => Equals(x?.FullName, y?.FullName);

        public int GetHashCode([DisallowNull] Type obj)
        {
            return obj.FullName!.GetHashCode();
        }
    }

    [GeneratedRegex("""
        (?<=[I])(?=[A-Z]) &
        (?<=[A-Z])(?=[A-Z][a-z]) |
        (?<=[^A-Z])(?=[A-Z]) |
        (?<=[A-Za-z])(?=[^A-Za-z])
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ChangeOfCaseRegex();
}
