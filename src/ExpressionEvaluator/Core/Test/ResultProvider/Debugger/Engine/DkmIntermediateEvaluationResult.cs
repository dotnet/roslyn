// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmIntermediateEvaluationResult : DkmEvaluationResult
    {
        public string Expression { get; private set; }
        public DkmLanguage IntermediateLanguage { get; private set; }
        public DkmRuntimeInstance TargetRuntime { get; private set; }
        
        public static DkmIntermediateEvaluationResult Create(DkmInspectionContext InspectionContext, DkmStackWalkFrame StackFrame, string Name, string FullName, string Expression, DkmLanguage IntermediateLanguage, DkmRuntimeInstance TargetRuntime, DkmDataItem DataItem)
        {
            DkmIntermediateEvaluationResult result = new DkmIntermediateEvaluationResult
            {
                InspectionContext = InspectionContext,
                Name = Name,
                FullName = FullName,
                Expression = Expression,
                IntermediateLanguage = IntermediateLanguage,
                TargetRuntime = TargetRuntime
            };

            if (DataItem != null)
            {
                result.SetDataItem(DkmDataCreationDisposition.CreateNew, DataItem);
            }

            return result;
        }
    }
}
