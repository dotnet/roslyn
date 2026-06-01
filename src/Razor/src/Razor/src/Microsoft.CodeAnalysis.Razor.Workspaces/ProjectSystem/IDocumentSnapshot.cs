// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface IDocumentSnapshot
{
    RazorFileKind FileKind { get; }
    string FilePath { get; }
    string TargetPath { get; }
    IProjectSnapshot Project { get; }

    int Version { get; }

    ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken);
    ValueTask<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken);
    ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken);

    /// <summary>
    ///  Gets the Roslyn syntax tree for the generated C# for this Razor document
    /// </summary>
    /// <remarks>
    ///  ⚠️ Should be used sparingly in language server scenarios.
    /// </remarks>
    ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the decl-half source generated document for this Razor document, or
    /// <see langword="null"/> when the source generator did not emit a decl half (e.g.
    /// .cshtml files, non-component documents, or components whose primary method body is
    /// suppressed).
    /// </summary>
    /// <remarks>
    /// Implementations that do not have access to source-generator output (e.g. test
    /// doubles) should return <see langword="null"/>.
    /// </remarks>
    ValueTask<SourceGeneratedDocument?> TryGetDeclGeneratedDocumentAsync(CancellationToken cancellationToken);

    bool TryGetText([NotNullWhen(true)] out SourceText? result);
    bool TryGetTextVersion(out VersionStamp result);
    bool TryGetGeneratedOutput([NotNullWhen(true)] out RazorCodeDocument? result);

    IDocumentSnapshot WithText(SourceText text);
}
