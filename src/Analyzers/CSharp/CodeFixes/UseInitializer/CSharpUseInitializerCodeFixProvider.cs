// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Microsoft.CodeAnalysis.UseInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseInitializer;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

/// <summary>
/// Pass 3 of the IDE0017+IDE0028 unification: a single C# fix provider class registered for
/// IDE0017 (object-initializer), IDE0028 (collection-initializer), and IDE0400 (mixed
/// object/collection initializer), backed by a single walk
/// (<see cref="CSharpUseCollectionInitializerAnalyzer"/>). Replaces the prior
/// <c>CSharpUseObjectInitializerCodeFixProvider</c> and
/// <c>CSharpUseCollectionInitializerCodeFixProvider</c> classes, and removes the
/// per-language member-initializer walk dependency. Member-initializer synthesis lives in
/// this file; the collection-initializer and collection-expression synthesizers continue
/// to live in their existing partial files
/// (<c>CSharpUseCollectionInitializerCodeFixProvider_CollectionInitializer.cs</c> and
/// <c>_CollectionExpression.cs</c>), now declared as partials of this new class.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.UseImplicitObjectCreation)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpUseInitializerCodeFixProvider() :
    AbstractUseInitializerCodeFixProvider<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        // TAssignmentStatementSyntax — `=` and compound `op=` statements (also Add invocations
        // for the mixed-init Add-fold path) all bind to `ExpressionStatementSyntax` in C#.
        ExpressionStatementSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override ISyntaxFormatting SyntaxFormatting => CSharpSyntaxFormatting.Instance;

    protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

    protected override SyntaxTrivia Whitespace(string text)
        => SyntaxFactory.Whitespace(text);

    protected override async Task<(SyntaxNode oldNode, SyntaxNode newNode)> GetReplacementNodesForCollectionInitAsync(
        Document document,
        BaseObjectCreationExpressionSyntax objectCreation,
        bool useCollectionExpression,
        ImmutableArray<InitializerMatch<SyntaxNode>> preMatches,
        ImmutableArray<InitializerMatch<SyntaxNode>> postMatches,
        CancellationToken cancellationToken)
    {
        ExpressionSyntax newObjectCreation = useCollectionExpression
            ? await CreateCollectionExpressionAsync(document, objectCreation, preMatches, postMatches, cancellationToken).ConfigureAwait(false)
            : CreateObjectInitializerExpression(objectCreation, postMatches);

        return (objectCreation, newObjectCreation);
    }

    protected override StatementSyntax GetNewStatementForMemberInit(
        StatementSyntax statement,
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<InitializerMatch<SyntaxNode>> matches)
    {
        return statement.ReplaceNode(
            objectCreation,
            GetNewObjectCreationForMemberInit(objectCreation, options, matches));
    }

    private BaseObjectCreationExpressionSyntax GetNewObjectCreationForMemberInit(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<InitializerMatch<SyntaxNode>> matches)
    {
        return UseInitializerHelpers.GetNewObjectCreation(
            objectCreation,
            CreateMemberInitExpressions(objectCreation, options, matches));
    }

    private SeparatedSyntaxList<ExpressionSyntax> CreateMemberInitExpressions(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<InitializerMatch<SyntaxNode>> matches)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

        UseInitializerHelpers.AddExistingItems<InitializerMatch<SyntaxNode>, ExpressionSyntax>(
            objectCreation, nodesAndTokens, addTrailingComma: true, static (_, e) => e);

        for (var i = 0; i < matches.Length; i++)
        {
            var match = matches[i];

            // After Pass 1 of the IDE0017+IDE0028 unification the match stores only `Node`
            // and a `Kind` discriminator — the per-kind data (member-access for member-init,
            // argument expression for Add-fold) is recovered here from the statement. The
            // statement is always an `ExpressionStatementSyntax` because the member-initializer
            // walk only emits expression-statement-wrapped kinds (member assignment or Add
            // invocation).
            var expressionStatement = (ExpressionStatementSyntax)match.Node;

            ExpressionSyntax newElement;
            switch (match.Kind)
            {
                case InitializerMatchKind.MemberInitializer:
                    // `x.Name = value` (or any compound `x.Name op= value`). Detach the
                    // receiver and emit the bare `Name = value` (or compound) form.
                    {
                        var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
                        var memberAccess = (MemberAccessExpressionSyntax)assignment.Left;
                        var memberAccessTrivia = memberAccess.GetLeadingTrivia();
                        var newMemberTrivia = i == 0 ? memberAccessTrivia.WithoutLeadingBlankLines() : memberAccessTrivia;
                        newElement = assignment
                            .WithLeft(memberAccess.Name.WithLeadingTrivia(newMemberTrivia))
                            .WithRight(Indent(assignment.Right, options));
                        break;
                    }

                case InitializerMatchKind.AddInvocation:
                    // `x.Add(value)` — emit the bare argument expression as a collection
                    // element initializer (the mixed object/collection initializer shape per
                    // csharplang#10185). For multi-argument `x.Add(a, b)` the synthesized
                    // element is a brace-list `{ a, b }`, matching the legacy IDE0028
                    // collection-init synthesis for multi-arg Add. Pass 3b reuses IDE0028's
                    // Add detection in the unified walk so multi-arg Add now reaches this
                    // fixer (pre-Pass-3b PR 5's `TryMatchAddInvocation` only emitted single-
                    // arg Add matches).
                    {
                        var invocation = (InvocationExpressionSyntax)expressionStatement.Expression;
                        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                        var memberAccessTrivia = memberAccess.GetLeadingTrivia();
                        var newAddTrivia = i == 0 ? memberAccessTrivia.WithoutLeadingBlankLines() : memberAccessTrivia;
                        var arguments = invocation.ArgumentList.Arguments;
                        ExpressionSyntax addElement = arguments.Count == 1
                            ? arguments[0].Expression
                            : InitializerExpression(
                                SyntaxKind.ComplexElementInitializerExpression,
                                SeparatedList(arguments.Select(a => a.Expression), arguments.GetSeparators()));
                        newElement = Indent(addElement, options).WithLeadingTrivia(newAddTrivia);
                        break;
                    }

                default:
                    // The member-initializer walk never emits the other kinds today — guard
                    // against silent synthesis bugs if a future change extends the walk
                    // without extending here.
                    throw ExceptionUtilities.UnexpectedValue(match.Kind);
            }

            if (i < matches.Length - 1)
            {
                nodesAndTokens.Add(newElement);
                nodesAndTokens.Add(CommaToken.WithTriviaFrom(expressionStatement.SemicolonToken));
            }
            else
            {
                newElement = newElement.WithTrailingTrivia(
                    expressionStatement.GetTrailingTrivia());
                nodesAndTokens.Add(newElement);
            }
        }

        return SeparatedList<ExpressionSyntax>(nodesAndTokens);
    }
}
