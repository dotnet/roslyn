// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithRangeIndexerPattern pattern, BoundPattern? lengthPattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            return MakeTestsAndBindingsForListPattern(input, pattern, lengthPattern, bindings, pattern.GetLengthProperty, pattern.GetItemProperty);
        }

        private Tests MakeTestsAndBindingsForListPattern(
            BoundDagTemp input, BoundListPattern pattern, BoundPattern? lengthPattern, ArrayBuilder<BoundPatternBinding> bindings,
            PropertySymbol getLengthProperty, PropertySymbol? getItemProperty)
        {
            var syntax = pattern.Syntax;
            var subpatterns = pattern.Subpatterns.NullToEmpty();

            var tests = ArrayBuilder<Tests>.GetInstance(4 + subpatterns.Length * 2);
            MakeCheckNotNull(input, syntax, isExplicitTest: false, tests);

            var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, getLengthProperty, input);
            tests.Add(new Tests.One(lengthEvaluation));

            var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
            MakeLengthTests(syntax, pattern, lengthPattern, bindings, lengthTemp, tests);

            for (int index = 0; index < subpatterns.Length; index++)
            {
                var subpattern = subpatterns[index];
                if (subpattern is BoundSlicePattern slice)
                {
                    if (slice.Pattern is not null)
                    {
                        var sliceEvaluation = new BoundDagSliceEvaluation(syntax, slice.SliceMethod, lengthTemp, startIndex: index, endIndex: index - (subpatterns.Length - 1), input);
                        tests.Add(new Tests.One(sliceEvaluation));
                        var sliceTemp = new BoundDagTemp(syntax, slice.SliceMethod is null ? input.Type : slice.SliceMethod.ReturnType, sliceEvaluation);
                        tests.Add(MakeTestsAndBindings(sliceTemp, slice.Pattern, bindings));
                    }

                    for (int i = subpatterns.Length - 1, j = -1; i > index; i--, j--)
                    {
                        addIndexerTests(index: j, subpatterns[i]);
                    }

                    goto done;
                }

                addIndexerTests(index, subpattern);
            }
done:
            return Tests.AndSequence.Create(tests);

            void addIndexerTests(int index, BoundPattern subpattern)
            {
                var indexEvaluation = new BoundDagIndexEvaluation(syntax, getItemProperty, lengthTemp, index, input);
                tests.Add(new Tests.One(indexEvaluation));
                var indexTemp = new BoundDagTemp(syntax, pattern.ElementType, indexEvaluation);
                tests.Add(MakeTestsAndBindings(indexTemp, subpattern, bindings));
            }
        }

        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithArray pattern, BoundPattern? lengthPattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            if (input.Type.IsErrorType())
                return Tests.True.Instance;
            Debug.Assert(input.Type.IsSZArray());
            var getLengthProperty = (PropertySymbol)_compilation.GetSpecialTypeMember(SpecialMember.System_Array__Length);
            return MakeTestsAndBindingsForListPattern(input, pattern, lengthPattern, bindings, getLengthProperty, getItemProperty: null);
        }

        private static Tests MakeInferredLengthTests(SyntaxNode syntax, BoundListPattern pattern, BoundDagTemp lengthTemp)
        {
            if (pattern.Subpatterns.IsDefault)
                return Tests.True.Instance;
            return new Tests.One(pattern.HasSlice
                ? new BoundDagRelationalTest(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(pattern.Subpatterns.Length - 1), lengthTemp)
                : new BoundDagValueTest(syntax, ConstantValue.Create(pattern.Subpatterns.Length), lengthTemp));
        }

        private Tests MakeLengthTests(SyntaxNode syntax, BoundListPattern listPattern, BoundPattern? lengthPattern, ArrayBuilder<BoundPatternBinding> bindings, BoundDagTemp countTemp)
        {
            var tests = ArrayBuilder<Tests>.GetInstance(2);
            MakeLengthTests(syntax, listPattern, lengthPattern, bindings, countTemp, tests);
            return Tests.AndSequence.Create(tests);
        }

        private void MakeLengthTests(SyntaxNode syntax, BoundListPattern listPattern, BoundPattern? lengthPattern, ArrayBuilder<BoundPatternBinding> bindings, BoundDagTemp countTemp, ArrayBuilder<Tests> tests)
        {
            if (lengthPattern != null)
                tests.Add(MakeTestsAndBindings(countTemp, lengthPattern, bindings));
            tests.Add(MakeInferredLengthTests(syntax, listPattern, countTemp));
        }
    }
}
