// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System.Diagnostics;
using Cci = Microsoft.Cci;

#if !DEBUG
using TypeParameterSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.TypeParameterSymbol;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedTypeParameter : EmbeddedTypesManager.CommonEmbeddedTypeParameter
    {
        public EmbeddedTypeParameter(EmbeddedMethod containingMethod, TypeParameterSymbolAdapter underlyingTypeParameter) :
            base(containingMethod, underlyingTypeParameter)
        {
            Debug.Assert(underlyingTypeParameter.AdaptedTypeParameterSymbol.IsDefinition);
        }

        protected override IEnumerable<Cci.TypeReferenceWithAttributes> GetConstraints(EmitContext context)
        {
            return ((Cci.IGenericParameter)UnderlyingTypeParameter).GetConstraints(context);
        }

        protected override bool MustBeReferenceType
        {
            get
            {
                return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasReferenceTypeConstraint;
            }
        }

        protected override bool MustBeValueType
        {
            get
            {
                return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasValueTypeConstraint;
            }
        }

        protected override bool AllowsRefLikeType
        {
            get
            {
                return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.AllowsRefLikeType;
            }
        }

        protected override bool MustHaveDefaultConstructor
        {
            get
            {
                return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasConstructorConstraint;
            }
        }

        protected override string Name
        {
            get { return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.MetadataName; }
        }

        protected override ushort Index
        {
            get
            {
                return (ushort)UnderlyingTypeParameter.AdaptedTypeParameterSymbol.Ordinal;
            }
        }
    }
}

