// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class TelemetryMessage : IDisposable
{
    private string? _pdbSource;
    private string? _sourceFileSource;
    private string? _referenceAssembly;
    private string? _dll;
    private bool? _decompiled;

    private readonly IDisposable _logBlock;

    public TelemetryMessage(CancellationToken cancellationToken)
    {
        var logMessage = KeyValueLogMessage.Create(LogType.UserAction, SetLogProperties);
        _logBlock = Logger.LogBlock(FunctionId.NavigateToExternalSources, logMessage, cancellationToken);
    }

    public void SetPdbSource(string source)
    {
        _pdbSource = source;
    }

    public void SetSourceFileSource(string source)
    {
        _sourceFileSource = source;
    }

    public void SetReferenceAssembly(string referenceAssembly)
    {
        _referenceAssembly = referenceAssembly;
    }

    public void SetDll(string dll)
    {
        _dll = dll;
    }

    public void SetDecompiled(bool decompiled)
    {
        _decompiled = decompiled;
    }

    private void SetLogProperties(Dictionary<string, object?> properties)
    {
        properties["pdb"] = _pdbSource ?? "none";
        properties["source"] = _sourceFileSource ?? "none";
        properties["referenceassembly"] = _referenceAssembly ?? "no";
        if (_dll is not null)
        {
            properties["dll"] = new PiiValue(_dll);
        }

        properties["decompiled"] = _decompiled switch
        {
            true => "yes",
            false => "no",
            _ => "n/a"
        };
    }

    public void Dispose()
    {
        _logBlock.Dispose();
    }
}
