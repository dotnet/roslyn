// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder keeps track of the local variable (if any) that is currently being evaluated
    /// so that it can be passed into the next call to LocalSymbol.GetConstantValue (and
    /// its callers).
    /// </summary>
    internal sealed class LocalInProgressBinder : Binder
    {
        private readonly LocalSymbol _inProgress;

        internal LocalInProgressBinder(LocalSymbol inProgress, Binder next)
            : base(next)
        {
            _inProgress = inProgress;
        }

        internal override LocalSymbol LocalInProgress
        {
            get
            {
                return _inProgress;
            }
        }
    }
}
