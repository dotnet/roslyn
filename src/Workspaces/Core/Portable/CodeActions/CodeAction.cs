// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Tags;
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

        internal virtual string Message => Title;

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
        public virtual string EquivalenceKey => null;

        internal virtual bool IsInlinable => false;

        internal virtual CodeActionPriority Priority => CodeActionPriority.Medium;

        /// <summary>
        /// Descriptive tags from <see cref="WellKnownTags"/>.
        /// These tags may influence how the item is displayed.
        /// </summary>
        public virtual ImmutableArray<string> Tags => ImmutableArray<string>.Empty;

        internal virtual ImmutableArray<CodeAction> NestedCodeActions
            => ImmutableArray<CodeAction>.Empty;

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        public Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            return GetOperationsAsync(new ProgressTracker(), cancellationToken);
        }

        internal Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return GetOperationsCoreAsync(progressTracker, cancellationToken);
        }

        /// <summary>
        /// The sequence of operations that define the code action.
        /// </summary>
        internal virtual async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var operations = await this.ComputeOperationsAsync(progressTracker, cancellationToken).ConfigureAwait(false);

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

        internal virtual async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var operations = await ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);
            return operations.ToImmutableArrayOrEmpty();
        }

        /// <summary>
        /// Override this method if you want to implement a <see cref="CodeAction"/> that has a set of preview operations that are different
        /// than the operations produced by <see cref="ComputeOperationsAsync(CancellationToken)"/>.
        /// </summary>
        protected virtual Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return ComputeOperationsAsync(cancellationToken);
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

        internal virtual Task<Solution> GetChangedSolutionAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return GetChangedSolutionAsync(cancellationToken);
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
        internal async Task<Solution> GetChangedSolutionInternalAsync(bool postProcessChanges = true, CancellationToken cancellationToken = default)
        {
            var solution = await GetChangedSolutionAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);
            if (solution == null || !postProcessChanges)
            {
                return solution;
            }

            return await this.PostProcessChangesAsync(solution, cancellationToken).ConfigureAwait(false);
        }

        internal Task<Document> GetChangedDocumentInternalAsync(CancellationToken cancellation)
        {
            return GetChangedDocumentAsync(cancellation);
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
                if (op is ApplyChangesOperation ac)
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
                var documentsToProcess = projectChanges.GetChangedDocuments(true).Concat(
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
        protected virtual Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
            => CleanupDocumentAsync(document, cancellationToken);

        internal static async Task<Document> CleanupDocumentAsync(
            Document document, CancellationToken cancellationToken)
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

        internal virtual bool PerformFinalApplicabilityCheck => false;

        /// <summary>
        /// Called by the CodeActions on the UI thread to determine if the CodeAction is still
        /// applicable and should be presented to the user.  CodeActions can override this if they
        /// need to do any final checking that must be performed on the UI thread (for example
        /// accessing and querying the Visual Studio DTE).
        /// </summary>
        internal virtual bool IsApplicable(Workspace workspace)
        {
            return true;
        }

        #region Factories for standard code actions

        /// <summary>
        /// Creates a <see cref="CodeAction"/> for a change to a single <see cref="Document"/>.
        /// Use this factory when the change is expensive to compute and should be deferred until requested.
        /// </summary>
        /// <param name="title">Title of the <see cref="CodeAction"/>.</param>
        /// <param name="createChangedDocument">Function to create the <see cref="Document"/>.</param>
        /// <param name="equivalenceKey">Optional value used to determine the equivalence of the <see cref="CodeAction"/> with other <see cref="CodeAction"/>s. See <see cref="CodeAction.EquivalenceKey"/>.</param>
        [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Preserving existing public API")]
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
        [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Preserving existing public API")]
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

        /// <summary>
        /// Creates a <see cref="CodeAction"/> representing a group of code actions.
        /// </summary>
        /// <param name="title">Title of the <see cref="CodeAction"/> group.</param>
        /// <param name="nestedActions">The code actions within the group.</param>
        /// <param name="isInlinable"><see langword="true"/> to allow inlining the members of the group into the parent;
        /// otherwise, <see langword="false"/> to require that this group appear as a group with nested actions.</param>
        public static CodeAction Create(string title, ImmutableArray<CodeAction> nestedActions, bool isInlinable)
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (nestedActions == null)
            {
                throw new ArgumentNullException(nameof(nestedActions));
            }

            return new CodeActionWithNestedActions(title, nestedActions, isInlinable);
        }

        internal abstract class SimpleCodeAction : CodeAction
        {
            public SimpleCodeAction(string title, string equivalenceKey)
            {
                Title = title;
                EquivalenceKey = equivalenceKey;
            }

            public sealed override string Title { get; }
            public sealed override string EquivalenceKey { get; }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<Document>(null);
            }
        }

        internal class CodeActionWithNestedActions : SimpleCodeAction
        {
            public CodeActionWithNestedActions(
                string title, ImmutableArray<CodeAction> nestedActions,
                bool isInlinable, CodeActionPriority priority = CodeActionPriority.Medium)
                : base(title, ComputeEquivalenceKey(nestedActions))
            {
                Debug.Assert(nestedActions.Length > 0);
                NestedCodeActions = nestedActions;
                IsInlinable = isInlinable;
                Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }

            internal sealed override bool IsInlinable { get; }

            internal sealed override ImmutableArray<CodeAction> NestedCodeActions { get; }

            private static string ComputeEquivalenceKey(ImmutableArray<CodeAction> nestedActions)
            {
                var equivalenceKey = StringBuilderPool.Allocate();
                try
                {
                    foreach (var action in nestedActions)
                    {
                        equivalenceKey.Append((action.EquivalenceKey ?? action.GetHashCode().ToString()) + ";");
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

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return _createChangedSolution(cancellationToken);
            }
        }

        #endregion
    }
}
