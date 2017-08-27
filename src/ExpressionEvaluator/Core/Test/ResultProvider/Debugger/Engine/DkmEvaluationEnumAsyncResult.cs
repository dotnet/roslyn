﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public struct DkmEvaluationEnumAsyncResult
    {
        public DkmEvaluationEnumAsyncResult(DkmEvaluationResult[] Items)
            : this()
        {
            if (Items == null)
            {
                throw new ArgumentNullException();
            }
            this.Items = Items;
        }

        public DkmEvaluationResult[] Items { get; internal set; }

        internal Exception Exception { get; set; }

        public static DkmEvaluationEnumAsyncResult CreateErrorResult(Exception exception)
        {
            return new DkmEvaluationEnumAsyncResult(new DkmEvaluationResult[0]) { Exception = exception };
        }
    }
}
