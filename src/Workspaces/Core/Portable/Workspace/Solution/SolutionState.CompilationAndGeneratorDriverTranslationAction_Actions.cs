// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private abstract partial class CompilationAndGeneratorDriverTranslationAction
        {
            internal sealed class TouchDocumentAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly DocumentState _oldState;
                private readonly DocumentState _newState;

                public TouchDocumentAction(DocumentState oldState, DocumentState newState)
                {
                    _oldState = oldState;
                    _newState = newState;
                }

                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return UpdateDocumentInCompilationAsync(oldCompilation, _oldState, _newState, cancellationToken);
                }

                public DocumentId DocumentId => _newState.Attributes.Id;
            }

            internal sealed class TouchAdditionalDocumentAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly TextDocumentState _oldState;
                private readonly TextDocumentState _newState;

                public TouchAdditionalDocumentAction(TextDocumentState oldState, TextDocumentState newState)
                {
                    _oldState = oldState;
                    _newState = newState;
                }

                public override TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
                {
                    // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional file has been changed
                    // to allow for incremental updates: our only option is to recreate the generator driver from scratch.
                    _ = _oldState;
                    _ = _newState;

                    return new TrackedGeneratorDriver(generatorDriver: null);
                }
            }

            internal sealed class RemoveDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<DocumentState> _documents;

                public RemoveDocumentsAction(ImmutableArray<DocumentState> documents)
                {
                    _documents = documents;
                }

                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(_documents.Length);
                    foreach (var document in _documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveSyntaxTrees(syntaxTrees);
                }
            }

            internal sealed class AddDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<DocumentState> _documents;

                public AddDocumentsAction(ImmutableArray<DocumentState> documents)
                {
                    _documents = documents;
                }

                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(capacity: _documents.Length);
                    foreach (var document in _documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.AddSyntaxTrees(syntaxTrees);
                }
            }

            internal sealed class ReplaceAllSyntaxTreesAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ProjectState _state;

                public ReplaceAllSyntaxTreesAction(ProjectState state)
                {
                    _state = state;
                }

                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(capacity: _state.DocumentIds.Count);

                    foreach (var documentState in _state.OrderedDocumentStates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await documentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);
                }
            }

            internal sealed class ProjectCompilationOptionsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly CompilationOptions _options;

                public ProjectCompilationOptionsAction(CompilationOptions options)
                {
                    _options = options;
                }

                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.WithOptions(_options));
                }
            }

            internal sealed class ProjectAssemblyNameAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly string _assemblyName;

                public ProjectAssemblyNameAction(string assemblyName)
                {
                    _assemblyName = assemblyName;
                }

                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.WithAssemblyName(_assemblyName));
                }
            }

            internal sealed class AddAnalyzerReferencesAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<AnalyzerReference> _analyzerReferences;

                public AddAnalyzerReferencesAction(ImmutableArray<AnalyzerReference> analyzerReferences)
                {
                    _analyzerReferences = analyzerReferences;
                }

                public override TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
                {
                    var generators = _analyzerReferences.SelectMany(a => a.GetGenerators()).ToImmutableArray();
                    return new TrackedGeneratorDriver(generatorDriver.GeneratorDriver?.AddGenerators(generators));
                }
            }

            internal sealed class RemoveAnalyzerReferencesAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<AnalyzerReference> _analyzerReferences;

                public RemoveAnalyzerReferencesAction(ImmutableArray<AnalyzerReference> analyzerReferences)
                {
                    _analyzerReferences = analyzerReferences;
                }

                public override TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
                {
                    var generators = _analyzerReferences.SelectMany(a => a.GetGenerators()).ToImmutableArray();
                    return new TrackedGeneratorDriver(generatorDriver.GeneratorDriver?.RemoveGenerators(generators));
                }
            }

            internal sealed class AddAdditionalDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
#pragma warning disable IDE0052 // Remove unread private members
                // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional file has been added
                private readonly ImmutableArray<TextDocumentState> _additionalDocuments;
#pragma warning restore IDE0052 // Remove unread private members

                public AddAdditionalDocumentsAction(ImmutableArray<TextDocumentState> additionalDocuments)
                {
                    _additionalDocuments = additionalDocuments;
                }

                public override TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
                {
                    // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional file has been added
                    // to allow for incremental updates: our only option is to recreate the generator driver from scratch.
                    // return generatorDriver.WithPendingEdits(_additionalDocuments.SelectAsArray(a => (PendingEdit)new AdditionalFileAddedEdit(new AdditionalTextWithState(a))));
                    return new TrackedGeneratorDriver(generatorDriver: null);
                }
            }

            internal sealed class RemoveAdditionalDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
#pragma warning disable IDE0052 // Remove unread private members
                // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional file has been added
                private readonly ImmutableArray<TextDocumentState> _additionalDocuments;
#pragma warning restore IDE0052 // Remove unread private members

                public RemoveAdditionalDocumentsAction(ImmutableArray<TextDocumentState> additionalDocuments)
                {
                    _additionalDocuments = additionalDocuments;
                }

                public override TrackedGeneratorDriver TransformGeneratorDriver(TrackedGeneratorDriver generatorDriver)
                {
                    // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional file has been removed
                    // to allow for incremental updates: our only option is to recreate the generator driver from scratch.
                    // return generatorDriver.WithPendingEdits(_additionalDocuments.SelectAsArray(a => (PendingEdit)new AdditionalFileRemovedEdit(...)));
                    return new TrackedGeneratorDriver(generatorDriver: null);
                }
            }
        }
    }
}
