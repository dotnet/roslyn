// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class EnumContextDataItem : DkmDataItem
    {
        public readonly DkmEvaluationResult Result;

        public EnumContextDataItem(DkmEvaluationResult result)
        {
            this.Result = result;
        }
    }
}
