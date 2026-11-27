// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.CodeAnalysis.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultImportProjectItem(string name, string content) : RazorProjectItem
{
    private readonly string _name = name;
    private readonly InMemoryFileContent _fileContent = new(content);
    private RazorSourceDocument? _source;

    // These properties all return null for default imports
    public override string BasePath => null!;
    public override string FilePath => null!;
    public override string PhysicalPath => null!;

    public override bool Exists => true;

    public override Stream Read() => _fileContent.CreateStream();

    internal override RazorSourceDocument GetSource()
        => _source ?? InterlockedOperations.Initialize(ref _source, base.GetSource());

    protected override string DebuggerToString() => _name;
}
