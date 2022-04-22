// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal sealed class FixAllState : CommonFixAllState<CodeRefactoringProvider, FixAllProvider, FixAllState>
    {
        public override FixAllKind FixAllKind => FixAllKind.Refactoring;

        public string CodeActionTitle { get; }

        /// <summary>
        /// Original selection span from which FixAll was invoked
        /// </summary>
        public TextSpan SelectionSpan { get; }

        internal FixAllState(
            FixAllProvider fixAllProvider,
            Document document!!,
            TextSpan selectionSpan,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            CodeAction codeAction)
            : this(fixAllProvider, document, document.Project, selectionSpan, codeRefactoringProvider, fixAllScope, codeAction.Title, codeAction.EquivalenceKey)
        {
        }

        internal FixAllState(
            FixAllProvider fixAllProvider,
            Project project!!,
            TextSpan selectionSpan,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            CodeAction codeAction)
            : this(fixAllProvider, document: null, project, selectionSpan, codeRefactoringProvider, fixAllScope, codeAction.Title, codeAction.EquivalenceKey)
        {
        }

        private FixAllState(
            FixAllProvider fixAllProvider,
            Document? document,
            Project project,
            TextSpan selectionSpan,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            string codeActionTitle,
            string? codeActionEquivalenceKey)
            : base(fixAllProvider, document, project, codeRefactoringProvider, fixAllScope, codeActionEquivalenceKey)
        {
            this.SelectionSpan = selectionSpan;
            this.CodeActionTitle = codeActionTitle;
        }

        protected override FixAllState With(Document? document, Project project, FixAllScope scope, string? codeActionEquivalenceKey)
        {
            return new FixAllState(
                this.FixAllProvider,
                document,
                project,
                this.SelectionSpan,
                this.Provider,
                scope,
                this.CodeActionTitle,
                codeActionEquivalenceKey);
        }

        /// <summary>
        /// Gets the spans to fix by document for the <see cref="FixAllScope"/> for this fix all occurences fix.
        /// </summary>
        internal async Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Document>? documentsToFix = null;
            switch (this.Scope)
            {
                case FixAllScope.ContainingType or FixAllScope.ContainingMember:
                    Contract.ThrowIfNull(Document);
                    var spanMappingService = Document.GetLanguageService<IFixAllSpanMappingService>();
                    if (spanMappingService is null)
                        return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty;

                    return await spanMappingService.GetFixAllSpansAsync(
                        Document, SelectionSpan, Scope, cancellationToken).ConfigureAwait(false);

                case FixAllScope.Document:
                    Contract.ThrowIfNull(Document);
                    documentsToFix = SpecializedCollections.SingletonEnumerable(Document);
                    break;

                case FixAllScope.Project:
                    documentsToFix = Project.Documents;
                    break;

                case FixAllScope.Solution:
                    documentsToFix = Project.Solution.Projects.SelectMany(p => p.Documents);
                    break;

                default:
                    return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty;
            }

            using var _ = PooledDictionary<Document, ImmutableArray<TextSpan>>.GetInstance(out var builder);
            foreach (var document in documentsToFix)
            {
                TextSpan span;
                if (document.SupportsSyntaxTree)
                {
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    span = root.FullSpan;
                }
                else
                {
                    span = default;
                }

                builder.Add(document, ImmutableArray.Create(span));
            }

            return builder.ToImmutableDictionary();
        }
    }
}
