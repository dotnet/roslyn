// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using FieldInfo = Microsoft.VisualStudio.Debugger.Metadata.FieldInfo;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class TupleExpansion : Expansion
    {
        internal static TupleExpansion CreateExpansion(
            DkmInspectionContext inspectionContext,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            int cardinality)
        {
            if (value.IsNull)
            {
                // No expansion.
                return null;
            }

            bool useRawView = (inspectionContext.EvaluationFlags & DkmEvaluationFlags.ShowValueRaw) != 0;
            return new TupleExpansion(new TypeAndCustomInfo(value.Type, declaredTypeAndInfo.Info), cardinality, useRawView);
        }

        private readonly TypeAndCustomInfo _typeAndInfo;
        private readonly int _cardinality;
        private readonly bool _useRawView;
        private Fields _lazyFields;

        private TupleExpansion(TypeAndCustomInfo typeAndInfo, int cardinality, bool useRawView)
        {
            _typeAndInfo = typeAndInfo;
            _cardinality = cardinality;
            _useRawView = useRawView;
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
            var fields = GetFields();
            var defaultView = fields.DefaultView;

            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, defaultView.Count, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                var row = GetMemberRow(resultProvider, inspectionContext, value, defaultView[i + offset], parent, _cardinality);
                rows.Add(row);
            }

            index += defaultView.Count;

            if (fields.IncludeRawView)
            {
                if (InRange(startIndex, count, index))
                {
                    rows.Add(this.CreateRawViewRow(inspectionContext, parent, value));
                }

                index++;
            }
        }

        private static EvalResult GetMemberRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            Field field,
            EvalResultDataItem parent,
            int cardinality)
        {
            var fullNameProvider = resultProvider.FullNameProvider;
            var parentFullName = parent.ChildFullNamePrefix;
            if (parentFullName != null)
            {
                if (parent.ChildShouldParenthesize)
                {
                    parentFullName = parentFullName.Parenthesize();
                }
                var parentRuntimeType = parent.Value.Type;
                if (!parent.DeclaredTypeAndInfo.Type.Equals(parentRuntimeType.GetLmrType()))
                {
                    parentFullName = fullNameProvider.GetClrCastExpression(
                        inspectionContext,
                        parentFullName,
                        parentRuntimeType,
                        customTypeInfo: null,
                        castExpressionOptions: DkmClrCastExpressionOptions.ParenthesizeEntireExpression);
                }
            }

            // Ideally if the caller requests multiple items in a nested tuple
            // we should only evaluate Rest once, and should only calculate
            // the full name for Rest once.
            string fullName;
            var fieldValue = GetValueAndFullName(
                fullNameProvider,
                inspectionContext,
                value,
                field,
                parentFullName,
                out fullName);
            var name = field.Name;
            var typeDeclaringMemberAndInfo = default(TypeAndCustomInfo);
            var declaredTypeAndInfo = field.FieldTypeAndInfo;
            var flags = fieldValue.EvalFlags;
            var formatSpecifiers = Formatter.NoFormatSpecifiers;

            if (field.IsRest)
            {
                var displayValue = fieldValue.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
                var displayType = ResultProvider.GetTypeName(
                    inspectionContext,
                    fieldValue,
                    declaredTypeAndInfo.ClrType,
                    declaredTypeAndInfo.Info,
                    isPointerDereference: false);
                var expansion = new TupleExpansion(declaredTypeAndInfo, cardinality - (TypeHelpers.TupleFieldRestPosition - 1), useRawView: true);
                return new EvalResult(
                    ExpansionKind.Explicit,
                    name,
                    typeDeclaringMemberAndInfo,
                    declaredTypeAndInfo,
                    useDebuggerDisplay: false,
                    value: fieldValue,
                    displayValue: displayValue,
                    expansion: expansion,
                    childShouldParenthesize: false,
                    fullName: fullName,
                    childFullNamePrefixOpt: flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                    formatSpecifiers: Formatter.AddFormatSpecifier(formatSpecifiers, "raw"),
                    category: DkmEvaluationResultCategory.Other,
                    flags: flags,
                    editableValue: null,
                    inspectionContext: inspectionContext,
                    displayName: name,
                    displayType: displayType);
            }

            return resultProvider.CreateDataItem(
                inspectionContext,
                name,
                typeDeclaringMemberAndInfo: typeDeclaringMemberAndInfo,
                declaredTypeAndInfo: declaredTypeAndInfo,
                value: fieldValue,
                useDebuggerDisplay: false,
                expansionFlags: ExpansionFlags.All,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: formatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: flags,
                evalFlags: DkmEvaluationFlags.None,
                canFavorite: false,
                isFavorite: false,
                supportsFavorites: true);
        }

        private static DkmClrValue GetValueAndFullName(
            IDkmClrFullNameProvider fullNameProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            Field field,
            string parentFullName,
            out string fullName)
        {
            var parent = field.Parent;
            if (parent != null)
            {
                value = GetValueAndFullName(
                    fullNameProvider,
                    inspectionContext,
                    value,
                    parent,
                    parentFullName,
                    out parentFullName);
            }
            var fieldName = field.FieldInfo.Name;
            fullName = (parentFullName == null)
                ? null
                : fullNameProvider.GetClrMemberName(
                    inspectionContext,
                    parentFullName,
                    clrType: field.DeclaringTypeAndInfo.ClrType,
                    customTypeInfo: null,
                    memberName: fieldName,
                    requiresExplicitCast: false,
                    isStatic: false);
            return value.GetFieldValue(fieldName, inspectionContext);
        }

        private sealed class Field
        {
            internal readonly TypeAndCustomInfo DeclaringTypeAndInfo;
            internal readonly TypeAndCustomInfo FieldTypeAndInfo;
            internal readonly FieldInfo FieldInfo; // type field
            internal readonly string Name;
            internal readonly Field Parent; // parent Rest field, if any
            internal readonly bool IsRest;

            internal Field(
                TypeAndCustomInfo declaringTypeAndInfo,
                TypeAndCustomInfo fieldTypeAndInfo,
                FieldInfo fieldInfo,
                string name,
                Field parent,
                bool isRest)
            {
                Debug.Assert(declaringTypeAndInfo.ClrType != null);
                Debug.Assert(fieldTypeAndInfo.ClrType != null);
                Debug.Assert(fieldInfo != null);
                Debug.Assert(name != null);
                Debug.Assert(declaringTypeAndInfo.Type.Equals(fieldInfo.DeclaringType));
                Debug.Assert(fieldTypeAndInfo.Type.Equals(fieldInfo.FieldType));
                Debug.Assert(parent == null || parent.FieldInfo.FieldType.Equals(fieldInfo.DeclaringType));

                DeclaringTypeAndInfo = declaringTypeAndInfo;
                FieldTypeAndInfo = fieldTypeAndInfo;
                FieldInfo = fieldInfo;
                Name = name;
                Parent = parent;
                IsRest = isRest;
            }
        }

        private sealed class Fields
        {
            internal readonly ReadOnlyCollection<Field> DefaultView;
            internal readonly bool IncludeRawView;

            internal Fields(ReadOnlyCollection<Field> defaultView, bool includeRawView)
            {
                DefaultView = defaultView;
                IncludeRawView = includeRawView;
            }
        }

        private Fields GetFields()
        {
            _lazyFields ??= GetFields(_typeAndInfo, _cardinality, _useRawView);
            return _lazyFields;
        }

        private static Fields GetFields(TypeAndCustomInfo declaringTypeAndInfo, int cardinality, bool useRawView)
        {
            Debug.Assert(declaringTypeAndInfo.Type.GetTupleCardinalityIfAny() == cardinality);

            var appDomain = declaringTypeAndInfo.ClrType.AppDomain;
            var customTypeInfoMap = CustomTypeInfoTypeArgumentMap.Create(declaringTypeAndInfo);
            var tupleElementNames = customTypeInfoMap.TupleElementNames;

            var builder = ArrayBuilder<Field>.GetInstance();
            Field parent = null;
            int offset = 0;
            bool includeRawView = false;

            while (true)
            {
                var declaringType = declaringTypeAndInfo.Type;
                int n = Math.Min(cardinality, TypeHelpers.TupleFieldRestPosition - 1);
                for (int index = 0; index < n; index++)
                {
                    var fieldName = TypeHelpers.GetTupleFieldName(index);
                    var field = declaringType.GetTupleField(fieldName);
                    if (field == null)
                    {
                        // Ignore missing fields.
                        continue;
                    }

                    var fieldTypeAndInfo = GetTupleFieldTypeAndInfo(appDomain, field, customTypeInfoMap);
                    if (!useRawView)
                    {
                        var name = CustomTypeInfo.GetTupleElementNameIfAny(tupleElementNames, offset + index);
                        if (name != null)
                        {
                            includeRawView = true;
                            builder.Add(new Field(declaringTypeAndInfo, fieldTypeAndInfo, field, name, parent, isRest: false));
                            continue;
                        }
                    }

                    builder.Add(new Field(
                        declaringTypeAndInfo,
                        fieldTypeAndInfo,
                        field,
                        (offset == 0) ? fieldName : TypeHelpers.GetTupleFieldName(offset + index),
                        parent,
                        isRest: false));
                }

                cardinality -= n;
                if (cardinality == 0)
                {
                    break;
                }

                var rest = declaringType.GetTupleField(TypeHelpers.TupleFieldRestName);
                if (rest == null)
                {
                    // Ignore remaining fields.
                    break;
                }

                var restTypeAndInfo = GetTupleFieldTypeAndInfo(appDomain, rest, customTypeInfoMap);
                var restField = new Field(declaringTypeAndInfo, restTypeAndInfo, rest, TypeHelpers.TupleFieldRestName, parent, isRest: true);

                if (useRawView)
                {
                    builder.Add(restField);
                    break;
                }

                includeRawView = true;
                parent = restField;
                declaringTypeAndInfo = restTypeAndInfo;
                offset += TypeHelpers.TupleFieldRestPosition - 1;
            }

            return new Fields(builder.ToImmutableAndFree(), includeRawView);
        }

        private static TypeAndCustomInfo GetTupleFieldTypeAndInfo(
            DkmClrAppDomain appDomain,
            FieldInfo field,
            CustomTypeInfoTypeArgumentMap customTypeInfoMap)
        {
            var declaringTypeDef = field.DeclaringType.GetGenericTypeDefinition();
            var fieldDef = declaringTypeDef.GetTupleField(field.Name);
            var fieldType = DkmClrType.Create(appDomain, field.FieldType);
            var fieldTypeInfo = customTypeInfoMap.SubstituteCustomTypeInfo(fieldDef.FieldType, null);
            return new TypeAndCustomInfo(fieldType, fieldTypeInfo);
        }

        private EvalResult CreateRawViewRow(
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value)
        {
            var displayName = Resources.RawView;
            var displayValue = value.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
            var displayType = ResultProvider.GetTypeName(
                inspectionContext,
                value,
                _typeAndInfo.ClrType,
                _typeAndInfo.Info,
                isPointerDereference: false);
            var expansion = new TupleExpansion(_typeAndInfo, _cardinality, useRawView: true);
            return new EvalResult(
                ExpansionKind.Explicit,
                displayName,
                default(TypeAndCustomInfo),
                _typeAndInfo,
                useDebuggerDisplay: false,
                value: value,
                displayValue: displayValue,
                expansion: expansion,
                childShouldParenthesize: parent.ChildShouldParenthesize,
                fullName: parent.FullNameWithoutFormatSpecifiers,
                childFullNamePrefixOpt: parent.ChildFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(parent.FormatSpecifiers, "raw"),
                category: DkmEvaluationResultCategory.Data,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext,
                displayName: displayName,
                displayType: displayType);
        }
    }
}
