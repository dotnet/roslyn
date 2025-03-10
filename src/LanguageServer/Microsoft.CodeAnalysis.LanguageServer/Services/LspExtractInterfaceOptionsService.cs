// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspExtractInterfaceOptionsService() : IExtractInterfaceOptionsService
{
    public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        ImmutableArray<ISymbol> extractableMembers,
        string defaultInterfaceName,
        ImmutableArray<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix)
    {
        var extension = document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb";
        return new(
            isCancelled: false,
            extractableMembers,
            defaultInterfaceName,
            defaultInterfaceName + extension,
            ExtractInterfaceOptionsResult.ExtractLocation.SameFile);
    }
}
