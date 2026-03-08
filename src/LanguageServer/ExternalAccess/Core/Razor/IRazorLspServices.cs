// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor.Features;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
#endif

internal interface IRazorLspServices
{
    T? GetService<T>() where T : notnull;
    T GetRequiredService<T>() where T : notnull;

    bool TryGetService(Type type, [NotNullWhen(true)] out object? service);

    IEnumerable<T> GetRequiredServices<T>();
}
