// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
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

            if (!token.IsKind(SyntaxKind.EnableKeyword))
                return;

            context.RegisterRefactoring(
                new MyCodeAction(cancellationToken => EnableNullableReferenceTypesAsync(document.Project.Solution, document.Project.Id, cancellationToken)));
        }

        public static async Task<Solution> EnableNullableReferenceTypesAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken)
        {
            var project = solution.GetRequiredProject(projectId);
            foreach (var document in project.Documents)
            {
                var updatedDocumentRoot = await EnableNullableReferenceTypesAsync(document, cancellationToken).ConfigureAwait(false);
                solution = solution.WithDocumentSyntaxRoot(document.Id, updatedDocumentRoot);
            }

            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
            solution = solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
            return solution;
        }

        private static async Task<SyntaxNode> EnableNullableReferenceTypesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var firstToken = root.GetFirstToken(includeDirectives: true);
            if (firstToken.IsKind(SyntaxKind.None))
            {
                // The document has no content, so it's fine to change the nullable context
                return root;
            }

            var firstNonDirectiveToken = root.GetFirstToken();
            var firstDirective = root.GetFirstDirective(s_isNullableDirectiveTriviaPredicate);
            if (firstNonDirectiveToken.IsKind(SyntaxKind.None) && firstDirective.IsKind(SyntaxKind.None))
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

            firstToken = root.GetFirstToken(includeDirectives: true);

            // Add a new '#nullable disable' to the top of each file
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var newLine = SyntaxFactory.EndOfLine(options.GetOption(FormattingOptions2.NewLine));
            var nullableDisableTrivia = SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword).WithPrependedLeadingTrivia(SyntaxFactory.ElasticSpace), isActive: true));
            var updatedRoot = root.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(firstToken.LeadingTrivia.Add(nullableDisableTrivia).Add(newLine).Add(newLine)));

            // TODO: remove all '#nullable disable' directives in code with no nullable types

            // TODO: remove all redundant directives

            return updatedRoot;
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
