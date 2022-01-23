// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            string languageName,
            CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var viewModel = new ExtractInterfaceDialogViewModel(
                syntaxFactsService,
                _glyphService,
                notificationService,
                defaultInterfaceName,
                extractableMembers,
                allTypeNames,
                defaultNamespace,
                generatedNameTypeParameterSuffix,
                languageName);

            var dialog = new ExtractInterfaceDialog(viewModel);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                var includedMembers = viewModel.MemberContainers.Where(c => c.IsChecked).Select(c => c.Symbol);

                return new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: includedMembers.AsImmutable(),
                    interfaceName: viewModel.DestinationViewModel.TypeName.Trim(),
                    fileName: viewModel.DestinationViewModel.FileName.Trim(),
                    location: GetLocation(viewModel.DestinationViewModel.Destination));
            }
            else
            {
                return ExtractInterfaceOptionsResult.Cancelled;
            }
        }

        private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(NewTypeDestination destination)
        {
            switch (destination)
            {
                case NewTypeDestination.CurrentFile: return ExtractInterfaceOptionsResult.ExtractLocation.SameFile;
                case NewTypeDestination.NewFile: return ExtractInterfaceOptionsResult.ExtractLocation.NewFile;
                default: throw ExceptionUtilities.UnexpectedValue(destination);
            }
        }
    }
}
