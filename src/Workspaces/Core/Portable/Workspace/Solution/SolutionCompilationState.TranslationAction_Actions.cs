﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private abstract partial class TranslationAction
    {
        internal sealed class TouchDocumentAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            DocumentState oldState,
            DocumentState newState)
            : TranslationAction(oldProjectState, newProjectState)
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

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver;

            public override TranslationAction? TryMergeWithPrior(TranslationAction priorAction)
            {
                if (priorAction is TouchDocumentAction priorTouchAction &&
                    priorTouchAction._newState == _oldState)
                {
                    // As we're merging ourselves with the prior touch action, we want to keep the old project state
                    // that we are translating from.
                    return new TouchDocumentAction(priorAction.OldProjectState, this.NewProjectState, priorTouchAction._oldState, _newState);
                }

                return null;
            }
        }

        internal sealed class TouchAdditionalDocumentAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            AdditionalDocumentState oldState,
            AdditionalDocumentState newState)
            : TranslationAction(oldProjectState, newProjectState)
        {
            private readonly AdditionalDocumentState _oldState = oldState;
            private readonly AdditionalDocumentState _newState = newState;

            // Changing an additional document doesn't change the compilation directly, so we can "apply" the
            // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping the
            // compilation with stale trees around, answering true is still important.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override TranslationAction? TryMergeWithPrior(TranslationAction priorAction)
            {
                if (priorAction is TouchAdditionalDocumentAction priorTouchAction &&
                    priorTouchAction._newState == _oldState)
                {
                    // As we're merging ourselves with the prior touch action, we want to keep the old project state
                    // that we are translating from.
                    return new TouchAdditionalDocumentAction(priorAction.OldProjectState, this.NewProjectState, priorTouchAction._oldState, _newState);
                }

                return null;
            }

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                var oldText = _oldState.AdditionalText;
                var newText = _newState.AdditionalText;

                return generatorDriver.ReplaceAdditionalText(oldText, newText);
            }
        }

        internal sealed class RemoveDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<DocumentState> documents)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<SyntaxTree>.GetInstance(documents.Length, out var syntaxTrees);
                foreach (var document in documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    syntaxTrees.Add(await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                }

                return oldCompilation.RemoveSyntaxTrees(syntaxTrees);
            }

            // This action removes the specified trees, but leaves the generated trees untouched.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver;
        }

        internal sealed class AddDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<DocumentState> documents)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public readonly ImmutableArray<DocumentState> Documents = documents;

            public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                // Parse all the documents in parallel.
                using var _ = ArrayBuilder<Task<SyntaxTree>>.GetInstance(this.Documents.Length, out var tasks);
                foreach (var document in this.Documents)
                    tasks.Add(Task.Run(async () => await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false), cancellationToken));

                var trees = await Task.WhenAll(tasks).ConfigureAwait(false);
                return oldCompilation.AddSyntaxTrees(trees);
            }

            // This action adds the specified trees, but leaves the generated trees untouched.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver;
        }

        internal sealed class ReplaceAllSyntaxTreesAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            bool isParseOptionChange)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<SyntaxTree>.GetInstance(this.NewProjectState.DocumentStates.Count, out var syntaxTrees);
                foreach (var documentState in this.NewProjectState.DocumentStates.GetStatesInCompilationOrder())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    syntaxTrees.Add(await documentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                }

                return oldCompilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);
            }

            // Because this removes all trees, it'd also remove the generated trees.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => false;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                if (isParseOptionChange)
                {
                    RoslynDebug.AssertNotNull(this.NewProjectState.ParseOptions);
                    return generatorDriver.WithUpdatedParseOptions(this.NewProjectState.ParseOptions);
                }
                else
                {
                    // We are using this as a way to reorder syntax trees -- we don't need to do anything as the driver
                    // will get the new compilation once we pass it to it.
                    return generatorDriver;
                }
            }
        }

        internal sealed class ProjectCompilationOptionsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            bool isAnalyzerConfigChange)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                RoslynDebug.AssertNotNull(this.NewProjectState.CompilationOptions);
                return Task.FromResult(oldCompilation.WithOptions(this.NewProjectState.CompilationOptions));
            }

            // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
            // compilations with stale generated trees.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                if (isAnalyzerConfigChange)
                {
                    return generatorDriver.WithUpdatedAnalyzerConfigOptions(this.NewProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider);
                }
                else
                {
                    // Changing any other option is fine and the driver can be reused. The driver
                    // will get the new compilation once we pass it to it.
                    return generatorDriver;
                }
            }
        }

        internal sealed class ProjectAssemblyNameAction(
            ProjectState oldProjectState,
            ProjectState newProjectState)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation.WithAssemblyName(NewProjectState.AssemblyName));

            // Updating the options of a compilation doesn't require us to reparse trees, so we can use this to update
            // compilations with stale generated trees.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver;
        }

        internal sealed class AddOrRemoveAnalyzerReferencesAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<AnalyzerReference> referencesToAdd = default,
            ImmutableArray<AnalyzerReference> referencesToRemove = default)
            : TranslationAction(oldProjectState, newProjectState)
        {
            // Changing analyzer references doesn't change the compilation directly, so we can "apply" the
            // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping the
            // compilation with stale trees around, answering true is still important.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                var language = this.OldProjectState.Language;
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

        internal sealed class AddAdditionalDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<AdditionalDocumentState> additionalDocuments)
            : TranslationAction(oldProjectState, newProjectState)
        {
            // Changing an additional document doesn't change the compilation directly, so we can "apply" the
            // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping the
            // compilation with stale trees around, answering true is still important.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                return generatorDriver.AddAdditionalTexts(additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
            }
        }

        internal sealed class RemoveAdditionalDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<AdditionalDocumentState> additionalDocuments)
            : TranslationAction(oldProjectState, newProjectState)
        {
            // Changing an additional document doesn't change the compilation directly, so we can "apply" the
            // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping the
            // compilation with stale trees around, answering true is still important.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                return generatorDriver.RemoveAdditionalTexts(additionalDocuments.SelectAsArray(static documentState => documentState.AdditionalText));
            }
        }

        internal sealed class ReplaceGeneratorDriverAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            GeneratorDriver oldGeneratorDriver)
            : TranslationAction(oldProjectState, newProjectState)
        {
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            // Replacing the generator doesn't change the non-generator compilation.  So we can just return the old
            // compilation as is.
            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver _)
            {
                // The GeneratorDriver that we have here is from a prior version of the Project, it may be missing state changes due
                // to changes to the project. We'll update everything here.
                var generatorDriver = oldGeneratorDriver
                    .ReplaceAdditionalTexts(this.NewProjectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText))
                    .WithUpdatedParseOptions(this.NewProjectState.ParseOptions!)
                    .WithUpdatedAnalyzerConfigOptions(this.NewProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider)
                    .ReplaceGenerators(this.NewProjectState.SourceGenerators.ToImmutableArray());

                return generatorDriver;
            }

            public override TranslationAction? TryMergeWithPrior(TranslationAction priorAction)
            {
                // If the prior action is also a ReplaceGeneratorDriverAction, we'd entirely overwrite it's changes,
                // so we can drop the prior one's generator driver entirely.  Note: we still want to use it's
                // `OldProjectState` as that still represents the prior state we're translating from.
                return priorAction is ReplaceGeneratorDriverAction
                    ? new ReplaceGeneratorDriverAction(priorAction.OldProjectState, this.NewProjectState, oldGeneratorDriver)
                    : null;
            }
        }
    }
}
