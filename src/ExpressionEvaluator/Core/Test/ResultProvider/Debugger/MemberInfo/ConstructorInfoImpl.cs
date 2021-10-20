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
    internal sealed class ConstructorInfoImpl : ConstructorInfo
    {
        internal readonly System.Reflection.ConstructorInfo Constructor;

        internal ConstructorInfoImpl(System.Reflection.ConstructorInfo constructor)
        {
            Debug.Assert(constructor != null);
            this.Constructor = constructor;
        }

        public override System.Reflection.MethodAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override System.Reflection.CallingConventions CallingConvention
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return (TypeImpl)Constructor.DeclaringType;
            }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MemberTypes MemberType
        {
            get
            {
                return (MemberTypes)Constructor.MemberType;
            }
        }

        public override int MetadataToken
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Microsoft.VisualStudio.Debugger.Metadata.Module Module
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                return Constructor.Name;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override IList<Microsoft.VisualStudio.Debugger.Metadata.CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public override Microsoft.VisualStudio.Debugger.Metadata.MethodBody GetMethodBody()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override Microsoft.VisualStudio.Debugger.Metadata.ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            Debug.Assert(binder == null, "NYI");
            return Constructor.Invoke((System.Reflection.BindingFlags)invokeAttr, null, parameters, culture);
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
