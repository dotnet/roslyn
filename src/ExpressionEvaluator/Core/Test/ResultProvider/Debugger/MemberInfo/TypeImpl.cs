// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal class TypeImpl : Type
    {
        internal readonly System.Type Type;

        internal TypeImpl(System.Type type)
        {
            Debug.Assert(type != null);
            this.Type = type;
        }

        public static explicit operator TypeImpl(System.Type type)
        {
            return type == null ? null : new TypeImpl(type);
        }

        public override Assembly Assembly
        {
            get { return new AssemblyImpl(this.Type.Assembly); }
        }

        public override string AssemblyQualifiedName
        {
            get { throw new NotImplementedException(); }
        }

        public override Type BaseType
        {
            get { return (TypeImpl)this.Type.BaseType; }
        }

        public override bool ContainsGenericParameters
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return (TypeImpl)this.Type.DeclaringType; }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override string FullName
        {
            get { return this.Type.FullName; }
        }

        public override Guid GUID
        {
            get { throw new NotImplementedException(); }
        }

        public override MemberTypes MemberType
        {
            get
            {
                return (MemberTypes)this.Type.MemberType;
            }
        }

        public override int MetadataToken
        {
            get { throw new NotImplementedException(); }
        }

        public override Module Module
        {
            get { return new ModuleImpl(this.Type.Module); }
        }

        public override string Name
        {
            get { return Type.Name; }
        }

        public override string Namespace
        {
            get { return Type.Namespace; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override Type UnderlyingSystemType
        {
            get { return (TypeImpl)Type.UnderlyingSystemType; }
        }

        public override bool Equals(Type o)
        {
            return o is TypeImpl { Type: this.Type } && o.GetType() is this.GetType();
        }

        public override bool Equals(object objOther)
        {
            return Equals(objOther as Type);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override int GetArrayRank()
        {
            return Type.GetArrayRank();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return Type.GetConstructors((System.Reflection.BindingFlags)bindingAttr).Select(c => new ConstructorInfoImpl(c)).ToArray();
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
            return Type.GetCustomAttributesData().Select(a => new CustomAttributeDataImpl(a)).ToArray();
        }

        public override Type GetElementType()
        {
            return (TypeImpl)(Type.GetElementType());
        }

        public override EventInfo GetEvent(string name, BindingFlags flags)
        {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags flags)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            var field = Type.GetField(name, (System.Reflection.BindingFlags)bindingAttr);
            return (field == null) ? null : new FieldInfoImpl(field);
        }

        public override FieldInfo[] GetFields(BindingFlags flags)
        {
            return Type.GetFields((System.Reflection.BindingFlags)flags).Select(f => new FieldInfoImpl(f)).ToArray();
        }

        public override Type GetGenericTypeDefinition()
        {
            return (TypeImpl)this.Type.GetGenericTypeDefinition();
        }

        public override Type[] GetGenericArguments()
        {
            return Type.GetGenericArguments().Select(t => new TypeImpl(t)).ToArray();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            return Type.GetInterfaces().Select(i => new TypeImpl(i)).ToArray();
        }

        public override System.Reflection.InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            return Type.GetMember(name, (System.Reflection.BindingFlags)bindingAttr).Select(GetMember).ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return Type.GetMembers((System.Reflection.BindingFlags)bindingAttr).Select(GetMember).ToArray();
        }

        private static MemberInfo GetMember(System.Reflection.MemberInfo member)
        {
            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Constructor:
                    return new ConstructorInfoImpl((System.Reflection.ConstructorInfo)member);
                case System.Reflection.MemberTypes.Event:
                    return new EventInfoImpl((System.Reflection.EventInfo)member);
                case System.Reflection.MemberTypes.Field:
                    return new FieldInfoImpl((System.Reflection.FieldInfo)member);
                case System.Reflection.MemberTypes.Method:
                    return new MethodInfoImpl((System.Reflection.MethodInfo)member);
                case System.Reflection.MemberTypes.NestedType:
                    return new TypeImpl((System.Reflection.TypeInfo)member);
                case System.Reflection.MemberTypes.Property:
                    return new PropertyInfoImpl((System.Reflection.PropertyInfo)member);
                default:
                    throw new NotImplementedException(member.MemberType.ToString());
            }
        }

        public override MethodInfo[] GetMethods(BindingFlags flags)
        {
            return this.Type.GetMethods((System.Reflection.BindingFlags)flags).Select(m => new MethodInfoImpl(m)).ToArray();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags flags)
        {
            throw new NotImplementedException();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotImplementedException();
        }

        public override bool IsAssignableFrom(Type c)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsEnum
        {
            get { return this.Type.IsEnum; }
        }

        public override bool IsGenericParameter
        {
            get { return Type.IsGenericParameter; }
        }

        public override bool IsGenericType
        {
            get { return Type.IsGenericType; }
        }

        public override bool IsGenericTypeDefinition
        {
            get { return Type.IsGenericTypeDefinition; }
        }

        public override int GenericParameterPosition
        {
            get { return Type.GenericParameterPosition; }
        }

        public override ExplicitInterfaceInfo[] GetExplicitInterfaceImplementations()
        {
            var interfaceMaps = Type.GetInterfaces().Select(i => Type.GetInterfaceMap(i));

            // A dot is neither necessary nor sufficient for determining whether a member explicitly
            // implements an interface member, but it does characterize the set of members we're
            // interested in displaying differently.  For example, if the property is from VB, it will
            // be an explicit interface implementation, but will not have a dot.  Therefore, this is
            // good enough for our mock implementation.
            var infos = interfaceMaps.SelectMany(map =>
                map.InterfaceMethods.Zip(map.TargetMethods, (interfaceMethod, implementingMethod) =>
                    implementingMethod.Name.Contains(".")
                        ? MakeExplicitInterfaceInfo(interfaceMethod, implementingMethod)
                        : null));
            return infos.Where(i => i != null).ToArray();
        }

        private static ExplicitInterfaceInfo MakeExplicitInterfaceInfo(System.Reflection.MethodInfo interfaceMethod, System.Reflection.MethodInfo implementingMethod)
        {
            return (ExplicitInterfaceInfo)typeof(ExplicitInterfaceInfo).Instantiate(
                new MethodInfoImpl(interfaceMethod), new MethodInfoImpl(implementingMethod));
        }

        public override bool IsInstanceOfType(object o)
        {
            throw new NotImplementedException();
        }

        public override bool IsSubclassOf(Type c)
        {
            throw new NotImplementedException();
        }

        public override Type MakeArrayType()
        {
            return (TypeImpl)this.Type.MakeArrayType();
        }

        public override Type MakeArrayType(int rank)
        {
            return (TypeImpl)this.Type.MakeArrayType(rank);
        }

        public override Type MakeByRefType()
        {
            throw new NotImplementedException();
        }

        public override Type MakeGenericType(params Type[] argTypes)
        {
            return (TypeImpl)this.Type.MakeGenericType(argTypes.Select(t => ((TypeImpl)t).Type).ToArray());
        }

        public override Type MakePointerType()
        {
            return (TypeImpl)this.Type.MakePointerType();
        }

        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl()
        {
            System.Reflection.TypeAttributes result = 0;
            if (this.Type.IsClass)
            {
                result |= System.Reflection.TypeAttributes.Class;
            }
            if (this.Type.IsInterface)
            {
                result |= System.Reflection.TypeAttributes.Interface;
            }
            if (this.Type.IsAbstract)
            {
                result |= System.Reflection.TypeAttributes.Abstract;
            }
            if (this.Type.IsSealed)
            {
                result |= System.Reflection.TypeAttributes.Sealed;
            }
            return result;
        }

        protected override bool IsValueTypeImpl()
        {
            return this.Type.IsValueType;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(binder == null, "NYI");
            Debug.Assert(returnType == null, "NYI");
            Debug.Assert(types == null, "NYI");
            Debug.Assert(modifiers == null, "NYI");
            return new PropertyInfoImpl(Type.GetProperty(name, (System.Reflection.BindingFlags)bindingAttr, binder: null, returnType: null, types: new System.Type[0], modifiers: new System.Reflection.ParameterModifier[0]));
        }

        protected override TypeCode GetTypeCodeImpl()
        {
            return (TypeCode)System.Type.GetTypeCode(this.Type);
        }

        protected override bool HasElementTypeImpl()
        {
            return this.Type.HasElementType;
        }

        protected override bool IsArrayImpl()
        {
            return Type.IsArray;
        }

        protected override bool IsByRefImpl()
        {
            return Type.IsByRef;
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsContextfulImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsMarshalByRefImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl()
        {
            return Type.IsPointer;
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.Type.ToString();
        }

        public override Type[] GetInterfacesOnType()
        {
            var t = this.Type;
            var builder = ArrayBuilder<Type>.GetInstance();
            foreach (var @interface in t.GetInterfaces())
            {
                var map = t.GetInterfaceMap(@interface);
                if (map.TargetMethods.Any(m => m.DeclaringType == t))
                {
                    builder.Add((TypeImpl)@interface);
                }
            }
            return builder.ToArrayAndFree();
        }
    }
}
