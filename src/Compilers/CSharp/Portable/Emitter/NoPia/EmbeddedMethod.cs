// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public EmbeddedMethod(EmbeddedType containingType, MethodSymbol underlyingMethod) :
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
            return UnderlyingMethod.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override ImmutableArray<EmbeddedParameter> GetParameters()
        {
            return EmbeddedTypesManager.EmbedParameters(this, UnderlyingMethod.Parameters);
        }

        protected override ImmutableArray<EmbeddedTypeParameter> GetTypeParameters()
        {
            return UnderlyingMethod.TypeParameters.SelectAsArray((t, m) => new EmbeddedTypeParameter(m, t), this);
        }

        protected override bool IsAbstract
        {
            get
            {
                return UnderlyingMethod.IsAbstract;
            }
        }

        protected override bool IsAccessCheckedOnOverride
        {
            get
            {
                return UnderlyingMethod.IsAccessCheckedOnOverride;
            }
        }

        protected override bool IsConstructor
        {
            get
            {
                return UnderlyingMethod.MethodKind == MethodKind.Constructor;
            }
        }

        protected override bool IsExternal
        {
            get
            {
                return UnderlyingMethod.IsExternal;
            }
        }

        protected override bool IsHiddenBySignature
        {
            get
            {
                return !UnderlyingMethod.HidesBaseMethodsByName;
            }
        }

        protected override bool IsNewSlot
        {
            get
            {
                return UnderlyingMethod.IsMetadataNewSlot();
            }
        }

        protected override Cci.IPlatformInvokeInformation PlatformInvokeData
        {
            get
            {
                return UnderlyingMethod.GetDllImportData();
            }
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingMethod.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingMethod.HasSpecialName;
            }
        }

        protected override bool IsSealed
        {
            get
            {
                return UnderlyingMethod.IsMetadataFinal;
            }
        }

        protected override bool IsStatic
        {
            get
            {
                return UnderlyingMethod.IsStatic;
            }
        }

        protected override bool IsVirtual
        {
            get
            {
                return UnderlyingMethod.IsMetadataVirtual();
            }
        }

        protected override System.Reflection.MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return UnderlyingMethod.ImplementationAttributes;
        }

        protected override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return UnderlyingMethod.ReturnValueIsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation ReturnValueMarshallingInformation
        {
            get
            {
                return UnderlyingMethod.ReturnValueMarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return UnderlyingMethod.ReturnValueMarshallingDescriptor;
            }
        }

        protected override Cci.TypeMemberVisibility Visibility
        {
            get
            {
                return PEModuleBuilder.MemberVisibility(UnderlyingMethod);
            }
        }

        protected override string Name
        {
            get { return UnderlyingMethod.MetadataName; }
        }

        protected override bool AcceptsExtraArguments
        {
            get
            {
                return UnderlyingMethod.IsVararg;
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
                return UnderlyingMethod.ContainingNamespace;
            }
        }
    }
}
