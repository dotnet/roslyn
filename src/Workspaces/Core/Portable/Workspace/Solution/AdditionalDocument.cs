// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents an additional file passed down to analyzers.
/// </summary>
public sealed class AdditionalDocument : TextDocument
{
    internal AdditionalDocument(Project project, TextDocumentState state)
        : base(project, state, TextDocumentKind.AdditionalDocument)
    {
    }
}
