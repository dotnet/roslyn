// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    // TODO - This should be deleted when liveshare moves to VSCompletionItem.
    // https://github.com/dotnet/roslyn/projects/45#card-21249665
    internal class RoslynTaggedText
    {
        public string Tag { get; set; }
        public string Text { get; set; }

        public TaggedText ToTaggedText() => new TaggedText(Tag, Text);
    }
}
