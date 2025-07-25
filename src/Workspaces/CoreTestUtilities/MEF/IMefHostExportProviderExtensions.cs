// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class IMefHostExportProviderExtensions
{
    extension(IMefHostExportProvider provider)
    {
        public TExtension GetExportedValue<TExtension>()
        => provider.GetExports<TExtension>().Single().Value;

        public IEnumerable<TExtension> GetExportedValues<TExtension>()
            => provider.GetExports<TExtension>().Select(l => l.Value);
    }
}
