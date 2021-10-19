// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.EnableNullable)]
    [Shared]
    internal class EnableNullableCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly Func<DirectiveTriviaSyntax, bool> s_isNullableDirectiveTriviaPredicate =
            static directive => directive.IsKind(SyntaxKind.NullableDirectiveTrivia);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EnableNullableCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (!textSpan.IsEmpty)
                return;

            if (document.Project.CompilationOptions!.NullableContextOptions != NullableContextOptions.Disable)
                return;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start, findInsideTrivia: true);
            if (token.IsKind(SyntaxKind.EndOfDirectiveToken))
                token = root.FindToken(textSpan.Start - 1, findInsideTrivia: true);

            if (!token.IsKind(SyntaxKind.EnableKeyword) || !token.Parent.IsKind(SyntaxKind.NullableDirectiveTrivia))
                return;

            context.RegisterRefactoring(
                new MyCodeAction(cancellationToken => EnableNullableReferenceTypesAsync(document.Project, cancellationToken)));
        }

        public static async Task<Solution> EnableNullableReferenceTypesAsync(Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            foreach (var document in project.Documents)
            {
                var updatedDocumentRoot = await EnableNullableReferenceTypesAsync(document, cancellationToken).ConfigureAwait(false);
                solution = solution.WithDocumentSyntaxRoot(document.Id, updatedDocumentRoot);
            }

            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
            solution = solution.WithProjectCompilationOptions(project.Id, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
            return solution;
        }

        private static async Task<SyntaxNode> EnableNullableReferenceTypesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var firstToken = GetFirstTokenOfInterest(root);
            if (firstToken.IsKind(SyntaxKind.None))
            {
                // The document has no content, so it's fine to change the nullable context
                return root;
            }

            var firstNonDirectiveToken = root.GetFirstToken();
            var firstDirective = root.GetFirstDirective(s_isNullableDirectiveTriviaPredicate);
            if (firstNonDirectiveToken.IsKind(SyntaxKind.None) && firstDirective is null)
            {
                // The document has no semantic content, and also has no nullable directives to update
                return root;
            }

            // Update all prior nullable directives
            var directives = new List<NullableDirectiveTriviaSyntax>();
            for (var directive = firstDirective; directive.IsKind(SyntaxKind.NullableDirectiveTrivia); directive = directive.GetNextDirective(s_isNullableDirectiveTriviaPredicate))
            {
                directives.Add((NullableDirectiveTriviaSyntax)directive);
            }

            root = root.ReplaceNodes(
                directives,
                (originalNode, rewrittenNode) =>
                {
                    if (originalNode.SettingToken.IsKind(SyntaxKind.DisableKeyword))
                    {
                        // 'disable' keeps its meaning
                        return rewrittenNode;
                    }

                    if (originalNode.SettingToken.IsKind(SyntaxKind.RestoreKeyword))
                    {
                        return rewrittenNode.WithSettingToken(SyntaxFactory.Token(SyntaxKind.DisableKeyword).WithTriviaFrom(rewrittenNode.SettingToken));
                    }

                    if (originalNode.SettingToken.IsKind(SyntaxKind.EnableKeyword))
                    {
                        return rewrittenNode.WithSettingToken(SyntaxFactory.Token(SyntaxKind.RestoreKeyword).WithTriviaFrom(rewrittenNode.SettingToken));
                    }

                    Debug.Fail("Unexpected state?");
                    return rewrittenNode;
                });

            firstToken = GetFirstTokenOfInterest(root);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var newLine = SyntaxFactory.EndOfLine(options.GetOption(FormattingOptions2.NewLine));
            SyntaxNode updatedRoot;

            // Add a new '#nullable disable' to the top of each file
            if (!HasLeadingNullableDirective(root, out var leadingDirective))
            {
                var nullableDisableTrivia = SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword).WithPrependedLeadingTrivia(SyntaxFactory.ElasticSpace), isActive: true));
                updatedRoot = root.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(firstToken.LeadingTrivia.Add(nullableDisableTrivia).Add(newLine).Add(newLine)));
            }
            else if (leadingDirective.SettingToken.IsKind(SyntaxKind.RestoreKeyword) && leadingDirective.TargetToken.IsKind(SyntaxKind.None))
            {
                updatedRoot = root.ReplaceTrivia(leadingDirective.ParentTrivia, SyntaxFactory.ElasticMarker);
            }
            else
            {
                // No need to add a '#nullable disable' directive because the file already starts with an unconditional
                // '#nullable' directive that will override it.
                updatedRoot = root;
            }

            return updatedRoot;
        }

        private static SyntaxToken GetFirstTokenOfInterest(SyntaxNode root)
        {
            var firstToken = root.GetFirstToken(includeDirectives: true);
            if (firstToken.IsKind(SyntaxKind.None))
            {
                return firstToken;
            }

            if (firstToken.IsKind(SyntaxKind.HashToken) && firstToken.Parent.IsKind(SyntaxKind.RegionDirectiveTrivia))
            {
                // If the file starts with a #region/#endregion that contains no semantic content (e.g. just a file
                // header), skip it.
                var nextToken = firstToken.Parent.GetLastToken(includeDirectives: true).GetNextToken(includeDirectives: true);
                if (nextToken.IsKind(SyntaxKind.HashToken) && nextToken.Parent.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                {
                    firstToken = nextToken.Parent.GetLastToken(includeDirectives: true).GetNextToken(includeDirectives: true);
                }
            }

            return firstToken;
        }

        private static bool HasLeadingNullableDirective(SyntaxNode root, [NotNullWhen(true)] out NullableDirectiveTriviaSyntax? leadingNullableDirective)
        {
            // A leading nullable directive is a '#nullable' directive which precedes any conditional directives ('#if')
            // or code (non-trivia).
            var firstRelevantDirective = root.GetFirstDirective(static directive => directive.IsKind(SyntaxKind.NullableDirectiveTrivia, SyntaxKind.IfDirectiveTrivia));
            if (firstRelevantDirective.IsKind(SyntaxKind.NullableDirectiveTrivia, out NullableDirectiveTriviaSyntax? nullableDirective)
                && nullableDirective.TargetToken.IsKind(SyntaxKind.None))
            {
                var firstSemanticToken = root.GetFirstToken();
                if (firstSemanticToken.IsKind(SyntaxKind.None) || firstSemanticToken.SpanStart > nullableDirective.Span.End)
                {
                    leadingNullableDirective = nullableDirective;
                    return true;
                }
            }

            leadingNullableDirective = null;
            return false;
        }

        private sealed class MyCodeAction : CodeActions.CodeAction.SolutionChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(CSharpFeaturesResources.Enable_nullable_reference_types, createChangedSolution, nameof(CSharpFeaturesResources.Enable_nullable_reference_types))
            {
            }
        }
    }
}
