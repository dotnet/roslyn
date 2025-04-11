// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste;

/// <summary>
/// Special implementation of the copy paste service for testing.  This one avoids going through the actual
/// clipboard, avoiding complexity of interacting with that during testing.
/// </summary>
[ExportWorkspaceService(typeof(IStringCopyPasteService), ServiceLayer.Test), Shared, PartNotDiscoverable]
public sealed class TestStringCopyPasteService : IStringCopyPasteService
{
    private string? _key;
    private string? _data;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestStringCopyPasteService()
    {
    }

    public bool TrySetClipboardData(string key, string data)
    {
        _key = key;
        _data = data;
        return true;
    }

    public string? TryGetClipboardData(string key)
        => _key == key ? _data : null;
}
