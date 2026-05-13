// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

/// <summary>
/// An <see cref="IRazorEngineFeature"/> that determines whether a given base type
/// supports calling <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> for UTF-8 HTML literal emission.
/// </summary>
internal interface IUtf8WriteLiteralFeature : IRazorEngineFeature
{
    /// <summary>
    /// Returns <see langword="true"/> if the base type used by the specified file has a callable
    /// <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> overload.
    /// </summary>
    /// <param name="filePath">The file path of the Razor document.</param>
    /// <param name="baseTypeName">The raw <c>@inherits</c> value from the document.</param>
    bool IsSupported(string? filePath, string baseTypeName);
}
