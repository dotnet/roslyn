﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractInterface;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.ExtractInterface;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OmniSharpExtractInterfaceOptionsService(
    IOmniSharpExtractInterfaceOptionsService omniSharpExtractInterfaceOptionsService) : IExtractInterfaceOptionsService
{
    private readonly IOmniSharpExtractInterfaceOptionsService _omniSharpExtractInterfaceOptionsService = omniSharpExtractInterfaceOptionsService;

    public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        List<ISymbol> extractableMembers,
        string defaultInterfaceName,
        List<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix,
        CancellationToken cancellationToken)
    {
        var result = _omniSharpExtractInterfaceOptionsService.GetExtractInterfaceOptions(extractableMembers, defaultInterfaceName);
        return new(
            result.IsCancelled,
            result.IncludedMembers,
            result.InterfaceName,
            result.FileName,
            (ExtractInterfaceOptionsResult.ExtractLocation)result.Location);
    }
}
