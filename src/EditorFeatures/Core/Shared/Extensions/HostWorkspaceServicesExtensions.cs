// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class HostWorkspaceServicesExtensions
    {
        public static HostLanguageServices GetLanguageServices(
            this HostWorkspaceServices workspaceServices, ITextBuffer textBuffer)
        {
            return workspaceServices.GetLanguageServices(textBuffer.ContentType);
        }

        public static HostLanguageServices GetLanguageServices(
            this HostWorkspaceServices workspaceServices, IContentType contentType)
        {
            foreach (var language in workspaceServices.SupportedLanguages)
            {
                if (LanguageMatches(language, contentType, workspaceServices))
                {
                    return workspaceServices.GetLanguageServices(language);
                }
            }

            return null;
        }

        /// <summary>
        /// A cache of host services -> (language name -> content type name).
        /// </summary>
        private static readonly ConditionalWeakTable<HostWorkspaceServices, Dictionary<string, string>> s_hostServicesToContentTypeMap
            = new ConditionalWeakTable<HostWorkspaceServices, Dictionary<string, string>>();

        private static string GetDefaultContentTypeName(HostWorkspaceServices workspaceServices, string language)
        {
            Dictionary<string, string> contentTypeMap;
            if (!s_hostServicesToContentTypeMap.TryGetValue(workspaceServices, out contentTypeMap))
            {
                contentTypeMap = s_hostServicesToContentTypeMap.GetValue(workspaceServices, CreateContentTypeMap);
            }

            string contentTypeName;
            contentTypeMap.TryGetValue(language, out contentTypeName);
            return contentTypeName;
        }

        private static Dictionary<string, string> CreateContentTypeMap(HostWorkspaceServices hostWorkspaceServices)
        {
            // Are we being hosted in a MEF host? If so, we can get content type information directly from the 
            // metadata and avoid actually loading the assemblies
            var mefHostServices = (IMefHostExportProvider)hostWorkspaceServices.HostServices;

            if (mefHostServices != null)
            {
                var exports = mefHostServices.GetExports<ILanguageService, ContentTypeLanguageMetadata>();
                return exports
                        .Where(lz => !string.IsNullOrEmpty(lz.Metadata.DefaultContentType))
                        .ToDictionary(lz => lz.Metadata.Language, lz => lz.Metadata.DefaultContentType);
            }

            // We can't do anything special, so fall back to the expensive path
            return hostWorkspaceServices.SupportedLanguages.ToDictionary(
                l => l,
                l => hostWorkspaceServices.GetLanguageServices(l).GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType().TypeName);
        }

        public static IList<Lazy<T, TMetadata>> SelectMatchingExtensions<T, TMetadata>(
            this HostWorkspaceServices workspaceServices,
            IEnumerable<Lazy<T, TMetadata>> items,
            ITextBuffer textBuffer)
            where TMetadata : ILanguageMetadata
        {
            return workspaceServices.SelectMatchingExtensions(items, textBuffer.ContentType);
        }

        public static IList<T> SelectMatchingExtensionValues<T, TMetadata>(
            this HostWorkspaceServices workspaceServices,
            IEnumerable<Lazy<T, TMetadata>> items,
            ITextBuffer textBuffer)
            where TMetadata : ILanguageMetadata
        {
            return SelectMatchingExtensions(workspaceServices, items, textBuffer).Select(lazy => lazy.Value).ToList();
        }

        public static IList<Lazy<T, TMetadata>> SelectMatchingExtensions<T, TMetadata>(
            this HostWorkspaceServices workspaceServices,
            IEnumerable<Lazy<T, TMetadata>> items,
            IContentType contentType)
            where TMetadata : ILanguageMetadata
        {
            if (items == null)
            {
                return SpecializedCollections.EmptyList<Lazy<T, TMetadata>>();
            }

            return items.Where(lazy => LanguageMatches(lazy.Metadata.Language, contentType, workspaceServices)).ToList();
        }

        private static bool LanguageMatches(
            string language,
            IContentType contentType,
            HostWorkspaceServices workspaceServices)
        {
            var defaultContentType = GetDefaultContentTypeName(workspaceServices, language);
            if (defaultContentType != null)
            {
                return contentType.IsOfType(defaultContentType);
            }
            else
            {
                return false;
            }
        }
    }
}
