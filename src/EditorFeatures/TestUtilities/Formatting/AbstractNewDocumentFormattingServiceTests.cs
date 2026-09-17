// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.Formatting;

[UseExportProvider]
public abstract class AbstractNewDocumentFormattingServiceTests
{
    protected abstract string Language { get; }
    protected abstract EditorTestWorkspace CreateTestWorkspace(string testCode, ParseOptions? parseOptions);

    internal async Task TestAsync(
        string testCode,
        string expected,
        OptionsCollection? options = null,
        ParseOptions? parseOptions = null,
        string? hintDocumentText = null)
    {
        using var workspace = CreateTestWorkspace(testCode, parseOptions);
        options?.SetGlobalOptions(workspace.GlobalOptions);

        var document = workspace.CurrentSolution.Projects.First().Documents.First();
        Document? hintDocument = null;
        if (hintDocumentText is not null)
        {
            hintDocument = document.Project.AddDocument("Hint", SourceText.From(hintDocumentText));
            document = hintDocument.Project.GetDocument(document.Id)!;
        }

        var languageServices = document.Project.Services;

        var cleanupOptions =
            options?.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions: false) ??
            await document.GetCodeCleanupOptionsAsync(CancellationToken.None);

        var formattingService = document.GetRequiredLanguageService<INewDocumentFormattingService>();
        var formattedDocument = await formattingService.FormatNewDocumentAsync(document, hintDocument, cleanupOptions, CancellationToken.None);

        var actual = await formattedDocument.GetTextAsync();
        AssertEx.EqualOrDiff(expected, actual.ToString());
    }
}
