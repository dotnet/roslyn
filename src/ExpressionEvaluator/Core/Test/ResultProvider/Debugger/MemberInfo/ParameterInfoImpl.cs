// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override Type ParameterType
        {
            get { throw new NotImplementedException(); }
        }

        public override int Position
        {
            get { throw new NotImplementedException(); }
        }

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
