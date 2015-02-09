// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using System;
using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public abstract class DkmEvaluationResult : DkmDataContainer
    {
        public string FullName { get; internal set; }
        public string Name { get; internal set; }
        public DkmInspectionContext InspectionContext { get; internal set; }
        public DkmStackWalkFrame StackFrame { get; internal set; }

        public virtual void GetChildren(DkmWorkList workList, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
        {
            throw new NotImplementedException();
        }

        public virtual string GetUnderlyingString()
        {
            throw new NotImplementedException();
        }
    }
}
