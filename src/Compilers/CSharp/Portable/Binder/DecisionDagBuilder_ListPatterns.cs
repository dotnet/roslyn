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

            if (recursive.ListPatternClause is { Subpatterns: var subpatterns } clause)
            {
                Debug.Assert(!subpatterns.IsDefaultOrEmpty);
                Debug.Assert(subpatterns.Count(p => p.Kind == BoundKind.SlicePattern) <= 1);

                tests.Add(new Tests.One(clause.HasSlice
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
                            var sliceEvaluation = new BoundDagSliceEvaluation(syntax, slicePattern.InputType, lengthTemp, startIndex: startIndex, endIndex: index, slice.IndexerAccess, slice.SliceMethod, input);
                            tests.Add(new Tests.One(sliceEvaluation));
                            var sliceTemp = new BoundDagTemp(syntax, slicePattern.InputType, sliceEvaluation);
                            tests.Add(MakeTestsAndBindings(sliceTemp, slicePattern, bindings));
                        }

                        continue;
                    }

                    var indexEvaluation = new BoundDagIndexerEvaluation(syntax, subpattern.InputType, lengthTemp, index++, clause.IndexerAccess, clause.IndexerSymbol, input);
                    tests.Add(new Tests.One(indexEvaluation));
                    var indexTemp = new BoundDagTemp(syntax, subpattern.InputType, indexEvaluation);
                    tests.Add(MakeTestsAndBindings(indexTemp, subpattern, bindings));
                }
            }
        }
    }
}
