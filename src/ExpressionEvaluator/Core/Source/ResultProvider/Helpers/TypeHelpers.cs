// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using Roslyn.Utilities;
using MethodAttributes = System.Reflection.MethodAttributes;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class TypeHelpers
    {
        internal const BindingFlags MemberBindingFlags = BindingFlags.Public |
                                                         BindingFlags.NonPublic |
                                                         BindingFlags.Instance |
                                                         BindingFlags.Static |
                                                         BindingFlags.DeclaredOnly;

        internal static void AppendTypeMembers(
            this Type type,
            ArrayBuilder<MemberAndDeclarationInfo> includedMembers,
            Predicate<MemberInfo> predicate,
            Type declaredType,
            DkmClrAppDomain appDomain,
            bool includeInherited,
            bool hideNonPublic,
            bool isProxyType,
            bool includeCompilerGenerated)
        {
            Debug.Assert(!type.IsInterface);

            var memberLocation = DeclarationInfo.FromSubTypeOfDeclaredType;
            var previousDeclarationMap = includeInherited ? new Dictionary<string, DeclarationInfo>() : null;

            int inheritanceLevel = 0;
            while (!type.IsObject())
            {
                if (type.Equals(declaredType))
                {
                    Debug.Assert(memberLocation == DeclarationInfo.FromSubTypeOfDeclaredType);
                    memberLocation = DeclarationInfo.FromDeclaredTypeOrBase;
                }

                // Get the state from DebuggerBrowsableAttributes for the members of the current type.
                var browsableState = DkmClrType.Create(appDomain, type).GetDebuggerBrowsableAttributeState();

                // Hide non-public members if hideNonPublic is specified (intended to reflect the
                // DkmInspectionContext's DkmEvaluationFlags), and the type is from an assembly
                // with no symbols.
                var hideNonPublicBehavior = DeclarationInfo.None;
                if (hideNonPublic)
                {
                    var moduleInstance = appDomain.FindClrModuleInstance(type.Module.ModuleVersionId);
                    if (moduleInstance == null || moduleInstance.Module == null)
                    {
                        // Synthetic module or no symbols loaded.
                        hideNonPublicBehavior = DeclarationInfo.HideNonPublic;
                    }
                }

                foreach (var member in type.GetMembers(MemberBindingFlags))
                {
                    var memberName = member.Name;
                    if (!includeCompilerGenerated && memberName.IsCompilerGenerated())
                    {
                        continue;
                    }

                    // The native EE shows proxy members regardless of accessibility if they have a
                    // DebuggerBrowsable attribute of any value. Match that behaviour here.
                    if (!isProxyType || browsableState == null || !browsableState.ContainsKey(memberName))
                    {
                        if (!predicate(member))
                        {
                            continue;
                        }
                    }

                    // This represents information about the immediately preceding (more derived)
                    // declaration with the same name as the current member.
                    var previousDeclaration = DeclarationInfo.None;
                    var memberNameAlreadySeen = false;
                    if (includeInherited)
                    {
                        memberNameAlreadySeen = previousDeclarationMap.TryGetValue(memberName, out previousDeclaration);
                        if (memberNameAlreadySeen)
                        {
                            // There was a name conflict, so we'll need to include the declaring
                            // type of the member to disambiguate.
                            previousDeclaration |= DeclarationInfo.IncludeTypeInMemberName;
                        }

                        // Update previous member with name hiding (casting) and declared location information for next time.
                        previousDeclarationMap[memberName] =
                            (previousDeclaration & ~(DeclarationInfo.RequiresExplicitCast |
                                DeclarationInfo.FromSubTypeOfDeclaredType)) |
                            member.AccessingBaseMemberWithSameNameRequiresExplicitCast() |
                            memberLocation;
                    }

                    Debug.Assert(memberNameAlreadySeen != (previousDeclaration == DeclarationInfo.None));

                    // Decide whether to include this member in the list of members to display.
                    if (!memberNameAlreadySeen || previousDeclaration.IsSet(DeclarationInfo.RequiresExplicitCast))
                    {
                        DkmClrDebuggerBrowsableAttributeState? browsableStateValue = null;
                        if (browsableState != null)
                        {
                            DkmClrDebuggerBrowsableAttributeState value;
                            if (browsableState.TryGetValue(memberName, out value))
                            {
                                browsableStateValue = value;
                            }
                        }

                        if (memberLocation.IsSet(DeclarationInfo.FromSubTypeOfDeclaredType))
                        {
                            // If the current type is a sub-type of the declared type, then
                            // we always need to insert a cast to access the member
                            previousDeclaration |= DeclarationInfo.RequiresExplicitCast;
                        }
                        else if (previousDeclaration.IsSet(DeclarationInfo.FromSubTypeOfDeclaredType))
                        {
                            // If the immediately preceding member (less derived) was
                            // declared on a sub-type of the declared type, then we'll
                            // ignore the casting bit.  Accessing a member through the
                            // declared type is the same as casting to that type, so
                            // the cast would be redundant.
                            previousDeclaration &= ~DeclarationInfo.RequiresExplicitCast;
                        }

                        previousDeclaration |= hideNonPublicBehavior;

                        includedMembers.Add(new MemberAndDeclarationInfo(member, browsableStateValue, previousDeclaration, inheritanceLevel));
                    }
                }

                if (!includeInherited)
                {
                    break;
                }

                type = type.BaseType;
                inheritanceLevel++;
            }

            includedMembers.Sort(MemberAndDeclarationInfo.Comparer);
        }

        private static DeclarationInfo AccessingBaseMemberWithSameNameRequiresExplicitCast(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return DeclarationInfo.RequiresExplicitCast;
                case MemberTypes.Property:
                    var getMethod = GetNonIndexerGetMethod((PropertyInfo)member);
                    if ((getMethod != null) &&
                        (!getMethod.IsVirtual || ((getMethod.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot)))
                    {
                        return DeclarationInfo.RequiresExplicitCast;
                    }
                    return DeclarationInfo.None;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.MemberType);
            }
        }

        internal static bool IsVisibleMember(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return true;
                case MemberTypes.Property:
                    return GetNonIndexerGetMethod((PropertyInfo)member) != null;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the member is public or protected.
        /// </summary>
        internal static bool IsPublic(this MemberInfo member)
        {
            // Matches native EE which includes protected members.
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    {
                        var field = (FieldInfo)member;
                        var attributes = field.Attributes;
                        return ((attributes & System.Reflection.FieldAttributes.Public) == System.Reflection.FieldAttributes.Public) ||
                            ((attributes & System.Reflection.FieldAttributes.Family) == System.Reflection.FieldAttributes.Family);
                    }
                case MemberTypes.Property:
                    {
                        // Native EE uses the accessibility of the property rather than getter
                        // so "public object P { private get; set; }" is treated as public.
                        // Instead, we drop properties if the getter is inaccessible.
                        var getMethod = GetNonIndexerGetMethod((PropertyInfo)member);
                        if (getMethod == null)
                        {
                            return false;
                        }
                        var attributes = getMethod.Attributes;
                        return ((attributes & System.Reflection.MethodAttributes.Public) == System.Reflection.MethodAttributes.Public) ||
                            ((attributes & System.Reflection.MethodAttributes.Family) == System.Reflection.MethodAttributes.Family);
                    }
                default:
                    return false;
            }
        }

        private static MethodInfo GetNonIndexerGetMethod(PropertyInfo property)
        {
            return (property.GetIndexParameters().Length == 0) ?
                property.GetGetMethod(nonPublic: true) :
                null;
        }

        internal static bool IsBoolean(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.Boolean;
        }

        internal static bool IsCharacter(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.Char;
        }

        internal static bool IsDecimal(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.Decimal;
        }

        internal static bool IsDateTime(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.DateTime;
        }

        internal static bool IsObject(this Type type)
        {
            bool result = type is { IsClass: true, IsPointer: false } && (type.BaseType == null);
            Debug.Assert(result == type.IsMscorlibType("System", "Object"));
            return result;
        }

        internal static bool IsValueType(this Type type)
        {
            return type.IsMscorlibType("System", "ValueType");
        }

        internal static bool IsString(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.String;
        }

        internal static bool IsVoid(this Type type)
        {
            return type.IsMscorlibType("System", "Void") && !type.IsGenericType;
        }

        internal static bool IsIEnumerable(this Type type)
        {
            return type.IsMscorlibType("System.Collections", "IEnumerable");
        }

        internal static bool IsIntPtr(this Type type)
            => type.IsMscorlibType("System", "IntPtr");

        internal static bool IsUIntPtr(this Type type)
            => type.IsMscorlibType("System", "UIntPtr");

        internal static bool IsIEnumerableOfT(this Type type)
        {
            return type.IsMscorlibType("System.Collections.Generic", "IEnumerable`1");
        }

        internal static bool IsTypeVariables(this Type type)
        {
            return type.IsType(null, "<>c__TypeVariables");
        }

        internal static bool IsComObject(this Type type)
        {
            return type.IsType("System", "__ComObject");
        }

        internal static bool IsDynamicProperty(this Type type)
        {
            return type.IsType("Microsoft.CSharp.RuntimeBinder", "DynamicProperty");
        }

        internal static bool IsDynamicDebugViewEmptyException(this Type type)
        {
            return type.IsType("Microsoft.CSharp.RuntimeBinder", "DynamicDebugViewEmptyException");
        }

        internal static bool IsIDynamicMetaObjectProvider(this Type type)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.IsType("System.Dynamic", "IDynamicMetaObjectProvider"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns type argument if the type is
        /// Nullable&lt;T&gt;, otherwise null.
        /// </summary>
        internal static Type GetNullableTypeArgument(this Type type)
        {
            if (type.IsMscorlibType("System", "Nullable`1"))
            {
                var typeArgs = type.GetGenericArguments();
                if (typeArgs.Length == 1)
                {
                    return typeArgs[0];
                }
            }
            return null;
        }

        internal static bool IsNullable(this Type type)
        {
            return type.GetNullableTypeArgument() != null;
        }

        internal static DkmClrValue GetFieldValue(this DkmClrValue value, string name, DkmInspectionContext inspectionContext)
        {
            return value.GetMemberValue(name, (int)MemberTypes.Field, ParentTypeName: null, InspectionContext: inspectionContext);
        }

        internal static DkmClrValue GetPropertyValue(this DkmClrValue value, string name, DkmInspectionContext inspectionContext)
        {
            return value.GetMemberValue(name, (int)MemberTypes.Property, ParentTypeName: null, InspectionContext: inspectionContext);
        }

        internal static DkmClrValue GetNullableValue(this DkmClrValue value, Type nullableTypeArg, DkmInspectionContext inspectionContext)
        {
            var valueType = value.Type.GetLmrType();
            if (valueType.Equals(nullableTypeArg))
            {
                return value;
            }
            return value.GetNullableValue(inspectionContext);
        }

        internal static DkmClrValue GetNullableValue(this DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            Debug.Assert(value.Type.GetLmrType().IsNullable());

            var hasValue = value.GetFieldValue(InternalWellKnownMemberNames.NullableHasValue, inspectionContext);
            if (object.Equals(hasValue.HostObjectValue, false))
            {
                return null;
            }

            return value.GetFieldValue(InternalWellKnownMemberNames.NullableValue, inspectionContext);
        }

        internal const int TupleFieldRestPosition = 8;
        private const string TupleTypeNamePrefix = "ValueTuple`";
        private const string TupleFieldItemNamePrefix = "Item";
        internal const string TupleFieldRestName = "Rest";

        // See NamedTypeSymbol.IsTupleCompatible.
        internal static bool IsTupleCompatible(this Type type, out int cardinality)
        {
            if (type.IsGenericType &&
                AreNamesEqual(type.Namespace, "System") &&
                type.Name.StartsWith(TupleTypeNamePrefix, StringComparison.Ordinal))
            {
                var typeArguments = type.GetGenericArguments();
                int n = typeArguments.Length;
                if ((n > 0) && (n <= TupleFieldRestPosition))
                {
                    if (!AreNamesEqual(type.Name, TupleTypeNamePrefix + n))
                    {
                        cardinality = 0;
                        return false;
                    }

                    if (n < TupleFieldRestPosition)
                    {
                        cardinality = n;
                        return true;
                    }

                    var restType = typeArguments[n - 1];
                    int restCardinality;
                    if (restType.IsTupleCompatible(out restCardinality))
                    {
                        cardinality = n - 1 + restCardinality;
                        return true;
                    }
                }
            }

            cardinality = 0;
            return false;
        }

        // Returns cardinality if tuple type, otherwise 0.
        internal static int GetTupleCardinalityIfAny(this Type type)
        {
            int cardinality;
            type.IsTupleCompatible(out cardinality);
            return cardinality;
        }

        internal static FieldInfo GetTupleField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        internal static string GetTupleFieldName(int index)
        {
            Debug.Assert(index >= 0);
            return TupleFieldItemNamePrefix + (index + 1);
        }

        internal static bool TryGetTupleFieldValues(this DkmClrValue tuple, int cardinality, ArrayBuilder<string> values, DkmInspectionContext inspectionContext)
        {
            while (true)
            {
                var type = tuple.Type.GetLmrType();
                int n = Math.Min(cardinality, TupleFieldRestPosition - 1);
                for (int index = 0; index < n; index++)
                {
                    var fieldName = GetTupleFieldName(index);
                    var fieldInfo = type.GetTupleField(fieldName);
                    if (fieldInfo == null)
                    {
                        return false;
                    }
                    var value = tuple.GetFieldValue(fieldName, inspectionContext);
                    var str = value.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
                    values.Add(str);
                }
                cardinality -= n;
                if (cardinality == 0)
                {
                    return true;
                }
                var restInfo = type.GetTupleField(TypeHelpers.TupleFieldRestName);
                if (restInfo == null)
                {
                    return false;
                }
                tuple = tuple.GetFieldValue(TupleFieldRestName, inspectionContext);
            }
        }

        internal static Type GetBaseTypeOrNull(this Type underlyingType, DkmClrAppDomain appDomain, out DkmClrType type)
        {
            Debug.Assert((underlyingType.BaseType != null) || underlyingType.IsPointer || underlyingType.IsArray, "BaseType should only return null if the underlyingType is a pointer or array.");

            underlyingType = underlyingType.BaseType;
            type = (underlyingType != null) ? DkmClrType.Create(appDomain, underlyingType) : null;

            return underlyingType;
        }

        /// <summary>
        /// Get the first attribute from <see cref="DkmClrType.GetEvalAttributes()"/> (including inherited attributes)
        /// that is of type T, as well as the type that it targeted.
        /// </summary>
        internal static bool TryGetEvalAttribute<T>(this DkmClrType type, out DkmClrType attributeTarget, out T evalAttribute)
            where T : DkmClrEvalAttribute
        {
            attributeTarget = null;
            evalAttribute = null;

            var appDomain = type.AppDomain;
            var underlyingType = type.GetLmrType();
            while ((underlyingType != null) && !underlyingType.IsObject())
            {
                foreach (var attribute in type.GetEvalAttributes())
                {
                    evalAttribute = attribute as T;
                    if (evalAttribute != null)
                    {
                        attributeTarget = type;
                        return true;
                    }
                }

                underlyingType = underlyingType.GetBaseTypeOrNull(appDomain, out type);
            }

            return false;
        }

        /// <summary>
        /// Returns the set of DebuggerBrowsableAttribute state for the
        /// members of the type, indexed by member name, or null if there
        /// are no DebuggerBrowsableAttributes on members of the type.
        /// </summary>
        private static Dictionary<string, DkmClrDebuggerBrowsableAttributeState> GetDebuggerBrowsableAttributeState(this DkmClrType type)
        {
            Dictionary<string, DkmClrDebuggerBrowsableAttributeState> result = null;
            foreach (var attribute in type.GetEvalAttributes())
            {
                var browsableAttribute = attribute as DkmClrDebuggerBrowsableAttribute;
                if (browsableAttribute == null)
                {
                    continue;
                }
                if (result == null)
                {
                    result = new Dictionary<string, DkmClrDebuggerBrowsableAttributeState>();
                }

                // There can be multiple same attributes for derived classes.
                // Debugger provides attributes starting from derived classes and then up to base ones.
                // We should use derived attributes if there is more than one instance.
                if (!result.ContainsKey(browsableAttribute.TargetMember))
                {
                    result.Add(browsableAttribute.TargetMember, browsableAttribute.State);
                }
            }
            return result;
        }

        /// <summary>
        /// Extracts information from the first <see cref="DebuggerDisplayAttribute"/> on the runtime type of <paramref name="value"/>, if there is one.
        /// </summary>
        internal static bool TryGetDebuggerDisplayInfo(this DkmClrValue value, out DebuggerDisplayInfo displayInfo)
        {
            displayInfo = default(DebuggerDisplayInfo);

            // The native EE does not consider DebuggerDisplayAttribute
            // on null or error instances.
            if (value.IsError() || value.IsNull)
            {
                return false;
            }

            var clrType = value.Type;

            DkmClrType attributeTarget;
            DkmClrDebuggerDisplayAttribute attribute;
            if (clrType.TryGetEvalAttribute(out attributeTarget, out attribute)) // First, as in dev12.
            {
                displayInfo = new DebuggerDisplayInfo(attributeTarget, attribute);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the array of <see cref="DkmCustomUIVisualizerInfo"/> objects of the type from its <see cref="DkmClrDebuggerVisualizerAttribute"/> attributes,
        /// or null if the type has no [DebuggerVisualizer] attributes associated with it.
        /// </summary>
        internal static DkmCustomUIVisualizerInfo[] GetDebuggerCustomUIVisualizerInfo(this DkmClrType type)
        {
            var builder = ArrayBuilder<DkmCustomUIVisualizerInfo>.GetInstance();

            var appDomain = type.AppDomain;
            var underlyingType = type.GetLmrType();
            while ((underlyingType != null) && !underlyingType.IsObject())
            {
                foreach (var attribute in type.GetEvalAttributes())
                {
                    var visualizerAttribute = attribute as DkmClrDebuggerVisualizerAttribute;
                    if (visualizerAttribute == null)
                    {
                        continue;
                    }

                    builder.Add(DkmCustomUIVisualizerInfo.Create((uint)builder.Count,
                        visualizerAttribute.VisualizerDescription,
                        visualizerAttribute.VisualizerDescription,
                        // ClrCustomVisualizerVSHost is a registry entry that specifies the CLSID of the
                        // IDebugCustomViewer class that will be instantiated to display the custom visualizer.
                        "ClrCustomVisualizerVSHost",
                        visualizerAttribute.UISideVisualizerTypeName,
                        visualizerAttribute.UISideVisualizerAssemblyName,
                        visualizerAttribute.UISideVisualizerAssemblyLocation,
                        visualizerAttribute.DebuggeeSideVisualizerTypeName,
                        visualizerAttribute.DebuggeeSideVisualizerAssemblyName));
                }

                underlyingType = underlyingType.GetBaseTypeOrNull(appDomain, out type);
            }

            var result = (builder.Count > 0) ? builder.ToArray() : null;
            builder.Free();
            return result;
        }

        internal static DkmClrType GetProxyType(this DkmClrType type)
        {
            // CONSIDER: If needed, we could probably compute a new DynamicAttribute for
            // the proxy type based on the DynamicAttribute of the argument.
            DkmClrType attributeTarget;
            DkmClrDebuggerTypeProxyAttribute attribute;
            if (type.TryGetEvalAttribute(out attributeTarget, out attribute))
            {
                var targetedType = attributeTarget.GetLmrType();
                var proxyType = attribute.ProxyType;
                var underlyingProxy = proxyType.GetLmrType();
                if (underlyingProxy.IsGenericType && targetedType.IsGenericType)
                {
                    var typeArgs = targetedType.GetGenericArguments();

                    // Drop the proxy type if the arity does not match.
                    if (typeArgs.Length != underlyingProxy.GetGenericArguments().Length)
                    {
                        return null;
                    }

                    // Substitute target type arguments for proxy type arguments.
                    var constructedProxy = underlyingProxy.Substitute(underlyingProxy, typeArgs);
                    proxyType = DkmClrType.Create(type.AppDomain, constructedProxy);
                }

                return proxyType;
            }

            return null;
        }

        /// <summary>
        /// Substitute references to type parameters from 'typeDef'
        /// with type arguments from 'typeArgs' in type 'type'.
        /// </summary>
        internal static Type Substitute(this Type type, Type typeDef, Type[] typeArgs)
        {
            Debug.Assert(typeDef.IsGenericTypeDefinition);
            Debug.Assert(typeDef.GetGenericArguments().Length == typeArgs.Length);

            if (type.IsGenericType)
            {
                var builder = ArrayBuilder<Type>.GetInstance();
                foreach (var t in type.GetGenericArguments())
                {
                    builder.Add(t.Substitute(typeDef, typeArgs));
                }
                var typeDefinition = type.GetGenericTypeDefinition();
                return typeDefinition.MakeGenericType(builder.ToArrayAndFree());
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                elementType = elementType.Substitute(typeDef, typeArgs);
                var n = type.GetArrayRank();
                return (n == 1) ? elementType.MakeArrayType() : elementType.MakeArrayType(n);
            }
            else if (type.IsPointer)
            {
                var elementType = type.GetElementType();
                elementType = elementType.Substitute(typeDef, typeArgs);
                return elementType.MakePointerType();
            }
            else if (type.IsGenericParameter)
            {
                if (type.DeclaringType.Equals(typeDef))
                {
                    var ordinal = type.GenericParameterPosition;
                    return typeArgs[ordinal];
                }
            }

            return type;
        }

        // Returns the IEnumerable interface implemented by the given type,
        // preferring System.Collections.Generic.IEnumerable<T> over
        // System.Collections.IEnumerable. If there are multiple implementations
        // of IEnumerable<T> on base and derived types, the implementation on
        // the most derived type is returned. If there are multiple implementations
        // of IEnumerable<T> on the same type, it is undefined which is returned.
        internal static Type GetIEnumerableImplementationIfAny(this Type type)
        {
            var t = type;
            do
            {
                foreach (var @interface in t.GetInterfacesOnType())
                {
                    if (@interface.IsIEnumerableOfT())
                    {
                        // Return the first implementation of IEnumerable<T>.
                        return @interface;
                    }
                }
                t = t.BaseType;
            } while (t != null);

            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.IsIEnumerable())
                {
                    return @interface;
                }
            }

            return null;
        }

        internal static bool IsEmptyResultsViewException(this Type type)
        {
            return type.IsType("System.Linq", "SystemCore_EnumerableDebugViewEmptyException");
        }

        internal static bool IsOrInheritsFrom(this Type type, Type baseType)
        {
            Debug.Assert(type != null);
            Debug.Assert(baseType != null);
            Debug.Assert(!baseType.IsInterface);

            if (type.IsInterface)
            {
                return false;
            }

            do
            {
                if (type.Equals(baseType))
                {
                    return true;
                }
                type = type.BaseType;
            }
            while (type != null);

            return false;
        }

        private static bool IsMscorlib(this Assembly assembly)
        {
            return assembly.GetReferencedAssemblies().Length == 0;
        }

        private static bool IsMscorlibType(this Type type, string @namespace, string name)
        {
            // Ignore IsMscorlib for now since type.Assembly returns
            // System.Runtime.dll for some types in mscorlib.dll.
            // TODO: Re-enable commented out check.
            return type.IsType(@namespace, name) /*&& type.Assembly.IsMscorlib()*/;
        }

        internal static bool IsOrInheritsFrom(this Type type, string @namespace, string name)
        {
            do
            {
                if (type.IsType(@namespace, name))
                {
                    return true;
                }
                type = type.BaseType;
            }
            while (type != null);
            return false;
        }

        internal static bool IsType(this Type type, string @namespace, string name)
        {
            Debug.Assert((@namespace == null) || (@namespace.Length > 0)); // Type.Namespace is null not empty.
            Debug.Assert(!string.IsNullOrEmpty(name));
            return AreNamesEqual(type.Namespace, @namespace) &&
                AreNamesEqual(type.Name, name);
        }

        private static bool AreNamesEqual(string nameA, string nameB)
        {
            return string.Equals(nameA, nameB, StringComparison.Ordinal);
        }

        internal static MemberInfo GetOriginalDefinition(this MemberInfo member)
        {
            var declaringType = member.DeclaringType;
            if (!declaringType.IsGenericType)
            {
                return member;
            }

            var declaringTypeOriginalDefinition = declaringType.GetGenericTypeDefinition();
            if (declaringType.Equals(declaringTypeOriginalDefinition))
            {
                return member;
            }

            foreach (var candidate in declaringTypeOriginalDefinition.GetMember(member.Name, MemberBindingFlags))
            {
                var memberType = candidate.MemberType;
                if (memberType != member.MemberType) continue;

                switch (memberType)
                {
                    case MemberTypes.Field:
                        return candidate;
                    case MemberTypes.Property:
                        Debug.Assert(((PropertyInfo)member).GetIndexParameters().Length == 0);
                        if (((PropertyInfo)candidate).GetIndexParameters().Length == 0)
                        {
                            return candidate;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(memberType);
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal static Type GetInterfaceListEntry(this Type interfaceType, Type declaration)
        {
            Debug.Assert(interfaceType.IsInterface);

            if (!interfaceType.IsGenericType || !declaration.IsGenericType)
            {
                return interfaceType;
            }

            var index = Array.IndexOf(declaration.GetInterfacesOnType(), interfaceType);
            Debug.Assert(index >= 0);

            var result = declaration.GetGenericTypeDefinition().GetInterfacesOnType()[index];
            Debug.Assert(interfaceType.GetGenericTypeDefinition().Equals(result.GetGenericTypeDefinition()));
            return result;
        }

        internal static MemberAndDeclarationInfo GetMemberByName(this DkmClrType type, string name)
        {
            var members = type.GetLmrType().GetMember(name, TypeHelpers.MemberBindingFlags);
            Debug.Assert(members.Length == 1);
            return new MemberAndDeclarationInfo(members[0], browsableState: null, info: DeclarationInfo.None, inheritanceLevel: 0);
        }
    }
}
