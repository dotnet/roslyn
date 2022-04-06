// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// If a proper method named "nameof" exists in the outer scopes, this binder does nothing.
    /// Otherwise, it relaxes the instance-vs-static requirement for top-level member access expressions
    /// and when inside an attribute on a method it adds type parameters from the target of that attribute.
    /// To do so, it works together with <see cref="ContextualAttributeBinder"/>.
    /// 
    /// For other attributes (on types, type parameters or parameters) we use a WithTypeParameterBinder directly
    /// in the binder chain and some filtering (<see cref="LookupOptions.MustNotBeMethodTypeParameter"/>) to keep
    /// pre-existing behavior.
    /// </summary>
    internal sealed class NameofBinder : Binder
    {
        private readonly SyntaxNode _nameofArgument;
        private readonly WithTypeParametersBinder? _withTypeParametersBinder;

        // One bit encodes whether this was computed, the other bit encodes a boolean value
        private int _lazyIsInsideNameof;
        private const byte _lazyIsInsideNameof_IsInitialized = 1 << 0;
        private const byte _lazyIsInsideNameof_Value = 1 << 1;

        internal NameofBinder(SyntaxNode nameofArgument, Binder next, WithTypeParametersBinder? withTypeParametersBinder)
            : base(next)
        {
            _nameofArgument = nameofArgument;
            _withTypeParametersBinder = withTypeParametersBinder;
        }

        internal override bool IsInsideNameof
        {
            get
            {
                if ((_lazyIsInsideNameof & _lazyIsInsideNameof_IsInitialized) == 0)
                {
                    bool isInsideNameof = !NextRequired.InvocableNameofInScope() || base.IsInsideNameof;

                    int value = _lazyIsInsideNameof_IsInitialized;
                    value |= isInsideNameof ? _lazyIsInsideNameof_Value : 0;
                    ThreadSafeFlagOperations.Set(ref _lazyIsInsideNameof, value);
                }

                return (_lazyIsInsideNameof & _lazyIsInsideNameof_Value) != 0;
            }
        }

        protected override SyntaxNode? EnclosingNameofArgument => IsInsideNameof ? _nameofArgument : base.EnclosingNameofArgument;

        internal override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (_withTypeParametersBinder is not null && IsInsideNameof)
            {
                _withTypeParametersBinder.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            if (_withTypeParametersBinder is not null && IsInsideNameof)
            {
                _withTypeParametersBinder.AddLookupSymbolsInfoInSingleBinder(info, options, originalBinder);
            }
        }
    }
}
