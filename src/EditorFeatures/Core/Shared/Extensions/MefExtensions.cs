// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

/// <summary>
/// Helper class to perform ContentType best-match against a set of extensions. This could
/// become a public service.
/// </summary>
internal static class MefExtensions
{
    extension<TExtension, TMetadata>(IEnumerable<Lazy<TExtension, TMetadata>> extensions) where TMetadata : IContentTypeMetadata
    {
        /// <summary>
        /// Given a list of extensions that provide content types, filter the list and return that
        /// subset which matches the given content type
        /// </summary>
        public IList<Lazy<TExtension, TMetadata>> SelectMatchingExtensions(
            params IContentType[] contentTypes)
        {
            return extensions.SelectMatchingExtensions((IEnumerable<IContentType>)contentTypes);
        }

        /// <summary>
        /// Given a list of extensions that provide content types, filter the list and return that
        /// subset which matches any of the given content types.
        /// </summary>
        public IList<Lazy<TExtension, TMetadata>> SelectMatchingExtensions(
            IEnumerable<IContentType> contentTypes)
        {
            return [.. extensions.Where(h => contentTypes.Any(d => d.MatchesAny(h.Metadata.ContentTypes)))];
        }

        public IList<TExtension> SelectMatchingExtensionValues(
            params IContentType[] contentTypes)
        {
            return [.. extensions.SelectMatchingExtensions(contentTypes).Select(p => p.Value)];
        }

        public Lazy<TExtension, TMetadata> SelectMatchingExtension(
            params IContentType[] contentTypes)
        {
            return extensions.SelectMatchingExtensions(contentTypes).Single();
        }

        public TExtension SelectMatchingExtensionValue(
            params IContentType[] contentTypes)
        {
            return extensions.SelectMatchingExtension(contentTypes).Value;
        }
    }
}
