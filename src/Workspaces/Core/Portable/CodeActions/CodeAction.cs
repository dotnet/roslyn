// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// An action produced by a <see cref="CodeFixProvider"/> or a <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public abstract class CodeAction
    {
        /// <summary>
        /// A short title describing the action that may appear in a menu.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Two code actions are treated as equivalent if they have equal non-null <see cref="EquivalenceKey"/> values and were generated
        /// by the same <see cref="CodeFixProvider"/> or <see cref="CodeRefactoringProvider"/>.
        /// </summary>
        /// <remarks>
        /// Equivalence of code actions affects some Visual Studio behavior. For example, if multiple equivalent
        /// code actions result from code fixes or refactorings for a single Visual Studio light bulb instance,
        /// the light bulb UI will present only one code action from each set of equivalent code actions.
        /// Additionally, a Fix All operation will apply only code actions that are equivalent to the original code action.
        /// 
        /// If two code actions that could be treated as equivalent do not have equal <see cref="EquivalenceKey"/> values, Visual Studio behavior
        /// may be less helpful than would be optimal. If two code actions that should be treated as distinct have
        /// equal <see cref="EquivalenceKey"/> values, Visual Studio behavior may appear incorrect.
        /// </remarks>
        public virtual string EquivalenceKey { get { return null; } }

        internal virtual bool HasCodeActions => false;

        internal virtual bool IsInvokable => true;

        internal virtual ImmutableArray<CodeAction> GetCodeActions()
        {
            return ImmutableArray<CodeAction>.Empty;
        }

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        public async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            return await GetOperationsCoreAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        internal virtual async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(CancellationToken cancellationToken)
        {
            var operations = await this.ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                return await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<CodeActionOperation>.Empty;
        }

        /// <summary>
        /// The sequence of operations used to construct a preview. 
        /// </summary>
        public async Task<ImmutableArray<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken)
        {
            var operations = await this.ComputePreviewOperationsAsync(cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                return await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<CodeActionOperation>.Empty;
        }

        /// <summary>
        /// Override this method if you want to implement a <see cref="CodeAction"/> subclass that includes custom <see cref="CodeActionOperation"/>'s.
        /// </summary>
        protected virtual async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            var changedSolution = await GetChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
            if (changedSolution == null)
            {
                return null;
            }

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
            if (changedDocument == null)
            {
                return null;
            }

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
        internal async Task<Solution> GetChangedSolutionInternalAsync(bool postProcessChanges = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            var solution = await GetChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
            if (solution == null || !postProcessChanges)
            {
                return solution;
            }

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
        protected async Task<ImmutableArray<CodeActionOperation>> PostProcessAsync(IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken)
        {
            var arrayBuilder = new ArrayBuilder<CodeActionOperation>();

            foreach (var op in operations)
            {
                var ac = op as ApplyChangesOperation;
                if (ac != null)
                {
                    arrayBuilder.Add(new ApplyChangesOperation(await this.PostProcessChangesAsync(ac.ChangedSolution, cancellationToken).ConfigureAwait(false)));
                }
                else
                {
                    arrayBuilder.Add(op);
                }
            }

            return arrayBuilder.ToImmutableAndFree();
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
            if (document.SupportsSyntaxTree)
            {
                document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any node with explicit formatter annotation
                document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                // format any elastic whitespace
                document = await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                document = await CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        #region Factories for standard code actions

        /// <summary>
        /// Creates a <see cref="CodeAction"/> for a change to a single <see cref="Document"/>. 
        /// Use this factory when the change is expensive to compute and should be deferred until requested.
        /// </summary>
        /// <param name="title">Title of the <see cref="CodeAction"/>.</param>
        /// <param name="createChangedDocument">Function to create the <see cref="Document"/>.</param>
        /// <param name="equivalenceKey">Optional value used to determine the equivalence of the <see cref="CodeAction"/> with other <see cref="CodeAction"/>s. See <see cref="CodeAction.EquivalenceKey"/>.</param>
        public static CodeAction Create(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedDocument == null)
            {
                throw new ArgumentNullException(nameof(createChangedDocument));
            }

            return new DocumentChangeAction(title, createChangedDocument, equivalenceKey);
        }

        /// <summary>
        /// Creates a <see cref="CodeAction"/> for a change to more than one <see cref="Document"/> within a <see cref="Solution"/>.
        /// Use this factory when the change is expensive to compute and should be deferred until requested.
        /// </summary>
        /// <param name="title">Title of the <see cref="CodeAction"/>.</param>
        /// <param name="createChangedSolution">Function to create the <see cref="Solution"/>.</param>
        /// <param name="equivalenceKey">Optional value used to determine the equivalence of the <see cref="CodeAction"/> with other <see cref="CodeAction"/>s. See <see cref="CodeAction.EquivalenceKey"/>.</param>
        public static CodeAction Create(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedSolution == null)
            {
                throw new ArgumentNullException(nameof(createChangedSolution));
            }

            return new SolutionChangeAction(title, createChangedSolution, equivalenceKey);
        }

        internal class SimpleCodeAction : CodeAction
        {
            private readonly string _title;
            private readonly string _equivalenceKey;
            private readonly ImmutableArray<CodeAction> _nestedActions;

            public SimpleCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
            {
                _title = title;
                _nestedActions = nestedActions;
                _equivalenceKey = ComputeEquivalenceKey(nestedActions);
            }

            public SimpleCodeAction(string title, string equivalenceKey)
            {
                _title = title;
                _equivalenceKey = equivalenceKey;
                _nestedActions = ImmutableArray<CodeAction>.Empty;
            }

            public sealed override string Title => _title;
            public sealed override string EquivalenceKey => _equivalenceKey;

            internal override bool IsInvokable => false;
            internal override bool HasCodeActions => _nestedActions.Length > 0;

            internal override ImmutableArray<CodeAction> GetCodeActions()
            {
                return _nestedActions;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<Document>(null);
            }

            private static string ComputeEquivalenceKey(IEnumerable<CodeAction> nestedActions)
            {
                if (nestedActions == null)
                {
                    return null;
                }

                var equivalenceKey = StringBuilderPool.Allocate();
                try
                {
                    foreach (var action in nestedActions)
                    {
                        equivalenceKey.Append(action.EquivalenceKey ?? action.GetHashCode().ToString() + ";");
                    }

                    return equivalenceKey.Length > 0 ? equivalenceKey.ToString() : null;
                }
                finally
                {
                    StringBuilderPool.ReturnAndFree(equivalenceKey);
                }
            }
        }

        internal class DocumentChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

            public DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
                : base(title, equivalenceKey)
            {
                _createChangedDocument = createChangedDocument;
            }

            internal override bool IsInvokable => true;

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return _createChangedDocument(cancellationToken);
            }
        }

        internal class SolutionChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolution;

            public SolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
                : base(title, equivalenceKey)
            {
                _createChangedSolution = createChangedSolution;
            }

            internal override bool IsInvokable => true;

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return _createChangedSolution(cancellationToken);
            }
        }

        #endregion
    }
}
