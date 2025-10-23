// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
/// </summary>
internal readonly record struct DocumentSpan(
    Document Document, TextSpan SourceSpan, bool IsGeneratedCode)
{
    public DocumentSpan(Document document, TextSpan sourceSpan)
        : this(document, sourceSpan, IsGeneratedCode: false)
    {
    }
}
