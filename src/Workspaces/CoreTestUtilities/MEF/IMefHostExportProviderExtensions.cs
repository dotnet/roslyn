// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal static class IMefHostExportProviderExtensions
    {
        public static TExtension GetExportedValue<TExtension>(this IMefHostExportProvider provider)
            => provider.GetExports<TExtension>().Single().Value;

        public static IEnumerable<TExtension> GetExportedValues<TExtension>(this IMefHostExportProvider provider)
            => provider.GetExports<TExtension>().Select(l => l.Value);
    }
}
