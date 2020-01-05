// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal interface IMoveToNamespaceOptionsService : IWorkspaceService
    {
        MoveToNamespaceOptionsResult GetChangeNamespaceOptions(
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces,
            ISyntaxFactsService syntaxFactsService);
    }
}
