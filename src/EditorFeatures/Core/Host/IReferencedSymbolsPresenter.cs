// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IReferencedSymbolsPresenter
    {
        void DisplayResult(Solution solution, IEnumerable<ReferencedSymbol> result);
    }
}
