// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Collections.Generic;

/// <summary>
/// Well-known semantic token modifiers.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokenModifiers">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal static class SemanticTokenModifiers
{
    /// <summary>
    /// Semantic token modifier for 'declaration'.
    /// </summary>
    public const string Declaration = "declaration";

    /// <summary>
    /// Semantic token modifier for 'definition'.
    /// </summary>
    public const string Definition = "definition";

    /// <summary>
    /// Semantic token modifier for 'readonly'.
    /// </summary>
    public const string Readonly = "readonly";

    /// <summary>
    /// Semantic token modifier for 'static'.
    /// </summary>
    public const string Static = "static";

    /// <summary>
    /// Semantic token modifier for 'deprecated'.
    /// </summary>
    public const string Deprecated = "deprecated";

    /// <summary>
    /// Semantic token modifier for 'abstract'.
    /// </summary>
    public const string Abstract = "abstract";

    /// <summary>
    /// Semantic token modifier for 'async'.
    /// </summary>
    public const string Async = "async";

    /// <summary>
    /// Semantic token modifier for 'modification'.
    /// </summary>
    public const string Modification = "modification";

    /// <summary>
    /// Semantic token modifier for 'documentation'.
    /// </summary>
    public const string Documentation = "documentation";

    /// <summary>
    /// Semantic token modifier for 'defaultLibrary'.
    /// </summary>
    public const string DefaultLibrary = "defaultLibrary";

    /// <summary>
    /// Collection containing all well-known semantic tokens modifiers.
    /// </summary>
    public static readonly IReadOnlyList<string> AllModifiers =
    [
        Declaration,
        Definition,
        Readonly,
        Static,
        Deprecated,
        Abstract,
        Async,
        Modification,
        Documentation,
        DefaultLibrary,
    ];
}
