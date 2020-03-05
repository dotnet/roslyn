// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    [Export(typeof(IMoveToNamespaceOptionsService)), Shared]
    internal class VisualStudioMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
        [ImportingConstructor]
        public VisualStudioMoveToNamespaceOptionsService()
        {
        }

        public MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces,
            ISyntaxFactsService syntaxFactsService)
        {
            var viewModel = new MoveToNamespaceDialogViewModel(
                defaultNamespace,
                availableNamespaces,
                syntaxFactsService);

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
