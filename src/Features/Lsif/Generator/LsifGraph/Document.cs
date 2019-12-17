// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    internal sealed class Document : Vertex
    {
        public Uri Uri { get; }
        public string LanguageId { get; }
        public string? Contents { get; }

        public Document(Uri uri, string languageId, string? contents = null)
            : base(label: "document")
        {
            this.Uri = uri;
            this.LanguageId = languageId;
            this.Contents = contents;
        }
    }
}
