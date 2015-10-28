// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BindingFlags = System.Reflection.BindingFlags;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ResultsViewTests : CSharpResultProviderTestBase
    {
        // IEnumerable pattern not supported.
        [Fact]
        public void IEnumerablePattern()
        {
            var source =
@"using System.Collections;
class C
{
    private readonly IEnumerable e;
    internal C(IEnumerable e)
    {
        this.e = e;
    }
    public IEnumerator GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("e", "{int[2]}", "System.Collections.IEnumerable {int[]}", "o.e", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        // IEnumerable<T> pattern not supported.
        [Fact]
        public void IEnumerableOfTPattern()
        {
            var source =
@"using System.Collections.Generic;
class C<T>
{
    private readonly IEnumerable<T> e;
    internal C(IEnumerable<T> e)
    {
        this.e = e;
    }
    public IEnumerator<T> GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C`1").MakeGenericType(typeof(int));
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C<int>}", "C<int>", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("e", "{int[2]}", "System.Collections.Generic.IEnumerable<int> {int[]}", "o.e", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        [Fact]
        public void IEnumerableImplicitImplementation()
        {
            var source =
@"using System.Collections;
class C : IEnumerable
{
    private readonly IEnumerable e;
    internal C(IEnumerable e)
    {
        this.e = e;
    }
    public IEnumerator GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "e",
                        "{int[2]}",
                        "System.Collections.IEnumerable {int[]}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult("[0]", "1", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o).Items[0]"),
                    EvalResult("[1]", "2", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o).Items[1]"));
            }
        }

        [Fact]
        public void IEnumerableOfTImplicitImplementation()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
struct S<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> e;
    internal S(IEnumerable<T> e)
    {
        this.e = e;
    }
    public IEnumerator<T> GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("S`1").MakeGenericType(typeof(int));
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{S<int>}", "S<int>", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "e",
                        "{int[2]}",
                        "System.Collections.Generic.IEnumerable<int> {int[]}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult("[0]", "1", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[0]"),
                    EvalResult("[1]", "2", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[1]"));
            }
        }

        [Fact]
        public void IEnumerableExplicitImplementation()
        {
            var source =
@"using System.Collections;
class C : IEnumerable
{
    private readonly IEnumerable e;
    internal C(IEnumerable e)
    {
        this.e = e;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "e",
                        "{int[2]}",
                        "System.Collections.IEnumerable {int[]}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult("[0]", "1", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o).Items[0]"),
                    EvalResult("[1]", "2", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o).Items[1]"));
            }
        }

        [Fact]
        public void IEnumerableOfTExplicitImplementation()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> e;
    internal C(IEnumerable<T> e)
    {
        this.e = e;
    }
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C`1").MakeGenericType(typeof(int));
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C<int>}", "C<int>", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "e",
                        "{int[2]}",
                        "System.Collections.Generic.IEnumerable<int> {int[]}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult("[0]", "1", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[0]"),
                    EvalResult("[1]", "2", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[1]"));
            }
        }

        // Results View not supported for
        // IEnumerator implementation.
        [Fact]
        public void IEnumerator()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
class C : IEnumerator<int>
{
    private int[] c = new[] { 1, 2, 3 };
    private int i = 0;
    object IEnumerator.Current
    {
        get { return this.c[this.i]; }
    }
    int IEnumerator<int>.Current
    {
        get { return this.c[this.i]; }
    }
    bool IEnumerator.MoveNext()
    {
        this.i++;
        return this.i < this.c.Length;
    }
    void IEnumerator.Reset()
    {
        this.i = 0;
    }
    void IDisposable.Dispose()
    {
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "System.Collections.Generic.IEnumerator<int>.Current",
                        "1",
                        "int",
                        "((System.Collections.Generic.IEnumerator<int>)o).Current",
                        DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "System.Collections.IEnumerator.Current",
                        "1",
                        "object {int}",
                        "((System.Collections.IEnumerator)o).Current",
                        DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("c", "{int[3]}", "int[]", "o.c", DkmEvaluationResultFlags.Expandable),
                    EvalResult("i", "0", "int", "o.i"));
            }
        }

        [Fact]
        public void Overrides()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
class A : IEnumerable<object>
{
    public virtual IEnumerator<object> GetEnumerator()
    {
        yield return 0;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class B1 : A
{
    public override IEnumerator<object> GetEnumerator()
    {
        yield return 1;
    }
}
class B2 : A, IEnumerable<int>
{
    public new IEnumerator<int> GetEnumerator()
    {
        yield return 2;
    }
}
class B3 : A
{
    public new IEnumerable<int> GetEnumerator()
    {
        yield return 3;
    }
}
class B4 : A
{
}
class C
{
    A _1 = new B1();
    A _2 = new B2();
    A _3 = new B3();
    A _4 = new B4();
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "{B1}", "A {B1}", "o._1", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_2", "{B2}", "A {B2}", "o._2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_3", "{B3}", "A {B3}", "o._3", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_4", "{B4}", "A {B4}", "o._4", DkmEvaluationResultFlags.Expandable));
                // A _1 = new B1();
                var moreChildren = GetChildren(children[0]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._1, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("[0]", "1", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView<object>(o._1).Items[0]"));
                // A _2 = new B2();
                moreChildren = GetChildren(children[1]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._2, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("[0]", "2", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o._2).Items[0]"));
                // A _3 = new B3();
                moreChildren = GetChildren(children[2]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._3, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("[0]", "0", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView<object>(o._3).Items[0]"));
                // A _4 = new B4();
                moreChildren = GetChildren(children[3]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._4, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("[0]", "0", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView<object>(o._4).Items[0]"));
            }
        }

        /// <summary>
        /// Include Results View on base types
        /// (matches legacy EE behavior).
        /// </summary>
        [Fact]
        public void BaseTypes()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class A1
{
}
class B1 : A1, IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return 0;
    }
}
class A2
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}
class B2 : A2, IEnumerable<object>
{
    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        yield return 2;
    }
}
struct S : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return 3;
    }
}
class C
{
    A1 _1 = new B1();
    B2 _2 = new B2();
    System.ValueType _3 = new S();
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "{B1}", "A1 {B1}", "o._1", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_2", "{B2}", "B2", "o._2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_3", "{S}", "System.ValueType {S}", "o._3", DkmEvaluationResultFlags.Expandable));
                // A1 _1 = new B1();
                var moreChildren = GetChildren(children[0]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._1, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                Verify(GetChildren(moreChildren[0]),
                    EvalResult("[0]", "0", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o._1).Items[0]"));
                // B2 _2 = new B2();
                moreChildren = GetChildren(children[1]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._2, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                Verify(GetChildren(moreChildren[0]),
                    EvalResult("[0]", "2", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView<object>(o._2).Items[0]"));
                // System.ValueType _3 = new S();
                moreChildren = GetChildren(children[2]);
                Verify(moreChildren,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o._3, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                Verify(GetChildren(moreChildren[0]),
                    EvalResult("[0]", "3", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(o._3).Items[0]"));
            }
        }

        [Fact]
        public void Nullable()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
struct S : IEnumerable<object>
{
    internal readonly object[] c;
    internal S(object[] c)
    {
        this.c = c;
    }
    public IEnumerator<object> GetEnumerator()
    {
        foreach (var o in this.c)
        {
            yield return o;
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class C
{
    S? F = new S(new object[] { null });
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "{S}", "S?", "o.F", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult("c", "{object[1]}", "object[]", "o.F.c", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o.F, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "null",
                        "object",
                        "new System.Linq.SystemCore_EnumerableDebugView<object>(o.F).Items[0]"));
            }
        }

        [Fact]
        public void ConstructedType()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
class A<T> : IEnumerable<T>
{
    private readonly T[] items;
    internal A(T[] items)
    {
        this.items = items;
    }
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class B
{
    internal object F;
}
class C : A<B>
{
    internal C() : base(new[] { new B() })
    {
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "items",
                        "{B[1]}",
                        "B[]",
                        "o.items",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                var moreChildren = GetChildren(children[1]);
                Verify(moreChildren,
                    // The legacy EE treats the Items elements as readonly, but since
                    // Items is a T[], we treat the elements as read/write. However, Items
                    // is not updated when modifying elements so this is harmless.
                    EvalResult(
                        "[0]",
                        "{B}",
                        "B",
                        "new System.Linq.SystemCore_EnumerableDebugView<B>(o).Items[0]",
                        DkmEvaluationResultFlags.Expandable));
                moreChildren = GetChildren(moreChildren[0]);
                Verify(moreChildren,
                    EvalResult("F", "null", "object", "(new System.Linq.SystemCore_EnumerableDebugView<B>(o).Items[0]).F"));
            }
        }

        /// <summary>
        /// System.Array should not have Results View.
        /// </summary>
        [Fact]
        public void Array()
        {
            var source =
@"using System;
using System.Collections;
class C
{
    char[] _1 = new char[] { '1' };
    Array _2 = new char[] { '2' };
    IEnumerable _3 = new char[] { '3' };
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "{char[1]}", "char[]", "o._1", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_2", "{char[1]}", "System.Array {char[]}", "o._2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("_3", "{char[1]}", "System.Collections.IEnumerable {char[]}", "o._3", DkmEvaluationResultFlags.Expandable));
                Verify(GetChildren(children[0]),
                    EvalResult("[0]", "49 '1'", "char", "o._1[0]", editableValue: "'1'"));
                Verify(GetChildren(children[1]),
                    EvalResult("[0]", "50 '2'", "char", "((char[])o._2)[0]", editableValue: "'2'"));
                children = GetChildren(children[2]);
                Verify(children,
                    EvalResult("[0]", "51 '3'", "char", "((char[])o._3)[0]", editableValue: "'3'"));
            }
        }

        /// <summary>
        /// String should not have Results View.
        /// </summary>
        [Fact]
        public void String()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C
{
    string _1 = ""1"";
    object _2 = ""2"";
    IEnumerable _3 = ""3"";
    IEnumerable<char> _4 = ""4"";
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("_1", "\"1\"", "string", "o._1", DkmEvaluationResultFlags.RawString, editableValue: "\"1\""),
                    EvalResult("_2", "\"2\"", "object {string}", "o._2", DkmEvaluationResultFlags.RawString, editableValue: "\"2\""),
                    EvalResult("_3", "\"3\"", "System.Collections.IEnumerable {string}", "o._3", DkmEvaluationResultFlags.RawString, editableValue: "\"3\""),
                    EvalResult("_4", "\"4\"", "System.Collections.Generic.IEnumerable<char> {string}", "o._4", DkmEvaluationResultFlags.RawString, editableValue: "\"4\""));
            }
        }

        [WorkItem(1006160)]
        [Fact]
        public void MultipleImplementations_DifferentImplementors()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
class A<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        yield return default(T);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class B1 : A<object>, IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        yield return 1;
    }
}
class B2 : A<int>, IEnumerable<object>
{
    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        yield return null;
    }
}
class B3 : A<object>, IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return 3;
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                // class B1 : A<object>, IEnumerable<int>
                var type = assembly.GetType("B1");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{B1}", "B1", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "1",
                        "int",
                        "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[0]"));
                // class B2 : A<int>, IEnumerable<object>
                type = assembly.GetType("B2");
                value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{B2}", "B2", "o", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "null",
                        "object",
                        "new System.Linq.SystemCore_EnumerableDebugView<object>(o).Items[0]"));
                // class B3 : A<object>, IEnumerable
                type = assembly.GetType("B3");
                value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{B3}", "B3", "o", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "null",
                        "object",
                        "new System.Linq.SystemCore_EnumerableDebugView<object>(o).Items[0]"));
            }
        }

        [Fact]
        public void MultipleImplementations_SameImplementor()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
class A : IEnumerable<int>, IEnumerable<string>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        yield return 1;
    }
    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        yield return null;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class B : IEnumerable<string>, IEnumerable<int>
{
    IEnumerator<string> IEnumerable<string>.GetEnumerator()
    {
        yield return null;
    }
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        yield return 1;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                // class A : IEnumerable<int>, IEnumerable<string>
                var type = assembly.GetType("A");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("a", value);
                Verify(evalResult,
                    EvalResult("a", "{A}", "A", "a", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "a, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "1",
                        "int",
                        "new System.Linq.SystemCore_EnumerableDebugView<int>(a).Items[0]"));
                // class B : IEnumerable<string>, IEnumerable<int>
                type = assembly.GetType("B");
                value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                evalResult = FormatResult("b", value);
                Verify(evalResult,
                    EvalResult("b", "{B}", "B", "b", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "b, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(
                        "[0]",
                        "null",
                        "string",
                        "new System.Linq.SystemCore_EnumerableDebugView<string>(b).Items[0]"));
            }
        }

        /// <summary>
        /// Types with [DebuggerTypeProxy] should not have Results View.
        /// </summary>
        [Fact]
        public void DebuggerTypeProxy()
        {
            var source =
@"using System.Collections;
using System.Diagnostics;
public class P : IEnumerable
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
    }
    public IEnumerator GetEnumerator()
    {
        return this.c.GetEnumerator();
    }
    public int Length
    {
        get { return this.c.o.Length; }
    }
}
[DebuggerTypeProxy(typeof(P))]
public class C : IEnumerable
{
    internal readonly object[] o;
    public C(object[] o)
    {
        this.o = o;
    }
    public IEnumerator GetEnumerator()
    {
        return this.o.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new object[] { new object[] { string.Empty } }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("Length", "1", "int", "new P(o).Length", DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Raw View", null, "", "o, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult("o", "{object[1]}", "object[]", "o.o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        /// <summary>
        /// Do not expose Results View if the proxy type is missing.
        /// </summary>
        [Fact]
        public void MissingProxyType()
        {
            var source =
@"using System.Collections;
class C : IEnumerable
{
    private readonly IEnumerable e;
    internal C(IEnumerable e)
    {
        this.e = e;
    }
    public IEnumerator GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = new[] { assembly };
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("e", "{int[2]}", "System.Collections.IEnumerable {int[]}", "o.e", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        /// <summary>
        /// Proxy type not in System.Core.dll.
        /// </summary>
        [Fact]
        public void MissingProxyType_SystemCore()
        {
            // "System.Core.dll"
            var source0 = "";
            var compilation0 = CSharpTestBase.CreateCompilationWithMscorlib(source0, assemblyName: "system.core");
            var assembly0 = ReflectionUtilities.Load(compilation0.EmitToArray());
            var source =
@"using System.Collections;
class C : IEnumerable
{
    private readonly IEnumerable e;
    internal C(IEnumerable e)
    {
        this.e = e;
    }
    public IEnumerator GetEnumerator()
    {
        return this.e.GetEnumerator();
    }
}";
            var assembly = GetAssembly(source);
            var assemblies = new[] { assembly0, assembly };
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(new[] { 1, 2 }),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("e", "{int[2]}", "System.Collections.IEnumerable {int[]}", "o.e", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
                // Verify the module was found but ResolveTypeName failed.
                var module = runtime.Modules.Single(m => m.Assembly == assembly0);
                Assert.Equal(module.ResolveTypeNameFailures, 1);
            }
        }

        /// <summary>
        /// Report "Enumeration yielded no results" when
        /// GetEnumerator returns an empty collection or null.
        /// </summary>
        [Fact]
        public void GetEnumeratorEmptyOrNull()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
// IEnumerable returns empty collection.
class C0 : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield break;
    }
}
// IEnumerable<T> returns empty collection.
class C1<T> : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator()
    {
        yield break;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
// IEnumerable returns null.
class C2 : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        return null;
    }
}
// IEnumerable<T> returns null.
class C3<T> : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator()
    {
        return null;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
class C
{
    C0 _0 = new C0();
    C1<object> _1 = new C1<object>();
    C2 _2 = new C2();
    C3<object> _3 = new C3<object>();
}";
            using (new EnsureEnglishUICulture())
            {
                var assembly = GetAssembly(source);
                var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
                using (ReflectionUtilities.LoadAssemblies(assemblies))
                {
                    var runtime = new DkmClrRuntimeInstance(assemblies);
                    var type = assembly.GetType("C");
                    var value = CreateDkmClrValue(
                        value: type.Instantiate(),
                        type: runtime.GetType((TypeImpl)type));
                    var evalResult = FormatResult("o", value);
                    Verify(evalResult,
                        EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                    var children = GetChildren(evalResult);
                    Verify(children,
                        EvalResult("_0", "{C0}", "C0", "o._0", DkmEvaluationResultFlags.Expandable),
                        EvalResult("_1", "{C1<object>}", "C1<object>", "o._1", DkmEvaluationResultFlags.Expandable),
                        EvalResult("_2", "{C2}", "C2", "o._2", DkmEvaluationResultFlags.Expandable),
                        EvalResult("_3", "{C3<object>}", "C3<object>", "o._3", DkmEvaluationResultFlags.Expandable));
                    //  C0 _0 = new C0();
                    var moreChildren = GetChildren(children[0]);
                    Verify(moreChildren,
                        EvalResult(
                            "Results View",
                            "Expanding the Results View will enumerate the IEnumerable",
                            "",
                            "o._0, results",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                            DkmEvaluationResultCategory.Method));
                    moreChildren = GetChildren(moreChildren[0]);
                    Verify(moreChildren,
                        EvalResult(
                            "Empty",
                            "\"Enumeration yielded no results\"",
                            "string",
                            null,
                            DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.RawString));
                    // C1<object> _1 = new C1<object>();
                    moreChildren = GetChildren(children[1]);
                    Verify(moreChildren,
                        EvalResult(
                            "Results View",
                            "Expanding the Results View will enumerate the IEnumerable",
                            "",
                            "o._1, results",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                            DkmEvaluationResultCategory.Method));
                    moreChildren = GetChildren(moreChildren[0]);
                    Verify(moreChildren,
                        EvalResult(
                            "Empty",
                            "\"Enumeration yielded no results\"",
                            "string",
                            null,
                            DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.RawString));
                    // C2 _2 = new C2();
                    moreChildren = GetChildren(children[2]);
                    Verify(moreChildren,
                        EvalResult(
                            "Results View",
                            "Expanding the Results View will enumerate the IEnumerable",
                            "",
                            "o._2, results",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                            DkmEvaluationResultCategory.Method));
                    moreChildren = GetChildren(moreChildren[0]);
                    Verify(moreChildren,
                        EvalResult(
                            "Empty",
                            "\"Enumeration yielded no results\"",
                            "string",
                            null,
                            DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.RawString));
                    // C3<object> _3 = new C3<object>();
                    moreChildren = GetChildren(children[3]);
                    Verify(moreChildren,
                        EvalResult(
                            "Results View",
                            "Expanding the Results View will enumerate the IEnumerable",
                            "",
                            "o._3, results",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                            DkmEvaluationResultCategory.Method));
                    moreChildren = GetChildren(moreChildren[0]);
                    Verify(moreChildren,
                        EvalResult(
                            "Empty",
                            "\"Enumeration yielded no results\"",
                            "string",
                            null,
                            DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.RawString));
                }
            }
        }

        /// <summary>
        /// Do not instantiate proxy type for null IEnumerable.
        /// </summary>
        [WorkItem(1009646)]
        [Fact]
        public void IEnumerableNull()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
interface I : IEnumerable
{
}
class C
{
    IEnumerable<char> E = null;
    I F = null;
    string S = null;
}";
            var assembly = GetAssembly(source);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: type.Instantiate(),
                    type: runtime.GetType((TypeImpl)type));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("E", "null", "System.Collections.Generic.IEnumerable<char>", "o.E"),
                    EvalResult("F", "null", "I", "o.F"),
                    EvalResult("S", "null", "string", "o.S"));
            }
        }

        [Fact]
        public void GetEnumeratorException()
        {
            var source =
@"using System;
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}";
            using (new EnsureEnglishUICulture())
            {
                var assembly = GetAssembly(source);
                var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
                using (ReflectionUtilities.LoadAssemblies(assemblies))
                {
                    var runtime = new DkmClrRuntimeInstance(assemblies);
                    var type = assembly.GetType("C");
                    var value = CreateDkmClrValue(
                        value: type.Instantiate(),
                        type: runtime.GetType((TypeImpl)type));
                    var evalResult = FormatResult("o", value);
                    Verify(evalResult,
                        EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                    var children = GetChildren(evalResult);
                    Verify(children,
                        EvalResult(
                            "Results View",
                            "Expanding the Results View will enumerate the IEnumerable",
                            "",
                            "o, results",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                            DkmEvaluationResultCategory.Method));
                    children = GetChildren(children[0]);
                    Verify(children[6],
                        EvalResult("Message", "\"The method or operation is not implemented.\"", "string", null, DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                }
            }
        }

        [Fact, WorkItem(1145125, "DevDiv")]
        public void GetEnumerableException()
        {
            var source =
@"using System;
using System.Collections;
class E : Exception, IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return 1;
    }
}
class C
{
    internal IEnumerable P
    {
        get { throw new NotImplementedException(); }
    }
    internal IEnumerable Q
    {
        get { throw new E(); }
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "P",
                        "'o.P' threw an exception of type 'System.NotImplementedException'",
                        "System.Collections.IEnumerable {System.NotImplementedException}",
                        "o.P",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                    EvalResult(
                        "Q",
                        "'o.Q' threw an exception of type 'E'",
                        "System.Collections.IEnumerable {E}",
                        "o.Q",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown));
                children = GetChildren(children[1]);
                Verify(children[6],
                    EvalResult(
                        "Message",
                        "\"Exception of type 'E' was thrown.\"",
                        "string",
                        null,
                        DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        [Fact]
        public void GetEnumerableError()
        {
            var source =
@"using System.Collections;
class C
{
    bool f;
    internal ArrayList P
    {
        get { while (!this.f) { } return new ArrayList(); }
    }
}";
            DkmClrRuntimeInstance runtime = null;
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "P") ? CreateErrorValue(runtime.GetType(typeof(System.Collections.ArrayList)), "Function evaluation timed out") : null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var memberValue = value.GetMemberValue("P", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                var evalResult = FormatResult("o.P", memberValue);
                Verify(evalResult,
                    EvalFailedResult("o.P", "Function evaluation timed out", "System.Collections.ArrayList", "o.P"));
            }
        }

        /// <summary>
        /// If evaluation of the proxy Items property returns an error
        /// (say, evaluation of the enumerable requires func-eval and
        /// either func-eval is disabled or we're debugging a .dmp),
        /// we should include a row that reports the error rather than
        /// having an empty expansion (since the container Items property
        /// is [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]).
        /// Note, the native EE has an empty expansion when .dmp debugging.
        /// </summary>
        [WorkItem(1043746)]
        [Fact]
        public void GetProxyPropertyValueError()
        {
            var source =
@"using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}";
            DkmClrRuntimeInstance runtime = null;
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "Items") ? CreateErrorValue(runtime.GetType(typeof(object)).MakeArrayType(), string.Format("Unable to evaluate '{0}'", m)) : null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalFailedResult("Error", "Unable to evaluate 'Items'", flags: DkmEvaluationResultFlags.None));
            }
        }

        /// <summary>
        /// Root-level synthetic values declared as IEnumerable or
        /// IEnumerable&lt;T&gt; should be expanded directly
        /// without intermediate "Results View" row.
        /// </summary>
        [WorkItem(1114276)]
        [Fact]
        public void SyntheticIEnumerable()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C
{
    IEnumerable P { get { yield return 1; yield return 2; } }
    IEnumerable<int> Q { get { yield return 3; } }
    IEnumerable R { get { return null; } }
    IEnumerable S { get { return string.Empty; } }
    IEnumerable<int> T { get { return new int[] { 4, 5 }; } }
    IList<int> U { get { return new List<int>(new int[] { 6 }); } }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();

                // IEnumerable
                var evalResult = FormatPropertyValue(runtime, value, "P");
                Verify(evalResult,
                    EvalResult(
                        "P",
                        "{C.<get_P>d__1}",
                        "System.Collections.IEnumerable {C.<get_P>d__1}",
                        "P",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("[0]", "1", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(P).Items[0]"),
                    EvalResult("[1]", "2", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(P).Items[1]"));

                // IEnumerable<int>
                evalResult = FormatPropertyValue(runtime, value, "Q");
                Verify(evalResult,
                    EvalResult(
                        "Q",
                        "{C.<get_Q>d__3}",
                        "System.Collections.Generic.IEnumerable<int> {C.<get_Q>d__3}",
                        "Q",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("[0]", "3", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(Q).Items[0]"));

                // null (unchanged)
                evalResult = FormatPropertyValue(runtime, value, "R");
                Verify(evalResult,
                    EvalResult(
                        "R",
                        "null",
                        "System.Collections.IEnumerable",
                        "R",
                        DkmEvaluationResultFlags.None));

                // string (unchanged)
                evalResult = FormatPropertyValue(runtime, value, "S");
                Verify(evalResult,
                    EvalResult(
                        "S",
                        "\"\"",
                        "System.Collections.IEnumerable {string}",
                        "S",
                        DkmEvaluationResultFlags.RawString,
                        DkmEvaluationResultCategory.Other,
                       editableValue: "\"\""));

                // array (unchanged)
                evalResult = FormatPropertyValue(runtime, value, "T");
                Verify(evalResult,
                    EvalResult(
                        "T",
                        "{int[2]}",
                        "System.Collections.Generic.IEnumerable<int> {int[]}",
                        "T",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("[0]", "4", "int", "((int[])T)[0]"),
                    EvalResult("[1]", "5", "int", "((int[])T)[1]"));

                // IList<int> declared type (unchanged)
                evalResult = FormatPropertyValue(runtime, value, "U");
                Verify(evalResult,
                    EvalResult(
                        "U",
                        "Count = 1",
                        "System.Collections.Generic.IList<int> {System.Collections.Generic.List<int>}",
                        "U",
                        DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("[0]", "6", "int", "new System.Collections.Generic.Mscorlib_CollectionDebugView<int>(U).Items[0]"),
                    EvalResult("Raw View", null, "", "U, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            }
        }

        [WorkItem(4098, "https://github.com/dotnet/roslyn/issues/4098")]
        [Fact]
        public void IEnumerableOfAnonymousType()
        {
            var code =
@"using System.Collections.Generic;
using System.Linq;

class C
{
    static void M(List<int> list)
    {
        var result = from x in list from y in list where x > 0 select new { x, y };
    }
}";
            var assembly = GetAssembly(code);
            var assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly);
            using (ReflectionUtilities.LoadAssemblies(assemblies))
            {
                var runtime = new DkmClrRuntimeInstance(assemblies);
                var anonymousType = assembly.GetType("<>f__AnonymousType0`2").MakeGenericType(typeof(int), typeof(int));
                var type = typeof(Enumerable).GetNestedType("WhereSelectEnumerableIterator`2", BindingFlags.NonPublic).MakeGenericType(anonymousType, anonymousType);
                var displayClass = assembly.GetType("C+<>c");
                var instance = displayClass.Instantiate();
                var ctor = type.GetConstructors().Single();
                var parameters = ctor.GetParameters();
                var listType = typeof(List<>).MakeGenericType(anonymousType);
                var source = listType.Instantiate();
                listType.GetMethod("Add").Invoke(source, new[] { anonymousType.Instantiate(1, 1) });
                var predicate = Delegate.CreateDelegate(parameters[1].ParameterType, instance, displayClass.GetMethod("<M>b__0_2", BindingFlags.Instance | BindingFlags.NonPublic));
                var selector = Delegate.CreateDelegate(parameters[2].ParameterType, instance, displayClass.GetMethod("<M>b__0_3", BindingFlags.Instance | BindingFlags.NonPublic));
                var value = CreateDkmClrValue(
                    value: type.Instantiate(source, predicate, selector),
                    type: runtime.GetType((TypeImpl)type));
                var expr = "from x in my_list from y in my_list where x > 0 select new { x, y }";
                var typeName = "System.Linq.Enumerable.WhereSelectEnumerableIterator<<>f__AnonymousType0<int, int>, <>f__AnonymousType0<int, int>>";

                var name = expr + ";";
                var evalResult = FormatResult(name, value);
                Verify(evalResult,
                    EvalResult(name, $"{{{typeName}}}", typeName, expr, DkmEvaluationResultFlags.Expandable));
                var resultsViewRow = GetChildren(evalResult).Last();
                Verify(GetChildren(resultsViewRow),
                    EvalResult(
                        "[0]",
                        "{{ x = 1, y = 1 }}",
                        "<>f__AnonymousType0<int, int>",
                        null,
                        DkmEvaluationResultFlags.Expandable));

                name = expr + ", results";
                evalResult = FormatResult(name, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalResult(name, $"{{{typeName}}}", typeName, name, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
                Verify(GetChildren(evalResult),
                    EvalResult(
                        "[0]",
                        "{{ x = 1, y = 1 }}",
                        "<>f__AnonymousType0<int, int>",
                        null,
                        DkmEvaluationResultFlags.Expandable));
            }
        }

        private DkmEvaluationResult FormatPropertyValue(DkmClrRuntimeInstance runtime, object value, string propertyName)
        {
            var propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var propertyValue = propertyInfo.GetValue(value);
            var propertyType = runtime.GetType(propertyInfo.PropertyType);
            var valueType = (propertyValue == null) ? propertyType : runtime.GetType(propertyValue.GetType());
            return FormatResult(
                propertyName,
                CreateDkmClrValue(propertyValue, type: valueType, valueFlags: DkmClrValueFlags.Synthetic),
                declaredType: propertyType);
        }
    }
}
