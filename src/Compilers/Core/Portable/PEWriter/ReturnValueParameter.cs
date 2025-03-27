// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Cci
{
    internal sealed class ReturnValueParameter : ParameterDefinitionBase
    {
        internal ReturnValueParameter(IMethodDefinition containingMethod)
        {
            _containingMethod = containingMethod;
        }

        public override IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _containingMethod.GetReturnValueAttributes(context);
        }

        private readonly IMethodDefinition _containingMethod;

        public override ImmutableArray<Cci.ICustomModifier> RefCustomModifiers
        {
            get { return _containingMethod.RefCustomModifiers; }
        }

        public override ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { return _containingMethod.ReturnValueCustomModifiers; }
        }

        public override ushort Index
        {
            get { return 0; }
        }

        public override bool IsByReference
        {
            get { return _containingMethod.ReturnValueIsByRef; }
        }

        public override bool IsMarshalledExplicitly
        {
            get { return _containingMethod.ReturnValueIsMarshalledExplicitly; }
        }

        public override IMarshallingInformation MarshallingInformation
        {
            get { return _containingMethod.ReturnValueMarshallingInformation; }
        }

        public override ImmutableArray<byte> MarshallingDescriptor
        {
            get { return _containingMethod.ReturnValueMarshallingDescriptor; }
        }

        public override string Name
        {
            get { return string.Empty; }
        }

        public override ITypeReference GetType(EmitContext context)
        {
            return _containingMethod.GetType(context);
        }
    }
}
