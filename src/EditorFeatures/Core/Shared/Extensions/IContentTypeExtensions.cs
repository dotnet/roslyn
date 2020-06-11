// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => extensionContentTypes.Any(v => dataContentType.IsOfType(v));

        public static bool MatchesAny(this IContentType dataContentType, params string[] extensionContentTypes)
            => dataContentType.MatchesAny((IEnumerable<string>)extensionContentTypes);
    }
}
