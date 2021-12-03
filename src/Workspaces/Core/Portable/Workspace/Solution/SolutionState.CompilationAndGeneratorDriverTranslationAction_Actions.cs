// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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

                // Replacing a single tree doesn't impact the generated trees in a compilation, so we can use this against
                // compilations that have generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override CompilationAndGeneratorDriverTranslationAction? TryMergeWithPrior(CompilationAndGeneratorDriverTranslationAction priorAction)
                {
                    if (priorAction is TouchDocumentAction priorTouchAction &&
                        priorTouchAction._newState == this._oldState)
                    {
                        return new TouchDocumentAction(priorTouchAction._oldState, this._newState);
                    }

                    return null;
                }
            }

            internal sealed class TouchAdditionalDocumentAction : CompilationAndGeneratorDriverTranslationAction
            {
#pragma warning disable IDE0052 // Remove unread private members
                // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an additional document changed
                private readonly TextDocumentState _oldState;
                private readonly TextDocumentState _newState;
#pragma warning restore IDE0052 // Remove unread private members

                public TouchAdditionalDocumentAction(TextDocumentState oldState, TextDocumentState newState)
                {
                    _oldState = oldState;
                    _newState = newState;
                }

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override CompilationAndGeneratorDriverTranslationAction? TryMergeWithPrior(CompilationAndGeneratorDriverTranslationAction priorAction)
                {
                    if (priorAction is TouchAdditionalDocumentAction priorTouchAction &&
                        priorTouchAction._newState == this._oldState)
                    {
                        return new TouchAdditionalDocumentAction(priorTouchAction._oldState, this._newState);
                    }

                    return null;
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

                // This action removes the specified trees, but leaves the generated trees untouched.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
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

                // This action adds the specified trees, but leaves the generated trees untouched.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
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
                    var syntaxTrees = new List<SyntaxTree>(capacity: _state.DocumentStates.Count);

                    foreach (var documentState in _state.DocumentStates.GetStatesInCompilationOrder())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await documentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);
                }

                // Because this removes all trees, it'd also remove the generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => false;
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

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
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

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class AddAnalyzerReferencesAction : CompilationAndGeneratorDriverTranslationAction
            {
#pragma warning disable IDE0052 // Remove unread private members
                // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an analyzer reference has been added
                private readonly ImmutableArray<AnalyzerReference> _analyzerReferences;
                private readonly string _language;
#pragma warning restore IDE0052 // Remove unread private members

                public AddAnalyzerReferencesAction(ImmutableArray<AnalyzerReference> analyzerReferences, string language)
                {
                    _analyzerReferences = analyzerReferences;
                    _language = language;
                }

                // Changing analyzer references doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class RemoveAnalyzerReferencesAction : CompilationAndGeneratorDriverTranslationAction
            {
#pragma warning disable IDE0052 // Remove unread private members
                // https://github.com/dotnet/roslyn/issues/44161: right now there is no way to tell a GeneratorDriver that an analyzer reference has been removed
                private readonly ImmutableArray<AnalyzerReference> _analyzerReferences;
                private readonly string _language;
#pragma warning restore IDE0052 // Remove unread private members

                public RemoveAnalyzerReferencesAction(ImmutableArray<AnalyzerReference> analyzerReferences, string language)
                {
                    _analyzerReferences = analyzerReferences;
                    _language = language;
                }

                // Changing analyzer references doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
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

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
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

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }
        }
    }
}
