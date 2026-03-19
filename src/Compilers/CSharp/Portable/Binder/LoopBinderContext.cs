// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LoopBinder : LocalScopeBinder
    {
        private readonly GeneratedLabelSymbol _breakLabel;
        private readonly GeneratedLabelSymbol _continueLabel;

        protected LoopBinder(Binder enclosing)
            : base(enclosing)
        {
            _breakLabel = new GeneratedLabelSymbol("break");
            _continueLabel = new GeneratedLabelSymbol("continue");
        }

        internal override GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return _breakLabel;
            }
        }

        internal override GeneratedLabelSymbol ContinueLabel
        {
            get
            {
                return _continueLabel;
            }
        }
    }
}
