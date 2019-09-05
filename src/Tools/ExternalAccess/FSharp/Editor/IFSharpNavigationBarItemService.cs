// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    internal interface IFSharpNavigationBarItemService
    {
        Task<IList<FSharpNavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken);
    }
}
