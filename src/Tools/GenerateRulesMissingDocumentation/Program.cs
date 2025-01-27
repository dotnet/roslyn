// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

const int expectedArguments = 4;
const string validateOnlyPrefix = "-validateOnly:";
const string rulesMissingDocumentationFileName = "RulesMissingDocumentation.md";

if (args.Length != expectedArguments)
{
    await Console.Error.WriteLineAsync($"Excepted {expectedArguments} arguments, found {args.Length}: {string.Join(';', args)}").ConfigureAwait(false);
    return 1;
}

if (!args[0].StartsWith(validateOnlyPrefix, StringComparison.OrdinalIgnoreCase))
{
    await Console.Error.WriteLineAsync($"Excepted the first argument to start with `{validateOnlyPrefix}`. found `{args[0]}`.").ConfigureAwait(false);
    return 1;
}

if (!bool.TryParse(args[0][validateOnlyPrefix.Length..], out var validateOnly))
{
    validateOnly = false;
}

var httpClient = new HttpClient();

string analyzerDocumentationFileDir = args[1];
string binDirectory = args[2];
string configuration = args[3];

var directory = Directory.CreateDirectory(analyzerDocumentationFileDir);
var fileWithPath = Path.Combine(directory.FullName, rulesMissingDocumentationFileName);

var builder = new StringBuilder();
builder.Append(@"# Rules without documentation

Rule ID | Missing Help Link | Title |
--------|-------------------|-------|
");

var actualContent = Array.Empty<string>();
if (validateOnly)
{
    actualContent = File.ReadAllLines(fileWithPath);
}

var allRulesById = getAllRulesById(binDirectory, configuration);

foreach (var ruleById in allRulesById)
{
    string ruleId = ruleById.Key;
    DiagnosticDescriptor descriptor = ruleById.Value;

    var helpLinkUri = descriptor.HelpLinkUri;
    if (!string.IsNullOrWhiteSpace(helpLinkUri) &&
        await checkHelpLinkAsync(helpLinkUri).ConfigureAwait(false))
    {
        // Rule with valid documentation link
        continue;
    }

    // The angle brackets around helpLinkUri are added to follow MD034 rule:
    // https://github.com/DavidAnson/markdownlint/blob/82cf68023f7dbd2948a65c53fc30482432195de4/doc/Rules.md#md034---bare-url-used
    if (!string.IsNullOrWhiteSpace(helpLinkUri))
    {
        helpLinkUri = $"<{helpLinkUri}>";
    }

    var escapedTitle = descriptor.Title.ToString(CultureInfo.InvariantCulture).Replace("<", "\\<");
    var line = $"{ruleId} | {helpLinkUri} | {escapedTitle} |";
    if (validateOnly)
    {
        // We consider having "extra" entries as valid. This is to prevent CI failures due to rules being documented.
        // However, we consider "missing" entries as invalid. This is to force updating the file when new rules are added.
        if (!actualContent.Contains(line))
        {
            await Console.Error.WriteLineAsync($"Missing entry in '{fileWithPath}'. Please add the below entry to this file to fix the build:").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(line).ConfigureAwait(false);
            // The file is missing an entry. Mark it as invalid and break the loop as there is no need to continue validating.
            return 1;
        }
    }
    else
    {
        builder.AppendLine(line);
    }
}

if (!validateOnly)
{
    File.WriteAllText(fileWithPath, builder.ToString());
}

// NOTE: Network errors (timeouts and 5xx status codes) are not considered failures.
async Task<bool> checkHelpLinkAsync(string helpLink)
{
    try
    {
        if (!Uri.TryCreate(helpLink, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var request = new HttpRequestMessage(HttpMethod.Head, uri);
        using var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        var success = response?.StatusCode == HttpStatusCode.OK;

        if (!success && response is not null)
        {
            Console.WriteLine($"##[warning]Failed to check '{helpLink}': {response.StatusCode}");
            if ((int)response.StatusCode >= 500)
            {
                return true;
            }
        }

        return success;
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine($"##[warning]Timeout while checking '{helpLink}'.");
        return true;
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"##[warning]Failed while checking '{helpLink}' (${e.StatusCode}, ${e.HttpRequestError}): ${e.Message}");
        return true;
    }
}

static SortedList<string, DiagnosticDescriptor> getAllRulesById(string binDirectory, string configuration)
{
    var allRulesById = new SortedList<string, DiagnosticDescriptor>();

    foreach (string assembly in s_assemblies)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assembly);
        string path = Path.Combine(binDirectory, assemblyName, configuration, "netstandard2.0", assembly);
        if (!File.Exists(path))
        {
            throw new Exception($"'{path}' does not exist");
        }

        var analyzerFileReference = new AnalyzerFileReference(path, AnalyzerAssemblyLoader.Instance);
        analyzerFileReference.AnalyzerLoadFailed += (sender, e) => throw e.Exception ?? new NotSupportedException(e.Message);
        var analyzers = analyzerFileReference.GetAnalyzersForAllLanguages();

        foreach (var analyzer in analyzers)
        {
            foreach (var rule in analyzer.SupportedDiagnostics)
            {
                allRulesById[rule.Id] = rule;
            }
        }
    }

    return allRulesById;
}

return 0;

partial class Program
{
    private static readonly ImmutableArray<string> s_assemblies = ImmutableArray.Create(
        "Microsoft.CodeAnalysis.Features.dll",
        "Microsoft.CodeAnalysis.CSharp.Features.dll",
        "Microsoft.CodeAnalysis.VisualBasic.Features.dll");

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
