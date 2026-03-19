// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Defines whether the insert text in a completion item should be
/// interpreted as plain text or as a snippet.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#insertTextFormat">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal enum InsertTextFormat
{
    /// <summary>
    /// The primary text to be inserted is treated as a plain string.
    /// </summary>
    Plaintext = 1,

    /// <summary>
    /// The primary text to be inserted is treated as a snippet.
    /// <para>
    /// A snippet can define tab stops and placeholders with <c>$1</c>, <c>$2</c>
    /// and <c>${3:foo}</c>. <c>$0</c> defines the final tab stop and defaults to
    /// the end of the snippet. Placeholders with equal identifiers are
    /// linked, such that typing in one will update others too.
    /// </para>
    /// </summary>
    Snippet = 2,
}
