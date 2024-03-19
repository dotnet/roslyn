// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Snippets;

internal sealed class SnippetInfo(string shortcut, string title, string description, string path)
{
    public string Shortcut { get; } = shortcut;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public string Path { get; } = path;
}
