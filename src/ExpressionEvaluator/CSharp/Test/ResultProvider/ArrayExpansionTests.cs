// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ArrayExpansionTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Array()
        {
            var rootExpr = "new[] { 1, 2, 3 }";
            var value = CreateDkmClrValue(new[] { 1, 2, 3 });
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[3]}", "int[]", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "1", "int", "(new[] { 1, 2, 3 })[0]"),
                EvalResult("[1]", "2", "int", "(new[] { 1, 2, 3 })[1]"),
                EvalResult("[2]", "3", "int", "(new[] { 1, 2, 3 })[2]"));
        }

        [Fact]
        public void ZeroLengthArray()
        {
            var rootExpr = "new object[0]";
            var value = CreateDkmClrValue(new object[0]);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{object[0]}", "object[]", rootExpr));

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 100, null, out enumContext);
            Verify(children);

            var items = GetItems(enumContext, 0, enumContext.Count);
            Verify(items);
        }

        [Fact]
        public void NestedArray()
        {
            var rootExpr = "new int[][] { new[] { 1, 2 }, new[] { 3 } }";
            var value = CreateDkmClrValue(new int[][] { [1, 2], [3] });
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[2][]}", "int[][]", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "{int[2]}", "int[]", "(new int[][] { new[] { 1, 2 }, new[] { 3 } })[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("[1]", "{int[1]}", "int[]", "(new int[][] { new[] { 1, 2 }, new[] { 3 } })[1]", DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(children[0]),
                EvalResult("[0]", "1", "int", "(new int[][] { new[] { 1, 2 }, new[] { 3 } })[0][0]"),
                EvalResult("[1]", "2", "int", "(new int[][] { new[] { 1, 2 }, new[] { 3 } })[0][1]"));
            Verify(GetChildren(children[1]),
                EvalResult("[0]", "3", "int", "(new int[][] { new[] { 1, 2 }, new[] { 3 } })[1][0]"));
        }

        [Fact]
        public void MultiDimensionalArray()
        {
            var rootExpr = "new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } }";
            var value = CreateDkmClrValue(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } });
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[3, 2]}", "int[,]", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0, 0]", "1", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[0, 0]"),
                EvalResult("[0, 1]", "2", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[0, 1]"),
                EvalResult("[1, 0]", "3", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[1, 0]"),
                EvalResult("[1, 1]", "4", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[1, 1]"),
                EvalResult("[2, 0]", "5", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[2, 0]"),
                EvalResult("[2, 1]", "6", "int", "(new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })[2, 1]"));
        }

        [Fact]
        public void ZeroLengthMultiDimensionalArray()
        {
            var rootExpr = "new int[2, 3, 0]";
            var value = CreateDkmClrValue(new int[2, 3, 0]);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[2, 3, 0]}", "int[,,]", rootExpr));
            Verify(GetChildren(evalResult));

            rootExpr = "new int[2, 0, 3]";
            value = CreateDkmClrValue(new int[2, 0, 3]);
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[2, 0, 3]}", "int[,,]", rootExpr));
            Verify(GetChildren(evalResult));

            rootExpr = "new int[0, 2, 3]";
            value = CreateDkmClrValue(new int[0, 2, 3]);
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[0, 2, 3]}", "int[,,]", rootExpr));
            Verify(GetChildren(evalResult));

            rootExpr = "new int[0, 0, 0]";
            value = CreateDkmClrValue(new int[0, 0, 0]);
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[0, 0, 0]}", "int[,,]", rootExpr));
            Verify(GetChildren(evalResult));
        }

        [Fact]
        public void NullArray()
        {
            var rootExpr = "new int[][,,] { null, new int[2, 3, 4] }";
            var evalResult = FormatResult(rootExpr, CreateDkmClrValue(new int[][,,] { null, new int[2, 3, 4] }));
            Verify(evalResult,
                EvalResult(rootExpr, "{int[2][,,]}", "int[][,,]", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "null", "int[,,]", "(new int[][,,] { null, new int[2, 3, 4] })[0]"),
                EvalResult("[1]", "{int[2, 3, 4]}", "int[,,]", "(new int[][,,] { null, new int[2, 3, 4] })[1]", DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void BaseType()
        {
            var source =
@"class C
{
    object o = new int[] { 1, 2 };
    System.Array a = new object[] { null };
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{object[1]}", "System.Array {object[]}", "(new C()).a", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("o", "{int[2]}", "object {int[]}", "(new C()).o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));
            Verify(GetChildren(children[0]),
                EvalResult("[0]", "null", "object", "((object[])(new C()).a)[0]"));
            Verify(GetChildren(children[1]),
                EvalResult("[0]", "1", "int", "((int[])(new C()).o)[0]"),
                EvalResult("[1]", "2", "int", "((int[])(new C()).o)[1]"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933845")]
        public void BaseElementType()
        {
            var source =
@"class A
{
    internal object F;
}
class B : A
{
    internal B(object f)
    {
        F = f;
    }
    internal object P { get { return this.F; } }
}";
            var assembly = GetAssembly(source);
            var typeB = assembly.GetType("B");
            var value = CreateDkmClrValue(new object[] { 1, typeB.Instantiate(2) });
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{object[2]}", "object[]", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "1", "object {int}", "o[0]"),
                EvalResult("[1]", "{B}", "object {B}", "o[1]", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "2", "object {int}", "((A)o[1]).F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P", "2", "object {int}", "((B)o[1]).P", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022157")]
        public void Covariance()
        {
            var source =
@"interface I { }
class A
{
    object F = 1;
}
class B : A, I { }
class C
{
    object[] F = new[] { new A() };
    A[] G = new[] { new B() };
    I[] H = new[] { new B() };
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "{A[1]}", "object[] {A[]}", "o.F", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("G", "{B[1]}", "A[] {B[]}", "o.G", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("H", "{B[1]}", "I[] {B[]}", "o.H", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));
            var moreChildren = GetChildren(children[0]);
            Verify(moreChildren,
                EvalResult("[0]", "{A}", "object {A}", "((A[])o.F)[0]", DkmEvaluationResultFlags.Expandable));
            moreChildren = GetChildren(moreChildren[0]);
            Verify(moreChildren,
                EvalResult("F", "1", "object {int}", "((A)((A[])o.F)[0]).F", DkmEvaluationResultFlags.CanFavorite));
            moreChildren = GetChildren(children[1]);
            Verify(moreChildren,
                EvalResult("[0]", "{B}", "A {B}", "((B[])o.G)[0]", DkmEvaluationResultFlags.Expandable));
            moreChildren = GetChildren(children[2]);
            Verify(moreChildren,
                EvalResult("[0]", "{B}", "I {B}", "((B[])o.H)[0]", DkmEvaluationResultFlags.Expandable));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1001844")]
        public void Interface()
        {
            var source =
@"class C
{
    char[] F = new char[] { '1' };
    System.Collections.IEnumerable G = new char[] { '2' };
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "{char[1]}", "char[]", "o.F", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("G", "{char[1]}", "System.Collections.IEnumerable {char[]}", "o.G", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));
            var moreChildren = GetChildren(children[0]);
            Verify(moreChildren,
                EvalResult("[0]", "49 '1'", "char", "o.F[0]", editableValue: "'1'"));
            moreChildren = GetChildren(children[1]);
            Verify(moreChildren,
                EvalResult("[0]", "50 '2'", "char", "((char[])o.G)[0]", editableValue: "'2'"));
        }

        [Fact]
        public void NonZeroLowerBounds()
        {
            var rootExpr = "arrayExpr";
            var array = (int[,])System.Array.CreateInstance(typeof(int), [2, 3], [3, 4]);
            array[3, 4] = 1;
            array[3, 5] = 2;
            array[3, 6] = 3;
            array[4, 4] = 4;
            array[4, 5] = 5;
            array[4, 6] = 6;
            var value = CreateDkmClrValue(array);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{int[3..4, 4..6]}", "int[,]", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[3, 4]", "1", "int", "arrayExpr[3, 4]"),
                EvalResult("[3, 5]", "2", "int", "arrayExpr[3, 5]"),
                EvalResult("[3, 6]", "3", "int", "arrayExpr[3, 6]"),
                EvalResult("[4, 4]", "4", "int", "arrayExpr[4, 4]"),
                EvalResult("[4, 5]", "5", "int", "arrayExpr[4, 5]"),
                EvalResult("[4, 6]", "6", "int", "arrayExpr[4, 6]"));
        }

        [Fact]
        public void Hexadecimal()
        {
            var value = CreateDkmClrValue(new[] { 10, 20, 30 });
            var inspectionContext = CreateDkmInspectionContext(radix: 16);
            var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("o", "{int[0x00000003]}", "int[]", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult, inspectionContext);
            // Hex could be used for indices: [0x00000000], etc.
            Verify(children,
                EvalResult("[0]", "0x0000000a", "int", "o[0]"),
                EvalResult("[1]", "0x00000014", "int", "o[1]"),
                EvalResult("[2]", "0x0000001e", "int", "o[2]"));
        }

        [Fact]
        public void HexadecimalNonZeroLowerBounds()
        {
            var array = (int[,])System.Array.CreateInstance(typeof(int), [2, 1], [-3, 4]);
            array[-3, 4] = 1;
            array[-2, 4] = 2;
            var value = CreateDkmClrValue(array);
            var inspectionContext = CreateDkmInspectionContext(radix: 16);
            var evalResult = FormatResult("a", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("a", "{int[0xfffffffd..0xfffffffe, 0x00000004..0x00000004]}", "int[,]", "a", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult, inspectionContext);
            // Hex could be used for indices: [0xfffffffd, 0x00000004], etc.
            Verify(children,
                EvalResult("[-3, 4]", "0x00000001", "int", "a[-3, 4]"),
                EvalResult("[-2, 4]", "0x00000002", "int", "a[-2, 4]"));
        }

        /// <summary>
        /// Expansion should be lazy so that the IDE can
        /// reduce overhead by expanding a subset of rows.
        /// </summary>
        [Fact]
        public void LazyExpansion()
        {
            var rootExpr = "new byte[10, 1000, 1000]";
            var parenthesizedExpr = string.Format("({0})", rootExpr);
            var value = CreateDkmClrValue(new byte[10, 1000, 1000]); // Array with 10M elements
            var evalResults = new DkmEvaluationResult[100]; // 100 distinct evaluations of the array
            for (int i = 0; i < evalResults.Length; i++)
            {
                var evalResult = FormatResult(rootExpr, value);
                evalResults[i] = evalResult;
                // Expand a subset.
                int offset = i * 100 * 1000;

                DkmEvaluationResultEnumContext enumContext;
                GetChildren(evalResult, 0, null, out enumContext);

                var items = GetItems(enumContext, offset, 2);
                var indices1 = string.Format("{0}, {1}, {2}", offset / 1000000, (offset % 1000000) / 1000, 0);
                var indices2 = string.Format("{0}, {1}, {2}", (offset + 1) / 1000000, ((offset + 1) % 1000000) / 1000, 1);
                Verify(items,
                    EvalResult(string.Format("[{0}]", indices1), "0", "byte", string.Format("{0}[{1}]", parenthesizedExpr, indices1)),
                    EvalResult(string.Format("[{0}]", indices2), "0", "byte", string.Format("{0}[{1}]", parenthesizedExpr, indices2)));
            }
        }

        /// <summary>
        /// Validate that our helper is able to identify the compiler-generated fixed buffer types.
        /// </summary>
        [Fact]
        public void IdentifyFixedBuffer()
        {
            // The mock DkmClrValue.GetArrayElement relies on casting RawValue `object` to `Array` but fixed buffers are not `Array`.
            // We can get the first element of the generated type via the single defined field,
            // but everything gets boxed coming out of reflection so we can't do unsafe reads to get at the rest of the elements.
            // We can't cast `object` to our known SampleFixedBuffer type because that is the enclosing type that defines the field;
            // the actual type that shows up in the `GetArrayElement` mock is the generated field type, something like SampleFixedBuffer+<Buffer>e__Buffer.
            // All we can do with these testing limitations is to validate that our helper returns accurate information when it encounters a fixed buffer.
            var instance = SampleFixedBuffer.Create();
            var fixedBuffer = CreateDkmClrValue(instance)
                .GetMemberValue(nameof(SampleFixedBuffer.Buffer), (int)MemberTypes.Field, null, DefaultInspectionContext);

            // Validate the actual ResultProvider gives back an ArrayExpansion for our fixed buffer field
            var dataItem = FormatResult("instance.Buffer", fixedBuffer).GetDataItem<EvalResultDataItem>();
            Assert.IsAssignableFrom<ArrayExpansion>(dataItem.Expansion);

            // Directly validate the values are computed correctly
            Assert.True(InlineArrayHelpers.TryGetFixedBufferInfo(fixedBuffer.Type.GetLmrType(), out var length, out var elementType));
            Assert.Equal(4, length);
            Assert.Equal(typeof(byte).FullName, elementType.FullName);

            // Validate fixed buffer identification / expansion does not kick in for a nearly identical shape
            var source =
@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe struct Enclosing
{
    [CompilerGenerated]
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public struct AlmostFixedBuffer
    {
        public byte FixedElementField;
    }
    
    // Missing the generated [FixedBuffer(Type, int)] attribute
    public AlmostFixedBuffer Buffer;
}";

            var assembly = GetUnsafeAssembly(source);
            var type = assembly.GetType("Enclosing");
            var fakeValue = CreateDkmClrValue(Activator.CreateInstance(type));
            var fakeBuffer = fakeValue.GetMemberValue("Buffer", (int)MemberTypes.Field, null, DefaultInspectionContext);
            var fakeDataItem = FormatResult("fake.Buffer", fakeBuffer).GetDataItem<EvalResultDataItem>();
            Assert.IsNotAssignableFrom<ArrayExpansion>(fakeDataItem.Expansion);
            Assert.False(InlineArrayHelpers.TryGetFixedBufferInfo(fakeBuffer.Type.GetLmrType(), out _, out _));
        }
    }
}
