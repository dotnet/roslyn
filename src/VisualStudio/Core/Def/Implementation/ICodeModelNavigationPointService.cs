// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
