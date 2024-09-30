// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Snippets;

/// <summary>
/// Contains language-neutral snippet identifiers,
/// which can theoretically be used in a snippet provider regardless its target language
/// </summary>
internal static class CommonSnippetIdentifiers
{
    public const string ConsoleWriteLine = "cw";
    public const string Constructor = "ctor";
    public const string Property = "prop";
    public const string RequiredProperty = "propr";
    public const string GetOnlyProperty = "propg";
}
