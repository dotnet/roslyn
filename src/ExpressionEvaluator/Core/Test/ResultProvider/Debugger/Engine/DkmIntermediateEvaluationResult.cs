// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmIntermediateEvaluationResult : DkmEvaluationResult
    {
        public readonly string Expression;
        public readonly DkmLanguage IntermediateLanguage;
        public readonly DkmRuntimeInstance TargetRuntime;

        private DkmIntermediateEvaluationResult(
            DkmInspectionContext inspectionContext,
            DkmStackWalkFrame stackFrame,
            string name,
            string fullName,
            string expression,
            DkmLanguage intermediateLanguage,
            DkmRuntimeInstance targetRuntime,
            DkmDataItem dataItem) :
            base(inspectionContext, stackFrame, name, fullName, DkmEvaluationResultFlags.None, null, dataItem)
        {
            this.Expression = expression;
            this.IntermediateLanguage = intermediateLanguage;
            this.TargetRuntime = targetRuntime;
        }

        public static DkmIntermediateEvaluationResult Create(
            DkmInspectionContext InspectionContext,
            DkmStackWalkFrame StackFrame,
            string Name,
            string FullName,
            string Expression,
            DkmLanguage IntermediateLanguage,
            DkmRuntimeInstance TargetRuntime,
            DkmDataItem DataItem)
        {
            return new DkmIntermediateEvaluationResult(
                InspectionContext,
                StackFrame,
                Name,
                FullName,
                Expression,
                IntermediateLanguage,
                TargetRuntime,
                DataItem);
        }
    }
}
