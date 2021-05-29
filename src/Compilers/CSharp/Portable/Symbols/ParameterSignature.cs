// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class ParameterSignature
    {
        internal readonly ImmutableArray<TypeWithAnnotations> parameterTypesWithAnnotations;
        internal readonly ImmutableArray<RefKind> parameterRefKinds;

        internal static readonly ParameterSignature NoParams =
            new ParameterSignature(ImmutableArray<TypeWithAnnotations>.Empty, default(ImmutableArray<RefKind>));

        private ParameterSignature(ImmutableArray<TypeWithAnnotations> parameterTypesWithAnnotations,
                                   ImmutableArray<RefKind> parameterRefKinds)
        {
            this.parameterTypesWithAnnotations = parameterTypesWithAnnotations;
            this.parameterRefKinds = parameterRefKinds;
        }

        private static ParameterSignature MakeParamTypesAndRefKinds(ImmutableArray<ParameterSymbol> parameters)
        {
            if (parameters.Length == 0)
            {
                return NoParams;
            }

            var types = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            ArrayBuilder<RefKind> refs = null;

            for (int parm = 0; parm < parameters.Length; ++parm)
            {
                var parameter = parameters[parm];
                types.Add(parameter.TypeWithAnnotations);

                var refKind = parameter.RefKind;
                if (refs == null)
                {
                    if (refKind != RefKind.None)
                    {
                        refs = ArrayBuilder<RefKind>.GetInstance(parm, RefKind.None);
                        refs.Add(refKind);
                    }
                }
                else
                {
                    refs.Add(refKind);
                }
            }

            ImmutableArray<RefKind> refKinds = refs != null ? refs.ToImmutableAndFree() : default(ImmutableArray<RefKind>);
            return new ParameterSignature(types.ToImmutableAndFree(), refKinds);
        }

        internal static void PopulateParameterSignature(ImmutableArray<ParameterSymbol> parameters, ref ParameterSignature lazySignature)
        {
            if (lazySignature == null)
            {
                Interlocked.CompareExchange(ref lazySignature, MakeParamTypesAndRefKinds(parameters), null);
            }
        }
    }
}
