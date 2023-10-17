﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal partial class CSharpUseCollectionInitializerCodeFixProvider() :
    AbstractUseCollectionInitializerCodeFixProvider<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override async Task<(SyntaxNode, SyntaxNode)> GetReplacementNodesAsync(
        Document document,
        CodeActionOptionsProvider fallbackOptions,
        BaseObjectCreationExpressionSyntax objectCreation,
        bool useCollectionExpression,
        ImmutableArray<Match<StatementSyntax>> matches,
        CancellationToken cancellationToken)
    {
        var newObjectCreation = await GetNewObjectCreationAsync(
            document, fallbackOptions, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);
        return (objectCreation, newObjectCreation);
    }

    private static async Task<ExpressionSyntax> GetNewObjectCreationAsync(
        Document document,
        CodeActionOptionsProvider fallbackOptions,
        BaseObjectCreationExpressionSyntax objectCreation,
        bool useCollectionExpression,
        ImmutableArray<Match<StatementSyntax>> matches,
        CancellationToken cancellationToken)
    {
        return useCollectionExpression
            ? await CreateCollectionExpressionAsync(document, fallbackOptions, objectCreation, matches, cancellationToken).ConfigureAwait(false)
            : CreateObjectInitializerExpression(objectCreation, matches);
    }
}
