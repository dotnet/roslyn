// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
