// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// This type exists to protect <see cref="ExpressionEvaluator.EvalResultDataItem"/> from
    /// spurious <see cref="DkmDataItem.OnClose"/> calls.  We need to attach the same information
    /// to <see cref="Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResult"/> and 
    /// <see cref="Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResultEnumContext"/>
    /// but they have different lifetimes.  Enum contexts (which are effectively child lists)
    /// are closed before the corresponding evaluation results.  We don't want to actually clean
    /// up until the evaluation result is closed.
    /// </summary>
    internal sealed class EnumContextDataItem : DkmDataItem
    {
        public readonly EvalResultDataItem EvalResultDataItem;

        /// <remarks>
        /// Only <see cref="ExpressionEvaluator.EvalResultDataItem"/> is expected to instantiate this type.
        /// </remarks>
        public EnumContextDataItem(EvalResultDataItem dataItem)
        {
            this.EvalResultDataItem = dataItem;
        }
    }
}
