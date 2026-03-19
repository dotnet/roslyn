// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Enum values for inlay hint kinds.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal enum InlayHintKind
{
    /// <summary>
    /// An inlay hint that for a type annotation
    /// </summary>
    Type = 1,

    /// <summary>
    /// An inlay hint that is for a parameter
    /// </summary>
    Parameter = 2,
}
