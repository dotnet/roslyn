// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
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
                Assert.False(type.GetLmrType().IsTupleCompatible(out cardinality));
                Assert.Equal(0, cardinality);

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
@".class System.ValueTuple`3<T1, T2>
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
    (async @var, @namespace.@struct @class) F;
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
                    EvalResult("F", "(null, {namespace.struct})", "(async, namespace.struct)", "o.F", DkmEvaluationResultFlags.Expandable));
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
