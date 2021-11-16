// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class FieldInfoImpl : FieldInfo
    {
        internal readonly System.Reflection.FieldInfo Field;

        internal FieldInfoImpl(System.Reflection.FieldInfo field)
        {
            Debug.Assert(field != null);
            this.Field = field;
        }

        public override System.Reflection.FieldAttributes Attributes
        {
            get { return this.Field.Attributes; }
        }

        public override Type DeclaringType
        {
            get { return (TypeImpl)this.Field.DeclaringType; }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type FieldType
        {
            get { return (TypeImpl)this.Field.FieldType; }
        }

        public override MemberTypes MemberType
        {
            get { return (MemberTypes)this.Field.MemberType; }
        }

        public override int MetadataToken
        {
            get { throw new NotImplementedException(); }
        }

        public override Module Module
        {
            get { throw new NotImplementedException(); }
        }

        public override string Name
        {
            get { return this.Field.Name; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return this.Field.GetCustomAttributesData().Select(a => new CustomAttributeDataImpl(a)).ToArray();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public override object GetRawConstantValue()
        {
            return Field.GetRawConstantValue();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj)
        {
            return Field.GetValue(obj);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.Field.ToString();
        }
    }
}
