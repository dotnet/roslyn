// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Type member expansion.
    /// </summary>
    /// <remarks>
    /// Includes accesses to static members with instance receivers and
    /// accesses to instance members with dynamic receivers.
    /// </remarks>
    internal sealed class MemberExpansion : Expansion
    {
        internal static Expansion CreateExpansion(
            DkmInspectionContext inspectionContext,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            ExpansionFlags flags,
            Predicate<MemberInfo> predicate,
            Formatter formatter)
        {
            // For members of type DynamicProperty (part of Dynamic View expansion), we want
            // to expand the underlying value (not the members of the DynamicProperty type).
            var type = value.Type;
            var isDynamicProperty = type.GetLmrType().IsDynamicProperty();
            if (isDynamicProperty)
            {
                Debug.Assert(!value.IsNull);
                value = value.GetFieldValue("value", inspectionContext);
            }

            var runtimeType = type.GetLmrType();
            // Primitives, enums, function pointers, and null values with a declared type that is an interface have no visible members.
            Debug.Assert(!runtimeType.IsInterface || value.IsNull);
            if (formatter.IsPredefinedType(runtimeType) || runtimeType.IsEnum || runtimeType.IsInterface || runtimeType.IsFunctionPointer())
            {
                return null;
            }

            // As in the old C# EE, DynamicProperty members are only expandable if they have a Dynamic View expansion.
            var dynamicViewExpansion = DynamicViewExpansion.CreateExpansion(inspectionContext, value, formatter);
            if (isDynamicProperty && (dynamicViewExpansion == null))
            {
                return null;
            }

            var dynamicFlagsMap = DynamicFlagsMap.Create(declaredTypeAndInfo);

            var expansions = ArrayBuilder<Expansion>.GetInstance();

            // From the members, collect the fields and properties,
            // separated into static and instance members.
            var staticMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var instanceMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var appDomain = value.Type.AppDomain;

            // Expand members. (Ideally, this should be done lazily.)
            var allMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var includeInherited = (flags & ExpansionFlags.IncludeBaseMembers) == ExpansionFlags.IncludeBaseMembers;
            var hideNonPublic = (inspectionContext.EvaluationFlags & DkmEvaluationFlags.HideNonPublicMembers) == DkmEvaluationFlags.HideNonPublicMembers;
            runtimeType.AppendTypeMembers(allMembers, predicate, declaredTypeAndInfo.Type, appDomain, includeInherited, hideNonPublic);

            foreach (var member in allMembers)
            {
                var name = member.Name;
                if (name.IsCompilerGenerated())
                {
                    continue;
                }
                if (member.IsStatic)
                {
                    staticMembers.Add(member);
                }
                else if (!value.IsNull)
                {
                    instanceMembers.Add(member);
                }
            }

            allMembers.Free();

            // Public and non-public instance members.
            Expansion publicInstanceExpansion;
            Expansion nonPublicInstanceExpansion;
            GetPublicAndNonPublicMembers(
                instanceMembers,
                dynamicFlagsMap,
                out publicInstanceExpansion,
                out nonPublicInstanceExpansion);

            // Public and non-public static members.
            Expansion publicStaticExpansion;
            Expansion nonPublicStaticExpansion;
            GetPublicAndNonPublicMembers(
                staticMembers,
                dynamicFlagsMap,
                out publicStaticExpansion,
                out nonPublicStaticExpansion);

            if (publicInstanceExpansion != null)
            {
                expansions.Add(publicInstanceExpansion);
            }

            if ((publicStaticExpansion != null) || (nonPublicStaticExpansion != null))
            {
                var staticExpansions = ArrayBuilder<Expansion>.GetInstance();
                if (publicStaticExpansion != null)
                {
                    staticExpansions.Add(publicStaticExpansion);
                }
                if (nonPublicStaticExpansion != null)
                {
                    staticExpansions.Add(nonPublicStaticExpansion);
                }
                Debug.Assert(staticExpansions.Count > 0);
                var staticMembersExpansion = new StaticMembersExpansion(
                    runtimeType,
                    AggregateExpansion.CreateExpansion(staticExpansions));
                staticExpansions.Free();
                expansions.Add(staticMembersExpansion);
            }

            if (value.NativeComPointer != 0)
            {
                expansions.Add(NativeViewExpansion.Instance);
            }

            if (nonPublicInstanceExpansion != null)
            {
                expansions.Add(nonPublicInstanceExpansion);
            }

            // Include Results View if necessary.
            if ((flags & ExpansionFlags.IncludeResultsView) != 0)
            {
                var resultsViewExpansion = ResultsViewExpansion.CreateExpansion(inspectionContext, value, formatter);
                if (resultsViewExpansion != null)
                {
                    expansions.Add(resultsViewExpansion);
                }
            }

            if (dynamicViewExpansion != null)
            {
                expansions.Add(dynamicViewExpansion);
            }

            var result = AggregateExpansion.CreateExpansion(expansions);
            expansions.Free();
            return result;
        }

        private static void GetPublicAndNonPublicMembers(
            ArrayBuilder<MemberAndDeclarationInfo> allMembers,
            DynamicFlagsMap dynamicFlagsMap,
            out Expansion publicExpansion,
            out Expansion nonPublicExpansion)
        {
            var publicExpansions = ArrayBuilder<Expansion>.GetInstance();
            var publicMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var nonPublicMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();

            foreach (var member in allMembers)
            {
                if (member.BrowsableState.HasValue)
                {
                    switch (member.BrowsableState.Value)
                    {
                        case DkmClrDebuggerBrowsableAttributeState.RootHidden:
                            if (publicMembers.Count > 0)
                            {
                                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray(), dynamicFlagsMap));
                                publicMembers.Clear();
                            }
                            publicExpansions.Add(new RootHiddenExpansion(member, dynamicFlagsMap));
                            continue;
                        case DkmClrDebuggerBrowsableAttributeState.Never:
                            continue;
                    }
                }

                if (member.HideNonPublic && !member.IsPublic)
                {
                    nonPublicMembers.Add(member);
                }
                else
                {
                    publicMembers.Add(member);
                }
            }

            if (publicMembers.Count > 0)
            {
                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray(), dynamicFlagsMap));
            }
            publicMembers.Free();

            publicExpansion = AggregateExpansion.CreateExpansion(publicExpansions);
            publicExpansions.Free();

            nonPublicExpansion = (nonPublicMembers.Count > 0) ?
                new NonPublicMembersExpansion(
                    members: new MemberExpansion(nonPublicMembers.ToArray(), dynamicFlagsMap)) :
                null;
            nonPublicMembers.Free();
        }

        private readonly MemberAndDeclarationInfo[] _members;
        private readonly DynamicFlagsMap _dynamicFlagsMap;

        private MemberExpansion(MemberAndDeclarationInfo[] members, DynamicFlagsMap dynamicFlagsMap)
        {
            Debug.Assert(members != null);
            Debug.Assert(members.Length > 0);
            Debug.Assert(dynamicFlagsMap != null);

            _members = members;
            _dynamicFlagsMap = dynamicFlagsMap;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResultDataItem> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, _members.Length, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                rows.Add(GetMemberRow(resultProvider, inspectionContext, value, _members[i + offset], parent, _dynamicFlagsMap));
            }

            index += _members.Length;
        }

        private static EvalResultDataItem GetMemberRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            MemberAndDeclarationInfo member,
            EvalResultDataItem parent,
            DynamicFlagsMap dynamicFlagsMap)
        {
            var memberValue = value.GetMemberValue(member, inspectionContext);
            return CreateMemberDataItem(
                resultProvider,
                inspectionContext,
                member,
                memberValue,
                parent,
                dynamicFlagsMap,
                ExpansionFlags.All);
        }

        /// <summary>
        /// An explicit user request to bypass "Just My Code" and display
        /// the inaccessible members of an instance of an imported type.
        /// </summary>
        private sealed class NonPublicMembersExpansion : Expansion
        {
            private readonly Expansion _members;

            internal NonPublicMembersExpansion(Expansion members)
            {
                _members = members;
            }

            internal override void GetRows(
                ResultProvider resultProvider,
                ArrayBuilder<EvalResultDataItem> rows,
                DkmInspectionContext inspectionContext,
                EvalResultDataItem parent,
                DkmClrValue value,
                int startIndex,
                int count,
                bool visitAll,
                ref int index)
            {
                if (InRange(startIndex, count, index))
                {
                    rows.Add(GetRow(
                        resultProvider,
                        inspectionContext,
                        value,
                        _members,
                        parent));
                }

                index++;
            }

            private static readonly ReadOnlyCollection<string> s_hiddenFormatSpecifiers = new ReadOnlyCollection<string>(new[] { "hidden" });

            private static EvalResultDataItem GetRow(
                ResultProvider resultProvider,
                DkmInspectionContext inspectionContext,
                DkmClrValue value,
                Expansion expansion,
                EvalResultDataItem parent)
            {
                return new EvalResultDataItem(
                    ExpansionKind.NonPublicMembers,
                    name: Resources.NonPublicMembers,
                    typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                    declaredTypeAndInfo: default(TypeAndCustomInfo),
                    parent: null,
                    value: value,
                    displayValue: null,
                    expansion: expansion,
                    childShouldParenthesize: parent.ChildShouldParenthesize,
                    fullName: parent.FullNameWithoutFormatSpecifiers,
                    childFullNamePrefixOpt: parent.ChildFullNamePrefix,
                    formatSpecifiers: s_hiddenFormatSpecifiers,
                    category: DkmEvaluationResultCategory.Data,
                    flags: DkmEvaluationResultFlags.ReadOnly,
                    editableValue: null,
                    inspectionContext: inspectionContext);
            }
        }

        /// <summary>
        /// A transition from an instance of a type to the type itself (for inspecting static members).
        /// </summary>
        private sealed class StaticMembersExpansion : Expansion
        {
            private readonly Type _runtimeType;
            private readonly Expansion _members;

            internal StaticMembersExpansion(Type runtimeType, Expansion members)
            {
                _runtimeType = runtimeType;
                _members = members;
            }

            internal override void GetRows(
                ResultProvider resultProvider,
                ArrayBuilder<EvalResultDataItem> rows,
                DkmInspectionContext inspectionContext,
                EvalResultDataItem parent,
                DkmClrValue value,
                int startIndex,
                int count,
                bool visitAll,
                ref int index)
            {
                if (InRange(startIndex, count, index))
                {
                    rows.Add(GetRow(
                        resultProvider,
                        inspectionContext,
                        new TypeAndCustomInfo(_runtimeType),
                        value,
                        _members));
                }

                index++;
            }

            private static EvalResultDataItem GetRow(
                ResultProvider resultProvider,
                DkmInspectionContext inspectionContext,
                TypeAndCustomInfo declaredTypeAndInfo,
                DkmClrValue value,
                Expansion expansion)
            {
                var formatter = resultProvider.Formatter;
                bool sawInvalidIdentifier;
                var fullName = formatter.GetTypeName(declaredTypeAndInfo, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                if (sawInvalidIdentifier)
                {
                    fullName = null;
                }
                return new EvalResultDataItem(
                    ExpansionKind.StaticMembers,
                    name: formatter.StaticMembersString,
                    typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                    declaredTypeAndInfo: declaredTypeAndInfo,
                    parent: null,
                    value: value,
                    displayValue: null,
                    expansion: expansion,
                    childShouldParenthesize: false,
                    fullName: fullName,
                    childFullNamePrefixOpt: fullName,
                    formatSpecifiers: Formatter.NoFormatSpecifiers,
                    category: DkmEvaluationResultCategory.Class,
                    flags: DkmEvaluationResultFlags.ReadOnly,
                    editableValue: null,
                    inspectionContext: inspectionContext);
            }
        }

        internal static EvalResultDataItem CreateMemberDataItem(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            MemberAndDeclarationInfo member,
            DkmClrValue memberValue,
            EvalResultDataItem parent,
            DynamicFlagsMap dynamicFlagsMap,
            ExpansionFlags flags)
        {
            var declaredType = member.Type;
            var declaredTypeInfo = dynamicFlagsMap.SubstituteDynamicFlags(member.OriginalDefinitionType, DynamicFlagsCustomTypeInfo.Create(member.TypeInfo)).GetCustomTypeInfo();
            string memberName;
            // Considering, we're not handling the case of a member inherited from a generic base type.
            var typeDeclaringMember = member.GetExplicitlyImplementedInterface(out memberName) ?? member.DeclaringType;
            var typeDeclaringMemberInfo = typeDeclaringMember.IsInterface
                ? dynamicFlagsMap.SubstituteDynamicFlags(typeDeclaringMember.GetInterfaceListEntry(member.DeclaringType), originalDynamicFlags: default(DynamicFlagsCustomTypeInfo)).GetCustomTypeInfo()
                : null;
            var formatter = resultProvider.Formatter;
            bool sawInvalidIdentifier;
            memberName = formatter.GetIdentifierEscapingPotentialKeywords(memberName, out sawInvalidIdentifier);
            var fullName = sawInvalidIdentifier
                ? null
                : MakeFullName(
                    formatter,
                    memberName,
                    new TypeAndCustomInfo(typeDeclaringMember, typeDeclaringMemberInfo), // Note: Won't include DynamicAttribute.
                    member.RequiresExplicitCast,
                    member.IsStatic,
                    parent);
            return resultProvider.CreateDataItem(
                inspectionContext,
                memberName,
                typeDeclaringMemberAndInfo: (member.IncludeTypeInMemberName || typeDeclaringMember.IsInterface) ? new TypeAndCustomInfo(typeDeclaringMember, typeDeclaringMemberInfo) : default(TypeAndCustomInfo), // Note: Won't include DynamicAttribute.
                declaredTypeAndInfo: new TypeAndCustomInfo(declaredType, declaredTypeInfo),
                value: memberValue,
                parent: parent,
                expansionFlags: flags,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: memberValue.EvalFlags,
                evalFlags: DkmEvaluationFlags.None);
        }

        private static string MakeFullName(
            Formatter formatter,
            string name,
            TypeAndCustomInfo typeDeclaringMemberAndInfo,
            bool memberAccessRequiresExplicitCast,
            bool memberIsStatic,
            EvalResultDataItem parent)
        {
            // If the parent is an exception thrown during evaluation,
            // there is no valid fullname expression for the child.
            if (parent.Value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown))
            {
                return null;
            }

            var parentFullName = parent.ChildFullNamePrefix;
            if (parentFullName == null)
            {
                return null;
            }

            if (parent.ChildShouldParenthesize)
            {
                parentFullName = $"({parentFullName})";
            }

            bool sawInvalidIdentifier;
            var typeDeclaringMember = typeDeclaringMemberAndInfo.Type;
            if (!typeDeclaringMember.IsInterface)
            {
                string qualifier;
                if (memberIsStatic)
                {
                    qualifier = formatter.GetTypeName(typeDeclaringMemberAndInfo, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                    if (sawInvalidIdentifier)
                    {
                        return null; // FullName wouldn't be parseable.
                    }
                }
                else if (memberAccessRequiresExplicitCast)
                {
                    var typeName = formatter.GetTypeName(typeDeclaringMemberAndInfo, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                    if (sawInvalidIdentifier)
                    {
                        return null; // FullName wouldn't be parseable.
                    }
                    qualifier = formatter.GetCastExpression(
                        parentFullName,
                        typeName,
                        parenthesizeEntireExpression: true);
                }
                else
                {
                    qualifier = parentFullName;
                }
                return $"{qualifier}.{name}";
            }
            else
            {
                // NOTE: This should never interact with debugger proxy types:
                //   1) Interfaces cannot have debugger proxy types.
                //   2) Debugger proxy types cannot be interfaces.
                if (typeDeclaringMember.Equals(parent.DeclaredTypeAndInfo.Type))
                {
                    return $"{parentFullName}.{name}";
                }
                else
                {
                    var interfaceName = formatter.GetTypeName(typeDeclaringMemberAndInfo, escapeKeywordIdentifiers: true, sawInvalidIdentifier: out sawInvalidIdentifier);
                    if (sawInvalidIdentifier)
                    {
                        return null; // FullName wouldn't be parseable.
                    }
                    return $"(({interfaceName}){parentFullName}).{name}";
                }
            }
        }
    }
}
