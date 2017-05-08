// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal interface IRemoteFindUsages
    {
        Task FindImplementationsAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg);
        Task FindSymbolUsagesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg);
        Task FindLiteralUsagesAsync(string title, object value);
    }
}