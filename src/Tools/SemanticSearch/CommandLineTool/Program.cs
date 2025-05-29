// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using var workspace = MSBuildWorkspace.Create();

if (args is not [var solutionPath, var queryFilePath, ..])
{
    Console.Error.WriteLine("Usage: <SolutionPath> <QueryFilePath>");
    return -1;
}

var query = File.ReadAllText(queryFilePath, Encoding.UTF8);

var cancellationToken = CancellationToken.None;

var traceSource = new TraceSource("SemanticSearchTool", SourceLevels.Warning);

var referenceAssembliesDir = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "ReferenceAssemblies");

Console.WriteLine($"Loading '{solutionPath}' ...");

await workspace.OpenSolutionAsync(solutionPath);

var solution = workspace.CurrentSolution;

Console.WriteLine("Compiling query ...");

var service = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ISemanticSearchService>();
var compiledQuery = service.CompileQuery(solution.Services, query, referenceAssembliesDir, traceSource, cancellationToken);

if (compiledQuery.CompilationErrors.Any())
{
    foreach (var error in compiledQuery.CompilationErrors)
    {
        Console.Error.WriteLine(error.ToString());
    }

    return 1;
}

Console.WriteLine("Executing query ...");

var observer = new Observer();

var result = await service.ExecuteQueryAsync(solution, compiledQuery.QueryId, observer, new OptionsProvider(), traceSource, cancellationToken);

if (result.ErrorMessage != null)
{
    Console.Error.WriteLine(result.ErrorMessage, result.ErrorMessageArgs!);
    return 2;
}

var updatedSolution = await observer.GetUpdatedSolutionAsync(solution, cancellationToken);

Console.WriteLine("Validating updated solution ...");

int errorCount = 0;

await Parallel.ForEachAsync(solution.Projects, cancellationToken, async (project, cancellationToken) =>
{
    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
    if (compilation == null)
    {
        return;
    }

    foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
    {
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            Console.Error.WriteLine($"'{project.Name}': {diagnostic}");
            Interlocked.Increment(ref errorCount);
        }
    }
});

if (errorCount > 0)
{
    Console.Error.WriteLine($"Found {errorCount} errors in the updated solution.");
    return 3;
}

Console.WriteLine("No errors found.");

Console.WriteLine("Applying changes...");

if (!workspace.TryApplyChanges(updatedSolution))
{
    Console.Error.WriteLine("Unable to apply changes");
}
else
{
    Console.WriteLine("Changes applied.");
}

return 0;

class OptionsProvider : OptionsProvider<ClassificationOptions>
{
    public ValueTask<ClassificationOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => ValueTask.FromResult(ClassificationOptions.Default);
}

class Observer : ISemanticSearchResultsObserver
{
    private readonly ConcurrentStack<(DocumentId documentId, ImmutableArray<TextChange> changes)> _documentUpdates = new();

    public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Found: [|{definition.NameDisplayParts.ToVisibleDisplayString(includeLeftToRightMarker: false)}|]");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnLogMessageAsync(string message, CancellationToken cancellationToken)
    {
        Console.WriteLine(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
    {
        var message = $"Exception: {exception.TypeName.ToVisibleDisplayString(includeLeftToRightMarker: false)}: {exception.Message}{Environment.NewLine}{exception.StackTrace.ToVisibleDisplayString(includeLeftToRightMarker: false)}";
        Console.Error.WriteLine(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDocumentUpdatedAsync(DocumentId documentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        _documentUpdates.Push((documentId, changes));
        return ValueTask.CompletedTask;
    }

    public async ValueTask<Solution> GetUpdatedSolutionAsync(Solution oldSolution, CancellationToken cancellationToken)
    {
        var newSolution = oldSolution;

        foreach (var (documentId, changes) in _documentUpdates)
        {
            var document = newSolution.GetRequiredDocument(documentId);
            var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (changes.IsEmpty)
            {
                newSolution = newSolution.RemoveDocument(documentId);
            }
            else
            {
                // TODO: auto-format/clean up changed spans
                newSolution = newSolution.WithDocumentText(documentId, oldText.WithChanges(changes));

                var newDocument = newSolution.GetRequiredDocument(documentId);

                // TODO: postprocess:
                //var organizeImportsService = newDocument.GetRequiredLanguageService<IOrganizeImportsService>();
                //var options = await newDocument.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
                // newDocument = await organizeImportsService.OrganizeImportsAsync(newDocument, options, cancellationToken).ConfigureAwait(false);

                var updatedText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                newSolution = newSolution.WithDocumentText(newDocument.Id, updatedText);
            }

            Console.WriteLine($"updating '{document.FilePath}'");
        }

        return newSolution;
    }
}
