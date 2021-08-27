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
    internal sealed class PropertyInfoImpl : PropertyInfo
    {
        internal readonly System.Reflection.PropertyInfo Property;

        internal PropertyInfoImpl(System.Reflection.PropertyInfo property)
        {
            Debug.Assert(property != null);
            this.Property = property;
        }

        public override System.Reflection.PropertyAttributes Attributes
        {
            get { return this.Property.Attributes; }
        }

        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get
            {
                return this.Property.CanWrite;
            }
        }

        public override Type DeclaringType
        {
            get { return (TypeImpl)this.Property.DeclaringType; }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override MemberTypes MemberType
        {
            get { return (MemberTypes)this.Property.MemberType; }
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
            get { return this.Property.Name; }
        }

        public override Type PropertyType
        {
            get { return (TypeImpl)this.Property.PropertyType; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return this.Property.GetAccessors(nonPublic).Select(a => new MethodInfoImpl(a)).ToArray();
        }

        public override object GetConstantValue()
        {
            throw new NotImplementedException();
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
            return this.Property.GetCustomAttributesData().Select(a => new CustomAttributeDataImpl(a)).ToArray();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            var method = this.Property.GetGetMethod(nonPublic);
            return (method == null) ? null : new MethodInfoImpl(method);
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return this.Property.GetIndexParameters().Select(p => new ParameterInfoImpl(p)).ToArray();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            var setMethod = this.Property.GetSetMethod(nonPublic);
            return (setMethod != null) ? new MethodInfoImpl(setMethod) : null;
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            Debug.Assert(binder == null, "NYI");
            Debug.Assert(index == null, "NYI");
            Debug.Assert(culture == null, "NYI");
            return this.Property.GetValue(obj, (System.Reflection.BindingFlags)invokeAttr, null, null, null);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.Property.ToString();
        }
    }
}
