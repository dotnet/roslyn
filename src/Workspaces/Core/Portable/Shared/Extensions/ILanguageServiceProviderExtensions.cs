// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ILanguageServiceProviderExtensions
{
    public static IEnumerable<Lazy<T, TMetadata>> SelectMatchingExtensions<T, TMetadata>(
        this HostLanguageServices serviceProvider,
        IEnumerable<Lazy<T, TMetadata>>? items)
        where TMetadata : ILanguageMetadata
    {
        if (items == null)
            return [];

        return items.Where(lazy => lazy.Metadata.Language == serviceProvider.Language);
    }
}
