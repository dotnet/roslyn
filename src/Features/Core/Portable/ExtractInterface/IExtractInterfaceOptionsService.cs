// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExtractInterface;

internal interface IExtractInterfaceOptionsService : IWorkspaceService
{
    ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        ImmutableArray<ISymbol> extractableMembers,
        string defaultInterfaceName,
        ImmutableArray<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix);
}
