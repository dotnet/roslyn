// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private interface ICompilationTracker
        {
            ProjectState ProjectState { get; }

            CachedSkeletonReferences CachedSkeletonReferences { get; }

            /// <summary>
            /// Returns <see langword="true"/> if this <see cref="Project"/>/<see cref="Compilation"/> could produce the
            /// given <paramref name="symbol"/>.  The symbol must be a <see cref="IAssemblySymbol"/>, <see
            /// cref="IModuleSymbol"/> or <see cref="IDynamicTypeSymbol"/>.
            /// </summary>
            /// <remarks>
            /// If <paramref name="primary"/> is true, then <see cref="Compilation.References"/> will not be considered
            /// when answering this question.  In other words, if <paramref name="symbol"/>  is an <see
            /// cref="IAssemblySymbol"/> and <paramref name="primary"/> is <see langword="true"/> then this will only
            /// return true if the symbol is <see cref="Compilation.Assembly"/>.  If <paramref name="primary"/> is
            /// false, then it can return true if <paramref name="symbol"/> is <see cref="Compilation.Assembly"/> or any
            /// of the symbols returned by <see cref="Compilation.GetAssemblyOrModuleSymbol(MetadataReference)"/> for
            /// any of the references of the <see cref="Compilation.References"/>.
            /// </remarks>
            bool ContainsAssemblyOrModuleOrDynamic(ISymbol symbol, bool primary);
            ICompilationTracker Fork(SolutionServices solutionServices, ProjectState newProject, CompilationAndGeneratorDriverTranslationAction? translate = null, CancellationToken cancellationToken = default);
            ICompilationTracker FreezePartialStateWithTree(SolutionState solution, DocumentState docState, SyntaxTree tree, CancellationToken cancellationToken);
            Task<Compilation> GetCompilationAsync(SolutionState solution, CancellationToken cancellationToken);
            Task<VersionStamp> GetDependentSemanticVersionAsync(SolutionState solution, CancellationToken cancellationToken);
            Task<VersionStamp> GetDependentVersionAsync(SolutionState solution, CancellationToken cancellationToken);

            /// <summary>
            /// Get a metadata reference to this compilation info's compilation with respect to
            /// another project. For cross language references produce a skeletal assembly. If the
            /// compilation is not available, it is built. If a skeletal assembly reference is
            /// needed and does not exist, it is also built.
            /// </summary>
            Task<MetadataReference> GetMetadataReferenceAsync(SolutionState solution, ProjectState fromProject, ProjectReference projectReference, CancellationToken cancellationToken);
            CompilationReference? GetPartialMetadataReference(ProjectState fromProject, ProjectReference projectReference);
            ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(SolutionState solution, CancellationToken cancellationToken);
            Task<bool> HasSuccessfullyLoadedAsync(SolutionState solution, CancellationToken cancellationToken);
            bool TryGetCompilation([NotNullWhen(true)] out Compilation? compilation);
            SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(DocumentId documentId);
        }
    }
}
