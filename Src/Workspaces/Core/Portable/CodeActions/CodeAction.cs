// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// An action produced by a <see cref="CodeFixProvider"/> or a <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public abstract partial class CodeAction
    {
        /// <summary>
        /// A short title describing the action that may appear in a menu.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// An optional Id for the code action.
        /// If non-null, then this Id must be unique amongst all the code actions produced by the corresponding <see cref="CodeFixProvider"/> or <see cref="CodeRefactoringProvider"/>.
        /// </summary>
        public virtual string Id { get { return null; } }

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        public async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            return await GetOperationsCoreAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        internal virtual async Task<IEnumerable<CodeActionOperation>> GetOperationsCoreAsync(CancellationToken cancellationToken)
        {
            var operations = await this.ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                operations = await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return operations;
        }

        /// <summary>
        /// The sequence of operations used to construct a preview. 
        /// </summary>
        public async Task<IEnumerable<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken)
        {
            var operations = await this.ComputePreviewOperationsAsync(cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                operations = await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return operations;
        }

        /// <summary>
        /// Override this method if you want to implement a <see cref="CodeAction"/> subclass that includes custom <see cref="CodeActionOperation"/>'s.
        /// </summary>
        protected virtual async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            var changedSolution = await GetChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
            return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
        }

        /// <summary>
        /// Override this method if you want to implement a <see cref="CodeAction"/> that has a set of preview operations that are different
        /// than the operations produced by <see cref="ComputeOperationsAsync(CancellationToken)"/>.
        /// </summary>
        protected virtual async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return await ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Computes all changes for an entire solution.
        /// Override this method if you want to implement a <see cref="CodeAction"/> subclass that changes more than one document.
        /// </summary>
        protected async virtual Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            var changedDocument = await GetChangedDocumentAsync(cancellationToken).ConfigureAwait(false);
            return changedDocument.Project.Solution;
        }

        /// <summary>
        /// Computes changes for a single document.
        /// Override this method if you want to implement a <see cref="CodeAction"/> subclass that changes a single document.
        /// </summary>
        protected virtual Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// used by batch fixer engine to get new solution
        /// </summary>
        internal async Task<Solution> GetChangedSolutionInternalAsync(CancellationToken cancellationToken)
        {
            var solution = await GetChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
            return await this.PostProcessChangesAsync(solution, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<Document> GetChangedDocumentInternalAsync(CancellationToken cancellation)
        {
            return await this.GetChangedDocumentAsync(cancellation).ConfigureAwait(false);
        }

        /// <summary>
        /// Apply post processing steps to any <see cref="ApplyChangesOperation"/>'s.
        /// </summary>
        /// <param name="operations">A list of operations.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A new list of operations with post processing steps applied to any <see cref="ApplyChangesOperation"/>'s.</returns>
        protected async Task<IEnumerable<CodeActionOperation>> PostProcessAsync(IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken)
        {
            var list = new List<CodeActionOperation>();

            foreach (var op in operations)
            {
                var ac = op as ApplyChangesOperation;
                if (ac != null)
                {
                    list.Add(new ApplyChangesOperation(await this.PostProcessChangesAsync(ac.ChangedSolution, cancellationToken).ConfigureAwait(false)));
                }
                else
                {
                    list.Add(op);
                }
            }

            return list;
        }

        /// <summary>
        ///  Apply post processing steps to solution changes, like formatting and simplification.
        /// </summary>
        /// <param name="changedSolution">The solution changed by the <see cref="CodeAction"/>.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        protected async Task<Solution> PostProcessChangesAsync(Solution changedSolution, CancellationToken cancellationToken)
        {
            var solutionChanges = changedSolution.GetChanges(changedSolution.Workspace.CurrentSolution);

            var processedSolution = changedSolution;

            // process changed projects
            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                var documentsToProcess = projectChanges.GetChangedDocuments().Concat(
                    projectChanges.GetAddedDocuments());

                foreach (var documentId in documentsToProcess)
                {
                    var document = processedSolution.GetDocument(documentId);
                    var processedDocument = await PostProcessChangesAsync(document, cancellationToken).ConfigureAwait(false);
                    processedSolution = processedDocument.Project.Solution;
                }
            }

            // process completely new projects too
            foreach (var addedProject in solutionChanges.GetAddedProjects())
            {
                var documentsToProcess = addedProject.DocumentIds;

                foreach (var documentId in documentsToProcess)
                {
                    var document = processedSolution.GetDocument(documentId);
                    var processedDocument = await PostProcessChangesAsync(document, cancellationToken).ConfigureAwait(false);
                    processedSolution = processedDocument.Project.Solution;
                }
            }

            return processedSolution;
        }

        /// <summary>
        /// Apply post processing steps to a single document:
        ///   Reducing nodes annotated with <see cref="Simplifier.Annotation"/>
        ///   Formatting nodes annotated with <see cref="Formatter.Annotation"/>
        /// </summary>
        /// <param name="document">The document changed by the <see cref="CodeAction"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A document with the post processing changes applied.</returns>
        protected virtual async Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
        {
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            // format any node with explicit formatter annotation
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            // format any elastic whitespace
            document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            document = await CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken).ConfigureAwait(false);
            return document;
        }

        #region Factories for standard code actions

        /// <summary>
        /// Creates a code action for a change to a single document. 
        /// Use this factory when the change is expensive to compute, and should be deferred until requested.
        /// </summary>
        public static CodeAction Create(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedDocument == null)
            {
                throw new ArgumentNullException(nameof(createChangedDocument));
            }

            return new DocumentChangeAction(title, createChangedDocument, id);
        }

        /// <summary>
        /// Creates a code action for a change to more than one document within a solution.
        /// Use this factory when the change is expensive to compute, and should be deferred until requested.
        /// </summary>
        public static CodeAction Create(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string id = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedSolution == null)
            {
                throw new ArgumentNullException(nameof(createChangedSolution));
            }

            return new SolutionChangeAction(title, createChangedSolution, id);
        }

        internal class DocumentChangeAction : CodeAction
        {
            private readonly string title;
            private readonly Func<CancellationToken, Task<Document>> createChangedDocument;
            private readonly string id;

            public DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id = null)
            {
                this.title = title;
                this.createChangedDocument = createChangedDocument;
                this.id = id;
            }

            public override string Title
            {
                get { return this.title; }
            }

            public override string Id
            {
                get { return this.id; }
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.createChangedDocument(cancellationToken);
            }
        }

        internal class SolutionChangeAction : CodeAction
        {
            private readonly string title;
            private readonly Func<CancellationToken, Task<Solution>> createChangedSolution;
            private readonly string id;

            public SolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string id = null)
            {
                this.title = title;
                this.createChangedSolution = createChangedSolution;
                this.id = id;
            }

            public override string Title
            {
                get { return this.title; }
            }

            public override string Id
            {
                get { return this.id; }
            }

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return this.createChangedSolution(cancellationToken);
            }
        }

        #endregion
    }
}