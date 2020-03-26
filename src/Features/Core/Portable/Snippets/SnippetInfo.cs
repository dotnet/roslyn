// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Snippets
{
    internal sealed class SnippetInfo
    {
        public string Shortcut { get; }
        public string Title { get; }
        public string Description { get; }
        public string Path { get; }

        public SnippetInfo(string shortcut, string title, string description, string path)
        {
            Shortcut = shortcut;
            Title = title;
            Description = description;
            Path = path;
        }
    }
}
