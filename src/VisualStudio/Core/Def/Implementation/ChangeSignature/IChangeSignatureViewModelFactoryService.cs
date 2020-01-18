// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal interface IChangeSignatureViewModelFactoryService : ILanguageService
    {
        Task<ChangeSignatureIntellisenseTextBoxesViewModel?> CreateViewModelsAsync(
            IContentTypeRegistryService contentTypeRegistryService,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory,
            Document document,
            int insertPosition);

        SymbolDisplayPart[] GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel);

        bool IsTypeNameValid(string typeName);
    }
}
