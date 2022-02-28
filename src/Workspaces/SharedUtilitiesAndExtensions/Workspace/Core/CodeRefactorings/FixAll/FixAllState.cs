// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal partial class FixAllState
    {
        internal readonly int CorrelationId = LogAggregator.GetNextId();

        public FixAllProvider? FixAllProvider { get; }
        public CodeAction CodeAction { get; }
        public CodeRefactoringProvider CodeRefactoringProvider { get; }
        public Document? Document { get; }
        public Project Project { get; }
        public FixAllScope FixAllScope { get; }
        public TextSpan? FixAllSpan { get; }
        public Solution Solution => this.Project.Solution;

        internal FixAllState(
            FixAllProvider? fixAllProvider,
            Document document,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            TextSpan? fixAllSpan,
            CodeAction codeAction)
            : this(fixAllProvider, document, document.Project,  codeRefactoringProvider, fixAllScope, fixAllSpan, codeAction)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
        }

        internal FixAllState(
            FixAllProvider? fixAllProvider,
            Project project,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            CodeAction codeAction)
            : this(fixAllProvider, document: null, project, codeRefactoringProvider, fixAllScope, fixAllSpan: null, codeAction)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
        }

        private FixAllState(
            FixAllProvider? fixAllProvider,
            Document? document,
            Project project,
            CodeRefactoringProvider codeRefactoringProvider,
            FixAllScope fixAllScope,
            TextSpan? fixAllSpan,
            CodeAction codeAction)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfFalse(!fixAllSpan.HasValue || fixAllScope is FixAllScope.Document or
                FixAllScope.Selection or FixAllScope.ContainingMember or FixAllScope.ContainingType);

            this.FixAllProvider = fixAllProvider;
            this.Document = document;
            this.Project = project;
            this.CodeRefactoringProvider = codeRefactoringProvider ?? throw new ArgumentNullException(nameof(codeRefactoringProvider));
            this.FixAllScope = fixAllScope;
            this.FixAllSpan = fixAllSpan;
            this.CodeAction = codeAction;
        }

        public FixAllState WithDocument(Document? document)
            => this.With(document: document);

        public FixAllState WithProject(Project project)
            => this.With(project: project, fixAllSpan: null);

        public FixAllState WithScope(FixAllScope scope)
            => this.With(scope: scope);

        public FixAllState With(
            Optional<Document?> document = default,
            Optional<Project> project = default,
            Optional<FixAllScope> scope = default,
            Optional<TextSpan?> fixAllSpan = default)
        {
            var newDocument = document.HasValue ? document.Value : this.Document;
            var newProject = project.HasValue ? project.Value : this.Project;
            var newFixAllScope = scope.HasValue ? scope.Value : this.FixAllScope;
            var newFixAllSpan = fixAllSpan.HasValue ? fixAllSpan.Value : this.FixAllSpan;

            if (newDocument == this.Document &&
                newProject == this.Project &&
                newFixAllScope == this.FixAllScope &&
                newFixAllSpan == this.FixAllSpan)
            {
                return this;
            }

            return new FixAllState(
                this.FixAllProvider,
                newDocument,
                newProject,
                this.CodeRefactoringProvider,
                newFixAllScope,
                newFixAllSpan,
                this.CodeAction);
        }
    }
}
