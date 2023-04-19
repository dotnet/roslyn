// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        /// <summary>
        /// An implementation of <see cref="ICompilationTracker"/> that takes a compilation from another compilation tracker and updates it
        /// to return a generated document with a specific content, regardless of what the generator actually produces. In other words, it says
        /// "take the compilation this other thing produced, and pretend the generator gave this content, even if it wouldn't."
        /// </summary>
        private class GeneratedFileReplacingCompilationTracker : ICompilationTracker
        {
            private readonly ICompilationTracker _underlyingTracker;
            private readonly SourceGeneratedDocumentState _replacedGeneratedDocumentState;

            /// <summary>
            /// The lazily-produced compilation that has the generated document updated. This is initialized by call to
            /// <see cref="GetCompilationAsync"/>.
            /// </summary>
            [DisallowNull]
            private Compilation? _compilationWithReplacement;

            public GeneratedFileReplacingCompilationTracker(ICompilationTracker underlyingTracker, SourceGeneratedDocumentState replacementDocumentState)
            {
                _underlyingTracker = underlyingTracker;
                _replacedGeneratedDocumentState = replacementDocumentState;
            }

            public ProjectState ProjectState => _underlyingTracker.ProjectState;

            public bool ContainsAssemblyOrModuleOrDynamic(ISymbol symbol, bool primary)
            {
                if (_compilationWithReplacement == null)
                {
                    // We don't have a compilation yet, so this couldn't have came from us
                    return false;
                }
                else
                {
                    return UnrootedSymbolSet.Create(_compilationWithReplacement).ContainsAssemblyOrModuleOrDynamic(symbol, primary);
                }
            }

            public bool? ContainsSymbolsWithNameFromDeclarationOnlyCompilation(Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
            {
                // TODO: This only needs to be implemented if a feature that operates from a source generated file needs to search for declarations
                // with other names; those APIs are only used by certain code fixes which isn't a need for now. This will need to be fixed up when
                // we complete https://github.com/dotnet/roslyn/issues/49533.
                throw new NotImplementedException();
            }

            public bool? ContainsSymbolsWithNameFromDeclarationOnlyCompilation(string name, SymbolFilter filter, CancellationToken cancellationToken)
            {
                // TODO: This only needs to be implemented if a feature that operates from a source generated file needs to search for declarations
                // with other names; those APIs are only used by certain code fixes which isn't a need for now. This will need to be fixed up when
                // we complete https://github.com/dotnet/roslyn/issues/49533.
                throw new NotImplementedException();
            }

            public ICompilationTracker Fork(ProjectState newProject, CompilationAndGeneratorDriverTranslationAction? translate = null, CancellationToken cancellationToken = default)
            {
                // TODO: This only needs to be implemented if a feature that operates from a source generated file then makes
                // further mutations to that project, which isn't needed for now. This will be need to be fixed up when we complete
                // https://github.com/dotnet/roslyn/issues/49533.
                throw new NotImplementedException();
            }

            public ICompilationTracker FreezePartialStateWithTree(SolutionState solution, DocumentState docState, SyntaxTree tree, CancellationToken cancellationToken)
            {
                // Because we override SourceGeneratedDocument.WithFrozenPartialSemantics directly, we shouldn't be able to get here.
                throw ExceptionUtilities.Unreachable;
            }

            public async Task<Compilation> GetCompilationAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                // Fast path if we've definitely already done this before
                if (_compilationWithReplacement != null)
                {
                    return _compilationWithReplacement;
                }

                var underlyingCompilation = await _underlyingTracker.GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);
                var underlyingSourceGeneratedDocuments = await _underlyingTracker.GetSourceGeneratedDocumentStatesAsync(solution, cancellationToken).ConfigureAwait(false);

                underlyingSourceGeneratedDocuments.TryGetState(_replacedGeneratedDocumentState.Id, out var existingState);

                Compilation newCompilation;

                var newSyntaxTree = await _replacedGeneratedDocumentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                if (existingState != null)
                {
                    // The generated file still exists in the underlying compilation, but the contents may not match the open file if the open file
                    // is stale. Replace the syntax tree so we have a tree that matches the text.
                    var existingSyntaxTree = await existingState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    newCompilation = underlyingCompilation.ReplaceSyntaxTree(existingSyntaxTree, newSyntaxTree);
                }
                else
                {
                    // The existing output no longer exists in the underlying compilation. This could happen if the user made
                    // an edit which would cause this file to no longer exist, but they're still operating on an open representation
                    // of that file. To ensure that this snapshot is still usable, we'll just add this document back in. This is not a
                    // semantically correct operation, but working on stale snapshots never has that guarantee.
                    newCompilation = underlyingCompilation.AddSyntaxTrees(newSyntaxTree);
                }

                Interlocked.CompareExchange(ref _compilationWithReplacement, newCompilation, null);

                return _compilationWithReplacement;
            }

            public Task<VersionStamp> GetDependentSemanticVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                return _underlyingTracker.GetDependentSemanticVersionAsync(solution, cancellationToken);
            }

            public Task<VersionStamp> GetDependentVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                return _underlyingTracker.GetDependentVersionAsync(solution, cancellationToken);
            }

            public async Task<MetadataReference> GetMetadataReferenceAsync(SolutionState solution, ProjectState fromProject, ProjectReference projectReference, CancellationToken cancellationToken)
            {
                var compilation = await GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);

                // If it's the same language we can just make a CompilationReference
                if (this.ProjectState.LanguageServices == fromProject.LanguageServices)
                {
                    return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                }
                else
                {
                    var version = await GetDependentSemanticVersionAsync(solution, cancellationToken).ConfigureAwait(false);
                    return MetadataOnlyReference.GetOrBuildReference(solution, projectReference, compilation, version, cancellationToken);
                }
            }

            public CompilationReference? GetPartialMetadataReference(ProjectState fromProject, ProjectReference projectReference)
            {
                // This method is used if you're forking a solution with partial semantics, and used to quickly produce references.
                // So this method should only be called if:
                //
                // 1. Project A has a open source generated document, and this CompilationTracker represents A
                // 2. Project B references that A, and is being frozen for partial semantics.
                //
                // We generally don't use partial semantics in a different project than the open file, so this isn't a scenario we need to support.
                throw new NotImplementedException();
            }

            public async ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var underlyingGeneratedDocumentStates = await _underlyingTracker.GetSourceGeneratedDocumentStatesAsync(solution, cancellationToken).ConfigureAwait(false);

                if (underlyingGeneratedDocumentStates.Contains(_replacedGeneratedDocumentState.Id))
                {
                    // The generated file still exists in the underlying compilation, but the contents may not match the open file if the open file
                    // is stale. Replace the syntax tree so we have a tree that matches the text.
                    return underlyingGeneratedDocumentStates.SetState(_replacedGeneratedDocumentState.Id, _replacedGeneratedDocumentState);
                }
                else
                {
                    // The generated output no longer exists in the underlying compilation. This could happen if the user made
                    // an edit which would cause this file to no longer exist, but they're still operating on an open representation
                    // of that file. To ensure that this snapshot is still usable, we'll just add this document back in. This is not a
                    // semantically correct operation, but working on stale snapshots never has that guarantee.
                    return underlyingGeneratedDocumentStates.AddRange(ImmutableArray.Create(_replacedGeneratedDocumentState));
                }
            }

            public Task<bool> HasSuccessfullyLoadedAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                return _underlyingTracker.HasSuccessfullyLoadedAsync(solution, cancellationToken);
            }

            public bool TryGetCompilation([NotNullWhen(true)] out Compilation? compilation)
            {
                compilation = _compilationWithReplacement;
                return compilation != null;
            }

            public SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(DocumentId documentId)
            {
                if (documentId == _replacedGeneratedDocumentState.Id)
                {
                    return _replacedGeneratedDocumentState;
                }
                else
                {
                    return _underlyingTracker.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
                }
            }
        }
    }
}
