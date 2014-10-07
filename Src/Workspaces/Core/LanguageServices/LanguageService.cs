// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// A service for accessing other language specific services.
    /// </summary>
    internal static class LanguageService
    {
        public static IEnumerable<string> GetSupportedLanguages(Workspace workspace)
        {
            var factory = WorkspaceServices.WorkspaceService.GetService<ILanguageServiceProviderFactory>(workspace);
            if (factory != null)
            {
                return factory.SupportedLanguages;
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<string>();
            }
        }

        public static ILanguageServiceProvider GetProvider(Project project)
        {
            return project.GetLanguageServiceProviderInternal();
        }

        public static ILanguageServiceProvider GetProvider(Document document)
        {
            return document.Project.GetLanguageServiceProviderInternal();
        }

        public static ILanguageServiceProvider GetProvider(Workspace workspace, string language)
        {
            var factory = WorkspaceServices.WorkspaceService.GetService<ILanguageServiceProviderFactory>(workspace);
            if (factory != null)
            {
                return factory.GetLanguageServiceProvider(language);
            }

            return null;
        }

        public static TService GetService<TService>(Project project) where TService : class, ILanguageService
        {
            return GetProvider(project).GetService<TService>();
        }

        public static TService GetService<TService>(Document document) where TService : class, ILanguageService
        {
            return GetProvider(document.Project).GetService<TService>();
        }

        public static TService GetService<TService>(Workspace workspace, string language) where TService : class, ILanguageService
        {
            var provider = GetProvider(workspace, language);
            if (provider != null)
            {
                return provider.GetService<TService>();
            }

            return null;
        }

        public static IEnumerable<TService> GetServices<TService>(Workspace workspace) where TService : class, ILanguageService
        {
            var factory = WorkspaceServices.WorkspaceService.GetService<ILanguageServiceProviderFactory>(workspace);
            if (factory != null)
            {
                return factory.GetServices<TService>();
            }

            return SpecializedCollections.EmptyEnumerable<TService>();
        }
    }
}
