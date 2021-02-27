// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithRangeIndexerPattern pattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            return MakeTestsAndBindingsForListPattern(input, pattern, bindings, pattern.GetLengthProperty, pattern.GetItemProperty);
        }

        private Tests MakeTestsAndBindingsForListPattern(
            BoundDagTemp input, BoundListPatternInfo pattern, ArrayBuilder<BoundPatternBinding> bindings,
            PropertySymbol getLengthProperty, PropertySymbol? getItemProperty)
        {
            var syntax = pattern.Syntax;
            var subpatterns = pattern.Subpatterns.NullToEmpty();

            var tests = ArrayBuilder<Tests>.GetInstance(4 + subpatterns.Length * 2);
            MakeCheckNotNull(input, syntax, isExplicitTest: false, tests);

            var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, getLengthProperty, input);
            tests.Add(new Tests.One(lengthEvaluation));

            var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
            MakeLengthTests(syntax, pattern, bindings, lengthTemp, tests);

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

        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input, BoundListPatternWithArray pattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            if (input.Type.IsErrorType())
                return Tests.True.Instance;
            Debug.Assert(input.Type.IsSZArray());
            var getLengthProperty = (PropertySymbol)_compilation.GetSpecialTypeMember(SpecialMember.System_Array__Length);
            return MakeTestsAndBindingsForListPattern(input, pattern, bindings, getLengthProperty, getItemProperty: null);
        }

        private static Tests MakeInferredLengthTests(SyntaxNode syntax, BoundListPatternInfo pattern, BoundDagTemp lengthTemp)
        {
            if (pattern.Subpatterns.IsDefault)
                return Tests.True.Instance;
            return new Tests.One(pattern.HasSlice
                ? new BoundDagRelationalTest(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(pattern.Subpatterns.Length - 1), lengthTemp)
                : new BoundDagValueTest(syntax, ConstantValue.Create(pattern.Subpatterns.Length), lengthTemp));
        }

        private Tests MakeTestsAndBindingsForListPattern(BoundDagTemp input,
            BoundListPatternWithEnumerablePattern pattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            var syntax = pattern.Syntax;
            var subpatterns = pattern.Subpatterns;
            var info = pattern.EnumeratorInfo;

            MethodSymbol moveNextMethod = info.MoveNextInfo.Method;
            PropertySymbol currentProperty = (PropertySymbol)info.CurrentPropertyGetter.AssociatedSymbol;

            Debug.Assert(moveNextMethod is not null);
            Debug.Assert(currentProperty is not null);

            var tests = ArrayBuilder<Tests>.GetInstance();

            // TryGetCount
            MethodSymbol? tryGetCountMethod = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Linq_Enumerable__TryGetNonEnumeratedCount));
            if (tryGetCountMethod != null)
            {
                var tryGetCountEvaluation = new BoundDagMethodEvaluation(syntax, tryGetCountMethod, index: 0, input);
                var successTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Boolean), tryGetCountEvaluation, index: 0);
                var countOutTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), tryGetCountEvaluation, index: 1);
                tests.Add(new Tests.One(tryGetCountEvaluation));
                tests.Add(makeDisjunction(
                    new Tests.One(new BoundDagValueTest(syntax, ConstantValue.Create(false), successTemp)),
                    MakeLengthTests(syntax, pattern, bindings, countOutTemp)));
            }

            // GetEnumerator
            var enumeratorEvaluation = new BoundDagEnumeratorEvaluation(syntax, info, input);
            tests.Add(new Tests.One(enumeratorEvaluation));
            var enumeratorTemp = new BoundDagTemp(syntax, info.GetEnumeratorInfo.Method.ReturnType, enumeratorEvaluation);
            var countTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), enumeratorEvaluation, index: 1);

            // BufferCtor
            var (bufferType, bufferCtor, pushMethod, popMethod) = getWellKnownMembers();
            var bufferCtorEvaluation = new BoundDagMethodEvaluation(syntax, bufferCtor, index: 0, enumeratorTemp);
            var bufferTemp = new BoundDagTemp(syntax, bufferType, bufferCtorEvaluation);
            tests.Add(new Tests.One(bufferCtorEvaluation));

            if (subpatterns.IsDefault)
            {
                Debug.Assert(pattern.LengthPattern is not null);
                addTrailingTests(0); // [3]
                goto done;
            }

            for (int index = 0; index < subpatterns.Length; index++)
            {
                BoundPattern subpattern = subpatterns[index];
                if (subpattern is BoundSlicePattern slice)
                {
                    if (slice.Pattern is not null)
                        throw new NotImplementedException("error: enumerator with slice");
                    addTrailingTests(index);
                    goto done;
                }

                tests.Add(new Tests.One(new BoundDagMoveNextTest(syntax, index, enumeratorTemp)));
                var incrementEvaluation = new BoundDagIncrementEvaluation(syntax, index, countTemp);
                tests.Add(new Tests.One(incrementEvaluation));
                var currentEvaluation = new BoundDagPropertyEvaluation(syntax, currentProperty, index, enumeratorTemp);
                tests.Add(new Tests.One(currentEvaluation));
                var currentTemp = new BoundDagTemp(syntax, pattern.ElementType, currentEvaluation);
                var currentPushEvaluation = new BoundDagMethodEvaluation(syntax, pushMethod, index, bufferTemp);
                tests.Add(new Tests.One(currentPushEvaluation));
                tests.Add(MakeTestsAndBindings(currentTemp, subpattern, bindings));
            }

            Debug.Assert(!pattern.HasSlice);
            tests.Add(Tests.Not.Create(new Tests.One(new BoundDagMoveNextTest(syntax, subpatterns.Length, enumeratorTemp))));

            if (pattern.LengthPattern is not null)
                computeLengthValueSet(countTemp); // {1,2,3} [3]

done:
            return Tests.AndSequence.Create(tests);

            // local functions

            void addTrailingTests(int indexOfSlice)
            {
                int currentIndex = indexOfSlice + 1;
                bool hasTrailing = !subpatterns.IsDefault && subpatterns.Length != currentIndex;
                if (pattern.LengthPattern is null && !hasTrailing)
                {
                    // {1,2,3, ..}
                    return;
                }

                var (minLength, maxLength) = computeLengthValueSet(countTemp)?.GetRange() ?? (int.MinValue, int.MaxValue);
                tests.Add(new Tests.One(new BoundDagIterationTest(syntax, hasTrailing ? pushMethod : null, bufferTemp, maxLength, enumeratorTemp)));
                tests.Add(MakeRelationalTests(syntax, BinaryOperatorKind.IntGreaterThanOrEqual, ConstantValue.Create(minLength), countTemp));

                if (!subpatterns.IsDefaultOrEmpty)
                {
                    for (int i = subpatterns.Length - 1, j = 1; i >= currentIndex; i--, j++)
                    {
                        var popEvaluation = new BoundDagMethodEvaluation(syntax, popMethod, index: j, bufferTemp);
                        tests.Add(new Tests.One(popEvaluation));
                        var popTemp = new BoundDagTemp(syntax, pattern.ElementType, popEvaluation);
                        tests.Add(MakeTestsAndBindings(popTemp, subpatterns[i], bindings));
                    }
                }
            }

            static Tests makeDisjunction(Tests test1, Tests test2)
            {
                var tests = ArrayBuilder<Tests>.GetInstance(2);
                tests.Add(test1);
                tests.Add(test2);
                return Tests.OrSequence.Create(tests);
            }

            INumericValueSet<int>? computeLengthValueSet(BoundDagTemp countTemp)
            {
                Tests lengthTests = MakeInferredLengthTests(syntax, pattern, countTemp);
                Tests lengthPatternTests = pattern.LengthPattern is not null ? MakeTestsAndBindings(countTemp, pattern.LengthPattern, bindings) : Tests.True.Instance;
                IValueSet values1 = lengthTests.ComputeIntValueSet();
                IValueSet values2 = lengthPatternTests.ComputeIntValueSet();
                if (values1.Intersect(values2) is INumericValueSet<int> { IsContiguous: true } lengthValueSet)
                    return lengthValueSet;

                _diagnostics.Add(ErrorCode.ERR_InvalidLengthPattern, (pattern.LengthPattern?.Syntax ?? syntax).Location);
                return null;
            }

            (NamedTypeSymbol bufferType, MethodSymbol bufferCtor, MethodSymbol pushMethod, MethodSymbol popMethod) getWellKnownMembers()
            {
                // TODO(alrz) Handle missing types and members
                var bufferType = _compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_Deque_T).Construct(pattern.ElementType);
                var bufferCtor = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_Deque_T__ctor))!.AsMember(bufferType);
                var pushMethod = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_Deque_T__Enqueue))!.AsMember(bufferType);
                var popMethod = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_Deque_T__Pop))!.AsMember(bufferType);
                return (bufferType, bufferCtor, pushMethod, popMethod);
            }
        }

        private Tests MakeLengthTests(SyntaxNode syntax, BoundListPatternInfo pattern, ArrayBuilder<BoundPatternBinding> bindings, BoundDagTemp countTemp)
        {
            var tests = ArrayBuilder<Tests>.GetInstance(2);
            MakeLengthTests(syntax, pattern, bindings, countTemp, tests);
            return Tests.AndSequence.Create(tests);
        }

        private void MakeLengthTests(SyntaxNode syntax, BoundListPatternInfo pattern, ArrayBuilder<BoundPatternBinding> bindings, BoundDagTemp countTemp, ArrayBuilder<Tests> tests)
        {
            if (pattern.LengthPattern != null)
                tests.Add(MakeTestsAndBindings(countTemp, pattern.LengthPattern, bindings));
            tests.Add(MakeInferredLengthTests(syntax, pattern, countTemp));
        }

        private void MakeTestsAndBindingsForListPattern0(BoundDagTemp input, BoundListPatternWithArray pattern, ArrayBuilder<BoundPatternBinding> bindings)
        {
            throw new NotImplementedException();
#if false
            var syntax = pattern.Syntax;
            var subpatterns = pattern.Subpatterns;
            var subpatternCount = subpatterns.Length;

            Debug.Assert(isMultidimensional(subpatterns));
            var sizes = calculateSizes();
            var tests = ArrayBuilder<Tests>.GetInstance(1 + sizes.Length * 2 + subpatternCount * 2);
            MakeCheckNotNull(input, syntax, isExplicitTest: false, tests);
            var lengthTempBuilder = ArrayBuilder<BoundDagTemp>.GetInstance(sizes.Length);
            for (int i = 0; i < sizes.Length; ++i)
            {
                var lengthEvaluation = new BoundDagArrayLengthEvaluation(syntax, dimension: i, input);
                tests.Add(new Tests.One(lengthEvaluation));
                var lengthTemp = new BoundDagTemp(syntax, _compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
                lengthTempBuilder.Add(lengthTemp);
                tests.Add(new Tests.One(new BoundDagValueTest(syntax, ConstantValue.Create(sizes[i]), lengthTemp)));
            }

            var lengthTemps = lengthTempBuilder.ToImmutableAndFree();
            var indices = ArrayBuilder<(int, ImmutableArray<BoundListPatternInfo>)>.GetInstance();
            for (int i = 0; i < subpatternCount; i++)
            {
                indices.Push((i, ((BoundListPatternWithArray)subpatterns[i]).Subpatterns));
                makeTestsAndBindingsForListPatternWithArrayRecursive(indices);
            }

            Debug.Assert(!indices.Any());
            indices.Free();

            return Tests.AndSequence.Create(tests);

            ImmutableArray<int> calculateSizes()
            {
                var builder = ArrayBuilder<int>.GetInstance(((ArrayTypeSymbol)input.Type).Rank);
                builder.Add(pattern.Subpatterns.Length);

                var first = pattern.Subpatterns[0];
                while (first is BoundListPatternWithArray nested)
                {
                    builder.Add(nested.Subpatterns.Length);
                    first = nested.Subpatterns[0];
                }

                return builder.ToImmutableAndFree();
            }

            void makeTestsAndBindingsForListPatternWithArrayRecursive(ArrayBuilder<(int Index, ImmutableArray<BoundPattern> Subpatterns)> indices)
            {
                var top = indices.Peek();
                var subpatterns = top.Subpatterns;

                if (isMultidimensional(subpatterns))
                {
                    for (int i = 0; i < subpatterns.Length; i++)
                    {
                        indices.Push((i, ((BoundListPatternWithArray)subpatterns[i]).Subpatterns));
                        makeTestsAndBindingsForListPatternWithArrayRecursive(indices);
                    }
                }
                else
                {
                    for (int i = 0; i < subpatterns.Length; i++)
                    {
                        var subpattern = subpatterns[i];

                        Debug.Assert(indices.Count == ((ArrayTypeSymbol)input.Type).Rank - 1);

                        var indexBuilder = ArrayBuilder<int>.GetInstance(indices.Count + 1);
                        foreach (var idx in indices)
                            indexBuilder.Add(idx.Index);
                        indexBuilder.Add(i);

                        var arrayEvaluation = new BoundDagArrayIndexEvaluation(syntax, lengthTemps, indexBuilder.ToImmutableAndFree(), input);
                        tests.Add(new Tests.One(arrayEvaluation));
                        var arrayTemp = new BoundDagTemp(syntax, pattern.ElementType, arrayEvaluation);
                        tests.Add(MakeTestsAndBindings(arrayTemp, subpattern, bindings));
                    }
                }

                indices.Pop();
            }

            static bool isMultidimensional(ImmutableArray<BoundPattern> subpatterns)
            {
                Debug.Assert(subpatterns.All((p) => p.Kind != BoundKind.ListPatternWithArray) ||
                             subpatterns.All((p) => p.Kind == BoundKind.ListPatternWithArray),
                    "all or none should be nested");

                return subpatterns.Length != 0 && subpatterns[0].Kind == BoundKind.ListPatternWithArray;
            }
#endif
        }
    }
}
