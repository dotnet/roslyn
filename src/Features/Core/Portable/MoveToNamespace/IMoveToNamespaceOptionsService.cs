// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.MoveToNamespace;

internal interface IMoveToNamespaceOptionsService : IWorkspaceService
{
    MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
        string defaultNamespace,
        ImmutableArray<string> availableNamespaces,
        ISyntaxFacts syntaxFactsService);
}
