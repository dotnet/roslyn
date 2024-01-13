// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private readonly CustomTypeInfoTypeArgumentMap _customTypeInfoMap;

        internal TypeVariablesExpansion(TypeAndCustomInfo declaredTypeAndInfo)
        {
            var declaredType = declaredTypeAndInfo.Type;
            Debug.Assert(declaredType.IsGenericType);
            Debug.Assert(!declaredType.IsGenericTypeDefinition);

            _customTypeInfoMap = CustomTypeInfoTypeArgumentMap.Create(declaredTypeAndInfo);

            var typeDef = declaredType.GetGenericTypeDefinition();
            _typeParameters = typeDef.GetGenericArguments();
            _typeArguments = declaredType.GetGenericArguments();

            Debug.Assert(_typeParameters.Length == _typeArguments.Length);
            Debug.Assert(Array.TrueForAll(_typeParameters, t => t.IsGenericParameter));
            Debug.Assert(Array.TrueForAll(_typeArguments, t => !t.IsGenericParameter));
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
            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, _typeArguments.Length, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                rows.Add(GetRow(inspectionContext, value, i + offset, parent));
            }

            index += _typeArguments.Length;
        }

        private EvalResult GetRow(
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            int index,
            EvalResultDataItem parent)
        {
            var typeParameter = _typeParameters[index];
            var typeArgument = _typeArguments[index];
            var typeArgumentInfo = _customTypeInfoMap.SubstituteCustomTypeInfo(typeParameter, customInfo: null);
            var formatSpecifiers = Formatter.NoFormatSpecifiers;
            return new EvalResult(
                ExpansionKind.TypeVariable,
                typeParameter.Name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: new TypeAndCustomInfo(DkmClrType.Create(value.Type.AppDomain, typeArgument), typeArgumentInfo),
                useDebuggerDisplay: parent != null,
                value: value,
                displayValue: inspectionContext.GetTypeName(DkmClrType.Create(value.Type.AppDomain, typeArgument), typeArgumentInfo, formatSpecifiers),
                expansion: null,
                childShouldParenthesize: false,
                fullName: null,
                childFullNamePrefixOpt: null,
                formatSpecifiers: formatSpecifiers,
                category: DkmEvaluationResultCategory.Data,
                flags: DkmEvaluationResultFlags.ReadOnly,
                editableValue: null,
                inspectionContext: inspectionContext);
        }
    }
}
