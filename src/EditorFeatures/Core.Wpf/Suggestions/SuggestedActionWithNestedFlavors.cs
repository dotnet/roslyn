// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Base type for all SuggestedActions that have 'flavors'.  'Flavors' are child actions that
    /// are presented as simple links, not as menu-items, in the light-bulb.  Examples of 'flavors'
    /// include 'preview changes' (for refactorings and fixes) and 'fix all in document, project, solution'
    /// (for fixes).
    /// 
    /// Because all derivations support 'preview changes', we bake that logic into this base type.
    /// </summary>
    internal abstract partial class SuggestedActionWithNestedFlavors : SuggestedAction, ISuggestedActionWithFlavors
    {
        private readonly SuggestedActionSet _additionalFlavors;
        private ImmutableArray<SuggestedActionSet> _nestedFlavors;

        public SuggestedActionWithNestedFlavors(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace, ITextBuffer subjectBuffer,
            object provider, CodeAction codeAction,
            SuggestedActionSet additionalFlavors = null)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer,
                   provider, codeAction)
        {
            _additionalFlavors = additionalFlavors;
        }

        /// <summary>
        /// HasActionSets is always true because we always know we provide 'preview changes'.
        /// </summary>
        public sealed override bool HasActionSets => true;

        public async sealed override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Light bulb will always invoke this property on the UI thread.
            AssertIsForeground();

            if (_nestedFlavors.IsDefault)
            {
                var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();

                // We use ConfigureAwait(true) to stay on the UI thread.
                _nestedFlavors = await extensionManager.PerformFunctionAsync(
                    Provider, () => CreateAllFlavors(cancellationToken),
                    defaultValue: ImmutableArray<SuggestedActionSet>.Empty).ConfigureAwait(true);
            }

            Contract.ThrowIfTrue(_nestedFlavors.IsDefault);
            return _nestedFlavors;
        }

        private async Task<ImmutableArray<SuggestedActionSet>> CreateAllFlavors(CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<SuggestedActionSet>.GetInstance();

            // We use ConfigureAwait(true) to stay on the UI thread.
            var previewChangesSuggestedActionSet = await GetPreviewChangesFlavor(cancellationToken).ConfigureAwait(true);
            if (previewChangesSuggestedActionSet != null)
            {
                builder.Add(previewChangesSuggestedActionSet);
            }

            if (_additionalFlavors != null)
            {
                builder.Add(_additionalFlavors);
            }

            return builder.ToImmutableAndFree();
        }

        private async Task<SuggestedActionSet> GetPreviewChangesFlavor(CancellationToken cancellationToken)
        {
            // We use ConfigureAwait(true) to stay on the UI thread.
            var previewChangesAction = await PreviewChangesSuggestedAction.CreateAsync(
                this, cancellationToken).ConfigureAwait(true);
            if (previewChangesAction == null)
            {
                return null;
            }

            return new SuggestedActionSet(categoryName: null, actions: ImmutableArray.Create(previewChangesAction));
        }

        // HasPreview is called synchronously on the UI thread. In order to avoid blocking the UI thread,
        // we need to provide a 'quick' answer here as opposed to the 'right' answer. Providing the 'right'
        // answer is expensive (because we will need to call CodeAction.GetPreviewOperationsAsync() for this
        // and this will involve computing the changed solution for the ApplyChangesOperation for the fix /
        // refactoring). So we always return 'true' here (so that platform will call GetActionSetsAsync()
        // below). Platform guarantees that nothing bad will happen if we return 'true' here and later return
        // 'null' / empty collection from within GetPreviewAsync().
        public override bool HasPreview => true;

        public override async Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Light bulb will always invoke this function on the UI thread.
            AssertIsForeground();

            var previewPaneService = Workspace.Services.GetService<IPreviewPaneService>();
            if (previewPaneService == null)
            {
                return null;
            }

            // after this point, this method should only return at GetPreviewPane. otherwise, DifferenceViewer will leak
            // since there is no one to close the viewer
            var preferredDocumentId = Workspace.GetDocumentIdInCurrentContext(SubjectBuffer.AsTextContainer());
            var preferredProjectId = preferredDocumentId?.ProjectId;

            var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
            var previewContents = await extensionManager.PerformFunctionAsync(Provider, async () =>
            {
                // We need to stay on UI thread after GetPreviewResultAsync() so that TakeNextPreviewAsync()
                // below can execute on UI thread. We use ConfigureAwait(true) to stay on the UI thread.
                var previewResult = await GetPreviewResultAsync(cancellationToken).ConfigureAwait(true);
                if (previewResult == null)
                {
                    return null;
                }
                else
                {
                    // TakeNextPreviewAsync() needs to run on UI thread.
                    AssertIsForeground();
                    return await previewResult.GetPreviewsAsync(preferredDocumentId, preferredProjectId, cancellationToken).ConfigureAwait(true);
                }

                // GetPreviewPane() below needs to run on UI thread. We use ConfigureAwait(true) to stay on the UI thread.
            }, defaultValue: null).ConfigureAwait(true);

            // GetPreviewPane() needs to run on the UI thread.
            AssertIsForeground();

            return previewPaneService.GetPreviewPane(GetDiagnostic(), previewContents);
        }

        protected virtual DiagnosticData GetDiagnostic() => null;
    }
}
