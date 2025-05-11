// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

/// <summary>
/// The LSP modifiers from <see cref="Roslyn.LanguageServer.Protocol.SemanticTokenModifiers"/>
/// Roslyn currently supports. Enum is used to signify the modifier(s) that apply to a given token.
/// </summary>
[Flags]
internal enum TokenModifiers
{
    None = 0,
    Static = 1,
    ReassignedVariable = 2,
    Deprecated = 4,
}
