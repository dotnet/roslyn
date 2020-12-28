// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
                public static readonly State Empty = new(
                    compilationWithoutGeneratedDocuments: null,
                    declarationOnlyCompilation: null,
                    generatedDocuments: ImmutableArray<SourceGeneratedDocumentState>.Empty,
                    generatedDocumentsAreFinal: false,
                    unrootedSymbolSet: null);

                /// <summary>
                /// A strong reference to the declaration-only compilation. This compilation isn't used to produce symbols,
                /// nor does it have any references. It just holds the declaration table alive.
                /// </summary>
                public Compilation? DeclarationOnlyCompilation { get; }

                /// <summary>
                /// The best compilation that is available that source generators have not ran on. May be an in-progress,
                /// full declaration,  a final compilation, or <see langword="null"/>.
                /// The value is an <see cref="Optional{Compilation}"/> to represent the
                /// possibility of the compilation already having been garabage collected.
                /// </summary>
                public ValueSource<Optional<Compilation>>? CompilationWithoutGeneratedDocuments { get; }

                /// <summary>
                /// The best generated documents we have for the current state. <see cref="GeneratedDocumentsAreFinal"/> specifies whether the
                /// documents are to be considered final and can be reused, or whether they're from a prior snapshot which needs to be recomputed.
                /// </summary>
                public ImmutableArray<SourceGeneratedDocumentState> GeneratedDocuments { get; }

                /// <summary>
                /// Whether the generated documents in <see cref="GeneratedDocuments"/> are final and should not be regenerated. It's important
                /// that once we've ran generators once we don't want to run them again. Once we've ran them the first time, those syntax trees
                /// are visible from other parts of the Workspaces model; if we run them a second time we'd end up with new trees which would
                /// confuse our snapshot model -- once the tree has been handed out we can't make a second tree later.
                /// </summary>
                public bool GeneratedDocumentsAreFinal { get; }

                /// <summary>
                /// Weak set of the assembly, module and dynamic symbols that this compilation tracker has created.
                /// This can be used to determine which project an assembly symbol came from after the fact.  This is
                /// needed as the compilation an assembly came from can be GC'ed and further requests to get that
                /// compilation (or any of it's assemblies) may produce new assembly symbols.
                /// </summary>
                public readonly UnrootedSymbolSet? UnrootedSymbolSet;

                /// <summary>
                /// Specifies whether <see cref="FinalCompilationWithGeneratedDocuments"/> and all compilations it depends on contain full information or not. This can return
                /// <see langword="null"/> if the state isn't at the point where it would know, and it's necessary to transition to <see cref="FinalState"/> to figure that out.
                /// </summary>
                public virtual bool? HasSuccessfullyLoaded => null;

                /// <summary>
                /// The final compilation is potentially available, otherwise <see langword="null"/>.
                /// The value is an <see cref="Optional{Compilation}"/> to represent the
                /// possibility of the compilation already having been garabage collected.
                /// </summary>
                public virtual ValueSource<Optional<Compilation>>? FinalCompilationWithGeneratedDocuments => null;

                protected State(
                    ValueSource<Optional<Compilation>>? compilationWithoutGeneratedDocuments,
                    Compilation? declarationOnlyCompilation,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    bool generatedDocumentsAreFinal,
                    UnrootedSymbolSet? unrootedSymbolSet)
                {
                    // Declaration-only compilations should never have any references
                    Contract.ThrowIfTrue(declarationOnlyCompilation != null && declarationOnlyCompilation.ExternalReferences.Any());

                    CompilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments;
                    DeclarationOnlyCompilation = declarationOnlyCompilation;
                    GeneratedDocuments = generatedDocuments;
                    GeneratedDocumentsAreFinal = generatedDocumentsAreFinal;
                    UnrootedSymbolSet = unrootedSymbolSet;
                }

                public static State Create(
                    Compilation compilation,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    ImmutableArray<ValueTuple<ProjectState, CompilationAndGeneratorDriverTranslationAction>> intermediateProjects)
                {
                    Contract.ThrowIfTrue(intermediateProjects.IsDefault);

                    // If we don't have any intermediate projects to process, just initialize our
                    // DeclarationState now. We'll pass false for generatedDocumentsAreFinal because this is being called
                    // if our referenced projects are changing, so we'll have to rerun to consume changes.
                    return intermediateProjects.Length == 0
                        ? new FullDeclarationState(compilation, generatedDocuments, generatedDocumentsAreFinal: false)
                        : (State)new InProgressState(compilation, generatedDocuments, intermediateProjects);
                }

                public static ValueSource<Optional<Compilation>> CreateValueSource(
                    Compilation compilation,
                    SolutionServices services)
                {
                    return services.SupportsCachingRecoverableObjects
                        ? new WeakValueSource<Compilation>(compilation)
                        : (ValueSource<Optional<Compilation>>)new ConstantValueSource<Optional<Compilation>>(compilation);
                }

                public static UnrootedSymbolSet GetUnrootedSymbols(Compilation compilation)
                {

                    var primaryAssembly = new WeakReference<IAssemblySymbol>(compilation.Assembly);

                    // The dynamic type is also unrooted (i.e. doesn't point back at the compilation or source
                    // assembly).  So we have to keep track of it so we can get back from it to a project in case the 
                    // underlying compilation is GC'ed.
                    var primaryDynamic = new WeakReference<ITypeSymbol?>(
                        compilation.Language == LanguageNames.CSharp ? compilation.DynamicType : null);

                    var secondarySymbols = new WeakSet<ISymbol>();
                    foreach (var reference in compilation.References)
                    {
                        var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                        if (symbol == null)
                            continue;

                        secondarySymbols.Add(symbol);
                    }

                    return new UnrootedSymbolSet(primaryAssembly, primaryDynamic, secondarySymbols);
                }
            }

            /// <summary>
            /// A state where we are holding onto a previously built compilation, and have a known set of transformations
            /// that could get us to a more final state.
            /// </summary>
            private sealed class InProgressState : State
            {
                public ImmutableArray<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> IntermediateProjects { get; }

                public InProgressState(
                    Compilation inProgressCompilation,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    ImmutableArray<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> intermediateProjects)
                    : base(compilationWithoutGeneratedDocuments: new ConstantValueSource<Optional<Compilation>>(inProgressCompilation),
                           declarationOnlyCompilation: null,
                           generatedDocuments,
                           generatedDocumentsAreFinal: false, // since we have a set of transformations to make, we'll always have to run generators again
                           GetUnrootedSymbols(inProgressCompilation))
                {
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
                public LightDeclarationState(Compilation declarationOnlyCompilation,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    bool generatedDocumentsAreFinal)
                    : base(compilationWithoutGeneratedDocuments: null,
                           declarationOnlyCompilation,
                           generatedDocuments,
                           generatedDocumentsAreFinal,
                           unrootedSymbolSet: null)
                {
                }
            }

            /// <summary>
            /// A built compilation for the tracker that contains the fully built DeclarationTable,
            /// but may not have references initialized
            /// </summary>
            private sealed class FullDeclarationState : State
            {
                public FullDeclarationState(Compilation declarationCompilation,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    bool generatedDocumentsAreFinal)
                    : base(new WeakValueSource<Compilation>(declarationCompilation),
                           declarationCompilation.Clone().RemoveAllReferences(),
                           generatedDocuments,
                           generatedDocumentsAreFinal,
                           GetUnrootedSymbols(declarationCompilation))
                {
                }
            }

            /// <summary>
            /// The final state a compilation tracker reaches. The <see cref="State.DeclarationOnlyCompilation"/> is available,
            /// as well as the real <see cref="State.FinalCompilationWithGeneratedDocuments"/>.
            /// </summary>
            private sealed class FinalState : State
            {
                public override bool? HasSuccessfullyLoaded { get; }

                /// <summary>
                /// The final compilation, with all references and source generators run. This is distinct from
                /// <see cref="Compilation"/>, which in the <see cref="FinalState"/> case will be the compilation
                /// before any source generators were ran. This ensures that a later invocation of the source generators
                /// consumes <see cref="Compilation"/> which will avoid generators being ran a second time on a compilation that
                /// already contains the output of other generators. If source generators are not active, this is equal to <see cref="Compilation"/>.
                /// </summary>
                public override ValueSource<Optional<Compilation>> FinalCompilationWithGeneratedDocuments { get; }

                public FinalState(
                    ValueSource<Optional<Compilation>> finalCompilationSource,
                    ValueSource<Optional<Compilation>> compilationWithoutGeneratedFilesSource,
                    Compilation compilationWithoutGeneratedFiles,
                    bool hasSuccessfullyLoaded,
                    ImmutableArray<SourceGeneratedDocumentState> generatedDocuments,
                    UnrootedSymbolSet? unrootedSymbolSet)
                    : base(compilationWithoutGeneratedFilesSource,
                           compilationWithoutGeneratedFiles.Clone().RemoveAllReferences(),
                           generatedDocuments,
                           generatedDocumentsAreFinal: true, // when we're in a final state, we've ran generators and should not run again
                           unrootedSymbolSet)
                {
                    HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                    FinalCompilationWithGeneratedDocuments = finalCompilationSource;

                    if (GeneratedDocuments.IsEmpty)
                    {
                        // In this case, the finalCompilationSource and compilationWithoutGeneratedFilesSource should point to the
                        // same Compilation, which should be compilationWithoutGeneratedFiles itself
                        Debug.Assert(finalCompilationSource.TryGetValue(out var finalCompilation));
                        Debug.Assert(object.ReferenceEquals(finalCompilation.Value, compilationWithoutGeneratedFiles));
                    }
                }
            }
        }
    }
}
