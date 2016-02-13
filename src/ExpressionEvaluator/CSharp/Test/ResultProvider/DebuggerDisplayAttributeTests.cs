// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DebuggerDisplayAttributeTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void WithoutExpressionHoles()
        {
            var source = @"
using System.Diagnostics;

class C0 { }

[DebuggerDisplay(""Value"")]
class C1 { }

[DebuggerDisplay(""Value"", Name=""Name"")]
class C2 { }

[DebuggerDisplay(""Value"", Type=""Type"")]
class C3 { }

[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
class C4 { }

class Wrapper
{
    C0 c0 = new C0();
    C1 c1 = new C1();
    C2 c2 = new C2();
    C3 c3 = new C3();
    C4 c4 = new C4();
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("Wrapper");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(GetChildren(FormatResult("w", value)),
                EvalResult("c0", "{C0}", "C0", "w.c0", DkmEvaluationResultFlags.None),
                EvalResult("c1", "Value", "C1", "w.c1", DkmEvaluationResultFlags.None),
                EvalResult("Name", "Value", "C2", "w.c2", DkmEvaluationResultFlags.None),
                EvalResult("c3", "Value", "Type", "w.c3", DkmEvaluationResultFlags.None),
                EvalResult("Name", "Value", "Type", "w.c4", DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void OnlyExpressionHoles()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""{value}"", Name=""{name}"", Type=""{type}"")]
class C
{
    string name = ""Name"";
    string value = ""Value"";
    string type = ""Type"";
}

class Wrapper
{
    C c = new C();
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("Wrapper");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(GetChildren(FormatResult("c", value)),
                EvalResult("\"Name\"", "\"Value\"", "\"Type\"", "c.c", DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void FormatStrings()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""<{value}>"", Name=""<{name}>"", Type=""<{type}>"")]
class C
{
    string name = ""Name"";
    string value = ""Value"";
    string type = ""Type"";
}

class Wrapper
{
    C c = new C();
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("Wrapper");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(GetChildren(FormatResult("w", value)),
                EvalResult("<\"Name\">", "<\"Value\">", "<\"Type\">", "w.c", DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void BindingError()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""<{missing}>"")]
class C
{
}
";
            const string rootExpr = "c"; // Note that this is the full name in all cases - DebuggerDisplayAttribute does not affect it.

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(FormatResult(rootExpr, value),
                EvalResult(rootExpr, "<Problem evaluating expression>", "C", rootExpr, DkmEvaluationResultFlags.None)); // Message inlined without quotation marks.
        }

        [Fact]
        public void RecursiveDebuggerDisplay()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""{value}"")]
class C
{
    C value;

    C()
    {
        this.value = this;
    }
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            // No stack overflow, since attribute on computed value is ignored.
            Verify(FormatResult(rootExpr, value),
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void MultipleAttributes()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""V1"")]
[DebuggerDisplay(""V2"")]
class C
{
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            // First attribute wins, as in dev12.
            Verify(FormatResult(rootExpr, value),
                EvalResult(rootExpr, "V1", "C", rootExpr));
        }

        [Fact]
        public void NullValues()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(null, Name=null, Type=null)]
class C
{
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(FormatResult(rootExpr, value),
                EvalResult(rootExpr, "{C}", "C", rootExpr));
        }

        [Fact]
        public void EmptyStringValues()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay("""", Name="""", Type="""")]
class C
{
}

class Wrapper
{
    C c = new C();
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("Wrapper");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(GetChildren(FormatResult("w", value)),
                EvalResult("", "", "", "w.c"));
        }

        [Fact]
        public void ConstructedGenericType()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Name"")]
class C<T>
{
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C`1").MakeGenericType(typeof(int));
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(FormatResult(rootExpr, value),
                EvalResult("c", "Name", "C<int>", rootExpr));
        }

        [Fact]
        public void MemberExpansion()
        {
            var source = @"
using System.Diagnostics;

interface I
{
    D P { get; }
}

class C : I
{
    D I.P { get { return new D(); } }
    D Q { get { return new D(); } }
}

[DebuggerDisplay(""Value"", Name=""Name"")]
class D 
{
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            var root = FormatResult(rootExpr, value);

            Verify(root,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(root),
                EvalResult("Name", "Value", "D", "((I)c).P", DkmEvaluationResultFlags.ReadOnly), // Not "I.Name".
                EvalResult("Name", "Value", "D", "c.Q", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void PointerDereferenceExpansion_Null()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
unsafe struct Display
{
    Display* DisplayPointer;
    NoDisplay* NoDisplayPointer;
}

unsafe struct NoDisplay
{
    Display* DisplayPointer;
    NoDisplay* NoDisplayPointer;
}

class Wrapper
{
    Display display = new Display();
}
";
            var assembly = GetUnsafeAssembly(source);

            var type = assembly.GetType("Wrapper");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            var root = FormatResult("wrapper", value);

            Verify(DepthFirstSearch(GetChildren(root).Single(), maxDepth: 3),
                EvalResult("Name", "Value", "Type", "wrapper.display", DkmEvaluationResultFlags.Expandable),
                EvalResult("DisplayPointer", PointerToString(IntPtr.Zero), "Display*", "wrapper.display.DisplayPointer"),
                EvalResult("NoDisplayPointer", PointerToString(IntPtr.Zero), "NoDisplay*", "wrapper.display.NoDisplayPointer"));
        }

        [WorkItem(321, "https://github.com/dotnet/roslyn/issues/321")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/321")]
        public void PointerDereferenceExpansion_NonNull()
        {
            var source = @"
using System;
using System.Diagnostics;

[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
unsafe struct Display
{
    public Display* DisplayPointer;
    public NoDisplay* NoDisplayPointer;
}

unsafe struct NoDisplay
{
    public Display* DisplayPointer;
    public NoDisplay* NoDisplayPointer;
}

unsafe class C
{
    Display* DisplayPointer;
    NoDisplay* NoDisplayPointer;

    public C(IntPtr d, IntPtr nd)
    {
        this.DisplayPointer = (Display*)d;
        this.NoDisplayPointer = (NoDisplay*)nd;
    
        this.DisplayPointer->DisplayPointer = this.DisplayPointer;
        this.DisplayPointer->NoDisplayPointer = this.NoDisplayPointer;
    
        this.NoDisplayPointer->DisplayPointer = this.DisplayPointer;
        this.NoDisplayPointer->NoDisplayPointer = this.NoDisplayPointer;
    }
}
";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                var displayType = assembly.GetType("Display");
                var displayInstance = displayType.Instantiate();
                var displayHandle = GCHandle.Alloc(displayInstance, GCHandleType.Pinned);
                var displayPtr = displayHandle.AddrOfPinnedObject();

                var noDisplayType = assembly.GetType("NoDisplay");
                var noDisplayInstance = noDisplayType.Instantiate();
                var noDisplayHandle = GCHandle.Alloc(noDisplayInstance, GCHandleType.Pinned);
                var noDisplayPtr = noDisplayHandle.AddrOfPinnedObject();

                var testType = assembly.GetType("C");
                var testInstance = ReflectionUtilities.Instantiate(testType, displayPtr, noDisplayPtr);
                var testValue = CreateDkmClrValue(testInstance, testType, evalFlags: DkmEvaluationResultFlags.None);

                var displayPtrString = PointerToString(displayPtr);
                var noDisplayPtrString = PointerToString(noDisplayPtr);

                Verify(DepthFirstSearch(FormatResult("c", testValue), maxDepth: 3),
                    EvalResult("c", "{C}", "C", "c", DkmEvaluationResultFlags.Expandable),
                    EvalResult("DisplayPointer", displayPtrString, "Display*", "c.DisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("Name", "Value", "Type", "*c.DisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("DisplayPointer", displayPtrString, "Display*", "(*c.DisplayPointer).DisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("NoDisplayPointer", noDisplayPtrString, "NoDisplay*", "(*c.DisplayPointer).NoDisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("NoDisplayPointer", noDisplayPtrString, "NoDisplay*", "c.NoDisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("*c.NoDisplayPointer", "{NoDisplay}", "NoDisplay", "*c.NoDisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("DisplayPointer", displayPtrString, "Display*", "(*c.NoDisplayPointer).DisplayPointer", DkmEvaluationResultFlags.Expandable),
                    EvalResult("NoDisplayPointer", noDisplayPtrString, "NoDisplay*", "(*c.NoDisplayPointer).NoDisplayPointer", DkmEvaluationResultFlags.Expandable));

                displayHandle.Free();
                noDisplayHandle.Free();
            }
        }

        [Fact]
        public void ArrayExpansion()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
struct Display
{
    public Display[] DisplayArray;
    public NoDisplay[] NoDisplayArray;
}

struct NoDisplay
{
    public Display[] DisplayArray;
    public NoDisplay[] NoDisplayArray;
}

class C
{
    public Display[] DisplayArray;
    public NoDisplay[] NoDisplayArray;

    public C()
    {
        this.DisplayArray = new[] { new Display() };
        this.NoDisplayArray = new[] { new NoDisplay() };
    
        this.DisplayArray[0].DisplayArray = this.DisplayArray;
        this.DisplayArray[0].NoDisplayArray = this.NoDisplayArray;
    
        this.NoDisplayArray[0].DisplayArray = this.DisplayArray;
        this.NoDisplayArray[0].NoDisplayArray = this.NoDisplayArray;
    }
}
";
            var assembly = GetUnsafeAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            var root = FormatResult("c", value);

            Verify(DepthFirstSearch(root, maxDepth: 4),
                EvalResult("c", "{C}", "C", "c", DkmEvaluationResultFlags.Expandable),
                EvalResult("DisplayArray", "{Display[1]}", "Display[]", "c.DisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("Name", "Value", "Type", "c.DisplayArray[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("DisplayArray", "{Display[1]}", "Display[]", "c.DisplayArray[0].DisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("Name", "Value", "Type", "c.DisplayArray[0].DisplayArray[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("NoDisplayArray", "{NoDisplay[1]}", "NoDisplay[]", "c.DisplayArray[0].NoDisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("[0]", "{NoDisplay}", "NoDisplay", "c.DisplayArray[0].NoDisplayArray[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("NoDisplayArray", "{NoDisplay[1]}", "NoDisplay[]", "c.NoDisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("[0]", "{NoDisplay}", "NoDisplay", "c.NoDisplayArray[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("DisplayArray", "{Display[1]}", "Display[]", "c.NoDisplayArray[0].DisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("Name", "Value", "Type", "c.NoDisplayArray[0].DisplayArray[0]", DkmEvaluationResultFlags.Expandable),
                EvalResult("NoDisplayArray", "{NoDisplay[1]}", "NoDisplay[]", "c.NoDisplayArray[0].NoDisplayArray", DkmEvaluationResultFlags.Expandable),
                EvalResult("[0]", "{NoDisplay}", "NoDisplay", "c.NoDisplayArray[0].NoDisplayArray[0]", DkmEvaluationResultFlags.Expandable));
        }

        [Fact]
        public void DebuggerTypeProxyExpansion()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
public struct Display { }

public struct NoDisplay { }

[DebuggerTypeProxy(typeof(P))]
public class C
{
    public Display DisplayC = new Display();
    public NoDisplay NoDisplayC = new NoDisplay();
}

public class P
{
    public Display DisplayP = new Display();
    public NoDisplay NoDisplayP = new NoDisplay();

    public P(C c) { }
}
";
            var assembly = GetUnsafeAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);

            var root = FormatResult("c", value);

            Verify(DepthFirstSearch(root, maxDepth: 4),
                EvalResult("c", "{C}", "C", "c", DkmEvaluationResultFlags.Expandable),
                EvalResult("Name", "Value", "Type", "new P(c).DisplayP"),
                EvalResult("NoDisplayP", "{NoDisplay}", "NoDisplay", "new P(c).NoDisplayP"),
                EvalResult("Raw View", null, "", "c, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("Name", "Value", "Type", "c.DisplayC"),
                EvalResult("NoDisplayC", "{NoDisplay}", "NoDisplay", "c.NoDisplayC"));
        }

        [Fact]
        public void NullInstance()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Hello"")]
class C
{
}
";
            const string rootExpr = "c";

            var assembly = GetAssembly(source);

            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(null, type, evalFlags: DkmEvaluationResultFlags.None);

            Verify(FormatResult(rootExpr, value),
                EvalResult(rootExpr, "null", "C", rootExpr));
        }

        [Fact]
        public void NonGenericDisplayAttributeOnGenericBase()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Type={GetType()}"")]
class A<T> { }
class B : A<int> { }
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var value = CreateDkmClrValue(type.Instantiate(), type, evalFlags: DkmEvaluationResultFlags.None);
            var result = FormatResult("b", value);
            Verify(result,
                EvalResult("b", "Type={B}", "B", "b", DkmEvaluationResultFlags.None));
        }

        [WorkItem(1016895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016895")]
        [Fact]
        public void RootVersusInternal()
        {
            var source = @"
using System.Diagnostics;

[DebuggerDisplay(""Value"", Name = ""Name"")]
class A { }
class B
{
    A a;

    public B(A a)
    {
        this.a = a;
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A");
            var typeB = assembly.GetType("B");
            var instanceA = typeA.Instantiate();
            var instanceB = typeB.Instantiate(instanceA);
            var result = FormatResult("a", CreateDkmClrValue(instanceA));
            Verify(result,
                EvalResult("a", "Value", "A", "a", DkmEvaluationResultFlags.None));

            result = FormatResult("b", CreateDkmClrValue(instanceB));
            Verify(GetChildren(result),
                EvalResult("Name", "Value", "A", "b.a", DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void Error()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""Value"", Name=""Name"", Type=""Type"")]
class A
{
}
class B
{
    bool f;
    internal A P { get { return new A(); } }
    internal A Q { get { while(f) { } return new A(); } }
}
";
            DkmClrRuntimeInstance runtime = null;
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "Q") ? CreateErrorValue(runtime.GetType("A"), "Function evaluation timed out") : null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("B");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var evalResult = FormatResult("o", value);
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("Name", "Value", "Type", "o.P", DkmEvaluationResultFlags.ReadOnly),
                    EvalFailedResult("Q", "Function evaluation timed out", "A", "o.Q"),
                    EvalResult("f", "false", "bool", "o.f", DkmEvaluationResultFlags.Boolean));
            }
        }

        [Fact]
        public void UnhandledException()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""Value}"")]
class A
{
    internal int Value;
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A");
            var instanceA = typeA.Instantiate();
            var result = FormatResult("a", CreateDkmClrValue(instanceA));
            Verify(result,
                EvalFailedResult("a", "Unmatched closing brace in 'Value}'", null, null, DkmEvaluationResultFlags.None));
        }

        [Fact, WorkItem(171123, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv")]
        public void ExceptionDuringEvaluate()
        {
            var source = @"
using System.Diagnostics;
[DebuggerDisplay(""Make it throw."")]
public class Picard { }
";
            var assembly = GetAssembly(source);
            var picard = assembly.GetType("Picard");
            var jeanLuc = picard.Instantiate();
            var result = FormatResult("says", CreateDkmClrValue(jeanLuc), declaredType: new BadType(picard));
            Verify(result,
                EvalFailedResult("says", BadType.Exception.Message, null, null, DkmEvaluationResultFlags.None));
        }

        private class BadType : DkmClrType
        {
            public static readonly Exception Exception = new TargetInvocationException(new DkmException(DkmExceptionCode.E_PROCESS_DESTROYED));

            public BadType(System.Type innerType)
                : base((TypeImpl)innerType)
            {
            }

            public override VisualStudio.Debugger.Metadata.Type GetLmrType()
            {
                if (Environment.StackTrace.Contains("Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider.GetTypeName"))
                {
                    throw Exception;
                }

                return base.GetLmrType();
            }
        }

        private IReadOnlyList<DkmEvaluationResult> DepthFirstSearch(DkmEvaluationResult root, int maxDepth)
        {
            var builder = ArrayBuilder<DkmEvaluationResult>.GetInstance();

            DepthFirstSearchInternal(builder, root, 0, maxDepth);

            return builder.ToImmutableAndFree();
        }

        private void DepthFirstSearchInternal(ArrayBuilder<DkmEvaluationResult> builder, DkmEvaluationResult curr, int depth, int maxDepth)
        {
            Assert.InRange(depth, 0, maxDepth);
            builder.Add(curr);

            var childDepth = depth + 1;
            if (childDepth <= maxDepth)
            {
                foreach (var child in GetChildren(curr))
                {
                    DepthFirstSearchInternal(builder, child, childDepth, maxDepth);
                }
            }
        }
    }
}
