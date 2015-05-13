// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Emit
{
    internal partial class ReflectionEmitter
    {
        #region Unimplemented

        private abstract class UnimplementedType : Type
        {
            public override Assembly Assembly
            {
                get { throw new NotImplementedException(); }
            }

            public override string AssemblyQualifiedName
            {
                get { throw new NotImplementedException(); }
            }

            public override Type BaseType
            {
                get { throw new NotImplementedException(); }
            }

            public override string FullName
            {
                get { throw new NotImplementedException(); }
            }

            public override Guid GUID
            {
                get { throw new NotImplementedException(); }
            }

            protected override TypeAttributes GetAttributeFlagsImpl()
            {
                throw new NotImplementedException();
            }

            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetElementType()
            {
                throw new NotImplementedException();
            }

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetInterface(string name, bool ignoreCase)
            {
                throw new NotImplementedException();
            }

            public override Type[] GetInterfaces()
            {
                throw new NotImplementedException();
            }

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetNestedType(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type[] GetNestedTypes(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            protected override bool HasElementTypeImpl()
            {
                throw new NotImplementedException();
            }

            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
            {
                throw new NotImplementedException();
            }

            protected override bool IsArrayImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsByRefImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsCOMObjectImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsPointerImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsPrimitiveImpl()
            {
                throw new NotImplementedException();
            }

            public override Module Module
            {
                get { throw new NotImplementedException(); }
            }

            public override string Namespace
            {
                get { throw new NotImplementedException(); }
            }

            public override Type UnderlyingSystemType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }
        }

        private abstract class UnimplementedMethodInfo : MethodInfo
        {
            public override MethodInfo GetBaseDefinition()
            {
                throw new NotImplementedException();
            }

            public override ICustomAttributeProvider ReturnTypeCustomAttributes
            {
                get { throw new NotImplementedException(); }
            }

            public override MethodAttributes Attributes
            {
                get { throw new NotImplementedException(); }
            }

            public override MethodImplAttributes GetMethodImplementationFlags()
            {
                throw new NotImplementedException();
            }

            public override ParameterInfo[] GetParameters()
            {
                throw new NotImplementedException();
            }

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override RuntimeMethodHandle MethodHandle
            {
                get { throw new NotImplementedException(); }
            }

            public override Type DeclaringType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override Type ReflectedType
            {
                get { throw new NotImplementedException(); }
            }
        }

        private abstract class UnimplementedConstructorInfo : ConstructorInfo
        {
            public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override MethodAttributes Attributes
            {
                get { throw new NotImplementedException(); }
            }

            public override MethodImplAttributes GetMethodImplementationFlags()
            {
                throw new NotImplementedException();
            }

            public override ParameterInfo[] GetParameters()
            {
                throw new NotImplementedException();
            }

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override RuntimeMethodHandle MethodHandle
            {
                get { throw new NotImplementedException(); }
            }

            public override Type DeclaringType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override Type ReflectedType
            {
                get { throw new NotImplementedException(); }
            }
        }

        private abstract class UnimplementedFieldInfo : FieldInfo
        {
            public override FieldAttributes Attributes
            {
                get { throw new NotImplementedException(); }
            }

            public override RuntimeFieldHandle FieldHandle
            {
                get { throw new NotImplementedException(); }
            }

            public override Type FieldType
            {
                get { throw new NotImplementedException(); }
            }

            public override object GetValue(object obj)
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override Type DeclaringType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override Type ReflectedType
            {
                get { throw new NotImplementedException(); }
            }
        }

        private abstract class UnimplementedPropertyInfo : PropertyInfo
        {
            public override PropertyAttributes Attributes
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanRead
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanWrite
            {
                get { throw new NotImplementedException(); }
            }

            public override MethodInfo[] GetAccessors(bool nonPublic)
            {
                throw new NotImplementedException();
            }

            public override MethodInfo GetGetMethod(bool nonPublic)
            {
                throw new NotImplementedException();
            }

            public override ParameterInfo[] GetIndexParameters()
            {
                throw new NotImplementedException();
            }

            public override MethodInfo GetSetMethod(bool nonPublic)
            {
                throw new NotImplementedException();
            }

            public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override Type PropertyType
            {
                get { throw new NotImplementedException(); }
            }

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override Type DeclaringType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override Type ReflectedType
            {
                get { throw new NotImplementedException(); }
            }
        }

        #endregion

        private class SimpleParameterInfo : ParameterInfo
        {
            public SimpleParameterInfo(MemberInfo containingMember, int position, Type type)
            {
                this.PositionImpl = position + 1;
                this.ClassImpl = type;
                this.MemberImpl = containingMember;
            }
        }

        private sealed class ModifiedParameterInfo : SimpleParameterInfo
        {
            private readonly Type[] _reqMods;
            private readonly Type[] _optMods;

            public ModifiedParameterInfo(MemberInfo containingMember, int position, Type type, Type[] reqMods, Type[] optMods)
                : base(containingMember, position, type)
            {
                _reqMods = reqMods;
                _optMods = optMods;
            }

            public override Type[] GetRequiredCustomModifiers()
            {
                return _reqMods;
            }

            public override Type[] GetOptionalCustomModifiers()
            {
                return _optMods;
            }
        }

        /// <summary>
        /// GetMemberRefToken is broken for non-runtime members.
        /// This is to work around call to ResolveMethod:
        /// <code>
        /// methDef = method.Module.ResolveMethod(
        ///                 method.MetadataToken,
        ///                 method.DeclaringType != null ? method.DeclaringType.GetGenericArguments() : null,
        ///                 null);
        /// </code>
        /// and to force call to GetMemberRefToken in GetMethodTokenInternal. Calling GetMethodTokenInternal produces incorrect token for 
        /// a reference to a method on a baked type in a dynamic assembly (the modules are compared equal). Method gf{T} in test CompilationChain_Ldftn.
        /// <code>
        ///   if (!this.Equals(methodInfoUnbound.Module)
        ///       || (methodInfoUnbound.DeclaringType != null AndAlso methodInfoUnbound.DeclaringType.IsGenericType))
        ///   {
        ///       tk = GetMemberRefToken(methodInfoUnbound, null);
        ///   }
        ///   else
        ///   {
        ///       tk = GetMethodTokenInternal(methodInfoUnbound).Token;
        ///   }
        /// </code>
        /// </summary>
        private sealed class DummyModule : Module
        {
            internal const int DummyMethodToken = -1;

            private readonly MethodBase _definition;

            public DummyModule(MethodBase definition)
            {
                _definition = definition;
            }

            public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                Debug.Assert(metadataToken == DummyMethodToken);
                return _definition;
            }
        }

        private sealed class ByRefType : UnimplementedType
        {
            private readonly Type _elementType;

            public ByRefType(Type elementType)
            {
                _elementType = elementType;
            }

            protected override bool IsArrayImpl()
            {
                return false;
            }

            protected override bool IsByRefImpl()
            {
                return true;
            }

            protected override bool IsPointerImpl()
            {
                return false;
            }

            public override Type GetElementType()
            {
                return _elementType;
            }

            public override Type MakeByRefType()
            {
                return new ByRefType(this);
            }

            public override Type MakeArrayType()
            {
                return MakeSzArrayType(this);
            }
        }

        private sealed class ModifiedType : TypeDelegator
        {
            public readonly Type[] RequiredModifiers, OptionalModifiers;

            public Type UnmodifiedType
            {
                get { return base.typeImpl; }
            }

            public ModifiedType(Type type, Type[] reqMods, Type[] optMods)
                : base(type)
            {
                this.RequiredModifiers = reqMods;
                this.OptionalModifiers = optMods;
            }

            public override Type MakeByRefType()
            {
                return new ByRefType(this);
            }

            public override Type MakeArrayType()
            {
                // Ref.Emit signature helper doesn't support modified element type:
                // return MakeSzArrayType(this);
                throw new NotSupportedException("Ref.Emit limitation");
            }
        }

        private sealed class MethodGenericParameter : UnimplementedType
        {
            private readonly int _position;
            private readonly MethodInfo _containingMethod;

            public MethodGenericParameter(MethodInfo containingMethod, int position)
            {
                _position = position;
                _containingMethod = containingMethod;
            }

            public override MethodBase DeclaringMethod
            {
                get
                {
                    return _containingMethod;
                }
            }

            public override Type DeclaringType
            {
                get
                {
                    return null;
                }
            }

            public override int GenericParameterPosition
            {
                get
                {
                    return _position;
                }
            }

            public override bool IsGenericParameter
            {
                get
                {
                    return true;
                }
            }

            public override Type MakeByRefType()
            {
                return new ByRefType(this);
            }

            public override Type MakeArrayType()
            {
                return MakeSzArrayType(this);
            }
        }

        private sealed class MethodRef : UnimplementedMethodInfo
        {
            private readonly Type _containingType;
            private readonly string _name;
            private readonly CallingConventions _callingConvention;
            private readonly Module _moduleProxy;
            private readonly Type[] _extraParameterTypes;
            private ParameterInfo[] _parameters;
            private ParameterInfo _returnParameter;
            private Type[] _genericParameters;

            public MethodRef(Type containingType, string name, CallingConventions callingConvention, MethodBase unspecializedDefinition, Type[] extraParameterTypes)
            {
                Debug.Assert(containingType != null);
                Debug.Assert(name != null);

                _containingType = containingType;
                _name = name;
                _callingConvention = callingConvention;
                _extraParameterTypes = extraParameterTypes;

                // Always create proxy module to work around references to methods of types within the same dynamic assembly (the module compares equal).
                _moduleProxy = new DummyModule(unspecializedDefinition);
            }

            public void InitializeParameters(Type[] genericParameters, ParameterInfo[] parameters, ParameterInfo returnParameter)
            {
                Debug.Assert(genericParameters != null && parameters != null && returnParameter != null);
                Debug.Assert(_genericParameters == null && _parameters == null && _returnParameter == null);

                _genericParameters = genericParameters;
                _parameters = parameters;
                _returnParameter = returnParameter;
            }

            public Type[] ExtraParameterTypes
            {
                get { return _extraParameterTypes; }
            }

            public override bool IsGenericMethod
            {
                get { return _genericParameters.Length > 0; }
            }

            public override bool IsGenericMethodDefinition
            {
                get { return IsGenericMethod; }
            }

            public override Type[] GetGenericArguments()
            {
                return _genericParameters;
            }

            public override MethodInfo GetGenericMethodDefinition()
            {
                Debug.Assert(IsGenericMethod);
                return this;
            }

            public override int MetadataToken
            {
                get { return DummyModule.DummyMethodToken; }
            }

            public override Module Module
            {
                get { return _moduleProxy; }
            }

            public override string Name
            {
                get { return _name; }
            }

            public override Type ReflectedType
            {
                get { return _containingType; }
            }

            public override Type DeclaringType
            {
                get { return _containingType; }
            }

            public override MethodAttributes Attributes
            {
                get { return MethodAttributes.Public | ((_callingConvention & CallingConventions.HasThis) != 0 ? 0 : MethodAttributes.Static); }
            }

            public override CallingConventions CallingConvention
            {
                get { return _callingConvention; }
            }

            public override ParameterInfo[] GetParameters()
            {
                return _parameters;
            }

            public override ParameterInfo ReturnParameter
            {
                get { return _returnParameter; }
            }

            public override Type ReturnType
            {
                get { return _returnParameter.ParameterType; }
            }
        }

        private sealed class MethodSpec : UnimplementedMethodInfo
        {
            private readonly MethodInfo _definition;
            private readonly Type[] _typeArguments;

            public MethodSpec(MethodInfo definition, Type[] typeArguments)
            {
                Debug.Assert(typeArguments.Length > 0);

                _definition = definition;
                _typeArguments = typeArguments;
            }

            public override Module Module
            {
                get { return _definition.Module; }
            }

            public override Type[] GetGenericArguments()
            {
                return _typeArguments;
            }

            public override bool IsGenericMethod
            {
                get { return true; }
            }

            public override bool IsGenericMethodDefinition
            {
                get { return false; }
            }

            public override MethodInfo GetGenericMethodDefinition()
            {
                return _definition;
            }

            public override string Name
            {
                get { return _definition.Name; }
            }

            public override Type ReflectedType
            {
                get { return _definition.ReflectedType; }
            }

            public override Type DeclaringType
            {
                get { return _definition.DeclaringType; }
            }

            public override MethodAttributes Attributes
            {
                get { return _definition.Attributes; }
            }

            public override CallingConventions CallingConvention
            {
                get { return _definition.CallingConvention; }
            }
        }

        // Wrapper to pass to CustomAttributeBuilder
        private sealed class ConstructorRef : UnimplementedConstructorInfo
        {
            private readonly MethodBase _methodRef;

            public ConstructorRef(MethodBase methodRef)
            {
                _methodRef = methodRef;
            }

            public override Type DeclaringType
            {
                get { return _methodRef.DeclaringType; }
            }

            public override ParameterInfo[] GetParameters()
            {
                return _methodRef.GetParameters();
            }

            public override Type ReflectedType
            {
                get
                {
                    return _methodRef.ReflectedType;
                }
            }

            public override CallingConventions CallingConvention
            {
                get
                {
                    return _methodRef.CallingConvention;
                }
            }

            public override string Name
            {
                get
                {
                    return _methodRef.Name;
                }
            }

            public override MethodAttributes Attributes
            {
                get
                {
                    return _methodRef.Attributes;
                }
            }
        }

        private class FieldRef : UnimplementedFieldInfo
        {
            private readonly Type _containingType;
            private readonly string _name;
            private readonly Type _type;

            public FieldRef(Type containingType, string name, Type type)
            {
                Debug.Assert(name != null);
                Debug.Assert(containingType != null);

                _containingType = containingType;
                _name = name;
                _type = type;
            }

            public override Type FieldType
            {
                get { return _type; }
            }

            public override Type ReflectedType
            {
                get { return _containingType; }
            }

            public override Type DeclaringType
            {
                get { return _containingType; }
            }

            public override string Name
            {
                get { return _name; }
            }

            public override Type[] GetRequiredCustomModifiers()
            {
                return Type.EmptyTypes;
            }

            public override Type[] GetOptionalCustomModifiers()
            {
                return Type.EmptyTypes;
            }
        }

        private sealed class CustomAttributeProperty : UnimplementedPropertyInfo
        {
            private readonly Type _containingType;
            private readonly string _name;
            private readonly Type _type;

            public CustomAttributeProperty(Type containingType, string name, Type type)
            {
                Debug.Assert(containingType != null);
                Debug.Assert(name != null);
                Debug.Assert(type != null);

                _containingType = containingType;
                _name = name;
                _type = type;
            }

            public override Type ReflectedType
            {
                get { return _containingType; }
            }

            public override Type DeclaringType
            {
                get { return _containingType; }
            }

            public override string Name
            {
                get { return _name; }
            }

            public override Type PropertyType
            {
                get { return _type; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }
        }

        // TODO: unused
        private sealed class ModifiedFieldRef : FieldRef
        {
            private readonly Type[] _reqMods;
            private readonly Type[] _optMods;

            public ModifiedFieldRef(Type containingType, string name, Type type, Type[] reqMods, Type[] optMods)
                : base(containingType, name, type)
            {
                Debug.Assert(reqMods != null);
                Debug.Assert(optMods != null);

                _reqMods = reqMods;
                _optMods = optMods;
            }

            public override Type[] GetRequiredCustomModifiers()
            {
                return _reqMods;
            }

            public override Type[] GetOptionalCustomModifiers()
            {
                return _optMods;
            }
        }
    }
}
