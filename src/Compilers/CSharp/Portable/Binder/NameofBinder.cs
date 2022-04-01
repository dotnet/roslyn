// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// For other attributes (on types, type parameters or paramaeters) we use a WithTypeParameterBinder directly
    /// in the binder chain and some filtering (<see cref="LookupOptions.MustNotBeMethodTypeParameter"/>) to keep
    /// pre-existing behavior.
    /// </summary>
    internal sealed class NameofBinder : Binder
    {
        private readonly SyntaxNode _nameofArgument;
        private readonly WithTypeParametersBinder? _nextWhenNameofOperatorInAttribute;

        internal NameofBinder(SyntaxNode nameofArgument, Binder nextWhenNameofInvocation, WithTypeParametersBinder? nextWhenNameofOperatorInAttribute)
            : base(nextWhenNameofInvocation)
        {
            _nameofArgument = nameofArgument;
            _nextWhenNameofOperatorInAttribute = nextWhenNameofOperatorInAttribute;
        }

        // TODO2 pass in syntax to compare against _nameofArgument
        protected override bool IsInsideNameof => !NextRequired.InvocableNameofInScope();

        protected override SyntaxNode? EnclosingNameofArgument => !IsInsideNameof ? null : _nameofArgument;

        internal override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (IsInsideNameof && _nextWhenNameofOperatorInAttribute is not null)
            {
                _nextWhenNameofOperatorInAttribute.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            if (IsInsideNameof && _nextWhenNameofOperatorInAttribute is not null)
            {
                _nextWhenNameofOperatorInAttribute.AddLookupSymbolsInfoInSingleBinder(info, options, originalBinder);
            }
        }
    }
}
