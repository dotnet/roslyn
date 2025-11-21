// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ParameterInfoImpl : ParameterInfo
    {
        internal readonly System.Reflection.ParameterInfo Parameter;

        internal ParameterInfoImpl(System.Reflection.ParameterInfo parameter)
        {
            Debug.Assert(parameter != null);
            this.Parameter = parameter;
        }

        public override System.Reflection.ParameterAttributes Attributes
        {
            get { throw new NotImplementedException(); }
        }

        public override object DefaultValue
        {
            get { throw new NotImplementedException(); }
        }

        public override MemberInfo Member
        {
            get { throw new NotImplementedException(); }
        }

        public override string Name => Parameter.Name;

        public override Type ParameterType => new TypeImpl(Parameter.ParameterType);

        public override int Position => Parameter.Position;

        public override object RawDefaultValue
        {
            get { throw new NotImplementedException(); }
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            throw new NotImplementedException();
        }
    }
}
