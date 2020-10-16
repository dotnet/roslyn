// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedMethod : EmbeddedTypesManager.CommonEmbeddedMethod
    {
        public EmbeddedMethod(EmbeddedType containingType,
#if DEBUG
            MethodSymbolAdapter
#else
            MethodSymbol
#endif
                underlyingMethod) :
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
            return EmbeddedTypesManager.EmbedParameters(this, UnderlyingMethod.UnderlyingMethodSymbol.Parameters);
        }

        protected override ImmutableArray<EmbeddedTypeParameter> GetTypeParameters()
        {
            return UnderlyingMethod.UnderlyingMethodSymbol.TypeParameters.SelectAsArray((t, m) => new EmbeddedTypeParameter(m, t.GetAdapter()), this);
        }

        protected override bool IsAbstract
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsAbstract;
            }
        }

        protected override bool IsAccessCheckedOnOverride
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsAccessCheckedOnOverride;
            }
        }

        protected override bool IsConstructor
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.MethodKind == MethodKind.Constructor;
            }
        }

        protected override bool IsExternal
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsExternal;
            }
        }

        protected override bool IsHiddenBySignature
        {
            get
            {
                return !UnderlyingMethod.UnderlyingMethodSymbol.HidesBaseMethodsByName;
            }
        }

        protected override bool IsNewSlot
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsMetadataNewSlot();
            }
        }

        protected override Cci.IPlatformInvokeInformation PlatformInvokeData
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.GetDllImportData();
            }
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.HasSpecialName;
            }
        }

        protected override bool IsSealed
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsMetadataFinal;
            }
        }

        protected override bool IsStatic
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsStatic;
            }
        }

        protected override bool IsVirtual
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsMetadataVirtual();
            }
        }

        protected override System.Reflection.MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return UnderlyingMethod.UnderlyingMethodSymbol.ImplementationAttributes;
        }

        protected override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.ReturnValueIsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation ReturnValueMarshallingInformation
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.ReturnValueMarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.ReturnValueMarshallingDescriptor;
            }
        }

        protected override Cci.TypeMemberVisibility Visibility
        {
            get
            {
                return PEModuleBuilder.MemberVisibility(UnderlyingMethod.UnderlyingMethodSymbol);
            }
        }

        protected override string Name
        {
            get { return UnderlyingMethod.UnderlyingMethodSymbol.MetadataName; }
        }

        protected override bool AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethod.UnderlyingMethodSymbol.IsVararg;
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
                return UnderlyingMethod.UnderlyingMethodSymbol.ContainingNamespace.GetAdapter();
            }
        }
    }
}
