// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
    internal abstract partial class SuggestedActionWithFlavors : SuggestedAction, ISuggestedActionWithFlavors
    {
        private readonly SuggestedActionSet _additionalFlavors;
        private ImmutableArray<SuggestedActionSet> _allFlavors;

        public SuggestedActionWithFlavors(
            Workspace workspace, ITextBuffer subjectBuffer, ICodeActionEditHandlerService editHandler, 
            IWaitIndicator waitIndicator, CodeAction codeAction, object provider, 
            IAsynchronousOperationListener operationListener,
            SuggestedActionSet additionalFlavors = null) 
            : base(workspace, subjectBuffer, editHandler, waitIndicator, codeAction,
                  provider, operationListener, actionSets: null)
        {
            _additionalFlavors = additionalFlavors;
        }

        /// <summary>
        /// HasActionSets is always true because we always know we provide 'preview changes'.
        /// </summary>
        public override bool HasActionSets => true;

        public async sealed override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Light bulb will always invoke this property on the UI thread.
            AssertIsForeground();

            if (_allFlavors.IsDefault)
            {
                var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();

                // We use ConfigureAwait(true) to stay on the UI thread.
                _allFlavors = await extensionManager.PerformFunctionAsync(
                    Provider, () => CreateAllFlavors(cancellationToken),
                    defaultValue: ImmutableArray<SuggestedActionSet>.Empty).ConfigureAwait(true);
            }

            Contract.ThrowIfTrue(_allFlavors.IsDefault);
            return _allFlavors;
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

            return new SuggestedActionSet(ImmutableArray.Create(previewChangesAction));
        }
    }
}