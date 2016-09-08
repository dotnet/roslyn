// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class TupleTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void IsTupleCompatible()
        {
            var source =
@"namespace System
{
    struct ValueTuple { }
    struct ValueTuple<T1> { }
    struct ValueTuple<T1, T2> { }
    struct ValueTuple<T1, T2, T3> { }
    struct ValueTuple<T1, T2, T3, T4> { }
    struct ValueTuple<T1, T2, T3, T4, T5> { }
    struct ValueTuple<T1, T2, T3, T4, T5, T6> { }
    struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> { }
    struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> { }
    struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> { }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("System.ValueTuple");
                int cardinality;
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);

                type = runtime.GetType("System.ValueTuple`1", typeof(int));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(1, cardinality);

                type = runtime.GetType("System.ValueTuple`2", typeof(int), typeof(string));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(2, cardinality);

                type = runtime.GetType("System.ValueTuple`3", typeof(int), typeof(string), typeof(int));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(3, cardinality);

                type = runtime.GetType("System.ValueTuple`4", typeof(int), typeof(string), typeof(int), typeof(string));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(4, cardinality);

                type = runtime.GetType("System.ValueTuple`5", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(5, cardinality);

                type = runtime.GetType("System.ValueTuple`6", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(6, cardinality);

                type = runtime.GetType("System.ValueTuple`7", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string), typeof(int));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(7, cardinality);

                type = runtime.GetType("System.ValueTuple`8", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string));
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);

                type = runtime.GetType("System.ValueTuple`1", typeof(string));
                type = runtime.GetType("System.ValueTuple`8", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), ((TypeImpl)type.GetLmrType()).Type);
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(8, cardinality);

                type = runtime.GetType("System.ValueTuple`9", typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string), typeof(int), typeof(string), typeof(int));
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);
            }
        }

        [Fact]
        public void IsTupleCompatible_NonStruct()
        {
            var source =
@"namespace System
{
    class ValueTuple<T1, T2> { }
    delegate void ValueTuple<T1, T2, T3>();
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("System.ValueTuple`2", typeof(object), typeof(object));
                int cardinality;
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(2, cardinality);

                type = runtime.GetType("System.ValueTuple`3", typeof(object), typeof(object), typeof(object));
                Assert.True(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(3, cardinality);
            }
        }

        [Fact]
        public void IsTupleCompatible_Other()
        {
            var source =
@"namespace System
{
    struct ValueTuple<T1, T2>
    {
        public struct S { }
    }
    struct ValueTuple2<T1, T2> { }
}
namespace Other
{
    struct ValueTuple<T1, T2> { }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("System.ValueTuple`2+S", typeof(object), typeof(object));
                int cardinality;
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);

                type = runtime.GetType("System.ValueTuple2`2", typeof(object), typeof(object));
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);

                type = runtime.GetType("Other.ValueTuple`2", typeof(object), typeof(object));
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);
            }
        }

        [Fact]
        public void IsTupleCompatible_InvalidName()
        {
            var source =
@".class sealed System.ValueTuple`3<T1, T2> extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(source, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assemblyBytes)));
            using (runtime.Load())
            {
                var type = runtime.GetType("System.ValueTuple`3", typeof(object), typeof(int));
                int cardinality;
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{System.ValueTuple<object, int>}", "System.ValueTuple<object, int>", "o"));
            }
        }

        [Fact]
        public void Tuples()
        {
            var source =
@"using System;
class C
{
    object _1 = new ValueTuple<int>(1);
    object _2 = new ValueTuple<int, int>(1, 2);
    object _3 = new ValueTuple<int, int, int>(1, 2, 3);
    object _4 = new ValueTuple<int, int, int, int>(1, 2, 3, 4);
    object _5 = new ValueTuple<int, int, int, int, int>(1, 2, 3, 4, 5);
    object _6 = new ValueTuple<int, int, int, int, int, int>(1, 2, 3, 4, 5, 6);
    object _7 = new ValueTuple<int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7);
    object _8 = new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>(1, 2, 3, 4, 5, 6, 7, new ValueTuple<int>(8));
    object _8A = new ValueTuple<int, int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7, 8);
    object _9 = new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>(1, 2, 3, 4, 5, 6, 7, new ValueTuple<int, int>(8, 9));
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib(source, references: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "{System.ValueTuple<int>}", "object {System.ValueTuple<int>}", "o._1", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_2", "(1, 2)", "object {(int, int)}", "o._2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_3", "(1, 2, 3)", "object {(int, int, int)}", "o._3", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_4", "(1, 2, 3, 4)", "object {(int, int, int, int)}", "o._4", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_5", "(1, 2, 3, 4, 5)", "object {(int, int, int, int, int)}", "o._5", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_6", "(1, 2, 3, 4, 5, 6)", "object {(int, int, int, int, int, int)}", "o._6", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_7", "(1, 2, 3, 4, 5, 6, 7)", "object {(int, int, int, int, int, int, int)}", "o._7", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_8", "(1, 2, 3, 4, 5, 6, 7, 8)", "object {(int, int, int, int, int, int, int, int)}", "o._8", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_8A", "{System.ValueTuple<int, int, int, int, int, int, int, int>}", "object {System.ValueTuple<int, int, int, int, int, int, int, int>}", "o._8A", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_9", "(1, 2, 3, 4, 5, 6, 7, 8, 9)", "object {(int, int, int, int, int, int, int, int, int)}", "o._9", DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void LongTuple()
        {
            var source =
@"class C
{
    (short, short, short, short, short, short, short, short, short, short, short, short, short, short, short, short, short) _17 =
        (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17);
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib(source, references: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var inspectionContext = CreateDkmInspectionContext(radix: 16);
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult, inspectionContext);
                Verify(children,
                    EvalResult(
                        "_17",
                        "(0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x000e, 0x000f, 0x0010, 0x0011)",
                        "(short, short, short, short, short, short, short, short, short, short, short, short, short, short, short, short, short)",
                        "o._17",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0], inspectionContext);
                Assert.Equal(8, children.Length); // Should be 18. https://github.com/dotnet/roslyn/issues/13421
                var child = children[children.Length - 1];
                Verify(child,
                    EvalResult(
                        "Rest",
                        "(0x0008, 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x000e, 0x000f, 0x0010, 0x0011)",
                        "(short, short, short, short, short, short, short, short, short, short)",
                        "o._17.Rest",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(child, inspectionContext);
                Assert.Equal(8, children.Length); // Should be 11. https://github.com/dotnet/roslyn/issues/13421
                child = children[children.Length - 1];
                Verify(child,
                    EvalResult(
                        "Rest",
                        "(0x000f, 0x0010, 0x0011)",
                        "(short, short, short)",
                        "o._17.Rest.Rest",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void NullNullableAndArray()
        {
            var source =
@"using System;
namespace System
{
    class ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
    }
    struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
    }
}
class C
{
    ValueTuple<object, int> _1 = null;
    ValueTuple<object, int, object>? _2 = new ValueTuple<object, int, object>();
    ValueTuple<object, int>[] _3 = new ValueTuple<object, int>[1];
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "null", "(object, int)", "o._1"),
                    EvalResult("_2", "(null, 0, null)", "(object, int, object)?", "o._2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_3", "{(object, int)[1]}", "(object, int)[]", "o._3", DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void Dynamic()
        {
            var source =
@"class C
{
    (dynamic, (object, dynamic)) F = (1, (2, 3));
    (object, object, object, object, object, object, dynamic[], dynamic[]) G = (1, 2, 3, 4, 5, 6, new object[] { 7 }, new object[] { 8 });
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "F",
                        "(1, (2, 3))",
                        "(dynamic, (object, dynamic)) {(object, (object, object))}",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable),
                    EvalResult(
                        "G",
                        "(1, 2, 3, 4, 5, 6, {object[1]}, {object[1]})",
                        "(object, object, object, object, object, object, dynamic[], dynamic[]) {(object, object, object, object, object, object, object[], object[])}",
                        "o.G",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [WorkItem(13625, "https://github.com/dotnet/roslyn/issues/13625")]
        [Fact]
        public void Names_LongTuple()
        {
            var source =
@"class C
{
    ((int A, (int B, int C) D, int E, int F, int G, int H, int I, int J) K, (int L, int M, int N) O) F =
        ((1, (2, 3), 4, 5, 6, 7, 8, 9), (10, 11, 12));
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "F",
                        "((1, (2, 3), 4, 5, 6, 7, 8, 9), (10, 11, 12))",
                        "((int A, (int B, int C) D, int E, int F, int G, int H, int I, int J) K, (int L, int M, int N) O)",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void PartialNames()
        {
            var source =
@"class C
{
    ((int A, (int B, int C) D, int, int F, int G, int, int I, int J, int K, int L) M, (int N, int, int P) Q) F =
        ((1, (2, 3), 4, 5, 6, 7, 8, 9, 10, 11), (12, 13, 14));
    (int A, (int B, int)[] C, (object, object), (int, int D, int E, int F, int G, int H, int I, int J, int) K)[] G =
        new[] { (1, new[] { (2, 3) }, ((object, object))(4, 5), (6, 7, 8, 9, 10, 11, 12, 13, 14)) };
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "F",
                        "((1, (2, 3), 4, 5, 6, 7, 8, 9, 10, 11), (12, 13, 14))",
                        "((int A, (int B, int C) D, int, int F, int G, int, int I, int J, int K, int L) M, (int N, int, int P) Q)",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable),
                    EvalResult(
                        "G",
                        "{(int, (int, int)[], (object, object), (int, int, int, int, int, int, int, int, int))[1]}",
                        "(int A, (int B, int)[] C, (object, object), (int, int D, int E, int F, int G, int H, int I, int J, int) K)[]",
                        "o.G",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "(1, {(int, int)[1]}, (4, 5), (6, 7, 8, 9, 10, 11, 12, 13, 14))",
                        "(int A, (int B, int)[] C, (object, object), (int, int D, int E, int F, int G, int H, int I, int J, int) K)",
                        "o.G[0]",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void NamesAndValueTuple1()
        {
            var source =
@"using System;
class C<T>
{
    internal C(T t) { }
}
class C
{
    (ValueTuple<int> A, int B) F = (new ValueTuple<int>(1), 2);
    (int A, ValueTuple<int> B) G = (3, new ValueTuple<int>(4));
    ValueTuple<(int A, int B)> H = new ValueTuple<(int, int)>((5, 6));
    (int A, ValueTuple<(int B, int C)> D) I = (7, new ValueTuple<(int, int)>((8, 9)));
    C<(int A, int B)> J = new C<(int, int)>((10, 11));
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "({System.ValueTuple<int>}, 2)", "(System.ValueTuple<int> A, int B)", "o.F", DkmEvaluationResultFlags.Expandable),
                    EvalResult("G", "(3, {System.ValueTuple<int>})", "(int A, System.ValueTuple<int> B)", "o.G", DkmEvaluationResultFlags.Expandable),
                    EvalResult("H", "{System.ValueTuple<(int, int)>}", "System.ValueTuple<(int A, int B)>", "o.H", DkmEvaluationResultFlags.Expandable),
                    EvalResult("I", "(7, {System.ValueTuple<(int, int)>})", "(int A, System.ValueTuple<(int B, int C)> D)", "o.I", DkmEvaluationResultFlags.Expandable),
                    EvalResult("J", "{C<(int, int)>}", "C<(int A, int B)>", "o.J"));
            }
        }

        [Fact]
        public void NamesAndDynamic()
        {
            var source =
@"class C
{
    (dynamic A, (int B, dynamic C)[] D, dynamic E, (int F, dynamic G, int H, int I, int J, int K, int L, int M, int N) O) F =
        (1, new (int, dynamic)[] { (2, 3) }, (4, 5), (6, 7, 8, 9, 10, 11, 12, 13, 14));
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "F",
                        "(1, {(int, object)[1]}, (4, 5), (6, 7, 8, 9, 10, 11, 12, 13, 14))",
                        "(dynamic A, (int B, dynamic C)[] D, dynamic E, (int F, dynamic G, int H, int I, int J, int K, int L, int M, int N) O) {(object, (int, object)[], object, (int, object, int, int, int, int, int, int, int))}",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void NamesAndDynamic_Other()
        {
            var source =
@"class C1 { }
class C2 { }
class C3 { }
class C4 { }
class C5 { }
class C6 { }
class C7 { }
class C8 { }
class C9 { }
class C10 { }
class C11 { }
class C12 { }
class C
{
    (((C1 C1, dynamic C2) B1, (C3 C3, dynamic C4)) A1, (dynamic B3, (C7 C7, C8 C8) B4) A2, ((C9 C9, C10 C10), dynamic B6) A3) F =
        ((
            ((new C1(), new C2()), (new C3(), new C4())),
            ((new C5(), new C6()), (new C7(), new C8())),
            ((new C9(), new C10()), (new C11(), new C12()))
        ));
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "F",
                        "((({C1}, {C2}), ({C3}, {C4})), (({C5}, {C6}), ({C7}, {C8})), (({C9}, {C10}), ({C11}, {C12})))",
                        "(((C1 C1, dynamic C2) B1, (C3 C3, dynamic C4)) A1, (dynamic B3, (C7 C7, C8 C8) B4) A2, ((C9 C9, C10 C10), dynamic B6) A3) {(((C1, object), (C3, object)), (object, (C7, C8)), ((C9, C10), object))}",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void NamesFromTypeArguments()
        {
            var source =
@"class A<T, U>
{
    T F;
    U[] G = new U[0];
}
class B<T>
{
    internal struct S { }
    (dynamic X, T Y) F = (null, default(T));
}
class C
{
    A<(dynamic A, object B)[], (object C, dynamic[] D)> F = new A<(dynamic A, object B)[], (object, dynamic[])>();
    B<(object E, B<(object F, dynamic G)>.S H)> G = new B<(object E, B<(object F, dynamic G)>.S H)>();
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "{A<(object, object)[], (object, object[])>}", "A<(dynamic A, object B)[], (object C, dynamic[] D)> {A<(object, object)[], (object, object[])>}", "o.F", DkmEvaluationResultFlags.Expandable),
                    EvalResult("G", "{B<(object, B<(object, object)>.S)>}", "B<(object E, B<(object F, dynamic G)>.S H)> {B<(object, B<(object, object)>.S)>}", "o.G", DkmEvaluationResultFlags.Expandable));
                var moreChildren = GetChildren(children[0]);
                Verify(moreChildren,
                    EvalResult("F", "null", "(dynamic A, object B)[] {(object, object)[]}", "o.F.F"),
                    EvalResult("G", "{(object, object[])[0]}", "(object C, dynamic[] D)[] {(object, object[])[]}", "o.F.G"));
                moreChildren = GetChildren(children[1]);
                Verify(moreChildren,
                    EvalResult("F", "(null, (null, {B<(object, object)>.S}))", "(dynamic X, (object E, B<(object F, dynamic G)>.S H) Y) {(object, (object, B<(object, object)>.S))}", "o.G.F", DkmEvaluationResultFlags.Expandable));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("Item1", "null", "dynamic {object}", "o.G.F.Item1"),
                    EvalResult("Item2", "(null, {B<(object, object)>.S})", "(object E, B<(object F, dynamic G)>.S H) {(object, B<(object, object)>.S)}", "o.G.F.Item2", DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void NamesFromTypeArguments_LongTuples()
        {
            var source =
@"class A1 { }
class A2 { }
class A3 { }
class A4 { }
class A5 { }
class A6 { }
class A7 { }
class A8 { }
class B1 { }
class B2 { }
class B3 { }
class B4 { }
class B5 { }
class B6 { }
class B7 { }
class B8 { }
class B9 { }
class B10 { }
class C1 { }
class C2 { }
class A<T, U>
{
    (dynamic A1, A2 A2, T A3, A4 A4, A5 A5, U A6, A7 A7, A8 A8, (T A9, U A10) A11) F =
        (new A1(), new A2(), default(T), new A4(), new A5(), default(U), new A7(), new A8(), (default(T), default(U)));
}
class B
{
    A<((dynamic B1, B2 B2) B3, B4 B4, dynamic B5, B6 B6, B7 B7, dynamic B8, B9 B9, B10 B10, dynamic B11), (C1 C1, (C2 C2, dynamic C3) C4)> G =
        new A<((dynamic B1, B2 B2) B3, B4 B4, dynamic B5, B6 B6, B7 B7, dynamic B8, B9 B9, B10 B10, dynamic B11), (C1 C1, (C2 C2, dynamic C3) C4)>();
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, additionalRefs: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("B");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "G",
                        "{A<((object, B2), B4, object, B6, B7, object, B9, B10, object), (C1, (C2, object))>}",
                        "A<((dynamic B1, B2 B2) B3, B4 B4, dynamic B5, B6 B6, B7 B7, dynamic B8, B9 B9, B10 B10, dynamic B11), (C1 C1, (C2 C2, dynamic C3) C4)> {A<((object, B2), B4, object, B6, B7, object, B9, B10, object), (C1, (C2, object))>}",
                        "o.G",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "F",
                        "({A1}, {A2}, ((null, null), null, null, null, null, null, null, null, null), {A4}, {A5}, (null, (null, null)), {A7}, {A8}, (((null, null), null, null, null, null, null, null, null, null), (null, (null, null))))",
                        "(dynamic A1, A2 A2, ((dynamic B1, B2 B2) B3, B4 B4, dynamic B5, B6 B6, B7 B7, dynamic B8, B9 B9, B10 B10, dynamic B11) A3, A4 A4, A5 A5, (C1 C1, (C2 C2, dynamic C3) C4) A6, A7 A7, A8 A8, (((dynamic B1, B2 B2) B3, B4 B4, dynamic B5, B6 B6, B7 B7, dynamic B8, B9 B9, B10 B10, dynamic B11) A9, (C1 C1, (C2 C2, dynamic C3) C4) A10) A11) {(object, A2, ((object, B2), B4, object, B6, B7, object, B9, B10, object), A4, A5, (C1, (C2, object)), A7, A8, (((object, B2), B4, object, B6, B7, object, B9, B10, object), (C1, (C2, object))))}",
                        "o.G.F",
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void Keywords()
        {
            var source =
@"namespace @namespace
{
    struct @struct
    {
    }
}
class async
{
    static (async @var, @namespace.@struct @class) F() => (null, default(@namespace.@struct));
    object _f = F();
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib(source, references: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("async");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{async}", "async", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_f", "(null, {namespace.struct})", "object {(async, namespace.struct)}", "o._f", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult("Item1", "null", "async", "(((@async, @namespace.@struct))o._f).Item1"),
                    EvalResult("Item2", "{namespace.struct}", "namespace.struct", "(((@async, @namespace.@struct))o._f).Item2"));
            }
        }

        /// <summary>
        /// DebuggerDisplayAttribute on field types is
        /// ignored for Value and Type.
        /// </summary>
        [Fact]
        public void DebuggerDisplayAttribute()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""F={F}"")]
class A
{
    internal object F;
}
class B
{
    (A, A) F = (new A() { F = 1 }, new A() { F = 2 });
}";
            var assembly0 = GenerateTupleAssembly();
            var reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference();
            var compilation1 = CSharpTestBaseBase.CreateCompilationWithMscorlib(source, references: new[] { reference0 });
            var assembly1 = compilation1.EmitToArray();
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)));
            using (runtime.Load())
            {
                var type = runtime.GetType("B");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "({A}, {A})", "(A, A)", "o.F", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult("Item1", "F=1", "A", "o.F.Item1", DkmEvaluationResultFlags.Expandable),
                    EvalResult("Item2", "F=2", "A", "o.F.Item2", DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void ObjectId()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(GenerateTupleAssembly())));
            using (runtime.Load())
            {
                var type = runtime.GetType("System.ValueTuple`2", typeof(object), typeof(int));
                var value = type.Instantiate(new object[0], alias: "$3", evalFlags: DkmEvaluationResultFlags.HasObjectId);
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "(null, 0) {$3}", "(object, int)", "o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasObjectId));
            }
        }

        private static ImmutableArray<byte> GenerateTupleAssembly()
        {
            var source =
@"namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;
        public ValueTuple(T1 _1)
        {
            Item1 = _1;
        }
    }
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 _1, T2 _2)
        {
            Item1 = _1;
            Item2 = _2;
        }
    }
    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public ValueTuple(T1 _1, T2 _2, T3 _3)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
        }
    }
    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public ValueTuple(T1 _1, T2 _2, T3 _3, T4 _4)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
            Item4 = _4;
        }
    }
    public struct ValueTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public ValueTuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
            Item4 = _4;
            Item5 = _5;
        }
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public ValueTuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
            Item4 = _4;
            Item5 = _5;
            Item6 = _6;
        }
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public ValueTuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
            Item4 = _4;
            Item5 = _5;
            Item6 = _6;
            Item7 = _7;
        }
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public T8 Rest;
        public ValueTuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8)
        {
            Item1 = _1;
            Item2 = _2;
            Item3 = _3;
            Item4 = _4;
            Item5 = _5;
            Item6 = _6;
            Item7 = _7;
            Rest = _8;
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] names)
        {
        }
    }
}";
            var comp = CSharpTestBaseBase.CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();
            return comp.EmitToArray();
        }
    }
}
