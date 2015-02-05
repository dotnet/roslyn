// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class PointerDereferenceExpansion : Expansion
    {
        private readonly Type _elementType;

        public PointerDereferenceExpansion(Type elementType)
        {
            Debug.Assert(elementType != null);
            _elementType = elementType;
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
            if (InRange(startIndex, count, index))
            {
                rows.Add(GetRow(resultProvider, inspectionContext, value, _elementType, parent));
            }

            index++;
        }

        private static DkmEvaluationResult GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue pointer,
            Type elementType,
            EvalResultDataItem parent)
        {
            var value = pointer.Dereference();
            var valueType = value.Type.GetLmrType();
            var wasExceptionThrown = value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown);

            string debuggerDisplayName;
            string debuggerDisplayValue;
            string debuggerDisplayType;
            value.GetDebuggerDisplayStrings(out debuggerDisplayName, out debuggerDisplayValue, out debuggerDisplayType);

            var declaredType = elementType;
            var typeName = debuggerDisplayType ?? pointer.InspectionContext.GetTypeName(DkmClrType.Create(pointer.Type.AppDomain, declaredType));
            var expansion = wasExceptionThrown
                ? null
                : resultProvider.GetTypeExpansion(inspectionContext, declaredType, value, ExpansionFlags.None);
            var fullName = string.Format("*{0}", parent.ChildFullNamePrefix);
            var editableValue = resultProvider.Formatter.GetEditableValue(value);

            // NB: Full name is based on the real (i.e. not DebuggerDisplay) name.  This is a change from dev12, 
            // which used the DebuggerDisplay name, causing surprising results in "Add Watch" scenarios.
            var dataItem = new EvalResultDataItem(
                name: null, // Okay for pointer dereferences.
                typeDeclaringMember: null,
                declaredType: declaredType,
                value: value,
                expansion: expansion,
                childShouldParenthesize: true,
                fullName: fullName,
                childFullNamePrefixOpt: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: DkmEvaluationResultFlags.None,
                editableValue: editableValue);

            var name = debuggerDisplayName ?? fullName;
            var display = debuggerDisplayValue ?? 
                (wasExceptionThrown ? string.Format(Resources.InvalidPointerDereference, fullName) : value.GetValueString());

            return ResultProvider.CreateEvaluationResult(
                value,
                name,
                typeName,
                display,
                dataItem);
        }
    }
}
