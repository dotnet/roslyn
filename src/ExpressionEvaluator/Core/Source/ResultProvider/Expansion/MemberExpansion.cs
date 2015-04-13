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
            Type declaredType,
            DkmClrValue value,
            ExpansionFlags flags,
            Predicate<MemberInfo> predicate,
            Formatter formatter)
        {
            var runtimeType = value.Type.GetLmrType();
            // Primitives, enums and null values with a declared type that is an interface have no visible members.
            Debug.Assert(!runtimeType.IsInterface || value.IsNull);
            if (formatter.IsPredefinedType(runtimeType) || runtimeType.IsEnum || runtimeType.IsInterface)
            {
                return null;
            }

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
            runtimeType.AppendTypeMembers(allMembers, predicate, declaredType, appDomain, includeInherited, hideNonPublic);

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
                out publicInstanceExpansion,
                out nonPublicInstanceExpansion);

            // Public and non-public static members.
            Expansion publicStaticExpansion;
            Expansion nonPublicStaticExpansion;
            GetPublicAndNonPublicMembers(
                staticMembers,
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

            var result = AggregateExpansion.CreateExpansion(expansions);
            expansions.Free();
            return result;
        }

        private static void GetPublicAndNonPublicMembers(
            ArrayBuilder<MemberAndDeclarationInfo> allMembers,
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
                                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray()));
                                publicMembers.Clear();
                            }
                            publicExpansions.Add(new RootHiddenExpansion(member));
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
                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray()));
            }
            publicMembers.Free();

            publicExpansion = AggregateExpansion.CreateExpansion(publicExpansions);
            publicExpansions.Free();

            nonPublicExpansion = (nonPublicMembers.Count > 0) ?
                new NonPublicMembersExpansion(
                    declaredType: null,
                    members: new MemberExpansion(nonPublicMembers.ToArray())) :
                null;
            nonPublicMembers.Free();
        }

        private readonly MemberAndDeclarationInfo[] _members;

        private MemberExpansion(MemberAndDeclarationInfo[] members)
        {
            Debug.Assert(members != null);
            Debug.Assert(members.Length > 0);

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
            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, _members.Length, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                rows.Add(GetMemberRow(resultProvider, inspectionContext, value, _members[i + offset], parent));
            }

            index += _members.Length;
        }

        private static EvalResultDataItem GetMemberRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            MemberAndDeclarationInfo member,
            EvalResultDataItem parent)
        {
            var memberValue = GetMemberValue(value, member, inspectionContext);
            return CreateMemberDataItem(
                resultProvider,
                inspectionContext,
                member,
                memberValue,
                parent,
                ExpansionFlags.All);
        }

        private static DkmClrValue GetMemberValue(DkmClrValue container, MemberAndDeclarationInfo member, DkmInspectionContext inspectionContext)
        {
            // Note: GetMemberValue() may return special value
            // when func-eval of properties is disabled.
            return container.GetMemberValue(member.Name, (int)member.MemberType, member.DeclaringType.FullName, inspectionContext);
        }

        private sealed class RootHiddenExpansion : Expansion
        {
            private readonly MemberAndDeclarationInfo _member;

            internal RootHiddenExpansion(MemberAndDeclarationInfo member)
            {
                _member = member;
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
                var memberValue = GetMemberValue(value, _member, inspectionContext);
                if (memberValue.IsError())
                {
                    if (InRange(startIndex, count, index))
                    {
                        var row = new EvalResultDataItem(Resources.ErrorName, errorMessage: (string)memberValue.HostObjectValue);
                        rows.Add(row);
                    }
                    index++;
                }
                else
                {
                    parent = CreateMemberDataItem(
                        resultProvider,
                        inspectionContext,
                        _member,
                        memberValue,
                        parent,
                        ExpansionFlags.IncludeBaseMembers | ExpansionFlags.IncludeResultsView);
                    var expansion = parent.Expansion;
                    if (expansion != null)
                    {
                        expansion.GetRows(resultProvider, rows, inspectionContext, parent, parent.Value, startIndex, count, visitAll, ref index);
                    }
                }
            }
        }

        /// <summary>
        /// An explicit user request to bypass "Just My Code" and display
        /// the inaccessible members of an instance of an imported type.
        /// </summary>
        private sealed class NonPublicMembersExpansion : Expansion
        {
            private readonly Type _declaredType;
            private readonly Expansion _members;

            internal NonPublicMembersExpansion(Type declaredType, Expansion members)
            {
                _declaredType = declaredType;
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
                        _declaredType,
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
                Type declaredType,
                DkmClrValue value,
                Expansion expansion,
                EvalResultDataItem parent)
            {
                return new EvalResultDataItem(
                    ExpansionKind.NonPublicMembers,
                    name: Resources.NonPublicMembers,
                    typeDeclaringMember: null,
                    declaredType: declaredType,
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
            private readonly Type _declaredType;
            private readonly Expansion _members;

            internal StaticMembersExpansion(Type declaredType, Expansion members)
            {
                _declaredType = declaredType;
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
                        _declaredType,
                        value,
                        _members));
                }

                index++;
            }

            private static EvalResultDataItem GetRow(
                ResultProvider resultProvider,
                DkmInspectionContext inspectionContext,
                Type declaredType,
                DkmClrValue value,
                Expansion expansion)
            {
                var formatter = resultProvider.Formatter;
                var fullName = formatter.GetTypeName(declaredType, escapeKeywordIdentifiers: true);
                return new EvalResultDataItem(
                    ExpansionKind.StaticMembers,
                    name: formatter.StaticMembersString,
                    typeDeclaringMember: null,
                    declaredType: declaredType,
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

        private static EvalResultDataItem CreateMemberDataItem(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            MemberAndDeclarationInfo member,
            DkmClrValue memberValue,
            EvalResultDataItem parent,
            ExpansionFlags flags)
        {
            var formatter = resultProvider.Formatter;
            string memberName;
            var typeDeclaringMember = member.GetExplicitlyImplementedInterface(out memberName) ?? member.DeclaringType;
            memberName = formatter.GetIdentifierEscapingPotentialKeywords(memberName);
            var fullName = MakeFullName(
                formatter,
                memberName,
                typeDeclaringMember,
                member.RequiresExplicitCast,
                member.IsStatic,
                parent);
            return resultProvider.CreateDataItem(
                inspectionContext,
                memberName,
                typeDeclaringMember: (member.IncludeTypeInMemberName || typeDeclaringMember.IsInterface) ? typeDeclaringMember : null,
                declaredType: member.Type,
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
            Type typeDeclaringMember,
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

            if (!typeDeclaringMember.IsInterface)
            {
                string qualifier;
                if (memberIsStatic)
                {
                    qualifier = formatter.GetTypeName(typeDeclaringMember, escapeKeywordIdentifiers: false);
                }
                else if (memberAccessRequiresExplicitCast)
                {
                    var typeName = formatter.GetTypeName(typeDeclaringMember, escapeKeywordIdentifiers: true);
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
                if (typeDeclaringMember.Equals(parent.DeclaredType))
                {
                    var memberAccessTemplate = parent.ChildShouldParenthesize
                        ? "({0}).{1}"
                        : "{0}.{1}";
                    return string.Format(memberAccessTemplate, parent.ChildFullNamePrefix, name);
                }
                else
                {
                    var interfaceName = formatter.GetTypeName(typeDeclaringMember, escapeKeywordIdentifiers: true);
                    var memberAccessTemplate = parent.ChildShouldParenthesize
                        ? "(({0})({1})).{2}"
                        : "(({0}){1}).{2}";
                    return string.Format(memberAccessTemplate, interfaceName, parent.ChildFullNamePrefix, name);
                }
            }
        }
    }
}
