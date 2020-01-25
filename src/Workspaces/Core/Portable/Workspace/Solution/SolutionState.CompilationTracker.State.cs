﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
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
                public static readonly State Empty = new State(compilation: null, declarationOnlyCompilation: null);

                /// <summary>
                /// A strong reference to the declaration-only compilation. This compilation isn't used to produce symbols,
                /// nor does it have any references. It just holds the declaration table alive.
                /// </summary>
                public Compilation? DeclarationOnlyCompilation { get; }

                /// <summary>
                /// The best compilation that is available. May be an in-progress, full declaration, a final compilation, or <see langword="null"/>.
                /// </summary>
                public ValueSource<Optional<Compilation>>? Compilation { get; }

                /// <summary>
                /// Specifies whether <see cref="FinalCompilation"/> and all compilations it depends on contain full information or not. This can return
                /// <see langword="null"/> if the state isn't at the point where it would know, and it's necessary to transition to <see cref="FinalState"/> to figure that out.
                /// </summary>
                public virtual bool? HasSuccessfullyLoaded => null;

                /// <summary>
                /// The final compilation if available, otherwise <see langword="null"/>.
                /// </summary>
                public virtual ValueSource<Optional<Compilation>>? FinalCompilation => null;

                protected State(ValueSource<Optional<Compilation>>? compilation, Compilation? declarationOnlyCompilation)
                {
                    // Declaration-only compilations should never have any references
                    Contract.ThrowIfTrue(declarationOnlyCompilation != null && declarationOnlyCompilation.ExternalReferences.Any());

                    Compilation = compilation;
                    DeclarationOnlyCompilation = declarationOnlyCompilation;
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

                public static ValueSource<Optional<Compilation>> CreateValueSource(
                    Compilation compilation,
                    SolutionServices services)
                {
                    return services.SupportsCachingRecoverableObjects
                        ? new WeakValueSource<Compilation>(compilation)
                        : (ValueSource<Optional<Compilation>>)new ConstantValueSource<Optional<Compilation>>(compilation);
                }
            }

            /// <summary>
            /// A state where we are holding onto a previously built compilation, and have a known set of transformations
            /// that could get us to a more final state.
            /// </summary>
            private sealed class InProgressState : State
            {
                public ImmutableArray<(ProjectState state, CompilationTranslationAction action)> IntermediateProjects { get; }

                public InProgressState(
                    Compilation inProgressCompilation,
                    ImmutableArray<(ProjectState state, CompilationTranslationAction action)> intermediateProjects)
                    : base(compilation: new ConstantValueSource<Optional<Compilation>>(inProgressCompilation), declarationOnlyCompilation: null)
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
                    : base(compilation: null, declarationOnlyCompilation: declarationOnlyCompilation)
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
                    : base(new WeakValueSource<Compilation>(declarationCompilation), declarationCompilation.Clone().RemoveAllReferences())
                {
                }
            }

            /// <summary>
            /// The final state a compilation tracker reaches. The <see cref="State.DeclarationOnlyCompilation"/> is available,
            /// as well as the real <see cref="State.FinalCompilation"/>.
            /// </summary>
            private sealed class FinalState : State
            {
                private readonly bool _hasSuccessfullyLoaded;

                public override bool? HasSuccessfullyLoaded => _hasSuccessfullyLoaded;
                public override ValueSource<Optional<Compilation>>? FinalCompilation => Compilation;

                public FinalState(ValueSource<Optional<Compilation>> finalCompilationSource, Compilation finalCompilation, bool hasSuccessfullyLoaded)
                    : base(finalCompilationSource, finalCompilation.Clone().RemoveAllReferences())
                {
                    _hasSuccessfullyLoaded = hasSuccessfullyLoaded;
                }
            }
        }
    }
}
