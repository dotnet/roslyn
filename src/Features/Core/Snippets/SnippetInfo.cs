// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Snippets
{
    internal sealed class SnippetInfo
    {
        public string Shortcut { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Path { get; private set; }

        public SnippetInfo(string shortcut, string title, string description, string path)
        {
            this.Shortcut = shortcut;
            this.Title = title;
            this.Description = description;
            this.Path = path;
        }
    }
}
