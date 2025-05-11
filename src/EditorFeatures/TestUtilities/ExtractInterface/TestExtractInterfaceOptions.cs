// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Test), Shared, PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TestExtractInterfaceOptionsService() : IExtractInterfaceOptionsService
{
    public ImmutableArray<ISymbol> AllExtractableMembers { get; private set; }
    public string DefaultInterfaceName { get; private set; }
    public ImmutableArray<string> ConflictingTypeNames { get; private set; }
    public string DefaultNamespace { get; private set; }
    public string GeneratedNameTypeParameterSuffix { get; set; }

    public bool IsCancelled { get; set; }
    public string ChosenInterfaceName { get; set; }
    public string ChosenFileName { get; set; }
    public IEnumerable<ISymbol> ChosenMembers { get; set; }
    public bool SameFile { get; set; }

    public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        ImmutableArray<ISymbol> extractableMembers,
        string defaultInterfaceName,
        ImmutableArray<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix)
    {
        this.AllExtractableMembers = extractableMembers;
        this.DefaultInterfaceName = defaultInterfaceName;
        this.ConflictingTypeNames = conflictingTypeNames;
        this.DefaultNamespace = defaultNamespace;
        this.GeneratedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;

        var result = IsCancelled
            ? ExtractInterfaceOptionsResult.Cancelled
            : new ExtractInterfaceOptionsResult(
                isCancelled: false,
                includedMembers: (ChosenMembers ?? AllExtractableMembers).AsImmutable(),
                interfaceName: ChosenInterfaceName ?? defaultInterfaceName,
                fileName: ChosenFileName ?? defaultInterfaceName,
                location: SameFile ? ExtractInterfaceOptionsResult.ExtractLocation.SameFile : ExtractInterfaceOptionsResult.ExtractLocation.NewFile);

        return result;
    }
}
