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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyLinqTypeCheckAndCast), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpSimplifyLinqTypeCheckAndCastCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.SimplifyLinqTypeCheckAndCastDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Simplify_LINQ_expression, nameof(AnalyzersResources.Simplify_LINQ_expression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        // Because the pattern is very specific (`.Where(a => a is Type).Cast<Type>()`), we know that no diagnostic can
        // be nested in another.  So we don't have to process these inside-out like we do with other fixers.
        foreach (var diagnostic in diagnostics)
        {
            var castOrSelectInvocation = (InvocationExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var typeSyntax = (TypeSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);

            var castOrSelectMemberAccess = (MemberAccessExpressionSyntax)castOrSelectInvocation.Expression;
            var castOrSelectName = castOrSelectMemberAccess.Name;
            var castOrSelectNameToken = castOrSelectName.Identifier;

            var ofTypeToken = Identifier(nameof(Enumerable.OfType)).WithTriviaFrom(castOrSelectNameToken);
            if (castOrSelectName is GenericNameSyntax)
            {
                // Change .Cast<T>() to .OfType<T>()
                editor.ReplaceNode(
                    castOrSelectName,
                    castOrSelectName.ReplaceToken(castOrSelectNameToken, ofTypeToken));
            }
            else
            {
                // Change .Select(...) to .OfType<T>()
                editor.ReplaceNode(
                    castOrSelectName,
                    GenericName(ofTypeToken).AddTypeArgumentListArguments(typeSyntax.WithoutTrivia()));
                editor.ReplaceNode(
                    castOrSelectInvocation.ArgumentList,
                    castOrSelectInvocation.ArgumentList.WithArguments([]));
            }

            var whereInvocation = (InvocationExpressionSyntax)castOrSelectMemberAccess.Expression;
            var whereMemberAccess = (MemberAccessExpressionSyntax)whereInvocation.Expression;

            // Snip out the `.Where(...)` portion so that `expr.Where(...).OfType<T>()` becomes `expr.OfType<T>()`
            editor.ReplaceNode(whereInvocation, whereMemberAccess.Expression);
        }

        return Task.CompletedTask;
    }
}
