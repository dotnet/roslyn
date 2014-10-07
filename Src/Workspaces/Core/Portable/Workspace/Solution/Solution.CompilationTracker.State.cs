// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            // An empty compilation 
            // Used as a base class to hold the SkeletonReference property used by derived types.
            private class State
            {
                public static readonly State Empty = new State();

                // strong reference to declaration only compilation. 
                // this doesn't make any expensive information such as symbols or references alive. just
                // things like decleration table alive.
                public Compilation DeclarationOnlyCompilation { get; private set; }

                // The compilation available.  May be an InProgress, Full Declaration, or Final compilation
                public ValueSource<Compilation> Compilation { get; private set; }

                // The Final compilation if available, otherwise an empty IValueSource
                public virtual ValueSource<Compilation> FinalCompilation
                {
                    get { return ConstantValueSource<Compilation>.Empty; }
                }

                private State()
                    : this(ConstantValueSource<Compilation>.Empty, null)
                {
                }

                protected State(ValueSource<Compilation> compilation)
                    : this(compilation, null)
                {
                }

                protected State(Compilation declarationOnlyCompilation)
                    : this(ConstantValueSource<Compilation>.Empty, declarationOnlyCompilation)
                {
                }

                protected State(ValueSource<Compilation> compilation, Compilation declarationOnlyCompilation)
                {
                    this.Compilation = compilation;
                    this.DeclarationOnlyCompilation = declarationOnlyCompilation;
                }

                public static State Create(
                    ValueSource<Compilation> compilationSource,
                    ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> intermediateProjects)
                {
                    Contract.ThrowIfNull(compilationSource);
                    Contract.ThrowIfNull(compilationSource.GetValue());
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);

                    // If we don't have any intermediate projects to process, just initialize our
                    // DeclarationState now.
                    return intermediateProjects.Length == 0
                        ? (State)new FullDeclarationState(compilationSource)
                        : (State)new InProgressState(compilationSource, intermediateProjects);
                }
            }

            // A previously built compilation that we can incrementally build a 
            // DeclarationCompilation from by iteratively processing IntermediateProjects
            private sealed class InProgressState : State
            {
                public ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> IntermediateProjects { get; private set; }

                public InProgressState(
                    ValueSource<Compilation> inProgressCompilationSource,
                    ImmutableArray<ValueTuple<ProjectState, CompilationTranslationAction>> intermediateProjects)
                    : base(inProgressCompilationSource)
                {
                    Contract.ThrowIfNull(inProgressCompilationSource);
                    Contract.ThrowIfNull(inProgressCompilationSource.GetValue());
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);
                    Contract.ThrowIfFalse(intermediateProjects.Length > 0);

                    this.IntermediateProjects = intermediateProjects;
                }
            }

            // declaration only state that has no associated references or symbols. just declaration table only.
            private sealed class LightDeclarationState : State
            {
                public LightDeclarationState(Compilation declarationOnlyCompilation)
                    : base(declarationOnlyCompilation)
                {
                }
            }

            // A built compilation for the tracker that contains the fully built DeclarationTable,
            // but may not have references initialized
            private sealed class FullDeclarationState : State
            {
                public FullDeclarationState(ValueSource<Compilation> declarationCompilation)
                    : base(declarationCompilation, declarationCompilation.GetValue().RemoveAllReferences())
                {
                }
            }

            // The final built compilation for the tracker containing the DeclarationTable and references
            private sealed class FinalState : State
            {
                public override ValueSource<Compilation> FinalCompilation
                {
                    get { return this.Compilation; }
                }

                public FinalState(ValueSource<Compilation> finalCompilationSource)
                    : base(finalCompilationSource, finalCompilationSource.GetValue().Clone().RemoveAllReferences())
                {
                }
            }
        }
    }
}