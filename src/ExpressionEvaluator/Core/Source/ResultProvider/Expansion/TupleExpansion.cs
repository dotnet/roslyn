// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal static TupleExpansion CreateExpansion(DkmClrValue value, TypeAndCustomInfo declaredTypeAndInfo, int cardinality)
        {
            if (value.IsNull)
            {
                // No expansion.
                return null;
            }
            return new TupleExpansion(new TypeAndCustomInfo(value.Type, declaredTypeAndInfo.Info), cardinality);
        }

        private readonly TypeAndCustomInfo _typeAndInfo;
        private readonly int _cardinality;
        private ReadOnlyCollection<Field> _lazyFields;

        private TupleExpansion(TypeAndCustomInfo typeAndInfo, int cardinality)
        {
            _typeAndInfo = typeAndInfo;
            _cardinality = cardinality;
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

            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, fields.Count, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                var row = GetMemberRow(resultProvider, inspectionContext, value, fields[i + offset], parent);
                rows.Add(row);
            }

            index += fields.Count;
        }

        private static EvalResult GetMemberRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            Field field,
            EvalResultDataItem parent)
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
                    parentFullName = fullNameProvider.GetClrCastExpression(inspectionContext, parentFullName, parentRuntimeType, customTypeInfo: null, parenthesizeArgument: false, parenthesizeEntireExpression: true);
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
            return resultProvider.CreateDataItem(
                inspectionContext,
                field.Name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: field.FieldTypeAndInfo,
                value: fieldValue,
                useDebuggerDisplay: false,
                expansionFlags: ExpansionFlags.All,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: fieldValue.EvalFlags,
                evalFlags: DkmEvaluationFlags.None);
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
            fullName = (parentFullName == null) ?
                null :
                fullNameProvider.GetClrMemberName(
                    inspectionContext,
                    parentFullName,
                    declaringType: field.DeclaringTypeAndInfo.ClrType,
                    declaringTypeInfo: null,
                    memberName: fieldName,
                    memberAccessRequiresExplicitCast: false,
                    memberIsStatic: false);
            return value.GetFieldValue(fieldName, inspectionContext);
        }

        private sealed class Field
        {
            internal readonly TypeAndCustomInfo DeclaringTypeAndInfo;
            internal readonly TypeAndCustomInfo FieldTypeAndInfo;
            internal readonly FieldInfo FieldInfo; // type field
            internal readonly string Name;
            internal readonly Field Parent; // parent Rest field, if any

            internal Field(
                TypeAndCustomInfo declaringTypeAndInfo,
                TypeAndCustomInfo fieldTypeAndInfo,
                FieldInfo fieldInfo,
                string name,
                Field parent)
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
            }
        }

        private ReadOnlyCollection<Field> GetFields()
        {
            if (_lazyFields == null)
            {
                _lazyFields = GetFields(_typeAndInfo, _cardinality);
            }
            return _lazyFields;
        }

        private static ReadOnlyCollection<Field> GetFields(TypeAndCustomInfo declaringTypeAndInfo, int cardinality)
        {
            Debug.Assert(declaringTypeAndInfo.Type.GetTupleCardinalityIfAny() == cardinality);

            var appDomain = declaringTypeAndInfo.ClrType.AppDomain;

            var customTypeInfoMap = CustomTypeInfoTypeArgumentMap.Create(declaringTypeAndInfo);
            var tupleElementNames = customTypeInfoMap.TupleElementNames;

            var builder = ArrayBuilder<Field>.GetInstance();
            Field parent = null;
            int offset = 0;

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
                    var name = CustomTypeInfo.GetTupleElementNameIfAny(tupleElementNames, offset + index);
                    if (name != null)
                    {
                        builder.Add(new Field(declaringTypeAndInfo, fieldTypeAndInfo, field, name, parent));
                    }
                    builder.Add(new Field(
                        declaringTypeAndInfo,
                        fieldTypeAndInfo,
                        field,
                        (offset == 0) ? fieldName : TypeHelpers.GetTupleFieldName(offset + index),
                        parent));
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
                parent = new Field(declaringTypeAndInfo, restTypeAndInfo, rest, TypeHelpers.TupleFieldRestName, parent);
                declaringTypeAndInfo = restTypeAndInfo;
                offset += TypeHelpers.TupleFieldRestPosition - 1;
            }

            // If there were any nested ValueTuples,
            // add the Rest field of the outermost.
            if (parent != null)
            {
                while (parent.Parent != null)
                {
                    parent = parent.Parent;
                }
                builder.Add(parent);
            }

            return builder.ToImmutableAndFree();
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
    }
}
