// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Symbol tags are extra annotations that tweak the rendering of a symbol.
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal enum SymbolTag
{
    /// <summary>
    /// Render a symbol as obsolete, usually using a strike-out.
    /// </summary>
    Deprecated = 1
}
