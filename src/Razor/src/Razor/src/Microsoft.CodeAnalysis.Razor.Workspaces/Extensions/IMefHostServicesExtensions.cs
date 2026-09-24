// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis;

internal static class IMefHostExportProviderExtensions
{
    public static TService GetService<TService>(this IMefHostExportProvider exportProvider)
    {
        return exportProvider.GetExports<TService>().First().Value;
    }
}
