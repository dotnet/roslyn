// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public void PositionalPattern_03()
    {
        // implicit span conversion
        var src = """
int[] i = new int[] { 1, 2 };
if (i is var (x, y))
    System.Console.Write((x, y));

static class E
{
    extension(System.Span<int> s)
    {
        public void Deconstruct(out int i, out int j) { i = 42; j = 43; }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("(42, 43)"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void PositionalPattern_04()
    {
        // implicit tuple conversion
        var src = """
var t = (42, "ran");
if (t is var (x, y))
    System.Console.Write((x, y));

static class E
{
    extension((object, object) t)
    {
        public void Deconstruct(out int i, out int j) { i = 42; j = 43; }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("(42, ran)")).VerifyDiagnostics();
    }

    [Fact]
    public void PositionalPattern_05()
    {
        // We check conversion during initial binding
        var src = """
int[] i = [];
_ = i is var (x, y);

static class E
{
    extension(System.ReadOnlySpan<int> r)
    {
        public void Deconstruct(out int i, out int j) => throw null;
    }
}

namespace System
{
    public ref struct ReadOnlySpan<T>
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,14): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.op_Implicit'
            // _ = i is var (x, y);
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(x, y)").WithArguments("System.ReadOnlySpan<T>", "op_Implicit").WithLocation(2, 14));
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
        try
        {
            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
            // Tracked by https://github.com/dotnet/roslyn/issues/78828 : assertion in NullableWalker
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

    [Fact]
    public void AnonymousType_01()
    {
        var src = """
var person = new { Name = "John", Age = 30 };
person.M();
person.M2();
_ = person.Property;

public static class E
{
    extension<T>(T t)
    {
        public void M() { System.Console.Write("method "); }
        public int Property { get { System.Console.Write("property"); return 42; } }
    }

    public static void M2<T>(this T t) { System.Console.Write("method2 "); }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "method method2 property").VerifyDiagnostics();
    }

    [Fact]
    public void AnonymousType_02()
    {
        // instance members come first
        var src = """
System.Action a = () => { System.Console.Write("method "); };
var person = new { DoStuff = a, Property = 42 };

person.DoStuff();
System.Console.Write(person.Property);

public static class E
{
    extension<T>(T t)
    {
        public void DoStuff() => throw null;
        public int Property => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "method 42").VerifyDiagnostics();
    }

    [Fact]
    public void Attribute_01()
    {
        var src = """
[My(Property = 42)]
class C { }

public class MyAttribute : System.Attribute { }

public static class E
{
    extension(MyAttribute a)
    {
        public int Property { get => throw null; set => throw null; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            // [My(Property = 42)]
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(1, 5));
    }

    [Fact]
    public void Lock_01()
    {
        var src = """
System.Threading.Lock x = new System.Threading.Lock();
lock (x) { }

namespace System.Threading
{
    public sealed class Lock
    {
        public Scope EnterScope() { System.Console.Write("ran "); return new Scope(); }

        public ref struct Scope
        {
            public void Dispose() { System.Console.Write("disposed"); }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        CompileAndVerify(comp, expectedOutput: "ran disposed", verify: Verification.Skipped).VerifyDiagnostics();

        src = """
System.Threading.Lock x = new System.Threading.Lock();
lock (x) { }

namespace System.Threading
{
    public sealed class Lock
    {
        public ref struct Scope
        {
            public void Dispose() => throw null;
        }
    }
}

public static class E
{
    extension(System.Threading.Lock x)
    {
        public System.Threading.Lock.Scope EnterScope() => throw null;
    }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (x) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));

        src = """
System.Threading.Lock x = new System.Threading.Lock();
lock (x) { }

namespace System.Threading
{
    public sealed class Lock
    {
        public Scope EnterScope() => throw null;
        public ref struct Scope
        {
        }
    }
}

public static class E
{
    extension(System.Threading.Lock.Scope x)
    {
        public void Dispose() => throw null;
    }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (x) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void Nullability_PropertyAccess_01()
    {
        // nullability check on the receiver, annotated extension parameter
        var src = """
#nullable enable

object? oNull = null;
_ = oNull.P;

object? oNull2 = null;
E.get_P(oNull2);

object? oNotNull = new object();
_ = oNotNull.P;

E.get_P(oNotNull);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object? o)
    {
        public int P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics();

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_Params_01()
    {
        var src = """
new object().M(1, 2, 3);
E.M(new object(), 1, 2, 3);
""";
        var libSrc = """
public static class E
{
    extension(object o)
    {
        public void M(params int[] i) { System.Console.Write((i[0], i[1], i[2])); }
    }
}
""";
        var comp = CreateCompilation([src, libSrc]);
        CompileAndVerify(comp, expectedOutput: "(1, 2, 3)(1, 2, 3)").VerifyDiagnostics();

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        CompileAndVerify(comp2, expectedOutput: "(1, 2, 3)(1, 2, 3)").VerifyDiagnostics();
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_Params_02()
    {
        var src = """
new object().M(1, 2, 3);
E.M(new object(), 1, 2, 3);
""";
        var libSrc = """
using System.Linq;
public static class E
{
    extension(object o)
    {
        public void M(params System.Collections.Generic.IEnumerable<int> i) { int[] i2 = i.ToArray(); System.Console.Write((i2[0], i2[1], i2[2])); }
    }
}
""";
        var comp = CreateCompilation([src, libSrc]);
        CompileAndVerify(comp, expectedOutput: "(1, 2, 3)(1, 2, 3)", symbolValidator: validate).VerifyDiagnostics();

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        CompileAndVerify(comp, expectedOutput: "(1, 2, 3)(1, 2, 3)").VerifyDiagnostics();

        static void validate(ModuleSymbol m)
        {
            var module = (PEModuleSymbol)m;
            var parameterSymbol = (PEParameterSymbol)m.GlobalNamespace.GetMember<MethodSymbol>("E.M").Parameters[1];
            Assert.True(module.Module.HasParamCollectionAttribute(parameterSymbol.Handle));
        }
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_Dynamic_01()
    {
        var src = """
public static class E
{
    extension(object o)
    {
        public void M(dynamic d) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.DynamicAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.M").Parameters[1].GetAttributes().ToStrings());
        }
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_Dynamic_02()
    {
        var src = """
public class C<T> { }

public static class E
{
    extension(C<dynamic> d)
    {
        public void M() { }
        public int Property => 42;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.DynamicAttribute({false, true})"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.M").Parameters[0].GetAttributes().ToStrings());

            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.DynamicAttribute({false, true})"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_Property").Parameters[0].GetAttributes().ToStrings());
        }
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_In_01()
    {
        var src = """
class C
{
    void M(in int i)
    {
        _ = i.P;
        _ = E.get_P(i);

        _ = i.P2;
        _ = E.get_P2(ref i);
    }
}
""";
        var libSrc = """
public static class E
{
    extension(in int i)
    {
        public int P => throw null!;
    }

    extension(ref int i)
    {
        public int P2 => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (8,13): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         _ = i.P2;
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(8, 13),
            // (9,26): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         _ = E.get_P2(ref i);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(9, 26)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        CompileAndVerify(libComp, symbolValidator: validate);

        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);

        static void validate(ModuleSymbol m)
        {
            var module = (PEModuleSymbol)m;
            var parameterSymbol = (PEParameterSymbol)m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P").Parameters[0];
            Assert.True(module.Module.HasIsReadOnlyAttribute(parameterSymbol.Handle));

            parameterSymbol = (PEParameterSymbol)m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P2").Parameters[0];
            Assert.False(module.Module.HasIsReadOnlyAttribute(parameterSymbol.Handle));
        }
    }

    [Fact]
    public void SynthesizedAttributeOnParameters_In_02()
    {
        var src = """
class C
{
    void M(in int i)
    {
        i.M();
        E.M(i);

        i.M2();
        E.M2(ref i);
    }
}
""";
        var libSrc = """
public static class E
{
    extension(in int i)
    {
        public void M() => throw null!;
    }

    extension(ref int i)
    {
        public void M2() => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (8,9): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         i.M2();
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(8, 9),
            // (9,18): error CS8329: Cannot use variable 'i' as a ref or out value because it is a readonly variable
            //         E.M2(ref i);
            Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(9, 18)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void SynthesizedAttributeOnReturn_Dynamic_01()
    {
        var src = """
new object().P.Dynamic();
E.get_P(new object()).Dynamic();
""";
        var libSrc = """
public static class E
{
    extension(object o)
    {
        public dynamic P => throw null!;
    }
}
""";
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.DynamicAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P").GetReturnTypeAttributes().ToStrings());
        }

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_PropertyAccess_02()
    {
        // nullability check on the receiver, un-annotated extension parameter
        var src = """
#nullable enable

object? oNull = null;
_ = oNull.P;

object? oNull2 = null;
E.get_P(oNull2);

object? oNotNull = new object();
_ = oNotNull.P;

E.get_P(oNotNull);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object o)
    {
        public int P { get => throw null!; }
    }
    public static void M(object here) { }
}
""";
        DiagnosticDescription[] expected = [
            // (4,5): warning CS8604: Possible null reference argument for parameter 'o' in 'extension(object)'.
            // _ = oNull.P;
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull").WithArguments("o", "extension(object)").WithLocation(4, 5),
            // (7,9): warning CS8604: Possible null reference argument for parameter 'o' in 'int E.get_P(object o)'.
            // E.get_P(oNull2);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "oNull2").WithArguments("o", "int E.get_P(object o)").WithLocation(7, 9)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_03()
    {
        // nullability check on the return value
        var src = """
#nullable enable

object o1 = object.P; // 1
object? o2 = object.P;

object o3 = E.get_P(); // 2
object? o4 = E.get_P();

object o5 = object.P2;
object? o6 = object.P2;

object o7 = E.get_P2();
object? o8 = E.get_P2();
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        public static object? P { get => throw null!; }
        public static object P2 { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
            // object o1 = object.P; // 1
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "object.P").WithLocation(3, 13),
            // (6,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
            // object o3 = E.get_P(); // 2
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "E.get_P()").WithLocation(6, 13)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_04()
    {
        // nullability check on the set value
        var src = """
#nullable enable

object.P = null;
object.P = new object();

E.set_P(null);
E.set_P(new object());

object.P2 = null; // 1
object.P2 = new object();

E.set_P2(null); // 2
E.set_P2(new object());
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        public static object? P { set => throw null!; }
        public static object P2 { set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (9,13): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object.P2 = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 13),
            // (12,10): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_P2(null); // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 10)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_05()
    {
        // nullability check on compound assignment
        var src = """
#nullable enable

object.P ??= null;
object.P ??= new object();

object.P2 ??= null; // 1
object.P2 ??= new object();

static class E
{
    extension(object)
    {
        public static object? P { get => throw null!; set => throw null!; }
        public static object P2 { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object.P2 ??= null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 15));
    }

    [Fact]
    public void Nullability_PropertyAccess_06()
    {
        // generic extension parameter, property read access
        var src = """
#nullable enable

object? oNull = null;
oNull.P.ToString();

object? oNotNull = new object();
oNotNull.P.ToString();

static class E
{
    extension<T>(T t)
    {
        public T P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // oNull.P.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull.P").WithLocation(4, 1));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var propertyAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "oNull.P");
        AssertEx.Equal("System.Object? E.extension<System.Object?>(System.Object?).P { get; }", model.GetSymbolInfo(propertyAccess1).Symbol.ToTestDisplayString(includeNonNullable: true));

        var propertyAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "oNotNull.P");
        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).P { get; }", model.GetSymbolInfo(propertyAccess2).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_PropertyAccess_07()
    {
        // generic extension parameter, instance member, property write access
        var src = """
#nullable enable

object? oNull = null;
oNull.P = null;

object? oNull2 = null;
oNull2.P = new object();

object? oNotNull = new object();
oNotNull.P = null; // 1

oNotNull.P = new object();

oNotNull?.P = null; // 2

static class E
{
    extension<T>(T t)
    {
        public T P { set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (10,14): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull.P = null; // 1
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 14),
            // (14,15): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // oNotNull?.P = null; // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(14, 15));
    }

    [Fact]
    public void Nullability_PropertyAccess_08()
    {
        // generic extension parameter, static member
        var src = """
#nullable enable

C<object>.P = new C<object?>(); // 1
C<object>.P = new C<object>();

C<object?>.P = new C<object?>();
C<object?>.P = new C<object>(); // 2

class C<T> { }

static class E
{
    extension<T>(T)
    {
        public static T P { set => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): warning CS8619: Nullability of reference types in value of type 'C<object?>' doesn't match target type 'C<object>'.
            // C<object>.P = new C<object?>(); // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new C<object?>()").WithArguments("C<object?>", "C<object>").WithLocation(3, 15),
            // (7,16): warning CS8619: Nullability of reference types in value of type 'C<object>' doesn't match target type 'C<object?>'.
            // C<object?>.P = new C<object>(); // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "new C<object>()").WithArguments("C<object>", "C<object?>").WithLocation(7, 16));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var propertyAccess = GetSyntaxes<MemberAccessExpressionSyntax>(tree, "C<object?>.P").First();
        AssertEx.Equal("C<System.Object?>! E.extension<C<System.Object?>!>(C<System.Object?>!).P { set; }", model.GetSymbolInfo(propertyAccess).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_PropertyAccess_09()
    {
        // notnull constraint
        var src = """
#nullable enable

object? oNull = null;
_ = oNull.P;

object? oNull2 = null;
_ = oNull2?.P;

object? oNotNull = new object();
_ = oNotNull.P;

static class E
{
    extension<T>(T t) where T : notnull
    {
        public T P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // _ = oNull.P;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull.P").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(4, 5));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        var propertyAccess1 = GetSyntax<MemberAccessExpressionSyntax>(tree, "oNull.P");
        AssertEx.Equal("System.Object? E.extension<System.Object?>(System.Object?).P { get; }", model.GetSymbolInfo(propertyAccess1).Symbol.ToTestDisplayString(includeNonNullable: true));

        var propertyAccess2 = GetSyntax<MemberAccessExpressionSyntax>(tree, "oNotNull.P");
        AssertEx.Equal("System.Object! E.extension<System.Object!>(System.Object!).P { get; }", model.GetSymbolInfo(propertyAccess2).Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void Nullability_PropertyAccess_10()
    {
        // notnull constraint, in tuple
        var src = """
#nullable enable

object? oNull = null;
_ = (1, oNull.P);

static class E
{
    extension<T>(T t) where T : notnull
    {
        public T P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,9): warning CS8714: The type 'object?' cannot be used as type parameter 'T' in the generic type or method 'E.extension<T>(T)'. Nullability of type argument 'object?' doesn't match 'notnull' constraint.
            // _ = (1, oNull.P);
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "oNull.P").WithArguments("E.extension<T>(T)", "T", "object?").WithLocation(4, 9));
    }

    [Fact]
    public void Nullability_PropertyAccess_11()
    {
        // implicit reference conversion on the receiver
        var src = """
#nullable enable

string? sNull = null;
_ = sNull.P;

string? sNotNull = "";
_ = sNotNull.P;

static class E
{
    extension(object? o)
    {
        public int P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_PropertyAccess_12()
    {
        // implicit reference conversion on the receiver
        var src = """
#nullable enable

string? sNull = null;
_ = sNull.P;

string? sNotNull = "";
_ = sNotNull.P;

static class E
{
    extension(object o)
    {
        public int P { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8604: Possible null reference argument for parameter 'o' in 'extension(object)'.
            // _ = sNull.P;
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "sNull").WithArguments("o", "extension(object)").WithLocation(4, 5));
    }

    [Fact]
    public void Nullability_PropertyAccess_13()
    {
        // `ref` extension parameter
        var src = """
#nullable enable

S<object?> s1 = default;
_ = s1.P;

S<object> s2 = default;
_ = s2.P; // 1

S<object?> s3 = default;
_ = s3.P2; // 2

S<object> s4 = default;
_ = s4.P2;

struct S<T> { }

static class E
{
    extension(ref S<object?> o)
    {
        public int P { get => throw null!; }
    }
    extension(ref S<object> o)
    {
        public int P2 { get => throw null!; }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,5): warning CS8620: Argument of type 'S<object>' cannot be used for parameter 'o' of type 'S<object?>' in 'extension(ref S<object?>)' due to differences in the nullability of reference types.
            // _ = s2.P; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s2").WithArguments("S<object>", "S<object?>", "o", "extension(ref S<object?>)").WithLocation(7, 5),
            // (10,5): warning CS8620: Argument of type 'S<object?>' cannot be used for parameter 'o' of type 'S<object>' in 'extension(ref S<object>)' due to differences in the nullability of reference types.
            // _ = s3.P2; // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s3").WithArguments("S<object?>", "S<object>", "o", "extension(ref S<object>)").WithLocation(10, 5));
    }

    [Fact]
    public void Nullability_PropertyAccess_14()
    {
        // `in` extension parameter
        var src = """
#nullable enable

S<object?> s1 = default;
_ = s1.P;

S<object> s2 = default;
_ = s2.P; // 1

S<object?> s3 = default;
_ = s3.P2; // 2

S<object> s4 = default;
_ = s4.P2;
""";
        var libSrc = """
#nullable enable

public struct S<T> { }

public static class E
{
    extension(in S<object?> o)
    {
        public int P { get => throw null!; }
    }
    extension(in S<object> o)
    {
        public int P2 { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (7,5): warning CS8620: Argument of type 'S<object>' cannot be used for parameter 'o' of type 'S<object?>' in 'extension(in S<object?>)' due to differences in the nullability of reference types.
            // _ = s2.P; // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s2").WithArguments("S<object>", "S<object?>", "o", "extension(in S<object?>)").WithLocation(7, 5),
            // (10,5): warning CS8620: Argument of type 'S<object?>' cannot be used for parameter 'o' of type 'S<object>' in 'extension(in S<object>)' due to differences in the nullability of reference types.
            // _ = s3.P2; // 2
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "s3").WithArguments("S<object?>", "S<object>", "o", "extension(in S<object>)").WithLocation(10, 5)
            ];

        var comp = CreateCompilation([src, libSrc]);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_15()
    {
        // NotNullIfNotNull
        var src = """
#nullable enable

object? oNull = null;
oNull.P.ToString(); // 1

object? oNull2 = null;
E.get_P(oNull2).ToString(); // 2

object oNotNull = new object();
oNotNull.P.ToString();

E.get_P(oNotNull).ToString();
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object? o)
    {
        [property: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(o))]
        public object? P { get => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // oNull.P.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNull.P").WithLocation(4, 1),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_P(oNull2).ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P(oNull2)").WithLocation(7, 1),

            // Tracked by https://github.com/dotnet/roslyn/issues/37238 : NotNullIfNotNull not yet supported on indexers. The last two warnings are spurious

            // (10,1): warning CS8602: Dereference of a possibly null reference.
            // oNotNull.P.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "oNotNull.P").WithLocation(10, 1),
            // (12,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_P(oNotNull).ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P(oNotNull)").WithLocation(12, 1)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);

        src = """
#nullable enable

object? oNull = null;
new C()[oNull].ToString();

object oNotNull = new object();
new C()[oNotNull].ToString();

class C
{
    [property: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(o))]
    public object? this[object? o] { get => throw null!; }
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        // Tracked by https://github.com/dotnet/roslyn/issues/37238 : NotNullIfNotNull not yet supported on indexers. 
        comp.VerifyEmitDiagnostics(
            // (4,1): warning CS8602: Dereference of a possibly null reference.
            // new C()[oNull].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new C()[oNull]").WithLocation(4, 1),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // new C()[oNotNull].ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "new C()[oNotNull]").WithLocation(7, 1));
    }

    [Fact]
    public void Nullability_PropertyAccess_16()
    {
        // NotNull
        var src = """
#nullable enable

object.P.ToString();
object.P = null;

E.get_P().ToString();
E.set_P(null);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        [property: System.Diagnostics.CodeAnalysis.NotNull]
        public static object? P { get => throw null!; set => throw null!; }

        public static object? P2 { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.NotNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P").GetReturnTypeAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").Parameters[0].GetAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P2").GetReturnTypeAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").Parameters[0].GetAttributes());
        }
    }

    [Fact]
    public void Nullability_PropertyAccess_17()
    {
        // MaybeNull
        var src = """
#nullable enable

object.P.ToString(); // 1
object.P = null; // 2
object.P = "";

E.get_P().ToString(); // 3
E.set_P(null); // 4
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        [property: System.Diagnostics.CodeAnalysis.MaybeNull]
        public static object P { get => throw null!; set => throw null!; }

        public static object P2 { get => throw null!; set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // object.P.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P").WithLocation(3, 1),
            // (4,12): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object.P = null; // 2
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 12),
            // (7,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_P().ToString(); // 3
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P()").WithLocation(7, 1),
            // (8,9): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_P(null); // 4
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 9)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.MaybeNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P").GetReturnTypeAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").Parameters[0].GetAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.get_P2").GetReturnTypeAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").GetReturnTypeAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").Parameters[0].GetAttributes());
        }
    }

    [Fact]
    public void Nullability_PropertyAccess_18()
    {
        // AllowNull
        var src = """
#nullable enable

object.P.ToString();
object.P = null;

E.get_P().ToString();
E.set_P(null);
""";
        var libSrc = """
public static class E
{
    extension(object)
    {
        [property: System.Diagnostics.CodeAnalysis.AllowNull]
        public static object P { get => throw null!; set => throw null!; }

        public static object P2 { get => throw null!; set => throw null!; }
    }
}
""";
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics();

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.AllowNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").Parameters[0].GetAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").GetReturnTypeAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").Parameters[0].GetAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").GetReturnTypeAttributes());
        }
    }

    [Fact]
    public void Nullability_PropertyAccess_19()
    {
        // DisallowNull
        var src = """
#nullable enable

object.P.ToString();
object.P = null;

E.get_P().ToString();
E.set_P(null);
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        [property: System.Diagnostics.CodeAnalysis.DisallowNull]
        public static object? P { get => throw null!; set => throw null!; }

        public static object? P2 { get => throw null!; set => throw null!; }
    }
}
""";
        DiagnosticDescription[] expected = [
            // (3,1): warning CS8602: Dereference of a possibly null reference.
            // object.P.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P").WithLocation(3, 1),
            // (4,12): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // object.P = null;
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 12),
            // (6,1): warning CS8602: Dereference of a possibly null reference.
            // E.get_P().ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P()").WithLocation(6, 1),
            // (7,9): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // E.set_P(null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 9)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);

        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped);

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Diagnostics.CodeAnalysis.DisallowNullAttribute"],
                m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").Parameters[0].GetAttributes().ToStrings());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P").GetReturnTypeAttributes());

            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").Parameters[0].GetAttributes());
            Assert.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("E.set_P2").GetReturnTypeAttributes());
        }
    }

    [Fact]
    public void Nullability_PropertyAccess_20()
    {
        // DoesNotReturn
        var src = """
#nullable enable

bool b = false;
object? o = null;

if (b)
{
    _ = object.P;
    o.ToString(); // incorrect
}

if (b)
{
    object.P = 0;
    o.ToString(); // incorrect
}

if (b)
{
    E.get_P();
    o.ToString();
}

if (b)
{
    E.set_P(0);
    o.ToString();
}
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        public static int P
        {
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            get => throw null!;
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            set => throw null!;
        }
    }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/50018 : DoesNotReturn not yet supported on indexers.
        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(9, 5),
            // (15,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 5));

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(9, 5),
            // (15,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 5));

        src = """
#nullable enable

bool b = false;
object? o = null;

if (b)
{
    _ = object.P;
    o.ToString(); // incorrect
}

if (b)
{
    object.P = 0;
    o.ToString(); // incorrect
}

public static class E
{
    extension(object)
    {
        public static int P
        {
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            get => throw null!;
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            set => throw null!;
        }
    }
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(9, 5),
            // (15,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // incorrect
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(15, 5));
    }

    [Fact]
    public void Nullability_PropertyAccess_21()
    {
        // NotNullWhen
        var src = """
#nullable enable

object? o = null;
if (o.P)
    o.ToString();
else
    o.ToString(); // 1

object? o2 = null;
if (E.get_P(o2))
    o2.ToString();
else
    o2.ToString(); // 2
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? o)
    {
        public bool P => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (7,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(7, 5),
            // (13,5): warning CS8602: Dereference of a possibly null reference.
            //     o2.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(13, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_22()
    {
        // MaybeNullWhen
        var src = """
#nullable enable

object o = new object();
if (o.P)
    o.ToString(); // 1
else
    o.ToString();

object o2 = new object();
if (E.get_P(o2))
    o2.ToString(); // 2
else
    o2.ToString();
""";
        var libSrc = """
#nullable enable

public static class E
{
    extension([System.Diagnostics.CodeAnalysis.MaybeNullWhen(true)] object? o)
    {
        public bool P => throw null!;
    }
}
""";
        DiagnosticDescription[] expected = [
            // (5,5): warning CS8602: Dereference of a possibly null reference.
            //     o.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(5, 5),
            // (11,5): warning CS8602: Dereference of a possibly null reference.
            //     o2.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o2").WithLocation(11, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_23()
    {
        // MemberNotNull
        var src = """
#nullable enable

if (object.P)
    object.P2.ToString(); // 1
else
    object.P2.ToString();

if (E.get_P())
    E.get_P2().ToString(); // 2
else
    E.get_P2().ToString();

""";
        var libSrc = """
#nullable enable

public static class E
{
    extension(object)
    {
        [System.Diagnostics.CodeAnalysis.MemberNotNull("P2")]
        public static bool P => throw null!;

        public static object? P2 => throw null!;
    }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/76130 : should we extend member post-conditions to work with extension members?
        DiagnosticDescription[] expected = [
            // (4,5): warning CS8602: Dereference of a possibly null reference.
            //     object.P2.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P2").WithLocation(4, 5),
            // (6,5): warning CS8602: Dereference of a possibly null reference.
            //     object.P2.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "object.P2").WithLocation(6, 5),
            // (9,5): warning CS8602: Dereference of a possibly null reference.
            //     E.get_P2().ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P2()").WithLocation(9, 5),
            // (11,5): warning CS8602: Dereference of a possibly null reference.
            //     E.get_P2().ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "E.get_P2()").WithLocation(11, 5)
            ];

        var comp = CreateCompilation([src, libSrc], targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(expected);

        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Net90);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()], targetFramework: TargetFramework.Net90);
        comp2.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void Nullability_PropertyAccess_24()
    {
        var src = """
#nullable enable

public static class E
{
    extension<T>(T t)
    {
        public int P { get { _ = t.P; return 42; } }
        public static int P2 { get { _ = T.P2; return 42; } }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (8,42): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
            //         public static int P2 { get { _ = T.P2; return 42; } }
            Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(8, 42));
    }

    [Fact]
    public void Nullability_NullableContext_01()
    {
        var src = """
#nullable enable

public static class E
{
    extension(object x)
    {
        public static void M1(object o) { }
        public static void M2(object o) { }
        public static void M3(object o) { }
        public static void M4(object o) { }
        public static void M5(object o) { }
        public static void M6(object o) { }
    }

    public static void N1(object? o) { }
    public static void N2(object? o) { }
    public static void N3(object? o) { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.NullableContextAttribute(1)", "System.Runtime.CompilerServices.NullableAttribute(0)"],
                m.GlobalNamespace.GetTypeMember("E").GetAttributes().ToStrings());
        }
    }

    [Fact]
    public void Nullability_NullableContext_02()
    {
        var src = """
#nullable enable

public static class E
{
    extension(object? x)
    {
        public static void M1(object? o) { }
        public static void M2(object? o) { }
        public static void M3(object? o) { }
        public static void M4(object? o) { }
        public static void M5(object? o) { }
        public static void M6(object? o) { }
    }

    public static void N1(object o) { }
    public static void N2(object o) { }
    public static void N3(object o) { }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

        static void validate(ModuleSymbol m)
        {
            AssertEx.SetEqual(m is SourceModuleSymbol ? new string[] { } : ["System.Runtime.CompilerServices.NullableContextAttribute(2)", "System.Runtime.CompilerServices.NullableAttribute(0)"],
                m.GlobalNamespace.GetTypeMember("E").GetAttributes().ToStrings());
        }
    }

    [Fact]
    public void Nullability_PropertyPattern_01()
    {
        // return type of property
        var src = """
#nullable enable

if (new object() is { P: var x })
    x.ToString(); // 1

if (new object() is { P2: var x2 })
    x2.ToString();

#nullable enable

public static class E
{
    extension(object o)
    {
        public object? P => throw null!;
        public object P2 => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8602: Dereference of a possibly null reference.
            //     x.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(4, 5));
    }

    [Fact]
    public void Nullability_PropertyPattern_02()
    {
        // nullability of extension parameter
        var src = """
#nullable enable

object? oNull = null;
_ = oNull is { P: 0 };

object? oNull2 = null;
_ = oNull2 is { P2: 0 };

#nullable enable

public static class E
{
    extension(object o)
    {
        public int P => throw null!;
    }
    extension(object? o)
    {
        public int P2 => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Nullability_PropertyPattern_03()
    {
        // NotNull
        var src = """
#nullable enable

if (new object() is { P: var x })
    x.ToString();

if (new object() is { P2: var x2 })
    x2.ToString(); // 1

if (new C() is { P3: var x3 })
    x3.ToString();

public static class E
{
    extension(object o)
    {
        [property: System.Diagnostics.CodeAnalysis.NotNull]
        public object? P => throw null!;

        public object? P2 => throw null!;
    }
}

class C
{
    [property: System.Diagnostics.CodeAnalysis.NotNull]
    public object? P3 => throw null!;
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/78828 : incorrect nullability analysis for property pattern with extension property (unexpected warning)
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (4,5): warning CS8602: Dereference of a possibly null reference.
            //     x.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(4, 5),
            // (7,5): warning CS8602: Dereference of a possibly null reference.
            //     x2.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(7, 5));
    }

    [Fact]
    public void WellKnownAttribute_Conditional()
    {
        var src = """
object.M();
E.M();
""";
        var libSrc = """
public static class E
{
    extension(object)
    {
        [System.Diagnostics.Conditional("CONDITION")]
        public static void M() { System.Console.Write("ran "); }
    }
}
""";

        var comp = CreateCompilation([src, libSrc]);
        CompileAndVerify(comp, expectedOutput: "").VerifyDiagnostics();

        var libComp = CreateCompilation(libSrc);
        var comp2 = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        CompileAndVerify(comp2, expectedOutput: "").VerifyDiagnostics();

        var src2 = """
#define CONDITION

object.M();
E.M();
""";

        comp = CreateCompilation([src2, libSrc]);
        CompileAndVerify(comp, expectedOutput: "ran ran").VerifyDiagnostics();

        libComp = CreateCompilation(libSrc);
        comp2 = CreateCompilation(src2, references: [libComp.EmitToImageReference()]);
        CompileAndVerify(comp2, expectedOutput: "ran ran").VerifyDiagnostics();
    }

    [Fact]
    public void WellKnownAttribute_ModuleInitializer()
    {
        var src = """
System.Console.Write("");

public static class E
{
    extension(object)
    {
        [System.Runtime.CompilerServices.ModuleInitializer] // 1
        public static void M() => throw null;

        public static int P
        {
            [System.Runtime.CompilerServices.ModuleInitializer] // 2
            get => throw null;
        }
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (7,10): error CS8813: A module initializer must be an ordinary member method
            //         [System.Runtime.CompilerServices.ModuleInitializer] // 1
            Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "System.Runtime.CompilerServices.ModuleInitializer").WithLocation(7, 10),
            // (12,14): error CS8813: A module initializer must be an ordinary member method
            //             [System.Runtime.CompilerServices.ModuleInitializer] // 2
            Diagnostic(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, "System.Runtime.CompilerServices.ModuleInitializer").WithLocation(12, 14));
    }

    [Fact]
    public void WellKnownAttribute_UnscopedRef_01()
    {
        var src = """
public static class E
{
    extension<T>(System.Span<T> span)
    {
        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref T GetFirst() => throw null;

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref T First => throw null;
    }
}
""";

        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (5,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
            //         [System.Diagnostics.CodeAnalysis.UnscopedRef]
            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "System.Diagnostics.CodeAnalysis.UnscopedRef").WithLocation(5, 10),
            // (8,10): error CS9101: UnscopedRefAttribute can only be applied to struct or virtual interface instance methods and properties, and cannot be applied to constructors or init-only members.
            //         [System.Diagnostics.CodeAnalysis.UnscopedRef]
            Diagnostic(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, "System.Diagnostics.CodeAnalysis.UnscopedRef").WithLocation(8, 10));
    }

    [Fact]
    public void Ambiguity_01()
    {
        var src = """
var x = object.M; // 1
x();

System.Action y = object.M; // 2

static class E1
{
    extension(object)
    {
        public static void M() { }
    }
}

static class E2
{
    extension(object)
    {
        public static int M => 0;
    }
}
""";

        var comp = CreateCompilation(src);
        // Tracked by https://github.com/dotnet/roslyn/issues/78830 : diagnostic quality, the diagnostic should describe what went wrong
        comp.VerifyEmitDiagnostics(
            // (1,9): error CS9286: 'object' does not contain a definition for 'M' and no accessible extension member 'M' for receiver of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // var x = object.M; // 1
            Diagnostic(ErrorCode.ERR_ExtensionResolutionFailed, "object.M").WithArguments("object", "M").WithLocation(1, 9),
            // (4,19): error CS9286: 'object' does not contain a definition for 'M' and no accessible extension member 'M' for receiver of type 'object' could be found (are you missing a using directive or an assembly reference?)
            // System.Action y = object.M; // 2
            Diagnostic(ErrorCode.ERR_ExtensionResolutionFailed, "object.M").WithArguments("object", "M").WithLocation(4, 19));

        src = """
var x = I.M; // binds to I1.M (method)
x();

System.Action y = I.M; // binds to I1.M (method)
y();

interface I1 { static void M() { System.Console.Write("I1.M() "); } }
interface I2 { static int M => 0;   }
interface I3 { static int M = 0;   }
interface I : I1, I2, I3 { }
""";

        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("I1.M() I1.M()"), verify: Verification.Skipped).VerifyDiagnostics();

        src = """
I i = new C();
var x = i.M; // binds to I1.M (method)
x();

System.Action y = i.M; // binds to I1.M (method)
y();

interface I1 { void M() { System.Console.Write("I1.M() "); } }
interface I2 { int M => 0;   }
interface I : I1, I2 { }

class C : I { }
""";

        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("I1.M() I1.M()"), verify: Verification.Skipped).VerifyDiagnostics();
    }

    [Fact]
    public void CS1943ERR_QueryTypeInferenceFailedSelectMany()
    {
        // ReportQueryInferenceFailedSelectMany
        var comp = CreateCompilation("""
using System;
using System.Collections.Generic;

class Test
{
    class TestClass
    { }

    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        TestClass tc = new TestClass();

        var x = from n in nums
                from s in tc // CS1943
                select n + s;
    }
}

static class E
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public IEnumerable<TResult> SelectMany<TCollection, TResult>(
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection,TResult> resultSelector)
            => throw null;
    }
}
""");

        comp.VerifyEmitDiagnostics(
            // (13,27): error CS1943: An expression of type 'Test.TestClass' is not allowed in a subsequent from clause in a query expression with source type 'int[]'.  Type inference failed in the call to 'SelectMany'.
            // tc
            Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailedSelectMany, "tc").WithArguments("Test.TestClass", "int[]", "SelectMany"));
    }

    [Fact]
    public void Foreach_Extension_01()
    {
        var src = """
class Program
{
    public static void M(Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct Span<T>
    {
    }
}

static class Ext 
{
    extension<T>(System.Span<T> f)
    {
        public Enumerator<T> GetEnumerator() => default;
    }

    public ref struct Enumerator<T>
    {
        public ref T Current => throw null;

        public bool MoveNext() => false;
    }
}

[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
        comp.VerifyEmitDiagnostics(
            // (5,26): error CS9189: foreach statement on an inline array of type 'Buffer4<int>' is not supported
            //         foreach(var s in x)
            Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(5, 26),
            // (20,25): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. Using the type defined in ''.
            //     extension<T>(System.Span<T> f)
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Span<T>").WithArguments("", "System.Span<T>", "System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Span<T>").WithLocation(20, 25)
            );
    }

    [Fact]
    public void DelegateCreation_01()
    {
        var src = """
string s;
_ = new System.Action(s.M);

string s2;
_ = new System.Action(s2.M2);

_ = new System.Action(string.M2);

static class E 
{
    extension(string s)
    {
        public void M() { }
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,23): error CS0165: Use of unassigned local variable 's'
            // _ = new System.Action(s.M);
            Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(2, 23),
            // (4,8): warning CS0168: The variable 's2' is declared but never used
            // string s2;
            Diagnostic(ErrorCode.WRN_UnreferencedVar, "s2").WithArguments("s2").WithLocation(4, 8),
            // (5,23): error CS0176: Member 'E.extension(string).M2()' cannot be accessed with an instance reference; qualify it with a type name instead
            // _ = new System.Action(s2.M2);
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "s2.M2").WithArguments("E.extension(string).M2()").WithLocation(5, 23));
    }

    [Fact]
    public void AsyncMethodBuilder_01()
    {
        var src = """
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

Console.Write(await object.M());

static class C
{
    extension(object)
    {
        [AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
        public static async MyTask<int> M() { await Task.Yield(); Console.Write("M "); return 3; }
    }
}

public class MyTask<T>
{
    private Action _continuation;
    private bool _isCompleted;
    internal  T _result;

    public Awaiter GetAwaiter() => new Awaiter(this);
    public T Result => _result;

    internal void Complete(T result)
    {
        _result = result;
        _isCompleted = true;
        _continuation?.Invoke();
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly MyTask<T> _task;
        internal Awaiter(MyTask<T> task) => _task = task;

        public bool IsCompleted => _task._isCompleted;
        public T GetResult() => _task._result;

        public void OnCompleted(Action cont) => HandleCompletion(cont);
        public void UnsafeOnCompleted(Action cont) => HandleCompletion(cont);

        private void HandleCompletion(Action cont)
        {
            if (_task._isCompleted) { cont(); return; }

            _task._continuation = cont;
        }
    }
}

public struct MyTaskMethodBuilder<T>
{
    private readonly MyTask<T> _task;
    private MyTaskMethodBuilder(MyTask<T> task) => _task = task;

    public static MyTaskMethodBuilder<T> Create() => new MyTaskMethodBuilder<T>(new MyTask<T>());
    public MyTask<T> Task => _task;
    public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine => sm.MoveNext();

    public void SetStateMachine(IAsyncStateMachine _) { }
    public void SetResult(T result) => _task.Complete(result);
    public void SetException(Exception e) => throw null;

    public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM: IAsyncStateMachine => a.OnCompleted(sm.MoveNext);
    public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM: IAsyncStateMachine => a.UnsafeOnCompleted(sm.MoveNext);
}

namespace System.Runtime.CompilerServices { class AsyncMethodBuilderAttribute : System.Attribute { public AsyncMethodBuilderAttribute(System.Type t) { } } } 
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "M 3").VerifyDiagnostics();
    }

    [Fact]
    public void PEMethodSymbol_GetUseSiteInfo()
    {
        // missing implementation method for M
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void Retargeting_01()
    {
        var libSrc = """
public static class E
{
    extension(object)
    {
        public static void M() { }
    }
}

namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : System.Attribute {}
}
""";
        var libComp = CreateCompilation(libSrc, targetFramework: TargetFramework.Mscorlib40);

        var src = """
object.M();
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Mscorlib46, references: [libComp.ToMetadataReference()]);
        comp.VerifyEmitDiagnostics();

        var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
        Assert.IsType<RetargetingNamedTypeSymbol>(extension);
        AssertExtensionDeclaration(extension.GetPublicSymbol());
    }

    [Theory]
    [InlineData("public")]
    [InlineData("assembly")]
    [InlineData("family")]
    public void PENamedTypeSymbol_01(string accessibility)
    {
        // Accessibility of extension marker is not private
        var ilSrc = $$"""
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method {{accessibility}} hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "int.M()");
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        Assert.Equal([], model.GetSymbolInfo(invocation).CandidateSymbols.ToTestDisplayStrings());
        Assert.Equal([], model.GetMemberGroup(invocation).ToTestDisplayStrings());
    }

    [Fact]
    public void PENamedTypeSymbol_02()
    {
        // Accessibility of extension marker is not private, instance extension method
        var ilSrc = $$"""
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig instance void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M ( int32 i ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
42.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,4): error CS1061: 'int' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            // 42.M();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int", "M").WithLocation(1, 4));
    }

    [Fact]
    public void PENamedTypeSymbol_03()
    {
        // Extension marker method is generic
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$'<T> ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_04()
    {
        // Extension marker method is not static
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_05()
    {
        // Extension marker doesn't return void
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static int32 '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
            IL_0000: ldc.i4.0
            IL_0001: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_06()
    {
        // Extension marker lacks its parameter
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' () cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_07()
    {
        // Extension marker has an extra parameter
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '', string s ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_08()
    {
        // No containing type
        var ilSrc = """
.class public auto ansi sealed specialname beforefieldinit '<>E__0'
    extends [mscorlib]System.Object
{
    .method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
        IL_0000: ret
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        var extension = comp.GlobalNamespace.GetTypeMember("<>E__0");
        Assert.True(extension.IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_09()
    {
        // Two extension markers
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method private hidebysig specialname static void '<Extension>$' ( string '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_10()
    {
        // Arity mismatch between skeleton and implementation
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M<T> () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_11()
    {
        // Accessibility mismatch between skeleton and implementation
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method assembly hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_12()
    {
        // parameter count mismatch between skeleton and implementation
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M (string s) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_13()
    {
        // return type mismatch between skeleton and implementation
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static int32 M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_14()
    {
        // parameter type mismatch, instance method
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig instance void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M ( object i ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
42.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,4): error CS0570: 'E.extension(int).M()' is not supported by the language
            // 42.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 4));
    }

    [Fact]
    public void PENamedTypeSymbol_15()
    {
        // parameter type mismatch, instance method
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig instance void M ( string s ) cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M ( int32 i, object s ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
42.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,4): error CS0570: 'E.extension(int).M(string)' is not supported by the language
            // 42.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M(string)").WithLocation(1, 4));
    }

    [Fact]
    public void PENamedTypeSymbol_16()
    {
        // constraint mismatch between skeleton and implementation
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M<T> () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M<class T> () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M<T>()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M<T>()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_17()
    {
        // implementation is not static
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig instance void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M()").WithLocation(1, 5));
    }

    [Fact]
    public void PENamedTypeSymbol_18()
    {
        // skeleton type is not sealed
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.False(comp.GetTypeByMetadataName("E").GetTypeMembers().Single().IsExtension);
    }

    [Theory]
    [InlineData("assembly")]
    [InlineData("family")]
    public void PENamedTypeSymbol_19(string accessibility)
    {
        // skeleton type is not public
        var ilSrc = $$"""
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested {{accessibility}} auto ansi specialname beforefieldinit sealed '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.False(comp.GetTypeByMetadataName("E").GetTypeMembers().Single().IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_20()
    {
        // skeleton type not sealed
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.False(comp.GetTypeByMetadataName("E").GetTypeMembers().Single().IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_21()
    {
        // skeleton type has a base that's not object
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi specialname beforefieldinit sealed '<>E__0'
		extends [mscorlib]System.String
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.False(comp.GetTypeByMetadataName("E").GetTypeMembers().Single().IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_22()
    {
        // skeleton type implements an interface
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi specialname beforefieldinit sealed '<>E__0'
		extends [mscorlib]System.Object
        implements I
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M () cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}

.class interface private auto ansi abstract beforefieldinit I
{
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.False(comp.GetTypeByMetadataName("E").GetTypeMembers().Single().IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_23()
    {
        // parameter type mismatch, static method
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
	.class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
		extends [mscorlib]System.Object
	{
		.method private hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
			IL_0000: ret
		}

		.method public hidebysig static void M ( string s ) cil managed 
		{
			IL_0000: ldnull
			IL_0001: throw
		}
	}

    .method public hidebysig static void M ( object s ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension(int).M(string)' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension(int).M(string)").WithLocation(1, 5));
    }

    [Fact]
    public void RefReadonlyExtensionParameterWithOneExplicitRefArgument()
    {
        var src = """
int i = 0;
42.Copy(ref i);
System.Console.Write(i);

static class E
{
    extension(ref readonly int i)
    {
        public void Copy(ref int j) { j = i; }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "42");
    }

    [Fact]
    public void Cref_01()
    {
        var src = """
/// <see cref="E.extension(int).M(string)"/>
/// <see cref="E.M(int, string)"/>
/// <see cref="E.extension(int).M"/>
/// <see cref="E.M"/>
static class E
{
    extension(int i)
    {
        /// <see cref="M(int, string)"/>
        /// <see cref="M(string)"/>
        /// <see cref="M"/>
        public void M(string s) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (10,24): warning CS1574: XML comment has cref attribute 'M(string)' that could not be resolved
            //         /// <see cref="M(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "M(string)").WithArguments("M(string)").WithLocation(10, 24));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="M:E.&lt;&gt;E__0.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>

""", e.GetDocumentationCommentXml());

        AssertEx.Equal("T:E.<>E__0", e.GetTypeMembers().Single().GetDocumentationCommentId());

        var mSkeleton = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single().GetMember("M");
        AssertEx.Equal("""
<member name="M:E.&lt;&gt;E__0.M(System.String)">
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="!:M(string)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>

""", mSkeleton.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).M(string), void E.<>E__0.M(System.String s))",
            "(E.M(int, string), void E.M(this System.Int32 i, System.String s))",
            "(E.extension(int).M, void E.<>E__0.M(System.String s))",
            "(E.M, void E.M(this System.Int32 i, System.String s))",
            "(M(int, string), void E.M(this System.Int32 i, System.String s))",
            "(M(string), null)",
            "(M, void E.M(this System.Int32 i, System.String s))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_02()
    {
        var src = """
/// <see cref="E.extension{T}(T).M{U}(U)"/>
static class E
{
    extension<T>(T t)
    {
        public void M<U>(U u) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0`1.M``1(``0)"/>
</member>

""", e.GetDocumentationCommentXml());

        AssertEx.Equal("T:E.<>E__0`1", e.GetTypeMembers().Single().GetDocumentationCommentId());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension{T}(T).M{U}(U), void E.<>E__0<T>.M<U>(U u))"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_03()
    {
        var src = """
/// <see cref="E.extension(ref int).M()"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(ref int).M()' that could not be resolved
            // /// <see cref="E.extension(ref int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(ref int).M()").WithArguments("extension(ref int).M()").WithLocation(1, 16));
    }

    [Fact]
    public void Cref_04()
    {
        var src = """
/// <see cref="E.extension(ref int).M()"/>
/// <see cref="E.extension(int).M()"/>
static class E
{
    extension(ref int i)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).M()' that could not be resolved
            // /// <see cref="E.extension(int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M()").WithArguments("extension(int).M()").WithLocation(2, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
    <see cref="!:E.extension(int).M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(ref int).M(), void E.<>E__0.M())",
            "(E.extension(int).M(), null)"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_05()
    {
        var src = """
/// <see cref="E.extension(int).M()"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
    extension(string s)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M(), void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_06()
    {
        var src = """
/// <see cref="E.extension(int).M()"/>
/// <see cref="E.extension(int).M"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
    extension(int)
    {
        public static void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M()'. Assuming 'E.extension(int).M()', but could have also matched other overloads including 'E.extension(int).M()'.
            // /// <see cref="E.extension(int).M()"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M()").WithArguments("E.extension(int).M()", "E.extension(int).M()", "E.extension(int).M()").WithLocation(1, 16),
            // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M'. Assuming 'E.extension(int).M()', but could have also matched other overloads including 'E.extension(int).M()'.
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M").WithArguments("E.extension(int).M", "E.extension(int).M()", "E.extension(int).M()").WithLocation(2, 16),
            // (11,28): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public static void M() => throw null!;
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(11, 28));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).M(), null)",
            "(E.extension(int).M, null)"],
            PrintXmlCrefSymbols(tree, model));

        var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
        var crefs = docComments.SelectMany(doc => doc.DescendantNodes().OfType<XmlCrefAttributeSyntax>()).ToArray();
        Assert.Equal(CandidateReason.OverloadResolutionFailure, model.GetSymbolInfo(crefs[0].Cref).CandidateReason);
        Assert.Equal(CandidateReason.Ambiguous, model.GetSymbolInfo(crefs[1].Cref).CandidateReason);
    }

    [Fact]
    public void Cref_08()
    {
        var src = """
/// <see cref="E.extension(int).M()"/>
static class E
{
    extension(int i, int j)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (4,22): error CS9285: An extension container can have only one receiver parameter
            //     extension(int i, int j)
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int j").WithLocation(4, 22));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M(), void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_09()
    {
        var src = """
/// <see cref="E.extension(int, int).M()"/>
static class E
{
    extension(int i, int j)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int, int).M()' that could not be resolved
            // /// <see cref="E.extension(int, int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int, int).M()").WithArguments("extension(int, int).M()").WithLocation(1, 16),
            // (4,22): error CS9285: An extension container can have only one receiver parameter
            //     extension(int i, int j)
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int j").WithLocation(4, 22));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int, int).M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int, int).M(), null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_10()
    {
        // Missing closing parens
        var src = """
/// <see cref="E.extension(.M()"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'E.extension(.M()'
            // /// <see cref="E.extension(.M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "E.extension(.M()").WithArguments("E.extension(.M()").WithLocation(1, 16),
            // (1,28): warning CS1658: ) expected. See also error CS1026.
            // /// <see cref="E.extension(.M()"/>
            Diagnostic(ErrorCode.WRN_ErrorOverride, ".").WithArguments(") expected", "1026").WithLocation(1, 28));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(.M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(.M(), null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_11()
    {
        // Missing extension parameter
        var src = """
/// <see cref="E.extension().M()"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension().M()' that could not be resolved
            // /// <see cref="E.extension().M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension().M()").WithArguments("extension().M()").WithLocation(1, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension().M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension().M(), null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_12()
    {
        // Two extension parameters
        var src = """
/// <see cref="E.extension(int, int).M()"/>
static class E
{
    extension(int i, int j)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int, int).M()' that could not be resolved
            // /// <see cref="E.extension(int, int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int, int).M()").WithArguments("extension(int, int).M()").WithLocation(1, 16),
            // (4,22): error CS9285: An extension container can have only one receiver parameter
            //     extension(int i, int j)
            Diagnostic(ErrorCode.ERR_ReceiverParameterOnlyOne, "int j").WithLocation(4, 22));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int, int).M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int, int).M(), null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_13()
    {
        var src = """
/// <see cref="E.extension(int).P"/>
static class E
{
    extension(int i)
    {
        public int P => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="P:E.&lt;&gt;E__0.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).P, System.Int32 E.<>E__0.P { get; })"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_14()
    {
        var src = """
/// <see cref="E.extension(int).P"/>
static class E
{
    extension(int i)
    {
        public int P => throw null!;
    }
    extension(string s)
    {
        public string P => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="P:E.&lt;&gt;E__0.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).P, System.Int32 E.<>E__0.P { get; })"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_15()
    {
        var src = """
/// <see cref="E.extension(string).P"/>
static class E
{
    extension(int i)
    {
        public int P => throw null!;
    }
    extension(string s)
    {
        public string P => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="P:E.&lt;&gt;E__1.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(string).P, System.String E.<>E__1.P { get; })"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_16()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_17()
    {
        var src = """
/// <see cref="E.extension(string).M"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(string).M' that could not be resolved
            // /// <see cref="E.extension(string).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(string).M").WithArguments("extension(string).M").WithLocation(1, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(string).M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(string).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_18()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
static class E
{
    extension(int i)
    {
        public void M<T>() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M``1"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, void E.<>E__0.M<T>())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_19()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
static class E
{
    extension(int i)
    {
        public void M<T>() => throw null;
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_20()
    {
        var src = """
/// <see cref="E.extension(int).M{U}"/>
/// <see cref="E.M{U}"/>
static class E
{
    extension(int i)
    {
        public void M<T>() => throw null;
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M``1"/>
    <see cref="M:E.M``1(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).M{U}, void E.<>E__0.M<U>())",
            "(E.M{U}, void E.M<U>(this System.Int32 i))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_21()
    {
        // Arity for extension in cref differs from that in declaration
        var src = """
/// <see cref="E.extension(int).M"/>
static class E
{
    extension<T>(int i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).M' that could not be resolved
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M").WithArguments("extension(int).M").WithLocation(1, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int).M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_22()
    {
        // Arity for extension in cref differs from that in declaration
        var src = """
/// <see cref="E.extension{T}(int).M"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension{T}(int).M' that could not be resolved
            // /// <see cref="E.extension{T}(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension{T}(int).M").WithArguments("extension{T}(int).M").WithLocation(1, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension&lt;T&gt;(int).M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension{T}(int).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_23()
    {
        var src = """
/// <see cref="E.extension{T}(int).M"/>
static class E
{
    extension<T>(int i)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0`1.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension{T}(int).M, void E.<>E__0<T>.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_24()
    {
        var src = """
/// <see cref="E.M"/>
static class E<T>
{
    public static void M() => throw null;
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'M' that could not be resolved
            // /// <see cref="E.M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.M").WithArguments("M").WithLocation(1, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E`1">
    <see cref="!:E.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_25()
    {
        // Type argument name differs from type parameter name
        var src = """
/// <see cref="E.extension{U}(U).M"/>
static class E
{
    extension<T>(T t)
    {
        public void M() => throw null;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0`1.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension{U}(U).M, void E.<>E__0<U>.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_26()
    {
        // __arglist
        var src = """
/// <see cref="E.extension(string).P"/>
static class E
{
    extension(__arglist)
    {
        public int P => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(string).P' that could not be resolved
            // /// <see cref="E.extension(string).P"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(string).P").WithArguments("extension(string).P").WithLocation(1, 16),
            // (4,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist)
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(4, 15));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(string).P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(string).P, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_27()
    {
        // member named "extension"
        var src = """
/// <see cref="E.extension(string)"/>
static class E
{
    public static void extension(string s) => throw null!;
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.extension(System.String)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(string), void E.extension(System.String s))"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_28()
    {
        var src = """
/// <see cref="E.extension(int)."/>
static class E
{
    extension(int i)
    {
        public int P => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'E.extension(int).'
            // /// <see cref="E.extension(int)."/>
            Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "E.extension(int).").WithArguments("E.extension(int).").WithLocation(1, 16),
            // (1,33): warning CS1658: Identifier expected. See also error CS1001.
            // /// <see cref="E.extension(int)."/>
            Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Identifier expected", "1001").WithLocation(1, 33));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int)."/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int)., null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_29()
    {
        var src = """
/// <see cref="E.extension(int).Nested"/>
static class E
{
    extension(int i)
    {
        public class Nested { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).Nested' that could not be resolved
            // /// <see cref="E.extension(int).Nested"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).Nested").WithArguments("extension(int).Nested").WithLocation(1, 16),
            // (6,22): error CS9282: This member is not allowed in an extension block
            //         public class Nested { }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Nested").WithLocation(6, 22));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int).Nested"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).Nested, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_30()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
static class E
{
    extension(int i)
    {
        void I.M() { }
    }
}

interface I
{
    void M();
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).M' that could not be resolved
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M").WithArguments("extension(int).M").WithLocation(1, 16),
            // (6,16): error CS0541: 'E.extension(int).M()': explicit interface declaration can only be declared in a class, record, struct or interface
            //         void I.M() { }
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("E.extension(int).M()").WithLocation(6, 16),
            // (6,16): error CS9282: This member is not allowed in an extension block
            //         void I.M() { }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "M").WithLocation(6, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(int).M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_31()
    {
        var src = """
/// <see cref="E.extension(missing).M"/>
static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(missing).M' that could not be resolved
            // /// <see cref="E.extension(missing).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(missing).M").WithArguments("extension(missing).M").WithLocation(1, 16),
            // (1,28): warning CS1580: Invalid type for parameter missing in XML comment cref attribute: 'E.extension(missing).M'
            // /// <see cref="E.extension(missing).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "missing").WithArguments("missing", "E.extension(missing).M").WithLocation(1, 28));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="!:E.extension(missing).M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(missing).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_32()
    {
        // nested type named "extension"
        var src = """
/// <see cref="E.extension"/>
/// <see cref="E.extension.M"/>
/// <see cref="E.extension.M()"/>
static class E
{
    class @extension
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="T:E.extension"/>
    <see cref="M:E.extension.M"/>
    <see cref="M:E.extension.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension, E.extension)",
            "(E.extension.M, void E.extension.M())",
            "(E.extension.M(), void E.extension.M())"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_33()
    {
        // nested type named "extension", error cases
        var src = """
/// <see cref="E.extension()"/>
/// <see cref="E.extension().M"/>
/// <see cref="E.extension(int).M"/>
static class E
{
    class @extension
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (2,16): warning CS1574: XML comment has cref attribute 'extension().M' that could not be resolved
            // /// <see cref="E.extension().M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension().M").WithArguments("extension().M").WithLocation(2, 16),
            // (3,16): warning CS1574: XML comment has cref attribute 'extension(int).M' that could not be resolved
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M").WithArguments("extension(int).M").WithLocation(3, 16));
    }

    [Fact]
    public void Cref_34()
    {
        // generic nested type named "extension"
        var src = """
/// <see cref="E.extension{T}"/>
/// <see cref="E.extension{T}.M"/>
static class E
{
    class @extension<T>
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="T:E.extension`1"/>
    <see cref="M:E.extension`1.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension{T}, E.extension<T>)",
            "(E.extension{T}.M, void E.extension<T>.M())"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_35()
    {
        // generic nested type named "extension", error cases
        var src = """
/// <see cref="E.extension{T}(int).M"/>
static class E
{
    class @extension<T>
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension{T}(int).M' that could not be resolved
            // /// <see cref="E.extension{T}(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension{T}(int).M").WithArguments("extension{T}(int).M").WithLocation(1, 16));
    }

    [Fact]
    public void Cref_36()
    {
        // can refer to method named "extension", but cannot refer to extension block
        var src = """
/// <see cref="E.extension()"/>
/// <see cref="E.extension(int)"/>
static class E
{
    public static void extension() { }
    public static void extension(int i) { }
}

/// <see cref="E2.extension()"/>
/// <see cref="E2.extension(int)"/>
static class E2
{
    extension(int)
    {
    }

    /// <see cref="extension()"/>
    /// <see cref="extension(int)"/>
    static void M() { }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (9,16): warning CS1574: XML comment has cref attribute 'extension()' that could not be resolved
            // /// <see cref="E2.extension()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E2.extension()").WithArguments("extension()").WithLocation(9, 16),
            // (10,16): warning CS1574: XML comment has cref attribute 'extension(int)' that could not be resolved
            // /// <see cref="E2.extension(int)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E2.extension(int)").WithArguments("extension(int)").WithLocation(10, 16),
            // (17,20): warning CS1574: XML comment has cref attribute 'extension()' that could not be resolved
            //     /// <see cref="extension()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension()").WithArguments("extension()").WithLocation(17, 20),
            // (18,20): warning CS1574: XML comment has cref attribute 'extension(int)' that could not be resolved
            //     /// <see cref="extension(int)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int)").WithArguments("extension(int)").WithLocation(18, 20));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(), void E.extension())",
            "(E.extension(int), void E.extension(System.Int32 i))",
            "(E2.extension(), null)",
            "(E2.extension(int), null)",
            "(extension(), null)",
            "(extension(int), null)"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_37()
    {
        // method named "extension"
        var src = """
/// <see cref="E.extension()"/>
/// <see cref="E.extension(int)"/>
static class E
{
    public static void extension() { }
    public static void extension(int i) { }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(), void E.extension())",
            "(E.extension(int), void E.extension(System.Int32 i))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_38()
    {
        // method named "extension", error case
        var src = """
/// <see cref="E.extension().M"/>
/// <see cref="E.extension(int).M"/>
static class E
{
    public static void extension() { }
    public static void extension(int i) { }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension().M' that could not be resolved
            // /// <see cref="E.extension().M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension().M").WithArguments("extension().M").WithLocation(1, 16),
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).M' that could not be resolved
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M").WithArguments("extension(int).M").WithLocation(2, 16));
    }

    [Fact]
    public void Cref_39()
    {
        // nested type named "extension"
        var src = """
/// <see cref="E.extension"/>
static class E
{
    class @extension
    {
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="T:E.extension"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension, E.extension)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_40()
    {
        // inaccessible due to file accessibility on type
        var src1 = """
/// <see cref="E.extension(object).M"/>
class C { }
""";
        var src2 = """
file static class E
{
    extension(object)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation([(src1, "file1"), (src2, "file2")], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // file1(1,16): warning CS1574: XML comment has cref attribute 'extension(object).M' that could not be resolved
            // /// <see cref="E.extension(object).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(object).M").WithArguments("extension(object).M").WithLocation(1, 16));

        var c = comp.GetMember<NamedTypeSymbol>("C");
        AssertEx.Equal("""
<member name="T:C">
    <see cref="!:E.extension(object).M"/>
</member>

""", c.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(object).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_41()
    {
        // inaccessible due to internal
        var src = """
/// <see cref="E.extension(object).M"/>
class C { }
""";
        var libSrc = """
public static class E
{
    extension(object)
    {
        internal static void M() { }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()],
            parseOptions: TestOptions.RegularPreviewWithDocumentationComments,
            options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

        comp.VerifyEmitDiagnostics();

        var c = comp.GetMember<NamedTypeSymbol>("C");
        AssertEx.Equal("""
<member name="T:C">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", c.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(object).M, void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_42()
    {
        // inaccessible due to private
        var src = """
/// <see cref="E.extension(object).M"/>
class C { }
""";
        var libSrc = """
public static class E
{
    extension(object)
    {
        private static void M() { }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.RegularPreviewWithDocumentationComments, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(object).M' that could not be resolved
            // /// <see cref="E.extension(object).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(object).M").WithArguments("extension(object).M").WithLocation(1, 16));

        var c = comp.GetMember<NamedTypeSymbol>("C");
        AssertEx.Equal("""
<member name="T:C">
    <see cref="!:E.extension(object).M"/>
</member>

""", c.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(object).M, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_43()
    {
        var src = """
/// <see cref="E.extension{int}(int).M"/>
static class E
{
    extension<T>(T t)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'E.extension{int}(int).M'
            // /// <see cref="E.extension{int}(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "E.extension{int}(int).M").WithArguments("E.extension{int}(int).M").WithLocation(1, 16),
            // (1,28): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
            // /// <see cref="E.extension{int}(int).M"/>
            Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081").WithLocation(1, 28));
    }

    [Fact]
    public void Cref_44()
    {
        var src = """
/// <see cref="extension(int).M"/>
extension(int)
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).M' that could not be resolved
            // /// <see cref="extension(int).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).M").WithArguments("extension(int).M").WithLocation(1, 16),
            // (2,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension(int)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(2, 1),
            // (4,24): warning CS1591: Missing XML comment for publicly visible type or member 'extension(int).M()'
            //     public static void M() { }
            Diagnostic(ErrorCode.WRN_MissingXMLComment, "M").WithArguments("extension(int).M()").WithLocation(4, 24));
    }

    [Fact]
    public void Cref_45()
    {
        var src = """
/// <see cref="E.extension(int).extension(string).M"/>
static class E
{
    extension(int)
    {
        extension(string)
        {
            public static void M() { }
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'E.extension(int).extension(string).M'
            // /// <see cref="E.extension(int).extension(string).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "E.extension(int).extension(string).M").WithArguments("E.extension(int).extension(string).M").WithLocation(1, 16),
            // (1,33): warning CS1658: An extension member syntax is disallowed in nested position within an extension member syntax. See also error CS9309.
            // /// <see cref="E.extension(int).extension(string).M"/>
            Diagnostic(ErrorCode.WRN_ErrorOverride, "extension(string).M").WithArguments("An extension member syntax is disallowed in nested position within an extension member syntax", "9309").WithLocation(1, 33),
            // (6,9): error CS9282: This member is not allowed in an extension block
            //         extension(string)
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(6, 9));
    }

    [Fact]
    public void Cref_46()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
/// <see cref="E.extension(int).M2"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
        public void M(int j) => throw null!;
        public void M2(int j) => throw null!;
        public void M2() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M'. Assuming 'E.extension(int).M()', but could have also matched other overloads including 'E.extension(int).M(int)'.
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M").WithArguments("E.extension(int).M", "E.extension(int).M()", "E.extension(int).M(int)").WithLocation(1, 16),
            // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M2'. Assuming 'E.extension(int).M2(int)', but could have also matched other overloads including 'E.extension(int).M2()'.
            // /// <see cref="E.extension(int).M2"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M2").WithArguments("E.extension(int).M2", "E.extension(int).M2(int)", "E.extension(int).M2()").WithLocation(2, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
    <see cref="M:E.&lt;&gt;E__0.M2(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, null)", "(E.extension(int).M2, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_47()
    {
        // Xml doc APIs on PE symbols
        var src = """
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var comp2 = CreateCompilation("", references: [comp.EmitToImageReference(documentation: new TestDocumentationProvider())]);

        var mSkeleton = comp2.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single().GetMember("M");
        Assert.Equal("M:E.<>E__0.M", mSkeleton.GetDocumentationCommentId());
        Assert.Equal("M:E.<>E__0.M", mSkeleton.GetDocumentationCommentXml());
    }

    private class TestDocumentationProvider : DocumentationProvider
    {
        protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            return documentationMemberID;
        }

        public override bool Equals(object obj) => (object)this == obj;

        public override int GetHashCode() => throw new NotImplementedException();
    }

    [Fact]
    public void Cref_48()
    {
        var libSrc = """
public static class E
{
    extension(int i)
    {
        public void M() => throw null!;
    }
}
""";
        var libComp = CreateCompilation(libSrc);

        var src = """
/// <see cref="E.extension(int).M"/>
class C
{
}
""";
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var c = comp.GetMember<NamedTypeSymbol>("C");
        AssertEx.Equal("""
<member name="T:C">
    <see cref="M:E.&lt;&gt;E__0.M"/>
</member>

""", c.GetDocumentationCommentXml());
    }

    [Fact]
    public void Cref_49()
    {
        var src = """
/// <see cref="E.extension(missing).M"/>
static class E
{
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(missing).M' that could not be resolved
            // /// <see cref="E.extension(missing).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(missing).M").WithArguments("extension(missing).M").WithLocation(1, 16),
            // (1,28): warning CS1580: Invalid type for parameter missing in XML comment cref attribute: 'E.extension(missing).M'
            // /// <see cref="E.extension(missing).M"/>
            Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "missing").WithArguments("missing", "E.extension(missing).M").WithLocation(1, 28));
    }

    [Fact]
    public void Cref_50()
    {
        var src = """
/// <see cref="E.extension(int).M"/>
/// <see cref="E.extension(int).M2"/>
static class E
{
    extension(int i)
    {
        public void M() => throw null!;
        public void M2(int j) => throw null!;
    }
    extension(int i)
    {
        public void M(int j) => throw null!;
        public void M2() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M'. Assuming 'E.extension(int).M()', but could have also matched other overloads including 'E.extension(int).M(int)'.
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M").WithArguments("E.extension(int).M", "E.extension(int).M()", "E.extension(int).M(int)").WithLocation(1, 16),
            // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'E.extension(int).M2'. Assuming 'E.extension(int).M2(int)', but could have also matched other overloads including 'E.extension(int).M2()'.
            // /// <see cref="E.extension(int).M2"/>
            Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "E.extension(int).M2").WithArguments("E.extension(int).M2", "E.extension(int).M2(int)", "E.extension(int).M2()").WithLocation(2, 16));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        AssertEx.Equal("""
<member name="T:E">
    <see cref="M:E.&lt;&gt;E__0.M"/>
    <see cref="M:E.&lt;&gt;E__0.M2(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).M, null)", "(E.extension(int).M2, null)"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_51()
    {
        var src = """
/// <see cref="E.extension(int).@M"/>
static class E
{
    extension(int)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal(["(E.extension(int).@M, void E.<>E__0.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_52()
    {
        // unqualified reference
        var src = """
/// <see cref="extension(int).Method"/>
/// <see cref="extension(int).Property"/>
static class E
{
    extension(int)
    {
        /// <see cref="extension(int).Method"/>
        /// <see cref="extension(int).Property"/>
        public static void M1() { }

        public static void Method() { }
        public static int Property => 42;
    }

    /// <see cref="extension(int).Method"/>
    /// <see cref="extension(int).Property"/>
    extension(object)
    {
    }

    /// <see cref="extension(int).M2"/>
    /// <see cref="extension(int).Property"/>
    public static void M2() { }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/78967 : cref, such unqualified references in CREF should work within context of enclosing static type
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).Method' that could not be resolved
            // /// <see cref="extension(int).Method"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Method").WithArguments("extension(int).Method").WithLocation(1, 16),
            // (2,16): warning CS1574: XML comment has cref attribute 'extension(int).Property' that could not be resolved
            // /// <see cref="extension(int).Property"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Property").WithArguments("extension(int).Property").WithLocation(2, 16),
            // (7,24): warning CS1574: XML comment has cref attribute 'extension(int).Method' that could not be resolved
            //         /// <see cref="extension(int).Method"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Method").WithArguments("extension(int).Method").WithLocation(7, 24),
            // (8,24): warning CS1574: XML comment has cref attribute 'extension(int).Property' that could not be resolved
            //         /// <see cref="extension(int).Property"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Property").WithArguments("extension(int).Property").WithLocation(8, 24),
            // (15,20): warning CS1574: XML comment has cref attribute 'extension(int).Method' that could not be resolved
            //     /// <see cref="extension(int).Method"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Method").WithArguments("extension(int).Method").WithLocation(15, 20),
            // (16,20): warning CS1574: XML comment has cref attribute 'extension(int).Property' that could not be resolved
            //     /// <see cref="extension(int).Property"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Property").WithArguments("extension(int).Property").WithLocation(16, 20),
            // (21,20): warning CS1574: XML comment has cref attribute 'extension(int).M2' that could not be resolved
            //     /// <see cref="extension(int).M2"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).M2").WithArguments("extension(int).M2").WithLocation(21, 20),
            // (22,20): warning CS1574: XML comment has cref attribute 'extension(int).Property' that could not be resolved
            //     /// <see cref="extension(int).Property"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).Property").WithArguments("extension(int).Property").WithLocation(22, 20));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension(int).Method, null)",
            "(extension(int).Property, null)",
            "(extension(int).Method, null)",
            "(extension(int).Property, null)",
            "(extension(int).Method, null)",
            "(extension(int).Property, null)",
            "(extension(int).M2, null)",
            "(extension(int).Property, null)"],
            PrintXmlCrefSymbols(tree, model));

        src = """
/// <see cref="Nested.Method"/>
static class E
{
    /// <see cref="Nested.Method"/>
    static class Nested
    {
        /// <see cref="Nested.Method"/>
        public static void M() { }

        public static void Method() { }
    }
}
""";
        comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Cref_53()
    {
        var src = """
static class E
{
    extension(int i)
    {
        /// <see cref="M2(string)"/>
        /// <see cref="M2"/>
        public void M(string s) => throw null!;
    }
    extension(int i)
    {
        public void M2(string s) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (5,24): warning CS1574: XML comment has cref attribute 'M2(string)' that could not be resolved
            //         /// <see cref="M2(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "M2(string)").WithArguments("M2(string)").WithLocation(5, 24));

        var mSkeleton = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().First().GetMember("M");
        AssertEx.Equal("""
<member name="M:E.&lt;&gt;E__0.M(System.String)">
    <see cref="!:M2(string)"/>
    <see cref="M:E.M2(System.Int32,System.String)"/>
</member>

""", mSkeleton.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(M2(string), null)",
            "(M2, void E.M2(this System.Int32 i, System.String s))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_54()
    {
        var libSrc = """
public static class E
{
    extension(int)
    {
        public static void M() { }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var libRef = libComp.EmitToImageReference();

        var src = """
/// <see cref="E.extension(int).M"/>
class C
{
}
""";
        var comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular13.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS8652: The feature 'extensions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "extension(int).M").WithArguments("extensions").WithLocation(1, 18));

        comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Cref_55()
    {
        var src = """
/// <see cref="E.M(string)"/>
static class E
{
    extension(int i)
    {
        public void M(string s) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'M(string)' that could not be resolved
            // /// <see cref="E.M(string)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.M(string)").WithArguments("M(string)").WithLocation(1, 16));
    }
}

