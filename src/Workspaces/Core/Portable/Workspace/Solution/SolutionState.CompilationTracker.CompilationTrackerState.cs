// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private partial class CompilationTracker
        {
            private readonly struct CompilationTrackerGeneratorInfo
            {
                /// <summary>
                /// The best generated documents we have for the current state. <see cref="DocumentsAreFinal"/>
                /// specifies whether the documents are to be considered final and can be reused, or whether they're from
                /// a prior snapshot which needs to be recomputed.
                /// </summary>
                public readonly TextDocumentStates<SourceGeneratedDocumentState> Documents;

                /// <summary>
                /// The <see cref="GeneratorDriver"/> that was used for the last run, to allow for incremental reuse. May
                /// be null if we don't have generators in the first place, haven't ran generators yet for this project,
                /// or had to get rid of our driver for some reason.
                /// </summary>
                public readonly GeneratorDriver? Driver;

                /// <summary>
                /// Whether the generated documents in <see cref="Documents"/> are final and should not be regenerated. 
                /// It's important that once we've ran generators once we don't want to run them again. Once we've ran
                /// them the first time, those syntax trees are visible from other parts of the Workspaces model; if we
                /// run them a second time we'd end up with new trees which would confuse our snapshot model -- once the
                /// tree has been handed out we can't make a second tree later.
                /// </summary>
                public readonly bool DocumentsAreFinal;

                /// <summary>
                /// Whether the generated documents are frozen and generators should never be ran again, ever, even if a document
                /// is later changed. This is used to ensure that when we produce a frozen solution for partial semantics,
                /// further downstream forking of that solution won't rerun generators. This is because of two reasons:
                /// <list type="number">
                /// <item>Generally once we've produced a frozen solution with partial semantics, we now want speed rather
                /// than accuracy; a generator running in a later path will still cause issues there.</item>
                /// <item>The frozen solution with partial semantics makes no guarantee that other syntax trees exist or
                /// whether we even have references -- it's pretty likely that running a generator might produce worse results
                /// than what we originally had.</item>
                /// </list>
                /// </summary>
                public readonly bool DocumentsAreFinalAndFrozen;

                public CompilationTrackerGeneratorInfo(
                    TextDocumentStates<SourceGeneratedDocumentState> documents,
                    GeneratorDriver? driver,
                    bool documentsAreFinal,
                    bool documentsAreFinalAndFrozen = false)
                {
                    Documents = documents;
                    Driver = driver;
                    DocumentsAreFinal = documentsAreFinal;
                    DocumentsAreFinalAndFrozen = documentsAreFinalAndFrozen;

                    // If we're frozen, that implies final as well
                    Contract.ThrowIfTrue(documentsAreFinalAndFrozen && !documentsAreFinal);
                }

                public CompilationTrackerGeneratorInfo WithDocumentsAreFinal(bool documentsAreFinal)
                {
                    // If we're already frozen, then we won't do anything even if somebody calls WithDocumentsAreFinal(false);
                    // this for example would happen if we had a frozen snapshot, and then we fork it further with additional changes.
                    // In that case we would be calling WithDocumentsAreFinal(false) to force generators to run again, but if we've
                    // frozen in partial semantics, we're done running them period. So we'll just keep treating them as final,
                    // no matter the wishes of the caller.
                    if (DocumentsAreFinalAndFrozen || DocumentsAreFinal == documentsAreFinal)
                        return this;
                    else
                        return new(Documents, Driver, documentsAreFinal);
                }

                public CompilationTrackerGeneratorInfo WithDocumentsAreFinalAndFrozen()
                {
                    return DocumentsAreFinalAndFrozen ? this : new(Documents, Driver, documentsAreFinal: true, documentsAreFinalAndFrozen: true);
                }

                public CompilationTrackerGeneratorInfo WithDriver(GeneratorDriver? driver)
                    => Driver == driver ? this : new(Documents, driver, DocumentsAreFinal, DocumentsAreFinalAndFrozen);
            }

            /// <summary>
            /// The base type of all <see cref="CompilationTracker"/> states. The state of a <see cref="CompilationTracker" />
            /// starts at <see cref="Empty"/>, and then will progress through the other states until it finally reaches
            /// <see cref="FinalState" />.
            /// </summary>
            private abstract class CompilationTrackerState
            {
                /// <summary>
                /// The base <see cref="CompilationTrackerState"/> that starts with everything empty.
                /// </summary>
                public static readonly CompilationTrackerState Empty = new NoCompilationState(
                    new CompilationTrackerGeneratorInfo(
                        documents: TextDocumentStates<SourceGeneratedDocumentState>.Empty,
                        driver: null,
                        documentsAreFinal: false));

                /// <summary>
                /// The best compilation that is available that source generators have not ran on. May be an
                /// in-progress, full declaration,  a final compilation, or <see langword="null"/>.
                /// </summary>
                public Compilation? CompilationWithoutGeneratedDocuments { get; }

                public CompilationTrackerGeneratorInfo GeneratorInfo { get; }

                /// <summary>
                /// Specifies whether <see cref="FinalCompilationWithGeneratedDocuments"/> and all compilations it depends on contain full information or not. This can return
                /// <see langword="null"/> if the state isn't at the point where it would know, and it's necessary to transition to <see cref="FinalState"/> to figure that out.
                /// </summary>
                public virtual bool? HasSuccessfullyLoaded => null;

                /// <summary>
                /// The final compilation is potentially available, otherwise <see langword="null"/>.
                /// </summary>
                public virtual Compilation? FinalCompilationWithGeneratedDocuments => null;

                protected CompilationTrackerState(
                    Compilation? compilationWithoutGeneratedDocuments,
                    CompilationTrackerGeneratorInfo generatorInfo)
                {
                    CompilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments;
                    GeneratorInfo = generatorInfo;

#if DEBUG

                    // As a sanity check, we should never see the generated trees inside of the compilation that should not
                    // have generated trees.
                    var compilation = compilationWithoutGeneratedDocuments;

                    if (compilation != null)
                    {
                        foreach (var generatedDocument in generatorInfo.Documents.States.Values)
                        {
                            Contract.ThrowIfTrue(compilation.SyntaxTrees.Contains(generatedDocument.GetSyntaxTree(CancellationToken.None)));
                        }
                    }
#endif
                }

                public static CompilationTrackerState Create(
                    Compilation compilation,
                    CompilationTrackerGeneratorInfo generatorInfo,
                    Compilation? compilationWithGeneratedDocuments,
                    ImmutableList<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> intermediateProjects)
                {
                    Contract.ThrowIfTrue(intermediateProjects is null);

                    // If we don't have any intermediate projects to process, just initialize our
                    // DeclarationState now. We'll pass false for generatedDocumentsAreFinal because this is being called
                    // if our referenced projects are changing, so we'll have to rerun to consume changes.
                    return intermediateProjects.IsEmpty
                        ? new AllSyntaxTreesParsedState(compilation, generatorInfo.WithDocumentsAreFinal(false))
                        : new InProgressState(compilation, generatorInfo, compilationWithGeneratedDocuments, intermediateProjects);
                }
            }

            /// <summary>
            /// State used when we potentially have some information (like prior generated documents)
            /// but no compilation.
            /// </summary>
            private sealed class NoCompilationState(CompilationTrackerGeneratorInfo generatorInfo) : CompilationTrackerState(compilationWithoutGeneratedDocuments: null, generatorInfo)
            {
            }

            /// <summary>
            /// A state where we are holding onto a previously built compilation, and have a known set of transformations
            /// that could get us to a more final state.
            /// </summary>
            private sealed class InProgressState : CompilationTrackerState
            {
                /// <summary>
                /// The list of changes that have happened since we last computed a compilation. The oldState corresponds to
                /// the state of the project prior to the mutation.
                /// </summary>
                public ImmutableList<(ProjectState oldState, CompilationAndGeneratorDriverTranslationAction action)> IntermediateProjects { get; }

                /// <summary>
                /// The result of taking the original completed compilation that had generated documents and updating them by
                /// apply the <see cref="CompilationAndGeneratorDriverTranslationAction" />; this is not a correct snapshot in that
                /// the generators have not been rerun, but may be reusable if the generators are later found to give the
                /// same output.
                /// </summary>
                public Compilation? CompilationWithGeneratedDocuments { get; }

                public InProgressState(
                    Compilation inProgressCompilation,
                    CompilationTrackerGeneratorInfo generatorInfo,
                    Compilation? compilationWithGeneratedDocuments,
                    ImmutableList<(ProjectState state, CompilationAndGeneratorDriverTranslationAction action)> intermediateProjects)
                    : base(compilationWithoutGeneratedDocuments: inProgressCompilation,
                           generatorInfo.WithDocumentsAreFinal(false)) // since we have a set of transformations to make, we'll always have to run generators again
                {
                    Contract.ThrowIfTrue(intermediateProjects is null);
                    Contract.ThrowIfFalse(intermediateProjects.Count > 0);

                    this.IntermediateProjects = intermediateProjects;
                    this.CompilationWithGeneratedDocuments = compilationWithGeneratedDocuments;
                }
            }

            /// <summary>
            /// A built compilation for the tracker that contains the fully built DeclarationTable,
            /// but may not have references initialized
            /// </summary>
            private sealed class AllSyntaxTreesParsedState(Compilation declarationCompilation, CompilationTrackerGeneratorInfo generatorInfo) : CompilationTrackerState(declarationCompilation, generatorInfo)
            {
            }

            /// <summary>
            /// The final state a compilation tracker reaches. The real <see cref="CompilationTrackerState.FinalCompilationWithGeneratedDocuments"/> is available. It is a
            /// requirement that any <see cref="Compilation"/> provided to any clients of the <see cref="Solution"/>
            /// (for example, through <see cref="Project.GetCompilationAsync"/> or <see
            /// cref="Project.TryGetCompilation"/> must be from a <see cref="FinalState"/>.  This is because <see
            /// cref="FinalState"/> stores extra information in it about that compilation that the <see
            /// cref="Solution"/> can be queried for (for example: <see
            /// cref="Solution.GetOriginatingProject(ISymbol)"/>.  If <see cref="Compilation"/>s from other <see
            /// cref="CompilationTrackerState"/>s are passed out, then these other APIs will not function correctly.
            /// </summary>
            private sealed class FinalState : CompilationTrackerState
            {
                public override bool? HasSuccessfullyLoaded { get; }

                /// <summary>
                /// Weak set of the assembly, module and dynamic symbols that this compilation tracker has created.
                /// This can be used to determine which project an assembly symbol came from after the fact.  This is
                /// needed as the compilation an assembly came from can be GC'ed and further requests to get that
                /// compilation (or any of it's assemblies) may produce new assembly symbols.
                /// </summary>
                public readonly UnrootedSymbolSet UnrootedSymbolSet;

                /// <summary>
                /// The final compilation, with all references and source generators run. This is distinct from <see
                /// cref="Compilation"/>, which in the <see cref="FinalState"/> case will be the compilation before any
                /// source generators were ran. This ensures that a later invocation of the source generators consumes
                /// <see cref="Compilation"/> which will avoid generators being ran a second time on a compilation that
                /// already contains the output of other generators. If source generators are not active, this is equal
                /// to <see cref="Compilation"/>.
                /// </summary>
                public override Compilation FinalCompilationWithGeneratedDocuments { get; }

                private FinalState(
                    Compilation finalCompilation,
                    Compilation compilationWithoutGeneratedFiles,
                    bool hasSuccessfullyLoaded,
                    CompilationTrackerGeneratorInfo generatorInfo,
                    UnrootedSymbolSet unrootedSymbolSet)
                    : base(compilationWithoutGeneratedFiles,
                           generatorInfo.WithDocumentsAreFinal(true)) // when we're in a final state, we've ran generators and should not run again
                {
                    Contract.ThrowIfNull(finalCompilation);
                    HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                    FinalCompilationWithGeneratedDocuments = finalCompilation;
                    UnrootedSymbolSet = unrootedSymbolSet;

                    if (this.GeneratorInfo.Documents.IsEmpty)
                    {
                        // If we have no generated files, the pre-generator compilation and post-generator compilation
                        // should be the exact same instance; that way we're not creating more compilations than
                        // necessary that would be unable to share source symbols.
                        Debug.Assert(object.ReferenceEquals(finalCompilation, compilationWithoutGeneratedFiles));
                    }
                }

                /// <param name="finalCompilation">Not held onto</param>
                /// <param name="projectId">Not held onto</param>
                /// <param name="metadataReferenceToProjectId">Not held onto</param>
                public static FinalState Create(
                    Compilation finalCompilationSource,
                    Compilation compilationWithoutGeneratedFiles,
                    bool hasSuccessfullyLoaded,
                    CompilationTrackerGeneratorInfo generatorInfo,
                    Compilation finalCompilation,
                    ProjectId projectId,
                    Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId)
                {
                    // Keep track of information about symbols from this Compilation.  This will help support other APIs
                    // the solution exposes that allows the user to map back from symbols to project information.

                    var unrootedSymbolSet = UnrootedSymbolSet.Create(finalCompilation);
                    RecordAssemblySymbols(projectId, finalCompilation, metadataReferenceToProjectId);

                    return new FinalState(
                        finalCompilationSource,
                        compilationWithoutGeneratedFiles,
                        hasSuccessfullyLoaded,
                        generatorInfo,
                        unrootedSymbolSet);
                }

                private static void RecordAssemblySymbols(ProjectId projectId, Compilation compilation, Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId)
                {
                    RecordSourceOfAssemblySymbol(compilation.Assembly, projectId);

                    if (metadataReferenceToProjectId != null)
                    {
                        foreach (var (metadataReference, currentID) in metadataReferenceToProjectId)
                        {
                            var symbol = compilation.GetAssemblyOrModuleSymbol(metadataReference);
                            RecordSourceOfAssemblySymbol(symbol, currentID);
                        }
                    }
                }

                private static void RecordSourceOfAssemblySymbol(ISymbol? assemblyOrModuleSymbol, ProjectId projectId)
                {
                    // TODO: how would we ever get a null here?
                    if (assemblyOrModuleSymbol == null)
                    {
                        return;
                    }

                    Contract.ThrowIfNull(projectId);
                    // remember which project is associated with this assembly
                    if (!s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblyOrModuleSymbol, out var tmp))
                    {
                        // use GetValue to avoid race condition exceptions from Add.
                        // the first one to set the value wins.
                        s_assemblyOrModuleSymbolToProjectMap.GetValue(assemblyOrModuleSymbol, _ => projectId);
                    }
                    else
                    {
                        // sanity check: this should always be true, no matter how many times
                        // we attempt to record the association.
                        Debug.Assert(tmp == projectId);
                    }
                }
            }
        }
    }
}
