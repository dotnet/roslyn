﻿// Licensed to the .NET Foundation under one or more agreements.
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
        // Tracked by https://github.com/dotnet/roslyn/issues/78828 : nullability, should we extend member post-conditions to work with extension members?
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

    [Fact]
    public void PropertyAccess_CompoundAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        this.P1 += Program.Get1();
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1 += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  ldobj      ""S1""
  IL_000c:  call       ""int E.get_P1(S1)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  add
  IL_0017:  stloc.0
  IL_0018:  ldobj      ""S1""
  IL_001d:  ldloc.0
  IL_001e:  call       ""void E.set_P1(S1, int)""
  IL_0023:  nop
  IL_0024:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""S1""
  IL_0007:  call       ""int E.get_P1(S1)""
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  ldarg.0
  IL_0014:  ldobj      ""S1""
  IL_0019:  ldloc.0
  IL_001a:  call       ""void E.set_P1(S1, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_CompoundAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        this.P1 += Program.Get1();
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1 += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""int E.get_P1(" + refKind + @" S1)""
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  add
  IL_0012:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_0017:  nop
  IL_0018:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       21 (0x15)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.0
  IL_0003:  call       ""int E.get_P1(" + refKind + @" S1)""
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  add
  IL_000e:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1).P1 += 1;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS1510: A ref or out value must be an assignable variable
                    //         default(S1).P1 += 1;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(15, 9)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (15,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         default(S1).P1 += 1;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(15, 9),
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 += 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 += 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_CompoundAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program
{
    public static C1 F;

    static void Main()
    {
        F = new C1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1 += Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""int E.get_P1(C1)""
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  add
  IL_0012:  call       ""void E.set_P1(C1, int)""
  IL_0017:  nop
  IL_0018:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_CompoundAssignment_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return 0;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1 += Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f.P1 += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F.P1 += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (T V_0,
                T& V_1,
                T V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.2
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""int E.get_P1<T>(T)""
  IL_002a:  call       ""int Program.Get1()""
  IL_002f:  add
  IL_0030:  stloc.3
  IL_0031:  ldobj      ""T""
  IL_0036:  ldloc.3
  IL_0037:  call       ""void E.set_P1<T>(T, int)""
  IL_003c:  nop
  IL_003d:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  ldobj      ""T""
  IL_0008:  call       ""int E.get_P1<T>(T)""
  IL_000d:  call       ""int Program.Get1()""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldobj      ""T""
  IL_0019:  ldloc.0
  IL_001a:  call       ""void E.set_P1<T>(T, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_CompoundAssignment_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return 0;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f.P1 += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F.P1 += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       21 (0x15)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  call       ""int E.get_P1<T>(ref T)""
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  add
  IL_000e:  call       ""void E.set_P1<T>(ref T, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1 { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T).P1 += 1;
    }
}

namespace NS1
{
    static class E
    {
        extension<T>(in T x) where T : struct
        {
        }
    }
}

namespace NS2
{
    static class E
    {
        extension<T>(ref readonly T x) where T : struct
        {
        }
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (13,9): error CS1510: A ref or out value must be an assignable variable
            //         default(T).P1 += 1;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact]
    public void PropertyAccess_CompoundAssignment_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int P1
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
                return 0;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        await Test3<C1>();
        System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1 += Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f.P1 += Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F.P1 += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125:123123125:123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (T V_0,
                T& V_1,
                T V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.2
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""int E.get_P1<T>(T)""
  IL_002a:  call       ""int Program.Get1()""
  IL_002f:  add
  IL_0030:  stloc.3
  IL_0031:  ldobj      ""T""
  IL_0036:  ldloc.3
  IL_0037:  call       ""void E.set_P1<T>(T, int)""
  IL_003c:  nop
  IL_003d:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       26 (0x1a)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  dup
  IL_0008:  call       ""int E.get_P1<T>(T)""
  IL_000d:  call       ""int Program.Get1()""
  IL_0012:  add
  IL_0013:  call       ""void E.set_P1<T>(T, int)""
  IL_0018:  nop
  IL_0019:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PrefixIncrementAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        ++this.P1;
    }
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        ++F.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  ldobj      ""S1""
  IL_000c:  call       ""S2 E.get_P1(S1)""
  IL_0011:  call       ""S2 S2.op_Increment(S2)""
  IL_0016:  stloc.0
  IL_0017:  ldobj      ""S1""
  IL_001c:  ldloc.0
  IL_001d:  call       ""void E.set_P1(S1, S2)""
  IL_0022:  nop
  IL_0023:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""S1""
  IL_0007:  call       ""S2 E.get_P1(S1)""
  IL_000c:  call       ""S2 S2.op_Increment(S2)""
  IL_0011:  stloc.0
  IL_0012:  ldarg.0
  IL_0013:  ldobj      ""S1""
  IL_0018:  ldloc.0
  IL_0019:  call       ""void E.set_P1(S1, S2)""
  IL_001e:  nop
  IL_001f:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_PrefixIncrementAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        ++this.P1;
    }
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        ++F.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""S2 E.get_P1(" + refKind + @" S1)""
  IL_000c:  call       ""S2 S2.op_Increment(S2)""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  call       ""void E.set_P1(" + refKind + @" S1, S2)""
  IL_0018:  nop
  IL_0019:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""S2 E.get_P1(" + refKind + @" S1)""
  IL_0007:  call       ""S2 S2.op_Increment(S2)""
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""void E.set_P1(" + refKind + @" S1, S2)""
  IL_0014:  nop
  IL_0015:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        ++default(S1).P1;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (15,11): error CS1510: A ref or out value must be an assignable variable
                    //         ++default(S1).P1;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(15, 11)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (15,11): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         ++default(S1).P1;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(15, 11),
                    // (15,11): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         ++default(S1).P1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 11)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (15,11): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         ++default(S1).P1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 11)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_PrefixIncrementAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return x;
    }
}

class Program
{
    public static C1 F = new C1 { F1 = 123 };

    static void Main()
    {
        Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        ++F.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""S2 E.get_P1(C1)""
  IL_000c:  call       ""S2 S2.op_Increment(S2)""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  call       ""void E.set_P1(C1, S2)""
  IL_0018:  nop
  IL_0019:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PrefixIncrementAssignment_04()
    {
        var src = """
static class E
{
    extension<T>(T x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test1(ref F);
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test1<T>(ref T f)
    {
        ++f.P1;
    }

    static void Test2<T>(ref T f) where T : struct
    {
        ++f.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                S2 V_2,
                T V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""S2 E.get_P1<T>(T)""
  IL_002a:  call       ""S2 S2.op_Increment(S2)""
  IL_002f:  stloc.2
  IL_0030:  ldobj      ""T""
  IL_0035:  ldloc.2
  IL_0036:  call       ""void E.set_P1<T>(T, S2)""
  IL_003b:  nop
  IL_003c:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  ldobj      ""T""
  IL_0008:  call       ""S2 E.get_P1<T>(T)""
  IL_000d:  call       ""S2 S2.op_Increment(S2)""
  IL_0012:  stloc.0
  IL_0013:  ldobj      ""T""
  IL_0018:  ldloc.0
  IL_0019:  call       ""void E.set_P1<T>(T, S2)""
  IL_001e:  nop
  IL_001f:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PrefixIncrementAssignment_05()
    {
        var src = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test2<T>(ref T f) where T : struct
    {
        ++f.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  call       ""S2 E.get_P1<T>(ref T)""
  IL_0008:  call       ""S2 S2.op_Increment(S2)""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""void E.set_P1<T>(ref T, S2)""
  IL_0014:  nop
  IL_0015:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1 { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        ++default(T).P1;
    }
}

namespace NS1
{
    static class E
    {
        extension<T>(in T x) where T : struct
        {
        }
    }
}

namespace NS2
{
    static class E
    {
        extension<T>(ref readonly T x) where T : struct
        {
        }
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (13,11): error CS1510: A ref or out value must be an assignable variable
            //         ++default(T).P1;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(13, 11),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact]
    public void PropertyAccess_PrefixIncrementAssignment_06()
    {
        var src = """
static class E
{
    extension<T>(T x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return default;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return x;
    }
}

class Program
{
    public static C1 F;

    static void Main()
    {
        F = new C1 { F1 = 123 };
        Test1(ref F);
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new C1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test1<T>(ref T f)
    {
        ++f.P1;
    }

    static void Test2<T>(ref T f) where T : class
    {
        ++f.P1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125:123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                S2 V_2,
                T V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""S2 E.get_P1<T>(T)""
  IL_002a:  call       ""S2 S2.op_Increment(S2)""
  IL_002f:  stloc.2
  IL_0030:  ldobj      ""T""
  IL_0035:  ldloc.2
  IL_0036:  call       ""void E.set_P1<T>(T, S2)""
  IL_003b:  nop
  IL_003c:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  dup
  IL_0008:  call       ""S2 E.get_P1<T>(T)""
  IL_000d:  call       ""S2 S2.op_Increment(S2)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  call       ""void E.set_P1<T>(T, S2)""
  IL_0019:  nop
  IL_001a:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PostfixIncrementAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        this.P1++;
    }
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  ldobj      ""S1""
  IL_000c:  call       ""S2 E.get_P1(S1)""
  IL_0011:  call       ""S2 S2.op_Increment(S2)""
  IL_0016:  stloc.0
  IL_0017:  ldobj      ""S1""
  IL_001c:  ldloc.0
  IL_001d:  call       ""void E.set_P1(S1, S2)""
  IL_0022:  nop
  IL_0023:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""S1""
  IL_0007:  call       ""S2 E.get_P1(S1)""
  IL_000c:  call       ""S2 S2.op_Increment(S2)""
  IL_0011:  stloc.0
  IL_0012:  ldarg.0
  IL_0013:  ldobj      ""S1""
  IL_0018:  ldloc.0
  IL_0019:  call       ""void E.set_P1(S1, S2)""
  IL_001e:  nop
  IL_001f:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_PostfixIncrementAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        this.P1++;
    }
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""S2 E.get_P1(" + refKind + @" S1)""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  call       ""S2 S2.op_Increment(S2)""
  IL_0013:  call       ""void E.set_P1(" + refKind + @" S1, S2)""
  IL_0018:  nop
  IL_0019:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""S2 E.get_P1(" + refKind + @" S1)""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  ldloc.0
  IL_000a:  call       ""S2 S2.op_Increment(S2)""
  IL_000f:  call       ""void E.set_P1(" + refKind + @" S1, S2)""
  IL_0014:  nop
  IL_0015:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1).P1++;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS1510: A ref or out value must be an assignable variable
                    //         default(S1).P1++;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(15, 9)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (15,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         default(S1).P1++;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(15, 9),
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1++;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1++;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_PostfixIncrementAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return default;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return x;
    }
}

class Program
{
    public static C1 F = new C1 { F1 = 123 };

    static void Main()
    {
        Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  dup
  IL_0007:  call       ""S2 E.get_P1(C1)""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  call       ""S2 S2.op_Increment(S2)""
  IL_0013:  call       ""void E.set_P1(C1, S2)""
  IL_0018:  nop
  IL_0019:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PostfixIncrementAssignment_04()
    {
        var src = """
static class E
{
    extension<T>(T x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test1(ref F);
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1++;
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                S2 V_2,
                T V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""S2 E.get_P1<T>(T)""
  IL_002a:  call       ""S2 S2.op_Increment(S2)""
  IL_002f:  stloc.2
  IL_0030:  ldobj      ""T""
  IL_0035:  ldloc.2
  IL_0036:  call       ""void E.set_P1<T>(T, S2)""
  IL_003b:  nop
  IL_003c:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  ldobj      ""T""
  IL_0008:  call       ""S2 E.get_P1<T>(T)""
  IL_000d:  call       ""S2 S2.op_Increment(S2)""
  IL_0012:  stloc.0
  IL_0013:  ldobj      ""T""
  IL_0018:  ldloc.0
  IL_0019:  call       ""void E.set_P1<T>(T, S2)""
  IL_001e:  nop
  IL_001f:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_PostfixIncrementAssignment_05()
    {
        var src = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program.F.F1++;
                return default;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F.F1++;
        return x;
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  call       ""S2 E.get_P1<T>(ref T)""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  call       ""S2 S2.op_Increment(S2)""
  IL_000f:  call       ""void E.set_P1<T>(ref T, S2)""
  IL_0014:  nop
  IL_0015:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1 { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T).P1++;
    }
}

namespace NS1
{
    static class E
    {
        extension<T>(in T x) where T : struct
        {
        }
    }
}

namespace NS2
{
    static class E
    {
        extension<T>(ref readonly T x) where T : struct
        {
        }
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (13,9): error CS1510: A ref or out value must be an assignable variable
            //         default(T).P1++;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact]
    public void PropertyAccess_PostfixIncrementAssignment_06()
    {
        var src = """
static class E
{
    extension<T>(T x)
    {
        public S2 P1
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return default;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

struct S2
{
    public static S2 operator ++(S2 x)
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return x;
    }
}

class Program
{
    public static C1 F;

    static void Main()
    {
        F = new C1 { F1 = 123 };
        Test1(ref F);
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new C1 { F1 = 123 };
        Test2(ref F);
        System.Console.Write(F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1++;
    }

    static void Test2<T>(ref T f) where T : class
    {
        f.P1++;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125:123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                S2 V_2,
                T V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  ldobj      ""T""
  IL_0025:  call       ""S2 E.get_P1<T>(T)""
  IL_002a:  call       ""S2 S2.op_Increment(S2)""
  IL_002f:  stloc.2
  IL_0030:  ldobj      ""T""
  IL_0035:  ldloc.2
  IL_0036:  call       ""void E.set_P1<T>(T, S2)""
  IL_003b:  nop
  IL_003c:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (S2 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  dup
  IL_0008:  call       ""S2 E.get_P1<T>(T)""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""S2 S2.op_Increment(S2)""
  IL_0014:  call       ""void E.set_P1<T>(T, S2)""
  IL_0019:  nop
  IL_001a:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_ConditionalAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public object P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }

        public int? P2
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test1()
    {
        this.P1 ??= Program.Get1();
    }

    public void Test2()
    {
        this.P2 ??= Program.Get1();
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test1();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test1();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        Test2();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test2();
        System.Console.Write(F.F1);
    }

    static void Test1()
    {
        F.P1 ??= Get1();
    }

    static void Test2()
    {
        F.P2 ??= Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125:123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (S1& V_0,
                object V_1,
                object V_2)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  call       ""object E.get_P1(S1)""
  IL_0012:  brtrue.s   IL_002e
  IL_0014:  call       ""int Program.Get1()""
  IL_0019:  box        ""int""
  IL_001e:  stloc.1
  IL_001f:  ldloc.0
  IL_0020:  ldobj      ""S1""
  IL_0025:  ldloc.1
  IL_0026:  dup
  IL_0027:  stloc.2
  IL_0028:  call       ""void E.set_P1(S1, object)""
  IL_002d:  nop
  IL_002e:  ret
}
");

        verifier.VerifyIL("S1.Test1",
@"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (object V_0,
                object V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""S1""
  IL_0007:  call       ""object E.get_P1(S1)""
  IL_000c:  brtrue.s   IL_0028
  IL_000e:  call       ""int Program.Get1()""
  IL_0013:  box        ""int""
  IL_0018:  stloc.0
  IL_0019:  ldarg.0
  IL_001a:  ldobj      ""S1""
  IL_001f:  ldloc.0
  IL_0020:  dup
  IL_0021:  stloc.1
  IL_0022:  call       ""void E.set_P1(S1, object)""
  IL_0027:  nop
  IL_0028:  ret
}
");

        verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       66 (0x42)
  .maxstack  3
  .locals init (S1& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  call       ""int? E.get_P2(S1)""
  IL_0012:  stloc.1
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""int int?.GetValueOrDefault()""
  IL_001a:  stloc.2
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       ""bool int?.HasValue.get""
  IL_0022:  brtrue.s   IL_0041
  IL_0024:  call       ""int Program.Get1()""
  IL_0029:  stloc.2
  IL_002a:  ldloc.0
  IL_002b:  ldobj      ""S1""
  IL_0030:  ldloca.s   V_3
  IL_0032:  ldloc.2
  IL_0033:  call       ""int?..ctor(int)""
  IL_0038:  ldloc.3
  IL_0039:  call       ""void E.set_P2(S1, int?)""
  IL_003e:  nop
  IL_003f:  br.s       IL_0041
  IL_0041:  ret
}
");

        verifier.VerifyIL("S1.Test2",
@"
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (int? V_0,
                int V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""S1""
  IL_0007:  call       ""int? E.get_P2(S1)""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int int?.GetValueOrDefault()""
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""bool int?.HasValue.get""
  IL_001c:  brtrue.s   IL_003b
  IL_001e:  call       ""int Program.Get1()""
  IL_0023:  stloc.1
  IL_0024:  ldarg.0
  IL_0025:  ldobj      ""S1""
  IL_002a:  ldloca.s   V_2
  IL_002c:  ldloc.1
  IL_002d:  call       ""int?..ctor(int)""
  IL_0032:  ldloc.2
  IL_0033:  call       ""void E.set_P2(S1, int?)""
  IL_0038:  nop
  IL_0039:  br.s       IL_003b
  IL_003b:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_ConditionalAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public object P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
        public int? P2
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test1()
    {
        this.P1 ??= Program.Get1();
    }

    public void Test2()
    {
        this.P2 ??= Program.Get1();
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test1();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test1();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        Test2();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test2();
        System.Console.Write(F.F1);
    }

    static void Test1()
    {
        F.P1 ??= Get1();
    }

    static void Test2()
    {
        F.P2 ??= Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125:123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (S1& V_0,
            object V_1)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""object E.get_P1(" + refKind + @" S1)""
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  ldloc.0
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  box        ""int""
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  call       ""void E.set_P1(" + refKind + @" S1, object)""
  IL_0021:  nop
  IL_0022:  ret
}
");

        verifier.VerifyIL("S1.Test1",
@"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""object E.get_P1(" + refKind + @" S1)""
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  ldarg.0
  IL_000a:  call       ""int Program.Get1()""
  IL_000f:  box        ""int""
  IL_0014:  dup
  IL_0015:  stloc.0
  IL_0016:  call       ""void E.set_P1(" + refKind + @" S1, object)""
  IL_001b:  nop
  IL_001c:  ret
}
");

        verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (S1& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int? E.get_P2(" + refKind + @" S1)""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""int int?.GetValueOrDefault()""
  IL_0015:  stloc.2
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       ""bool int?.HasValue.get""
  IL_001d:  brtrue.s   IL_0037
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldloc.0
  IL_0026:  ldloca.s   V_3
  IL_0028:  ldloc.2
  IL_0029:  call       ""int?..ctor(int)""
  IL_002e:  ldloc.3
  IL_002f:  call       ""void E.set_P2(" + refKind + @" S1, int?)""
  IL_0034:  nop
  IL_0035:  br.s       IL_0037
  IL_0037:  ret
}
");

        verifier.VerifyIL("S1.Test2",
@"
{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (int? V_0,
            int V_1,
            int? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int? E.get_P2(" + refKind + @" S1)""
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""int int?.GetValueOrDefault()""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_0031
  IL_0019:  call       ""int Program.Get1()""
  IL_001e:  stloc.1
  IL_001f:  ldarg.0
  IL_0020:  ldloca.s   V_2
  IL_0022:  ldloc.1
  IL_0023:  call       ""int?..ctor(int)""
  IL_0028:  ldloc.2
  IL_0029:  call       ""void E.set_P2(" + refKind + @" S1, int?)""
  IL_002e:  nop
  IL_002f:  br.s       IL_0031
  IL_0031:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public object P1 { get => 0; set {} }
        public int? P2 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test1()
    {
        default(S1).P1 ??= 1;
    }
    static void Test2()
    {
        default(S1).P2 ??= 1;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (16,9): error CS1510: A ref or out value must be an assignable variable
                    //         default(S1).P1 ??= 1;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(16, 9),
                    // (20,9): error CS1510: A ref or out value must be an assignable variable
                    //         default(S1).P2 ??= 1;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(20, 9)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (16,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         default(S1).P1 ??= 1;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(16, 9),
                    // (16,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 ??= 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(16, 9),
                    // (20,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         default(S1).P2 ??= 1;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(20, 9),
                    // (20,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P2 ??= 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P2").WithLocation(20, 9)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (16,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 ??= 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(16, 9),
                    // (20,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P2 ??= 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P2").WithLocation(20, 9)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_ConditionalAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public object P1
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
        public int? P2
        {
            get
            {
                System.Console.Write(x.F1);
                Program.F = new C1 { F1 = Program.F.F1 + 1 };
                return null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program
{
    public static C1 F;

    static void Main()
    {
        F = new C1 { F1 = 123 };
        Test1();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new C1 { F1 = 123 };
        Test2();
        System.Console.Write(F.F1);
    }

    static void Test1()
    {
        F.P1 ??= Get1();
    }

    static void Test2()
    {
        F.P2 ??= Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125:123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (C1 V_0,
                object V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""object E.get_P1(C1)""
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  ldloc.0
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  box        ""int""
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  call       ""void E.set_P1(C1, object)""
  IL_0021:  nop
  IL_0022:  ret
}
");

        verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (C1 V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int? E.get_P2(C1)""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""int int?.GetValueOrDefault()""
  IL_0015:  stloc.2
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       ""bool int?.HasValue.get""
  IL_001d:  brtrue.s   IL_0037
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldloc.0
  IL_0026:  ldloca.s   V_3
  IL_0028:  ldloc.2
  IL_0029:  call       ""int?..ctor(int)""
  IL_002e:  ldloc.3
  IL_002f:  call       ""void E.set_P2(C1, int?)""
  IL_0034:  nop
  IL_0035:  br.s       IL_0037
  IL_0037:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_ConditionalAssignment_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public object P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
        public int? P2
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}


class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test11(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test12(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test13<S1>();
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test21(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test22(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test23<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test11<T>(ref T f)
    {
        f.P1 ??= Get1();
    }

    static void Test21<T>(ref T f)
    {
        f.P2 ??= Get1();
    }

    static void Test12<T>(ref T f) where T : struct
    {
        f.P1 ??= Get1();
    }

    static void Test22<T>(ref T f) where T : struct
    {
        f.P2 ??= Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test13<T>()
    {
        Program<T>.F.P1 ??= await Get1Async();
    }

    static async Task Test23<T>()
    {
        Program<T>.F.P2 ??= await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125:123125125:123125125:123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test11<T>(ref T)",
@"
{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                T V_3,
                object V_4,
                object V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.2
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.1
  IL_001a:  ldloca.s   V_1
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.2
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  ldobj      ""T""
  IL_0026:  call       ""object E.get_P1<T>(T)""
  IL_002b:  brtrue.s   IL_004a
  IL_002d:  call       ""int Program.Get1()""
  IL_0032:  box        ""int""
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.0
  IL_003a:  ldobj      ""T""
  IL_003f:  ldloc.s    V_4
  IL_0041:  dup
  IL_0042:  stloc.s    V_5
  IL_0044:  call       ""void E.set_P1<T>(T, object)""
  IL_0049:  nop
  IL_004a:  ret
}
");

        verifier.VerifyIL("Program.Test12<T>(ref T)",
@"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (T& V_0,
                object V_1,
                object V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldobj      ""T""
  IL_0009:  call       ""object E.get_P1<T>(T)""
  IL_000e:  brtrue.s   IL_002a
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  box        ""int""
  IL_001a:  stloc.1
  IL_001b:  ldloc.0
  IL_001c:  ldobj      ""T""
  IL_0021:  ldloc.1
  IL_0022:  dup
  IL_0023:  stloc.2
  IL_0024:  call       ""void E.set_P1<T>(T, object)""
  IL_0029:  nop
  IL_002a:  ret
}
");

        verifier.VerifyIL("Program.Test21<T>(ref T)",
@"
{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                int? V_3,
                int V_4,
                T V_5,
                int? V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_5
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_5
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  ldobj      ""T""
  IL_0027:  call       ""int? E.get_P2<T>(T)""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_3
  IL_002f:  call       ""int int?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloca.s   V_3
  IL_0038:  call       ""bool int?.HasValue.get""
  IL_003d:  brtrue.s   IL_005f
  IL_003f:  call       ""int Program.Get1()""
  IL_0044:  stloc.s    V_4
  IL_0046:  ldloc.0
  IL_0047:  ldobj      ""T""
  IL_004c:  ldloca.s   V_6
  IL_004e:  ldloc.s    V_4
  IL_0050:  call       ""int?..ctor(int)""
  IL_0055:  ldloc.s    V_6
  IL_0057:  call       ""void E.set_P2<T>(T, int?)""
  IL_005c:  nop
  IL_005d:  br.s       IL_005f
  IL_005f:  ret
}
");

        verifier.VerifyIL("Program.Test22<T>(ref T)",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldobj      ""T""
  IL_0009:  call       ""int? E.get_P2<T>(T)""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""int int?.GetValueOrDefault()""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""bool int?.HasValue.get""
  IL_001e:  brtrue.s   IL_003d
  IL_0020:  call       ""int Program.Get1()""
  IL_0025:  stloc.2
  IL_0026:  ldloc.0
  IL_0027:  ldobj      ""T""
  IL_002c:  ldloca.s   V_3
  IL_002e:  ldloc.2
  IL_002f:  call       ""int?..ctor(int)""
  IL_0034:  ldloc.3
  IL_0035:  call       ""void E.set_P2<T>(T, int?)""
  IL_003a:  nop
  IL_003b:  br.s       IL_003d
  IL_003d:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_ConditionalAssignment_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public object P1
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
        public int? P2
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                Program<S1>.F.F1++;
                return null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test12(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test13<S1>();
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test22(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test23<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test12<T>(ref T f) where T : struct
    {
        f.P1 ??= Get1();
    }

    static void Test22<T>(ref T f) where T : struct
    {
        f.P2 ??= Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test13<T>() where T : struct
    {
        Program<T>.F.P1 ??= await Get1Async();
    }

    static async Task Test23<T>() where T : struct
    {
        Program<T>.F.P2 ??= await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125:123125125:123125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test12<T>(ref T)",
@"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (T& V_0,
            object V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""object E.get_P1<T>(ref T)""
  IL_0009:  brtrue.s   IL_001e
  IL_000b:  ldloc.0
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  box        ""int""
  IL_0016:  dup
  IL_0017:  stloc.1
  IL_0018:  call       ""void E.set_P1<T>(ref T, object)""
  IL_001d:  nop
  IL_001e:  ret
}
");

        verifier.VerifyIL("Program.Test22<T>(ref T)",
@"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (T& V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int? E.get_P2<T>(ref T)""
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""int int?.GetValueOrDefault()""
  IL_0011:  stloc.2
  IL_0012:  ldloca.s   V_1
  IL_0014:  call       ""bool int?.HasValue.get""
  IL_0019:  brtrue.s   IL_0033
  IL_001b:  call       ""int Program.Get1()""
  IL_0020:  stloc.2
  IL_0021:  ldloc.0
  IL_0022:  ldloca.s   V_3
  IL_0024:  ldloc.2
  IL_0025:  call       ""int?..ctor(int)""
  IL_002a:  ldloc.3
  IL_002b:  call       ""void E.set_P2<T>(ref T, int?)""
  IL_0030:  nop
  IL_0031:  br.s       IL_0033
  IL_0033:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public object P1 { get => 0; set {} }
        public int? P2 { get => 0; set {} }
    }
}

class Program
{
    static void Test1<T>() where T : struct
    {
        default(T).P1 += 1;
    }
    static void Test2<T>() where T : struct
    {
        default(T).P2 += 1;
    }
}

namespace NS1
{
    static class E
    {
        extension<T>(in T x) where T : struct
        {
        }
    }
}

namespace NS2
{
    static class E
    {
        extension<T>(ref readonly T x) where T : struct
        {
        }
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (14,9): error CS1510: A ref or out value must be an assignable variable
            //         default(T).P1 += 1;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(14, 9),
            // (18,9): error CS1510: A ref or out value must be an assignable variable
            //         default(T).P2 += 1;
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(18, 9),
            // (26,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(26, 25),
            // (36,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(36, 35)
            );
    }

    [Fact]
    public void PropertyAccess_ConditionalAssignment_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public object P1
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
                return null;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
        public int? P2
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
                return null;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test11(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test12(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        await Test13<C1>();
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test21(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test22(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        await Test23<C1>();
        System.Console.Write(Program<C1>.F.F1);
    }

    static void Test11<T>(ref T f)
    {
        f.P1 ??= Get1();
    }

    static void Test21<T>(ref T f)
    {
        f.P2 ??= Get1();
    }

    static void Test12<T>(ref T f) where T : class
    {
        f.P1 ??= Get1();
    }

    static void Test22<T>(ref T f) where T : class
    {
        f.P2 ??= Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    static async Task Test13<T>()
    {
        Program<T>.F.P1 ??= await Get1Async();
    }

    static async Task Test23<T>()
    {
        Program<T>.F.P2 ??= await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125:123123125:123123125:123123125:123123125:123123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test11<T>(ref T)",
@"
{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                T V_3,
                object V_4,
                object V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_3
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.3
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.2
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.1
  IL_001a:  ldloca.s   V_1
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.2
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  ldobj      ""T""
  IL_0026:  call       ""object E.get_P1<T>(T)""
  IL_002b:  brtrue.s   IL_004a
  IL_002d:  call       ""int Program.Get1()""
  IL_0032:  box        ""int""
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.0
  IL_003a:  ldobj      ""T""
  IL_003f:  ldloc.s    V_4
  IL_0041:  dup
  IL_0042:  stloc.s    V_5
  IL_0044:  call       ""void E.set_P1<T>(T, object)""
  IL_0049:  nop
  IL_004a:  ret
}
");

        verifier.VerifyIL("Program.Test12<T>(ref T)",
@"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (T V_0,
                object V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""object E.get_P1<T>(T)""
  IL_000e:  brtrue.s   IL_0023
  IL_0010:  ldloc.0
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  box        ""int""
  IL_001b:  dup
  IL_001c:  stloc.1
  IL_001d:  call       ""void E.set_P1<T>(T, object)""
  IL_0022:  nop
  IL_0023:  ret
}
");

        verifier.VerifyIL("Program.Test21<T>(ref T)",
@"
{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (T& V_0,
            T V_1,
            T& V_2,
            int? V_3,
            int V_4,
            T V_5,
            int? V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_5
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_5
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  ldobj      ""T""
  IL_0027:  call       ""int? E.get_P2<T>(T)""
  IL_002c:  stloc.3
  IL_002d:  ldloca.s   V_3
  IL_002f:  call       ""int int?.GetValueOrDefault()""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloca.s   V_3
  IL_0038:  call       ""bool int?.HasValue.get""
  IL_003d:  brtrue.s   IL_005f
  IL_003f:  call       ""int Program.Get1()""
  IL_0044:  stloc.s    V_4
  IL_0046:  ldloc.0
  IL_0047:  ldobj      ""T""
  IL_004c:  ldloca.s   V_6
  IL_004e:  ldloc.s    V_4
  IL_0050:  call       ""int?..ctor(int)""
  IL_0055:  ldloc.s    V_6
  IL_0057:  call       ""void E.set_P2<T>(T, int?)""
  IL_005c:  nop
  IL_005d:  br.s       IL_005f
  IL_005f:  ret
}
");

        verifier.VerifyIL("Program.Test22<T>(ref T)",
@"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (T V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""int? E.get_P2<T>(T)""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""int int?.GetValueOrDefault()""
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""bool int?.HasValue.get""
  IL_001e:  brtrue.s   IL_0038
  IL_0020:  call       ""int Program.Get1()""
  IL_0025:  stloc.2
  IL_0026:  ldloc.0
  IL_0027:  ldloca.s   V_3
  IL_0029:  ldloc.2
  IL_002a:  call       ""int?..ctor(int)""
  IL_002f:  ldloc.3
  IL_0030:  call       ""void E.set_P2<T>(T, int?)""
  IL_0035:  nop
  IL_0036:  br.s       IL_0038
  IL_0038:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_DeconstructAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        (this.P1, _) = (Program.Get1(), 0);
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        (F.P1, _) = (Get1(), 0);
    }

    public static int Get1()
    {
        System.Console.Write(Program.F.F1);
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124124:123124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  stloc.0
  IL_000c:  ldobj      ""S1""
  IL_0011:  ldloc.0
  IL_0012:  call       ""void E.set_P1(S1, int)""
  IL_0017:  nop
  IL_0018:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""int Program.Get1()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  ldloc.0
  IL_000e:  call       ""void E.set_P1(S1, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_DeconstructAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        (this.P1, _) = (Program.Get1(), 0);
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        (F.P1, _) = (Get1(), 0);
    }

    public static int Get1()
    {
        System.Console.Write(Program.F.F1);
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124124:123124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_0012:  nop
  IL_0013:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""int Program.Get1()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_000e:  nop
  IL_000f:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int P1 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        (default(S1).P1, _) = (1, 0);
    }
}
""";

        var comp2 = CreateCompilation(src2);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (15,10): error CS1510: A ref or out value must be an assignable variable
                    //         (default(S1).P1, _) = (1, 0);
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(15, 10)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (15,10): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         (default(S1).P1, _) = (1, 0);
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(15, 10),
                    // (15,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         (default(S1).P1, _) = (1, 0);
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 10)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (15,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         (default(S1).P1, _) = (1, 0);
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 10)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_DeconstructAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program
{
    public static C1 F = new C1 { F1 = 123 };

    static void Main()
    {
        Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        (F.P1, _) = (Get1(), 0);
    }

    static int Get1()
    {
        System.Console.Write(Program.F.F1);
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""void E.set_P1(C1, int)""
  IL_0012:  nop
  IL_0013:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_DeconstructAssignment_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        (f.P1, _) = (Get1(), 0);
    }

    static void Test2<T>(ref T f) where T : struct
    {
        (f.P1, _) = (Get1(), 0);
    }

    static int Get1()
    {
        System.Console.Write(Program<S1>.F.F1);
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>()
    {
        (Program<T>.F.P1, _) = (await Get1Async(), 0);
    }

    static async Task<int> Get1Async()
    {
        System.Console.Write(Program<S1>.F.F1);
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124124:123124124:123124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                T V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.2
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.3
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.3
  IL_002b:  call       ""void E.set_P1<T>(T, int)""
  IL_0030:  nop
  IL_0031:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  ldobj      ""T""
  IL_000d:  ldloc.0
  IL_000e:  call       ""void E.set_P1<T>(T, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_DeconstructAssignment_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(((S1)(object)x).F1);
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test2<T>(ref T f) where T : struct
    {
        (f.P1, _) = (Get1(), 0);
    }

    static int Get1()
    {
        System.Console.Write(Program<S1>.F.F1);
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        (Program<T>.F.P1, _) = (await Get1Async(), 0);
    }

    static async Task<int> Get1Async()
    {
        System.Console.Write(Program<S1>.F.F1);
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124124:123124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""void E.set_P1<T>(ref T, int)""
  IL_000e:  nop
  IL_000f:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int P1 { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        (default(T).P1, _) = (1, 0);
    }
}

namespace NS1
{
    static class E
    {
        extension<T>(in T x) where T : struct
        {
        }
    }
}

namespace NS2
{
    static class E
    {
        extension<T>(ref readonly T x) where T : struct
        {
        }
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (13,10): error CS1510: A ref or out value must be an assignable variable
            //         (default(T).P1, _) = (1, 0);
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(T)").WithLocation(13, 10),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact]
    public void PropertyAccess_DeconstructAssignment_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int P1
        {
            get
            {
                throw null;
            }
            set
            {
                System.Console.Write(((C1)(object)x).F1);
            }
        }
    }
}

class C1
{
    public int F1;
}

class Program<T>
{
    public static T F;
}

class Program
{
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        await Test3<C1>();
        System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        (f.P1, _) = (Get1(), 0);
    }

    static void Test2<T>(ref T f) where T : class
    {
        (f.P1, _) = (Get1(), 0);
    }

    static int Get1()
    {
        System.Console.Write(Program<C1>.F.F1);
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    static async Task Test3<T>()
    {
        (Program<T>.F.P1, _) = (await Get1Async(), 0);
    }

    static async Task<int> Get1Async()
    {
        System.Console.Write(Program<C1>.F.F1);
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123124:123123124:123123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                T V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_2
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.2
  IL_000c:  box        ""T""
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloc.1
  IL_0014:  ldobj      ""T""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.3
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.3
  IL_002b:  call       ""void E.set_P1<T>(T, int)""
  IL_0030:  nop
  IL_0031:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  call       ""void E.set_P1<T>(T, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");
    }

    [Fact]
    public void GroupingTypeRawName_01()
    {
        // extension parameter name
        var src = """
static class E
{
    extension(object o)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        Assert.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        Assert.Equal("extension(System.Object o)", extension.ComputeExtensionMarkerRawName());
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("for")]
    [InlineData("if")]
    [InlineData("true")]
    [InlineData("throw")]
    [InlineData("ref")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("return")]
    [InlineData("void")]
    [InlineData("dynamic")]
    [InlineData("unmanaged")]
    [InlineData("notnull")]
    [InlineData("await")]
    [InlineData("field")]
    [InlineData("file")]
    [InlineData("record")]
    public void GroupingTypeRawName_02(string keyword)
    {
        // keyword or contextual keyword as extension parameter name
        var src = $$"""
static class E
{
    extension(object @{{keyword}})
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        Assert.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        Assert.Equal($"extension(System.Object @{keyword})", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_04()
    {
        // separator for containing types is slash
        var src = """
static class E
{
    extension(N1.N2.C1.C2.C3)
    {
    }
}

namespace N1
{
    namespace N2
    {
        class C1
        {
            public class C2
            {
                public class C3 { }
            }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        Assert.Equal("extension(N1.N2.C1/C2/C3)", extension.ComputeExtensionGroupingRawName());
        Assert.Equal("extension(N1.N2.C1.C2.C3)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_05()
    {
        // containing type gets an arity, all type arguments are included
        var src = """
static class E
{
    extension(C1<int>.C2<string>)
    {
    }
}

class C1<T> { public class C2<U> { } }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(C1`1/C2`1<System.Int32, System.String>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(C1<System.Int32>.C2<System.String>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_06()
    {
        // Arity above 10
        var src = """
static class E
{
    extension(C<int, int, int, int, int, int, int, int, int, int, int, int>)
    {
    }
}

class C<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(C`12<System.Int32, System.Int32, System.Int32, System.Int32, " +
            "System.Int32, System.Int32, System.Int32, System.Int32, " +
            "System.Int32, System.Int32, System.Int32, System.Int32>)",
            extension.ComputeExtensionGroupingRawName());

        AssertEx.Equal("extension(C<System.Int32, System.Int32, System.Int32, System.Int32, " +
            "System.Int32, System.Int32, System.Int32, System.Int32, " +
            "System.Int32, System.Int32, System.Int32, System.Int32>)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_07()
    {
        // Nested type arguments
        var src = """
static class E
{
    extension(C<C<int>>)
    {
    }
}
class C<T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(C`1<C`1<System.Int32>>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(C<C<System.Int32>>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_08()
    {
        // Short tuple
        var src = """
static class E
{
    extension((int alice, string bob))
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.ValueTuple`2<System.Int32, System.String>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension((System.Int32 alice, System.String bob))", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_09()
    {
        // Long tuple
        var src = """
static class E
{
    extension((int x0, int x1, int x2, int x3, int x4, int x5, int x6, string x7))
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.ValueTuple`8<System.Int32, System.Int32, System.Int32, System.Int32, " +
            "System.Int32, System.Int32, System.Int32, System.ValueTuple`1<System.String>>)",
            extension.ComputeExtensionGroupingRawName());

        AssertEx.Equal("extension((System.Int32 x0, System.Int32 x1, System.Int32 x2, System.Int32 x3, " +
            "System.Int32 x4, System.Int32 x5, System.Int32 x6, System.String x7))",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_10()
    {
        // Simple types
        var src = """
static class E
{
    extension(C<char, string, bool, sbyte, short, int, long, float, double, byte, ushort, uint, ulong>)
    {
    }
}
class C<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(C`13<System.Char, System.String, System.Boolean, System.SByte, " +
            "System.Int16, System.Int32, System.Int64, System.Single, System.Double, " +
            "System.Byte, System.UInt16, System.UInt32, System.UInt64>)",
            extension.ComputeExtensionGroupingRawName());

        AssertEx.Equal("extension(C<System.Char, System.String, System.Boolean, System.SByte, " +
            "System.Int16, System.Int32, System.Int64, System.Single, System.Double, " +
            "System.Byte, System.UInt16, System.UInt32, System.UInt64>)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_11()
    {
        // Native ints
        var src = """
static class E
{
    extension((nint, nuint))
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();
        Assert.True(comp.Assembly.RuntimeSupportsNumericIntPtr);
        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.ValueTuple`2<System.IntPtr, System.UIntPtr>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension((System.IntPtr, System.UIntPtr))", extension.ComputeExtensionMarkerRawName());

        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        Assert.False(comp.Assembly.RuntimeSupportsNumericIntPtr);
        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension((System.IntPtr, System.UIntPtr))", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_12()
    {
        // System.Nullable
        var src = """
static class E
{
    extension(int?)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Nullable`1<System.Int32>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Nullable<System.Int32>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_13()
    {
        // Referencing type parameter
        var src = """
static class E
{
    extension<U>(U)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<U>(U)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_14()
    {
        // Referencing over 10 type parameters as type arguments
        var src = """
static class E
{
    extension<U0, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12>(C<U0, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12>)
    {
    }
}
class C<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<,,,,,,,,,,,,>(C`13<!0, !1, !2, !3, !4, !5, !6, !7, !8, !9, !10, !11, !12>)",
            extension.ComputeExtensionGroupingRawName());

        AssertEx.Equal("extension<U0, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12>(C<U0, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12>)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_15()
    {
        // Attributes are removed from CLR-level signature
        var src = """
static class E
{
    extension<[My] T>(T)
    {
    }
}

class MyAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<[MyAttribute/*()*/] T>(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_16()
    {
        // Nullability annotations are removed from CLR-level signature
        var src = """
#nullable enable

static class E
{
    extension(object?)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object?)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_17()
    {
        // Array
        var src = """
static class E
{
    extension(object[][,])
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[,][])", extension.ComputeExtensionGroupingRawName());
        // Note: we're using the inner dimensions first order (as we do when nullability annotations are present)
        AssertEx.Equal("extension(System.Object[,][])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_18()
    {
        // Array with nullability annotations
        var src = """
#nullable enable

static class E
{
    extension(object?[]?[,])
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[][,])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object?[]?[,]!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_19()
    {
        // Vector / single-dimensional array
        var src = """
#nullable enable
static class E
{
    extension(object[,])
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[,])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object![,]!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_20()
    {
        // Pointer type
        var src = """
unsafe static class E
{
    extension(int*[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32*[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Int32*[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_21()
    {
        // Pointer type
        var src = """
unsafe static class E
{
    extension(int**[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32**[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Int32**[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_22()
    {
        // Function pointer type
        var src = """
unsafe static class E
{
    extension(delegate*<int, string, void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method void *(System.Int32, System.String)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<System.Int32, System.String, void>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate*<int, string, void>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method void *(int32, string)[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_23()
    {
        // null ExtensionParameter
        var src = """
static class E
{
    extension(__arglist)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist)
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension()", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension()", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_24()
    {
        // constraints: class
        var src = """
static class E
{
    extension<T>(T) where T : class
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : class", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_25()
    {
        // constraints: struct
        var src = """
static class E
{
    extension<T>(T) where T : struct
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : struct", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_26()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : class, new()
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class .ctor>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : class, new()", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_27()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : new(), class
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,31): error CS0401: The new() constraint must be the last restrictive constraint specified
            //     extension<T>(T) where T : new(), class
            Diagnostic(ErrorCode.ERR_NewBoundMustBeLast, "new").WithLocation(3, 31),
            // (3,38): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
            //     extension<T>(T) where T : new(), class
            Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(3, 38));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class .ctor>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : class, new()", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_28()
    {
        // constraints: unmanaged
        var src = """
static class E
{
    extension<T>(T) where T : unmanaged
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType))>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : unmanaged", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C<T> where T : unmanaged
{
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C`1", """
.class private sequential ansi sealed beforefieldinit C`1<valuetype .ctor (class [System.Runtime]System.ValueType modreq([System.Runtime]System.Runtime.InteropServices.UnmanagedType)) T>
    extends [System.Runtime]System.ValueType
{
    .param type T
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
            01 00 00 00
        )
    .pack 0
    .size 1
} // end of class C`1
""");
    }

    [Fact]
    public void GroupingTypeRawName_29()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : System.ValueType
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,31): error CS0702: Constraint cannot be special class 'ValueType'
            //     extension<T>(T) where T : System.ValueType
            Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "System.ValueType").WithArguments("System.ValueType").WithLocation(3, 31));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_30()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : unmanaged, new()
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,42): error CS8375: The 'new()' constraint cannot be used with the 'unmanaged' constraint
            //     extension<T>(T) where T : unmanaged, new()
            Diagnostic(ErrorCode.ERR_NewBoundWithUnmanaged, "new").WithLocation(3, 42));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType))>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : unmanaged, new()", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_31()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : unmanaged, I
    {
    }
}

interface I { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (I, System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType))>(!0)",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : unmanaged, I", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension<T>(T) where T : I, unmanaged
    {
    }
}

interface I { }
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,34): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
            //     extension<T>(T) where T : I, unmanaged
            Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "unmanaged").WithLocation(3, 34));

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I)>(!0)",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_32()
    {
        // type constraints
        var src = """
static class E
{
    extension<T>(T) where T : I
    {
    }
}

interface I { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_33()
    {
        // type constraints
        var src = """
static class E
{
    extension<T>(T) where T : I1, I2
    {
    }
}

interface I1 { }
interface I2 { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I1, I2)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I1, I2", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_34()
    {
        // type constraints
        var src = """
static class E
{
    extension<T>(T) where T : I2, I1
    {
    }
}

interface I1 { }
interface I2 { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I1, I2)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I1, I2", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_35()
    {
        // type constraints
        var src = """
static class E
{
    extension<T>(T) where T : C, I1, I2
    {
    }
}

interface I1 { }
interface I2 { }
class C { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(C, I1, I2)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : C, I1, I2", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension<T>(T) where T : I2, I1, C
    {
    }
}

interface I1 { }
interface I2 { }
class C { }
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,39): error CS0406: The class type constraint 'C' must come before any other constraints
            //     extension<T>(T) where T : I2, I1, C
            Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "C").WithArguments("C").WithLocation(3, 39));

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I1, I2)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I1, I2", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_36()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : struct, I
    {
    }
}

interface I { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (I, System.ValueType)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : struct, I", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_37()
    {
        // constraints: allows ref struct
        var src = """
static class E
{
    extension<T>(T) where T : allows ref struct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<byreflike>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : allows ref struct", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension<T>(T) where T : allows ref struct, I
    {
    }
}

interface I { }
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,31): error CS9242: The 'allows' constraint clause must be the last constraint specified
            //     extension<T>(T) where T : allows ref struct, I
            Diagnostic(ErrorCode.ERR_AllowsClauseMustBeLast, "allows").WithLocation(3, 31));

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<byreflike (I)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I, allows ref struct", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension<T>(T) where T : I, allows ref struct
    {
    }
}

interface I { }
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<byreflike (I)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : I, allows ref struct", extension.ComputeExtensionMarkerRawName());

        // Note: IL should have byreflike flag
        var src2 = """
struct C<T> where T : allows ref struct
{
}
""";
        CompileAndVerify(src2, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C`1", """
.class private sequential ansi sealed beforefieldinit C`1<T>
    extends [System.Runtime]System.ValueType
{
    .pack 0
    .size 1
} // end of class C`1
""");
    }

    [Fact]
    public void GroupingTypeRawName_38()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : struct, allows ref struct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype byreflike .ctor (System.ValueType)>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : struct, allows ref struct", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_39()
    {
        // constraints
        var src = """
static class E
{
    extension<T>(T) where T : new(), allows ref struct
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<byreflike .ctor>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : new(), allows ref struct", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_40()
    {
        // type constraints
        var src = """
static class E
{
    extension<T, U>(T) where T : U
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(!1),>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T, U>(T) where T : U", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_41()
    {
        var src = """
unsafe static class E
{
    extension(delegate*<D, D>[])
    {
    }
}

struct D { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method D *(D)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<D, D>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_42()
    {
        var src = """
unsafe static class E
{
    extension(delegate*<D, D>[])
    {
    }
}

class D { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method D *(D)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<D, D>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_43()
    {
        var src = """
unsafe static class E
{
    extension<T>(delegate*<T, T>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(method !0 *(!0)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(delegate*<T, T>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_44()
    {
        var src = """
static class E
{
    extension(C<C<int>>)
    {
    }
}

struct C<T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(C`1<C`1<System.Int32>>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(C<C<System.Int32>>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_45()
    {
        var src = """
static class E
{
    extension(C<C<int>>)
    {
    }
}

class C<T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(C`1<C`1<System.Int32>>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(C<C<System.Int32>>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_46()
    {
        var src = """
static class E
{
    extension(ERROR)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
            //     extension(ERROR)
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(ERROR)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(ERROR)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_47()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_48()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Cdecl]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged cdecl void *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[CDecl]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_49()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Stdcall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged stdcall void *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Stdcall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_50()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Thiscall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged thiscall void *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Thiscall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_51()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Fastcall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged fastcall void *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Fastcall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_52()
    {
        // function pointer modifiers: 1 non-special calling convention
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[SuppressGCTransition]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[SuppressGCTransition]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_53()
    {
        // function pointer modifiers: 1 non-special calling convention
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Vectorcall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,35): error CS8890: Type 'CallConvVectorcall' is not defined.
            //     extension(delegate* unmanaged[Vectorcall]<void>[])
            Diagnostic(ErrorCode.ERR_TypeNotFound, "Vectorcall").WithArguments("CallConvVectorcall").WithLocation(3, 35));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void modopt(System.Runtime.CompilerServices.CallConvVectorcall) *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Vectorcall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_54()
    {
        // function pointer modifiers: more than 1 special calling convention
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Stdcall, Thiscall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void modopt(System.Runtime.CompilerServices.CallConvThiscall) modopt(System.Runtime.CompilerServices.CallConvStdcall) *()[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Stdcall, Thiscall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_55()
    {
        // function pointer modifiers: more than 1 special calling convention, reverse order
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Thiscall, Stdcall]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void modopt(System.Runtime.CompilerServices.CallConvStdcall) modopt(System.Runtime.CompilerServices.CallConvThiscall) *()[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Thiscall, Stdcall]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_56()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Stdcall, SuppressGCTransition]<void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged void modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt(System.Runtime.CompilerServices.CallConvStdcall) *()[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Stdcall, SuppressGCTransition]<void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_57()
    {
        // function pointer refness: ref
        var src = """
unsafe static class E
{
    extension(delegate* <ref int, ref long>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method System.Int64& *(System.Int32&)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<ref System.Int32, ref System.Int64>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate* <ref int, ref long>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method int64& *(int32&)[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_58()
    {
        // function pointer refness: ref readonly
        var src = """
unsafe static class E
{
    extension(delegate* <ref readonly int, ref readonly long>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method System.Int64& modreq(System.Runtime.InteropServices.InAttribute) *(System.Int32& modopt(System.Runtime.CompilerServices.RequiresLocationAttribute))[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<ref readonly System.Int32, ref readonly System.Int64>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate* <ref readonly int, ref readonly long>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method int64& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) *(int32& modopt([System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute))[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_59()
    {
        // function pointer refness: in
        var src = """
unsafe static class E
{
    extension(delegate* <in int, void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method void *(System.Int32& modreq(System.Runtime.InteropServices.InAttribute))[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<in System.Int32, void>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate* <in int, void>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method void *(int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute))[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_60()
    {
        // function pointer refness: out
        var src = """
unsafe static class E
{
    extension(delegate* <out int, void>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method void *(System.Int32& modreq(System.Runtime.InteropServices.OutAttribute))[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<out System.Int32, void>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate* <out int, void>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method void *(int32& modreq([System.Runtime]System.Runtime.InteropServices.OutAttribute))[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_61()
    {
        // function pointer nullability
        var src = """
#nullable enable

unsafe static class E
{
    extension(delegate* <object?, object>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method System.Object *(System.Object)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<System.Object?, System.Object!>[]!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_62()
    {
        // function pointer: type alias
        var src = """
using Obj = System.Object;

unsafe static class E
{
    extension(delegate* <Obj, Obj>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method System.Object *(System.Object)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<System.Object, System.Object>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_63()
    {
        // function pointer modifiers
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[Stdcall, SuppressGCTransition]<ref readonly int>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged System.Int32& modreq(System.Runtime.InteropServices.InAttribute) modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt(System.Runtime.CompilerServices.CallConvStdcall) *()[])",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[Stdcall, SuppressGCTransition]<ref readonly System.Int32>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate* unmanaged[Stdcall, SuppressGCTransition]<ref readonly int>[] field;
}
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method unmanaged int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) modopt([System.Runtime]System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt([System.Runtime]System.Runtime.CompilerServices.CallConvStdcall) *()[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_64()
    {
        var src = """
static class E<T>
{
    extension(T)
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension(T)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(!T)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_65()
    {
        var src = """
static class E<T0>
{
    extension<T>(T0)
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension<T>(T0)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!T0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T0)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_66()
    {
        var src = """
static class E<T>
{
    extension<U>(U)
    {
        extension<V>(V)
        {
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension<U>(U)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5),
            // (5,9): error CS9282: This member is not allowed in an extension block
            //         extension<V>(V)
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(5, 9));

        var nestedExtension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single().GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", nestedExtension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<V>(V)", nestedExtension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_67()
    {
        var src = """
unsafe static class E
{
    extension(delegate*<scoped ref int, scoped ref int>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (3,25): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
            //     extension(delegate*<scoped ref int, scoped ref int>[])
            Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(3, 25),
            // (3,41): error CS8808: 'scoped' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
            //     extension(delegate*<scoped ref int, scoped ref int>[])
            Diagnostic(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, "scoped").WithArguments("scoped").WithLocation(3, 41));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method System.Int32& *(System.Int32&)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<ref System.Int32, ref System.Int32>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_68()
    {
        var src = """
static class E
{
    extension((dynamic, dynamic))
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.ValueTuple`2<System.Object, System.Object>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension((dynamic, dynamic))", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_69()
    {
        var src = """
static class E
{
    extension(dynamic)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'dynamic'
            //     extension(dynamic)
            Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(3, 15));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
    }

    [Fact]
    public void GroupingTypeRawName_70()
    {
        var src = """
static class E
{
    extension<in T>(T)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     extension<in T>(T)
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(3, 15));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_71()
    {
        var src = """
static class E
{
    extension<out T>(T)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     extension<out T>(T)
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(3, 15));

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_72()
    {
        var src = """
static class E
{
    extension<in T>(T t)
    {
        public void M() { }
    }
    extension<T>(T t)
    {
        public void M() { }
    }

    public static void M2<in T>(this T t) { }
    public static void M2<T>(this T t) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     extension<in T>(T t)
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(3, 15),
            // (9,21): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(9, 21),
            // (12,27): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     public static void M2<in T>(this T t) { }
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(12, 27),
            // (13,24): error CS0111: Type 'E' already defines a member called 'M2' with the same parameter types
            //     public static void M2<T>(this T t) { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "E").WithLocation(13, 24));
    }

    [Fact]
    public void GroupingTypeRawName_73()
    {
        // Function pointer type with type named "void"
        var src = """
unsafe static class E
{
    extension(delegate*<@void, @void>[])
    {
    }
}
class @void { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method 'void' *('void')[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<@void, @void>[])", extension.ComputeExtensionMarkerRawName());

        var src2 = """
unsafe struct C
{
    delegate*<@void, @void>[] field;
}
class @void { }
""";
        CompileAndVerify(src2, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90, verify: Verification.FailsPEVerify).VerifyTypeIL("C", """
.class private sequential ansi sealed beforefieldinit C
    extends [System.Runtime]System.ValueType
{
    // Fields
    .field private method class 'void' *(class 'void')[] 'field'
} // end of class C
""");
    }

    [Fact]
    public void GroupingTypeRawName_74()
    {
        // Function pointer type with type named "void" in namespace
        var src = """
unsafe static class E
{
    extension(delegate*<N.@void, N.@void>[])
    {
    }
}

namespace N
{
    class @void { }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method N.void *(N.void)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<N.@void, N.@void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void GroupingTypeRawName_75()
    {
        // Function pointer type with nested type named "void"
        var src = """
unsafe static class E
{
    extension(delegate*<C.@void, C.@void>[])
    {
    }
}

class C
{
    public class @void { }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method C/void *(C/void)[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate*<C.@void, C.@void>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("for")]
    [InlineData("if")]
    [InlineData("true")]
    [InlineData("throw")]
    [InlineData("ref")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("return")]
    [InlineData("new")]
    [InlineData("void")]
    [InlineData("dynamic")]
    [InlineData("unmanaged")]
    [InlineData("notnull")]
    [InlineData("await")]
    [InlineData("field")]
    [InlineData("file")]
    [InlineData("record")]
    public void MarkerTypeRawName_04(string keyword)
    {
        // keyword or contextual keyword in extended type
        var src = $$"""
static class E
{
    extension(@{{keyword}})
    {
    }
}

class @{{keyword}} { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();

        AssertEx.Equal(keyword is "void" ? "extension('void')" : $"extension({keyword})", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal($"extension(@{keyword})", extension.ComputeExtensionMarkerRawName());

        src = $$"""
static class E
{
    extension(N.@{{keyword}})
    {
    }
}

namespace N
{
    class @{{keyword}} { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal($"extension(N.{keyword})", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal($"extension(N.@{keyword})", extension.ComputeExtensionMarkerRawName());

        src = $$"""
static class E
{
    extension(C.@{{keyword}})
    {
    }
}

class C
{
    public class @{{keyword}} { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal($"extension(C/{keyword})", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal($"extension(C.@{keyword})", extension.ComputeExtensionMarkerRawName());
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("for")]
    [InlineData("if")]
    [InlineData("true")]
    [InlineData("throw")]
    [InlineData("ref")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("return")]
    [InlineData("void")]
    [InlineData("dynamic")]
    [InlineData("unmanaged")]
    [InlineData("notnull")]
    [InlineData("await")]
    [InlineData("field")]
    [InlineData("file")]
    [InlineData("record")]
    public void MarkerTypeRawName_05(string keyword)
    {
        // keyword or contextual keyword in type parameter and type parameter constraint
        var src = $$"""
static class E
{
    extension<@{{keyword}}, T>(int)
        where @{{keyword}} : class
        where T : @{{keyword}}
    {
    }
}

class @{{keyword}} { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class, (!0)>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal($"extension<@{keyword}, T>(System.Int32) where @{keyword} : class where T : @{keyword}", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_08()
    {
        // nullable annotations on extension parameter
        var src = """
#nullable enable

static class E
{
    extension<T>(T?)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T?)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_09()
    {
        // nullable annotations on extension parameter
        var src = """
#nullable enable

static class E
{
    extension<T>(T)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_10()
    {
        // nullable annotations on extension parameter
        var src = """
#nullable enable

static class E
{
    extension<T>(
#nullable disable
        T
#nullable enable
        )
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_11()
    {
        // nullable annotations in tuple
        var src = """
#nullable enable

static class E
{
    extension<T>((
        string?, T?,
        string, T,
#nullable disable
        string, T
#nullable enable
        )) where T : class
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class>(System.ValueTuple`6<System.String, !0, System.String, !0, System.String, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>((System.String?, T?, System.String!, T!, System.String, T)) where T : class!", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_12()
    {
        // nullable annotations in tuple
        var src = """
#nullable enable

static class E
{
    extension<T>((
        T?,
        T,
#nullable disable
        T
#nullable enable
        ))
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.ValueTuple`3<!0, !0, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>((T?, T!, T))", extension.ComputeExtensionMarkerRawName());
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("for")]
    [InlineData("if")]
    [InlineData("true")]
    [InlineData("throw")]
    [InlineData("ref")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("return")]
    [InlineData("void")]
    [InlineData("dynamic")]
    [InlineData("unmanaged")]
    [InlineData("notnull")]
    [InlineData("await")]
    [InlineData("field")]
    [InlineData("file")]
    [InlineData("record")]
    public void MarkerTypeRawName_13(string keyword)
    {
        // tuple with keyword or contextual keyword as element names
        var src = $$"""
static class E
{
    extension<T>((int @{{keyword}}, int x))
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.ValueTuple`2<System.Int32, System.Int32>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal($$"""extension<T>((System.Int32 @{{keyword}}, System.Int32 x))""", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_14()
    {
        // nullable annotations in type arguments
        var src = """
#nullable enable

static class E
{
    extension<T>(S<
        string?, T?,
        string, T,
#nullable disable
        string, T
#nullable enable
        >) where T : class
    {
    }
}

struct S<T0, T1, T2, T3, T4, T5>
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class>(S`6<System.String, !0, System.String, !0, System.String, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(S<System.String?, T?, System.String!, T!, System.String, T>) where T : class!", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_15()
    {
        // nullable annotations in type arguments
        var src = """
#nullable enable

static class E
{
    extension<T>(S<
        T?,
        T,
#nullable disable
        T
#nullable enable
        >)
    {
    }
}

struct S<T0, T1, T2>
{
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(S`3<!0, !0, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(S<T?, T!, T>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_16()
    {
        // constraints: class
        var src = """
#nullable enable

static class E
{
    extension<T1, T2, T3>(int)
        where T1 : class
        where T2 : class?
#nullable disable
        where T3 : class
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class, class, class>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : class! where T2 : class? where T3 : class", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_17()
    {
        // misc constraints
        var src = """
#nullable enable

static class E
{
    extension<T1, T2, T3>(int)
        where T1 : struct
        where T2 : unmanaged
        where T3 : notnull
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType), valuetype .ctor (System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType)),>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : struct where T2 : unmanaged where T3 : notnull", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_18()
    {
        // type constraints
        var src = """
#nullable enable

static class E
{
    extension<T1, T2, T3>(int)
        where T1 : I
        where T2 : I?
#nullable disable
        where T3 : I
#nullable enable
    {
    }
}

interface I { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I), (I), (I)>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : I! where T2 : I? where T3 : I", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_19()
    {
        // type constraints are sorted
        var src = """
static class E
{
    extension<T1, T2, T3>(int)
        where T1 : I1, I2
        where T2 : I2, I1
    {
    }
}

interface I1 { }
interface I2 { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I1, I2), (I1, I2),>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : I1, I2 where T2 : I1, I2", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_20()
    {
        // attributes are sorted
        var src = """
static class E
{
    extension<[A, B] T1>([A, B]int)
    {
    }
}

class AAttribute : System.Attribute { }
class BAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<[AAttribute/*()*/] [BAttribute/*()*/] T1>([AAttribute/*()*/] [BAttribute/*()*/] System.Int32)", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension<[B, A] T1>([B, A]int)
    {
    }
}

class AAttribute : System.Attribute { }
class BAttribute : System.Attribute { }
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<[AAttribute/*()*/] [BAttribute/*()*/] T1>([AAttribute/*()*/] [BAttribute/*()*/] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_21()
    {
        // attribute in namespace
        var src = """
static class E
{
    extension([N.C.My(10)] int)
    {
    }
}

namespace N
{
    public class C
    {
        public class MyAttribute : System.Attribute 
        { 
            public MyAttribute(int value) { }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension([N.C.MyAttribute/*(System.Int32)*/(10)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_22()
    {
        // attribute with arguments and properties, properties are sorted
        var src = """
static class E
{
    extension([My(10, "hello", P = 20, P2 = "hello2")] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int value, string s) { }
    public int P { get; set; }
    public string P2 { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("""extension([MyAttribute/*(System.Int32, System.String)*/(10, "hello", P = 20, P2 = "hello2")] System.Int32)""", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension([My(s: "hello", value: 10, P2 = "hello2", P = 20)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int value, string s) { }
    public int P { get; set; }
    public string P2 { get; set; }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("""extension([MyAttribute/*(System.Int32, System.String)*/(10, "hello", P = 20, P2 = "hello2")] System.Int32)""", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_23()
    {
        // attribute with parameters of various primitive types
        var src = """
static class E
{
    extension([My(true, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 'c', "hello", 42L)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(bool x1, sbyte x2, short x3, int x4, long x5, byte x6, ushort x7, uint x8, ulong x9, float x10, double x11, char x12, string x13, object x14) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Boolean, System.SByte, System.Int16, System.Int32, System.Int64, System.Byte, System.UInt16, System.UInt32, System.UInt64, System.Single, System.Double, System.Char, System.String, System.Object)*/" +
            "(true, 1, 2, 3, 4, 5, 6, 7, 8, 1091567616, 4621819117588971520, 'c', \"hello\", 42)] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_24()
    {
        // attribute with parameters of other types
        var src = """
static class E
{
    extension([My(new int[] { 1, 2, 3 }, N.MyEnum.A, typeof(string))] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int[] x1, N.MyEnum x2, System.Type x3) { }
}

namespace N
{
    enum MyEnum { A, B, C }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[], N.MyEnum, System.Type)*/([1, 2, 3], 0, typeof(System.String))] System.Int32)",
            extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension([My(new int[] { 3, 2, 1 })] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int[] x1) { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[])*/([3, 2, 1])] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_25()
    {
        // attribute with properties of various primitive types
        var src = """
static class E
{
    extension([My(BoolProperty = false, SByteProperty = -1, ShortProperty = -2, IntProperty = -3, LongProperty = -4,
        ByteProperty = 5, UShortProperty = 6, UIntProperty = 7, ULongProperty = 8,
        FloatProperty = 9, DoubleProperty = 10, CharProperty = 'c', StringProperty = "hello", ObjectProperty = 42L)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }

    public bool BoolProperty { get; set; }
    public sbyte SByteProperty { get; set; }
    public short ShortProperty { get; set; }
    public int IntProperty { get; set; }
    public long LongProperty { get; set; }
    public byte ByteProperty { get; set; }
    public ushort UShortProperty { get; set; }
    public uint UIntProperty { get; set; }
    public ulong ULongProperty { get; set; }
    public float FloatProperty { get; set; }
    public double DoubleProperty { get; set; }
    public char CharProperty { get; set; }
    public string StringProperty { get; set; }
    public object ObjectProperty { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/(BoolProperty = false, ByteProperty = 5, CharProperty = 'c', " +
            "DoubleProperty = 4621819117588971520, FloatProperty = 1091567616, IntProperty = -3, LongProperty = -4, ObjectProperty = 42, " +
            "SByteProperty = -1, ShortProperty = -2, StringProperty = \"hello\", UIntProperty = 7, ULongProperty = 8, UShortProperty = 6)] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_26()
    {
        // attribute with properties of other types
        var src = """
static class E
{
    extension([My(IntArrayProperty = new[] { int.MaxValue, int.MinValue }, EnumProperty = MyEnum.B, TypeProperty = typeof(int), ObjectProperty = null)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }

    public int[] IntArrayProperty { get; set; }
    public MyEnum EnumProperty { get; set; }
    public System.Type TypeProperty { get; set; }
    public object ObjectProperty { get; set; }
}

enum MyEnum { A, B, C }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/(EnumProperty = 1, IntArrayProperty = [2147483647, -2147483648], ObjectProperty = null, TypeProperty = typeof(System.Int32))] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_27()
    {
        // attribute with misc float and double values
        var src = """
static class E
{
    extension([My(
        [float.MaxValue, float.MinValue, float.Epsilon, float.PositiveInfinity, float.NegativeInfinity, 0, float.NegativeZero, float.NaN], 
        [double.MaxValue, double.MinValue, double.PositiveInfinity, double.NegativeInfinity, 0, double.NegativeZero, double.NaN])] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(float[] x1, double[] x2) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Single[], System.Double[])*/(" +
            "[2139095039, -8388609, 1, 2139095040, -8388608, 0, -2147483648, -4194304], " +
            "[9218868437227405311, -4503599627370497, 9218868437227405312, -4503599627370496, 0, -9223372036854775808, -2251799813685248])] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_28()
    {
        // attribute with default parameter value
        var src = """
static class E
{
    extension([My] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int value = 42) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension([MyAttribute/*(System.Int32)*/(42)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("for")]
    [InlineData("if")]
    [InlineData("true")]
    [InlineData("throw")]
    [InlineData("ref")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("return")]
    [InlineData("void")]
    [InlineData("dynamic")]
    [InlineData("unmanaged")]
    [InlineData("notnull")]
    [InlineData("await")]
    [InlineData("field")]
    [InlineData("file")]
    [InlineData("record")]
    public void MarkerTypeRawName_29(string keyword)
    {
        // attribute with keyword or contextual keyword as property name
        var src = $$"""
static class E
{
    extension([My(@{{keyword}} = 42)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }

    public int @{{keyword}} { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal($$"""extension([MyAttribute/*()*/(@{{keyword}} = 42)] System.Int32)""",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_30()
    {
        // attribute with keyword as enum type and type name
        var src = """
static class E
{
    extension([My(EnumProperty = @for.A, TypeProperty = typeof(@for))] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }

    public @for EnumProperty { get; set; }
    public System.Type TypeProperty { get; set; }
}

enum @for { A = 0 }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/(EnumProperty = 0, TypeProperty = typeof(for))] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_31()
    {
        // attribute with error value
        var src = """
static class E
{
    extension([My(IntProperty = ERROR)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }
    public int IntProperty { get; set; }
}

enum @for { A = 0 }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,33): error CS0103: The name 'ERROR' does not exist in the current context
            //     extension([My(IntProperty = ERROR)] int)
            Diagnostic(ErrorCode.ERR_NameNotInContext, "ERROR").WithArguments("ERROR").WithLocation(3, 33));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/(IntProperty = error)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_32()
    {
        // Array with nullability annotations
        var src = """
#nullable enable

static class E
{
    extension(object[][,]?)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[,][])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object![,]![]?)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_33()
    {
        // Array with nullability annotations
        var src = """
#nullable enable

static class E
{
    extension(object[][,]?
#nullable disable
        [,,,]
#nullable enable
        )
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        // Note: we're using the inner dimensions first order (whether nullability annotations are present or not)
        AssertEx.Equal("extension(System.Object[,][][,,,])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object![,]![]?[,,,])", extension.ComputeExtensionMarkerRawName());
    }
    [Fact]
    public void MarkerTypeRawName_34()
    {
        // Array with nullability annotations
        var src = """
#nullable enable

static class E
{
    extension(object
        []
#nullable disable
        [,]
#nullable enable
        )
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[,][])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object![,][]!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_35()
    {
        // Array with nullability annotations
        var src = """
#nullable enable

static class E
{
    extension(object
#nullable disable
        []
#nullable enable
        [,]
        )
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object[,][])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object![,]![])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_36()
    {
        // Pointer type with nullability annotations
        var src = """
#nullable enable

unsafe static class E
{
    extension(object?*[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (5,15): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('object')
            //     extension(object?*[])
            Diagnostic(ErrorCode.WRN_ManagedAddr, "object?*[]").WithArguments("object").WithLocation(5, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object*[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object?*[]!)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_37()
    {
        // extension parameter modifiers: ref
        var src = """
static class E
{
    extension(ref int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(ref System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_38()
    {
        // extension parameter modifiers: out
        var src = """
static class E
{
    extension(out int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS8328:  The parameter modifier 'out' cannot be used with 'extension'
            //     extension(out int i)
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "extension").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(out System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_39()
    {
        // extension parameter modifiers: ref readonly
        var src = """
static class E
{
    extension(ref readonly int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(ref readonly System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_40()
    {
        // extension parameter modifiers: in
        var src = """
static class E
{
    extension(in int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(in System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_41()
    {
        // extension parameter modifiers: scoped
        var src = """
static class E
{
    extension(scoped ref int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(scoped ref System.Int32 i)", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    public static void M(this scoped ref int i) { }
    public static void M(this ref int i) { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,24): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //     public static void M(this ref int i) { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(4, 24));
    }

    [Fact]
    public void MarkerTypeRawName_42()
    {
        // [UnscopedRef]
        var src = """
static class E
{
    extension([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int i)
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(ref [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute/*()*/] System.Int32 i)", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int i)
    {
        public void M() { }
        public static void M2() { }
        public int P => 0;
        public static int P2 => 0;
    }

    extension(ref int i)
    {
        public void M() { }
        public static void M2() { }
        public int P => 0;
        public static int P2 => 0;
    }
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics(
            // (13,21): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(13, 21),
            // (14,28): error CS0111: Type 'E' already defines a member called 'M2' with the same parameter types
            //         public static void M2() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "E").WithLocation(14, 28),
            // (15,20): error CS0102: The type 'E' already contains a definition for 'P'
            //         public int P => 0;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("E", "P").WithLocation(15, 20),
            // (16,27): error CS0102: The type 'E' already contains a definition for 'P2'
            //         public static int P2 => 0;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("E", "P2").WithLocation(16, 27));
    }

    [Fact]
    public void MarkerTypeRawName_43()
    {
        // [AllowNull]
        var src = """
#nullable enable

static class E
{
    extension([System.Diagnostics.CodeAnalysis.AllowNull] object o)
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension([System.Diagnostics.CodeAnalysis.AllowNullAttribute/*()*/] System.Object! o)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_44()
    {
        // default parameter value
        var src = """
static class E
{
    extension(int i = 42)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9284: The receiver parameter of an extension cannot have a default value
            //     extension(int i = 42)
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = 42").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_45()
    {
        // params
        var src = """
static class E
{
    extension(params int[] i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1670: params is not valid in this context
            //     extension(params int[])
            Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32[] i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_46()
    {
        // multiple type parameters with constraints
        var src = """
static class E
{
    extension<T, U>(int) where T : class where U : struct
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<T, U>(System.Int32) where T : class where U : struct", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_47()
    {
        // type in a namespace vs. in a containing type
        var src = """
static class E
{
    extension(A.B)
    {
    }
}

namespace A
{
    class B { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(A.B)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(A.B)", extension.ComputeExtensionMarkerRawName());

        src = """
static class E
{
    extension(A.B)
    {
    }
}

class A
{
    public class B { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(A/B)", extension.ComputeExtensionGroupingRawName());
        // Note: it's okay that the marker name has ambiguity (can't distinguish between a containing namespace and containing type) since the grouping name is unambiguous
        AssertEx.Equal("extension(A.B)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_48()
    {
        // Containing type with type arguments
        var src = """
static class E
{
    extension(A<int>.B<string>)
    {
    }
}

class A<T>
{
    public class B<U> { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(A`1/B`1<System.Int32, System.String>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(A<System.Int32>.B<System.String>)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_49()
    {
        // attribute with empty array
        var src = """
static class E
{
    extension([My(IntArrayProperty = new int[] { })] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute() { }
    public int[] IntArrayProperty { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/(IntArrayProperty = [])] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_50()
    {
        // attribute with null array
        var src = """
static class E
{
    extension([My(null, IntArrayProperty = null)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int[] value) { }
    public int[] IntArrayProperty { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[])*/(null, IntArrayProperty = null)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_51()
    {
        // attribute with default struct
        var src = """
static class E
{
    extension([My(default)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int value) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32)*/(0)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_52()
    {
        // attribute with byte enum
        var src = """
static class E
{
    extension([My(MyEnum.A)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(MyEnum x) { }
}

enum MyEnum : byte { A = 42 }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(MyEnum)*/(42)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_53()
    {
        // attribute with escaped char and string
        var src = """
static class E
{
    extension([My('\'', "quote: \" backslash: \\")] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(char c, string s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal(""""extension([MyAttribute/*(System.Char, System.String)*/('\'', "quote: \" backslash: \\")] System.Int32)"""", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_54()
    {
        // attribute with null System.Type argument
        var src = """
static class E
{
    extension([My(null)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(System.Type x) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Type)*/(null)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_55()
    {
        // attribute with string with escaped strings
        var src = """
static class E
{
    extension([My(@"\r\n\t\0\a\b\f\v\U0001D11E
end")] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(string s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        var escapedNewline = Environment.NewLine switch
        {
            "\r\n" => "\\r\\n",
            "\n" => "\\n",
            _ => throw ExceptionUtilities.Unreachable()
        };

        AssertEx.Equal($$"""extension([MyAttribute/*(System.String)*/("\\r\\n\\t\\0\\a\\b\\f\\v\\U0001D11E{{escapedNewline}}end")] System.Int32)""", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_56()
    {
        // attribute with string with escaped strings
        var src = """
static class E
{
    extension([My("\r\n\t\0\a\b\f\v\U0001D11E")] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(string s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("""extension([MyAttribute/*(System.String)*/("\r\n\t\0\a\b\f\v𝄞")] System.Int32)""", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_57()
    {
        // Incompatible constraints
        var src = """
#nullable enable

static class E
{
    extension<T1, T2, T3>(int)
        where T1 : struct, unmanaged
        where T2 : class, notnull
        where T3 : unmanaged, notnull
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,28): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
            //         where T1 : struct, unmanaged
            Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "unmanaged").WithLocation(6, 28),
            // (7,27): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
            //         where T2 : class, notnull
            Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "notnull").WithLocation(7, 27),
            // (8,31): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
            //         where T3 : unmanaged, notnull
            Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "notnull").WithLocation(8, 31));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : struct where T2 : class! where T3 : unmanaged", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_58()
    {
        // attribute with misc arrays
        var src = """
static class E
{
    extension([My(null, null)] int)
    {
    }
}

class MyAttribute : System.Attribute 
{ 
    public MyAttribute(int[][] x1, long [,] x2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x1' has type 'int[][]', which is not a valid attribute parameter type
            //     extension([My(null, null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x1", "int[][]").WithLocation(3, 16),
            // (3,16): error CS0181: Attribute constructor parameter 'x2' has type 'long[*,*]', which is not a valid attribute parameter type
            //     extension([My(null, null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x2", "long[*,*]").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[][], System.Int64[,])*/(error, error)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_59()
    {
        // function pointer with non-void return type and modifier
        var src = """
unsafe static class E
{
    extension(delegate* unmanaged[SuppressGCTransition]<int>[])
    {
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(method unmanaged System.Int32 modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) *()[])", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(delegate* unmanaged[SuppressGCTransition]<System.Int32>[])", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_60()
    {
        // attribute with modifiers
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 modopt(int32) modopt(string) x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
public static class E
{
    extension([My(42)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32 modopt(System.Int32) modopt(System.String))*/(42)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_61()
    {
        // attribute with modifiers, reverse order
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 modopt(string) modopt(int32) x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
public static class E
{
    extension([My(42)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32 modopt(System.String) modopt(System.Int32))*/(42)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_62()
    {
        // attribute with modifiers, array type
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32[] modopt(int32) modopt(string) x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
public static class E
{
    extension([My(new[] { 42 })] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[] modopt(System.Int32) modopt(System.String))*/([42])] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_63()
    {
        // attribute with modifiers, array type
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 modopt(int32) modopt(string)[] x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
public static class E
{
    extension([My(new[] { 42 })] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32 modopt(System.Int32) modopt(System.String)[])*/([42])] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_64()
    {
        // attribute with modifiers, pointer type with modifiers
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32* modopt(int32) modopt(string) x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
unsafe public static class E
{
    extension([My(null)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'int*', which is not a valid attribute parameter type
            //     extension([My(null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "int*").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32* modopt(System.Int32) modopt(System.String))*/(error)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_65()
    {
        // attribute with modifiers, pointer type with modifiers
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 modopt(int32) modopt(string)* x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";
        var src = """
unsafe public static class E
{
    extension([My(null)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'int*', which is not a valid attribute parameter type
            //     extension([My(null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "int*").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32 modopt(System.Int32) modopt(System.String)*)*/(error)] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_66()
    {
        if (ExecutionConditionUtil.IsWindowsDesktop)
        {
            // Can't build this IL on older ilasm
            return;
        }

        // attribute with modifiers, function pointer type with modifiers
        // parameter is: `delegate* unmanaged[Stdcall, SuppressGCTransition]<ref readonly int>` with a modopt on `int`
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( method unmanaged int32 modopt(string)& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt([mscorlib]System.Runtime.CompilerServices.CallConvStdcall) *() x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.CallConvSuppressGCTransition
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
""";
        var src = """
unsafe public static class E
{
    extension([My(null)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'delegate* unmanaged[Stdcall]<ref readonly int>', which is not a valid attribute parameter type
            //     extension([My(null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "delegate* unmanaged[Stdcall]<ref readonly int>").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(method unmanaged System.Int32 modopt(System.String)& " +
            "modreq(System.Runtime.InteropServices.InAttribute) modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt(System.Runtime.CompilerServices.CallConvStdcall) *())*/(error)] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_67()
    {
        if (ExecutionConditionUtil.IsWindowsDesktop)
        {
            // Can't build this IL on older ilasm
            return;
        }

        // attribute with modifiers, function pointer type with modifiers
        // parameter is: `delegate* unmanaged[Stdcall, SuppressGCTransition]<void>` with a modopt on `void`
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( method unmanaged void modopt(string) modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt([mscorlib]System.Runtime.CompilerServices.CallConvStdcall) *() x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}

.class public auto ansi beforefieldinit System.Runtime.CompilerServices.CallConvSuppressGCTransition
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
""";
        var src = """
unsafe public static class E
{
    extension([My(null)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'delegate* unmanaged[Stdcall]<void>', which is not a valid attribute parameter type
            //     extension([My(null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "delegate* unmanaged[Stdcall]<void>").WithLocation(3, 16));

        // Note: the order of modifiers is reversed (also shown in example below). Tracked by issue https://github.com/dotnet/roslyn/issues/79344
        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(method unmanaged void modopt(System.String) modopt(System.Runtime.CompilerServices.CallConvSuppressGCTransition) modopt(System.Runtime.CompilerServices.CallConvStdcall) *())*/(error)] System.Int32)",
            extension.ComputeExtensionMarkerRawName());

        ilSrc = """
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .field public int32 modopt(int64) modopt(string) modopt(int32) 'field'

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
""";
        comp = CreateCompilationWithIL("", ilSrc, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();

        var field = comp.GetMember<NamedTypeSymbol>("C").GetField("field");
        AssertEx.Equal(["System.Int32", "System.String", "System.Int64"], field.TypeWithAnnotations.CustomModifiers.SelectAsArray(m => m.Modifier.ToTestDisplayString()));
    }

    [Fact]
    public void MarkerTypeRawName_68()
    {
        // attribute with modifiers, modopt in type arguments
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor ( class C`1<int32 modopt(string)> x ) cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}

.class public auto ansi beforefieldinit C`1<T>
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
} 
""";
        var src = """
public static class E
{
    extension([My(null)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'C<int>', which is not a valid attribute parameter type
            //     extension([My(null)] int)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "C<int>").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(C`1<System.Int32 modopt(System.String)>)*/(error)] System.Int32)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_69()
    {
        // ScopedKind.ScopedValue
        var src = """
public static class E
{
    extension(scoped int i)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
            //     extension(scoped int i)
            Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped int i").WithLocation(3, 15));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(scoped System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_70()
    {
        // attribute signature with nested type
        var src = """
public static class E
{
    extension([My(null)] int i)
    {
    }
}

class C
{
    public class Nested { } 
}

class MyAttribute : System.Attribute 
{
    public MyAttribute(C.Nested x) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'C.Nested', which is not a valid attribute parameter type
            //     extension([My(null)] int i)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "C.Nested").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(C/Nested)*/(error)] System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_71()
    {
        // attribute signature with function pointer type
        var src = """
public unsafe static class E
{
    extension([My(null)] int i)
    {
    }
}

class C
{
    public class Nested { } 
}

unsafe class MyAttribute : System.Attribute 
{
    public MyAttribute(delegate*<void> x) { }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'x' has type 'delegate*<void>', which is not a valid attribute parameter type
            //     extension([My(null)] int i)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("x", "delegate*<void>").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(method void *())*/(error)] System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_72()
    {
        // attribute with params
        var src = """
public static class E
{
    extension([My(1, 2)] int i)
    {
    }
}

class MyAttribute : System.Attribute 
{
    public MyAttribute(params int[] i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[])*/([1, 2])] System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_73()
    {
        // attribute with params
        var src = """
public static class E
{
    extension([My(null)] int i)
    {
    }
}

class MyAttribute : System.Attribute 
{
    public MyAttribute(params int[] i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[])*/(null)] System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_74()
    {
        // attribute with params
        var src = """
public static class E
{
    extension([My(new[] { 1, 2 })] int i)
    {
    }
}

class MyAttribute : System.Attribute 
{
    public MyAttribute(params int[] i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Int32[])*/([1, 2])] System.Int32 i)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_75()
    {
        // attribute with typeof with C#-isms
        var src = """
public static class E
{
    extension([My(typeof((int a, int b)))] int i)
    {
    }
}

class MyAttribute : System.Attribute 
{
    public MyAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Type)*/(typeof(System.ValueTuple`2<System.Int32, System.Int32>))] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_76()
    {
        // file-local type
        var src = """
public static class E
{
    extension([My(typeof(C))] int i)
    {
    }
}

file class C { }

class MyAttribute : System.Attribute 
{
    public MyAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Type)*/(typeof(<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C))] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_77()
    {
        // file-local type
        var src = """
file static class E
{
    extension([My(null)] int i)
    {
    }
}

file class C { }

file class MyAttribute : System.Attribute 
{
    public MyAttribute(C c) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 'c' has type 'C', which is not a valid attribute parameter type
            //     extension([My(null)] int i)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("c", "C").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C)*/(error)] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_78()
    {
        // unbound generic type
        var src = """
public static class E
{
    extension([My(typeof(C<>))] int i)
    {
    }
}

public class C<T> { }
public class MyAttribute : System.Attribute 
{
    public MyAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(System.Type)*/(typeof(C`1))] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_79()
    {
        // attribute with user-defined struct
        var src = """
public static class E
{
    extension([My(new S())] int i)
    {
    }
}

public struct S { }

public class MyAttribute : System.Attribute 
{
    public MyAttribute(S s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 's' has type 'S', which is not a valid attribute parameter type
            //     extension([My(new S())] int i)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("s", "S").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(S)*/(error)] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_80()
    {
        // attribute with user-defined struct
        var src = """
public static class E
{
    extension([My(default)] int i)
    {
    }
}

public struct S { }

public class MyAttribute : System.Attribute 
{
    public MyAttribute(S s) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,16): error CS0181: Attribute constructor parameter 's' has type 'S', which is not a valid attribute parameter type
            //     extension([My(new S())] int i)
            Diagnostic(ErrorCode.ERR_BadAttributeParamType, "My").WithArguments("s", "S").WithLocation(3, 16));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*(S)*/(error)] System.Int32 i)",
            extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void MarkerTypeRawName_81()
    {
        // attribute with two properties of the same name
        var ilSrc = """
.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }

    .property instance int32 Property()
    {
         .set instance void MyAttribute::set_Property(int32)
    }

     .method private hidebysig specialname instance void set_Property ( int32 'value' ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }

    .property instance int64 Property()
    {
         .set instance void MyAttribute::set_Property(int64)
    }

     .method private hidebysig specialname instance void set_Property ( int64 'value' ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""";
        var src = """
public static class E
{
    extension([My(Property = (int)1)] int)
    {
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (3,19): error CS0246: The type or namespace name 'Property' could not be found (are you missing a using directive or an assembly reference?)
            //     extension([My(Property = (int)1)] int)
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Property").WithArguments("Property").WithLocation(3, 19));

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension([MyAttribute/*()*/] System.Int32)", extension.ComputeExtensionMarkerRawName());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_01()
    {
        var src = """
new object().M(1);

public static class E
{
    extension(object @this)
    {
        public void M<T>(T x)
        {
            @this.M(x, default);
        }

        public void M(params System.ReadOnlySpan<object> span)
        {
        }

        private void M<T>(T x, System.ReadOnlySpan<object> span)
        {
            System.Console.Write("ran");
        }
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: ExpectedOutput("ran"), verify: Verification.FailsPEVerify);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_02()
    {
        var src = """
new object().M(a: null);

public static class E
{
    extension(object o1)
    {
        public void M(object a) { }

        public void M(string a) { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_03()
    {
        var src = """
new object().M(a: null);

public static class E
{
    extension(object o1)
    {
        public void M(object a) { }
    }

    public static void M(this object o, string a) { System.Console.Write("ran"); }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_04()
    {
        var src = """
new object().M(a: null);

public static class E
{
    extension(object o1)
    {
        public void M(string a) { System.Console.Write("ran"); }
    }

    public static void M(this object o, object a) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_05()
    {
        var src = """
new object().M(a: null);

public static class E
{
    extension(object o1)
    {
        public void M(object a) { }
        public void M(params int[] a) { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_06()
    {
        var src = """
new object().M(1, 2);

public static class E
{
    extension(object o1)
    {
        public void M(int x1, int x2) { System.Console.Write("ran"); }
        public void M(params int[] a) { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_07()
    {
        var src = """
new object().M(a: 1, 2);

public static class E
{
    extension(object o1)
    {
        public void M(int a, int b) { System.Console.Write("ran"); }
        public void M(params int[] a) { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_08()
    {
        var src = """
new object().M(index: 0, 1, 2);

public static class E
{
    extension(object o1)
    {
        public void M(int index, int a, int b) { System.Console.Write("ran"); }
        public void M(int index, params int[] a) { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_09()
    {
        var src = """
object.M(index: 0, 1, 2);

public static class E
{
    extension(object)
    {
        public static void M(int index, int a, int b) { System.Console.Write("ran"); }
        public static void M(int index, params int[] a) { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79193")]
    public void OverloadResolution_10()
    {
        var src = """
1.M(2);

public static class E
{
    extension(params int[] i)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,15): error CS1670: params is not valid in this context
            //     extension(params int[] i)
            Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(5, 15),
            // (1,3): error CS1501: No overload for method 'M' takes 1 arguments
            // 1.M(2);
            Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "1").WithLocation(1, 3));
    }

    [Fact]
    public void FunctionType_MissingImplementationMethod()
    {
        // Based on the following, but without implementation method
        // public static class E
        // {
        //     extension(int)
        //     {
        //         public static void M() { }
        //     }
        // }
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
var x = int.M;
""";

        var comp = CreateCompilationWithIL(src, ilSrc, parseOptions: TestOptions.Regular12);
        comp.VerifyEmitDiagnostics(
            // (1,9): error CS0570: 'E.extension(int).M()' is not supported by the language
            // var x = int.M;
            Diagnostic(ErrorCode.ERR_BindToBogus, "int.M").WithArguments("E.extension(int).M()").WithLocation(1, 9));

        DiagnosticDescription[] expected = [
            // (1,9): error CS0570: 'E.extension(int).M()' is not supported by the language
            // var x = int.M;
            Diagnostic(ErrorCode.ERR_BindToBogus, "int.M").WithArguments("E.extension(int).M()").WithLocation(1, 9)];

        comp = CreateCompilationWithIL(src, ilSrc, parseOptions: TestOptions.Regular13);
        comp.VerifyEmitDiagnostics(expected);

        comp = CreateCompilationWithIL(src, ilSrc, parseOptions: TestOptions.RegularNext);
        comp.VerifyEmitDiagnostics(expected);

        comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(expected);
    }

    [Fact]
    public void MemberNameSameAsType_01()
    {
        var src = """
static class E
{
    extension(int)
    {
        public static void E() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,28): error CS0542: 'E': member names cannot be the same as their enclosing type
            //         public static void E() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("E").WithLocation(5, 28));
    }

    [Fact]
    public void MemberNameSameAsType_02()
    {
        var src = """
static class E
{
    extension(object o)
    {
        public void E() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,21): error CS0542: 'E': member names cannot be the same as their enclosing type
            //         public void E() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("E").WithLocation(5, 21));
    }

    [Fact]
    public void MemberNameSameAsType_03()
    {
        var src = """
static class E
{
    extension(object)
    {
        public static int E { get => 0; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,27): error CS0542: 'E': member names cannot be the same as their enclosing type
            //         public static int E { get => 0; set { } }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("E").WithLocation(5, 27));
    }

    [Fact]
    public void MemberNameSameAsType_04()
    {
        var src = """
static class E
{
    extension(object o)
    {
        public int E { get => 0; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,20): error CS0542: 'E': member names cannot be the same as their enclosing type
            //         public int E { get => 0; set { } }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("E").WithLocation(5, 20));
    }

    [Fact]
    public void MemberNameSameAsType_05()
    {
        var src = """
static class get_E
{
    extension(object)
    {
        public static int E => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,32): error CS0542: 'get_E': member names cannot be the same as their enclosing type
            //         public static int E => 0;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "0").WithArguments("get_E").WithLocation(5, 32));
    }

    [Fact]
    public void MemberNameSameAsType_07()
    {
        var src = """
static class get_E
{
    extension(object)
    {
        public static int E => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,32): error CS0542: 'get_E': member names cannot be the same as their enclosing type
            //         public static int E => 0;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "0").WithArguments("get_E").WithLocation(5, 32));
    }

    [Fact]
    public void MemberNameSameAsType_08()
    {
        var src = """
static class op_Addition
{
    extension(object)
    {
        public static object operator+(object o1, object o2) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,38): error CS0542: 'op_Addition': member names cannot be the same as their enclosing type
            //         public static object operator+(object o1, object o2) => throw null;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "+").WithArguments("op_Addition").WithLocation(5, 38));
    }

    [Fact]
    public void MemberNameSameAsType_09()
    {
        var src = """
extension(object)
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension(object)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(1, 1));
    }

    [Fact]
    public void MemberNameSameAsType_10()
    {
        var src = """
static class E1
{
    static class E2
    {
        extension(object)
        {
            public static void E1() { }
            public static void E2() { }
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //         extension(object)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 9),
            // (8,32): error CS0542: 'E2': member names cannot be the same as their enclosing type
            //             public static void E2() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E2").WithArguments("E2").WithLocation(8, 32));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_01()
    {
        var src = """
static class E
{
    extension(Name)
    {
        public static void Name() { }
    }
}

class Name { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,28): error CS9326: 'Name': extension member names cannot be the same as their extended type
            //         public static void Name() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "Name").WithArguments("Name").WithLocation(5, 28));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_02()
    {
        var src = """
static class E
{
    extension(get_P)
    {
        public static int P => 0;
    }
}

class get_P { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,32): error CS9326: 'get_P': extension member names cannot be the same as their extended type
            //         public static int P => 0;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "0").WithArguments("get_P").WithLocation(5, 32));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_03()
    {
        var src = """
public static class E
{
    extension<T>(T t)
    {
        void T() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void MemberNameSameAsExtendedType_04()
    {
        var src = """
static class E
{
    extension(op_Addition)
    {
        public static op_Addition operator+(op_Addition o1, op_Addition o2) => throw null;
    }
}

class op_Addition { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,43): error CS9326: 'op_Addition': extension member names cannot be the same as their extended type
            //         public static op_Addition operator+(op_Addition o1, op_Addition o2) => throw null;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "+").WithArguments("op_Addition").WithLocation(5, 43));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_05()
    {
        var src = """
static class E
{
    extension(N.op_Addition)
    {
        public static N.op_Addition operator+(N.op_Addition o1, N.op_Addition o2) => throw null;
    }
}

namespace N
{
    class op_Addition { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,45): error CS9326: 'op_Addition': extension member names cannot be the same as their extended type
            //         public static N.op_Addition operator+(N.op_Addition o1, N.op_Addition o2) => throw null;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "+").WithArguments("op_Addition").WithLocation(5, 45));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_06()
    {
        var src = """
static class E
{
    extension<T>(op_Addition<T>)
    {
        public static op_Addition<T> operator+(op_Addition<T> o1, op_Addition<T> o2) => throw null;
    }
}

class op_Addition<T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,46): error CS9326: 'op_Addition': extension member names cannot be the same as their extended type
            //         public static op_Addition<T> operator+(op_Addition<T> o1, op_Addition<T> o2) => throw null;
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "+").WithArguments("op_Addition").WithLocation(5, 46));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_07()
    {
        var src = """
static class E
{
    extension((int, int))
    {
        public static void ValueTuple() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,28): error CS9326: 'ValueTuple': extension member names cannot be the same as their extended type
            //         public static void ValueTuple() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "ValueTuple").WithArguments("ValueTuple").WithLocation(5, 28));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_08()
    {
        var src = """
#nullable enable

static class E
{
    extension(string?)
    {
        public static void String() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (7,28): error CS9326: 'String': extension member names cannot be the same as their extended type
            //         public static void String() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "String").WithArguments("String").WithLocation(7, 28));
    }

    [Fact]
    public void MemberNameSameAsExtendedType_09()
    {
        var src = """
#nullable enable

static class E
{
    extension(string?)
    {
        public static void @string() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void MemberNameSameAsExtendedType_10()
    {
        var src = """
static class E
{
    extension(C<int>)
    {
        public static void C() { }
    }
}

public class C<T> { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (5,28): error CS9326: 'C': extension member names cannot be the same as their extended type
            //         public static void C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsExtendedType, "C").WithArguments("C").WithLocation(5, 28));
    }

    [Fact]
    public void ExtractCastInvocation_01()
    {
        var src = """
_ = /*<bind>*/ from int x in new C<int>()
    from int y in new C<int>()
    select x.ToString() + y.ToString() /*</bind>*/;

static class E
{
    extension(C<int> source)
    {
        public C<string> SelectMany(System.Func<int, C<int>> collectionSelector, System.Func<int, int, string> resultSelector) { System.Console.Write("SelectMany"); return new C<string>(); }
        public C<T> Cast<T>() { System.Console.Write("Cast "); return new C<T>(); }
    }
}

class C<T> { }
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, expectedOutput: "Cast SelectMany").VerifyDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var q = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Single();

        var info0 = model.GetQueryClauseInfo(q.FromClause);
        Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
        Assert.Null(info0.OperationInfo.Symbol);
        Assert.Equal("x", model.GetDeclaredSymbol(q.FromClause).Name);

        var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
        Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
        Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
        Assert.Equal("y", model.GetDeclaredSymbol(q.Body.Clauses[0]).Name);

        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname beforefieldinit '<>E__0'
        extends [mscorlib]System.Object
    {
        // Methods
        .method private hidebysig specialname static 
            void '<Extension>$' (
                class C`1<int32> source
            ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                01 00 00 00
            )
            // Method begins at RVA 0x212c
            // Code size 1 (0x1)
            .maxstack 8
            IL_0000: ret
        } // end of method '<>E__0'::'<Extension>$'
        .method public hidebysig 
            instance class C`1<string> SelectMany (
                class [mscorlib]System.Func`2<int32, class C`1<int32>> collectionSelector,
                class [mscorlib]System.Func`3<int32, int32, string> resultSelector
            ) cil managed 
        {
            // Method begins at RVA 0x212e
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::SelectMany
        .method public hidebysig 
            instance class C`1<!!T> Cast<T> () cil managed 
        {
            // Method begins at RVA 0x212e
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<>E__0'::Cast
    } // end of class <>E__0
    // Methods
    .method public hidebysig static 
        class C`1<string> SelectMany (
            class C`1<int32> source,
            class [mscorlib]System.Func`2<int32, class C`1<int32>> collectionSelector,
            class [mscorlib]System.Func`3<int32, int32, string> resultSelector
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x20cb
        // Code size 16 (0x10)
        .maxstack 8
        IL_0000: ldstr "SelectMany"
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: newobj instance void class C`1<string>::.ctor()
        IL_000f: ret
    } // end of method E::SelectMany
    .method public hidebysig static 
        class C`1<!!T> Cast<T> (
            class C`1<int32> source
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x20dc
        // Code size 16 (0x10)
        .maxstack 8
        IL_0000: ldstr "Cast "
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: newobj instance void class C`1<!!T>::.ctor()
        IL_000f: ret
    } // end of method E::Cast
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        var expectedOperationTree = """
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: C<System.String>) (Syntax: 'from int x  ... .ToString()')
Expression:
  IInvocationOperation ( C<System.String> E.<>E__0.SelectMany(System.Func<System.Int32, C<System.Int32>> collectionSelector, System.Func<System.Int32, System.Int32, System.String> resultSelector)) (OperationKind.Invocation, Type: C<System.String>, IsImplicit) (Syntax: 'from int y  ... ew C<int>()')
    Instance Receiver:
      IInvocationOperation ( C<System.Int32> E.<>E__0.Cast<System.Int32>()) (OperationKind.Invocation, Type: C<System.Int32>, IsImplicit) (Syntax: 'from int x  ... ew C<int>()')
        Instance Receiver:
          IObjectCreationOperation (Constructor: C<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: C<System.Int32>) (Syntax: 'new C<int>()')
            Arguments(0)
            Initializer:
              null
        Arguments(0)
    Arguments(2):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C<int>()')
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, C<System.Int32>>, IsImplicit) (Syntax: 'new C<int>()')
            Target:
              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'new C<int>()')
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'new C<int>()')
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'new C<int>()')
                    ReturnedValue:
                      IInvocationOperation ( C<System.Int32> E.<>E__0.Cast<System.Int32>()) (OperationKind.Invocation, Type: C<System.Int32>, IsImplicit) (Syntax: 'new C<int>()')
                        Instance Receiver:
                          IObjectCreationOperation (Constructor: C<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: C<System.Int32>) (Syntax: 'new C<int>()')
                            Arguments(0)
                            Initializer:
                              null
                        Arguments(0)
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.Int32, System.String>, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
            Target:
              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x.ToString( ... .ToString()')
                    ReturnedValue:
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'x.ToString( ... .ToString()')
                        Left:
                          IInvocationOperation (virtual System.String System.Int32.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
                            Instance Receiver:
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            Arguments(0)
                        Right:
                          IInvocationOperation (virtual System.String System.Int32.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'y.ToString()')
                            Instance Receiver:
                              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            Arguments(0)
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""";
        VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(src, expectedOperationTree, []);
    }

    [Fact]
    public void MissingSystemObject()
    {
        var src = """
static class E
{
    extension(object)
    {
    }
}
""";
        var comp = CreateEmptyCompilation(src);
        comp.VerifyEmitDiagnostics(
            // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
            Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
            // error CS0518: Predefined type 'System.Attribute' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
            // error CS0518: Predefined type 'System.Attribute' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
            // error CS0518: Predefined type 'System.Int32' is not defined or imported
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Int32").WithLocation(1, 1),
            // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
            // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
            // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
            // (1,14): error CS0518: Predefined type 'System.Object' is not defined or imported
            // static class E
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Object").WithLocation(1, 14),
            // (3,5): error CS0518: Predefined type 'System.Object' is not defined or imported
            //     extension(object)
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "extension").WithArguments("System.Object").WithLocation(3, 5),
            // (3,5): error CS1110: Cannot define a new extension because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
            //     extension(object)
            Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "extension").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(3, 5),
            // (3,14): error CS0518: Predefined type 'System.Void' is not defined or imported
            //     extension(object)
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(").WithArguments("System.Void").WithLocation(3, 14),
            // (3,15): error CS0518: Predefined type 'System.Object' is not defined or imported
            //     extension(object)
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "object").WithArguments("System.Object").WithLocation(3, 15));
    }
}

