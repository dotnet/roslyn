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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.SimplifyLinqExpression), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class SimplifyLinqExpressionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
       => [IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Simplify_LINQ_expression, nameof(AnalyzersResources.Simplify_LINQ_expression));
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics.OrderByDescending(diagnostics => diagnostics.Location.SourceSpan.Start))
        {
            var invocation = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            editor.ReplaceNode(invocation, (current, generator) =>
            {
                // 'current' is the original full expression.  like x.Where(...).Count();

                // 'x.Where(...).Count' in the above expression
                var memberAccess = syntaxFacts.GetExpressionOfInvocationExpression(current);

                // 'Count' in the above expression
                var outerName = syntaxFacts.GetNameOfMemberAccessExpression(memberAccess);

                // 'x.Where(...)' in the above expression.
                var innerInvocationExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess)!;

                // We originally walked an IOperation tree, not a syntax tree.  So we have to unwrap any superfluous
                // wrapper nodes in syntax node represented in IOpt.
                while (true)
                {
                    if (syntaxFacts.IsParenthesizedExpression(innerInvocationExpression))
                    {
                        innerInvocationExpression = syntaxFacts.GetExpressionOfParenthesizedExpression(innerInvocationExpression);
                        continue;
                    }

                    if (syntaxFacts.IsSuppressNullableWarningExpression(innerInvocationExpression))
                    {
                        innerInvocationExpression = syntaxFacts.GetOperandOfPostfixUnaryExpression(innerInvocationExpression);
                        continue;
                    }

                    break;
                }


                // 'x.Where' in the above expression.
                var innerMemberAccessExpression = syntaxFacts.GetExpressionOfInvocationExpression(innerInvocationExpression);

                // 'Where' in the above expression.
                var innerName = syntaxFacts.GetNameOfMemberAccessExpression(innerMemberAccessExpression);

                // trim down to the 'x.Where(...)', except with 'Where' replaced with 'Count'.
                return innerInvocationExpression.ReplaceNode(innerName, outerName.WithTriviaFrom(innerName)).WithTrailingTrivia(current.GetTrailingTrivia());
            });
        }
    }
}
