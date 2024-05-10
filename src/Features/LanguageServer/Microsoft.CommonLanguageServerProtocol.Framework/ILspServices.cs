// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal interface ILspServices : IDisposable
{
    T? GetService<T>() where T : notnull;
    T GetRequiredService<T>() where T : notnull;

    object? TryGetService(Type @type);

    IEnumerable<T> GetRequiredServices<T>();
}
