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
        private void MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithRangeIndexerPattern pattern, ArrayBuilder<BoundPatternBinding> bindings, ArrayBuilder<Tests> tests)
        {
            MakeTestsAndBindingsForListPattern(input, pattern, bindings, pattern.GetLengthProperty, pattern.GetItemProperty, tests);
        }

        private void MakeTestsAndBindingsForListPattern(
            BoundDagTemp input, BoundListPatternClause pattern, ArrayBuilder<BoundPatternBinding> bindings,
            PropertySymbol getLengthProperty, PropertySymbol? getItemProperty, ArrayBuilder<Tests> tests)
        {
            var syntax = pattern.Syntax;
            var subpatterns = pattern.Subpatterns;

            MakeCheckNotNull(input, syntax, isExplicitTest: false, tests);

            var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, getLengthProperty, input);
            tests.Add(new Tests.One(lengthEvaluation));

            var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
            MakeLengthTests(syntax, pattern, bindings, lengthTemp, tests);

            if (!subpatterns.IsDefaultOrEmpty)
            {
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

                        break;
                    }

                    addIndexerTests(index, subpattern);
                }
            }

            void addIndexerTests(int index, BoundPattern subpattern)
            {
                var indexEvaluation = new BoundDagIndexEvaluation(syntax, getItemProperty, lengthTemp, index, input);
                tests.Add(new Tests.One(indexEvaluation));
                var indexTemp = new BoundDagTemp(syntax, pattern.ElementType!, indexEvaluation);
                tests.Add(MakeTestsAndBindings(indexTemp, subpattern, bindings));
            }
        }

        private void MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithArray pattern, ArrayBuilder<BoundPatternBinding> bindings, ArrayBuilder<Tests> tests)
        {
            if (input.Type.IsErrorType())
                return;
            Debug.Assert(input.Type.IsSZArray());
            var getLengthProperty = (PropertySymbol)_compilation.GetSpecialTypeMember(SpecialMember.System_Array__Length);
            MakeTestsAndBindingsForListPattern(input, pattern, bindings, getLengthProperty, getItemProperty: null, tests);
        }

        private static void MakeInferredLengthTests(SyntaxNode syntax, BoundListPatternClause pattern, BoundDagTemp lengthTemp, ArrayBuilder<Tests> tests)
        {
            if (pattern.Subpatterns.IsDefaultOrEmpty)
                return;
            tests.Add(new Tests.One(pattern.HasSlice
                ? new BoundDagRelationalTest(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(pattern.Subpatterns.Length - 1), lengthTemp)
                : new BoundDagValueTest(syntax, ConstantValue.Create(pattern.Subpatterns.Length), lengthTemp)));
        }

        private void MakeLengthTests(SyntaxNode syntax, BoundListPatternClause pattern, ArrayBuilder<BoundPatternBinding> bindings, BoundDagTemp countTemp, ArrayBuilder<Tests> tests)
        {
            if (pattern.LengthPattern != null)
                tests.Add(MakeTestsAndBindings(countTemp, pattern.LengthPattern, bindings));
            MakeInferredLengthTests(syntax, pattern, countTemp, tests);
        }
    }
}
