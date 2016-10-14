// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IDatabaseFactoryService
    {
        AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes);
    }
}
