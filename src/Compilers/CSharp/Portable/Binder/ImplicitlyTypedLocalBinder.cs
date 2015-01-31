// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder is for binding the initializer of an implicitly typed
    /// local variable. While binding an implicitly typed local variable
    /// it is illegal to refer to the variable.
    /// </summary>

    internal sealed class ImplicitlyTypedLocalBinder : Binder
    {
        // 
        // This is legal:
        // 
        // int x = M(out x);
        // 
        // because x is known to be int, so overload resolution on M can work,
        // and x is assigned before it is read. This is not legal:
        // 
        // var x = M(out x);
        // 
        // We cannot know the type of x until overload resolution determines the
        // meaning of M, but that requires knowing the type of x.
        // 
        // In certain scenarios we might find ourselves in loops, like
        // 
        // var x = y;
        // var y = M(x);
        //
        // We break the cycle by ensuring that a var initializer which illegally refers
        // forwards to an in-scope local does not attempt to work out the type of the
        // forward local. However, just to make sure, we also keep track of every
        // local whose type we are attempting to infer. (This might be necessary for
        // "script class" scenarios where local vars are actually fields.)

        private readonly ConsList<LocalSymbol> _symbols;
        public ImplicitlyTypedLocalBinder(Binder next, LocalSymbol symbol)
            : base(next)
        {
            _symbols = new ConsList<LocalSymbol>(symbol, next.ImplicitlyTypedLocalsBeingBound);
        }

        public override ConsList<LocalSymbol> ImplicitlyTypedLocalsBeingBound
        {
            get
            {
                return _symbols;
            }
        }

        internal override LocalSymbol LocalInProgress
        {
            get
            {
                return _symbols.Head;
            }
        }
    }
}
