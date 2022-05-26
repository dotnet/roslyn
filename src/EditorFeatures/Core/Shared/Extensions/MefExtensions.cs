// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    /// <summary>
    /// Helper class to perform ContentType best-match against a set of extensions. This could
    /// become a public service.
    /// </summary>
    internal static class MefExtensions
    {
        /// <summary>
        /// Given a list of extensions that provide content types, filter the list and return that
        /// subset which matches the given content type
        /// </summary>
        public static IList<Lazy<TExtension, TMetadata>> SelectMatchingExtensions<TExtension, TMetadata>(
            this IEnumerable<Lazy<TExtension, TMetadata>> extensions,
            params IContentType[] contentTypes)
            where TMetadata : IContentTypeMetadata
        {
            return extensions.SelectMatchingExtensions((IEnumerable<IContentType>)contentTypes);
        }

        /// <summary>
        /// Given a list of extensions that provide content types, filter the list and return that
        /// subset which matches any of the given content types.
        /// </summary>
        public static IList<Lazy<TExtension, TMetadata>> SelectMatchingExtensions<TExtension, TMetadata>(
            this IEnumerable<Lazy<TExtension, TMetadata>> extensions,
            IEnumerable<IContentType> contentTypes)
            where TMetadata : IContentTypeMetadata
        {
            return extensions.Where(h => contentTypes.Any(d => d.MatchesAny(h.Metadata.ContentTypes))).ToList();
        }

        public static IList<TExtension> SelectMatchingExtensionValues<TExtension, TMetadata>(
            this IEnumerable<Lazy<TExtension, TMetadata>> extensions,
            params IContentType[] contentTypes)
            where TMetadata : IContentTypeMetadata
        {
            return extensions.SelectMatchingExtensions(contentTypes).Select(p => p.Value).ToList();
        }

        public static Lazy<TExtension, TMetadata> SelectMatchingExtension<TExtension, TMetadata>(
            this IEnumerable<Lazy<TExtension, TMetadata>> extensions,
            params IContentType[] contentTypes)
            where TMetadata : IContentTypeMetadata
        {
            return extensions.SelectMatchingExtensions(contentTypes).Single();
        }

        public static TExtension SelectMatchingExtensionValue<TExtension, TMetadata>(
            this IEnumerable<Lazy<TExtension, TMetadata>> extensions,
            params IContentType[] contentTypes)
            where TMetadata : IContentTypeMetadata
        {
            return extensions.SelectMatchingExtension(contentTypes).Value;
        }
    }
}
