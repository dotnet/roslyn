// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Microsoft.CodeAnalysis.UseObjectInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;

using static CSharpSyntaxTokens;
using static SyntaxFactory;
using ObjectInitializerMatch = InitializerMatch<StatementSyntax>;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseObjectInitializer), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpUseObjectInitializerCodeFixProvider() :
    AbstractUseObjectInitializerCodeFixProvider<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseNamedMemberInitializerAnalyzer>
{
    protected override CSharpUseNamedMemberInitializerAnalyzer GetAnalyzer()
        => CSharpUseNamedMemberInitializerAnalyzer.Allocate();

    protected override ISyntaxFormatting SyntaxFormatting => CSharpSyntaxFormatting.Instance;

    protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

    protected override SyntaxTrivia Whitespace(string text)
        => SyntaxFactory.Whitespace(text);

    protected override StatementSyntax GetNewStatement(
        StatementSyntax statement,
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        return statement.ReplaceNode(
            objectCreation,
            GetNewObjectCreation(objectCreation, options, matches));
    }

    private BaseObjectCreationExpressionSyntax GetNewObjectCreation(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        return UseInitializerHelpers.GetNewObjectCreation(
            objectCreation,
            CreateExpressions(objectCreation, options, matches));
    }

    private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
        BaseObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<ObjectInitializerMatch> matches)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

        UseInitializerHelpers.AddExistingItems<ObjectInitializerMatch, ExpressionSyntax>(
            objectCreation, nodesAndTokens, addTrailingComma: true, static (_, e) => e);

        for (var i = 0; i < matches.Length; i++)
        {
            var match = matches[i];

            // After Pass 1 of the IDE0017+IDE0028 unification the match stores only `Statement`
            // and a `Kind` discriminator — the per-kind data (member-access for member-init,
            // argument expression for Add-fold) is recovered here from the statement. The
            // statement is always an `ExpressionStatementSyntax` because IDE0017's walk only
            // emits expression-statement-wrapped kinds (member assignment or Add invocation).
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
                    // csharplang#10185).
                    {
                        var invocation = (InvocationExpressionSyntax)expressionStatement.Expression;
                        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                        var memberAccessTrivia = memberAccess.GetLeadingTrivia();
                        var newAddTrivia = i == 0 ? memberAccessTrivia.WithoutLeadingBlankLines() : memberAccessTrivia;
                        var argument = invocation.ArgumentList.Arguments[0].Expression;
                        newElement = Indent(argument, options).WithLeadingTrivia(newAddTrivia);
                        break;
                    }

                default:
                    // IDE0017's walk never emits the other kinds today — guard against silent
                    // synthesis bugs if a future change extends the walk without extending here.
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
