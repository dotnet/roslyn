// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IContentTypeExtensions
    {
        /// <summary>
        /// Test whether an extension matches a content type.
        /// </summary>
        /// <param name="dataContentType">Content type (typically of a text buffer) against which to
        /// match an extension.</param>
        /// <param name="extensionContentTypes">Content types from extension metadata.</param>
        public static bool MatchesAny(this IContentType dataContentType, IEnumerable<string> extensionContentTypes)
        {
            return extensionContentTypes.Any(v => dataContentType.IsOfType(v));
        }

        public static bool MatchesAny(this IContentType dataContentType, params string[] extensionContentTypes)
        {
            return dataContentType.MatchesAny((IEnumerable<string>)extensionContentTypes);
        }
    }
}
