// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A notebook cell kind.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookCellKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal enum NotebookCellKind
{
    /// <summary>
    /// A markup-cell is formatted source that is used for display.
    /// </summary>
    Markup = 1,

    /// <summary>
    /// A code-cell is source code.
    /// </summary>
    Code = 2
}
