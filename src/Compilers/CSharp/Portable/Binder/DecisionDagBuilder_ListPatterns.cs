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
        private void MakeTestsAndBindingsForLengthAndListPatterns(BoundDagTemp input, BoundRecursivePattern recursive, ArrayBuilder<BoundPatternBinding> bindings, ArrayBuilder<Tests> tests)
        {
            if (recursive.HasErrors)
                return;
            Debug.Assert(recursive.LengthProperty is not null || input.Type.IsSZArray());
            var syntax = recursive.Syntax;
            var lengthProperty = recursive.LengthProperty ?? (PropertySymbol)_compilation.GetSpecialTypeMember(SpecialMember.System_Array__Length);
            var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, lengthProperty, input);
            tests.Add(new Tests.One(lengthEvaluation));
            var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);

            if (recursive.LengthPattern is not null)
            {
                tests.Add(MakeTestsAndBindings(lengthTemp, recursive.LengthPattern, bindings));
            }

            if (recursive.ListPatternClause is { Subpatterns: { IsDefaultOrEmpty: false } subpatterns } clause)
            {
                tests.Add(new Tests.One(clause.HasSlice
                    ? new BoundDagRelationalTest(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(subpatterns.Length - 1), lengthTemp)
                    : new BoundDagValueTest(syntax, ConstantValue.Create(subpatterns.Length), lengthTemp)));

                for (int index = 0; index < subpatterns.Length; index++)
                {
                    var subpattern = subpatterns[index];
                    if (subpattern is BoundSlicePattern slice)
                    {
                        if (slice.Pattern is not null)
                        {
                            var sliceEvaluation = new BoundDagSliceEvaluation(syntax, slice.SliceType, lengthTemp, startIndex: index, endIndex: index - (subpatterns.Length - 1), slice.IndexerInfo, input);
                            tests.Add(new Tests.One(sliceEvaluation));
                            var sliceTemp = new BoundDagTemp(syntax, slice.SliceType, sliceEvaluation);
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

                void addIndexerTests(int index, BoundPattern subpattern)
                {
                    var indexEvaluation = new BoundDagIndexerEvaluation(syntax, clause.ElementType, lengthTemp, index, clause.IndexerInfo, input);
                    tests.Add(new Tests.One(indexEvaluation));
                    var indexTemp = new BoundDagTemp(syntax, clause.ElementType, indexEvaluation);
                    tests.Add(MakeTestsAndBindings(indexTemp, subpattern, bindings));
                }
            }
        }
    }
}
