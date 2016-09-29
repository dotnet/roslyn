// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal interface IAssemblySymbolInternal : IAssemblySymbol
    {
        Version AssemblyVersionPattern { get; }
    }
}
