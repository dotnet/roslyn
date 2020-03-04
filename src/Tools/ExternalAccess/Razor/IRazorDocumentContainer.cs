// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal interface IRazorDocumentContainer
    {
        public string FilePath { get; }

        public TextLoader GetTextLoader(string filePath);

        public IRazorSpanMappingService GetMappingService();

        public IRazorDocumentExcerptService GetExcerptService();

    }
}
