// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal interface IChangeSignatureLanguageService : ILanguageService
    {
        Task<IntellisenseTextBoxViewModel[]> CreateViewModelsAsync(
            string[] rolesCollectionType,
            string[] rolesCollectionName,
            int insertPosition,
            Document document,
            string documentText,
            IContentType contentType,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory,
            CancellationToken cancellationToken);

        void GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel, List<SymbolDisplayPart> displayParts);

        bool IsTypeNameValid(string typeName);
    }
}
