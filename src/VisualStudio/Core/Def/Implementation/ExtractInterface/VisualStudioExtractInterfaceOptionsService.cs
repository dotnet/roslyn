// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IWaitIndicator _waitIndicator;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public VisualStudioExtractInterfaceOptionsService(IGlyphService glyphService, IThreadingContext threadingContext, IWaitIndicator waitIndicator)
        {
            _glyphService = glyphService;
            _waitIndicator = waitIndicator;
            _threadingContext = threadingContext;
        }

        public async Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            INamedTypeSymbol symbol,
            IEnumerable<ISymbol> extractableMembers,
            string languageName)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            var memberViewModels = extractableMembers
                .SelectAsArray(member =>
                    new MoveMembersSymbolViewModel(member, _glyphService)
                    {
                        IsChecked = true,
                        MakeAbstract = false,
                        IsMakeAbstractCheckable = false,
                        IsCheckable = true
                    });

            var viewModel = new MoveMembersDialogViewModel(
                _waitIndicator,
                symbol,
                memberViewModels,
                dependentsMap: null,
                fileExtension: languageName == LanguageNames.CSharp ? ".cs" : ".vb");

            var dialog = new MoveMembersDialog(ServicesVSResources.Extract_Interface, description: "", viewModel);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                var includedMembers = viewModel.GetCheckedMembers().Select(c => c.member);
                var newTypeViewModel = (MoveToNewTypeControlViewModel)viewModel.SelectDestinationViewModel;
                return new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: includedMembers.AsImmutable(),
                    fileName: newTypeViewModel.FileName,
                    interfaceName: viewModel.SelectedDestination.Name.Trim(),
                    location: GetLocation(newTypeViewModel.NewSymbolDestination));
            }
            else
            {
                return ExtractInterfaceOptionsResult.Cancelled;
            }
        }

        private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(SymbolDestination destination)
        {
            switch (destination)
            {
                case SymbolDestination.CurrentFile: return ExtractInterfaceOptionsResult.ExtractLocation.SameFile;
                case SymbolDestination.NewFile: return ExtractInterfaceOptionsResult.ExtractLocation.NewFile;
                default: throw new InvalidOperationException();
            }
        }
    }
}
