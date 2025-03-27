// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
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
            ResultProvider resultProvider,
            bool isProxyType,
            bool supportsFavorites)
        {
            // For members of type DynamicProperty (part of Dynamic View expansion), we want
            // to expand the underlying value (not the members of the DynamicProperty type).
            var type = value.Type;
            var runtimeType = type.GetLmrType();
            var isDynamicProperty = runtimeType.IsDynamicProperty();
            if (isDynamicProperty)
            {
                Debug.Assert(!value.IsNull);
                value = value.GetFieldValue("value", inspectionContext);
            }

            // Primitives, enums, function pointers, IntPtr, UIntPtr and null values with a declared type that is an interface have no visible members.
            Debug.Assert(!runtimeType.IsInterface || value.IsNull);
            if (resultProvider.IsPrimitiveType(runtimeType) || runtimeType.IsEnum || runtimeType.IsInterface || runtimeType.IsFunctionPointer() ||
                runtimeType.IsIntPtr() || runtimeType.IsUIntPtr())
            {
                return null;
            }

            // As in the old C# EE, DynamicProperty members are only expandable if they have a Dynamic View expansion.
            var dynamicViewExpansion = DynamicViewExpansion.CreateExpansion(inspectionContext, value);
            if (isDynamicProperty && (dynamicViewExpansion == null))
            {
                return null;
            }

            var customTypeInfoMap = CustomTypeInfoTypeArgumentMap.Create(declaredTypeAndInfo);

            var expansions = ArrayBuilder<Expansion>.GetInstance();

            // Expand members. TODO: Ideally, this would be done lazily (https://github.com/dotnet/roslyn/issues/32800)
            // From the members, collect the fields and properties,
            // separated into static and instance members.
            var staticMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var instanceMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var appDomain = value.Type.AppDomain;

            var allMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();
            var includeInherited = (flags & ExpansionFlags.IncludeBaseMembers) == ExpansionFlags.IncludeBaseMembers;
            var hideNonPublic = (inspectionContext.EvaluationFlags & DkmEvaluationFlags.HideNonPublicMembers) == DkmEvaluationFlags.HideNonPublicMembers;
            var raw = (inspectionContext.EvaluationFlags & DkmEvaluationFlags.ShowValueRaw) == DkmEvaluationFlags.ShowValueRaw;
            var favoritesInfo = supportsFavorites ? type.GetFavorites() : null;
            runtimeType.AppendTypeMembers(allMembers, predicate, resultProvider, declaredTypeAndInfo.Type, appDomain, includeInherited, hideNonPublic, isProxyType, raw, supportsFavorites, favoritesInfo);

            var favoritesMembersByName = new Dictionary<string, MemberAndDeclarationInfo>();

            foreach (var member in allMembers)
            {
                // Favorites are currently never static
                if (member.IsFavorite && !value.IsNull)
                {
                    favoritesMembersByName.Add(member.DisplayName, member);
                }
                else if (member.IsStatic)
                {
                    staticMembers.Add(member);
                }
                else if (!value.IsNull)
                {
                    instanceMembers.Add(member);
                }
            }

            allMembers.Free();

            // Favorites members.
            Expansion favoritesExpansion = null;
            if (favoritesMembersByName.Count > 0)
            {
                var favoritesMembers = ArrayBuilder<MemberAndDeclarationInfo>.GetInstance();

                foreach (string name in favoritesInfo.Favorites)
                {
                    if (favoritesMembersByName.TryGetValue(name, out var memberAndDeclarationInfo))
                    {
                        favoritesMembers.Add(memberAndDeclarationInfo);
                    }
                }

                if (favoritesMembers.Count > 0)
                {
                    favoritesExpansion = new MemberExpansion(favoritesMembers.ToArrayAndFree(), customTypeInfoMap, containsFavorites: true);
                }
            }

            if (favoritesExpansion != null)
            {
                expansions.Add(favoritesExpansion);

                // Check if we are only expanding favorites.
                if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.FilterToFavorites) == DkmEvaluationFlags.FilterToFavorites)
                {
                    instanceMembers.Free();
                    staticMembers.Free();
                    expansions.Free();

                    return favoritesExpansion;
                }
            }

            // Public and non-public instance members.
            Expansion publicInstanceExpansion;
            Expansion nonPublicInstanceExpansion;
            GetPublicAndNonPublicMembers(
                instanceMembers,
                customTypeInfoMap,
                isProxyType,
                out publicInstanceExpansion,
                out nonPublicInstanceExpansion);

            // Public and non-public static members.
            Expansion publicStaticExpansion;
            Expansion nonPublicStaticExpansion;
            GetPublicAndNonPublicMembers(
                staticMembers,
                customTypeInfoMap,
                isProxyType,
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
                    type,
                    AggregateExpansion.CreateExpansion(staticExpansions));
                staticExpansions.Free();
                expansions.Add(staticMembersExpansion);
            }

            instanceMembers.Free();
            staticMembers.Free();

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
                var resultsViewExpansion = ResultsViewExpansion.CreateExpansion(inspectionContext, value, resultProvider);
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
            CustomTypeInfoTypeArgumentMap customTypeInfoMap,
            bool isProxyType,
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
                                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray(), customTypeInfoMap));
                                publicMembers.Clear();
                            }
                            publicExpansions.Add(new RootHiddenExpansion(member, customTypeInfoMap));
                            continue;
                        case DkmClrDebuggerBrowsableAttributeState.Never:
                            continue;
                    }
                }

                // The native EE shows proxy type members as public members if they have a
                // DebuggerBrowsable attribute of any value. Match that behaviour here.
                if (member.HideNonPublic && !member.IsPublic && (!isProxyType || !member.BrowsableState.HasValue))
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
                publicExpansions.Add(new MemberExpansion(publicMembers.ToArray(), customTypeInfoMap));
            }
            publicMembers.Free();

            publicExpansion = AggregateExpansion.CreateExpansion(publicExpansions);
            publicExpansions.Free();

            nonPublicExpansion = (nonPublicMembers.Count > 0)
                ? new NonPublicMembersExpansion(
                    members: new MemberExpansion(nonPublicMembers.ToArray(), customTypeInfoMap))
                : null;
            nonPublicMembers.Free();
        }

        private readonly MemberAndDeclarationInfo[] _members;
        private readonly CustomTypeInfoTypeArgumentMap _customTypeInfoMap;
        private readonly bool _containsFavorites;

        private MemberExpansion(MemberAndDeclarationInfo[] members, CustomTypeInfoTypeArgumentMap customTypeInfoMap, bool containsFavorites = false)
        {
            Debug.Assert(members != null);
            Debug.Assert(members.Length > 0);
            Debug.Assert(customTypeInfoMap != null);

            _members = members;
            _customTypeInfoMap = customTypeInfoMap;
            _containsFavorites = containsFavorites;
        }

        internal override bool ContainsFavorites => _containsFavorites;

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResult> rows,
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
                rows.Add(GetMemberRow(resultProvider, inspectionContext, value, _members[i + offset], parent, _customTypeInfoMap));
            }

            index += _members.Length;
        }

        private static EvalResult GetMemberRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            MemberAndDeclarationInfo member,
            EvalResultDataItem parent,
            CustomTypeInfoTypeArgumentMap customTypeInfoMap)
        {
            var memberValue = value.GetMemberValue(member, inspectionContext);
            return CreateMemberDataItem(
                resultProvider,
                inspectionContext,
                member,
                memberValue,
                parent,
                customTypeInfoMap,
                ExpansionFlags.All,
                supportsFavorites: true);
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
                ArrayBuilder<EvalResult> rows,
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
                        inspectionContext,
                        value,
                        _members,
                        parent));
                }

                index++;
            }

            private static readonly ReadOnlyCollection<string> s_hiddenFormatSpecifiers = new ReadOnlyCollection<string>(new[] { "hidden" });

            private static EvalResult GetRow(
                DkmInspectionContext inspectionContext,
                DkmClrValue value,
                Expansion expansion,
                EvalResultDataItem parent)
            {
                return new EvalResult(
                    ExpansionKind.NonPublicMembers,
                    name: Resources.NonPublicMembers,
                    typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                    declaredTypeAndInfo: default(TypeAndCustomInfo),
                    useDebuggerDisplay: false,
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
            private readonly DkmClrType _type;
            private readonly Expansion _members;

            internal StaticMembersExpansion(DkmClrType type, Expansion members)
            {
                _type = type;
                _members = members;
            }

            internal override void GetRows(
                ResultProvider resultProvider,
                ArrayBuilder<EvalResult> rows,
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
                        new TypeAndCustomInfo(_type),
                        value,
                        _members));
                }

                index++;
            }

            private static EvalResult GetRow(
                ResultProvider resultProvider,
                DkmInspectionContext inspectionContext,
                TypeAndCustomInfo declaredTypeAndInfo,
                DkmClrValue value,
                Expansion expansion)
            {
                var fullName = resultProvider.FullNameProvider.GetClrTypeName(inspectionContext, declaredTypeAndInfo.ClrType, declaredTypeAndInfo.Info);
                return new EvalResult(
                    ExpansionKind.StaticMembers,
                    name: resultProvider.StaticMembersString,
                    typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                    declaredTypeAndInfo: declaredTypeAndInfo,
                    useDebuggerDisplay: false,
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
#nullable enable
        internal static EvalResult CreateMemberDataItem(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            MemberAndDeclarationInfo member,
            DkmClrValue memberValue,
            EvalResultDataItem parent,
            CustomTypeInfoTypeArgumentMap customTypeInfoMap,
            ExpansionFlags flags,
            bool supportsFavorites)
        {
            var fullNameProvider = resultProvider.FullNameProvider;
            var declaredType = member.Type;
            var declaredTypeInfo = customTypeInfoMap.SubstituteCustomTypeInfo(member.OriginalDefinitionType, member.TypeInfo);
            string? memberNameForFullName;

            // Considering, we're not handling the case of a member inherited from a generic base type.
            if (member.TryGetExplicitlyImplementedInterface(out var declaringType, out var memberDisplayName))
            {
                // full name will include cast to the interface, e.g. "((I)obj).MemberName":
                memberNameForFullName = memberDisplayName;
            }
            else
            {
                declaringType = member.DeclaringType;
                memberDisplayName = member.DisplayName;
                memberNameForFullName = member.MetadataName;
            }

            var declaringTypeInfo = declaringType.IsInterface
                ? customTypeInfoMap.SubstituteCustomTypeInfo(declaringType.GetInterfaceListEntry(member.DeclaringType), customInfo: null)
                : null;

            var appDomain = memberValue.Type.AppDomain;
            string? fullName;

            memberNameForFullName = fullNameProvider.GetClrValidIdentifier(inspectionContext, memberNameForFullName);
            if (memberNameForFullName == null)
            {
                fullName = null;
            }
            else
            {
                memberDisplayName = memberNameForFullName;
                fullName = MakeFullName(
                       fullNameProvider,
                       inspectionContext,
                       memberNameForFullName,
                       new TypeAndCustomInfo(DkmClrType.Create(appDomain, declaringType), declaringTypeInfo), // Note: Won't include DynamicAttribute.
                       member.RequiresExplicitCast,
                       member.IsStatic,
                       parent);
            }

            return resultProvider.CreateDataItem(
                inspectionContext,
                memberDisplayName,
                typeDeclaringMemberAndInfo: (member.IncludeTypeInMemberName || declaringType.IsInterface) ? new TypeAndCustomInfo(DkmClrType.Create(appDomain, declaringType), declaringTypeInfo) : default(TypeAndCustomInfo), // Note: Won't include DynamicAttribute.
                declaredTypeAndInfo: new TypeAndCustomInfo(DkmClrType.Create(appDomain, declaredType), declaredTypeInfo),
                value: memberValue,
                useDebuggerDisplay: parent != null,
                expansionFlags: flags,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: memberValue.EvalFlags,
                evalFlags: DkmEvaluationFlags.None,
                canFavorite: member.CanFavorite,
                isFavorite: member.IsFavorite,
                supportsFavorites: supportsFavorites);
        }
#nullable disable
        private static string MakeFullName(
            IDkmClrFullNameProvider fullNameProvider,
            DkmInspectionContext inspectionContext,
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
                parentFullName = parentFullName.Parenthesize();
            }

            var typeDeclaringMember = typeDeclaringMemberAndInfo.Type;
            if (typeDeclaringMember.IsInterface)
            {
                memberAccessRequiresExplicitCast = !typeDeclaringMember.Equals(parent.DeclaredTypeAndInfo.Type);
            }

            return fullNameProvider.GetClrMemberName(
                inspectionContext,
                parentFullName,
                typeDeclaringMemberAndInfo.ClrType,
                typeDeclaringMemberAndInfo.Info,
                name,
                memberAccessRequiresExplicitCast,
                memberIsStatic);
        }
    }
}
