// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public struct DkmGetChildrenAsyncResult
    {
        public DkmGetChildrenAsyncResult(DkmEvaluationResult[] InitialChildren, DkmEvaluationResultEnumContext EnumContext)
            : this()
        {
            if (InitialChildren == null)
            {
                throw new ArgumentNullException();
            }
            this.InitialChildren = InitialChildren;
            this.EnumContext = EnumContext;
        }

        public DkmEvaluationResultEnumContext EnumContext { get; }
        public DkmEvaluationResult[] InitialChildren { get; }

        internal Exception Exception { get; set; }

        public static DkmGetChildrenAsyncResult CreateErrorResult(Exception exception)
        {
            return new DkmGetChildrenAsyncResult(new DkmEvaluationResult[0], default(DkmEvaluationResultEnumContext)) { Exception = exception };
        }
    }
}
