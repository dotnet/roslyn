// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
