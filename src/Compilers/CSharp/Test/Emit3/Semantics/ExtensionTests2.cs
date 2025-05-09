// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public partial class ExtensionTests : CompilingTestBase
{
    [Fact]
    public void Deconstruct_01()
    {
        var src = """
var (x, y) = "";

static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i, out int j, params int[] k) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(1, 6),
            // (1,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(1, 9),
            // (1,14): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'string', with 2 out parameters and a void return type.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, @"""""").WithArguments("string", "2").WithLocation(1, 14));
    }

    [Fact]
    public void Deconstruct_02()
    {
        var src = """
var (x, y) = "";

static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i, out int j, int k = 0) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,6): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(1, 6),
            // (1,9): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(1, 9),
            // (1,14): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'string', with 2 out parameters and a void return type.
            // var (x, y) = "";
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, @"""""").WithArguments("string", "2").WithLocation(1, 14));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75484")]
    public void Deconstruction_UnscopedRef_ExtensionMethod()
    {
        var source = """
class C
{
    R M1()
    {
        new S().Deconstruct(out var x, out _);
        return x; // 1
    }
    R M2()
    {
        (var x, _) = new S();
        return x; // 2
    }
    R M3()
    {
        if (new S() is (var x, _))
            return x; // 3
        return default;
    }
}
struct S;
ref struct R;
static class E
{
    extension(in S s)
    {
        public void Deconstruct(out R x, out int y) => throw null;
    }
}
""";
        CreateCompilation(source).VerifyDiagnostics(
            // (6,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x; // 1
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(6, 16),
            // (11,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x; // 2
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(11, 16),
            // (16,20): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //             return x; // 3
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(16, 20));
    }

    [Fact]
    public void Deconstruction_ScopedRef_ExtensionMethod()
    {
        var source = """
class C
{
    R M1()
    {
        new S().Deconstruct(out var x, out _);
        return x;
    }
    R M2()
    {
        (var x, _) = new S();
        return x;
    }
    R M3()
    {
        if (new S() is (var x, _))
            return x;
        return default;
    }
}
struct S;
ref struct R;
static class E
{
    extension(scoped in S s)
    {
        public void Deconstruct(out R x, out int y) => throw null;
    }
}
""";
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void ForeachDeconstruct_Conversion()
    {
        var src = """
C[] c = new C[] { new C() };
foreach (var (x1, x2) in c)
{
    System.Console.Write(x1.ToString());
}

class C { }

static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i1, out int i2) { i1 = i2 = 42; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void PositionalPattern_01()
    {
        var src = """
_ = "" is (i: 42, other: 43);

static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i, out int j) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,19): error CS8517: The name 'other' does not match the corresponding 'Deconstruct' parameter 'j'.
            // _ = "" is (i: 42, other: 43);
            Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "other").WithArguments("other", "j").WithLocation(1, 19));
    }

    [Fact]
    public void PositionalPattern_02()
    {
        var src = """
_ = new C() is var (x, y);

class C { }

static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i, out int j) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void InvocationOnNull()
    {
        var src = """
null.M1("");
null.M2("");

static class E
{
    extension<T>(T t1)
    {
        public void M1(T t2) => throw null!;
    }

    public static void M2<T>(this T t1, T t2) => throw null!;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            // null.M1("");
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.M1").WithArguments(".", "<null>").WithLocation(1, 1),
            // (2,1): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            // null.M2("");
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.M2").WithArguments(".", "<null>").WithLocation(2, 1));
    }

    [Fact]
    public void RemoveLowerPriorityMembers_Deconstruct()
    {
        var src = """
var (x, y) = "";

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public void Deconstruct(out int i2, out int i3) { System.Console.Write("ran"); i2 = i3 = 43; }
    }
    extension(string s)
    {
        public void Deconstruct(out int i2, out int i3) => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();

        src = """
var (x, y) = "";

public static class E
{
    extension(object o)
    {
        public void Deconstruct(out int i2, out int i3) => throw null;
    }
    extension(string s)
    {
        public void Deconstruct(out int i2, out int i3) { System.Console.Write("ran"); i2 = i3 = 43; }
    }
}
""";
        comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_Foreach_GetEnumerator()
    {
        var src = """
using System.Collections.Generic;

foreach (var x in new C()) { System.Console.Write(x); }

public class C { }

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public IEnumerator<int> GetEnumerator() { yield return 42; }
    }

    extension(C c)
    {
        public IEnumerator<int> GetEnumerator() => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        try
        {
            // Tracked by https://github.com/dotnet/roslyn/issues/76130 : assertion in NullableWalker
            CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
        }
        catch (InvalidOperationException)
        {
        }
    }

    [Fact]
    public void RemoveLowerPriorityMembers_CollectionInitializer()
    {
        var src = """
using System.Collections;
using System.Collections.Generic;

_ = new C() { 42 };

public class C : IEnumerable<int>, IEnumerable
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public void Add(int i) { System.Console.Write("add"); }
    }

    extension(C c)
    {
        public void Add(int i) => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "add").VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_Fixed()
    {
        var src = """
unsafe class C
{
    public static void Main()
    {
        fixed (int* p = new Fixable()) { }
    }
}

public class Fixable { }

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public ref int GetPinnableReference() { System.Console.Write("ran"); return ref (new int[] { 1, 2, 3 })[0]; }
    }

    extension(Fixable f)
    {
        public ref int GetPinnableReference() => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.UnsafeDebugExe);
        CompileAndVerify(comp, expectedOutput: "ran", verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_Await()
    {
        var src = """
using System;
using System.Runtime.CompilerServices;

int i = await new C();
System.Console.Write(i);

public class C { }

public class D : INotifyCompletion
{
    public int GetResult() => 42;
    public void OnCompleted(Action continuation) => throw null;
    public bool IsCompleted => true;
}

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public D GetAwaiter() => new D();
    }

    extension(C c)
    {
        public D GetAwaiter() => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_ObjectInitializer()
    {
        var src = """
_ = new C() { Property = 42 };

public class C { }

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int Property { set { System.Console.Write("property"); } }
    }

    extension(C c)
    {
        public int Property => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "property").VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_With()
    {
        var src = """
_ = new S() with { Property = 42 };

public struct S { }

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int Property { set { System.Console.Write("property"); } }
    }

    extension(S s)
    {
        public int Property { set => throw null; }
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "property").VerifyDiagnostics();
    }

    [Fact]
    public void RemoveLowerPriorityMembers_PropertyPattern()
    {
        var src = """
_ = new C() is { Property: 42 };

public class C{ }

public static class E
{
    extension(object o)
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public int Property { get { System.Console.Write("property"); return 42; } }
    }

    extension(C c)
    {
        public int Property => throw null;
    }
}
""";
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "property").VerifyDiagnostics();
    }
}

