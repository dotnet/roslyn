// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
            if (!s_hostServicesToContentTypeMap.TryGetValue(workspaceServices, out var contentTypeMap))
            {
                contentTypeMap = s_hostServicesToContentTypeMap.GetValue(workspaceServices, CreateContentTypeMap);
            }

            contentTypeMap.TryGetValue(language, out var contentTypeName);
            return contentTypeName;
        }

        private static Dictionary<string, string> CreateContentTypeMap(HostWorkspaceServices hostWorkspaceServices)
        {
            // Are we being hosted in a MEF host? If so, we can get content type information directly from the 
            // metadata and avoid actually loading the assemblies
            var mefHostServices = (IMefHostExportProvider)hostWorkspaceServices.HostServices;

            if (mefHostServices != null)
            {
                // Two assemblies may export the same language to content type mapping during development cycles where
                // a type is moving to a new assembly. Avoid failing during content type discovery by de-duplicating
                // services with identical metadata, opting to instead fail only in cases where the impacted service
                // instance is used.
                var exports = mefHostServices.GetExports<ILanguageService, ContentTypeLanguageMetadata>();
                return exports
                        .Where(lz => !string.IsNullOrEmpty(lz.Metadata.DefaultContentType))
                        .Select(lz => (lz.Metadata.Language, lz.Metadata.DefaultContentType))
                        .Distinct()
                        .ToDictionary(lz => lz.Language, lz => lz.DefaultContentType);
            }

            // We can't do anything special, so fall back to the expensive path
            return hostWorkspaceServices.SupportedLanguages.ToDictionary(
                l => l,
                l => hostWorkspaceServices.GetLanguageServices(l).GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType().TypeName);
        }

        internal static IList<T> SelectMatchingExtensionValues<T, TMetadata>(
            this HostWorkspaceServices workspaceServices,
            IEnumerable<Lazy<T, TMetadata>> items,
            IContentType contentType)
            where TMetadata : ILanguageMetadata
        {
            if (items == null)
            {
                return SpecializedCollections.EmptyList<T>();
            }

            return items.Where(lazy => LanguageMatches(lazy.Metadata.Language, contentType, workspaceServices)).
                Select(lazy => lazy.Value).ToList();
        }

        internal static IList<T> SelectMatchingExtensionValues<T>(
            this HostWorkspaceServices workspaceServices,
            IEnumerable<Lazy<T, OrderableLanguageAndRoleMetadata>> items,
            IContentType contentType,
            ITextViewRoleSet roleSet)
        {
            if (items == null)
            {
                return SpecializedCollections.EmptyList<T>();
            }

            return items.Where(lazy =>
                {
                    var metadata = lazy.Metadata;
                    return LanguageMatches(metadata.Language, contentType, workspaceServices) &&
                        RolesMatch(metadata.Roles, roleSet);
                }).
                Select(lazy => lazy.Value).ToList();
        }

        private static bool LanguageMatches(
            string language,
            IContentType contentType,
            HostWorkspaceServices workspaceServices)
        {
            var defaultContentType = GetDefaultContentTypeName(workspaceServices, language);
            return (defaultContentType != null) ? contentType.IsOfType(defaultContentType) : false;
        }

        private static bool RolesMatch(
            IEnumerable<string> roles,
            ITextViewRoleSet roleSet)
        {
            return (roles == null) || (roleSet == null) || roleSet.ContainsAll(roles);
        }
    }
}
