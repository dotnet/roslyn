// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmFailedEvaluationResult : DkmEvaluationResult
    {
        public readonly string ErrorMessage;

        private DkmFailedEvaluationResult(
            DkmInspectionContext inspectionContext,
            DkmStackWalkFrame stackFrame,
            string name,
            string fullName,
            string errorMessage,
            DkmEvaluationResultFlags flags,
            string type,
            DkmDataItem dataItem) :
            base(inspectionContext, stackFrame, name, fullName, flags, type, dataItem)
        {
            this.ErrorMessage = errorMessage;
        }

        public static DkmFailedEvaluationResult Create(
            DkmInspectionContext InspectionContext,
            DkmStackWalkFrame StackFrame,
            string Name,
            string FullName,
            string ErrorMessage,
            DkmEvaluationResultFlags Flags,
            string Type,
            DkmDataItem DataItem)
        {
            return new DkmFailedEvaluationResult(
                InspectionContext,
                StackFrame,
                Name,
                FullName,
                ErrorMessage,
                Flags,
                Type,
                DataItem);
        }
    }
}
