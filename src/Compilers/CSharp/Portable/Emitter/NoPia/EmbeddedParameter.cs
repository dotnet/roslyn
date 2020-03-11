// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedParameter : EmbeddedTypesManager.CommonEmbeddedParameter
    {
        public EmbeddedParameter(EmbeddedTypesManager.CommonEmbeddedMember containingPropertyOrMethod, ParameterSymbol underlyingParameter) :
            base(containingPropertyOrMethod, underlyingParameter)
        {
            Debug.Assert(underlyingParameter.IsDefinition);
        }

        protected override bool HasDefaultValue
        {
            get
            {
                return UnderlyingParameter.HasMetadataConstantValue;
            }
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingParameter.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override MetadataConstant GetDefaultValue(EmitContext context)
        {
            return UnderlyingParameter.GetMetadataConstantValue(context);
        }

        protected override bool IsIn
        {
            get
            {
                return UnderlyingParameter.IsMetadataIn;
            }
        }

        protected override bool IsOut
        {
            get
            {
                return UnderlyingParameter.IsMetadataOut;
            }
        }

        protected override bool IsOptional
        {
            get
            {
                return UnderlyingParameter.IsMetadataOptional;
            }
        }

        protected override bool IsMarshalledExplicitly
        {
            get
            {
                return UnderlyingParameter.IsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation MarshallingInformation
        {
            get
            {
                return UnderlyingParameter.MarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return UnderlyingParameter.MarshallingDescriptor;
            }
        }

        protected override string Name
        {
            get { return UnderlyingParameter.MetadataName; }
        }

        protected override Cci.IParameterTypeInformation UnderlyingParameterTypeInformation
        {
            get
            {
                return (Cci.IParameterTypeInformation)UnderlyingParameter;
            }
        }

        protected override ushort Index
        {
            get
            {
                return (ushort)UnderlyingParameter.Ordinal;
            }
        }
    }
}
