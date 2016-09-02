// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
{
#if false
    internal static class QuickInfoPresentationProviderExtensions
    {
        public static bool TryGetValue(this ImmutableArray<Lazy<QuickInfoPresentationProvider, QuickInfoPresentationProviderInfo>> lazyProviders, 
            string kind, out QuickInfoPresentationProvider provider)
        {
            foreach (var p in lazyProviders)
            {
                if (p.Metadata.Kinds != null && p.Metadata.Kinds.Contains(kind))
                {
                    provider = p.Value;
                    return true;
                }
            }

            provider = null;
            return false;
        }
    }
#endif
}