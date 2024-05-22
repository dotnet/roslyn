// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ILanguageMetadataExtensions
{
    public static TInterface? ToSpecificLanguage<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services, string languageName)
        where TMetadata : ILanguageMetadata
    {
        return services.Where(s => s.Metadata.Language == languageName).Select(s => s.Value).FirstOrDefault();
    }

    public static IEnumerable<TInterface> FilterToSpecificLanguage<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services, string languageName)
        where TMetadata : ILanguageMetadata
    {
        return services.Where(s => s.Metadata.Language == languageName).Select(s => s.Value);
    }

    public static ImmutableDictionary<string, ImmutableArray<Lazy<TInterface, TMetadata>>> ToPerLanguageMap<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services)
        where TMetadata : ILanguageMetadata
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ArrayBuilder<Lazy<TInterface, TMetadata>>>();

        foreach (var service in services)
        {
            var language = service.Metadata?.Language ?? string.Empty;
            var list = builder.GetOrAdd(language, _ => ArrayBuilder<Lazy<TInterface, TMetadata>>.GetInstance());
            list.Add(service);
        }

        return builder.Select(kvp => new KeyValuePair<string, ImmutableArray<Lazy<TInterface, TMetadata>>>(kvp.Key, kvp.Value.ToImmutableAndFree())).ToImmutableDictionary();
    }

    public static ImmutableDictionary<string, ImmutableArray<Lazy<TInterface, TMetadata>>> ToPerLanguageMapWithMultipleLanguages<TInterface, TMetadata>(this IEnumerable<Lazy<TInterface, TMetadata>> services)
        where TMetadata : ILanguagesMetadata
    {
        using var _ = PooledDictionary<string, ArrayBuilder<Lazy<TInterface, TMetadata>>>.GetInstance(out var map);

        foreach (var service in services)
        {
            foreach (var language in service.Metadata.Languages.Distinct())
            {
                if (!string.IsNullOrEmpty(language))
                {
                    var list = map.GetOrAdd(language, _ => ArrayBuilder<Lazy<TInterface, TMetadata>>.GetInstance());
                    list.Add(service);
                }
            }
        }

        return map.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree());
    }
}
