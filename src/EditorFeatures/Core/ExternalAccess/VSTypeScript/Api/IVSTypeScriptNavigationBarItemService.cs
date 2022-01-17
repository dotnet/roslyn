﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptNavigationBarItemService
    {
        Task<ImmutableArray<VSTypescriptNavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken);
    }
}
