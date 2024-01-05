// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionCompilationState
    {
        private abstract partial class CompilationAndGeneratorDriverTranslationAction
        {
            internal sealed class TouchDocumentAction(DocumentState oldState, DocumentState newState) : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly DocumentState _oldState = oldState;
                private readonly DocumentState _newState = newState;

                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return oldCompilation.ReplaceSyntaxTree(
                        await _oldState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false),
                        await _newState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
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

            internal sealed class TouchAdditionalDocumentAction(AdditionalDocumentState oldState, AdditionalDocumentState newState) : CompilationAndGeneratorDriverTranslationAction
            {
                private readonly AdditionalDocumentState _oldState = oldState;
                private readonly AdditionalDocumentState _newState = newState;

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

            internal sealed class RemoveDocumentsAction(ImmutableArray<DocumentState> documents) : CompilationAndGeneratorDriverTranslationAction
            {
                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(documents.Length);
                    foreach (var document in documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.RemoveSyntaxTrees(syntaxTrees);
                }

                // This action removes the specified trees, but leaves the generated trees untouched.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class AddDocumentsAction(ImmutableArray<DocumentState> documents) : CompilationAndGeneratorDriverTranslationAction
            {
                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(capacity: documents.Length);
                    foreach (var document in documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                    }

                    return oldCompilation.AddSyntaxTrees(syntaxTrees);
                }

                // This action adds the specified trees, but leaves the generated trees untouched.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class ReplaceAllSyntaxTreesAction(ProjectState state, bool isParseOptionChange) : CompilationAndGeneratorDriverTranslationAction
            {
                public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    var syntaxTrees = new List<SyntaxTree>(capacity: state.DocumentStates.Count);

                    foreach (var documentState in state.DocumentStates.GetStatesInCompilationOrder())
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
                    if (isParseOptionChange)
                    {
                        RoslynDebug.AssertNotNull(state.ParseOptions);
                        return generatorDriver.WithUpdatedParseOptions(state.ParseOptions);
                    }
                    else
                    {
                        // We are using this as a way to reorder syntax trees -- we don't need to do anything as the driver
                        // will get the new compilation once we pass it to it.
                        return generatorDriver;
                    }
                }
            }

            internal sealed class ProjectCompilationOptionsAction(ProjectState state, bool isAnalyzerConfigChange) : CompilationAndGeneratorDriverTranslationAction
            {
                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    RoslynDebug.AssertNotNull(state.CompilationOptions);
                    return Task.FromResult(oldCompilation.WithOptions(state.CompilationOptions));
                }

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    if (isAnalyzerConfigChange)
                    {
                        return generatorDriver.WithUpdatedAnalyzerConfigOptions(state.AnalyzerOptions.AnalyzerConfigOptionsProvider);
                    }
                    else
                    {
                        // Changing any other option is fine and the driver can be reused. The driver
                        // will get the new compilation once we pass it to it.
                        return generatorDriver;
                    }
                }
            }

            internal sealed class ProjectAssemblyNameAction(string assemblyName) : CompilationAndGeneratorDriverTranslationAction
            {
                public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                {
                    return Task.FromResult(oldCompilation.WithAssemblyName(assemblyName));
                }

                // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
                // compilations with stale generated trees.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;
            }

            internal sealed class AddOrRemoveAnalyzerReferencesAction(string language, ImmutableArray<AnalyzerReference> referencesToAdd = default, ImmutableArray<AnalyzerReference> referencesToRemove = default) : CompilationAndGeneratorDriverTranslationAction
            {

                // Changing analyzer references doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    if (!referencesToRemove.IsDefaultOrEmpty)
                    {
                        generatorDriver = generatorDriver.RemoveGenerators(referencesToRemove.SelectManyAsArray(r => r.GetGenerators(language)));
                    }

                    if (!referencesToAdd.IsDefaultOrEmpty)
                    {
                        generatorDriver = generatorDriver.AddGenerators(referencesToAdd.SelectManyAsArray(r => r.GetGenerators(language)));
                    }

                    return generatorDriver;
                }
            }

            internal sealed class AddAdditionalDocumentsAction(ImmutableArray<AdditionalDocumentState> additionalDocuments) : CompilationAndGeneratorDriverTranslationAction
            {

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    return generatorDriver.AddAdditionalTexts(additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
                }
            }

            internal sealed class RemoveAdditionalDocumentsAction(ImmutableArray<AdditionalDocumentState> additionalDocuments) : CompilationAndGeneratorDriverTranslationAction
            {

                // Changing an additional document doesn't change the compilation directly, so we can "apply" the
                // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping
                // the compilation with stale trees around, answering true is still important.
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver generatorDriver)
                {
                    return generatorDriver.RemoveAdditionalTexts(additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
                }
            }

            internal sealed class ReplaceGeneratorDriverAction(GeneratorDriver oldGeneratorDriver, ProjectState newProjectState) : CompilationAndGeneratorDriverTranslationAction
            {
                public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

                public override GeneratorDriver? TransformGeneratorDriver(GeneratorDriver _)
                {
                    // The GeneratorDriver that we have here is from a prior version of the Project, it may be missing state changes due
                    // to changes to the project. We'll update everything here.
                    var generatorDriver = oldGeneratorDriver.ReplaceAdditionalTexts(newProjectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText))
                                                     .WithUpdatedParseOptions(newProjectState.ParseOptions!)
                                                     .WithUpdatedAnalyzerConfigOptions(newProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider)
                                                     .ReplaceGenerators(newProjectState.SourceGenerators.ToImmutableArray());

                    return generatorDriver;
                }

                public override CompilationAndGeneratorDriverTranslationAction? TryMergeWithPrior(CompilationAndGeneratorDriverTranslationAction priorAction)
                {
                    // If the prior action is also a ReplaceGeneratorDriverAction, we'd entirely overwrite it's changes, so we can drop the prior one entirely.
                    return priorAction is ReplaceGeneratorDriverAction ? this : null;
                }
            }
        }
    }
}
