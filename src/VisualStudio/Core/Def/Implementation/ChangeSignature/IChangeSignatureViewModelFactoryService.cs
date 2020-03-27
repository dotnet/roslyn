// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal interface IChangeSignatureViewModelFactoryService : ILanguageService
    {
        Task CreateAndSetViewModelsAsync(
            Document document,
            int insertPosition,
            ContentControl typeNameContentControl,
            ContentControl parameterNameContentControl);

        SymbolDisplayPart[] GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel);

        bool IsTypeNameValid(string typeName);

        SyntaxNode GetTypeNode(string typeName);
    }
}
