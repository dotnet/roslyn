// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// If a proper method named "nameof" exists in the outer scopes, <see cref="IsNameofOperator"/> is false and this binder does nothing.
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
        private readonly Binder? _withParametersBinder;
        private ThreeState _lazyIsNameofOperator;

        internal NameofBinder(SyntaxNode nameofArgument, Binder next, WithTypeParametersBinder? withTypeParametersBinder, Binder? withParametersBinder)
            : base(next)
        {
            _nameofArgument = nameofArgument;
            _withTypeParametersBinder = withTypeParametersBinder;
            _withParametersBinder = withParametersBinder;
        }

        private bool IsNameofOperator
        {
            get
            {
                if (!_lazyIsNameofOperator.HasValue())
                {
                    _lazyIsNameofOperator = ThreeStateHelpers.ToThreeState(!NextRequired.InvocableNameofInScope());
                }

                return _lazyIsNameofOperator.Value();
            }
        }

        internal override bool IsInsideNameof => IsNameofOperator || base.IsInsideNameof;

        protected override SyntaxNode? EnclosingNameofArgument => IsNameofOperator ? _nameofArgument : base.EnclosingNameofArgument;

        internal override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            bool foundParameter = false;
            if (_withParametersBinder is not null && IsNameofOperator)
            {
                _withParametersBinder.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
                if (!result.IsClear)
                {
                    if (result.IsMultiViable)
                    {
                        return;
                    }

                    foundParameter = true;
                }
            }

            if (_withTypeParametersBinder is not null && IsNameofOperator)
            {
                if (foundParameter)
                {
                    var tmp = LookupResult.GetInstance();
                    _withTypeParametersBinder.LookupSymbolsInSingleBinder(tmp, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
                    result.MergeEqual(tmp);
                }
                else
                {
                    _withTypeParametersBinder.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
                }
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            if (_withParametersBinder is not null && IsNameofOperator)
            {
                _withParametersBinder.AddLookupSymbolsInfoInSingleBinder(info, options, originalBinder);
            }
            if (_withTypeParametersBinder is not null && IsNameofOperator)
            {
                _withTypeParametersBinder.AddLookupSymbolsInfoInSingleBinder(info, options, originalBinder);
            }
        }
    }
}
