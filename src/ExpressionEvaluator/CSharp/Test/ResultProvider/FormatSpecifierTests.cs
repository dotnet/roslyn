// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class FormatSpecifierTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void NoQuotes_String()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib());
            var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes);
            var stringType = runtime.GetType(typeof(string));

            // null
            var value = CreateDkmClrValue(null, type: stringType);
            var evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "null", "string", "s", editableValue: null, flags: DkmEvaluationResultFlags.None));

            // ""
            value = CreateDkmClrValue(string.Empty, type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "", "string", "s", editableValue: "\"\"", flags: DkmEvaluationResultFlags.RawString));

            // "'"
            value = CreateDkmClrValue("'", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "'", "string", "s", editableValue: "\"'\"", flags: DkmEvaluationResultFlags.RawString));

            // "\""
            value = CreateDkmClrValue("\"", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "\"", "string", "s", editableValue: "\"\\\"\"", flags: DkmEvaluationResultFlags.RawString));

            // "\\"
            value = CreateDkmClrValue("\\", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "\\", "string", "s", editableValue: "\"\\\\\"", flags: DkmEvaluationResultFlags.RawString));

            // "a\r\n\t\v\b\u001eb"
            value = CreateDkmClrValue("a\r\n\tb\v\b\u001ec", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "a\r\n\tb\v\b\u001ec", "string", "s", editableValue: "\"a\\r\\n\\tb\\v\\b\\u001ec\"", flags: DkmEvaluationResultFlags.RawString));

            // "a\0b"
            value = CreateDkmClrValue("a\0b", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "a\0b", "string", "s", editableValue: "\"a\\0b\"", flags: DkmEvaluationResultFlags.RawString));

            // "\u007f\u009f"
            value = CreateDkmClrValue("\u007f\u009f", type: stringType);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "\u007f\u009f", "string", "s", editableValue: "\"\\u007f\\u009f\"", flags: DkmEvaluationResultFlags.RawString));

            // " " with alias
            value = CreateDkmClrValue(" ", type: stringType, alias: "$1", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "  {$1}", "string", "s", editableValue: "\" \"", flags: DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.HasObjectId));

            // array
            value = CreateDkmClrValue(new string[] { "1" }, type: stringType.MakeArrayType());
            evalResult = FormatResult("a", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("a", "{string[1]}", "string[]", "a", editableValue: null, flags: DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            // DkmInspectionContext should not be inherited.
            Verify(children,
                EvalResult("[0]", "\"1\"", "string", "a[0]", editableValue: "\"1\"", flags: DkmEvaluationResultFlags.RawString));
        }

        [Fact]
        public void NoQuotes_Char()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib());
            var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes);
            var charType = runtime.GetType(typeof(char));

            // 0
            var value = CreateDkmClrValue((char)0, type: charType);
            var evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "0 \0", "char", "c", editableValue: "'\\0'", flags: DkmEvaluationResultFlags.None));

            // '\''
            value = CreateDkmClrValue('\'', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "39 '", "char", "c", editableValue: "'\\''", flags: DkmEvaluationResultFlags.None));

            // '"'
            value = CreateDkmClrValue('"', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "34 \"", "char", "c", editableValue: "'\"'", flags: DkmEvaluationResultFlags.None));

            // '\\'
            value = CreateDkmClrValue('\\', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "92 \\", "char", "c", editableValue: "'\\\\'", flags: DkmEvaluationResultFlags.None));

            // '\n'
            value = CreateDkmClrValue('\n', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "10 \n", "char", "c", editableValue: "'\\n'", flags: DkmEvaluationResultFlags.None));

            // '\u001e'
            value = CreateDkmClrValue('\u001e', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "30 \u001e", "char", "c", editableValue: "'\\u001e'", flags: DkmEvaluationResultFlags.None));

            // '\u007f'
            value = CreateDkmClrValue('\u007f', type: charType);
            evalResult = FormatResult("c", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("c", "127 \u007f", "char", "c", editableValue: "'\\u007f'", flags: DkmEvaluationResultFlags.None));

            // array
            value = CreateDkmClrValue(new char[] { '1' }, type: charType.MakeArrayType());
            evalResult = FormatResult("a", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("a", "{char[1]}", "char[]", "a", editableValue: null, flags: DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            // DkmInspectionContext should not be inherited.
            Verify(children,
                EvalResult("[0]", "49 '1'", "char", "a[0]", editableValue: "'1'", flags: DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void NoQuotes_DebuggerDisplay()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F}+{G}"")]
class C
{
    string F = ""f"";
    object G = 'g';
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes));
                Verify(evalResult,
                    EvalResult("o", "f+103 g", "C", "o", DkmEvaluationResultFlags.Expandable));
            }
        }

        [Fact]
        public void RawView_NoProxy()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib());
            var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.ShowValueRaw);

            // int
            var value = CreateDkmClrValue(1, type: runtime.GetType(typeof(int)));
            var evalResult = FormatResult("i", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("i", "1", "int", "i, raw", editableValue: null, flags: DkmEvaluationResultFlags.None));

            // string
            value = CreateDkmClrValue(string.Empty, type: runtime.GetType(typeof(string)));
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("s", "\"\"", "string", "s, raw", editableValue: "\"\"", flags: DkmEvaluationResultFlags.RawString));

            // object[]
            value = CreateDkmClrValue(new object[] { 1, 2, 3 }, type: runtime.GetType(typeof(object)).MakeArrayType());
            evalResult = FormatResult("a", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("a", "{object[3]}", "object[]", "a, raw", editableValue: null, flags: DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void RawView()
        {
            var source =
@"using System.Diagnostics;
internal class P
{
    public P(C c)
    {
        this.G = c.F != null;
    }
    public readonly bool G;
}
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal C() : this(new C(null))
    {
    }
    internal C(C f)
    {
        this.F = f;
    }
    internal readonly C F;
}
class Program
{
    static void Main()
    {
        var o = new C();
        System.Diagnostics.Debugger.Break();
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");

                // Non-null value.
                var value = type.Instantiate();
                var evalResult = FormatResult("o", "o, raw", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ShowValueRaw));
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o, raw", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "{C}", "C", "o.F", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
                children = GetChildren(children[0]);
                // ShowValueRaw is not inherited.
                Verify(children,
                    EvalResult("G", "false", "bool", "new P(o.F).G", DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Raw View", null, "", "o.F, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

                // Null value.
                value = CreateDkmClrValue(
                    value: null,
                    type: type);
                evalResult = FormatResult("o", "o, raw", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ShowValueRaw));
                Verify(evalResult,
                    EvalResult("o", "null", "C", "o, raw"));
            }
        }

        [Fact]
        public void ResultsView_FrameworkTypes()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore());
            var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly);

            // object: not enumerable
            var value = CreateDkmClrValue(new object(), type: runtime.GetType(typeof(object)));
            var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalFailedResult("o", "Only Enumerable types can have Results View", fullName: null));

            // string: not considered enumerable which is consistent with legacy EE
            value = CreateDkmClrValue("", type: runtime.GetType(typeof(string)));
            evalResult = FormatResult("s", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalFailedResult("s", "Only Enumerable types can have Results View"));

            // Array: not considered enumerable which is consistent with legacy EE
            value = CreateDkmClrValue(new[] { 1 }, type: runtime.GetType(typeof(int[])));
            evalResult = FormatResult("i", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalFailedResult("i", "Only Enumerable types can have Results View"));

            // ArrayList
            value = CreateDkmClrValue(new System.Collections.ArrayList(new[] { 2 }), type: runtime.GetType(typeof(System.Collections.ArrayList)));
            evalResult = FormatResult("a", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("a", "Count = 1", "System.Collections.ArrayList", "a, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "2", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView(a).Items[0]"));

            // List<object>
            value = CreateDkmClrValue(new System.Collections.Generic.List<object>(new object[] { 3 }), type: runtime.GetType(typeof(System.Collections.Generic.List<object>)));
            evalResult = FormatResult("l", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("l", "Count = 1", "System.Collections.Generic.List<object>", "l, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
            children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "3", "object {int}", "new System.Linq.SystemCore_EnumerableDebugView<object>(l).Items[0]"));

            // int?
            value = CreateDkmClrValue(1, type: runtime.GetType(typeof(System.Nullable<>)).MakeGenericType(runtime.GetType(typeof(int))));
            evalResult = FormatResult("i", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalFailedResult("i", "Only Enumerable types can have Results View"));
        }

        [Fact]
        public void ResultsView_IEnumerable()
        {
            var source =
@"using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new C();
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", "o, results, d", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o, results, d", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
                var children = GetChildren(evalResult);
                // ResultsOnly is not inherited.
                Verify(children,
                    EvalResult("[0]", "{C}", "object {C}", "new System.Linq.SystemCore_EnumerableDebugView(o).Items[0]"));
            }
        }

        [Fact]
        public void ResultsView_IEnumerableOfT()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;
struct S<T> : IEnumerable<T>
{
    private readonly T t;
    internal S(T t)
    {
        this.t = t;
    }
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        yield return t;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("S`1").MakeGenericType(runtime.GetType(typeof(int)));
                var value = type.Instantiate(2);
                var evalResult = FormatResult("o", "o, results", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalResult("o", "{S<int>}", "S<int>", "o, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("[0]", "2", "int", "new System.Linq.SystemCore_EnumerableDebugView<int>(o).Items[0]"));
            }
        }

        /// <summary>
        /// ResultsOnly is ignored for GetChildren and GetItems.
        /// </summary>
        [Fact]
        public void ResultsView_GetChildren()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C
{
    IEnumerable<int> F
    {
        get { yield return 1; }
    }
    IEnumerable G
    {
        get { yield return 2; }
    }
    int H
    {
        get { return 3; }
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", "o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                // GetChildren without ResultsOnly
                var children = GetChildren(evalResult, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.None));
                Verify(children,
                    EvalResult("F", "{C.<get_F>d__1}", "System.Collections.Generic.IEnumerable<int> {C.<get_F>d__1}", "o.F", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite),
                    EvalResult("G", "{C.<get_G>d__3}", "System.Collections.IEnumerable {C.<get_G>d__3}", "o.G", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite),
                    EvalResult("H", "3", "int", "o.H", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite));
                // GetChildren with ResultsOnly
                children = GetChildren(evalResult, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(children,
                    EvalResult("F", "{C.<get_F>d__1}", "System.Collections.Generic.IEnumerable<int> {C.<get_F>d__1}", "o.F, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("G", "{C.<get_G>d__3}", "System.Collections.IEnumerable {C.<get_G>d__3}", "o.G, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalFailedResult("H", "Only Enumerable types can have Results View", fullName: null));
            }
        }

        /// <summary>
        /// [DebuggerTypeProxy] should be ignored.
        /// </summary>
        [Fact]
        public void ResultsView_TypeProxy()
        {
            var source =
@"using System.Collections;
using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}
class P
{
    public P(C c)
    {
    }
    public object F
    {
        get { return 2; }
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", "o, results", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o, results", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
            }
        }

        [Fact]
        public void ResultsView_ExceptionThrown()
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
    internal ArrayList P
    {
        get { throw new NotImplementedException(); }
    }
    internal ArrayList Q
    {
        get { throw new E(); }
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var memberValue = value.GetMemberValue("P", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                var evalResult = FormatResult("o.P", "o.P, results", memberValue, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalFailedResult("o.P", "'o.P' threw an exception of type 'System.NotImplementedException'"));
                memberValue = value.GetMemberValue("Q", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                evalResult = FormatResult("o.Q", "o.Q, results", memberValue, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalFailedResult("o.Q", "'o.Q' threw an exception of type 'E'"));
            }
        }

        /// <summary>
        /// Report the error message for error values, regardless
        /// of whether the value is actually enumerable.
        /// </summary>
        [Fact]
        public void ResultsView_Error()
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
    internal int Q
    {
        get { while (!this.f) { } return 3; }
    }
}";
            DkmClrRuntimeInstance runtime = null;
            VisualStudio.Debugger.Evaluation.ClrCompilation.DkmClrValue getMemberValue(VisualStudio.Debugger.Evaluation.ClrCompilation.DkmClrValue v, string m)
            {
                switch (m)
                {
                    case "P":
                        return CreateErrorValue(runtime.GetType(typeof(System.Collections.ArrayList)), "Property 'P' evaluation timed out");
                    case "Q":
                        return CreateErrorValue(runtime.GetType(typeof(string)), "Property 'Q' evaluation timed out");
                    default:
                        return null;
                }
            }
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var memberValue = value.GetMemberValue("P", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                var evalResult = FormatResult("o.P", "o.P, results", memberValue, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalFailedResult("o.P", "Property 'P' evaluation timed out"));
                memberValue = value.GetMemberValue("Q", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                evalResult = FormatResult("o.Q", "o.Q, results", memberValue, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalFailedResult("o.Q", "Property 'Q' evaluation timed out"));
            }
        }

        [Fact]
        public void ResultsView_NoSystemCore()
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
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate();
                var evalResult = FormatResult("o", "o, results", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.ResultsOnly));
                Verify(evalResult,
                    EvalFailedResult("o", "Results View requires System.Core.dll to be referenced"));
            }
        }
    }
}
