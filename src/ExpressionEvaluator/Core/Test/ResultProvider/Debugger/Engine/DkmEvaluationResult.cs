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
    public abstract class DkmEvaluationResult : DkmDataContainer
    {
        public readonly DkmInspectionContext InspectionContext;
        public readonly DkmStackWalkFrame StackFrame;
        public readonly string Name;
        public readonly string FullName;
        public readonly DkmEvaluationResultFlags Flags;
        public readonly string Type;

        internal DkmEvaluationResult(
            DkmInspectionContext InspectionContext,
            DkmStackWalkFrame StackFrame,
            string Name,
            string FullName,
            DkmEvaluationResultFlags Flags,
            string Type,
            DkmDataItem DataItem)
        {
            this.InspectionContext = InspectionContext;
            this.StackFrame = StackFrame;
            this.Name = Name;
            this.FullName = FullName;
            this.Flags = Flags;
            this.Type = Type;

            if (DataItem != null)
            {
                this.SetDataItem(DkmDataCreationDisposition.CreateNew, DataItem);
            }
        }

        public void GetChildren(DkmWorkList workList, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
        {
            InspectionContext.InspectionSession.InvokeResultProvider(
                this,
                MethodId.GetChildren,
                r =>
                {
                    r.GetChildren(this, workList, initialRequestSize, inspectionContext, completionRoutine);
                    return (object)null;
                });
        }

        public string GetUnderlyingString()
        {
            return InspectionContext.InspectionSession.InvokeResultProvider(this, MethodId.GetUnderlyingString, r => r.GetUnderlyingString(this));
        }
    }
}
