// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

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
                        priorTouchAction._newState == _oldState)
                    {
                        return new TouchDocumentAction(priorTouchAction._oldState, _newState);
                    }

                    return null;
                }
            }

            internal sealed class TouchAdditionalDocumentAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly AdditionalDocumentState _oldState;
                private readonly AdditionalDocumentState _newState;

                public TouchAdditionalDocumentAction(AdditionalDocumentState oldState, AdditionalDocumentState newState)
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
                        priorTouchAction._newState == _oldState)
                    {
                        return new TouchAdditionalDocumentAction(priorTouchAction._oldState, _newState);
                    }

                    return null;
                }

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    var oldText = _oldState.AdditionalText;
                    var newText = _newState.AdditionalText;

                    return generatorDriver.ReplaceAdditionalText(oldText, newText);
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
                private readonly bool _isParseOptionChange;

                public ReplaceAllSyntaxTreesAction(ProjectState state, bool isParseOptionChange)
                {
                    _state = state;
                    _isParseOptionChange = isParseOptionChange;
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

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    if (_isParseOptionChange)
                    {
                        RoslynDebug.AssertNotNull(_state.ParseOptions);
                        return generatorDriver.WithUpdatedParseOptions(_state.ParseOptions);
                    }
                    else
                    {
                        // We are using this as a way to reorder syntax trees -- we don't need to do anything as the driver
                        // will get the new compilation once we pass it to it.
                        return generatorDriver;
                    }
                }
            }

            internal sealed class ProjectCompilationOptionsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ProjectState _state;
                private readonly bool _isAnalyzerConfigChange;

                public ProjectCompilationOptionsAction(ProjectState state, bool isAnalyzerConfigChange)
                {
                    _state = state;
                    _isAnalyzerConfigChange = isAnalyzerConfigChange;
                }

                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    RoslynDebug.AssertNotNull(_state.CompilationOptions);
                    return Task.FromResult(oldCompilation.WithOptions(_state.CompilationOptions));
                }

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    if (_isAnalyzerConfigChange)
                    {
                        return generatorDriver.WithUpdatedAnalyzerConfigOptions(_state.AnalyzerOptions.AnalyzerConfigOptionsProvider);
                    }
                    else
                    {
                        // Changing any other option is fine and the driver can be reused. The driver
                        // will get the new compilation once we pass it to it.
                        return generatorDriver;
                    }
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

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class AddOrRemoveAnalyzerReferencesAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly string _language;
                private readonly ImmutableArray<AnalyzerReference> _referencesToAdd;
                private readonly ImmutableArray<AnalyzerReference> _referencesToRemove;

                public AddOrRemoveAnalyzerReferencesAction(string language, ImmutableArray<AnalyzerReference> referencesToAdd = default, ImmutableArray<AnalyzerReference> referencesToRemove = default)
                {
                    _language = language;
                    _referencesToAdd = referencesToAdd;
                    _referencesToRemove = referencesToRemove;
                }

                // Changing analyzer references doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    if (!_referencesToRemove.IsDefaultOrEmpty)
                    {
                        generatorDriver = generatorDriver.RemoveGenerators(_referencesToRemove.SelectMany(r => r.GetGenerators(_language)).ToImmutableArray());
                    }

                    if (!_referencesToAdd.IsDefaultOrEmpty)
                    {
                        generatorDriver = generatorDriver.AddGenerators(_referencesToAdd.SelectMany(r => r.GetGenerators(_language)).ToImmutableArray());
                    }

                    return generatorDriver;
                }
            }

            internal sealed class AddAdditionalDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<AdditionalDocumentState> _additionalDocuments;

                public AddAdditionalDocumentsAction(ImmutableArray<AdditionalDocumentState> additionalDocuments)
                {
                    _additionalDocuments = additionalDocuments;
                }

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    return generatorDriver.AddAdditionalTexts(_additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
                }
            }

            internal sealed class RemoveAdditionalDocumentsAction : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly ImmutableArray<AdditionalDocumentState> _additionalDocuments;

                public RemoveAdditionalDocumentsAction(ImmutableArray<AdditionalDocumentState> additionalDocuments)
                {
                    _additionalDocuments = additionalDocuments;
                }

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    return generatorDriver.RemoveAdditionalTexts(_additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
                }
            }
        }
    }
}
