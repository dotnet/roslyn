// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
