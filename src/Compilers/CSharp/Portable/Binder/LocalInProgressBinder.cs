// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder keeps track of the local variable (if any) that is currently being evaluated
    /// so that it can be passed into the next call to LocalSymbol.GetConstantValue (and
    /// its callers).
    /// </summary>
    internal sealed class LocalInProgressBinder : Binder
    {
        public readonly EqualsValueClauseSyntax InitializerSyntax;
        private LocalSymbol? _localSymbol;

        internal LocalInProgressBinder(EqualsValueClauseSyntax initializerSyntax, Binder next)
            : base(next)
        {
            InitializerSyntax = initializerSyntax;
        }

        internal override LocalSymbol LocalInProgress
        {
            get
            {
                // The local symbol should have been initialized by now
                Debug.Assert(_localSymbol is not null);
                return _localSymbol;
            }
        }

        internal void SetLocalSymbol(LocalSymbol local)
        {
            Interlocked.CompareExchange(ref _localSymbol, local, null);
        }
    }
}
