// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember;

[UseExportProvider]
public abstract class AbstractGoToAdjacentMemberTests
{
    protected abstract string LanguageName { get; }
    protected abstract ParseOptions DefaultParseOptions { get; }

    protected async Task AssertNavigatedAsync(string code, bool next, SourceCodeKind? sourceCodeKind = null)
    {
        var kinds = sourceCodeKind != null
            ? SpecializedCollections.SingletonEnumerable(sourceCodeKind.Value)
            : [SourceCodeKind.Regular, SourceCodeKind.Script];

        foreach (var kind in kinds)
        {
            using var workspace = TestWorkspace.Create(
                LanguageName,
                compilationOptions: null,
                parseOptions: DefaultParseOptions.WithKind(kind),
                content: code);
            var hostDocument = workspace.DocumentWithCursor;
            var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            var parsedDocument = await ParsedDocument.CreateAsync(document, CancellationToken.None);
            Assert.Empty(parsedDocument.SyntaxTree.GetDiagnostics());
            var service = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var targetPosition = GoToAdjacentMemberCommandHandler.GetTargetPosition(
                service,
                parsedDocument.Root,
                hostDocument.CursorPosition.Value,
                next);

            Assert.NotNull(targetPosition);
            Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value);
        }
    }

    protected async Task<int?> GetTargetPositionAsync(string code, bool next)
    {
        using var workspace = TestWorkspace.Create(
            LanguageName,
            compilationOptions: null,
            parseOptions: DefaultParseOptions,
            content: code);
        var hostDocument = workspace.DocumentWithCursor;
        var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
        var parsedDocument = await ParsedDocument.CreateAsync(document, CancellationToken.None);
        Assert.Empty(parsedDocument.SyntaxTree.GetDiagnostics());

        return GoToAdjacentMemberCommandHandler.GetTargetPosition(
            document.GetRequiredLanguageService<ISyntaxFactsService>(),
            parsedDocument.Root,
            hostDocument.CursorPosition.Value,
            next);
    }
}
