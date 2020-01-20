// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal interface ICodingConventionContext
    {
        ICodingConventionsSnapshot CurrentConventions
        {
            get;
        }

        event CodingConventionsChangedAsyncEventHandler CodingConventionsChangedAsync;

        Task WriteConventionValueAsync(string conventionName, string conventionValue, CancellationToken cancellationToken);
    }
}
