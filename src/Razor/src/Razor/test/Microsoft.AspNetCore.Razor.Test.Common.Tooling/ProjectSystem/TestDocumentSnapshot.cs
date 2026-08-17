// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal sealed class TestDocumentSnapshot : IDocumentSnapshot
{
    private readonly string _filePath;
    private readonly RazorCodeDocument _codeDocument;

    public string FilePath => _filePath;
    public RazorFileKind FileKind => throw new NotImplementedException();
    public string TargetPath => throw new NotImplementedException();
    public IProjectSnapshot Project => throw new NotImplementedException();
    public int Version => throw new NotImplementedException();

    private TestDocumentSnapshot(string filePath, RazorCodeDocument codeDocument)
    {
        _filePath = filePath;
        _codeDocument = codeDocument;
    }

    internal static IDocumentSnapshot Create(string filePath, RazorCodeDocument codeDocument)
    {
        return new TestDocumentSnapshot(filePath, codeDocument);
    }

    public ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken)
    {
        return new(_codeDocument);
    }

    public ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        return new(_codeDocument.Source.Text);
    }

    public ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        return new(CSharpSyntaxTree.ParseText(_codeDocument.GetCSharpSourceText(), cancellationToken: cancellationToken));
    }

    public bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        result = _codeDocument;

        return result is not null;
    }

    public bool TryGetText([NotNullWhen(true)] out SourceText? result)
    {
        result = _codeDocument.Source.Text;
        return result is not null;
    }

    public bool TryGetTextVersion(out VersionStamp result)
        => throw new NotImplementedException();

    public IDocumentSnapshot WithText(SourceText text)
        => throw new NotImplementedException();
}
