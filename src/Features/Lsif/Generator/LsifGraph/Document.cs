// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
