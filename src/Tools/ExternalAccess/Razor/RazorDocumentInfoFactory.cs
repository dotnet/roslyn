// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class RazorDocumentInfoFactory
    {
        public static DocumentInfo Create(
            DocumentId id,
            string name,
            IEnumerable<string>? folders,
            SourceCodeKind sourceCodeKind,
            TextLoader? loader,
            string? filePath,
            bool isGenerated,
            RazorDocumentServiceProviderWrapper documentServiceProvider)
        {
            return DocumentInfo.Create(id, name, folders.ToBoxedImmutableArray(), sourceCodeKind, loader, filePath, isGenerated, documentServiceProvider);
        }
    }
}
