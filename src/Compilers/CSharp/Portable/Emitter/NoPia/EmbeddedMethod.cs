// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;

#if !DEBUG
using MethodSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedMethod : EmbeddedTypesManager.CommonEmbeddedMethod
    {
        public EmbeddedMethod(EmbeddedType containingType, MethodSymbolAdapter underlyingMethod) :
            base(containingType, underlyingMethod)
        {
        }

        internal override EmbeddedTypesManager TypeManager
        {
            get
            {
                return ContainingType.TypeManager;
            }
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingMethod.AdaptedSymbol.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override ImmutableArray<EmbeddedParameter> GetParameters()
        {
            return EmbeddedTypesManager.EmbedParameters(this, UnderlyingMethod.AdaptedMethodSymbol.Parameters);
        }

        protected override ImmutableArray<EmbeddedTypeParameter> GetTypeParameters()
        {
            return UnderlyingMethod.AdaptedMethodSymbol.TypeParameters.SelectAsArray((t, m) => new EmbeddedTypeParameter(m, t.GetCciAdapter()), this);
        }

        protected override bool IsAbstract
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsAbstract;
            }
        }

        protected override bool IsAccessCheckedOnOverride
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsAccessCheckedOnOverride;
            }
        }

        protected override bool IsConstructor
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.MethodKind == MethodKind.Constructor;
            }
        }

        protected override bool IsExternal
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsExternal;
            }
        }

        protected override bool IsHiddenBySignature
        {
            get
            {
                return !UnderlyingMethod.AdaptedMethodSymbol.HidesBaseMethodsByName;
            }
        }

        protected override bool IsNewSlot
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataNewSlot();
            }
        }

        protected override Cci.IPlatformInvokeInformation PlatformInvokeData
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.GetDllImportData();
            }
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.HasSpecialName;
            }
        }

        protected override bool IsSealed
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataFinal;
            }
        }

        protected override bool IsStatic
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsStatic;
            }
        }

        protected override bool IsVirtual
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataVirtual();
            }
        }

        protected override System.Reflection.MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return UnderlyingMethod.AdaptedMethodSymbol.ImplementationAttributes;
        }

        protected override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.ReturnValueIsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation ReturnValueMarshallingInformation
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.ReturnValueMarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.ReturnValueMarshallingDescriptor;
            }
        }

        protected override Cci.TypeMemberVisibility Visibility
            => UnderlyingMethod.AdaptedMethodSymbol.MetadataVisibility;

        protected override string Name
        {
            get { return UnderlyingMethod.AdaptedMethodSymbol.MetadataName; }
        }

        protected override bool AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.IsVararg;
            }
        }

        protected override Cci.ISignature UnderlyingMethodSignature
        {
            get
            {
                return (Cci.ISignature)UnderlyingMethod;
            }
        }

        protected override Cci.INamespace ContainingNamespace
        {
            get
            {
                return UnderlyingMethod.AdaptedMethodSymbol.ContainingNamespace.GetCciAdapter();
            }
        }
    }
}
