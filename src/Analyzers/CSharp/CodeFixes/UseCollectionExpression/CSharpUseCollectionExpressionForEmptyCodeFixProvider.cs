// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForEmpty), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseCollectionExpressionForEmptyCodeFixProvider()
    : AbstractUseCollectionExpressionCodeFixProvider<SyntaxNode>(
        CSharpCodeFixesResources.Use_collection_expression,
        IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId)
{
    private static readonly CollectionExpressionSyntax s_emptyCollection = CollectionExpression();

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId];

    protected override Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        SyntaxNode diagnosticNode,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        editor.ReplaceNode(
            diagnosticNode,
            (current, _) => s_emptyCollection.WithTriviaFrom(current));
        return Task.CompletedTask;
    }
}
