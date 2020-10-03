// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptTextExtensions
    {
        public static IEnumerable<Document> GetRelatedDocuments(this SourceTextContainer container)
            => TextExtensions.GetRelatedDocuments(container);

        public static Document? GetOpenDocumentInCurrentContextWithChanges(this SourceText text)
            => TextExtensions.GetOpenDocumentInCurrentContextWithChanges(text);
    }
}
