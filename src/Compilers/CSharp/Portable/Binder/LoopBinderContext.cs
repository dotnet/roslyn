// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LoopBinder : LocalScopeBinder
    {
        private readonly GeneratedLabelSymbol breakLabel;
        private readonly GeneratedLabelSymbol continueLabel;

        protected LoopBinder(Binder enclosing)
            : base(enclosing)
        {
            this.breakLabel = new GeneratedLabelSymbol("break");
            this.continueLabel = new GeneratedLabelSymbol("continue");
        }

        internal override GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return this.breakLabel;
            }
        }

        internal override GeneratedLabelSymbol ContinueLabel
        {
            get
            {
                return this.continueLabel;
            }
        }
    }
}