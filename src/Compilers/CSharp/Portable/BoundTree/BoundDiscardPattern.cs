// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDiscardPattern
    {
        private DiscardSymbol _discardSymbol;

        public DiscardSymbol DiscardSymbol
        {
            get
            {
                if (_discardSymbol is null)
                {
                    Debug.Assert(!(this.InputType is null));
                    _discardSymbol = new DiscardSymbol(this.InputType);
                }

                return _discardSymbol;
            }
        }
    }
}
