// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class MethodInfoImpl : MethodInfo
    {
        internal readonly System.Reflection.MethodInfo Method;

        internal MethodInfoImpl(System.Reflection.MethodInfo method)
        {
            Debug.Assert(method != null);
            this.Method = method;
        }

        public override System.Reflection.MethodAttributes Attributes
        {
            get { return this.Method.Attributes; }
        }

        public override System.Reflection.CallingConventions CallingConvention
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return (TypeImpl)this.Method.DeclaringType; }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override bool IsGenericMethodDefinition
        {
            get { throw new NotImplementedException(); }
        }

        public override MemberTypes MemberType
        {
            get { return (MemberTypes)this.Method.MemberType; }
        }

        public override int MetadataToken
        {
            get { throw new NotImplementedException(); }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Module Module
        {
            get { throw new NotImplementedException(); }
        }

        public override string Name
        {
            get { return this.Method.Name; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override ParameterInfo ReturnParameter
        {
            get { throw new NotImplementedException(); }
        }

        public override Type ReturnType
        {
            get { throw new NotImplementedException(); }
        }

        public override MethodInfo GetBaseDefinition()
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
            throw new NotImplementedException();
        }

        public override MethodBody GetMethodBody()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo MakeGenericMethod(params Type[] types)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.Method.ToString();
        }

        /// <remarks>
        /// These objects are used as dictionary keys, so we need Equals and GetHashCode.
        /// </remarks>
        public override bool Equals(object obj)
        {
            var other = obj as MethodInfoImpl;
            return other != null && this.Method.Equals(other.Method);
        }

        /// <remarks>
        /// These objects are used as dictionary keys, so we need Equals and GetHashCode.
        /// </remarks>
        public override int GetHashCode()
        {
            return this.Method.GetHashCode();
        }
    }
}
