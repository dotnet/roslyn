// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
    internal class MockReferencedSymbolsPresenter : IReferencedSymbolsPresenter
    {
        public Solution Solution { get; private set; }
        public IEnumerable<ReferencedSymbol> Result { get; private set; }

        public void DisplayResult(Solution solution, IEnumerable<ReferencedSymbol> result)
        {
            this.Solution = solution;
            this.Result = result;
        }
    }
}
