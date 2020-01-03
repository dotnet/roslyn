// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ILanguageMetadataExtensions
    {
        [return: MaybeNull]
        public static TInterface ToSpecificLanguage<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services, string languageName)
            where TMetadata : ILanguageMetadata
        {
            return services.Where(s => s.Metadata.Language == languageName).Select(s => s.Value).FirstOrDefault();
        }

        public static IEnumerable<TInterface> FilterToSpecificLanguage<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services, string languageName)
            where TMetadata : ILanguageMetadata
        {
            return services.Where(s => s.Metadata.Language == languageName).Select(s => s.Value);
        }

        public static Dictionary<string, List<Lazy<TInterface, TMetadata>>> ToPerLanguageMap<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services)
            where TMetadata : ILanguageMetadata
        {
            var map = new Dictionary<string, List<Lazy<TInterface, TMetadata>>>();

            foreach (var service in services)
            {
                var list = map.GetOrAdd(service.Metadata.Language, _ => new List<Lazy<TInterface, TMetadata>>());
                list.Add(service);
            }

            return map;
        }

        public static Dictionary<string, List<Lazy<TInterface, TMetadata>>> ToPerLanguageMapWithMultipleLanguages<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services)
            where TMetadata : ILanguagesMetadata
        {
            var map = new Dictionary<string, List<Lazy<TInterface, TMetadata>>>();

            foreach (var service in services)
            {
                foreach (var language in service.Metadata.Languages.Distinct())
                {
                    if (!string.IsNullOrEmpty(language))
                    {
                        var list = map.GetOrAdd(language, _ => new List<Lazy<TInterface, TMetadata>>());
                        list.Add(service);
                    }
                }
            }

            return map;
        }
    }
}
