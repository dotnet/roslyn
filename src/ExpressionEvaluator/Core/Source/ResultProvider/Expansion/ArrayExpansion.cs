// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ArrayExpansion : Expansion
    {
        private readonly TypeAndCustomInfo _elementTypeAndInfo;
        private readonly ReadOnlyCollection<int> _divisors;
        private readonly ReadOnlyCollection<int> _lowerBounds;
        private readonly int _count;

        internal static ArrayExpansion CreateExpansion(TypeAndCustomInfo elementTypeAndInfo, ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds)
        {
            Debug.Assert(elementTypeAndInfo.Type != null);
            Debug.Assert(sizes != null);
            Debug.Assert(lowerBounds != null);
            Debug.Assert(sizes.Count > 0);
            Debug.Assert(sizes.Count == lowerBounds.Count);

            int count = 1;
            foreach (var size in sizes)
            {
                count *= size;
            }
            return (count > 0) ? new ArrayExpansion(elementTypeAndInfo, sizes, lowerBounds, count) : null;
        }

        private ArrayExpansion(TypeAndCustomInfo elementTypeAndInfo, ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds, int count)
        {
            Debug.Assert(count > 0);
            _elementTypeAndInfo = elementTypeAndInfo;
            _divisors = CalculateDivisors(sizes);
            _lowerBounds = lowerBounds;
            _count = count;
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

        private EvalResult GetRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            int index,
            EvalResultDataItem parent)
        {
            var indices = GetIndices(index);
            var fullNameProvider = resultProvider.FullNameProvider;
            var name = fullNameProvider.GetClrArrayIndexExpression(inspectionContext, GetIndicesAsStrings(indices));
            var element = value.GetArrayElement(indices, inspectionContext);
            var fullName = GetFullName(inspectionContext, parent, name, fullNameProvider);
            return resultProvider.CreateDataItem(
                inspectionContext,
                name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: _elementTypeAndInfo,
                value: element,
                useDebuggerDisplay: parent != null,
                expansionFlags: ExpansionFlags.IncludeBaseMembers,
                childShouldParenthesize: false,
                fullName: fullName,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: element.EvalFlags,
                evalFlags: inspectionContext.EvaluationFlags,
                canFavorite: false,
                isFavorite: false,
                supportsFavorites: true);
        }

        private int[] GetIndices(int index)
        {
            if (_divisors == null)
            {
                // _divisors is null if dimension is 1, but
                // _lowerBounds need not necessarily be so.
                Debug.Assert(_lowerBounds == null || _lowerBounds.Count == 1);
                int lowerBound = _lowerBounds != null && _lowerBounds.Count == 1 ? _lowerBounds[0] : 0;
                return new[] { lowerBound + index };
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

        private static string[] GetIndicesAsStrings(int[] indices)
        {
            var n = indices.Length;
            var strings = new string[n];
            for (int i = 0; i < n; i++)
            {
                strings[i] = indices[i].ToString();
            }
            return strings;
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

        private static string GetFullName(DkmInspectionContext inspectionContext, EvalResultDataItem parent, string name, IDkmClrFullNameProvider fullNameProvider)
        {
            var parentFullName = parent.ChildFullNamePrefix;
            if (parentFullName == null)
            {
                return null;
            }

            if (parent.ChildShouldParenthesize)
            {
                parentFullName = parentFullName.Parenthesize();
            }
            var parentRuntimeType = parent.Value.Type;
            if (!parent.DeclaredTypeAndInfo.Type.Equals(parentRuntimeType.GetLmrType()))
            {
                parentFullName = fullNameProvider.GetClrCastExpression(
                    inspectionContext,
                    parentFullName,
                    parentRuntimeType,
                    customTypeInfo: null,
                    castExpressionOptions: DkmClrCastExpressionOptions.ParenthesizeEntireExpression);
                if (parentFullName == null)
                {
                    return null; // Contains invalid identifier.
                }
            }
            return parentFullName + name;
        }
    }
}
