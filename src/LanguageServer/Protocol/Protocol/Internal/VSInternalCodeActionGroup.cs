// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Enum which represents the various kinds of code actions.
/// </summary>
internal static class VSInternalCodeActionGroup
{
    /// <summary>
    /// Code action is a quick fix.
    /// </summary>
    public const string QuickFix = "quickfix";

    /// <summary>
    /// Code action is a refactor.
    /// </summary>
    public const string Refactor = "refactor";

    /// <summary>
    /// Code action is a refactor for extracting methods, functions, variables, etc.
    /// </summary>
    public const string RefactorExtract = "refactor.extract";

    /// <summary>
    /// Code action is a refactor for inlining methods, constants, etc.
    /// </summary>
    public const string RefactorInline = "refactor.inline";

    /// <summary>
    /// Code action is a refactor for rewrite actions, such as making methods static.
    /// </summary>
    public const string RefactorRewrite = "refactor.rewrite";

    /// <summary>
    /// Code action applies to the entire file.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Code actions is for organizing imports.
    /// </summary>
    public const string SourceOrganizeImports = "source.organizeImports";
}
