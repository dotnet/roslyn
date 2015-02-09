// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmFailedEvaluationResult : DkmEvaluationResult
    {
        public string ErrorMessage { get; private set; }
        public DkmEvaluationResultFlags Flags { get; private set; }
        public string Type { get; private set; }

        public static DkmFailedEvaluationResult Create(DkmInspectionContext InspectionContext, DkmStackWalkFrame StackFrame, string Name, string FullName, string ErrorMessage, DkmEvaluationResultFlags Flags, string Type, DkmDataItem DataItem)
        {
            DkmFailedEvaluationResult result = new DkmFailedEvaluationResult
            {
                InspectionContext = InspectionContext,
                Name = Name,
                FullName = FullName,
                ErrorMessage = ErrorMessage,
                Flags = Flags,
                Type = Type
            };

            if (DataItem != null)
            {
                result.SetDataItem(DkmDataCreationDisposition.CreateNew, DataItem);
            }

            return result;
        }
    }
}
