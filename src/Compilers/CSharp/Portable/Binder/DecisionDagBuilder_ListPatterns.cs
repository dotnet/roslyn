// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPattern list, out BoundDagTemp output, ArrayBuilder<BoundPatternBinding> bindings)
        {
            Debug.Assert(input.Type.IsErrorType() || list.HasErrors || list.InputType.IsErrorType() ||
                         input.Type.Equals(list.InputType, TypeCompareKind.AllIgnoreOptions) &&
                         input.Type.StrippedType().Equals(list.NarrowedType, TypeCompareKind.ConsiderEverything) &&
                         list.Subpatterns.Count(p => p.Kind == BoundKind.SlicePattern) == (list.HasSlice ? 1 : 0) &&
                         list.LengthAccess is not null);

            var syntax = list.Syntax;
            var subpatterns = list.Subpatterns;
            var tests = ArrayBuilder<Tests>.GetInstance(4 + subpatterns.Length * 2);
            output = input = MakeConvertToType(input, list.Syntax, list.NarrowedType, isExplicitTest: false, tests);

            if (list.HasErrors)
            {
                tests.Add(new Tests.One(new BoundDagTypeTest(list.Syntax, ErrorType(), input, hasErrors: true)));
            }
            else if (list.HasSlice &&
                     subpatterns.Length == 1 &&
                     subpatterns[0] is BoundSlicePattern { Pattern: null })
            {
                // If `..` is the only pattern in the list, bail. This is a no-op and we don't need to match anything further.
            }
            else
            {
                Debug.Assert(list.LengthAccess is not null);
                var lengthProperty = Binder.GetPropertySymbol(list.LengthAccess, out _, out _);
                Debug.Assert(lengthProperty is not null);
                var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, lengthProperty, isLengthOrCount: true, input);
                tests.Add(new Tests.One(lengthEvaluation));
                var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
                tests.Add(new Tests.One(list.HasSlice
                    ? new BoundDagRelationalTest(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(subpatterns.Length - 1), lengthTemp)
                    : new BoundDagValueTest(syntax, ConstantValue.Create(subpatterns.Length), lengthTemp)));

                int index = 0;
                foreach (BoundPattern subpattern in subpatterns)
                {
                    if (subpattern is BoundSlicePattern slice)
                    {
                        int startIndex = index;
                        index -= subpatterns.Length - 1;

                        if (slice.Pattern is BoundPattern slicePattern)
                        {
                            Debug.Assert(slice.IndexerAccess is not null);
                            Debug.Assert(index <= 0);
                            Debug.Assert(slice.ReceiverPlaceholder is not null);
                            Debug.Assert(slice.ArgumentPlaceholder is not null);

                            var sliceEvaluation = new BoundDagSliceEvaluation(slicePattern.Syntax, slicePattern.InputType, lengthTemp, startIndex: startIndex, endIndex: index,
                                slice.IndexerAccess, slice.ReceiverPlaceholder, slice.ArgumentPlaceholder, input);

                            tests.Add(new Tests.One(sliceEvaluation));
                            var sliceTemp = new BoundDagTemp(slicePattern.Syntax, slicePattern.InputType, sliceEvaluation);
                            tests.Add(MakeTestsAndBindings(sliceTemp, slicePattern, bindings));
                        }

                        continue;
                    }

                    Debug.Assert(list.IndexerAccess is not null);
                    Debug.Assert(list.ReceiverPlaceholder is not null);
                    Debug.Assert(list.ArgumentPlaceholder is not null);

                    var indexEvaluation = new BoundDagIndexerEvaluation(subpattern.Syntax, subpattern.InputType, lengthTemp, index++,
                        list.IndexerAccess, list.ReceiverPlaceholder, list.ArgumentPlaceholder, input);

                    tests.Add(new Tests.One(indexEvaluation));
                    var indexTemp = new BoundDagTemp(subpattern.Syntax, subpattern.InputType, indexEvaluation);
                    tests.Add(MakeTestsAndBindings(indexTemp, subpattern, bindings));
                }
            }

            if (list.VariableAccess is not null)
            {
                bindings.Add(new BoundPatternBinding(list.VariableAccess, input));
            }

            return Tests.AndSequence.Create(tests);
        }
    }
}
