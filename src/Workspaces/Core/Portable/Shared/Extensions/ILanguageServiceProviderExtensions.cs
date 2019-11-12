// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ILanguageServiceProviderExtensions
    {
        public static IEnumerable<Lazy<T, TMetadata>> SelectMatchingExtensions<T, TMetadata>(
            this HostLanguageServices serviceProvider,
            IEnumerable<Lazy<T, TMetadata>>? items)
            where TMetadata : ILanguageMetadata
        {
            if (items == null)
            {
                return SpecializedCollections.EmptyEnumerable<Lazy<T, TMetadata>>();
            }

            return items.Where(lazy => lazy.Metadata.Language == serviceProvider.Language);
        }
    }
}
