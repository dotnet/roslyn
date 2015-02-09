// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class TypeVariablesExpansion : Expansion
    {
        private readonly Type[] _typeParameters;
        private readonly Type[] _typeArguments;

        internal TypeVariablesExpansion(Type declaredType)
        {
            Debug.Assert(declaredType.IsGenericType);
            Debug.Assert(!declaredType.IsGenericTypeDefinition);

            var typeDef = declaredType.GetGenericTypeDefinition();
            _typeParameters = typeDef.GetGenericArguments();
            _typeArguments = declaredType.GetGenericArguments();

            Debug.Assert(_typeParameters.Length == _typeArguments.Length);
            Debug.Assert(Array.TrueForAll(_typeParameters, t => t.IsGenericParameter));
            Debug.Assert(Array.TrueForAll(_typeArguments, t => !t.IsGenericParameter));
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<DkmEvaluationResult> rows,
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
            GetIntersection(startIndex, count, index, _typeArguments.Length, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                rows.Add(GetRow(resultProvider, value, i + offset, parent));
            }

            index += _typeArguments.Length;
        }

        private DkmEvaluationResult GetRow(ResultProvider resultProvider, DkmClrValue value, int index, EvalResultDataItem parent)
        {
            var inspectionContext = value.InspectionContext;
            var appDomain = value.Type.AppDomain;
            var typeParameter = _typeParameters[index];
            var typeArgument = _typeArguments[index];
            var type = DkmClrType.Create(appDomain, typeArgument);
            var name = typeParameter.Name;
            var dataItem = new EvalResultDataItem(
                name,
                typeDeclaringMember: null,
                declaredType: typeArgument,
                value: null,
                expansion: null,
                childShouldParenthesize: false,
                fullName: null,
                childFullNamePrefixOpt: null,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Data,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null);
            var typeName = inspectionContext.GetTypeName(DkmClrType.Create(appDomain, typeArgument));
            return DkmSuccessEvaluationResult.Create(
                inspectionContext,
                value.StackFrame,
                name,
                dataItem.FullName,
                dataItem.Flags,
                Value: typeName,
                EditableValue: null,
                Type: typeName,
                Category: dataItem.Category,
                Access: value.Access,
                StorageType: value.StorageType,
                TypeModifierFlags: value.TypeModifierFlags,
                Address: value.Address,
                CustomUIVisualizers: null,
                ExternalModules: null,
                DataItem: dataItem);
        }
    }
}
