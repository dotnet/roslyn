// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmEvaluationResultEnumContext : DkmDataContainer
    {
        public readonly int Count;
        public readonly DkmInspectionContext InspectionContext;

        internal DkmEvaluationResultEnumContext(int count, DkmInspectionContext inspectionContext)
        {
            this.Count = count;
            this.InspectionContext = inspectionContext;
        }

        public static DkmEvaluationResultEnumContext Create(int Count, DkmStackWalkFrame StackFrame, DkmInspectionContext InspectionContext, DkmDataItem DataItem)
        {
            var enumContext = new DkmEvaluationResultEnumContext(Count, InspectionContext);
            if (DataItem != null)
            {
                enumContext.SetDataItem(DkmDataCreationDisposition.CreateNew, DataItem);
            }
            return enumContext;
        }

        public void GetItems(DkmWorkList workList, int startIndex, int count, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
        {
            InspectionContext.InspectionSession.InvokeResultProvider(
                MethodId.GetItems,
                r =>
                {
                    r.GetItems(this, workList, startIndex, count, completionRoutine);
                    return (object)null;
                });
        }
    }
}
