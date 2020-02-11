// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DkmEvaluationResultFlagsExtensions
    {
        public static bool Includes(this DkmEvaluationResultFlags flags, DkmEvaluationResultFlags desired)
        {
            return (flags & desired) == desired;
        }

        internal static DkmInspectionContext With(this DkmInspectionContext inspectionContext, DkmEvaluationFlags flags)
        {
            return DkmInspectionContext.Create(
                inspectionContext.InspectionSession,
                inspectionContext.RuntimeInstance,
                inspectionContext.Thread,
                inspectionContext.Timeout,
                inspectionContext.EvaluationFlags | flags,
                inspectionContext.FuncEvalFlags,
                inspectionContext.Radix,
                inspectionContext.Language,
                inspectionContext.ReturnValue,
                inspectionContext.AdditionalVisualizationData,
                inspectionContext.AdditionalVisualizationDataPriority,
                inspectionContext.ReturnValues);
        }
    }
}
