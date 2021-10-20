// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal sealed record SourceDocument
    {
        public readonly string FilePath;
        public readonly SourceText? EmbeddedText;

        public SourceDocument(string filePath, SourceText? embeddedText)
        {
            FilePath = filePath;
            EmbeddedText = embeddedText;
        }
    }
}
