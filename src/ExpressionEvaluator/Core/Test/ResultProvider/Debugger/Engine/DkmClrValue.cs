// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Metadata;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmClrValue : DkmDataContainer
    {
        internal DkmClrValue(
            object value,
            object hostObjectValue,
            DkmClrType type,
            string alias,
            DkmEvaluationResultFlags evalFlags,
            DkmClrValueFlags valueFlags,
            DkmEvaluationResultCategory category = default(DkmEvaluationResultCategory),
            DkmEvaluationResultAccessType access = default(DkmEvaluationResultAccessType),
            ulong nativeComPointer = 0)
        {
            Debug.Assert((type == null) || !type.GetLmrType().IsTypeVariables() || (valueFlags == DkmClrValueFlags.Synthetic));
            Debug.Assert((alias == null) || evalFlags.Includes(DkmEvaluationResultFlags.HasObjectId));
            // The "real" DkmClrValue will always have a value of zero for null pointers.
            Debug.Assert((type == null) || !type.GetLmrType().IsPointer || (value != null));

            this.RawValue = value;
            this.HostObjectValue = hostObjectValue;
            this.Type = type;
            this.Alias = alias;
            this.EvalFlags = evalFlags;
            this.ValueFlags = valueFlags;
            this.Category = category;
            this.Access = access;
            this.NativeComPointer = nativeComPointer;
        }

        public readonly DkmEvaluationResultFlags EvalFlags;
        public readonly DkmClrValueFlags ValueFlags;
        public readonly DkmClrType Type;
        public readonly DkmStackWalkFrame StackFrame;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultAccessType Access;
        public readonly DkmEvaluationResultStorageType StorageType;
        public readonly DkmEvaluationResultTypeModifierFlags TypeModifierFlags;
        public readonly DkmDataAddress Address;
        public readonly object HostObjectValue;
        public readonly string Alias;
        public readonly ulong NativeComPointer;

        internal readonly object RawValue;

        public void Close()
        {
        }

        public DkmClrValue Dereference(DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            if (RawValue == null)
            {
                throw new InvalidOperationException("Cannot dereference invalid value");
            }
            var elementType = this.Type.GetLmrType().GetElementType();
            var evalFlags = DkmEvaluationResultFlags.None;
            var valueFlags = DkmClrValueFlags.None;
            object value;
            try
            {
                var intPtr = Environment.Is64BitProcess ? new IntPtr((long)RawValue) : new IntPtr((int)RawValue);
                value = Dereference(intPtr, elementType);
            }
            catch (Exception e)
            {
                value = e;
                evalFlags |= DkmEvaluationResultFlags.ExceptionThrown;
            }
            var valueType = new DkmClrType(this.Type.RuntimeInstance, (value == null || elementType.IsPointer) ? elementType : (TypeImpl)value.GetType());
            return new DkmClrValue(
                value,
                value,
                valueType,
                alias: null,
                evalFlags: evalFlags,
                valueFlags: valueFlags,
                category: DkmEvaluationResultCategory.Other,
                access: DkmEvaluationResultAccessType.None);
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
                return ((RawValue == null) && !lmrType.IsValueType) || (lmrType.IsPointer && (Convert.ToInt64(RawValue) == 0));
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

        public string GetValueString(DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            return inspectionContext.InspectionSession.InvokeFormatter(this, MethodId.GetValueString, f => f.GetValueString(this, inspectionContext, formatSpecifiers));
        }

        public bool HasUnderlyingString(DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            return inspectionContext.InspectionSession.InvokeFormatter(this, MethodId.HasUnderlyingString, f => f.HasUnderlyingString(this, inspectionContext));
        }

        public string GetUnderlyingString(DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            return inspectionContext.InspectionSession.InvokeFormatter(this, MethodId.GetUnderlyingString, f => f.GetUnderlyingString(this, inspectionContext));
        }

        public void GetResult(
            DkmWorkList WorkList,
            DkmClrType DeclaredType,
            DkmClrCustomTypeInfo CustomTypeInfo,
            DkmInspectionContext InspectionContext,
            ReadOnlyCollection<string> FormatSpecifiers,
            string ResultName,
            string ResultFullName,
            DkmCompletionRoutine<DkmEvaluationAsyncResult> CompletionRoutine)
        {
            InspectionContext.InspectionSession.InvokeResultProvider(
                this,
                MethodId.GetResult,
                r =>
                {
                    r.GetResult(
                        this,
                        WorkList,
                        DeclaredType,
                        CustomTypeInfo,
                        InspectionContext,
                        FormatSpecifiers,
                        ResultName,
                        ResultFullName,
                        CompletionRoutine);
                    return (object)null;
                });
        }

        public string EvaluateToString(DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

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

            var rawValue = RawValue;
            Debug.Assert(rawValue != null || this.Type.GetLmrType().IsVoid(), "In our mock system, this should only happen for void.");
            return rawValue?.ToString();
        }

        /// <remarks>
        /// Very simple expression evaluation (may not support all syntax supported by Concord).
        /// </remarks>
        public void EvaluateDebuggerDisplayString(DkmWorkList workList, DkmInspectionContext inspectionContext, DkmClrType targetType, string formatString, DkmCompletionRoutine<DkmEvaluateDebuggerDisplayStringAsyncResult> completionRoutine)
        {
            Debug.Assert(!this.IsNull, "Not supported by VIL");

            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

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

                    var formatSpecifiers = Formatter.NoFormatSpecifiers;
                    int commaIndex = name.IndexOf(',');
                    if (commaIndex >= 0)
                    {
                        var rawFormatSpecifiers = name.Substring(commaIndex + 1).Split(',');
                        var trimmedFormatSpecifiers = ArrayBuilder<string>.GetInstance(rawFormatSpecifiers.Length);
                        trimmedFormatSpecifiers.AddRange(rawFormatSpecifiers.Select(fs => fs.Trim()));
                        formatSpecifiers = trimmedFormatSpecifiers.ToImmutableAndFree();
                        foreach (var formatSpecifier in formatSpecifiers)
                        {
                            if (formatSpecifier == "nq")
                            {
                                inspectionContext = new DkmInspectionContext(inspectionContext.InspectionSession, inspectionContext.EvaluationFlags | DkmEvaluationFlags.NoQuotes, inspectionContext.Radix, inspectionContext.RuntimeInstance);
                            }
                            // If we need to support additional format specifiers, add them here...
                        }

                        name = name.Substring(0, commaIndex);
                    }

                    var type = ((TypeImpl)this.Type.GetLmrType()).Type;
                    const System.Reflection.BindingFlags bindingFlags =
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Static;

                    DkmClrValue exprValue;
                    var appDomain = this.Type.AppDomain;

                    var field = type.GetField(name, bindingFlags);
                    if (field != null)
                    {
                        var fieldValue = field.GetValue(RawValue);
                        exprValue = new DkmClrValue(
                            fieldValue,
                            fieldValue,
                            DkmClrType.Create(appDomain, (TypeImpl)((fieldValue == null) ? field.FieldType : fieldValue.GetType())),
                            alias: null,
                            evalFlags: GetEvaluationResultFlags(fieldValue),
                            valueFlags: DkmClrValueFlags.None);
                    }
                    else
                    {
                        var property = type.GetProperty(name, bindingFlags);
                        if (property != null)
                        {
                            var propertyValue = property.GetValue(RawValue);
                            exprValue = new DkmClrValue(
                                propertyValue,
                                propertyValue,
                                DkmClrType.Create(appDomain, (TypeImpl)((propertyValue == null) ? property.PropertyType : propertyValue.GetType())),
                                alias: null,
                                evalFlags: GetEvaluationResultFlags(propertyValue),
                                valueFlags: DkmClrValueFlags.None);
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
                                var methodValue = method.Invoke(RawValue, []);
                                exprValue = new DkmClrValue(
                                    methodValue,
                                    methodValue,
                                    DkmClrType.Create(appDomain, (TypeImpl)((methodValue == null) ? method.ReturnType : methodValue.GetType())),
                                    alias: null,
                                    evalFlags: GetEvaluationResultFlags(methodValue),
                                    valueFlags: DkmClrValueFlags.None);
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
                                    evalFlags: DkmEvaluationResultFlags.None,
                                    valueFlags: DkmClrValueFlags.Error);
                            }
                        }
                    }

                    builder.Append(exprValue.GetValueString(inspectionContext, formatSpecifiers)); // Re-enter the formatter.
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

            workList.AddWork(() => completionRoutine(new DkmEvaluateDebuggerDisplayStringAsyncResult(pooled.ToStringAndFree())));
        }

        public DkmClrValue GetMemberValue(string MemberName, int MemberType, string ParentTypeName, DkmInspectionContext InspectionContext)
        {
            if (InspectionContext == null)
            {
                throw new ArgumentNullException(nameof(InspectionContext));
            }

            if (this.IsError())
            {
                throw new InvalidOperationException();
            }

            var runtime = this.Type.RuntimeInstance;
            var getMemberValue = runtime.GetMemberValue;
            if (getMemberValue != null)
            {
                var memberValue = getMemberValue(this, MemberName);
                if (memberValue != null)
                {
                    return memberValue;
                }
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
                    var boolValue = RawValue != null;
                    var boolType = runtime.GetType((TypeImpl)typeof(bool));
                    return new DkmClrValue(
                        boolValue,
                        boolValue,
                        type: boolType,
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Property,
                        access: DkmEvaluationResultAccessType.Public);
                }
                else if (MemberName == InternalWellKnownMemberNames.NullableValue)
                {
                    // In our mock implementation, RawValue is of type T rather than
                    // Nullable<T> for nullables, so we'll just return that value
                    // (no need to unwrap by getting "value" field).
                    var valueType = runtime.GetType((TypeImpl)RawValue.GetType());
                    return new DkmClrValue(
                        RawValue,
                        RawValue,
                        type: valueType,
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Property,
                        access: DkmEvaluationResultAccessType.Public);
                }
            }

            Type declaredType;
            object value;
            var evalFlags = DkmEvaluationResultFlags.None;
            var category = DkmEvaluationResultCategory.Other;
            var access = DkmEvaluationResultAccessType.None;

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
                    category = DkmEvaluationResultCategory.Data;
                    access = GetFieldAccess(field);
                    if (field.Attributes.HasFlag(System.Reflection.FieldAttributes.Literal) || field.Attributes.HasFlag(System.Reflection.FieldAttributes.InitOnly))
                    {
                        evalFlags |= DkmEvaluationResultFlags.ReadOnly;
                    }
                    try
                    {
                        value = field.GetValue(RawValue);
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        var exception = e.InnerException;
                        return new DkmClrValue(
                            exception,
                            exception,
                            type: runtime.GetType((TypeImpl)exception.GetType()),
                            alias: null,
                            evalFlags: evalFlags | DkmEvaluationResultFlags.ExceptionThrown,
                            valueFlags: DkmClrValueFlags.None,
                            category: category,
                            access: access);
                    }
                    break;
                case MemberTypes.Property:
                    var property = declaringType.GetProperty(MemberName, bindingFlags);
                    declaredType = property.PropertyType;
                    category = DkmEvaluationResultCategory.Property;
                    access = GetPropertyAccess(property);
                    if (property.GetSetMethod(nonPublic: true) == null)
                    {
                        evalFlags |= DkmEvaluationResultFlags.ReadOnly;
                    }
                    try
                    {
                        value = property.GetValue(RawValue, bindingFlags, null, null, null);
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        var exception = e.InnerException;
                        return new DkmClrValue(
                            exception,
                            exception,
                            type: runtime.GetType((TypeImpl)exception.GetType()),
                            alias: null,
                            evalFlags: evalFlags | DkmEvaluationResultFlags.ExceptionThrown,
                            valueFlags: DkmClrValueFlags.None,
                            category: category,
                            access: access);
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue((MemberTypes)MemberType);
            }

            Type type;
            if (value is System.Reflection.Pointer)
            {
                value = UnboxPointer(value);
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
                evalFlags: evalFlags,
                valueFlags: DkmClrValueFlags.None,
                category: category,
                access: access);
        }

        internal static unsafe object UnboxPointer(object value)
        {
            unsafe
            {
                if (Environment.Is64BitProcess)
                {
                    return (long)System.Reflection.Pointer.Unbox(value);
                }
                else
                {
                    return (int)System.Reflection.Pointer.Unbox(value);
                }
            }
        }

        public DkmClrValue GetArrayElement(int[] indices, DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            object element;
            System.Type elementType;
            if (RawValue is Array array)
            {
                element = array.GetValue(indices);
                elementType = (element == null) ? array.GetType().GetElementType() : element.GetType();
            }
            else
            {
#if NET8_0_OR_GREATER
                // Might be an inline array struct
                if (indices.Length == 1 && InlineArrayHelpers.IsInlineArray(Type.GetLmrType()))
                {
                    // Since reflection is inadequate to dynamically access inline array elements,
                    // we have to assume it's the special SampleInlineArray type we define for testing and 
                    // cast appropriately.
                    element = RawValue switch
                    {
                        SampleInlineArray<int> intInlineArray => intInlineArray[indices[0]],
                        // Add more cases here for other types as needed for testing
                        _ => throw new InvalidOperationException($"Missing cast case for SampleInlineArray"),
                    };

                    var fields = RawValue.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                              System.Reflection.BindingFlags.NonPublic |
                                                              System.Reflection.BindingFlags.Instance |
                                                              System.Reflection.BindingFlags.DeclaredOnly);
                    elementType = fields[0].FieldType;
                }
                else
                {
                    throw new InvalidOperationException("Not an array");
                }
#else
                throw new InvalidOperationException("Not an array");
#endif
            }

            var type = DkmClrType.Create(this.Type.AppDomain, (TypeImpl)(elementType));
            return new DkmClrValue(
                element,
                element,
                type: type,
                alias: null,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.None);
        }

        public ReadOnlyCollection<int> ArrayDimensions
        {
            get
            {
                var array = (Array)RawValue;
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
                var array = (Array)RawValue;
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

        public DkmClrValue InstantiateProxyType(DkmInspectionContext inspectionContext, DkmClrType proxyType)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            var lmrType = proxyType.GetLmrType();
            Debug.Assert(!lmrType.IsGenericTypeDefinition);
            const BindingFlags bindingFlags =
                BindingFlags.CreateInstance |
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public;
            var constructor = lmrType.GetConstructors(bindingFlags).Single();
            var value = constructor.Invoke(bindingFlags, null, new[] { RawValue }, null);
            return new DkmClrValue(
                value,
                value,
                type: proxyType,
                alias: null,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.None);
        }

        private static readonly ReadOnlyCollection<DkmClrType> s_noArguments = ArrayBuilder<DkmClrType>.GetInstance(0).ToImmutableAndFree();
        public DkmClrValue InstantiateDynamicViewProxy(DkmInspectionContext inspectionContext)
        {
            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            var module = new DkmClrModuleInstance(
                this.Type.AppDomain.RuntimeInstance,
                typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly,
                new DkmModule("Microsoft.CSharp.dll"));
            var proxyType = module.ResolveTypeName(
                "Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView",
                s_noArguments);
            return this.InstantiateProxyType(inspectionContext, proxyType);
        }

        public DkmClrValue InstantiateResultsViewProxy(DkmInspectionContext inspectionContext, DkmClrType enumerableType)
        {
            if (EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown))
            {
                throw new InvalidOperationException();
            }

            if (inspectionContext == null)
            {
                throw new ArgumentNullException(nameof(inspectionContext));
            }

            var appDomain = enumerableType.AppDomain;
            var module = GetModule(appDomain, "System.Core.dll");
            if (module == null)
            {
                return null;
            }

            var typeArgs = enumerableType.GenericArguments;
            Debug.Assert(typeArgs.Count <= 1);
            var proxyTypeName = (typeArgs.Count == 0)
                ? "System.Linq.SystemCore_EnumerableDebugView"
                : "System.Linq.SystemCore_EnumerableDebugView`1";
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

            return this.InstantiateProxyType(inspectionContext, proxyType);
        }

        private static DkmClrModuleInstance GetModule(DkmClrAppDomain appDomain, string moduleName)
        {
            var modules = appDomain.GetClrModuleInstances();
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

        private static unsafe object Dereference(IntPtr ptr, Type elementType)
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
                    var destinationType = elementType.IsPointer
                        ? (Environment.Is64BitProcess ? typeof(long) : typeof(int))
                        : ((TypeImpl)elementType).Type;
                    return Marshal.PtrToStructure(ptr, destinationType);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static DkmEvaluationResultAccessType GetFieldAccess(Microsoft.VisualStudio.Debugger.Metadata.FieldInfo field)
        {
            if (field.IsPrivate)
            {
                return DkmEvaluationResultAccessType.Private;
            }
            else if (field.IsFamily)
            {
                return DkmEvaluationResultAccessType.Protected;
            }
            else if (field.IsAssembly)
            {
                return DkmEvaluationResultAccessType.Internal;
            }
            else
            {
                return DkmEvaluationResultAccessType.Public;
            }
        }

        private static DkmEvaluationResultAccessType GetPropertyAccess(Microsoft.VisualStudio.Debugger.Metadata.PropertyInfo property)
        {
            return GetMethodAccess(property.GetGetMethod(nonPublic: true));
        }

        private static DkmEvaluationResultAccessType GetMethodAccess(Microsoft.VisualStudio.Debugger.Metadata.MethodBase method)
        {
            if (method.IsPrivate)
            {
                return DkmEvaluationResultAccessType.Private;
            }
            else if (method.IsFamily)
            {
                return DkmEvaluationResultAccessType.Protected;
            }
            else if (method.IsAssembly)
            {
                return DkmEvaluationResultAccessType.Internal;
            }
            else
            {
                return DkmEvaluationResultAccessType.Public;
            }
        }
    }
}
