// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDiscardPattern
    {
        public DiscardSymbol DiscardSymbol
        {
            get
            {
                // PROTOTYPE(patterns2): should cache the symbol so it always returns the same one.
                Debug.Assert((object)this.InputType != null);
                return new DiscardSymbol(this.InputType);
            }
        }
    }
}
