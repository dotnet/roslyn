// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public VisualStudioExtractInterfaceOptionsService(IGlyphService glyphService, IThreadingContext threadingContext)
        {
            _glyphService = glyphService;
            _threadingContext = threadingContext;
        }

        public async Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> allTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

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
                var includedMembers = viewModel.MemberContainers.Where(c => c.IsChecked).Select(c => c.Symbol);

                return new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: includedMembers.AsImmutable(),
                    interfaceName: viewModel.InterfaceName.Trim(),
                    fileName: viewModel.FileName.Trim(),
                    location: GetLocation(viewModel.Destination));
            }
            else
            {
                return ExtractInterfaceOptionsResult.Cancelled;
            }
        }

        private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(InterfaceDestination destination)
        {
            switch (destination)
            {
                case InterfaceDestination.CurrentFile: return ExtractInterfaceOptionsResult.ExtractLocation.SameFile;
                case InterfaceDestination.NewFile: return ExtractInterfaceOptionsResult.ExtractLocation.NewFile;
                default: throw new InvalidOperationException();
            }
        }
    }
}
