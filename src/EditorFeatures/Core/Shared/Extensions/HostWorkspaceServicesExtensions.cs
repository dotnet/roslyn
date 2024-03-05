// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class HostWorkspaceServicesExtensions
    {
        public static CodeAnalysis.Host.LanguageServices? GetProjectServices(
            this SolutionServices workspaceServices, ITextBuffer textBuffer)
        {
            return workspaceServices.GetProjectServices(textBuffer.ContentType);
        }

        public static CodeAnalysis.Host.LanguageServices? GetProjectServices(
            this SolutionServices workspaceServices, IContentType contentType)
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
        /// Returns the name of the language (see <see cref="LanguageNames"/>) associated with the specified buffer. 
        /// </summary>
        internal static string? GetLanguageName(this ITextBuffer buffer)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var workspace) ?
               workspace.Services.SolutionServices.GetProjectServices(buffer.ContentType)?.Language : null;

        /// <summary>
        /// A cache of host services -> (language name -> content type name).
        /// </summary>
        private static readonly ConditionalWeakTable<SolutionServices, Dictionary<string, string>> s_hostServicesToContentTypeMap = new();

        private static string? GetDefaultContentTypeName(SolutionServices workspaceServices, string language)
        {
            if (!s_hostServicesToContentTypeMap.TryGetValue(workspaceServices, out var contentTypeMap))
            {
                contentTypeMap = s_hostServicesToContentTypeMap.GetValue(workspaceServices, CreateContentTypeMap);
            }

            contentTypeMap.TryGetValue(language, out var contentTypeName);
            return contentTypeName;
        }

        private static Dictionary<string, string> CreateContentTypeMap(SolutionServices hostWorkspaceServices)
        {
            // Are we being hosted in a MEF host? If so, we can get content type information directly from the 
            // metadata and avoid actually loading the assemblies
            var mefHostServices = hostWorkspaceServices.ExportProvider;

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
                        .ToDictionary(lz => lz.Language, lz => lz.DefaultContentType)!;
            }

            // We can't do anything special, so fall back to the expensive path
            return hostWorkspaceServices.SupportedLanguages.ToDictionary(
                l => l,
                l => hostWorkspaceServices.GetLanguageServices(l).GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType().TypeName);
        }

        internal static IList<T> SelectMatchingExtensionValues<T, TMetadata>(
            this SolutionServices workspaceServices,
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

        private static bool LanguageMatches(
            string language,
            IContentType contentType,
            SolutionServices workspaceServices)
        {
            var defaultContentType = GetDefaultContentTypeName(workspaceServices, language);
            return (defaultContentType != null) ? contentType.IsOfType(defaultContentType) : false;
        }
    }
}
