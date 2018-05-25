// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal interface ICodeModelNavigationPointService : ILanguageService
    {
        /// <summary>
        /// Retrieves the start point of a given node for the specified EnvDTE.vsCMPart.
        /// </summary>
        VirtualTreePoint? GetStartPoint(SyntaxNode node, OptionSet options, EnvDTE.vsCMPart? part = null);

        /// <summary>
        /// Retrieves the end point of a given node for the specified EnvDTE.vsCMPart.
        /// </summary>
        VirtualTreePoint? GetEndPoint(SyntaxNode node, OptionSet options, EnvDTE.vsCMPart? part = null);
    }
}
