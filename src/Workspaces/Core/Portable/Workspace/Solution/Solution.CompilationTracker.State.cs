// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private partial class CompilationTracker
        {
            /// <summary>
            /// The base type of all <see cref="CompilationTracker"/> states. The state of a <see cref="CompilationTracker" />
            /// starts at <see cref="Empty"/>, and then will progress through the other states until it finally reaches
            /// <see cref="FinalState" />.
            /// </summary>
            private class State
            {
                /// <summary>
                /// The base <see cref="State"/> that starts with everything empty.
                /// </summary>
                public static readonly State Empty = new State(compilation: ConstantValueSource<Compilation>.Empty, declarationOnlyCompilation: null);

                /// <summary>
                /// A strong reference to the declaration-only compilation. This compilation isn't used to produce symbols,
                /// nor does it have any references. It just holds the declaration table alive.
                /// </summary>
                public Compilation DeclarationOnlyCompilation { get; }

                /// <summary>
                /// The best compilation that is available.  May be an in-progress, full declaration, or a final compilation.
                /// </summary>
                public ValueSource<Compilation> Compilation { get; }

                /// <summary>
                /// Specifies whether <see cref="FinalCompilation"/> and all compilations it depends on contain full information or not. This can return
                /// null if the state isn't at the point where it would know, and it's necessary to transition to <see cref="FinalState"/> to figure that out.
                /// </summary>
                public virtual bool? HasSuccessfullyLoadedTransitively => null;

                /// <summary>
                /// The final compilation if available, otherwise an empty <see cref="ValueSource{Compilation}"/>.
                /// </summary>
                public virtual ValueSource<Compilation> FinalCompilation
                {
                    get { return ConstantValueSource<Compilation>.Empty; }
                }

                protected State(ValueSource<Compilation> compilation, Compilation declarationOnlyCompilation)
                {
                    this.Compilation = compilation;
                    this.DeclarationOnlyCompilation = declarationOnlyCompilation;

                    // Declaration-only compilations should never have any references
                    Contract.ThrowIfTrue(declarationOnlyCompilation != null && declarationOnlyCompilation.ExternalReferences.Any());
                }

                public static State Create(
                    Compilation compilation,
                    ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> intermediateProjects)
                {
                    Contract.ThrowIfNull(compilation);
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);

                    // If we don't have any intermediate projects to process, just initialize our
                    // DeclarationState now.
                    return intermediateProjects.Length == 0
                        ? new FullDeclarationState(compilation)
                        : (State)new InProgressState(compilation, intermediateProjects);
                }

                public static ValueSource<Compilation> CreateValueSource(
                    Compilation compilation,
                    SolutionServices services)
                {
                    return services.SupportsCachingRecoverableObjects
                        ? new WeakConstantValueSource<Compilation>(compilation)
                        : (ValueSource<Compilation>)new ConstantValueSource<Compilation>(compilation);
                }
            }

            /// <summary>
            /// A state where we are holding onto a previously built compilation, and have a known set of transformations
            /// that could get us to a more final state.
            /// </summary>
            private sealed class InProgressState : State
            {
                public ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> IntermediateProjects { get; }

                public InProgressState(
                    Compilation inProgressCompilation,
                    ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> intermediateProjects)
                    : base(compilation: new ConstantValueSource<Compilation>(inProgressCompilation), declarationOnlyCompilation: null)
                {
                    Contract.ThrowIfNull(inProgressCompilation);
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);
                    Contract.ThrowIfFalse(intermediateProjects.Length > 0);

                    this.IntermediateProjects = intermediateProjects;
                }
            }

            /// <summary>
            /// Declaration-only state that has no associated references or symbols. just declaration table only.
            /// </summary>
            private sealed class LightDeclarationState : State
            {
                public LightDeclarationState(Compilation declarationOnlyCompilation)
                    : base(compilation: ConstantValueSource<Compilation>.Empty, declarationOnlyCompilation: declarationOnlyCompilation)
                {
                }
            }

            /// <summary>
            /// A built compilation for the tracker that contains the fully built DeclarationTable,
            /// but may not have references initialized
            /// </summary>
            private sealed class FullDeclarationState : State
            {
                public FullDeclarationState(Compilation declarationCompilation)
                    : base(new WeakConstantValueSource<Compilation>(declarationCompilation), declarationCompilation.Clone().RemoveAllReferences())
                {
                }
            }

            /// <summary>
            /// The final state a compilation tracker reaches. The <see cref="State.DeclarationOnlyCompilation"/> is available,
            /// as well as the real <see cref="State.FinalCompilation"/>.
            /// </summary>
            private sealed class FinalState : State
            {
                private readonly bool _hasSuccessfullyLoadedTransitively;

                public override bool? HasSuccessfullyLoadedTransitively => _hasSuccessfullyLoadedTransitively;
                public override ValueSource<Compilation> FinalCompilation => this.Compilation;

                public FinalState(ValueSource<Compilation> finalCompilationSource, bool hasSuccessfullyLoadedTransitively)
                    : base(finalCompilationSource, finalCompilationSource.GetValue().Clone().RemoveAllReferences())
                {
                    _hasSuccessfullyLoadedTransitively = hasSuccessfullyLoadedTransitively;
                }
            }
        }
    }
}
