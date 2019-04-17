// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    /// <summary>
    /// Tagged text for a completion item.
    /// This is an implementation detail of the server that is passed to the clients
    /// and returned back without the clients parsing it, so no need to make it public.
    /// </summary>
    internal class RoslynTaggedText
    {
        public string Tag { get; set; }
        public string Text { get; set; }

        public TaggedText ToTaggedText() => new TaggedText(Tag, Text);
    }
}
