// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.FixAll;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal partial class FixAllState
    {
        internal readonly int CorrelationId = LogAggregator.GetNextId();

        public FixAllProvider FixAllProvider { get; }
        public CodeAction CodeAction { get; }
        public CodeRefactoringProvider CodeRefactoringProvider { get; }
        public Document? Document { get; }
        public Project Project { get; }
        public FixAllScope FixAllScope { get; }
        public Solution Solution => this.Project.Solution;

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
            : this(fixAllProvider, document, document.Project, selectionSpan, codeRefactoringProvider, fixAllScope, codeAction)
        {
        }

        internal FixAllState(
            FixAllProvider fixAllProvider,
            Project project!!,
            TextSpan selectionSpan,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            CodeAction codeAction)
            : this(fixAllProvider, document: null, project, selectionSpan, codeRefactoringProvider, fixAllScope, codeAction)
        {
        }

        private FixAllState(
            FixAllProvider fixAllProvider,
            Document? document,
            Project project,
            TextSpan selectionSpan,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            CodeAction codeAction)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfNull(codeRefactoringProvider);

            this.FixAllProvider = fixAllProvider;
            this.Document = document;
            this.Project = project;
            this.SelectionSpan = selectionSpan;
            this.CodeRefactoringProvider = codeRefactoringProvider;
            this.FixAllScope = fixAllScope;
            this.CodeAction = codeAction;
        }

        public FixAllState WithDocument(Document? document)
            => this.With(document: document);

        public FixAllState WithProject(Project project)
            => this.With(project: project);

        public FixAllState WithScope(FixAllScope scope)
            => this.With(scope: scope);

        public FixAllState With(
            Optional<Document?> document = default,
            Optional<Project> project = default,
            Optional<FixAllScope> scope = default)
        {
            var newDocument = document.HasValue ? document.Value : this.Document;
            var newProject = project.HasValue ? project.Value : this.Project;
            var newFixAllScope = scope.HasValue ? scope.Value : this.FixAllScope;

            if (newDocument == this.Document &&
                newProject == this.Project &&
                newFixAllScope == this.FixAllScope)
            {
                return this;
            }

            return new FixAllState(
                this.FixAllProvider,
                newDocument,
                newProject,
                this.SelectionSpan,
                this.CodeRefactoringProvider,
                newFixAllScope,
                this.CodeAction);
        }

        /// <summary>
        /// Gets the spans to fix by document for the <see cref="FixAllScope"/> for this fix all occurences fix.
        /// </summary>
        internal async Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Document>? documentsToFix = null;
            switch (FixAllScope)
            {
                case FixAllScope.ContainingType or FixAllScope.ContainingMember:
                    Contract.ThrowIfNull(Document);
                    var spanMappingService = Document.GetLanguageService<IFixAllSpanMappingService>();
                    if (spanMappingService is null)
                        return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty;

                    return await spanMappingService.GetFixAllSpansAsync(
                        Document, SelectionSpan, FixAllScope, cancellationToken).ConfigureAwait(false);

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
