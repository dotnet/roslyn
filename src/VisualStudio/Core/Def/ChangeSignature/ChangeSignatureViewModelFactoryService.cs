// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

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
