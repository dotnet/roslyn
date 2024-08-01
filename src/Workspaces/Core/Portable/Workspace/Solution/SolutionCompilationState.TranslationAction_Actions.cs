// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private abstract partial class TranslationAction
    {
        internal sealed class TouchDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<DocumentState> oldStates,
            ImmutableArray<DocumentState> newStates) : TranslationAction(oldProjectState, newProjectState)
        {
            private readonly ImmutableArray<DocumentState> _oldStates = oldStates;
            private readonly ImmutableArray<DocumentState> _newStates = newStates;

            public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                var finalCompilation = oldCompilation;
                for (var i = 0; i < _newStates.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var newState = _newStates[i];
                    var oldState = _oldStates[i];
                    finalCompilation = finalCompilation.ReplaceSyntaxTree(
                        await oldState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false),
                        await newState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                }

                return finalCompilation;
            }

            /// <summary>
            /// Replacing a single tree doesn't impact the generated trees in a compilation, so we can use this against
            /// compilations that have generated trees.
            /// </summary>
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver;

            public override TranslationAction? TryMergeWithPrior(TranslationAction priorAction)
            {
                if (priorAction is TouchDocumentsAction priorTouchAction &&
                    priorTouchAction._newStates.SequenceEqual(_oldStates))
                {
                    // As we're merging ourselves with the prior touch action, we want to keep the old project state
                    // that we are translating from.
                    return new TouchDocumentsAction(priorAction.OldProjectState, NewProjectState, priorTouchAction._oldStates, _newStates);
                }

                return null;
            }
        }

        internal sealed class TouchAdditionalDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState,
            ImmutableArray<AdditionalDocumentState> oldStates,
            ImmutableArray<AdditionalDocumentState> newStates)
            : TranslationAction(oldProjectState, newProjectState)
        {
            private readonly ImmutableArray<AdditionalDocumentState> _oldStates = oldStates;
            private readonly ImmutableArray<AdditionalDocumentState> _newStates = newStates;

            // Changing an additional document doesn't change the compilation directly, so we can "apply" the
            // translation (which is a no-op). Since we use a 'false' here to mean that it's not worth keeping the
            // compilation with stale trees around, answering true is still important.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
                => Task.FromResult(oldCompilation);

            public override TranslationAction? TryMergeWithPrior(TranslationAction priorAction)
            {
                if (priorAction is TouchAdditionalDocumentsAction priorTouchAction &&
                    priorTouchAction._newStates.SequenceEqual(_oldStates))
                {
                    // As we're merging ourselves with the prior touch action, we want to keep the old project state
                    // that we are translating from.
                    return new TouchAdditionalDocumentsAction(priorAction.OldProjectState, NewProjectState, priorTouchAction._oldStates, _newStates);
                }

                return null;
            }

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
            {
                for (var i = 0; i < _newStates.Length; i++)
                {
                    generatorDriver = generatorDriver.ReplaceAdditionalText(_oldStates[i].AdditionalText, _newStates[i].AdditionalText);
                }

                return generatorDriver;
            }
        }

        internal sealed class TouchAnalyzerConfigDocumentsAction(
            ProjectState oldProjectState,
            ProjectState newProjectState)
            : TranslationAction(oldProjectState, newProjectState)
        {
            /// <summary>
            /// Updating editorconfig document updates <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/>.
            /// </summary>
            public override Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                RoslynDebug.AssertNotNull(this.NewProjectState.CompilationOptions);
                return Task.FromResult(oldCompilation.WithOptions(this.NewProjectState.CompilationOptions));
            }

            // Updating the analyzer config optons doesn't require us to reparse trees, so we can use this to update
            // compilations with stale generated trees.
            public override bool CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput => true;

            public override GeneratorDriver TransformGeneratorDriver(GeneratorDriver generatorDriver)
                => generatorDriver.WithUpdatedAnalyzerConfigOptions(NewProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider);
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
            /// <summary>
            /// Amount to break batches of documents into.  That allows us to process things in parallel, without also
            /// creating too many individual actions that then need to be processed.
            /// </summary>
            public const int AddDocumentsBatchSize = 32;

            public readonly ImmutableArray<DocumentState> Documents = documents;

            public override async Task<Compilation> TransformCompilationAsync(Compilation oldCompilation, CancellationToken cancellationToken)
            {
                // TODO(cyrusn): Do we need to ensure that the syntax trees we add to the compilation are in the same
                // order as the documents array we have added to the project?  If not, we can remove this map and the
                // sorting below.
                using var _ = PooledDictionary<DocumentState, int>.GetInstance(out var documentToIndex);
                foreach (var document in this.Documents)
                    documentToIndex.Add(document, documentToIndex.Count);

                var documentsAndTrees = await ProducerConsumer<(DocumentState document, SyntaxTree tree)>.RunParallelAsync(
                    source: this.Documents,
                    produceItems: static async (document, callback, _, cancellationToken) =>
                        callback((document, await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))),
                    args: default(VoidResult),
                    cancellationToken).ConfigureAwait(false);

                return oldCompilation.AddSyntaxTrees(documentsAndTrees
                    .Sort((dt1, dt2) => documentToIndex[dt1.document] - documentToIndex[dt2.document])
                    .Select(static dt => dt.tree));
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
            ProjectState newProjectState)
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
                // Changing any other option is fine and the driver can be reused. The driver
                // will get the new compilation once we pass it to it.
                return generatorDriver;
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
                    .ReplaceGenerators(GetSourceGenerators(this.NewProjectState));

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
