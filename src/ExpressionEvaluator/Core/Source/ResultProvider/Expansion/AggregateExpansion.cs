// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class AggregateExpansion : Expansion
    {
        private readonly Expansion[] _expansions;

        internal static Expansion CreateExpansion(ArrayBuilder<Expansion> expansions)
        {
            switch (expansions.Count)
            {
                case 0:
                    return null;
                case 1:
                    return expansions[0];
                default:
                    return new AggregateExpansion(expansions.ToArray());
            }
        }

        internal AggregateExpansion(Expansion[] expansions)
        {
            _expansions = expansions;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResult> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            foreach (var expansion in _expansions)
            {
                expansion.GetRows(resultProvider, rows, inspectionContext, parent, value, startIndex, count, visitAll, ref index);
                if (!visitAll && (index >= startIndex + count))
                {
                    return;
                }
            }
        }
    }
}
