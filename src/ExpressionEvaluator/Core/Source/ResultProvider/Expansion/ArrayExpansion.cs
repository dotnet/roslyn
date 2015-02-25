// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ArrayExpansion : Expansion
    {
        private readonly ReadOnlyCollection<int> _divisors;
        private readonly ReadOnlyCollection<int> _lowerBounds;
        private readonly int _count;

        internal static ArrayExpansion CreateExpansion(ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds)
        {
            Debug.Assert(sizes != null);
            Debug.Assert(lowerBounds != null);
            Debug.Assert(sizes.Count > 0);
            Debug.Assert(sizes.Count == lowerBounds.Count);

            int count = 1;
            foreach (var size in sizes)
            {
                count *= size;
            }
            return (count > 0) ? new ArrayExpansion(sizes, lowerBounds, count) : null;
        }

        private ArrayExpansion(ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds, int count)
        {
            Debug.Assert(count > 0);
            _divisors = CalculateDivisors(sizes);
            _lowerBounds = lowerBounds;
            _count = count;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResultDataItem> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            int startIndex2;
            int count2;
            GetIntersection(startIndex, count, index, _count, out startIndex2, out count2);

            int offset = startIndex2 - index;
            for (int i = 0; i < count2; i++)
            {
                rows.Add(GetRow(resultProvider, inspectionContext, value, i + offset, parent));
            }

            index += _count;
        }

        private EvalResultDataItem GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            int index,
            EvalResultDataItem parent)
        {
            var indices = GetIndices(index);
            var formatter = resultProvider.Formatter;
            var name = formatter.GetArrayIndexExpression(indices);
            var elementType = value.Type.ElementType;
            var element = value.GetArrayElement(indices, inspectionContext);
            var fullName = GetFullName(parent, name, formatter);
            return resultProvider.CreateDataItem(
                inspectionContext,
                name,
                typeDeclaringMember: null,
                declaredType: elementType.GetLmrType(),
                value: element,
                parent: parent,
                expansionFlags: ExpansionFlags.IncludeBaseMembers,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: element.EvalFlags,
                evalFlags: inspectionContext.EvaluationFlags);
        }

        private int[] GetIndices(int index)
        {
            if (_divisors == null)
            {
                return new[] { index };
            }

            var n = _divisors.Count;
            var indices = new int[n];
            for (int i = 0; i < n; i++)
            {
                int divisor = _divisors[i];
                indices[i] = _lowerBounds[i] + index / divisor;
                index = index % divisor;
            }
            return indices;
        }

        private static ReadOnlyCollection<int> CalculateDivisors(ReadOnlyCollection<int> sizes)
        {
            var n = sizes.Count;
            if (n == 1)
            {
                return null;
            }

            var divisors = new int[n];
            divisors[n - 1] = 1;
            for (int i = n - 1; i > 0; i--)
            {
                divisors[i - 1] = divisors[i] * sizes[i];
            }
            return new ReadOnlyCollection<int>(divisors);
        }

        private static string GetFullName(EvalResultDataItem parent, string name, Formatter formatter)
        {
            var parentFullName = parent.ChildFullNamePrefix;
            if (parent.ChildShouldParenthesize)
            {
                parentFullName = $"({parentFullName})";
            }
            var parentRuntimeType = parent.Value.Type.GetLmrType();
            if (!parent.DeclaredType.Equals(parentRuntimeType))
            {
                parentFullName = formatter.GetCastExpression(
                    parentFullName,
                    formatter.GetTypeName(parentRuntimeType, escapeKeywordIdentifiers: true),
                    parenthesizeEntireExpression: true);
            }
            return parentFullName + name;
        }
    }
}
