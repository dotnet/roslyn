// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        private readonly IGlyphService _glyphService;

        [ImportingConstructor]
        public VisualStudioExtractInterfaceOptionsService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
            ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> allTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName)
        {
            var viewModel = new ExtractInterfaceDialogViewModel(
                syntaxFactsService,
                _glyphService,
                notificationService,
                defaultInterfaceName,
                extractableMembers,
                allTypeNames,
                defaultNamespace,
                generatedNameTypeParameterSuffix,
                languageName,
                languageName == LanguageNames.CSharp ? ".cs" : ".vb");

            var dialog = new ExtractInterfaceDialog(viewModel);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                var includedMembers = viewModel.MemberContainers.Where(c => c.IsChecked).Select(c => c.MemberSymbol);

                return new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: includedMembers,
                    interfaceName: viewModel.InterfaceName.Trim(),
                    fileName: viewModel.FileName.Trim(),
                    location: GetLocation(viewModel.Destination));
            }
            else
            {
                return ExtractInterfaceOptionsResult.Cancelled;
            }
        }

        private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(ExtractInterface.ExtractInterfaceDialogViewModel.InterfaceDestination destination)
        {
            switch (destination)
            {
                case ExtractInterfaceDialogViewModel.InterfaceDestination.CurrentFile: return ExtractInterfaceOptionsResult.ExtractLocation.SameFile;
                case ExtractInterfaceDialogViewModel.InterfaceDestination.NewFile: return ExtractInterfaceOptionsResult.ExtractLocation.NewFile;
                default: throw new InvalidOperationException();
            }
        }
    }
}
