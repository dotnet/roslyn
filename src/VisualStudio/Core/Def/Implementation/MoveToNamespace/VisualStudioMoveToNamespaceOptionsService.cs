// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService)), Shared]
    internal class VisualStudioMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMoveToNamespaceOptionsService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<MoveToNamespaceOptionsResult> GetChangeNamespaceOptionsAsync(
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces,
            CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var viewModel = new MoveToNamespaceDialogViewModel(
                defaultNamespace,
                availableNamespaces);

            var dialog = new MoveToNamespaceDialog(viewModel);
            var result = dialog.ShowModal();

            if (result == true)
            {
                return new MoveToNamespaceOptionsResult(viewModel.NamespaceName);
            }
            else
            {
                return MoveToNamespaceOptionsResult.Cancelled;
            }
        }
    }
}
