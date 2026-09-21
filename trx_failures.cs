// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// dotnet run --file trx_failures.cs -- results.trx -o failures.md

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

return TrxFailures.Run(args);

static partial class TrxFailures
{
    private static readonly XNamespace s_trxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static int Run(string[] args)
    {
        if (!TryParseArguments(args, out var trxPaths, out var outputPath))
        {
            PrintUsage();
            return 1;
        }

        var failures = new List<TestFailure>();
        foreach (var path in trxPaths)
        {
            failures.AddRange(Collect(path));
        }

        var uniqueFailures = failures
            .OrderBy(failure => failure.Name, StringComparer.Ordinal)
            .ThenBy(failure => failure.File, StringComparer.Ordinal)
            .ThenBy(failure => failure.Line ?? 0)
            .DistinctBy(failure => (failure.Name, failure.File, failure.Line));

        var markdown = FormatMarkdown(uniqueFailures);
        if (outputPath is not null)
        {
            File.WriteAllText(outputPath, markdown + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        else
        {
            Console.WriteLine(markdown);
        }

        return 0;
    }

    private static bool TryParseArguments(string[] args, out List<string> trxPaths, out string? outputPath)
    {
        trxPaths = [];
        outputPath = null;
        var parseOptions = true;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (parseOptions && argument == "--")
            {
                parseOptions = false;
            }
            else if (parseOptions && argument is "-o" or "--output")
            {
                if (++i >= args.Length)
                {
                    return false;
                }

                outputPath = args[i];
            }
            else if (parseOptions && argument.StartsWith("--output=", StringComparison.Ordinal))
            {
                outputPath = argument["--output=".Length..];
                if (outputPath.Length == 0)
                {
                    return false;
                }
            }
            else if (parseOptions && argument.StartsWith("-o", StringComparison.Ordinal) && argument.Length > 2)
            {
                outputPath = argument[2..];
            }
            else if (parseOptions && argument.StartsWith('-', StringComparison.Ordinal))
            {
                return false;
            }
            else
            {
                trxPaths.Add(argument);
            }
        }

        return trxPaths.Count > 0;
    }

    private static IEnumerable<TestFailure> Collect(string trxPath)
    {
        var root = XDocument.Load(trxPath).Root
            ?? throw new InvalidDataException($"TRX file '{trxPath}' does not have a root element.");

        var definitions = new Dictionary<string, TestDefinition>(StringComparer.Ordinal);
        foreach (var unitTest in FindAll(root, "UnitTest"))
        {
            var id = (string?)unitTest.Attribute("id");
            if (id is null)
            {
                continue;
            }

            var method = Find(unitTest, "TestMethod");
            definitions[id] = new TestDefinition(
                (string?)method?.Attribute("className"),
                (string?)unitTest.Attribute("storage") ?? (string?)method?.Attribute("codeBase"));
        }

        foreach (var result in FindAll(root, "UnitTestResult"))
        {
            if (!string.Equals((string?)result.Attribute("outcome"), "failed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var testId = (string?)result.Attribute("testId");
            definitions.TryGetValue(testId ?? "", out var definition);

            var output = Find(result, "Output");
            var errorInfo = output is null ? null : Find(output, "ErrorInfo");
            var stack = GetText(errorInfo, "StackTrace");
            var message = string.Join(' ', GetText(errorInfo, "Message").Split((string[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var (file, line) = LocationFromStack(stack);

            yield return new TestFailure(
                (string?)result.Attribute("testName") ?? "",
                definition?.ClassName,
                definition?.Storage,
                file,
                line,
                message);
        }
    }

    private static string FormatMarkdown(IEnumerable<TestFailure> failures)
    {
        var lines = new List<string>
        {
            "| Test | Location |",
            "| --- | --- |",
        };

        foreach (var failure in failures)
        {
            var location = failure.File is not null
                ? $"{failure.File}:{failure.Line}"
                : failure.ClassName ?? failure.Storage ?? "unknown location";
            lines.Add($"| `{EscapeTableCell(failure.Name)}` | {EscapeTableCell(location)} |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapeTableCell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static (string? File, int? Line) LocationFromStack(string stack)
    {
        var match = StackLocationRegex().Match(stack);
        return match.Success
            ? (match.Groups["file"].Value.Trim(), int.Parse(match.Groups["line"].Value))
            : (null, null);
    }

    private static IReadOnlyList<XElement> FindAll(XElement root, string tag)
    {
        var namespaced = root.Descendants(s_trxNamespace + tag).ToList();
        return namespaced.Count > 0 ? namespaced : root.Descendants(tag).ToList();
    }

    private static XElement? Find(XElement element, string tag)
    {
        return element.Element(s_trxNamespace + tag) ?? element.Element(tag);
    }

    private static string GetText(XElement? element, string tag)
    {
        return element is null ? "" : Find(element, tag)?.FirstNode is XText text ? text.Value : "";
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: dotnet run --file trx_failures.cs -- <file.trx> [more.trx ...] [-o OUT.md]");
    }

    [GeneratedRegex(@"\sin\s(?<file>.+?):line\s(?<line>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex StackLocationRegex();

    private sealed record TestDefinition(string? ClassName, string? Storage);

    private sealed record TestFailure(
        string Name,
        string? ClassName,
        string? Storage,
        string? File,
        int? Line,
        string Message);
}
