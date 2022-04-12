// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.FixAll;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Context for "Fix all occurrences" for code refactorings provided by each <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public sealed class FixAllContext
    {
        internal FixAllState State { get; }

        internal FixAllProvider FixAllProvider => State.FixAllProvider;

        /// <summary>
        /// Document within which fix all occurrences was triggered.
        /// </summary>
        public Document Document => State.Document!;

        /// <summary>
        /// Original selection span with which fix all occurences was triggered.
        /// </summary>
        public TextSpan SelectionSpan => State.SelectionSpan;

        /// <summary>
        /// Underlying <see cref="CodeRefactoringProvider"/> which triggered this fix all.
        /// </summary>
        public CodeRefactoringProvider CodeRefactoringProvider => State.CodeRefactoringProvider;

        /// <summary>
        /// <see cref="FixAllScope"/> to fix all occurrences.
        /// </summary>
        public FixAllScope Scope => State.FixAllScope;

        /// <summary>
        /// The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.
        /// </summary>
        public string? CodeActionEquivalenceKey => State.CodeAction.EquivalenceKey;

        /// <summary>
        /// CancellationToken for fix all session.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal IProgressTracker ProgressTracker { get; }

        /// <summary>
        /// Project to fix all occurrences.
        /// Note that this property will always be the containing project of <see cref="Document"/>
        /// for publicly exposed FixAllContext instance. However, we might create an intermediate FixAllContext
        /// with null <see cref="Document"/> and non-null Project, so we require this internal property for intermediate computation.
        /// </summary>
        internal Project Project => State.Project;

        internal FixAllContext(
            FixAllState state,
            IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            State = state;
            this.ProgressTracker = progressTracker;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets a new <see cref="FixAllContext"/> with the given cancellationToken.
        /// </summary>
        public FixAllContext WithCancellationToken(CancellationToken cancellationToken)
        {
            if (this.CancellationToken == cancellationToken)
            {
                return this;
            }

            return new FixAllContext(State, this.ProgressTracker, cancellationToken);
        }

        /// <summary>
        /// Gets the spans to fix by document for the <see cref="Scope"/> for this fix all occurences fix.
        /// </summary>
        public Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(CancellationToken cancellationToken)
            => State.GetFixAllSpansAsync(cancellationToken);

        internal FixAllContext WithDocument(Document? document)
            => this.WithState(State.WithDocument(document));

        internal FixAllContext WithProject(Project project)
            => this.WithState(State.WithProject(project));

        internal FixAllContext WithScope(FixAllScope scope)
            => this.WithState(State.WithScope(scope));

        private FixAllContext WithState(FixAllState state)
            => this.State == state ? this : new FixAllContext(state, ProgressTracker, CancellationToken);
    }
}
