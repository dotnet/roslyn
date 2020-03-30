// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal interface IChangeSignatureViewModelFactoryService : ILanguageService
    {
        SymbolDisplayPart[] GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel);

        bool IsTypeNameValid(string typeName);

        SyntaxNode GetTypeNode(string typeName);
    }
}
