// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EEMethodBinder : Binder
    {
        private readonly MethodSymbol _containingMethod;
        private readonly int _parameterOffset;
        private readonly ImmutableArray<ParameterSymbol> _targetParameters;
        private readonly Binder _sourceBinder;

        internal EEMethodBinder(EEMethodSymbol method, MethodSymbol containingMethod, Binder next) : base(next, next.Flags | BinderFlags.InEEMethodBinder)
        {
            Debug.Assert(method.DeclaringCompilation is not null);

            // There are a lot of method symbols floating around and we're doing some subtle things with them.
            //   1) method is the EEMethodSymbol that we're going to synthesize and hand to the debugger to evaluate.
            //   2) containingMethod is the method that we are conceptually in, e.g. the method containing the
            //      lambda that is on top of the call stack.  Any type parameters will have been replaced with the
            //      corresponding type parameters from (1) and its containing type.
            //   3) method.SubstitutedSourceMethod is the method that we are actually in, e.g. a lambda method in
            //      a display class.  Any type parameters will have been replaced with the corresponding type parameters
            //      from (1).
            // So why do we need all these methods?
            //   1) gives us the parameters that we need to actually bind to (it's no good to bind to the symbols 
            //      owned by (2) or (3)).  Also, it happens to contain (3), so we don't need to pass (3) explicitly.
            //   2) is where we want to pretend we're binding expressions, so we make it the containing symbol of
            //      this binder.
            //   3) is where we'll pretend to be for the purposes of looking up parameters by name.  However, any
            //      parameters we bind to from (3) will be replaced by the corresponding parameters from (1).

            _containingMethod = containingMethod;
            var substitutedSourceMethod = method.SubstitutedSourceMethod;
            _parameterOffset = substitutedSourceMethod.IsStatic ? 0 : 1;
            _targetParameters = method.Parameters;

            // Note that we never expect this InMethodBinder to find candidate symbols which may be file-local, and therefore pass 'associatedFileIdentifier: null' to the BuckStopsHereBinder.
            _sourceBinder = new InMethodBinder(substitutedSourceMethod, new BuckStopsHereBinder(next.Compilation, associatedFileIdentifier: null).WithAdditionalFlags(BinderFlags.InEEMethodBinder));
        }

        internal override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            _sourceBinder.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteInfo);

            var symbols = result.Symbols;
            for (int i = 0; i < symbols.Count; i++)
            {
                // Type parameters requiring mapping to the target type and
                // should be found by WithMethodTypeParametersBinder instead.
                var parameter = (ParameterSymbol)symbols[i];
                Debug.Assert(parameter.ContainingSymbol == _sourceBinder.ContainingMemberOrLambda);
                Debug.Assert(GeneratedNameParser.GetKind(parameter.Name) == GeneratedNameKind.None);
                symbols[i] = _targetParameters[parameter.Ordinal + _parameterOffset];
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            throw new NotImplementedException();
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get { return _containingMethod; }
        }
    }
}
