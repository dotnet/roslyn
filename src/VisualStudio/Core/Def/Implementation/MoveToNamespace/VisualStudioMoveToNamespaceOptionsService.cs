// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveToNamespace;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    [ExportWorkspaceService(typeof(IMoveToNamespaceOptionsService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioMoveToNamespaceOptionsService : IMoveToNamespaceOptionsService
    {
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
