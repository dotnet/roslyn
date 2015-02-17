// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmClrValue : DkmDataContainer
    {
        internal DkmClrValue(
            object value,
            object hostObjectValue,
            DkmClrType type,
            string alias,
            IDkmClrFormatter formatter,
            DkmEvaluationResultFlags evalFlags,
            DkmClrValueFlags valueFlags,
            DkmInspectionContext inspectionContext)
        {
            Debug.Assert(!type.GetLmrType().IsTypeVariables() || (valueFlags == DkmClrValueFlags.Synthetic));
            Debug.Assert((alias == null) || evalFlags.Includes(DkmEvaluationResultFlags.HasObjectId));
            // The "real" DkmClrValue will always have a value of zero for null pointers.
            Debug.Assert(!type.GetLmrType().IsPointer || (value != null));

            _rawValue = value;
            this.HostObjectValue = hostObjectValue;
            this.Type = type;
            _formatter = formatter;
            this.Alias = alias;
            this.EvalFlags = evalFlags;
            this.ValueFlags = valueFlags;
            this.InspectionContext = inspectionContext ?? new DkmInspectionContext(formatter, DkmEvaluationFlags.None, 10);
        }

        public readonly DkmEvaluationResultFlags EvalFlags;
        public readonly DkmClrValueFlags ValueFlags;
        public readonly DkmClrType Type;
        public DkmClrType DeclaredType { get { throw new NotImplementedException(); } }
        public readonly DkmInspectionContext InspectionContext;
        public readonly DkmStackWalkFrame StackFrame;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultAccessType Access;
        public readonly DkmEvaluationResultStorageType StorageType;
        public readonly DkmEvaluationResultTypeModifierFlags TypeModifierFlags;
        public readonly DkmDataAddress Address;
        public readonly object HostObjectValue;
        public readonly string Alias;

        private readonly IDkmClrFormatter _formatter;
        private readonly object _rawValue;

        internal DkmClrValue WithInspectionContext(DkmInspectionContext inspectionContext)
        {
            return new DkmClrValue(
                _rawValue,
                this.HostObjectValue,
                this.Type,
                this.Alias,
                _formatter,
                this.EvalFlags,
                this.ValueFlags,
                inspectionContext);
        }

        public DkmClrValue Dereference()
        {
            if (_rawValue == null)
            {
                throw new InvalidOperationException("Cannot dereference invalid value");
            }
            var elementType = this.Type.GetLmrType().GetElementType();
            var evalFlags = DkmEvaluationResultFlags.None;
            var valueFlags = DkmClrValueFlags.None;
            object value;
            try
            {
                var intPtr = Environment.Is64BitProcess ? new IntPtr((long)_rawValue) : new IntPtr((int)_rawValue);
                value = Dereference(intPtr, elementType);
            }
            catch (Exception e)
            {
                value = e;
                evalFlags |= DkmEvaluationResultFlags.ExceptionThrown;
            }
            var valueType = new DkmClrType(this.Type.RuntimeInstance, (value == null) ? elementType : (TypeImpl)value.GetType());
            return new DkmClrValue(
                value,
                value,
                valueType,
                alias: null,
                formatter: _formatter,
                evalFlags: evalFlags,
                valueFlags: valueFlags,
                inspectionContext: this.InspectionContext);
        }

        public bool IsNull
        {
            get
            {
                if (this.IsError())
                {
                    // Should not be checking value for Error. (Throw rather than
                    // assert since the property may be called during debugging.)
                    throw new InvalidOperationException();
                }
                var lmrType = Type.GetLmrType();
                return ((_rawValue == null) && !lmrType.IsValueType) || (lmrType.IsPointer && (Convert.ToInt64(_rawValue) == 0));
            }
        }

        internal static object GetHostObjectValue(Type lmrType, object rawValue)
        {
            // NOTE: This is just a "for testing purposes" approximation of what the real DkmClrValue.HostObjectValue
            //       will return.  We will need to update this implementation to match the real behavior we add
            //       specialized support for additional types. 
            var typeCode = Metadata.Type.GetTypeCode(lmrType);
            return (lmrType.IsPointer || lmrType.IsEnum || typeCode != TypeCode.DateTime || typeCode != TypeCode.Object)
                ? rawValue
                : null;
        }

        public string GetValueString()
        {
            // The real version does some sort of dynamic dispatch that ultimately calls this method.
            return _formatter.GetValueString(this);
        }

        public bool HasUnderlyingString()
        {
            return _formatter.HasUnderlyingString(this);
        }

        public string GetUnderlyingString()
        {
            return _formatter.GetUnderlyingString(this);
        }

        public string EvaluateToString()
        {
            // This is a rough approximation of the real functionality.  Basically,
            // if object.ToString is not overridden, we return null and it is the
            // caller's responsibility to compute a string.

            var type = this.Type.GetLmrType();
            while (type != null)
            {
                if (type.IsObject() || type.IsValueType())
                {
                    return null;
                }

                // We should check the signature and virtual-ness, but this is
                // close enough for testing.
                if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m.Name == "ToString"))
                {
                    break;
                }

                type = type.BaseType;
            }

            var rawValue = _rawValue;
            Debug.Assert(rawValue != null || this.Type.GetLmrType().IsVoid(), "In our mock system, this should only happen for void.");
            return rawValue == null ? null : rawValue.ToString();
        }

        /// <remarks>
        /// Very simple expression evaluation (may not support all syntax supported by Concord).
        /// </remarks>
        public string EvaluateDebuggerDisplayString(string formatString)
        {
            Debug.Assert(!this.IsNull, "Not supported by VIL");

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            int openPos = -1;
            int length = formatString.Length;
            for (int i = 0; i < length; i++)
            {
                char ch = formatString[i];
                if (ch == '{')
                {
                    if (openPos >= 0)
                    {
                        throw new ArgumentException(string.Format("Nested braces in '{0}'", formatString));
                    }
                    openPos = i;
                }
                else if (ch == '}')
                {
                    if (openPos < 0)
                    {
                        throw new ArgumentException(string.Format("Unmatched closing brace in '{0}'", formatString));
                    }

                    string name = formatString.Substring(openPos + 1, i - openPos - 1);
                    openPos = -1;

                    // Ignore any format specifiers.
                    int commaIndex = name.IndexOf(',');
                    if (commaIndex >= 0)
                    {
                        name = name.Substring(0, commaIndex);
                    }

                    var type = ((TypeImpl)this.Type.GetLmrType()).Type;
                    var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                    DkmClrValue exprValue;
                    var appDomain = this.Type.AppDomain;

                    var field = type.GetField(name, bindingFlags);
                    if (field != null)
                    {
                        var fieldValue = field.GetValue(_rawValue);
                        exprValue = new DkmClrValue(
                            fieldValue,
                            fieldValue,
                            DkmClrType.Create(appDomain, (TypeImpl)((fieldValue == null) ? field.FieldType : fieldValue.GetType())),
                            alias: null,
                            formatter: _formatter,
                            evalFlags: GetEvaluationResultFlags(fieldValue),
                            valueFlags: DkmClrValueFlags.None,
                            inspectionContext: this.InspectionContext);
                    }
                    else
                    {
                        var property = type.GetProperty(name, bindingFlags);
                        if (property != null)
                        {
                            var propertyValue = property.GetValue(_rawValue);
                            exprValue = new DkmClrValue(
                                propertyValue,
                                propertyValue,
                                DkmClrType.Create(appDomain, (TypeImpl)((propertyValue == null) ? property.PropertyType : propertyValue.GetType())),
                                alias: null,
                                formatter: _formatter,
                                evalFlags: GetEvaluationResultFlags(propertyValue),
                                valueFlags: DkmClrValueFlags.None,
                                inspectionContext: this.InspectionContext);
                        }
                        else
                        {
                            var openParenIndex = name.IndexOf('(');
                            if (openParenIndex >= 0)
                            {
                                name = name.Substring(0, openParenIndex);
                            }

                            var method = type.GetMethod(name, bindingFlags);
                            // The real implementation requires parens on method invocations, so
                            // we'll return error if there wasn't at least an open paren...
                            if ((openParenIndex >= 0) && method != null)
                            {
                                var methodValue = method.Invoke(_rawValue, new object[] { });
                                exprValue = new DkmClrValue(
                                    methodValue,
                                    methodValue,
                                    DkmClrType.Create(appDomain, (TypeImpl)((methodValue == null) ? method.ReturnType : methodValue.GetType())),
                                    alias: null,
                                    formatter: _formatter,
                                    evalFlags: GetEvaluationResultFlags(methodValue),
                                    valueFlags: DkmClrValueFlags.None,
                                    inspectionContext: this.InspectionContext);
                            }
                            else
                            {
                                var stringValue = "Problem evaluating expression";
                                var stringType = DkmClrType.Create(appDomain, (TypeImpl)typeof(string));
                                exprValue = new DkmClrValue(
                                    stringValue,
                                    stringValue,
                                    stringType,
                                    alias: null,
                                    formatter: _formatter,
                                    evalFlags: DkmEvaluationResultFlags.None,
                                    valueFlags: DkmClrValueFlags.Error,
                                    inspectionContext: this.InspectionContext);
                            }
                        }
                    }

                    builder.Append(exprValue.GetValueString()); // Re-enter the formatter.
                }
                else if (openPos < 0)
                {
                    builder.Append(ch);
                }
            }

            if (openPos >= 0)
            {
                throw new ArgumentException(string.Format("Unmatched open brace in '{0}'", formatString));
            }

            return pooled.ToStringAndFree();
        }

        public DkmClrValue GetMemberValue(string MemberName, int MemberType, string ParentTypeName)
        {
            var runtime = this.Type.RuntimeInstance;

            var memberValue = runtime.GetMemberValue(this, MemberName);
            if (memberValue != null)
            {
                return memberValue;
            }

            var declaringType = this.Type.GetLmrType();
            if (ParentTypeName != null)
            {
                declaringType = GetAncestorType(declaringType, ParentTypeName);
                Debug.Assert(declaringType != null);
            }

            // Special cases for nullables
            if (declaringType.IsNullable())
            {
                if (MemberName == InternalWellKnownMemberNames.NullableHasValue)
                {
                    // In our mock implementation, RawValue is null for null nullables,
                    // so we have to compute HasValue some other way.
                    var boolValue = _rawValue != null;
                    var boolType = runtime.GetType((TypeImpl)typeof(bool));
                    return new DkmClrValue(
                        boolValue,
                        boolValue,
                        type: boolType,
                        alias: null,
                        formatter: _formatter,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        inspectionContext: this.InspectionContext);
                }
                else if (MemberName == InternalWellKnownMemberNames.NullableValue)
                {
                    // In our mock implementation, RawValue is of type T rather than
                    // Nullable<T> for nullables, so we'll just return that value
                    // (no need to unwrap by getting "value" field).
                    var valueType = runtime.GetType((TypeImpl)_rawValue.GetType());
                    return new DkmClrValue(
                        _rawValue,
                        _rawValue,
                        type: valueType,
                        alias: null,
                        formatter: _formatter,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        inspectionContext: this.InspectionContext);
                }
            }

            Type declaredType;
            object value;
            var evalFlags = DkmEvaluationResultFlags.None;

            const BindingFlags bindingFlags =
                BindingFlags.DeclaredOnly |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            switch ((MemberTypes)MemberType)
            {
                case MemberTypes.Field:
                    var field = declaringType.GetField(MemberName, bindingFlags);
                    declaredType = field.FieldType;
                    if (field.Attributes.HasFlag(FieldAttributes.Literal) || field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    {
                        evalFlags |= DkmEvaluationResultFlags.ReadOnly;
                    }
                    try
                    {
                        value = field.GetValue(_rawValue);
                    }
                    catch (TargetInvocationException e)
                    {
                        var exception = e.InnerException;
                        return new DkmClrValue(
                            exception,
                            exception,
                            type: runtime.GetType((TypeImpl)exception.GetType()),
                            alias: null,
                            formatter: _formatter,
                            evalFlags: evalFlags | DkmEvaluationResultFlags.ExceptionThrown,
                            valueFlags: DkmClrValueFlags.None,
                            inspectionContext: this.InspectionContext);
                    }
                    break;
                case MemberTypes.Property:
                    var property = declaringType.GetProperty(MemberName, bindingFlags);
                    declaredType = property.PropertyType;
                    if (property.GetSetMethod(nonPublic: true) == null)
                    {
                        evalFlags |= DkmEvaluationResultFlags.ReadOnly;
                    }
                    try
                    {
                        value = property.GetValue(_rawValue, bindingFlags, null, null, null);
                    }
                    catch (TargetInvocationException e)
                    {
                        var exception = e.InnerException;
                        return new DkmClrValue(
                            exception,
                            exception,
                            type: runtime.GetType((TypeImpl)exception.GetType()),
                            alias: null,
                            formatter: _formatter,
                            evalFlags: evalFlags | DkmEvaluationResultFlags.ExceptionThrown,
                            valueFlags: DkmClrValueFlags.None,
                            inspectionContext: this.InspectionContext);
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue((MemberTypes)MemberType);
            }

            Type type;
            if (value is Pointer)
            {
                unsafe
                {
                    if (Marshal.SizeOf(typeof(void*)) == 4)
                    {
                        value = (int)Pointer.Unbox(value);
                    }
                    else
                    {
                        value = (long)Pointer.Unbox(value);
                    }
                }
                type = declaredType;
            }
            else if (value == null || declaredType.IsNullable())
            {
                type = declaredType;
            }
            else
            {
                type = (TypeImpl)value.GetType();
            }

            return new DkmClrValue(
                value,
                value,
                type: runtime.GetType(type),
                alias: null,
                formatter: _formatter,
                evalFlags: evalFlags,
                valueFlags: DkmClrValueFlags.None,
                inspectionContext: this.InspectionContext);
        }

        public DkmClrValue GetArrayElement(int[] indices)
        {
            var array = (System.Array)_rawValue;
            var element = array.GetValue(indices);
            var type = DkmClrType.Create(this.Type.AppDomain, (TypeImpl)((element == null) ? array.GetType().GetElementType() : element.GetType()));
            return new DkmClrValue(
                element,
                element,
                type: type,
                alias: null,
                formatter: _formatter,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.None,
                inspectionContext: this.InspectionContext);
        }

        public ReadOnlyCollection<int> ArrayDimensions
        {
            get
            {
                var array = (Array)_rawValue;
                if (array == null)
                {
                    return null;
                }

                int rank = array.Rank;
                var builder = ArrayBuilder<int>.GetInstance(rank);
                for (int i = 0; i < rank; i++)
                {
                    builder.Add(array.GetUpperBound(i) - array.GetLowerBound(i) + 1);
                }
                return builder.ToImmutableAndFree();
            }
        }

        public ReadOnlyCollection<int> ArrayLowerBounds
        {
            get
            {
                var array = (Array)_rawValue;
                if (array == null)
                {
                    return null;
                }

                int rank = array.Rank;
                var builder = ArrayBuilder<int>.GetInstance(rank);
                for (int i = 0; i < rank; i++)
                {
                    builder.Add(array.GetLowerBound(i));
                }
                return builder.ToImmutableAndFree();
            }
        }

        public DkmClrValue InstantiateProxyType(DkmClrType proxyType)
        {
            var lmrType = proxyType.GetLmrType();
            Debug.Assert(!lmrType.IsGenericTypeDefinition);
            const BindingFlags bindingFlags =
                BindingFlags.CreateInstance |
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public;
            var constructor = lmrType.GetConstructors(bindingFlags).Single();
            var value = constructor.Invoke(bindingFlags, null, new[] { _rawValue }, null);
            return new DkmClrValue(
                value,
                value,
                type: proxyType,
                alias: null,
                formatter: _formatter,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.None,
                inspectionContext: this.InspectionContext);
        }

        public DkmClrValue InstantiateResultsViewProxy(DkmClrType enumerableType)
        {
            var appDomain = enumerableType.AppDomain;
            var module = GetModule(appDomain, "System.Core.dll");
            if (module == null)
            {
                return null;
            }

            var typeArgs = enumerableType.GenericArguments;
            Debug.Assert(typeArgs.Count <= 1);
            var proxyTypeName = (typeArgs.Count == 0) ?
                "System.Linq.SystemCore_EnumerableDebugView" :
                "System.Linq.SystemCore_EnumerableDebugView`1";
            DkmClrType proxyType;
            try
            {
                proxyType = module.ResolveTypeName(proxyTypeName, typeArgs);
            }
            catch (ArgumentException)
            {
                // ResolveTypeName throws ArgumentException if type is not found.
                return null;
            }

            return this.InstantiateProxyType(proxyType);
        }

        private static DkmClrModuleInstance GetModule(DkmClrAppDomain appDomain, string moduleName)
        {
            var modules = appDomain.GetClrModuleInstances();
            Debug.Assert(modules.Length > 0);
            foreach (var module in modules)
            {
                if (string.Equals(module.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module;
                }
            }
            return null;
        }

        private static Type GetAncestorType(Type type, string ancestorTypeName)
        {
            if (type.FullName == ancestorTypeName)
            {
                return type;
            }
            // Search interfaces.
            foreach (var @interface in type.GetInterfaces())
            {
                var ancestorType = GetAncestorType(@interface, ancestorTypeName);
                if (ancestorType != null)
                {
                    return ancestorType;
                }
            }
            // Search base type.
            var baseType = type.BaseType;
            if (baseType != null)
            {
                return GetAncestorType(baseType, ancestorTypeName);
            }
            return null;
        }

        private static DkmEvaluationResultFlags GetEvaluationResultFlags(object value)
        {
            if (true.Equals(value))
            {
                return DkmEvaluationResultFlags.BooleanTrue;
            }
            else if (value is bool)
            {
                return DkmEvaluationResultFlags.Boolean;
            }
            else
            {
                return DkmEvaluationResultFlags.None;
            }
        }

        private unsafe static object Dereference(IntPtr ptr, Type elementType)
        {
            // Only handling a subset of types currently.
            switch (Metadata.Type.GetTypeCode(elementType))
            {
                case TypeCode.Int32:
                    return *(int*)ptr;
                case TypeCode.Object:
                    if (ptr == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Dereferencing null");
                    }
                    return Marshal.PtrToStructure(ptr, ((TypeImpl)elementType).Type);
                default:
                    throw new InvalidOperationException();
            }
        }

		public void Close()
		{
		}
    }
}
