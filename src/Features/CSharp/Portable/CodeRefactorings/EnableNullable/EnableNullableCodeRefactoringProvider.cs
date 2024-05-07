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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.EnableNullable), Shared]
    internal partial class EnableNullableCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly Func<DirectiveTriviaSyntax, bool> s_isNullableDirectiveTriviaPredicate =
            directive => directive.IsKind(SyntaxKind.NullableDirectiveTrivia);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public EnableNullableCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (!textSpan.IsEmpty)
                return;

            if (!ShouldOfferRefactoring(document.Project))
            {
                return;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start, findInsideTrivia: true);
            if (token.IsKind(SyntaxKind.EndOfDirectiveToken))
                token = root.FindToken(textSpan.Start - 1, findInsideTrivia: true);

            if (token.Kind() is not (SyntaxKind.EnableKeyword or SyntaxKind.RestoreKeyword or SyntaxKind.DisableKeyword or SyntaxKind.NullableKeyword or SyntaxKind.HashToken) ||
                token.Parent is not NullableDirectiveTriviaSyntax nullableDirectiveTrivia)
            {
                return;
            }

            context.RegisterRefactoring(new CustomCodeAction(
                (purpose, progress, cancellationToken) => EnableNullableReferenceTypesAsync(document.Project, purpose, context.Options, progress, cancellationToken)));
        }

        private static bool ShouldOfferRefactoring(Project project)
            => project is
            {
                ParseOptions: CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp8 },
                CompilationOptions.NullableContextOptions: NullableContextOptions.Disable,
            };

        private static async Task<Solution> EnableNullableReferenceTypesAsync(
            Project project, CodeActionPurpose purpose, CodeActionOptionsProvider fallbackOptions, IProgress<CodeAnalysisProgress> _, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            foreach (var document in project.Documents)
            {
                if (await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                    continue;

                var updatedDocumentRoot = await EnableNullableReferenceTypesAsync(document, fallbackOptions, cancellationToken).ConfigureAwait(false);
                solution = solution.WithDocumentSyntaxRoot(document.Id, updatedDocumentRoot);
            }

            if (purpose is CodeActionPurpose.Apply)
            {
                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                solution = solution.WithProjectCompilationOptions(project.Id, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
            }

            return solution;
        }

        private static async Task<SyntaxNode> EnableNullableReferenceTypesAsync(Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var firstToken = GetFirstTokenOfInterest(root);
            if (firstToken.IsKind(SyntaxKind.None))
            {
                // The document has no content, so it's fine to change the nullable context
                return root;
            }

            // Update #nullable directives that already exist in the document
            (root, firstToken) = RewriteExistingDirectives(root, firstToken);

            // Update existing documents to retain their original semantics
            //
            // * Add '#nullable disable' if the document didn't specify other semantics
            // * Remove leading '#nullable restore' (was '#nullable enable' prior to rewrite in the previous step)
            // * Otherwise, leave existing '#nullable' directive since it will control the initial semantics for the document
            return await DisableNullableReferenceTypesInExistingDocumentIfNecessaryAsync(document, root, firstToken, fallbackOptions, cancellationToken).ConfigureAwait(false);
        }

        private static (SyntaxNode root, SyntaxToken firstToken) RewriteExistingDirectives(SyntaxNode root, SyntaxToken firstToken)
        {
            var firstNonDirectiveToken = root.GetFirstToken();
            var firstDirective = root.GetFirstDirective(s_isNullableDirectiveTriviaPredicate);
            if (firstNonDirectiveToken.IsKind(SyntaxKind.None) && firstDirective is null)
            {
                // The document has no semantic content, and also has no nullable directives to update
                return (root, firstToken);
            }

            // Update all prior nullable directives
            var directives = new List<NullableDirectiveTriviaSyntax>();
            for (var directive = firstDirective; directive is not null; directive = directive.GetNextDirective(s_isNullableDirectiveTriviaPredicate))
            {
                directives.Add((NullableDirectiveTriviaSyntax)directive);
            }

            var updatedRoot = root.ReplaceNodes(
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

            return (updatedRoot, GetFirstTokenOfInterest(updatedRoot));
        }

        private static async Task<SyntaxNode> DisableNullableReferenceTypesInExistingDocumentIfNecessaryAsync(Document document, SyntaxNode root, SyntaxToken firstToken, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var options = await document.GetCSharpCodeFixOptionsProviderAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var newLine = SyntaxFactory.EndOfLine(options.NewLine);

            // Add a new '#nullable disable' to the top of each file
            if (!HasLeadingNullableDirective(root, out var leadingDirective))
            {
                var nullableDisableTrivia = SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword).WithPrependedLeadingTrivia(SyntaxFactory.ElasticSpace), isActive: true));

                var existingTriviaList = firstToken.LeadingTrivia;
                var insertionIndex = GetInsertionPoint(existingTriviaList);

                return root.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(existingTriviaList.InsertRange(insertionIndex, new[] { nullableDisableTrivia, newLine, newLine })));
            }
            else if (leadingDirective.SettingToken.IsKind(SyntaxKind.RestoreKeyword) && leadingDirective.TargetToken.IsKind(SyntaxKind.None))
            {
                // Remove the leading `#nullable restore` directive because it's redundant. Since there is no
                // RemoveTrivia call, we replace the trivia with an empty marker.
                return root.ReplaceTrivia(leadingDirective.ParentTrivia, SyntaxFactory.ElasticMarker);
            }
            else
            {
                // No need to add a '#nullable disable' directive because the file already starts with an unconditional
                // '#nullable' directive that will override it.
                return root;
            }
        }

        private static int GetInsertionPoint(SyntaxTriviaList list)
        {
            var insertionPoint = list.Count;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                switch (list[i].Kind())
                {
                    case SyntaxKind.WhitespaceTrivia:
                    case SyntaxKind.EndOfLineTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        continue;

                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        // Insert before the documentation comment
                        insertionPoint = i;
                        continue;

                    default:
                        return insertionPoint;
                }
            }

            return insertionPoint;
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
            var firstRelevantDirective = root.GetFirstDirective(static directive => directive.Kind() is SyntaxKind.NullableDirectiveTrivia or SyntaxKind.IfDirectiveTrivia);
            if (firstRelevantDirective is NullableDirectiveTriviaSyntax nullableDirective
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

        private enum CodeActionPurpose
        {
            Preview,
            Apply,
        }

        private sealed class CustomCodeAction(
            Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> createChangedSolution)
            : CodeAction.SolutionChangeAction(
                CSharpFeaturesResources.Enable_nullable_reference_types_in_project,
                (progress, cancellationToken) => createChangedSolution(CodeActionPurpose.Apply, progress, cancellationToken),
                nameof(CSharpFeaturesResources.Enable_nullable_reference_types_in_project))
        {
            private readonly Func<CodeActionPurpose, IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> _createChangedSolution = createChangedSolution;

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                var changedSolution = await _createChangedSolution(CodeActionPurpose.Preview, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
                if (changedSolution is null)
                    return Array.Empty<CodeActionOperation>();

                return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
            }
        }
    }
}
