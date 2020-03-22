// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal abstract class ChangeSignatureViewModelFactoryService : IChangeSignatureViewModelFactoryService
    {
        public ChangeSignatureViewModelFactoryService()
        {
        }

        public abstract SymbolDisplayPart[] GeneratePreviewDisplayParts(
            ChangeSignatureDialogViewModel.AddedParameterViewModel addedParameterViewModel);

        public abstract bool IsTypeNameValid(string typeName);

        public abstract SyntaxNode GetTypeNode(string typeName);
    }
}
