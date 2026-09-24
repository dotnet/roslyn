// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

internal interface IFSharpNavigationBarItemService
{
    Task<IList<FSharpNavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken);
}
