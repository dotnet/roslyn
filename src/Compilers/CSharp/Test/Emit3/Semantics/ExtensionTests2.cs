﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections.Immutable;
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
        var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
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
    public void Nullability_PropertyPattern_Reinference_01()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;

            class Program
            {
                void M1(bool b)
                {
                    var item = "a";
                    if (b)
                    {
                        item = null;
                    }

                    var list = M2(item)/*T:System.Collections.Generic.List<string?>!*/;
                    if (list is { First: var first })
                    {
                        first.ToString(); // 1
                    }
                }

                List<T> M2<T>(T item) => [item];
            }

            static class ListExtensions
            {
                extension<T>(List<T> list)
                {
                    public T First => list[0];
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyTypes();
        comp.VerifyEmitDiagnostics(
            // (17,13): warning CS8602: Dereference of a possibly null reference.
            //             first.ToString(); // 1
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "first").WithLocation(17, 13));
    }

    [Fact]
    public void Nullability_PropertyPattern_Reinference_02()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;

            class Program
            {
                void M1(bool b)
                {
                    var item = "a";
                    if (b)
                    {
                        item = null;
                    }

                    var list = M2(item)/*T:System.Collections.Generic.List<string?>!*/;
                    if (list is { First: var first }) // 1
                    {
                        first.ToString();
                    }
                }

                List<T> M2<T>(T item) => [item];
            }

            static class ListExtensions
            {
                extension(List<string> list)
                {
                    public string First => list[0];
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyTypes();
        comp.VerifyEmitDiagnostics(
            // (15,23): warning CS8620: Argument of type 'List<string?>' cannot be used for parameter 'list' of type 'List<string>' in 'extension(List<string>)' due to differences in the nullability of reference types.
            //         if (list is { First: var first }) // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "First").WithArguments("System.Collections.Generic.List<string?>", "System.Collections.Generic.List<string>", "list", "extension(List<string>)").WithLocation(15, 23));
    }

    [Fact]
    public void Nullability_PropertyPattern_Reinference_03()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;

            class Program
            {
                void M1(bool b)
                {
                    var item = "a";
                    if (b)
                    {
                        item = null;
                    }

                    var list = M2(item)/*T:System.Collections.Generic.List<string?>!*/;
                    if (list is { First: var first }) // 1
                    {
                        first.ToString(); // 2
                    }
                }

                List<T> M2<T>(T item) => [item];
            }

            static class ListExtensions
            {
                extension<T>(List<T> list) where T : class
                {
                    public T First => list[0];
                }
            }
            """;

        // Tracked by https://github.com/dotnet/roslyn/issues/78830 : diagnostic quality consider reporting a better containing symbol
        var comp = CreateCompilation(source);
        comp.VerifyTypes();
        comp.VerifyEmitDiagnostics(
            // (15,23): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'ListExtensions.extension<T>(List<T>)'. Nullability of type argument 'string?' doesn't match 'class' constraint.
            //         if (list is { First: var first }) // 1
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "First").WithArguments("ListExtensions.extension<T>(System.Collections.Generic.List<T>)", "T", "string?").WithLocation(15, 23),
            // (17,13): warning CS8602: Dereference of a possibly null reference.
            //             first.ToString(); // 2
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "first").WithLocation(17, 13));
    }

    [Fact]
    public void Nullability_PropertyPattern_Postcondition_01()
    {
        var source = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            class Program
            {
                void M(object? obj)
                {
                    if (obj is { AsNotNull: var notNull })
                        notNull.ToString();
                }
            }

            static class Extensions
            {
                extension(object? obj)
                {
                    [NotNull]
                    public object? AsNotNull => obj!;
                }
            }
            """;

        var comp = CreateCompilation([source, NotNullAttributeDefinition]);
        // Tracked by https://github.com/dotnet/roslyn/issues/78828 : should we extend member post-conditions to work with extension members?
        comp.VerifyEmitDiagnostics(
            // (9,13): warning CS8602: Dereference of a possibly null reference.
            //             notNull.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "notNull").WithLocation(9, 13));
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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 46 34 42 34
                46 46 45 34 31 41 42 34 39 45 38 30 41 34 45 43
                46 33 39 30 43 46 36 45 42 33 37 32 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }
}
""" + ExtensionMarkerAttributeIL;
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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = $$"""
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method {{accessibility}} hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }
        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = GetSyntax<InvocationExpressionSyntax>(tree, "int.M()");
        Assert.Equal("void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
    }

    [Fact]
    public void PENamedTypeSymbol_03()
    {
        // Extension marker method is generic
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$'<T> ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 46 34 42 34
                46 46 45 34 31 41 42 34 39 45 38 30 41 34 45 43
                46 33 39 30 43 46 36 45 42 33 37 32 00 00
            )
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
""" + ExtensionMarkerAttributeIL;
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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }
        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static int32 '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ldc.i4.0
                IL_0001: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' () cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '', string s ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
        {
            .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
            IL_0000: ret
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
            01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
            43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
            30 36 32 46 35 39 45 44 34 44 36 39 00 00
        )
        IL_0000: ldnull
        IL_0001: throw
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        var extension = comp.GlobalNamespace.GetTypeMember("<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69");
        Assert.False(extension.IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_09()
    {
        // Two extension marker methods
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
             .method public hidebysig specialname static void '<Extension>$' ( string s ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;
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
        // Arity mismatch between extension member and implementation
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Accessibility mismatch between extension and implementation members
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // parameter count mismatch between extension and implementation members
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static void M ( string s ) cil managed 
    {
        IL_0000: nop
        IL_0001: ret
    }
}
""" + ExtensionMarkerAttributeIL;

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
        // return type mismatch between extension and implementation members
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static int32 M () cil managed 
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
""" + ExtensionMarkerAttributeIL;
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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 i ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig instance void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig instance void M ( string s ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // constraint mismatch between extension and implementation members
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M<T> () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig instance void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

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
        // grouping type is not sealed
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

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
        // grouping type is not public
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = $$"""
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested {{accessibility}} auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // grouping type has a base that's not object
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.String
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // grouping type implements an interface
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
        implements I
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M ( string s ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

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
    public void PENamedTypeSymbol_24()
    {
        // attributes on grouping and marker types or marker method are not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void MyAttribute::.ctor() = ( 01 00 00 00 )

        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends [mscorlib]System.Object
        {
            .custom instance void MyAttribute::.ctor() = ( 01 00 00 00 )

            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void MyAttribute::.ctor() = ( 01 00 00 00 )

                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}

.class public auto ansi beforefieldinit MyAttribute
    extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} 
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();
        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69", extension.ToTestDisplayString());
        AssertEx.SetEqual([], extension.GetAttributes().Select(a => a.ToString()));
        Assert.Equal("", extension.Name);
        AssertEx.Equal("<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69", extension.MetadataName);
    }

    [Fact]
    public void PENamedTypeSymbol_25()
    {
        var libSrc = """
public static class E
{
    extension<T>(T)
    {
        public static void M() { }
    }
}
""";
        var libComp = CreateCompilation(libSrc);
        var src = """
int.M();
""";
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        comp.VerifyEmitDiagnostics();

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal("E.<G>$8048A6C8BE30A622530249B904B537EB<T>", extension.ToTestDisplayString());
        Assert.Equal("", extension.Name);
        AssertEx.Equal("<M>$01CE3801593377B4E240F33E20D30D50", extension.MetadataName);
        Assert.False(extension.MangleName);
    }

    [Fact]
    public void PENamedTypeSymbol_26()
    {
        // marker type has different arity than grouping type
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$8048A6C8BE30A622530249B904B537EB'<$T0>
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$2789E59A55056F0AD9E820EBD5BCDFBF'<T, U>
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( !T '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 32 37 38 39
                45 35 39 41 35 35 30 35 36 46 30 41 44 39 45 38
                32 30 45 42 44 35 42 43 44 46 42 46 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0117: 'int' does not contain a definition for 'M'
            // int.M();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "M").WithArguments("int", "M").WithLocation(1, 5));

        Assert.Empty(comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers());
    }

    [Fact]
    public void PENamedTypeSymbol_27()
    {
        // marker type has different type constraints than grouping type
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$8048A6C8BE30A622530249B904B537EB'<$T0>
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$2789E59A55056F0AD9E820EBD5BCDFBF'<class T>
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( !T '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 32 37 38 39
                45 35 39 41 35 35 30 35 36 46 30 41 44 39 45 38
                32 30 45 42 44 35 42 43 44 46 42 46 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension<T>(T).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension<T>(T).M()").WithLocation(1, 5));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_28()
    {
        // implementation method has different type constraints than grouping and marker type
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$8048A6C8BE30A622530249B904B537EB'<$T0>
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$2789E59A55056F0AD9E820EBD5BCDFBF'<T>
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( !T '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 32 37 38 39
                45 35 39 41 35 35 30 35 36 46 30 41 44 39 45 38
                32 30 45 42 44 35 42 43 44 46 42 46 00 00
            )
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
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,5): error CS0570: 'E.extension<T>(T).M()' is not supported by the language
            // int.M();
            Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("E.extension<T>(T).M()").WithLocation(1, 5));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
    }

    [Fact]
    public void PENamedTypeSymbol_29()
    {
        // nested types in grouping and marker types not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }

            .class nested public auto ansi beforefieldinit Nested1
                extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
                {
                    IL_0000: ldnull
                    IL_0001: throw
                }
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }

        .class nested public auto ansi beforefieldinit Nested2
            extends System.Object
        {
            .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
            {
                IL_0000: ldnull
                IL_0001: throw
            }
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        Assert.Empty(extension.GetTypeMembers());
    }

    [Fact]
    public void PENamedTypeSymbol_30()
    {
        // fields in grouping and marker types not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }

            .field public static int32 'field'
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }

        .field public static int32 'field'
        .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
            01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
            43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
            30 36 32 46 35 39 45 44 34 44 36 39 00 00
        )
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics();

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal(["void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()"], extension.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void PENamedTypeSymbol_31()
    {
        // method from grouping or marker type not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }

            .method public hidebysig static void Method1 () cil managed 
            {
                IL_0000: ret
            }
        }

        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }

        .method public hidebysig static void Method2 () cil managed 
        {
            IL_0000: ret
        }
    }

    .method public hidebysig static void M () cil managed 
    {
        IL_0000: ret
    }

    .method public hidebysig static void Method1 () cil managed 
    {
        IL_0000: ret
    }

    .method public hidebysig static void Method2 () cil managed 
    {
        IL_0000: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
int.Method1();
int.Method2();
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (2,5): error CS0117: 'int' does not contain a definition for 'Method1'
            // int.Method1();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method1").WithArguments("int", "Method1").WithLocation(2, 5),
            // (3,5): error CS0117: 'int' does not contain a definition for 'Method2'
            // int.Method2();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Method2").WithArguments("int", "Method2").WithLocation(3, 5));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal(["void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()"], extension.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void PENamedTypeSymbol_32()
    {
        // property in grouping or marker type not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                IL_0000: ret
            }

            .method public hidebysig specialname static int32 get_P1 () cil managed
            {
                IL_0000: ldc.i4.0
                IL_0001: ret
            }

            .property int32 P1()
            {
                .get int32 E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'/'<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_P1()
            }
        }

        .method public hidebysig static void M () cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            IL_0000: ldnull
            IL_0001: throw
        }

        .method public hidebysig specialname static int32 get_P2 () cil managed
        {
            IL_0000: ldc.i4.0
            IL_0001: ret
        }

        .property int32 P2()
        {
            .get int32 E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_P2()
        }
    }

    .method public hidebysig static void M () cil managed
    {
        IL_0000: ret
    }

    .method public hidebysig static int32 get_P1 () cil managed
    {
        IL_0000: ldc.i4.0
        IL_0001: ret
    }

    .method public hidebysig static int32 get_P2 () cil managed
    {
        IL_0000: ldc.i4.0
        IL_0001: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
_ = int.P1;
_ = int.P2;
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (2,9): error CS0117: 'int' does not contain a definition for 'P1'
            // _ = int.P1;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P1").WithArguments("int", "P1").WithLocation(2, 9),
            // (3,9): error CS0117: 'int' does not contain a definition for 'P2'
            // _ = int.P2;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "P2").WithArguments("int", "P2").WithLocation(3, 9));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal(["void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()"], extension.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void PENamedTypeSymbol_33()
    {
        // event from grouping or marker type not loaded
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }

            .method public hidebysig specialname static void add_Event1 ( class [mscorlib]System.Action 'value' ) cil managed
            {
                ret
            }

            .method public hidebysig specialname static void remove_Event1 ( class [mscorlib]System.Action 'value' ) cil managed
            {
                ret
            }

            .event [mscorlib]System.Action Event1
            {
                .addon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'/'<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::add_Event1(class [mscorlib]System.Action)
                .removeon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'/'<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::remove_Event1(class [mscorlib]System.Action)
            }
        }

        .method public hidebysig static void M () cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            ldnull
            throw
        }

        .method public hidebysig specialname static void add_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
        {
            ret
        }

        .method public hidebysig specialname static void remove_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
        {
            ret
        }

        .event [mscorlib]System.Action Event2
        {
            .addon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::add_Event2(class [mscorlib]System.Action)
            .removeon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::remove_Event2(class [mscorlib]System.Action)
        }
    }

    .method public hidebysig static void M () cil managed
    {
        ret
    }

    .method public hidebysig specialname static void add_Event1 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }

    .method public hidebysig specialname static void remove_Event1 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }

    .method public hidebysig specialname static void add_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }

    .method public hidebysig specialname static void remove_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
int.Event1 += (System.Action)null;
int.Event2 += (System.Action)null;
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (2,5): error CS0117: 'int' does not contain a definition for 'Event1'
            // int.Event1 += (System.Action)null;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Event1").WithArguments("int", "Event1").WithLocation(2, 5),
            // (3,5): error CS0117: 'int' does not contain a definition for 'Event2'
            // int.Event2 += (System.Action)null;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Event2").WithArguments("int", "Event2").WithLocation(3, 5));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal(["void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()"], extension.GetMembers().ToTestDisplayStrings());
    }

    [Fact]
    public void PENamedTypeSymbol_34()
    {
        // event from grouping type not loaded, even with ExtensionMarkerAttribute
        // Note: the grouping and marker types and attributes use a previous naming convention (which doesn't affect metadata loading)
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<Marker>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }

        .method public hidebysig static void M () cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            ldnull
            throw
        }

        .method public hidebysig specialname static void add_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            ret
        }

        .method public hidebysig specialname static void remove_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            ret
        }

        .event [mscorlib]System.Action Event2
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 29 3c 4d 61 72 6b 65 72 3e 24 42 41 34 31
                43 46 45 32 42 35 45 44 41 45 42 38 43 31 42 39
                30 36 32 46 35 39 45 44 34 44 36 39 00 00
            )
            .addon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::add_Event2(class [mscorlib]System.Action)
            .removeon void E/'<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::remove_Event2(class [mscorlib]System.Action)
        }
    }

    .method public hidebysig static void M () cil managed
    {
        ret
    }

    .method public hidebysig specialname static void add_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }

    .method public hidebysig specialname static void remove_Event2 ( class [mscorlib]System.Action 'value' ) cil managed
    {
        ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
int.M();
int.Event2 += (System.Action)null;
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (2,5): error CS0117: 'int' does not contain a definition for 'Event2'
            // int.Event2 += (System.Action)null;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "Event2").WithArguments("int", "Event2").WithLocation(2, 5));

        var extension = (PENamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.IsExtension);
        AssertEx.Equal([
            "void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()",
            "void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.add_Event2(System.Action value)",
            "void E.<Extension>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.remove_Event2(System.Action value)"
            ], extension.GetMembers().ToTestDisplayStrings());
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>

""", e.GetDocumentationCommentXml());

        AssertEx.Equal("T:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.<M>$F4B4FFE41AB49E80A4ECF390CF6EB372",
            e.GetTypeMembers().Single().GetDocumentationCommentId());

        var mSkeleton = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single().GetMember("M");
        AssertEx.Equal("""
<member name="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)">
    <see cref="M:E.M(System.Int32,System.String)"/>
    <see cref="!:M(string)"/>
    <see cref="M:E.M(System.Int32,System.String)"/>
</member>

""", mSkeleton.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).M(string), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String s))",
            "(E.M(int, string), void E.M(this System.Int32 i, System.String s))",
            "(E.extension(int).M, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String s))",
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
    <see cref="M:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB.M``1(``0)"/>
</member>

""", e.GetDocumentationCommentXml());

        AssertEx.Equal("T:E.<G>$8048A6C8BE30A622530249B904B537EB.<M>$D1693D81A12E8DED4ED68FE22D9E856F",
            e.GetTypeMembers().Single().GetDocumentationCommentId());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension{T}(T).M{U}(U), void E.<G>$8048A6C8BE30A622530249B904B537EB<T>.M<U>(U u))"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
    <see cref="!:E.extension(int).M()"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(ref int).M(), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())",
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M(), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M(), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int, int).M(), null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(.M(), null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension().M(), null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int, int).M(), null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="P:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).P, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P { get; })"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="P:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).P, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P { get; })"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="P:E.&lt;G&gt;$34505F560D9EACF86A87F3ED1F85E448.P"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(string).P, System.String E.<G>$34505F560D9EACF86A87F3ED1F85E448.P { get; })"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(string).M, null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M``1"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M<T>())"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M``1"/>
    <see cref="M:E.M``1(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(E.extension(int).M{U}, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M<U>())",
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
        AssertEx.SequenceEqual(["(E.extension(int).M, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension{T}(int).M, null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$B8D310208B4544F25EEBACB9990FC73B.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension{T}(int).M, void E.<G>$B8D310208B4544F25EEBACB9990FC73B<T>.M())"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.M, null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$8048A6C8BE30A622530249B904B537EB.M"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension{U}(U).M, void E.<G>$8048A6C8BE30A622530249B904B537EB<U>.M())"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(string).P, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(string), void E.extension(System.String s))"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int)., null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int).Nested, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int).M, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(missing).M, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension, E.extension)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(object).M, null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$C43E2675C7BBF9284AF22FB8A9BF0280.M"/>
</member>

""", c.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(object).M, void E.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.M())"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(object).M, null)"], PrintXmlCrefSymbols(tree, model));
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M2(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M, null)", "(E.extension(int).M2, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.Equal("M:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M", mSkeleton.GetDocumentationCommentId());
        AssertEx.Equal("M:E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M", mSkeleton.GetDocumentationCommentXml());
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
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
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M"/>
    <see cref="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M2(System.Int32)"/>
</member>

""", e.GetDocumentationCommentXml());

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.SequenceEqual(["(E.extension(int).M, null)", "(E.extension(int).M2, null)"], PrintXmlCrefSymbols(tree, model));
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
        AssertEx.SequenceEqual(["(E.extension(int).@M, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"], PrintXmlCrefSymbols(tree, model));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78967")]
    public void Cref_52()
    {
        // unqualified extension block
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

    /// <see cref="extension(int).Method"/>
    /// <see cref="extension(int).Property"/>
    public static void M2() { }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension(int).Method, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Method())",
            "(extension(int).Property, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Property { get; })",
            "(extension(int).Method, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Method())",
            "(extension(int).Property, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Property { get; })",
            "(extension(int).Method, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Method())",
            "(extension(int).Property, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Property { get; })",
            "(extension(int).Method, void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Method())",
            "(extension(int).Property, System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.Property { get; })"],
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
<member name="M:E.&lt;G&gt;$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M(System.String)">
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
            // (1,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
            // /// <see cref="E.extension(int).M"/>
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "extension(int).M").WithArguments("extensions", "14.0").WithLocation(1, 18));

        comp = CreateCompilation(src, references: [libRef], parseOptions: TestOptions.Regular14.WithDocumentationMode(DocumentationMode.Diagnose));
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
    public void Cref_56()
    {
        var src = """
/// <see cref="E.extension(int).M()"/>
static class E
{
    extension(ref int i)
    {
        public static void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int).M()' that could not be resolved
            // /// <see cref="E.extension(int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int).M()").WithArguments("extension(int).M()").WithLocation(1, 16));

        var src2 = """
/// <see cref="E.extension(ref int).M()"/>
static class E
{
    extension(int i)
    {
        public static void M() => throw null!;
    }
}
""";
        var comp2 = CreateCompilation(src2, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp2.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(ref int).M()' that could not be resolved
            // /// <see cref="E.extension(ref int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(ref int).M()").WithArguments("extension(ref int).M()").WithLocation(1, 16));

        var extension = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        var extension2 = comp2.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        Assert.True(extension.ExtensionGroupingName == extension2.ExtensionGroupingName);
    }

    [Fact]
    public void Cref_57()
    {
        var src = """
/// <see cref="E.extension(int)"/>
static class E
{
    extension(int)
    {
        public static void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension(int)' that could not be resolved
            // /// <see cref="E.extension(int)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(int)").WithArguments("extension(int)").WithLocation(1, 16));
    }

    [Fact]
    public void Cref_58()
    {
        var src = """
static class E
{
    extension(int)
    {
        /// <see cref="extension(int).M()"/>
        public static void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension(int).M(), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M())"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_59()
    {
        var src = """
static class E
{
    /// <see cref="extension{U}(U).M()"/>
    extension<T>(T)
    {
        /// <see cref="extension{V}(V).M()"/>
        public static void M() => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension{U}(U).M(), void E.<G>$8048A6C8BE30A622530249B904B537EB<U>.M())",
            "(extension{V}(V).M(), void E.<G>$8048A6C8BE30A622530249B904B537EB<V>.M())"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_60()
    {
        var src = """
static class E
{
    /// <see cref="extension{U}(int).M(U)"/>
    extension<T>(int)
    {
        /// <see cref="extension{V}(int).M(V)"/>
        public static void M(T t) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension{U}(int).M(U), void E.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.M(U t))",
            "(extension{V}(int).M(V), void E.<G>$B8D310208B4544F25EEBACB9990FC73B<V>.M(V t))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_61()
    {
        // in namespace
        var src = """
namespace E
{
    extension<T>(int)
    {
        /// <see cref="extension{U}(int).M(U)"/>
        public static void M(T t) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension<T>(int)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5),
            // (3,5): warning CS1591: Missing XML comment for publicly visible type or member 'extension<T>(int)'
            //     extension<T>(int)
            Diagnostic(ErrorCode.WRN_MissingXMLComment, "extension").WithArguments("E.extension<T>(int)").WithLocation(3, 5),
            // (5,24): warning CS1574: XML comment has cref attribute 'extension{U}(int).M(U)' that could not be resolved
            //         /// <see cref="extension{U}(int).M(U)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension{U}(int).M(U)").WithArguments("extension{U}(int).M(U)").WithLocation(5, 24));
    }

    [Fact]
    public void Cref_62()
    {
        // top-level extension block
        var src = """
/// <see cref="extension{U}(int).M(U)"/>
extension<T>(int)
{
    /// <see cref="extension{U}(int).M(U)"/>
    public static void M(T t) => throw null!;
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (1,16): warning CS1574: XML comment has cref attribute 'extension{U}(int).M(U)' that could not be resolved
            // /// <see cref="extension{U}(int).M(U)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension{U}(int).M(U)").WithArguments("extension{U}(int).M(U)").WithLocation(1, 16),
            // (2,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension<T>(int)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(2, 1),
            // (4,20): warning CS1574: XML comment has cref attribute 'extension{U}(int).M(U)' that could not be resolved
            //     /// <see cref="extension{U}(int).M(U)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension{U}(int).M(U)").WithArguments("extension{U}(int).M(U)").WithLocation(4, 20));
    }

    [Fact]
    public void Cref_63()
    {
        var src = """
static class E
{
    extension(object)
    {
        public static void M1() => throw null!;

        extension<T>(int)
        {
            /// <see cref="extension{U}(int).M(U)"/>
            /// <see cref="extension(object).M1()"/>
            public static void M2(T t) => throw null!;
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (7,9): error CS9282: This member is not allowed in an extension block
            //         extension<T>(int)
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "extension").WithLocation(7, 9),
            // (9,28): warning CS1574: XML comment has cref attribute 'extension{U}(int).M(U)' that could not be resolved
            //             /// <see cref="extension{U}(int).M(U)"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension{U}(int).M(U)").WithArguments("extension{U}(int).M(U)").WithLocation(9, 28),
            // (10,28): warning CS1574: XML comment has cref attribute 'extension(object).M1()' that could not be resolved
            //             /// <see cref="extension(object).M1()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(object).M1()").WithArguments("extension(object).M1()").WithLocation(10, 28));
    }

    [Fact]
    public void Cref_64()
    {
        // generic static enclosing class
        var src = """
static class E<T0>
{
    /// <see cref="extension{U}(int).M(U)"/>
    extension<T>(int)
    {
        /// <see cref="extension{U}(int).M(U)"/>
        public static void M(T t) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension<T>(int)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(4, 5));

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension{U}(int).M(U), void E<T0>.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.M(U t))",
            "(extension{U}(int).M(U), void E<T0>.<G>$B8D310208B4544F25EEBACB9990FC73B<U>.M(U t))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_65()
    {
        // non-static enclosing class
        var src = """
class E
{
    /// <see cref="extension{U}(int).M(U)"/>
    extension<T>(int)
    {
        /// <see cref="extension{U}(int).M(U)"/>
        public static void M(T t) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension<T>(int)
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(4, 5));
    }

    [Fact]
    public void Cref_66()
    {
        var src = """
static class E
{
    /// <see cref="extension(int).M{U}(U)"/>
    extension(int)
    {
        /// <see cref="extension(int).M{U}(U)"/>
        public static void M<T>(T t) => throw null!;
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics();

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        AssertEx.Equal([
            "(extension(int).M{U}(U), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M<U>(U t))",
            "(extension(int).M{U}(U), void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M<U>(U t))"],
            PrintXmlCrefSymbols(tree, model));
    }

    [Fact]
    public void Cref_67()
    {
        var src = """
static class E
{
    extension(int)
    {
        public static void M() => throw null!;

        /// <see cref="extension(int).M()"/>
        class Nested
        {
            /// <see cref="extension(int).M()"/>
            public static void Method() => throw null!;
        }
    }
}
""";
        var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
        comp.VerifyEmitDiagnostics(
            // (7,24): warning CS1574: XML comment has cref attribute 'extension(int).M()' that could not be resolved
            //         /// <see cref="extension(int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).M()").WithArguments("extension(int).M()").WithLocation(7, 24),
            // (8,15): error CS9282: This member is not allowed in an extension block
            //         class Nested
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "Nested").WithLocation(8, 15),
            // (10,28): warning CS1574: XML comment has cref attribute 'extension(int).M()' that could not be resolved
            //             /// <see cref="extension(int).M()"/>
            Diagnostic(ErrorCode.WRN_BadXMLRef, "extension(int).M()").WithArguments("extension(int).M()").WithLocation(10, 28));
    }

    [Fact]
    public void PropertyAccess_Set_01()
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
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

public struct S1
{
    public int F1;

    public void Test()
    {
        this.P1 = Program.Get1();
    }

    public int P2
    {
        get
        {
            return 0;
        }
        set
        {
            System.Console.Write(F1);
        }
    }

    public void Test2()
    {
        this.P2 = Program.Get1();
    }

    public void Test3()
    {
        E.set_P1(this, Program.Get1()); 
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

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test2();
        System.Console.Write(F.F1);

        System.Console.Write(":");

        F = new S1 { F1 = 123 };
        F.Test3();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        F.P1 = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124:124124:123124", verify: Verification.Skipped).VerifyDiagnostics();

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
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  ldloc.0
  IL_000e:  call       ""void E.set_P1(S1, int)""
  IL_0013:  nop
  IL_0014:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension(S1 x)
    {
        public int P1 { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1).P1 = 1;
    }
}
""";

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1).P1 = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
            );
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_Set_02(string refKind)
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
        this.P1 = Program.Get1();
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
        F.P1 = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_0010:  nop
  IL_0011:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""void E.set_P1(" + refKind + @" S1, int)""
  IL_000c:  nop
  IL_000d:  ret
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
        default(S1).P1 = 1;
    }
}
""";

        var comp2 = CreateCompilation([src2]);
        switch (refKind)
        {
            case "ref":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS1510: A ref or out value must be an assignable variable
                    //         default(S1).P1 = 1;
                    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "default(S1)").WithLocation(15, 9)
                    );
                break;
            case "ref readonly":
                comp2.VerifyDiagnostics(
                    // (15,9): warning CS9193: Argument 0 should be a variable because it is passed to a 'ref readonly' parameter
                    //         default(S1).P1 = 1;
                    Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "default(S1)").WithArguments("0").WithLocation(15, 9),
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 = 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            case "in":
                comp2.VerifyDiagnostics(
                    // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                    //         default(S1).P1 = 1;
                    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1).P1").WithLocation(15, 9)
                    );
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(refKind);
        }
    }

    [Fact]
    public void PropertyAccess_Set_03()
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
        F.P1 = Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""void E.set_P1(C1, int)""
  IL_0010:  nop
  IL_0011:  ret
}
");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void PropertyAccess_Set_04()
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<S1>.F = new S1 { F1 = 123 };
        //await Test3<S1>();
        //System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1 = Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f.P1 = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F.P1 = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<S1>.F.F1++;
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
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

        var src2 = """
static class E
{
    extension<T>(T x) where T : struct
    {
        public int P1 { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T).P1 = 1;
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

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T).P1 = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T).P1").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact]
    public void PropertyAccess_Set_05()
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
        f.P1 = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F.P1 = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""void E.set_P1<T>(ref T, int)""
  IL_000c:  nop
  IL_000d:  ret
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
        default(T).P1 = 1;
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

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS1510: A ref or out value must be an assignable variable
            //         default(T).P1 = 1;
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
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void PropertyAccess_Set_06()
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<C1>.F = new C1 { F1 = 123 };
        //await Test3<C1>();
        //System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f.P1 = Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f.P1 = Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F.P1 = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124:123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
  IL_002b:  call       ""void E.set_P1<T>(T, int)""
  IL_0030:  nop
  IL_0031:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  call       ""void E.set_P1<T>(T, int)""
  IL_0011:  nop
  IL_0012:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_Set_07()
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

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>().P1 = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>().P1 = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""void E.set_P1<T>(T, int)""
  IL_0010:  nop
  IL_0011:  ret
}
");
    }

    [Fact]
    public void PropertyAccess_Set_08()
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

class Program
{
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>().P1 = Get1();
    }

    static void Test2<T>() where T : class
    {
        GetT<T>().P1 = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>().P1 = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""void E.set_P1<T>(T, int)""
  IL_0010:  nop
  IL_0011:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""void E.set_P1<T>(T, int)""
  IL_0010:  nop
  IL_0011:  ret
}
");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void PropertyAccess_Set_ReadonlyReceiver_040()
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
                Program.Increment();
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
    public static readonly T F;
}

class Program
{
    static async Task Main()
    {
        Initialize();
        Test1<S1>();
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Initialize();
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static unsafe void Initialize()
    {
        fixed (int* f1 = &Program<S1>.F.F1)
        {
            *f1 = 123;
        }
    }

    public static unsafe void Increment()
    {
        fixed (int* f1 = &Program<S1>.F.F1)
        {
            (*f1)++;
        }
    }

    static void Test1<T>()
    {
        Program<T>.F.P1 = Get1();
    }

    static int Get1()
    {
        Increment();
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F.P1 = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Increment();
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe.WithAllowUnsafe(true));
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0,
            T& V_1,
            int V_2,
            T V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""T Program<T>.F""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_3
  IL_0009:  initobj    ""T""
  IL_000f:  ldloc.3
  IL_0010:  box        ""T""
  IL_0015:  brtrue.s   IL_0022
  IL_0017:  ldloc.1
  IL_0018:  ldobj      ""T""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.1
  IL_0023:  call       ""int Program.Get1()""
  IL_0028:  stloc.2
  IL_0029:  ldobj      ""T""
  IL_002e:  ldloc.2
  IL_002f:  call       ""void E.set_P1<T>(T, int)""
  IL_0034:  nop
  IL_0035:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_Set_ReadonlyReceiver_041(string refKind)
    {
        var src = $$$"""
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
    static void Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1({{{(refKind == "ref" ? "ref" : "in")}}} Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>({{{refKind}}} T f)
    {
        f.P1 = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124").VerifyDiagnostics();

        verifier.VerifyIL($"Program.Test1<T>({refKind} T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
  IL_002b:  call       ""void E.set_P1<T>(T, int)""
  IL_0030:  nop
  IL_0031:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_Set_ReadonlyReceiver_061(string refKind)
    {
        var src = $$$"""
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
    static void Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1({{{(refKind == "ref" ? "ref" : "in")}}} Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>({{{refKind}}} T f)
    {
        f.P1 = Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124").VerifyDiagnostics();

        verifier.VerifyIL($"Program.Test1<T>({refKind} T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
  IL_002b:  call       ""void E.set_P1<T>(T, int)""
  IL_0030:  nop
  IL_0031:  ret
}
");
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
    public void PropertyAccess_CompoundAssignment_ReadonlyReceiver_040()
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
                Program.Increment();
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
    public static readonly T F;
}

class Program
{
    static async Task Main()
    {
        Initialize();
        Test1<S1>();
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Initialize();
        await Test3<S1>();
        System.Console.Write(Program<S1>.F.F1);
    }

    static unsafe void Initialize()
    {
        fixed (int* f1 = &Program<S1>.F.F1)
        {
            *f1 = 123;
        }
    }

    public static unsafe void Increment()
    {
        fixed (int* f1 = &Program<S1>.F.F1)
        {
            (*f1)++;
        }
    }

    static void Test1<T>()
    {
        Program<T>.F.P1 += Get1();
    }

    static int Get1()
    {
        Increment();
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F.P1 += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Increment();
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125:123125125", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       66 (0x42)
  .maxstack  3
  .locals init (T V_0,
            T& V_1,
            T V_2,
            int V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""T Program<T>.F""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_2
  IL_0009:  initobj    ""T""
  IL_000f:  ldloc.2
  IL_0010:  box        ""T""
  IL_0015:  brtrue.s   IL_0022
  IL_0017:  ldloc.1
  IL_0018:  ldobj      ""T""
  IL_001d:  stloc.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.1
  IL_0023:  dup
  IL_0024:  ldobj      ""T""
  IL_0029:  call       ""int E.get_P1<T>(T)""
  IL_002e:  call       ""int Program.Get1()""
  IL_0033:  add
  IL_0034:  stloc.3
  IL_0035:  ldobj      ""T""
  IL_003a:  ldloc.3
  IL_003b:  call       ""void E.set_P1<T>(T, int)""
  IL_0040:  nop
  IL_0041:  ret
}
");
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_CompoundAssignment_ReadonlyReceiver_041(string refKind)
    {
        var src = $$$"""
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
    static void Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1({{{(refKind == "ref" ? "ref" : "in")}}} Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>({{{refKind}}} T f)
    {
        f.P1 += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125125").VerifyDiagnostics();

        verifier.VerifyIL($"Program.Test1<T>({refKind} T)",
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
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void PropertyAccess_CompoundAssignment_ReadonlyReceiver_061(string refKind)
    {
        var src = $$$"""
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
    static void Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1({{{(refKind == "ref" ? "ref" : "in")}}} Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>({{{refKind}}} T f)
    {
        f.P1 += Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123125").VerifyDiagnostics();

        verifier.VerifyIL($"Program.Test1<T>({refKind} T)",
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

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int this[int i]
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
        this[Program.Get2()] += Program.Get1();
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
        F[Get2()] += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }

    public static int Get2()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get2()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldobj      ""S1""
  IL_0012:  ldloc.0
  IL_0013:  call       ""int E.get_Item(S1, int)""
  IL_0018:  call       ""int Program.Get1()""
  IL_001d:  add
  IL_001e:  stloc.1
  IL_001f:  ldobj      ""S1""
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  call       ""void E.set_Item(S1, int, int)""
  IL_002b:  nop
  IL_002c:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  call       ""int Program.Get2()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  ldloc.0
  IL_000e:  call       ""int E.get_Item(S1, int)""
  IL_0013:  call       ""int Program.Get1()""
  IL_0018:  add
  IL_0019:  stloc.1
  IL_001a:  ldarg.0
  IL_001b:  ldobj      ""S1""
  IL_0020:  ldloc.0
  IL_0021:  ldloc.1
  IL_0022:  call       ""void E.set_Item(S1, int, int)""
  IL_0027:  nop
  IL_0028:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension(S1 x)
    {
        public int this[int i] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0] += 1;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1)[0]").WithLocation(15, 9)
            );
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_CompoundAssignment_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i]
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
        this[Program.Get2()] += Program.Get1();
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
        F[Get2()] += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }

    public static int Get2()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (S1& V_0,
            int V_1)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get2()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  call       ""int E.get_Item(" + refKind + @" S1, int)""
  IL_0016:  call       ""int Program.Get1()""
  IL_001b:  add
  IL_001c:  call       ""void E.set_Item(" + refKind + @" S1, int, int)""
  IL_0021:  nop
  IL_0022:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""int Program.Get2()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  call       ""int E.get_Item(" + refKind + @" S1, int)""
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  add
  IL_0016:  call       ""void E.set_Item(" + refKind + @" S1, int, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0] += 1;
    }
}
""";

        var comp2 = CreateCompilation(src2);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1)[0]").WithLocation(15, 9)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int this[int i]
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
        F[Get2()] += Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }

    static int Get2()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (C1 V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get2()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  call       ""int E.get_Item(C1, int)""
  IL_0016:  call       ""int Program.Get1()""
  IL_001b:  add
  IL_001c:  call       ""void E.set_Item(C1, int, int)""
  IL_0021:  nop
  IL_0022:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
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
        f[0] += Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f[0] += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F[0] += await Get1Async();
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
  // Code size       64 (0x40)
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
  IL_0025:  ldc.i4.0
  IL_0026:  call       ""int E.get_Item<T>(T, int)""
  IL_002b:  call       ""int Program.Get1()""
  IL_0030:  add
  IL_0031:  stloc.3
  IL_0032:  ldobj      ""T""
  IL_0037:  ldc.i4.0
  IL_0038:  ldloc.3
  IL_0039:  call       ""void E.set_Item<T>(T, int, int)""
  IL_003e:  nop
  IL_003f:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  ldobj      ""T""
  IL_0008:  ldc.i4.0
  IL_0009:  call       ""int E.get_Item<T>(T, int)""
  IL_000e:  call       ""int Program.Get1()""
  IL_0013:  add
  IL_0014:  stloc.0
  IL_0015:  ldobj      ""T""
  IL_001a:  ldc.i4.0
  IL_001b:  ldloc.0
  IL_001c:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0021:  nop
  IL_0022:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(T x) where T : struct
    {
        public int this[int i] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0] += 1;
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
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T)[0]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] 
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
        f[0] += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F[0] += await Get1Async();
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
  // Code size       25 (0x19)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""int E.get_Item<T>(ref T, int)""
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  add
  IL_0012:  call       ""void E.set_Item<T>(ref T, int, int)""
  IL_0017:  nop
  IL_0018:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0] += 1;
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
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T)[0]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
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
        f[0] += Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f[0] += Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F[0] += await Get1Async();
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
  // Code size       64 (0x40)
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
  IL_0025:  ldc.i4.0
  IL_0026:  call       ""int E.get_Item<T>(T, int)""
  IL_002b:  call       ""int Program.Get1()""
  IL_0030:  add
  IL_0031:  stloc.3
  IL_0032:  ldobj      ""T""
  IL_0037:  ldc.i4.0
  IL_0038:  ldloc.3
  IL_0039:  call       ""void E.set_Item<T>(T, int, int)""
  IL_003e:  nop
  IL_003f:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  call       ""int E.get_Item<T>(T, int)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  add
  IL_0017:  call       ""void E.set_Item<T>(T, int, int)""
  IL_001c:  nop
  IL_001d:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_07()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[0] += Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[0] += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""int E.get_Item<T>(T, int)""
  IL_000d:  call       ""int Program.Get1()""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldloc.0
  IL_0016:  call       ""void E.set_Item<T>(T, int, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_08()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[0] += Get1();
    }

    static void Test2<T>() where T : class
    {
        GetT<T>()[0] += Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[0] += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""int E.get_Item<T>(T, int)""
  IL_000d:  call       ""int Program.Get1()""
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldloc.0
  IL_0016:  call       ""void E.set_Item<T>(T, int, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""int E.get_Item<T>(T, int)""
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  add
  IL_0016:  call       ""void E.set_Item<T>(T, int, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_01()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
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

public struct S1
{
    public int F1;

    public void Test()
    {
        this[Program.Get1(), $""] += Program.Get1();
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
        F[Program.Get1(), $""] += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124125127127:124125127127", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (S1& V_0,
            int V_1,
            InterpolationHandler V_2,
            int V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  ldobj      ""S1""
  IL_0015:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_001a:  stloc.2
  IL_001b:  ldloc.0
  IL_001c:  ldobj      ""S1""
  IL_0021:  ldloc.1
  IL_0022:  ldloc.2
  IL_0023:  call       ""int E.get_Item(S1, int, InterpolationHandler)""
  IL_0028:  call       ""int Program.Get1()""
  IL_002d:  add
  IL_002e:  stloc.3
  IL_002f:  ldloc.0
  IL_0030:  ldobj      ""S1""
  IL_0035:  ldloc.1
  IL_0036:  ldloc.2
  IL_0037:  ldloc.3
  IL_0038:  call       ""void E.set_Item(S1, int, InterpolationHandler, int)""
  IL_003d:  nop
  IL_003e:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       59 (0x3b)
  .maxstack  4
  .locals init (S1& V_0,
            int V_1,
            InterpolationHandler V_2,
            int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""S1""
  IL_0011:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_0016:  stloc.2
  IL_0017:  ldloc.0
  IL_0018:  ldobj      ""S1""
  IL_001d:  ldloc.1
  IL_001e:  ldloc.2
  IL_001f:  call       ""int E.get_Item(S1, int, InterpolationHandler)""
  IL_0024:  call       ""int Program.Get1()""
  IL_0029:  add
  IL_002a:  stloc.3
  IL_002b:  ldloc.0
  IL_002c:  ldobj      ""S1""
  IL_0031:  ldloc.1
  IL_0032:  ldloc.2
  IL_0033:  ldloc.3
  IL_0034:  call       ""void E.set_Item(S1, int, InterpolationHandler, int)""
  IL_0039:  nop
  IL_003a:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0, $""] += 1;
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(S1)[0, $""""]").WithLocation(15, 9)
            );
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_02(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
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
        this[Program.Get1(), $""] += Program.Get1();
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
        F[Program.Get1(), $""] += Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124125127127:124125127127").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       46 (0x2e)
  .maxstack  6
  .locals init (S1& V_0,
            int V_1,
            InterpolationHandler V_2)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  ldloc.2
  IL_001c:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler)""
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  add
  IL_0027:  call       ""void E.set_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_002c:  nop
  IL_002d:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       42 (0x2a)
  .maxstack  6
  .locals init (S1& V_0,
                int V_1,
                InterpolationHandler V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0011:  stloc.2
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  ldloc.2
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldloc.2
  IL_0018:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler)""
  IL_001d:  call       ""int Program.Get1()""
  IL_0022:  add
  IL_0023:  call       ""void E.set_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_0028:  nop
  IL_0029:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0, $""] += 1;
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(S1)[0, $""""]").WithLocation(15, 9)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_03()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C1 x)
    {
        System.Console.Write(x.F1);
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(C1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
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
        F[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123123127").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       46 (0x2e)
  .maxstack  6
  .locals init (C1 V_0,
            int V_1,
            InterpolationHandler V_2)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, C1)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  ldloc.2
  IL_001c:  call       ""int E.get_Item(C1, int, InterpolationHandler)""
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  add
  IL_0027:  call       ""void E.set_Item(C1, int, InterpolationHandler, int)""
  IL_002c:  nop
  IL_002d:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_04()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
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
        f[Get1(), $""] += Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F[Get1(), $""] += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124125127127:124125127127:124125127127").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       94 (0x5e)
  .maxstack  4
  .locals init (T& V_0,
            T V_1,
            T& V_2,
            int V_3,
            InterpolationHandler<T> V_4,
            T V_5,
            int V_6)
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
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.0
  IL_0037:  ldobj      ""T""
  IL_003c:  ldloc.3
  IL_003d:  ldloc.s    V_4
  IL_003f:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_0044:  call       ""int Program.Get1()""
  IL_0049:  add
  IL_004a:  stloc.s    V_6
  IL_004c:  ldloc.0
  IL_004d:  ldobj      ""T""
  IL_0052:  ldloc.3
  IL_0053:  ldloc.s    V_4
  IL_0055:  ldloc.s    V_6
  IL_0057:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_005c:  nop
  IL_005d:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       59 (0x3b)
  .maxstack  4
  .locals init (T& V_0,
            int V_1,
            InterpolationHandler<T> V_2,
            int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""T""
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  stloc.2
  IL_0017:  ldloc.0
  IL_0018:  ldobj      ""T""
  IL_001d:  ldloc.1
  IL_001e:  ldloc.2
  IL_001f:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_0024:  call       ""int Program.Get1()""
  IL_0029:  add
  IL_002a:  stloc.3
  IL_002b:  ldloc.0
  IL_002c:  ldobj      ""T""
  IL_0031:  ldloc.1
  IL_0032:  ldloc.2
  IL_0033:  ldloc.3
  IL_0034:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_0039:  nop
  IL_003a:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0, $""] += 1;
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

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(T)[0, $""""]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_05()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] 
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
        f[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F[Get1(), $""] += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124125127127:124125127127").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       42 (0x2a)
  .maxstack  6
  .locals init (T& V_0,
            int V_1,
            InterpolationHandler<T> V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler<T>..ctor(int, int, ref T)""
  IL_0011:  stloc.2
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  ldloc.2
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldloc.2
  IL_0018:  call       ""int E.get_Item<T>(ref T, int, InterpolationHandler<T>)""
  IL_001d:  call       ""int Program.Get1()""
  IL_0022:  add
  IL_0023:  call       ""void E.set_Item<T>(ref T, int, InterpolationHandler<T>, int)""
  IL_0028:  nop
  IL_0029:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0, $""] += 1;
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

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(T)[0, $""""]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_06()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
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
        f[Get1(), $""] += Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    static async Task Test3<T>()
    {
        Program<T>.F[Get1(), $""] += await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123123127:123123123127:123123123127").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       94 (0x5e)
  .maxstack  4
  .locals init (T& V_0,
            T V_1,
            T& V_2,
            int V_3,
            InterpolationHandler<T> V_4,
            T V_5,
            int V_6)
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
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  ldloc.0
  IL_0037:  ldobj      ""T""
  IL_003c:  ldloc.3
  IL_003d:  ldloc.s    V_4
  IL_003f:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_0044:  call       ""int Program.Get1()""
  IL_0049:  add
  IL_004a:  stloc.s    V_6
  IL_004c:  ldloc.0
  IL_004d:  ldobj      ""T""
  IL_0052:  ldloc.3
  IL_0053:  ldloc.s    V_4
  IL_0055:  ldloc.s    V_6
  IL_0057:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_005c:  nop
  IL_005d:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       47 (0x2f)
  .maxstack  6
  .locals init (T V_0,
            int V_1,
            InterpolationHandler<T> V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloc.0
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  stloc.2
  IL_0017:  ldloc.0
  IL_0018:  ldloc.1
  IL_0019:  ldloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldloc.1
  IL_001c:  ldloc.2
  IL_001d:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_0022:  call       ""int Program.Get1()""
  IL_0027:  add
  IL_0028:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002d:  nop
  IL_002e:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79415")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_07()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
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

class Program
{
// https://github.com/dotnet/roslyn/issues/79415 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Test1<S1>();

        // https://github.com/dotnet/roslyn/issues/79415 - uncomment the following code once fixed
        //System.Console.Write(":");

        //await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79415 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    GetT<T>()[Get1(), $""] += await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (T V_0,
                int V_1,
                InterpolationHandler<T> V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_001e:  call       ""int Program.Get1()""
  IL_0023:  add
  IL_0024:  stloc.3
  IL_0025:  ldloc.0
  IL_0026:  ldloc.1
  IL_0027:  ldloc.2
  IL_0028:  ldloc.3
  IL_0029:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002e:  nop
  IL_002f:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79415")]
    public void IndexerAccess_CompoundAssignment_WithInterpolationHandler_08()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
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

class Program
{
// https://github.com/dotnet/roslyn/issues/79415 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        // https://github.com/dotnet/roslyn/issues/79415 - uncomment the following code once fixed
        //System.Console.Write(":");

        //await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1(), $""] += Get1();
    }

    static void Test2<T>() where T : class
    {
        GetT<T>()[Get1(), $""] += Get1();
    }

    static int Get1()
    {
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79415 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    GetT<T>()[Get1(), $""] += await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123123:123123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (T V_0,
                int V_1,
                InterpolationHandler<T> V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_001e:  call       ""int Program.Get1()""
  IL_0023:  add
  IL_0024:  stloc.3
  IL_0025:  ldloc.0
  IL_0026:  ldloc.1
  IL_0027:  ldloc.2
  IL_0028:  ldloc.3
  IL_0029:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002e:  nop
  IL_002f:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       46 (0x2e)
  .maxstack  6
  .locals init (T V_0,
                int V_1,
                InterpolationHandler<T> V_2)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  ldloc.2
  IL_001c:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>)""
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  add
  IL_0027:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002c:  nop
  IL_002d:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_WithInterpolationHandler_01()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
        {
            get
            {
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

public struct S1
{
    public int F1;

    public void Test()
    {
        this[Program.Get1(), $""] = Program.Get1();
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
        F[Program.Get1(), $""] = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (S1& V_0,
                int V_1,
                InterpolationHandler V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  ldobj      ""S1""
  IL_0015:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_001a:  stloc.2
  IL_001b:  call       ""int Program.Get1()""
  IL_0020:  stloc.3
  IL_0021:  ldloc.0
  IL_0022:  ldobj      ""S1""
  IL_0027:  ldloc.1
  IL_0028:  ldloc.2
  IL_0029:  ldloc.3
  IL_002a:  call       ""void E.set_Item(S1, int, InterpolationHandler, int)""
  IL_002f:  nop
  IL_0030:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (S1& V_0,
            int V_1,
            InterpolationHandler V_2,
            int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""S1""
  IL_0011:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_0016:  stloc.2
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldobj      ""S1""
  IL_0023:  ldloc.1
  IL_0024:  ldloc.2
  IL_0025:  ldloc.3
  IL_0026:  call       ""void E.set_Item(S1, int, InterpolationHandler, int)""
  IL_002b:  nop
  IL_002c:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0, $""] = 1;
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(S1)[0, $""""]").WithLocation(15, 9)
            );
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Set_WithInterpolationHandler_02(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
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
        this[Program.Get1(), $""] = Program.Get1();
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
        F[Program.Get1(), $""] = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""void E.set_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       29 (0x1d)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int Program.Get1()""
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  call       ""void E.set_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0, $""] = 1;
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(S1)[0, $""""]").WithLocation(15, 9)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_WithInterpolationHandler_03()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C1 x)
    {
        System.Console.Write(x.F1);
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(C1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h]
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
        F[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (C1 V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, C1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""void E.set_Item(C1, int, InterpolationHandler, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Set_WithInterpolationHandler_04()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<S1>.F = new S1 { F1 = 123 };
        //await Test3<S1>();
        //System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f[Get1(), $""] = Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F[Get1(), $""] = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<S1>.F.F1++;
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                int V_3,
                InterpolationHandler<T> V_4,
                int V_5,
                T V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_6
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_6
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  call       ""int Program.Get1()""
  IL_003b:  stloc.s    V_5
  IL_003d:  ldloc.0
  IL_003e:  ldobj      ""T""
  IL_0043:  ldloc.3
  IL_0044:  ldloc.s    V_4
  IL_0046:  ldloc.s    V_5
  IL_0048:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_004d:  nop
  IL_004e:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                InterpolationHandler<T> V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""T""
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  stloc.2
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldobj      ""T""
  IL_0023:  ldloc.1
  IL_0024:  ldloc.2
  IL_0025:  ldloc.3
  IL_0026:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002b:  nop
  IL_002c:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0, $""] = 1;
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

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(T)[0, $""""]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_WithInterpolationHandler_05()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] 
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
        f[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F[Get1(), $""] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       29 (0x1d)
  .maxstack  5
  .locals init (T& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int Program.Get1()""
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler<T>..ctor(int, int, ref T)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  call       ""void E.set_Item<T>(ref T, int, InterpolationHandler<T>, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0, $""] = 1;
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

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0, $""] += 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, @"default(T)[0, $""""]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Set_WithInterpolationHandler_06()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<C1>.F = new C1 { F1 = 123 };
        //await Test3<C1>();
        //System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f[Get1(), $""] = Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F[Get1(), $""] = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123126:123123126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (T& V_0,
                T V_1,
                T& V_2,
                int V_3,
                InterpolationHandler<T> V_4,
                int V_5,
                T V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_6
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_6
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  call       ""int Program.Get1()""
  IL_003b:  stloc.s    V_5
  IL_003d:  ldloc.0
  IL_003e:  ldobj      ""T""
  IL_0043:  ldloc.3
  IL_0044:  ldloc.s    V_4
  IL_0046:  ldloc.s    V_5
  IL_0048:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_004d:  nop
  IL_004e:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       34 (0x22)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloc.0
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  call       ""int Program.Get1()""
  IL_001b:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_0020:  nop
  IL_0021:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_WithInterpolationHandler_07()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[Get1(), $""] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_WithInterpolationHandler_08()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1(), $""] = Get1();
    }

    static void Test2<T>() where T : class
    {
        GetT<T>()[Get1(), $""] = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[Get1(), $""] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""void E.set_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_01()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

public struct S1
{
    public int F1;

    public void Test()
    {
        _ = this[Program.Get1(), $"", Program.Get1()];
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
        _ = F[Program.Get1(), $"", Get1()];
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (S1& V_0,
                int V_1,
                InterpolationHandler V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  ldobj      ""S1""
  IL_0015:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_001a:  stloc.2
  IL_001b:  call       ""int Program.Get1()""
  IL_0020:  stloc.3
  IL_0021:  ldloc.0
  IL_0022:  ldobj      ""S1""
  IL_0027:  ldloc.1
  IL_0028:  ldloc.2
  IL_0029:  ldloc.3
  IL_002a:  call       ""int E.get_Item(S1, int, InterpolationHandler, int)""
  IL_002f:  pop
  IL_0030:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (S1& V_0,
                int V_1,
                InterpolationHandler V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""S1""
  IL_0011:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_0016:  stloc.2
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldobj      ""S1""
  IL_0023:  ldloc.1
  IL_0024:  ldloc.2
  IL_0025:  ldloc.3
  IL_0026:  call       ""int E.get_Item(S1, int, InterpolationHandler, int)""
  IL_002b:  pop
  IL_002c:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_02(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        _ = this[Program.Get1(), $"", Program.Get1()];
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
        _ = F[Program.Get1(), $"", Get1()];
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       29 (0x1d)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int Program.Get1()""
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001b:  pop
  IL_001c:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        _ = default(S1)[0, $"", 1];
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);
        // !!! Shouldn't there be a not a variable error for 'default(T)[0, $"", 1]' !!!
        comp2.VerifyDiagnostics(
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_03()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C1 x)
    {
        System.Console.Write(x.F1);
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(C1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
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
        _ = F[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (C1 V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, C1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(C1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_04()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<S1>.F = new S1 { F1 = 123 };
        //await Test3<S1>();
        //System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        _ = f[Get1(), $"", Get1()];
    }

    static void Test2<T>(ref T f) where T : struct
    {
        _ = f[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    _ = Program<T>.F[Get1(), $"", await Get1Async()];
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<S1>.F.F1++;
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (T& V_0,
            T V_1,
            T& V_2,
            int V_3,
            InterpolationHandler<T> V_4,
            int V_5,
            T V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_6
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_6
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  call       ""int Program.Get1()""
  IL_003b:  stloc.s    V_5
  IL_003d:  ldloc.0
  IL_003e:  ldobj      ""T""
  IL_0043:  ldloc.3
  IL_0044:  ldloc.s    V_4
  IL_0046:  ldloc.s    V_5
  IL_0048:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_004d:  pop
  IL_004e:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (T& V_0,
                int V_1,
                InterpolationHandler<T> V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.Get1()""
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  ldobj      ""T""
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  stloc.2
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldobj      ""T""
  IL_0023:  ldloc.1
  IL_0024:  ldloc.2
  IL_0025:  ldloc.3
  IL_0026:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_002b:  pop
  IL_002c:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_05()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        Program<S1>.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j] 
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
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
        _ = f[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        _ = Program<T>.F[Get1(), $"", await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126:124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       29 (0x1d)
  .maxstack  5
  .locals init (T& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int Program.Get1()""
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler<T>..ctor(int, int, ref T)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  call       ""int E.get_Item<T>(ref T, int, InterpolationHandler<T>, int)""
  IL_001b:  pop
  IL_001c:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        _ = default(T)[0, $"", 1];
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

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}
""";

        var comp2 = CreateCompilation([src2, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute]);

        // !!! Shouldn't there be a not a variable error for 'default(T)[0, $"", 1]' !!!
        comp2.VerifyDiagnostics(
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Get_WithInterpolationHandler_LValueReceiver_06()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                return 0;
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<C1>.F = new C1 { F1 = 123 };
        //await Test3<C1>();
        //System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        _ = f[Get1(), $"", Get1()];
    }

    static void Test2<T>(ref T f) where T : class
    {
        _ = f[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    _ = Program<T>.F[Get1(), $"", await Get1Async()];
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123126:123123126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (T& V_0,
            T V_1,
            T& V_2,
            int V_3,
            InterpolationHandler<T> V_4,
            int V_5,
            T V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloca.s   V_6
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_6
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.2
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.2
  IL_0020:  stloc.0
  IL_0021:  call       ""int Program.Get1()""
  IL_0026:  stloc.3
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.0
  IL_0029:  ldloc.0
  IL_002a:  ldobj      ""T""
  IL_002f:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0034:  stloc.s    V_4
  IL_0036:  call       ""int Program.Get1()""
  IL_003b:  stloc.s    V_5
  IL_003d:  ldloc.0
  IL_003e:  ldobj      ""T""
  IL_0043:  ldloc.3
  IL_0044:  ldloc.s    V_4
  IL_0046:  ldloc.s    V_5
  IL_0048:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_004d:  pop
  IL_004e:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       34 (0x22)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloc.0
  IL_0011:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0016:  call       ""int Program.Get1()""
  IL_001b:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_01()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, S1 x)
    {
        System.Console.Write(x.F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

public struct S1
{
    public int F1;
}

class Program
{
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetS1()[Program.Get1(), $"", Get1()];
    }

    static S1 GetS1() => new S1 { F1 = 123 };

    public static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (S1 V_0)
  IL_0000:  nop
  IL_0001:  call       ""S1 Program.GetS1()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, S1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(S1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_02(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
unsafe struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        fixed (int* f1 = &x.F1)
        {
            (*f1)++;
        }
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetS1()[Program.Get1(), $"", Get1()];
    }

    static S1 GetS1() => new S1 { F1 = 123 };

    public static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe.WithAllowUnsafe(true));
        var verifier = CompileAndVerify(comp, expectedOutput: "123124", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       35 (0x23)
  .maxstack  5
  .locals init (S1 V_0)
  IL_0000:  nop
  IL_0001:  call       ""S1 Program.GetS1()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_03()
    {
        var src = """
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, C1 x)
    {
        System.Console.Write(x.F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension(C1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
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
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetC1()[Get1(), $"", Get1()];
    }

    static C1 GetC1() => new C1 { F1 = 123 };

    static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (C1 V_0)
  IL_0000:  nop
  IL_0001:  call       ""C1 Program.GetC1()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, C1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(C1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_04()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}


static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        Test2<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        _ = GetT<T>()[Get1(), $"", Get1()];
    }

    static void Test2<T>() where T : struct
    {
        _ = GetT<T>()[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        _ = GetT<T>()[Get1(), $"", await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_05()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, ref TR x)
    {
        System.Console.Write(((S1)(object)x).F1);
        x = (TR)(object)new S1 { F1 = ((S1)(object)x).F1 + 1 };
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j] 
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static async Task Main()
    {
        Test2<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static void Test2<T>() where T : struct
    {
        _ = GetT<T>()[Get1(), $"", Get1()];
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        _ = GetT<T>()[Get1(), $"", await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124:123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       35 (0x23)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  newobj     ""InterpolationHandler<T>..ctor(int, int, ref T)""
  IL_0017:  call       ""int Program.Get1()""
  IL_001c:  call       ""int E.get_Item<T>(ref T, int, InterpolationHandler<T>, int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_WithInterpolationHandler_RValueReceiver_06()
    {
        var src = """
using System.Threading.Tasks;

[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler<TR>
{

    public InterpolationHandler(int literalLength, int formattedCount, TR x)
    {
        System.Console.Write(((C1)(object)x).F1);
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension<T>(T x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler<T> h, int j]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                return 0;
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
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        _ = GetT<T>()[Get1(), $"", Get1()];
    }

    static void Test2<T>() where T : class
    {
        _ = GetT<T>()[Get1(), $"", Get1()];
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        _ = GetT<T>()[Get1(), $"", await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123123:123123:123123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler<T>..ctor(int, int, T)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item<T>(T, int, InterpolationHandler<T>, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_WithInterpolationHandler_ReadonlyReceiver_020(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test({{{(refKind == "ref" ? "ref" : "in")}}} F);
        System.Console.Write(F.F1);
    }

    static void Test({{{refKind}}} S1 x)
    {
        _ = x[Program.Get1(), $"", Get1()];
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       29 (0x1d)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""int Program.Get1()""
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0011:  call       ""int Program.Get1()""
  IL_0016:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_WithInterpolationHandler_ReadonlyReceiver_021(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.F.F1++;
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 123 };
        Test();
        System.Console.Write(F.F1);
    }

    static void Test()
    {
        _ = GetS1()[Program.Get1(), $"", Get1()];
    }

    static {{{(refKind == "ref" ? "ref" : "ref readonly")}}} S1 GetS1() => ref Program.F;

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  call       """ + (refKind == "ref" ? "ref" : "ref readonly") + @" S1 Program.GetS1()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_WithInterpolationHandler_ReadonlyReceiver_022(string refKind)
    {
        var src = $$$"""
[System.Runtime.CompilerServices.InterpolatedStringHandler]
struct InterpolationHandler
{

    public InterpolationHandler(int literalLength, int formattedCount, {{{refKind}}} S1 x)
    {
        System.Console.Write(x.F1);
        Program.Increment();
    }
    public void AppendLiteral(string value) { }
    public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
}

static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("x")] InterpolationHandler h, int j]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    public static readonly S1 F;

    static void Main()
    {
        Initialize();
        Test();
        System.Console.Write(F.F1);
    }

    static unsafe void Initialize()
    {
        fixed (int* f1 = &F.F1)
        {
            *f1 = 123;
        }
    }

    public static unsafe void Increment()
    {
        fixed (int* f1 = &F.F1)
        {
            (*f1)++;
        }
    }

    static void Test()
    {
        _ = F[Program.Get1(), $"", Get1()];
    }

    public static int Get1()
    {
        Increment();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute], options: TestOptions.DebugExe.WithAllowUnsafe(true));
        var verifier = CompileAndVerify(comp, expectedOutput: "124126126", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (S1& V_0)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.i4.0
  IL_000f:  ldloc.0
  IL_0010:  newobj     ""InterpolationHandler..ctor(int, int, " + refKind + @" S1)""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  call       ""int E.get_Item(" + refKind + @" S1, int, InterpolationHandler, int)""
  IL_001f:  pop
  IL_0020:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int this[int i]
        {
            get
            {
                return 0;
            }
            set
            {
                System.Console.Write(x.F1);
            }
        }
    }
}

public struct S1
{
    public int F1;

    public void Test()
    {
        this[Program.Get1()] = Program.Get1();
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
        F[Program.Get1()] = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "125125:125125", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  stloc.0
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  stloc.1
  IL_0012:  ldobj      ""S1""
  IL_0017:  ldloc.0
  IL_0018:  ldloc.1
  IL_0019:  call       ""void E.set_Item(S1, int, int)""
  IL_001e:  nop
  IL_001f:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  stloc.1
  IL_000e:  ldobj      ""S1""
  IL_0013:  ldloc.0
  IL_0014:  ldloc.1
  IL_0015:  call       ""void E.set_Item(S1, int, int)""
  IL_001a:  nop
  IL_001b:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension(S1 x)
    {
        public int this[int i] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0] = 1;
    }
}
""";

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0] = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1)[0]").WithLocation(15, 9)
            );
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Set_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i]
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
        this[Program.Get1()] = Program.Get1();
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
        F[Program.Get1()] = Get1();
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "125125:125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int Program.Get1()""
  IL_0010:  call       ""void E.set_Item(" + refKind + @" S1, int, int)""
  IL_0015:  nop
  IL_0016:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  call       ""void E.set_Item(" + refKind + @" S1, int, int)""
  IL_0011:  nop
  IL_0012:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        default(S1)[0] = 1;
    }
}
""";

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (15,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(S1)[0] = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(S1)[0]").WithLocation(15, 9)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int this[int i]
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
        F[Get1()] = Get1();
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int Program.Get1()""
  IL_0010:  call       ""void E.set_Item(C1, int, int)""
  IL_0015:  nop
  IL_0016:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Set_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<S1>.F = new S1 { F1 = 123 };
        //await Test3<S1>();
        //System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f[Get1()] = Get1();
    }

    static void Test2<T>(ref T f) where T : struct
    {
        f[Get1()] = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F[Get1()] = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<S1>.F.F1++;
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "125125:125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (T V_0,
                T& V_1,
                int V_2,
                int V_3,
                T V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_4
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_4
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.1
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.0
  IL_001b:  ldloca.s   V_0
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.1
  IL_0020:  call       ""int Program.Get1()""
  IL_0025:  stloc.2
  IL_0026:  call       ""int Program.Get1()""
  IL_002b:  stloc.3
  IL_002c:  ldobj      ""T""
  IL_0031:  ldloc.2
  IL_0032:  ldloc.3
  IL_0033:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0038:  nop
  IL_0039:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  call       ""int Program.Get1()""
  IL_000d:  stloc.1
  IL_000e:  ldobj      ""T""
  IL_0013:  ldloc.0
  IL_0014:  ldloc.1
  IL_0015:  call       ""void E.set_Item<T>(T, int, int)""
  IL_001a:  nop
  IL_001b:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(T x) where T : struct
    {
        public int this[int i] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0] = 1;
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

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0] = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T)[0]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] 
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
        f[Get1()] = Get1();
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        Program<T>.F[Get1()] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "125125:125125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  call       ""void E.set_Item<T>(ref T, int, int)""
  IL_0011:  nop
  IL_0012:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        default(T)[0] = 1;
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

        var comp2 = CreateCompilation([src2]);
        comp2.VerifyDiagnostics(
            // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
            //         default(T)[0] = 1;
            Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default(T)[0]").WithLocation(13, 9),
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Set_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<C1>.F = new C1 { F1 = 123 };
        //await Test3<C1>();
        //System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        f[Get1()] = Get1();
    }

    static void Test2<T>(ref T f) where T : class
    {
        f[Get1()] = Get1();
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    Program<T>.F[Get1()] = await Get1Async();
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123125:123125").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (T V_0,
                T& V_1,
                int V_2,
                int V_3,
                T V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_4
  IL_0005:  initobj    ""T""
  IL_000b:  ldloc.s    V_4
  IL_000d:  box        ""T""
  IL_0012:  brtrue.s   IL_001f
  IL_0014:  ldloc.1
  IL_0015:  ldobj      ""T""
  IL_001a:  stloc.0
  IL_001b:  ldloca.s   V_0
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.1
  IL_0020:  call       ""int Program.Get1()""
  IL_0025:  stloc.2
  IL_0026:  call       ""int Program.Get1()""
  IL_002b:  stloc.3
  IL_002c:  ldobj      ""T""
  IL_0031:  ldloc.2
  IL_0032:  ldloc.3
  IL_0033:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0038:  nop
  IL_0039:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0016:  nop
  IL_0017:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_07()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1()] = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[Get1()] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int Program.Get1()""
  IL_0010:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0015:  nop
  IL_0016:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Set_08()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
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

class Program
{
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        GetT<T>()[Get1()] = Get1();
    }

    static void Test2<T>() where T : class
    {
        GetT<T>()[Get1()] = Get1();
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        GetT<T>()[Get1()] = await Get1Async();
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int Program.Get1()""
  IL_0010:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0015:  nop
  IL_0016:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int Program.Get1()""
  IL_0010:  call       ""void E.set_Item<T>(T, int, int)""
  IL_0015:  nop
  IL_0016:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_LValueReceiver_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

public struct S1
{
    public int F1;

    public void Test()
    {
        _ = this[Program.Get1()];
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
        _ = F[Program.Get1()];
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124", verify: Verification.Skipped).VerifyDiagnostics();

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
  IL_0012:  call       ""int E.get_Item(S1, int)""
  IL_0017:  pop
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
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  stloc.0
  IL_0008:  ldobj      ""S1""
  IL_000d:  ldloc.0
  IL_000e:  call       ""int E.get_Item(S1, int)""
  IL_0013:  pop
  IL_0014:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_LValueReceiver_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;

    public void Test()
    {
        _ = this[Program.Get1()];
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
        _ = F[Program.Get1()];
    }

    public static int Get1()
    {
        Program.F.F1++;
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldsflda    ""S1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item(" + refKind + @" S1, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");

        verifier.VerifyIL("S1.Test",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""int E.get_Item(" + refKind + @" S1, int)""
  IL_000c:  pop
  IL_000d:  ret
}
");

        var src2 = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i] { get => 0; set {} }
    }
}

struct S1;

class Program
{
    static void Test()
    {
        _ = default(S1)[0];
    }
}
""";

        var comp2 = CreateCompilation([src2]);
        // !!! Shouldn't there be a not a variable error for 'default(T)[0, $"", 1]' !!!
        comp2.VerifyDiagnostics(
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_LValueReceiver_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
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
        _ = F[Get1()];
    }

    static int Get1()
    {
        Program.F = new C1 { F1 = Program.F.F1 + 1 };
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1 Program.F""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item(C1, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Get_LValueReceiver_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<S1>.F = new S1 { F1 = 123 };
        Test1(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        System.Console.Write(":");

        Program<S1>.F = new S1 { F1 = 123 };
        Test2(ref Program<S1>.F);
        System.Console.Write(Program<S1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<S1>.F = new S1 { F1 = 123 };
        //await Test3<S1>();
        //System.Console.Write(Program<S1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        _ = f[Get1()];
    }

    static void Test2<T>(ref T f) where T : struct
    {
        _ = f[Get1()];
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    _ = Program<T>.F[await Get1Async()];
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<S1>.F.F1++;
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
  IL_002b:  call       ""int E.get_Item<T>(T, int)""
  IL_0030:  pop
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
  IL_000e:  call       ""int E.get_Item<T>(T, int)""
  IL_0013:  pop
  IL_0014:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_LValueReceiver_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] 
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
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
        _ = f[Get1()];
    }

    static int Get1()
    {
        Program<S1>.F.F1++;
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        _ = Program<T>.F[await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        Program<S1>.F.F1++;
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "124124:124124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""int Program.Get1()""
  IL_0007:  call       ""int E.get_Item<T>(ref T, int)""
  IL_000c:  pop
  IL_000d:  ret
}
");

        var src2 = """
static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] { get => 0; set {} }
    }
}

class Program
{
    static void Test<T>() where T : struct
    {
        _ = default(T)[0];
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

        var comp2 = CreateCompilation([src2]);

        // !!! Shouldn't there be a not a variable error for 'default(T)[0, $"", 1]' !!!
        comp2.VerifyDiagnostics(
            // (21,25): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(in T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(21, 25),
            // (31,35): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
            //         extension<T>(ref readonly T x) where T : struct
            Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(31, 35)
            );
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79416")]
    public void IndexerAccess_Get_LValueReceiver_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                return 0;
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
// https://github.com/dotnet/roslyn/issues/79416 - remove the pragma once fixed
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task Main()
    {
        Program<C1>.F = new C1 { F1 = 123 };
        Test1(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        System.Console.Write(":");

        Program<C1>.F = new C1 { F1 = 123 };
        Test2(ref Program<C1>.F);
        System.Console.Write(Program<C1>.F.F1);

        // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
        //System.Console.Write(":");

        //Program<C1>.F = new C1 { F1 = 123 };
        //await Test3<C1>();
        //System.Console.Write(Program<C1>.F.F1);
    }

    static void Test1<T>(ref T f)
    {
        _ = f[Get1()];
    }

    static void Test2<T>(ref T f) where T : class
    {
        _ = f[Get1()];
    }

    static int Get1()
    {
        Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
        return 1;
    }

    // https://github.com/dotnet/roslyn/issues/79416 - uncomment the following code once fixed
    //static async Task Test3<T>()
    //{
    //    _ = Program<T>.F[await Get1Async()];
    //}

    //static async Task<int> Get1Async()
    //{
    //    Program<C1>.F = new C1 { F1 = Program<C1>.F.F1 + 1 };
    //    await Task.Yield();
    //    return 1;
    //}
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123124:123124").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>(ref T)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T V_0,
                T& V_1,
                int V_2,
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
  IL_001f:  call       ""int Program.Get1()""
  IL_0024:  stloc.2
  IL_0025:  ldobj      ""T""
  IL_002a:  ldloc.2
  IL_002b:  call       ""int E.get_Item<T>(T, int)""
  IL_0030:  pop
  IL_0031:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>(ref T)",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldobj      ""T""
  IL_0007:  call       ""int Program.Get1()""
  IL_000c:  call       ""int E.get_Item<T>(T, int)""
  IL_0011:  pop
  IL_0012:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_RValueReceiver_01()
    {
        var src = """
static class E
{
    extension(S1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

public struct S1
{
    public int F1;
}

class Program
{
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetS1()[Get1()];
    }

    static S1 GetS1() => new S1 { F1 = 123 };

    public static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""S1 Program.GetS1()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item(S1, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("in")]
    public void IndexerAccess_Get_RValueReceiver_02(string refKind)
    {
        var src = $$$"""
static class E
{
    extension({{{refKind}}} S1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetS1()[Get1()];
    }

    static S1 GetS1() => new S1 { F1 = 123 };

    public static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe.WithAllowUnsafe(true));
        var verifier = CompileAndVerify(comp, expectedOutput: "123", verify: Verification.Skipped).VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (S1 V_0)
  IL_0000:  nop
  IL_0001:  call       ""S1 Program.GetS1()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  call       ""int E.get_Item(" + refKind + @" S1, int)""
  IL_0013:  pop
  IL_0014:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_RValueReceiver_03()
    {
        var src = """
static class E
{
    extension(C1 x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(x.F1);
                return 0;
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
    static void Main()
    {
        Test();
    }

    static void Test()
    {
        _ = GetC1()[Get1()];
    }

    static C1 GetC1() => new C1 { F1 = 123 };

    static int Get1()
    {
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""C1 Program.GetC1()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item(C1, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_RValueReceiver_04()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static async Task Main()
    {
        Test1<S1>();

        System.Console.Write(":");

        Test2<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static void Test1<T>()
    {
        _ = GetT<T>()[Get1()];
    }

    static void Test2<T>() where T : struct
    {
        _ = GetT<T>()[Get1()];
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        _ = GetT<T>()[await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item<T>(T, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item<T>(T, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_RValueReceiver_05()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(ref T x) where T : struct
    {
        public int this[int i] 
        {
            get
            {
                System.Console.Write(((S1)(object)x).F1);
                return 0;
            }
        }
    }
}

struct S1
{
    public int F1;
}

class Program
{
    static async Task Main()
    {
        Test2<S1>();

        System.Console.Write(":");

        await Test3<S1>();
    }

    static void Test2<T>() where T : struct
    {
        _ = GetT<T>()[Get1()];
    }

    static T GetT<T>() => (T)(object)new S1 { F1 = 123 };

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>() where T : struct
    {
        _ = GetT<T>()[await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""int Program.Get1()""
  IL_000e:  call       ""int E.get_Item<T>(ref T, int)""
  IL_0013:  pop
  IL_0014:  ret
}
");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/78829")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78829")]
    public void IndexerAccess_Get_RValueReceiver_06()
    {
        var src = """
using System.Threading.Tasks;

static class E
{
    extension<T>(T x)
    {
        public int this[int i]
        {
            get
            {
                System.Console.Write(((C1)(object)x).F1);
                return 0;
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
    static async Task Main()
    {
        Test1<C1>();

        System.Console.Write(":");

        Test2<C1>();

        System.Console.Write(":");

        await Test3<C1>();
    }

    static T GetT<T>() => (T)(object)new C1 { F1 = 123 };

    static void Test1<T>()
    {
        _ = GetT<T>()[Get1()];
    }

    static void Test2<T>() where T : class
    {
        _ = GetT<T>()[Get1()];
    }

    static int Get1()
    {
        return 1;
    }

    static async Task Test3<T>()
    {
        _ = GetT<T>()[await Get1Async()];
    }

    static async Task<int> Get1Async()
    {
        await Task.Yield();
        return 1;
    }
}
""";

        var comp = CreateCompilation([src], options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(comp, expectedOutput: "123:123:123").VerifyDiagnostics();

        verifier.VerifyIL("Program.Test1<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item<T>(T, int)""
  IL_0010:  pop
  IL_0011:  ret
}
");

        verifier.VerifyIL("Program.Test2<T>()",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  call       ""T Program.GetT<T>()""
  IL_0006:  call       ""int Program.Get1()""
  IL_000b:  call       ""int E.get_Item<T>(T, int)""
  IL_0010:  pop
  IL_0011:  ret
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

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(System.Object o)", extension.ComputeExtensionMarkerRawName());

        CompileAndVerify(comp).VerifyDiagnostics().VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$C43E2675C7BBF9284AF22FB8A9BF0280'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$119AA281C143547563250CAF89B48A76'
            extends [mscorlib]System.Object
        {
            // Methods
            .method private hidebysig specialname static 
                void '<Extension>$' (
                    object o
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$119AA281C143547563250CAF89B48A76'::'<Extension>$'
        } // end of class <M>$119AA281C143547563250CAF89B48A76
    } // end of class <G>$C43E2675C7BBF9284AF22FB8A9BF0280
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
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
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
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
        AssertEx.Equal("extension(N1.N2.C1/C2/C3)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(N1.N2.C1.C2.C3)", extension.ComputeExtensionMarkerRawName());
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

        var e = comp.GetMember<NamedTypeSymbol>("E");
        var extension = (SourceNamedTypeSymbol)e.GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<U>(U)", extension.ComputeExtensionMarkerRawName());

        CompileAndVerify(comp).VerifyDiagnostics().VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$8048A6C8BE30A622530249B904B537EB'<$T0>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A93DBF9EBD61C29E8B5CFA979E4C33E8'<U>
            extends [mscorlib]System.Object
        {
            // Methods
            .method private hidebysig specialname static 
                void '<Extension>$' (
                    !U ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A93DBF9EBD61C29E8B5CFA979E4C33E8'::'<Extension>$'
        } // end of class <M>$A93DBF9EBD61C29E8B5CFA979E4C33E8
    } // end of class <G>$8048A6C8BE30A622530249B904B537EB
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
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

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType))>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T) where T : unmanaged", extension.ComputeExtensionMarkerRawName());

        CompileAndVerify(comp).VerifyDiagnostics().VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BAC44226FEFE1ED1B549A4B5F35748C7'<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) $T0>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param type $T0
            .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
                01 00 00 00
            )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$03A00A6A168488BDF2B2E5B73B8099A6'<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>
            extends [mscorlib]System.Object
        {
            .param type T
                .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
                    01 00 00 00
                )
            // Methods
            .method private hidebysig specialname static 
                void '<Extension>$' (
                    !T ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$03A00A6A168488BDF2B2E5B73B8099A6'::'<Extension>$'
        } // end of class <M>$03A00A6A168488BDF2B2E5B73B8099A6
    } // end of class <G>$BAC44226FEFE1ED1B549A4B5F35748C7
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

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
            // (7,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(T t)
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(7, 5),
            // (9,21): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(9, 21),
            // (12,27): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     public static void M2<in T>(this T t) { }
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(12, 27),
            // (13,24): error CS0111: Type 'E' already defines a member called 'M2' with the same parameter types
            //     public static void M2<T>(this T t) { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "E").WithLocation(13, 24)
            );
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
        var expected = "E.extension<T>(T?)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T?)", extension.ComputeExtensionMarkerRawName());
    }

    /// <summary>Validates that the extension block was properly round-tripped (we didn't lose any details about type parameters, constraints or extension parameter).</summary>
    /// <remarks>This does not include the extension parameter name or attributes. Use <paramref name="extraValidator"/> for that.</remarks>
    private CompilationVerifier CompileAndVerifyAndValidate(CSharpCompilation comp, string expected, Action<ModuleSymbol> extraValidator = null, Verification verify = default)
    {
        return CompileAndVerify(comp,
            sourceSymbolValidator: m => validate(m, expected, extraValidator),
            symbolValidator: m => validate(m, expected, extraValidator),
            verify: verify);

        static void validate(ModuleSymbol module, string expected, Action<ModuleSymbol> extraValidator)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();

            SymbolDisplayFormat format = SymbolDisplayFormat.TestFormatWithConstraints
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)
                .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.None);

            AssertEx.Equal(expected, extension.ToDisplayString(format));

            if (extraValidator is not null)
            {
                extraValidator(module);
            }
        }
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
        var expected = "E.extension<T>(T)";
        CompileAndVerifyAndValidate(comp, expected, validate).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T!)", extension.ComputeExtensionMarkerRawName());

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            Assert.Equal(NullableAnnotation.NotAnnotated, extension.ExtensionParameter.TypeWithAnnotations.NullableAnnotation);
        }
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
        var expected = "E.extension<T>(T)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T>((System.String?, T?, System.String!, T!, System.String, T)) where T : class!";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T>((T?, T, T))";
        CompileAndVerifyAndValidate(comp, expected, validate).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.ValueTuple`3<!0, !0, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>((T?, T!, T))", extension.ComputeExtensionMarkerRawName());

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            var tupleElements = extension.ExtensionParameter.Type.TupleElementTypesWithAnnotations;
            Assert.Equal(NullableAnnotation.Annotated, tupleElements[0].NullableAnnotation);
            Assert.Equal(NullableAnnotation.NotAnnotated, tupleElements[1].NullableAnnotation);
            Assert.Equal(NullableAnnotation.Oblivious, tupleElements[2].NullableAnnotation);
        }
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

        bool isActualKeyword = keyword is not ("await" or "record" or "dynamic" or "field" or "file" or "notnull" or "unmanaged");
        var expected = $$"""E.extension<T>((System.Int32 {{(isActualKeyword ? "@" : "")}}{{keyword}}, System.Int32 x))""";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T>(S<System.String?, T?, System.String!, T!, System.String, T>) where T : class!";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T>(S<T?, T, T>)";
        CompileAndVerifyAndValidate(comp, expected, validate).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(S`3<!0, !0, !0>)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(S<T?, T!, T>)", extension.ComputeExtensionMarkerRawName());

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            var typeArguments = ((NamedTypeSymbol)extension.ExtensionParameter.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            Assert.Equal(NullableAnnotation.Annotated, typeArguments[0].NullableAnnotation);
            Assert.Equal(NullableAnnotation.NotAnnotated, typeArguments[1].NullableAnnotation);
            Assert.Equal(NullableAnnotation.Oblivious, typeArguments[2].NullableAnnotation);
        }
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
        string expected = "E.extension<T1, T2, T3>(System.Int32) where T1 : class! where T2 : class? where T3 : class";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T1, T2, T3>(System.Int32) where T1 : struct where T2 : unmanaged where T3 : notnull";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T1, T2, T3>(System.Int32) where T1 : I! where T2 : I? where T3 : I";
        var verifier = CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<(I), (I), (I)>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T1, T2, T3>(System.Int32) where T1 : I! where T2 : I? where T3 : I", extension.ComputeExtensionMarkerRawName());

        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$0AD8C3962A3C5E6BFA97E099F6F428C4'<(I) $T0, (I) $T1, (I) $T2>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$5B198AEBE2F597134BE1E94D84704187'<(I) T1, (I) T2, (I) T3>
            extends [mscorlib]System.Object
        {
            .param constraint T1, I
                .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 01 00 00
                )
            .param constraint T2, I
                .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                    01 00 02 00 00
                )
            // Methods
            .method private hidebysig specialname static 
                void '<Extension>$' (
                    int32 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208e
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$5B198AEBE2F597134BE1E94D84704187'::'<Extension>$'
        } // end of class <M>$5B198AEBE2F597134BE1E94D84704187
    } // end of class <G>$0AD8C3962A3C5E6BFA97E099F6F428C4
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
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
        var expected = "E.extension<T1, T2, T3>(System.Int32) where T1 : I1, I2 where T2 : I2, I1";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension<T1>(System.Int32)";
        var verifier = CompileAndVerifyAndValidate(comp, expected, validate).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<>(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<[AAttribute/*()*/] [BAttribute/*()*/] T1>([AAttribute/*()*/] [BAttribute/*()*/] System.Int32)", extension.ComputeExtensionMarkerRawName());

        verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$B8D310208B4544F25EEBACB9990FC73B'<$T0>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$D131137B02074799BD78183FB29034EC'<T1>
            extends [mscorlib]System.Object
        {
            .param type T1
                .custom instance void AAttribute::.ctor() = (
                    01 00 00 00
                )
                .custom instance void BAttribute::.ctor() = (
                    01 00 00 00
                )
            // Methods
            .method private hidebysig specialname static 
                void '<Extension>$' (
                    int32 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                .param [1]
                    .custom instance void AAttribute::.ctor() = (
                        01 00 00 00
                    )
                    .custom instance void BAttribute::.ctor() = (
                        01 00 00 00
                    )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$D131137B02074799BD78183FB29034EC'::'<Extension>$'
        } // end of class <M>$D131137B02074799BD78183FB29034EC
    } // end of class <G>$B8D310208B4544F25EEBACB9990FC73B
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

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

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            Assert.Equal(["AAttribute", "BAttribute"], extension.TypeParameters[0].GetAttributes().Select(a => a.ToString()));
            Assert.Equal(["AAttribute", "BAttribute"], extension.ExtensionParameter.GetAttributes().Select(a => a.ToString()));
            Assert.Equal(module is SourceModuleSymbol ? "" : "value", extension.ExtensionParameter.Name);
        }
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
    extension([My(10, "hello", P = 20, P2 = "hello2")] int i)
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
        var expected = "E.extension(System.Int32)";
        CompileAndVerifyAndValidate(comp, expected, validate).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Int32)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("""extension([MyAttribute/*(System.Int32, System.String)*/(10, "hello", P = 20, P2 = "hello2")] System.Int32 i)""", extension.ComputeExtensionMarkerRawName());

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

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            Assert.Equal(["""MyAttribute(10, "hello", P = 20, P2 = "hello2")"""], extension.ExtensionParameter.GetAttributes().Select(a => a.ToString()));
            Assert.Equal("i", extension.ExtensionParameter.Name);
        }
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
        var expected = "E.extension(System.Object![,]![]?)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(System.Object![,]![]?[,,,])";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(System.Object![][,]!)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(System.Object![,]![])";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(ref System.Int32)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(ref readonly System.Int32)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(in System.Int32)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(scoped ref System.Int32)";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(ref System.Int32)";
        CompileAndVerifyAndValidate(comp, expected, validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

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

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            Assert.Equal(["System.Diagnostics.CodeAnalysis.UnscopedRefAttribute"], extension.ExtensionParameter.GetAttributes().Select(a => a.ToString()));
        }
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
        var expected = "E.extension(System.Object!)";
        CompileAndVerifyAndValidate(comp, expected, validate, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension(System.Object)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension([System.Diagnostics.CodeAnalysis.AllowNullAttribute/*()*/] System.Object! o)", extension.ComputeExtensionMarkerRawName());

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
            Assert.Equal(["System.Diagnostics.CodeAnalysis.AllowNullAttribute"], extension.ExtensionParameter.GetAttributes().Select(a => a.ToString()));
        }
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
        var expected = "E.extension<T, U>(System.Int32) where T : class where U : struct";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

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
        var expected = "E.extension(delegate* unmanaged[SuppressGCTransition]<System.Int32>[])";
        CompileAndVerifyAndValidate(comp, expected, verify: Verification.FailsPEVerify).VerifyDiagnostics();

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
        AssertEx.SequenceEqual(["System.Int32", "System.String", "System.Int64"], field.TypeWithAnnotations.CustomModifiers.SelectAsArray(m => m.Modifier.ToTestDisplayString()));
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

    [Fact]
    public void MarkerTypeRawName_82()
    {
        // unmanaged constraint
        var src = """
public static class E
{
    extension<T>(int) where T : unmanaged
    {
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        var expected = "E.extension<T>(System.Int32) where T : unmanaged";
        CompileAndVerifyAndValidate(comp, expected, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<valuetype .ctor (System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType))>(System.Int32)",
            extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(System.Int32) where T : unmanaged",
            extension.ComputeExtensionMarkerRawName());

        comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
        expected = "E.extension<T>(System.Int32) where T : unmanaged";
        CompileAndVerifyAndValidate(comp, expected, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void MarkerTypeRawName_83()
    {
        // nullable annotations on extension parameter
        var src = """
#nullable enable

static class E
{
    extension<T>(T) where T : class
    {
    }
}
""";
        var comp = CreateCompilation(src);
        var expected = "E.extension<T>(T!) where T : class!";
        CompileAndVerifyAndValidate(comp, expected).VerifyDiagnostics();

        var extension = (SourceNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers().Single();
        AssertEx.Equal("extension<class>(!0)", extension.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension<T>(T!) where T : class!", extension.ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void ExtensionMarkerAttribute_01()
    {
        // synthesized attribute
        var src = """
public static class E
{
    extension(int)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

        verifier.VerifyTypeIL("ExtensionMarkerAttribute", """
.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ExtensionMarkerAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 dc 17 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            string name
        ) cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    } // end of method ExtensionMarkerAttribute::.ctor
} // end of class System.Runtime.CompilerServices.ExtensionMarkerAttribute
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        static void validate(ModuleSymbol module)
        {
            if (module is PEModuleSymbol)
            {
                AssertEx.Equal("System.Runtime.CompilerServices.ExtensionMarkerAttribute",
                    module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.ExtensionMarkerAttribute").ToTestDisplayString());
            }
            else
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.ExtensionMarkerAttribute"));
            }
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_02()
    {
        // no synthesized attribute
        var src = """
public static class E
{
    extension(int)
    {
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.ExtensionMarkerAttribute"));
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_03()
    {
        // attribute defined
        var libComp = CreateCompilation(ExtensionMarkerAttributeDefinition);

        var src = """
public static class E
{
    extension(int)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, references: [libComp.EmitToImageReference()]);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.ExtensionMarkerAttribute"));
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_04()
    {
        // attribute erased on method
        var src = """
public static class E
{
    extension(int)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            var method = extension.GetMembers().Single();
            AssertEx.Equal("void E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.M()", method.ToTestDisplayString());
            Assert.Equal([], method.GetAttributes().Select(a => a.ToString()));
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_05()
    {
        // attribute erased on property
        var src = """
public static class E
{
    extension(int)
    {
        public static int P => 0;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            var members = extension.GetMembers().ToArray();
            if (module is SourceModuleSymbol)
            {
                AssertEx.Equal(["System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P { get; }", "System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P.get"],
                    members.ToTestDisplayStrings());
                Assert.Equal([], members[0].GetAttributes().Select(a => a.ToString()));
                Assert.Equal([], members[1].GetAttributes().Select(a => a.ToString()));
            }
            else
            {
                AssertEx.Equal(["System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P.get", "System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.P { get; }"],
                    members.ToTestDisplayStrings());
                Assert.Equal([], members[0].GetAttributes().Select(a => a.ToString()));
                Assert.Equal([], members[1].GetAttributes().Select(a => a.ToString()));
            }
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_06()
    {
        // attribute erased on operator
        var src = """
public static class E
{
    extension(int)
    {
        public static int operator +(int i1, int i2) => throw null;
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            var extension = module.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            var member = extension.GetMembers().Single();
            AssertEx.Equal("System.Int32 E.<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69.op_Addition(System.Int32 i1, System.Int32 i2)",
                member.ToTestDisplayString());
            Assert.Equal([], member.GetAttributes().Select(a => a.ToString()));
        }
    }

    [Fact]
    public void ExtensionMarkerAttribute_07()
    {
        // attribute disallowed in source
        var src = """
using System.Runtime.CompilerServices;

[assembly: ExtensionMarker("assembly")]
[module: ExtensionMarker("module")]

[ExtensionMarker("class")]
public class C1 { }

[ExtensionMarker("struct")]
public struct S { }

[ExtensionMarker("enum")]
public enum E { }

public class C2
{
    [ExtensionMarker("constructor")]
    public C2() { }

    [ExtensionMarker("method")]
    public void M() { }
    
    public void M2([ExtensionMarker("parameter")] int i) { }

    [return: ExtensionMarker("return")]
    public int M3() => 0;

    [ExtensionMarker("property")]
    public int P => 0;

    [ExtensionMarker("field")]
    public int f = 0;

    [ExtensionMarker("event")]
    public event System.Action Event { add { } remove { } }
}

[ExtensionMarker("interface")]
public interface I { }

[ExtensionMarker("delegate")]
public delegate void D();

class C3<[ExtensionMarker("type parameter")] T> { }
""" + ExtensionMarkerAttributeDefinition;

        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,12): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            // [assembly: ExtensionMarker("assembly")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(3, 12),
            // (4,10): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            // [module: ExtensionMarker("module")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(4, 10),
            // (6,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            // [ExtensionMarker("class")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""class"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(6, 2),
            // (9,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            // [ExtensionMarker("struct")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""struct"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(9, 2),
            // (12,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            // [ExtensionMarker("enum")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""enum"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(12, 2),
            // (17,6): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            //     [ExtensionMarker("constructor")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(17, 6),
            // (20,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            //     [ExtensionMarker("method")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""method"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(20, 6),
            // (23,21): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            //     public void M2([ExtensionMarker("parameter")] int i) { }
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(23, 21),
            // (25,14): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            //     [return: ExtensionMarker("return")]
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(25, 14),
            // (28,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            //     [ExtensionMarker("property")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""property"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(28, 6),
            // (31,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            //     [ExtensionMarker("field")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""field"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(31, 6),
            // (34,6): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            //     [ExtensionMarker("event")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""event"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(34, 6),
            // (38,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            // [ExtensionMarker("interface")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""interface"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(38, 2),
            // (41,2): error CS8335: Do not use 'System.Runtime.CompilerServices.ExtensionMarkerAttribute'. This is reserved for compiler usage.
            // [ExtensionMarker("delegate")]
            Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, @"ExtensionMarker(""delegate"")").WithArguments("System.Runtime.CompilerServices.ExtensionMarkerAttribute").WithLocation(41, 2),
            // (44,11): error CS0592: Attribute 'ExtensionMarker' is not valid on this declaration type. It is only valid on 'class, struct, enum, method, property, indexer, field, event, interface, delegate' declarations.
            // class C3<[ExtensionMarker("type parameter")] T> { }namespace System.Runtime.CompilerServices
            Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ExtensionMarker").WithArguments("ExtensionMarker", "class, struct, enum, method, property, indexer, field, event, interface, delegate").WithLocation(44, 11));
    }

    [Fact]
    public void Grouping_01()
    {
        // extension blocks differing by tuple names are merged into a single grouping type
        var src = """
public static class E
{
    extension((int a, int b))
    {
        public static void M1() { }
    }
    extension((int c, int d))
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$49AAF2D3C1326E88AED3848611C299DA",
                "TypeDefinition:<M>$AB62B2C6B27F13EA3A1F6BB0E641E504",
                "TypeDefinition:<M>$1F33B79C79D1C037CA6976EB16158758"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    private static void VerifyCollisions(CSharpCompilation comp, bool groupingMatch, bool markerMatch)
    {
        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Equal(groupingMatch, extension1.ComputeExtensionGroupingRawName() == extension2.ComputeExtensionGroupingRawName());
        Assert.Equal(markerMatch, extension1.ComputeExtensionMarkerRawName() == extension2.ComputeExtensionMarkerRawName());
        Assert.Equal(groupingMatch, ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2));
        Assert.Equal(markerMatch, ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2));
    }

    [Fact]
    public void Grouping_02()
    {
        // extension blocks differing by nullability are merged into a single grouping type
        var src = """
#nullable enable

public static class E
{
    extension(object?)
    {
        public static void M1() { }
    }
    extension(object)
    {
        public static void M2() { }
    }
#nullable disable
    extension(object)
    {
        public static void M3() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$C43E2675C7BBF9284AF22FB8A9BF0280",
                "TypeDefinition:<M>$C28A9B43142182F45550A2A20368458B",
                "TypeDefinition:<M>$44947657E3B15A1A640D61854E160848",
                "TypeDefinition:<M>$C43E2675C7BBF9284AF22FB8A9BF0280"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_03()
    {
        // extension blocks differing by IL-level constraints each have a grouping type
        var src = """
public static class E
{
    extension<T>(T) where T : class
    {
        public static void M1() { }
    }
    extension<T>(T) where T : struct
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$66F77D1E46F965A5B22D4932892FA78B",
                "TypeDefinition:<M>$C8718A1AD9DFC47EBD0C706B9E6984E6",
                "TypeDefinition:<G>$BCF902721DDD961E5243C324D8379E5C",
                "TypeDefinition:<M>$B865B3ED3C68CE2EBBC104FFAF3CFF93"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_04()
    {
        // extension blocks differing by C#-level constraints are merged into a single grouping type
        var src = """
#nullable enable
public static class E
{
    extension<T>(T) where T : notnull
    {
        public static void M1() { }
    }
    extension<T>(T)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$8048A6C8BE30A622530249B904B537EB",
                "TypeDefinition:<M>$C7A07C3975E80DE5DBC93B5392C6C922",
                "TypeDefinition:<M>$2789E59A55056F0AD9E820EBD5BCDFBF"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_05()
    {
        // attribute on parameter vs. no attribute
        var src = """
public static class E
{
    extension([A] int)
    {
        public static void M1() { }
    }
    extension(int)
    {
        public static void M2() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$E32A05FB502A840C00FE0EDD5BE96810",
                "TypeDefinition:<M>$BA41CFE2B5EDAEB8C1B9062F59ED4D69"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_06()
    {
        // same attributes on parameter
        var src = """
public static class E
{
    extension([A] int)
    {
        public static void M1() { }
    }
    extension([A] int)
    {
        public static void M2() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$E32A05FB502A840C00FE0EDD5BE96810"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_07()
    {
        // different attributes constructors on parameter
        var src = """
public static class E
{
    extension([A] int) { }
    extension([A(1)] int) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute() { }
    public AAttribute(int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_08()
    {
        // attribute on parameter vs. different attribute
        var src = """
public static class E
{
    extension([A] int)
    {
        public static void M1() { }
    }
    extension([B] int)
    {
        public static void M2() { }
    }
}

public class AAttribute : System.Attribute { }
public class BAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$E32A05FB502A840C00FE0EDD5BE96810",
                "TypeDefinition:<M>$218F3E71AC85BD424B16D5E83C9E7F44"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_09()
    {
        // different attribute values on parameter
        var src = """
public static class E
{
    extension([A(1)] int)
    {
        public static void M1() { }
    }
    extension([A(2)] int)
    {
        public static void M2() { }
    }
}

public class AAttribute : System.Attribute
{
    public AAttribute(int value) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$B6EBDF480696A625FE9EDB09D32E1830",
                "TypeDefinition:<M>$30F160891A3959D878D7B02360CC7D54"
                ], reader.DumpNestedTypes(e.Handle));

            var extensions = e.GetTypeMembers();
            Assert.Equal(["AAttribute(1)"], extensions[0].ExtensionParameter.GetAttributes().Select(a => a.ToString()));
            Assert.Equal(["AAttribute(2)"], extensions[1].ExtensionParameter.GetAttributes().Select(a => a.ToString()));
        }
    }

    [Fact]
    public void Grouping_10()
    {
        // duplicate attribute on parameter vs. single attribute
        var src = """
public static class E
{
    extension([A, A] int)
    {
        public static void M1() { }
    }
    extension([A] int)
    {
        public static void M2() { }
    }
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
public class AAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$F3C360580C2136B2A9F2154F91355898",
                "TypeDefinition:<M>$E32A05FB502A840C00FE0EDD5BE96810"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_11()
    {
        // different attribute orders
        var src = """
public static class E
{
    extension([A(1), A(2)] int) { }
    extension([A(2), A(1)] int) { }
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
public class AAttribute : System.Attribute
{
    public AAttribute(int value) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_12()
    {
        // different attribute orders
        var src = """
public static class E
{
    extension([A(1), A(2)] int)
    {
        public static void M() { }
    }
    extension([A(2), A(1)] int)
    {
        public static void M() { }
    }
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
public class AAttribute : System.Attribute
{
    public AAttribute(int value) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (9,28): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public static void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(9, 28));
    }

    [Fact]
    public void Grouping_13()
    {
        // different order of named arguments in attribute
        var src = """
public static class E
{
    extension([A(P1 = 0, P2 = 0)] int) { }
    extension([A(P2 = 0, P1 = 0)] int) { }
}

public class AAttribute : System.Attribute
{
    public int P1 { get; set; }
    public int P2 { get; set; }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_14()
    {
        // different order of arguments in attribute
        var src = """
public static class E
{
    extension([A(i1: 0, i2: 0)] int) { }
    extension([A(i2: 0, i1: 0)] int) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(int i1, int i2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_15()
    {
        // attribute on parameter from assembly vs. from another assembly
        var libSrc = """
public class AAttribute : System.Attribute { }
""";
        var libComp1 = CreateCompilation(libSrc, assemblyName: "assembly1");
        var libComp2 = CreateCompilation(libSrc, assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::AAttribute;
using A2 = alias2::AAttribute;

public static class E
{
    extension([A1] int) { }
    extension([A2] int) { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension([A2] int) { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.True(ExtensionGroupingInfo.HaveSameILSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
    }

    [Fact]
    public void Grouping_16()
    {
        // attributes on type parameter vs. no attribute
        var src = """
public static class E
{
    extension<[A] T>(int)
    {
        public static void M1() { }
    }
    extension<T>(int)
    {
        public static void M2() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$B8D310208B4544F25EEBACB9990FC73B",
                "TypeDefinition:<M>$F167169D271C76FCF9FF858EA5CFC454",
                "TypeDefinition:<M>$9D7BB308433678477E9C2F4392A27B18"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_17()
    {
        // notnull vs. oblivious type parameter
        var src = """
#nullable enable
public static class E
{
    extension<T>(T) where T : notnull
    {
        public static void M1() { }
    }

#nullable disable
    extension<T>(T)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$8048A6C8BE30A622530249B904B537EB",
                "TypeDefinition:<M>$C7A07C3975E80DE5DBC93B5392C6C922",
                "TypeDefinition:<M>$01CE3801593377B4E240F33E20D30D50"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_18()
    {
        // different type parameter names
        var src = """
public static class E
{
    extension<T1>(int)
    {
        public static void M1() { }
    }

    extension<T2>(int)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$B8D310208B4544F25EEBACB9990FC73B",
                "TypeDefinition:<M>$A189EAA0A09C2534B53DBF86166AD56A",
                "TypeDefinition:<M>$869530FF3C2454D7BCCC5A8D0E31052F"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_19()
    {
        // same type parameter names
        var src = """
public static class E
{
    extension<T>(int)
    {
        public static void M1() { }
    }

    extension<T>(int)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$B8D310208B4544F25EEBACB9990FC73B",
                "TypeDefinition:<M>$9D7BB308433678477E9C2F4392A27B18"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_20()
    {
        // different parameter names
        var src = """
public static class E
{
    extension(int i1)
    {
        public static void M1() { }
    }

    extension(int i2)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$531E7AC45D443AE2243E7FFAB9455D60",
                "TypeDefinition:<M>$032E02D1D6078965F7C2AFC8F27F2F81"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_21()
    {
        // same parameter names
        var src = """
public static class E
{
    extension(int i)
    {
        public static void M1() { }
    }

    extension(int i)
    {
        public static void M2() { }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, symbolValidator: validate).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        static void validate(ModuleSymbol module)
        {
            var e = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("E");
            var reader = ((PEModuleSymbol)module).Module.GetMetadataReader();
            AssertEx.Equal([
                "TypeDefinition:E",
                "TypeDefinition:<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69",
                "TypeDefinition:<M>$F4B4FFE41AB49E80A4ECF390CF6EB372"
                ], reader.DumpNestedTypes(e.Handle));
        }
    }

    [Fact]
    public void Grouping_22()
    {
        // ref vs. by value
        var src = """
public static class E
{
    extension(ref int i) { }
    extension(int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_23()
    {
        // ref readonly vs. in
        var src = """
public static class E
{
    extension(ref readonly int i) { }
    extension(in int i) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_24()
    {
        var libComp1 = CreateCompilation("public class A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public class A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension(A1)
    {
        public static void M() { }
    }
    extension(A2)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (12,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension(A2)
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(12, 5),
            // (14,28): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public static void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(14, 28));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        AssertEx.Equal("extension(A)", extension1.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(A)", extension1.ComputeExtensionMarkerRawName());

        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        AssertEx.Equal("extension(A)", extension2.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(A)", extension2.ComputeExtensionMarkerRawName());

        Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2));
    }

    [Fact]
    public void Grouping_25()
    {
        var libComp1 = CreateCompilation("public class A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public class A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension(A1) { }
    extension(A2) { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension(A2) { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));
    }

    [Fact]
    public void Grouping_26()
    {
        var libComp1 = CreateCompilation("public class A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public class A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T>(T) where T : A1 { }
    extension<T>(T) where T : A2 { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(T) where T : A2 { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.False(ExtensionGroupingInfo.HaveSameILSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
    }

    [Fact]
    public void Grouping_27()
    {
        var libComp1 = CreateCompilation("public class A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public class A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T, U>(T) where T : A1 where U : T { }
    extension<T, U>(T) where T : A2 where U : T { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T, U>(T) where T : A2 where U : T { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.False(ExtensionGroupingInfo.HaveSameILSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
    }

    [Fact]
    public void Grouping_28()
    {
        var libComp1 = CreateCompilation("public interface A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public interface A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T>(T) where T : I, A1 { }
    extension<T>(T) where T : I, A2 { }
}

public interface I { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(T) where T : I, A2 { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.False(ExtensionGroupingInfo.HaveSameILSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
    }

    [Fact]
    public void Grouping_29()
    {
        var libComp1 = CreateCompilation("public interface A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public interface A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T>(T) where T : A1, I { }
    extension<T>(T) where T : I, A2 { }
}

public interface I { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(T) where T : I, A2 { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.False(ExtensionGroupingInfo.HaveSameILSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature((SourceNamedTypeSymbol)extensions[0], (SourceNamedTypeSymbol)extensions[1]));
    }

    [Fact]
    public void Grouping_30()
    {
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension(A1 a) { }
    extension(ref A2 a) { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension(ref A2 a) { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        AssertEx.Equal("extension(A)", extension1.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(A a)", extension1.ComputeExtensionMarkerRawName());

        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        AssertEx.Equal("extension(A)", extension2.ComputeExtensionGroupingRawName());
        AssertEx.Equal("extension(ref A a)", extension2.ComputeExtensionMarkerRawName());

        Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2));
    }

    [Fact]
    public void Grouping_31()
    {
        // types from different assemblies in extension parameter
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension(A1 a)
    {
        public static void M() { }
    }
    extension(ref A2 a)
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (12,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension(ref A2 a)
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(12, 5),
            // (14,28): error CS0111: Type 'E' already defines a member called 'M' with the same parameter types
            //         public static void M() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "E").WithLocation(14, 28));
    }

    [Fact]
    public void Grouping_32()
    {
        // types from different assemblies in nested position in extension parameter
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension(I<A1> a) { }
    extension(I<A2> a) { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension(I<A2> a) { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => Assert.True(extension1.ComputeExtensionGroupingRawName() == extension2.ComputeExtensionGroupingRawName()),
            () => Assert.True(extension1.ComputeExtensionMarkerRawName() == extension2.ComputeExtensionMarkerRawName()),
            () => Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_33()
    {
        // types from different assemblies in constraints
        var libComp1 = CreateCompilation("public interface A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public interface A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T>(int i) where T : A1 { }
    extension<T>(int i) where T : A2 { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int i) where T : I<A2> { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => Assert.True(extension1.ComputeExtensionGroupingRawName() == extension2.ComputeExtensionGroupingRawName()),
            () => Assert.True(extension1.ComputeExtensionMarkerRawName() == extension2.ComputeExtensionMarkerRawName()),
            () => Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_34()
    {
        // types from different assemblies in nested position in constraints
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension<T>(int i) where T : I<A1> { }
    extension<T>(int i) where T : I<A2> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int i) where T : I<A2> { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension<(I`1<A>)>(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32 i) where T : I<A>", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension<(I`1<A>)>(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32 i) where T : I<A>", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_35()
    {
        // types from different assemblies in typeof
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension([A(typeof(A1))] int i) { }
    extension([A(typeof(A2))] int i) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int i) where T : I<A2> { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(A))] System.Int32 i)", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(A))] System.Int32 i)", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.True(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_36()
    {
        var src = """
#nullable enable

public static class E
{
    extension([A(typeof(I<object?>))] int) { }
    extension([A(typeof(I<object>))] int) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type t) { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(I`1<System.Object>))] System.Int32)", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(I`1<System.Object>))] System.Int32)", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.True(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.True(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_37()
    {
        var src = """
public static class E
{
    extension([A(typeof((int a, int b)))] int) { }
    extension([A(typeof((int notA, int notB)))] int) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(System.ValueTuple`2<System.Int32, System.Int32>))] System.Int32)", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(System.ValueTuple`2<System.Int32, System.Int32>))] System.Int32)", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.True(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.True(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_38()
    {
        var src = """
public static class E
{
    extension([A(typeof((int a, int b)))] int) { }
    extension([A(typeof((int, int)))] int) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type t) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_39()
    {
        // types from different assemblies in nested position in typeof
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension([A(typeof(I<A1>))] int i) { }
    extension([A(typeof(I<A2>))] int i) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type t) { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int i) where T : I<A2> { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(I`1<A>))] System.Int32 i)", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type)*/(typeof(I`1<A>))] System.Int32 i)", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.True(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_40()
    {
        // types from different assemblies in nested position in typeof
        var libComp1 = CreateCompilation("public struct A { }", assemblyName: "assembly1");
        var libComp2 = CreateCompilation("public struct A { }", assemblyName: "assembly2");

        var src = """
extern alias alias1;
extern alias alias2;
using A1 = alias1::A;
using A2 = alias2::A;

public static class E
{
    extension([A([typeof(I<A1>)])] int i) { }
    extension([A([typeof(I<A2>)])] int i) { }
}

public class AAttribute : System.Attribute
{
    public AAttribute(System.Type[] t) { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, references: [libComp1.EmitToImageReference().WithAliases(["alias1"]), libComp2.EmitToImageReference().WithAliases(["alias2"])]);
        comp.VerifyEmitDiagnostics(
            // (9,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int i) where T : I<A2> { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(9, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.Multiple(
            () => AssertEx.Equal("extension(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type[])*/([typeof(I`1<A>)])] System.Int32 i)", extension1.ComputeExtensionMarkerRawName()),

            () => AssertEx.Equal("extension(System.Int32)", extension2.ComputeExtensionGroupingRawName()),
            () => AssertEx.Equal("extension([AAttribute/*(System.Type[])*/([typeof(I`1<A>)])] System.Int32 i)", extension2.ComputeExtensionMarkerRawName()),

            () => Assert.True(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2)),
            () => Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2))
        );
    }

    [Fact]
    public void Grouping_41()
    {
        // Function pointer type: ref vs. out
        var src = """
unsafe static class E
{
    extension(delegate*<ref int, void>[]) { }
    extension(delegate*<out int, void>[]) { }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_42()
    {
        // Constraints: new() vs. not
        var src = """
static class E
{
    extension<T>(int) where T : new() { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_43()
    {
        // Constraints: class vs. not
        var src = """
static class E
{
    extension<T>(int) where T : class { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_44()
    {
        // Constraints: struct vs. not
        var src = """
static class E
{
    extension<T>(int) where T : class { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_45()
    {
        // Constraints: allows ref struct vs. not
        var src = """
static class E
{
    extension<T>(int) where T : allows ref struct { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_46()
    {
        // Constraints: unmanaged vs. not
        var src = """
static class E
{
    extension<T>(int) where T : unmanaged { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_47()
    {
        // Constraints: variance difference
        var src = """
static class E
{
    extension<out T>(int) { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
            //     extension<out T>(int) { }
            Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(3, 15),
            // (4,5): error CS9329: This extension block collides with another extension block. They result in conflicting content-based type names in metadata, so must be in separate enclosing static classes.
            //     extension<T>(int) { }
            Diagnostic(ErrorCode.ERR_ExtensionBlockCollision, "extension").WithLocation(4, 5));

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];
        Assert.True(extension1.ComputeExtensionGroupingRawName() == extension2.ComputeExtensionGroupingRawName());
        Assert.True(extension1.ComputeExtensionMarkerRawName() == extension2.ComputeExtensionMarkerRawName());
        // Note: the extension grouping raw name doesn't account for variance, but the IL-level comparer considers it.
        // Consider ignoring variance in IL-level comparison too, to reduce a cascading diagnostic in this error scenario.
        Assert.False(ExtensionGroupingInfo.HaveSameILSignature(extension1, extension2));
        Assert.False(ExtensionGroupingInfo.HaveSameCSharpSignature(extension1, extension2));
    }

    [Fact]
    public void Grouping_48()
    {
        // difference in conditional attribute
        var src = """
public static partial class E
{
    extension([A] int) { }
}
""";
        var defineTestDirective = """
#define TEST

""";

        var src2 = """
public static partial class E
{
    extension([A] int) { }
}
""";
        var src3 = """
[System.Diagnostics.Conditional("TEST")]
public class AAttribute : System.Attribute { }
""";
        // attribute excluded
        var comp = CreateCompilation([src, src2, src3]);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        // attribute included by condition
        comp = CreateCompilation([src, defineTestDirective + src2, src3]);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_49()
    {
        // difference in conditional attribute
        var src = """
public static partial class E
{
    extension([A] int) { }
    extension(int) { }
}

[System.Diagnostics.Conditional("TEST")]
public class AAttribute : System.Attribute { }
""";

        var defineTestDirective = """
#define TEST

""";

        // attribute excluded
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        // attribute included by condition
        comp = CreateCompilation(defineTestDirective + src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_50()
    {
        // difference in conditional attribute
        var src = """
public static partial class E
{
    extension([B] int) { }
}
""";
        var defineTestDirective = """
#define TEST

""";

        var src2 = """
public static partial class E
{
    extension([A, B] int) { }
}
""";
        var src3 = """
[System.Diagnostics.Conditional("TEST")]
public class AAttribute : System.Attribute { }

public class BAttribute : System.Attribute { }
""";
        // attribute excluded
        var comp = CreateCompilation([src, src2, src3]);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        // attribute included by condition
        comp = CreateCompilation([src, defineTestDirective + src2, src3]);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Equal("extension([BAttribute/*()*/] System.Int32)", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName());
        AssertEx.Equal("extension([AAttribute/*()*/] [BAttribute/*()*/] System.Int32)", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName());
    }

    [Fact]
    public void Grouping_51()
    {
        // difference in nullability in constraints: annotated vs. unannotated
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I { }
    extension<T>(int) where T : I? { }
}

public interface I { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        var extension1 = (SourceNamedTypeSymbol)extensions[0];
        var extension2 = (SourceNamedTypeSymbol)extensions[1];

        Assert.Multiple(
            () => Assert.Equal("extension<(I)>(System.Int32)", extension1.ComputeExtensionGroupingRawName()),
            () => Assert.Equal("extension<(I)>(System.Int32)", extension2.ComputeExtensionGroupingRawName()),

            () => Assert.Equal("extension<T>(System.Int32) where T : I!", extension1.ComputeExtensionMarkerRawName()),
            () => Assert.Equal("extension<T>(System.Int32) where T : I?", extension2.ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_52()
    {
        // difference in nullability in constraints: annotated vs. oblivious
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I?
    {
    }

    extension<T>(int) where T :
#nullable disable
        I
#nullable enable
    {
    }
}

public interface I { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => Assert.Equal("extension<T>(System.Int32) where T : I?", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => Assert.Equal("extension<T>(System.Int32) where T : I", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_53()
    {
        // difference in nullability in constraints: type constraints with different nullabilities
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I1?, I2 { }
    extension<T>(int) where T : I1, I2?  { }
}

public interface I1 { }
public interface I2 { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => Assert.Equal("extension<T>(System.Int32) where T : I1?, I2!", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => Assert.Equal("extension<T>(System.Int32) where T : I1!, I2?", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_54()
    {
        // difference in nullability in constraints: type constraints with different nullabilities
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I1, 
#nullable disable
        I2
#nullable enable
    {
    }

    extension<T>(int) where T : 
#nullable disable
        I1
#nullable enable
        , I2
    {
    }
}

public interface I1 { }
public interface I2 { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => Assert.Equal("extension<T>(System.Int32) where T : I1!, I2", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => Assert.Equal("extension<T>(System.Int32) where T : I1, I2!", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_55()
    {
        // difference in nested nullability in constraints: annotated vs. unannotated
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I<object> { }
    extension<T>(int) where T : I<object?> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I<System.Object!>!", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I<System.Object?>!", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_56()
    {
        // difference in nested nullability in constraints: annotated vs. oblivious
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I<object?>
    {
        public static void M1() { }
    }
    extension<T>(int) where T : I<
#nullable disable
        object
#nullable enable
    >
    {
        public static void M2() { }
    }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I<System.Object?>!", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I<System.Object>!", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_57()
    {
        // difference in nested tuple names in constraints
        var src = """
#nullable enable

public static partial class E
{
    extension<T>(int) where T : I<(int a, int b)> { }
    extension<T>(int) where T : I<(int, int)> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_58()
    {
        // order of constraints
        var src = """
public static class E
{
    extension<T>(int) where T : I1, I2 { }
    extension<T>(int) where T : I1, I2 { }
}

public interface I1 { }
public interface I2 { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_59()
    {
        // order of constraints
        var src = """
public static class E
{
    extension<T>(int) where T : I1, I2 { }
    extension<T>(int) where T : I2, I1 { }
}

public interface I1 { }
public interface I2 { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_60()
    {
        // arglist
        var src = """
public static class E
{
    extension(__arglist) { }
    extension(__arglist) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
            // (4,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(4, 15));

        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_61()
    {
        // arglist vs. not
        var src = """
public static class E
{
    extension(__arglist) { }
    extension(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS1669: __arglist is not valid in this context
            //     extension(__arglist) { }
            Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15));

        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_62()
    {
        // default value
        var src = """
public static class E
{
    extension(int i = 0) { }
    extension(int i = 1) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS9284: The receiver parameter of an extension cannot have a default value
            //     extension(int i = 0) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = 0").WithLocation(3, 15),
            // (4,15): error CS9284: The receiver parameter of an extension cannot have a default value
            //     extension(int i = 1) { }
            Diagnostic(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, "int i = 1").WithLocation(4, 15));

        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_63()
    {
        var src = """
public static class E
{
    extension<T>(int) { }
    extension<T, T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,18): error CS0692: Duplicate type parameter 'T'
            //     extension<T, T>(int) { }
            Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T").WithLocation(4, 18));

        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_64()
    {
        // function pointer in constraints: ref vs. ref readonly
        var src = """
public unsafe static class E
{
    extension<T>(int) where T : I<delegate*<ref int, void>[]> { }
    extension<T>(int) where T : I<delegate*<ref readonly int, void>[]> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_65()
    {
        // function pointer in constraints: ref vs. out
        var src = """
public unsafe static class E
{
    extension<T>(int) where T : I<delegate*<ref int, void>[]> { }
    extension<T>(int) where T : I<delegate*<out int, void>[]> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_66()
    {
        // function pointer in constraints: calling convention difference
        var src = """
public unsafe static class E
{
    extension<T>(int) where T : I<delegate* unmanaged[Cdecl]<void>[]> { }
    extension<T>(int) where T : I<delegate* unmanaged[Stdcall]<void>[]> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_67()
    {
        var src = """
public static class E
{
    extension(error) { }
    extension(error2) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,15): error CS0246: The type or namespace name 'error' could not be found (are you missing a using directive or an assembly reference?)
            //     extension(error) { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error").WithArguments("error").WithLocation(3, 15),
            // (4,15): error CS0246: The type or namespace name 'error2' could not be found (are you missing a using directive or an assembly reference?)
            //     extension(error2) { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "error2").WithArguments("error2").WithLocation(4, 15));

        VerifyCollisions(comp, groupingMatch: false, markerMatch: false);
    }

    [Fact]
    public void Grouping_68()
    {
        var src = """
public static class E
{
    extension(scoped ref RS s) { }
    extension(ref RS s) { }
}

public ref struct RS { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_69()
    {
        var src = """
public static class E
{
    extension<T>(T) { }
    extension<T>(T) { }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
    }

    [Fact]
    public void Grouping_70()
    {
        var src = """
public static class E<T>
{
    extension(T) { }
    extension(T) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension(T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(4, 5),
            // (3,5): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //     extension(T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(3, 5));
    }

    [Fact]
    public void Grouping_71()
    {
        var src = """
extension<T>(T) { }
extension<T>(T) { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension<T>(T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(1, 1),
            // (2,1): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            // extension<T>(T) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(2, 1));
    }

    [Fact]
    public void Grouping_72()
    {
        var src = """
public static class E1<T1>
{
    public static class E2<T2>
    {
        extension((T1, T2)) { }
        extension((T1, T2)) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (6,9): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //         extension((T1, T2)) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(6, 9),
            // (5,9): error CS9283: Extensions must be declared in a top-level, non-generic, static class
            //         extension((T1, T2)) { }
            Diagnostic(ErrorCode.ERR_BadExtensionContainingType, "extension").WithLocation(5, 9));
    }

    [Fact]
    public void Grouping_73()
    {
        var src = """
public static class E
{
    extension(object) { }
    extension(dynamic) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,15): error CS1103: The receiver parameter of an extension cannot be of type 'dynamic'
            //     extension(dynamic) { }
            Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(4, 15));

        VerifyCollisions(comp, groupingMatch: true, markerMatch: false);
    }

    [Fact]
    public void Grouping_74()
    {
        var src = """
public static class E
{
    extension<T>(int) where T : I { }
    extension<T>(int) where T : I, I { }
}

public interface I { }
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,36): error CS0405: Duplicate constraint 'I' for type parameter 'T'
            //     extension<T>(int) where T : I, I { }
            Diagnostic(ErrorCode.ERR_DuplicateBound, "I").WithArguments("I", "T").WithLocation(4, 36));

        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32) where T : I", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_75()
    {
        var src = """
public static class E
{
    extension<T>(int) where T : System.Object { }
    extension<T>(int) { }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,33): error CS0702: Constraint cannot be special class 'object'
            //     extension<T>(int) where T : System.Object { }
            Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "System.Object").WithArguments("object").WithLocation(3, 33));

        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);

        var extensions = comp.GetMember<NamedTypeSymbol>("E").GetTypeMembers();
        Assert.Multiple(
            () => AssertEx.Equal("extension<T>(System.Int32)", ((SourceNamedTypeSymbol)extensions[0]).ComputeExtensionMarkerRawName()),
            () => AssertEx.Equal("extension<T>(System.Int32)", ((SourceNamedTypeSymbol)extensions[1]).ComputeExtensionMarkerRawName())
        );
    }

    [Fact]
    public void Grouping_76()
    {
        var src = """
public static class E
{
    extension<T>(int) where T : I<T> { }
    extension<T>(int) where T : I<T> { }
}

public interface I<T> { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp).VerifyDiagnostics();
        VerifyCollisions(comp, groupingMatch: true, markerMatch: true);
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
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( int32 '' ) cil managed 
            {
                ret
            }
        }
        .method public hidebysig static void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 42 41 34 31 43 46 45 32 42
                35 45 44 41 45 42 38 43 31 42 39 30 36 32 46 35
                39 45 44 34 44 36 39 00 00
            )
            ldnull
            throw
        }
    }
}
""" + ExtensionMarkerAttributeIL;

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

        comp = CreateCompilationWithIL(src, ilSrc, parseOptions: TestOptions.Regular14);
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
    .class nested public auto ansi sealed specialname '<G>$DCC6408136F6EFC8A90FB693F174BE24'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$586604688281C9157DFFE75E9BF93DF3'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C`1<int32> source
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x212f
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$586604688281C9157DFFE75E9BF93DF3'::'<Extension>$'
        } // end of class <M>$586604688281C9157DFFE75E9BF93DF3
        // Methods
        .method public hidebysig 
            instance class C`1<string> SelectMany (
                class [mscorlib]System.Func`2<int32, class C`1<int32>> collectionSelector,
                class [mscorlib]System.Func`3<int32, int32, string> resultSelector
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 35 38 36 36 30 34 36 38 38
                32 38 31 43 39 31 35 37 44 46 46 45 37 35 45 39
                42 46 39 33 44 46 33 00 00
            )
            // Method begins at RVA 0x212c
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$DCC6408136F6EFC8A90FB693F174BE24'::SelectMany
        .method public hidebysig 
            instance class C`1<!!T> Cast<T> () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 35 38 36 36 30 34 36 38 38
                32 38 31 43 39 31 35 37 44 46 46 45 37 35 45 39
                42 46 39 33 44 46 33 00 00
            )
            // Method begins at RVA 0x212c
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$DCC6408136F6EFC8A90FB693F174BE24'::Cast
    } // end of class <G>$DCC6408136F6EFC8A90FB693F174BE24
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
  IInvocationOperation ( C<System.String> E.<G>$DCC6408136F6EFC8A90FB693F174BE24.SelectMany(System.Func<System.Int32, C<System.Int32>> collectionSelector, System.Func<System.Int32, System.Int32, System.String> resultSelector)) (OperationKind.Invocation, Type: C<System.String>, IsImplicit) (Syntax: 'from int y  ... ew C<int>()')
    Instance Receiver:
      IInvocationOperation ( C<System.Int32> E.<G>$DCC6408136F6EFC8A90FB693F174BE24.Cast<System.Int32>()) (OperationKind.Invocation, Type: C<System.Int32>, IsImplicit) (Syntax: 'from int x  ... ew C<int>()')
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
                      IInvocationOperation ( C<System.Int32> E.<G>$DCC6408136F6EFC8A90FB693F174BE24.Cast<System.Int32>()) (OperationKind.Invocation, Type: C<System.Int32>, IsImplicit) (Syntax: 'new C<int>()')
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

    [Fact]
    public void CollectionExpression_01()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension(C c)
    {
        public void Add(int i) { System.Console.Write(i); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_02()
    {
        var src = """
C c = [1];

static class E
{
    extension(C c)
    {
        public void Add<T>(int i) { System.Console.Write(i); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,7): error CS9215: Collection expression type 'C' must have an instance or extension method 'Add' that can be called with a single argument.
            // C c = [1];
            Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[1]").WithArguments("C").WithLocation(1, 7));
    }

    [Fact]
    public void CollectionExpression_03()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension(C c)
    {
        public void Add<T>(T t) { System.Console.Write(t); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_04()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension<T>(C c)
    {
        public void Add(int i) { System.Console.Write(i); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,7): error CS9215: Collection expression type 'C' must have an instance or extension method 'Add' that can be called with a single argument.
            // C c = [1, 2];
            Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[1, 2]").WithArguments("C").WithLocation(1, 7));
    }

    [Fact]
    public void CollectionExpression_05()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension<T>(C c)
    {
        public void Add(T t) { System.Console.Write(t); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_06()
    {
        // Based on the following, but without an implementation method
        //public static class E
        //{
        //    extension(C c)
        //    {
        //        public void Add<T>(T t) { }
        //    }
        //}
        //public class C : System.Collections.IEnumerable
        //{
        //    public System.Collections.IEnumerator GetEnumerator() => null;
        //}
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$9794DAFCCB9E752B29BFD6350ADA77F2'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$73AD9F89912BC4337338E3DE7182B785'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( class C c ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }
        .method public hidebysig instance void Add<T> ( !!T t ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 37 33 41 44 39 46 38 39 39
                31 32 42 43 34 33 33 37 33 33 38 45 33 44 45 37
                31 38 32 42 37 38 35 00 00
            )
            ldnull
            throw
        }
    }
}
.class public auto ansi beforefieldinit C
    extends System.Object
    implements [mscorlib]System.Collections.IEnumerable
{
    .method public final hidebysig newslot virtual instance class [mscorlib]System.Collections.IEnumerator GetEnumerator () cil managed 
    {
        ldnull
        ret
    }
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
C c = [1];
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        comp.VerifyEmitDiagnostics(
            // (1,7): error CS0570: 'E.extension(C).Add<T>(T)' is not supported by the language
            // C c = [1];
            Diagnostic(ErrorCode.ERR_BindToBogus, "[1]").WithArguments("E.extension(C).Add<T>(T)").WithLocation(1, 7));
    }

    [Fact]
    public void CollectionExpression_07()
    {
        // Based on the following, but without an implementation method
        //public static class E
        //{
        //    extension(C c)
        //    {
        //        public void Add<T>(T t) { }
        //    }
        //}
        //public class C : System.Collections.IEnumerable
        //{
        //    public System.Collections.IEnumerator GetEnumerator() => null;
        //}
        var ilSrc = """
.class public auto ansi abstract sealed beforefieldinit E
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$9794DAFCCB9E752B29BFD6350ADA77F2'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$73AD9F89912BC4337338E3DE7182B785'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( class C c ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }
        .method public hidebysig instance void Add<T> ( !!T t ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 37 33 41 44 39 46 38 39 39
                31 32 42 43 34 33 33 37 33 33 38 45 33 44 45 37
                31 38 32 42 37 38 35 00 00
            )
            ldnull
            throw
        }
    }
}
.class public auto ansi beforefieldinit C
    extends System.Object
    implements [mscorlib]System.Collections.IEnumerable
{
    .method public final hidebysig newslot virtual instance class [mscorlib]System.Collections.IEnumerator GetEnumerator () cil managed 
    {
        ldnull
        ret
    }
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
""" + ExtensionMarkerAttributeIL;

        var src = """
C c = [1];

public static class E2
{
    extension(C c)
    {
        public void Add(int i) { System.Console.Write("ran"); }
    }
}
""";
        var comp = CreateCompilationWithIL(src, ilSrc);
        CompileAndVerify(comp, expectedOutput: "ran").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_08()
    {
        var src = """
int[] i = new[] { 1, 2 };
C c = /*<bind>*/ [1, .. i] /*</bind>*/;

static class E1
{
    extension(C c)
    {
        public void Add(int i) { }
    }
}

static class E2
{
    extension(C c)
    {
        public void Add(int i) { }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,19): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension(C).Add(int)' and 'E2.extension(C).Add(int)'
            // C c = /*<bind>*/ [1, .. i] /*</bind>*/;
            Diagnostic(ErrorCode.ERR_AmbigCall, "1").WithArguments("E1.extension(C).Add(int)", "E2.extension(C).Add(int)").WithLocation(2, 19),
            // (2,25): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension(C).Add(int)' and 'E2.extension(C).Add(int)'
            // C c = /*<bind>*/ [1, .. i] /*</bind>*/;
            Diagnostic(ErrorCode.ERR_AmbigCall, "i").WithArguments("E1.extension(C).Add(int)", "E2.extension(C).Add(int)").WithLocation(2, 25));

        VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp, """
ICollectionExpressionOperation (2 elements, ConstructMethod: C..ctor()) (OperationKind.CollectionExpression, Type: C, IsInvalid) (Syntax: '[1, .. i]')
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      ISpreadOperation (ElementType: System.Int32) (OperationKind.Spread, Type: null, IsInvalid) (Syntax: '.. i')
        Operand:
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32[], IsInvalid) (Syntax: 'i')
        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          (Identity)
""");
    }

    [Fact]
    public void CollectionExpression_09()
    {
        // Element type cannot be determined from a GetEnumerator extension method
        // new extension
        string src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> c = [];

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection<T>
{
}

class MyCollectionBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
}

static class E
{
    extension<T>(MyCollection<T> c)
    {
        public IEnumerator<T> GetEnumerator() => default;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,23): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
            // MyCollection<int> c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<int>").WithLocation(5, 23));

        // classic extension
        src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> c = [];

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection<T>
{
}

class MyCollectionBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
}

static class E
{
    public static IEnumerator<T> GetEnumerator<T>(this MyCollection<T> c) => default;
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,23): error CS9188: 'MyCollection<int>' has a CollectionBuilderAttribute but no element type.
            // MyCollection<int> c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderNoElementType, "[]").WithArguments("MyCollection<int>").WithLocation(5, 23));

        // non-extension method
        src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> c = [];

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection<T>
{
    public IEnumerator<T> GetEnumerator() => default;
}

class MyCollectionBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
}

static class E
{
    extension<T>(MyCollection<T> c)
    {
    }
}
""";
        comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void CollectionExpression_10()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension(C c)
    {
        public static void Add(int i) { }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,7): error CS1921: The best overloaded method match for 'E.extension(C).Add(int)' has wrong signature for the initializer element. The initializable Add must be an accessible instance method.
            // C c = [1, 2];
            Diagnostic(ErrorCode.ERR_InitializerAddHasWrongSignature, "[1, 2]").WithArguments("E.extension(C).Add(int)").WithLocation(1, 7));
    }

    [Fact]
    public void CollectionExpression_11()
    {
        var src = """
C c = [1, 2];

static class E
{
    extension(C c)
    {
        public static void Add() { }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (1,7): error CS1501: No overload for method 'Add' takes 1 arguments
            // C c = [1, 2];
            Diagnostic(ErrorCode.ERR_BadArgCount, "[1, 2]").WithArguments("Add", "1").WithLocation(1, 7));
    }

    [Fact]
    public void CollectionExpression_12()
    {
        var src = """
D d = [1, 2];

static class E
{
    extension(C c)
    {
        public void Add(int i) { System.Console.Write(i); }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}

public class D : C { }
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_13()
    {
        var src = """
C c = new C();
D d = [.. c];

static class E
{
    extension(C c)
    {
        public System.Collections.Generic.IEnumerator<int> GetEnumerator()
        {
            yield return 1;
            yield return 2; 
        }
    }
}

public class C { }

public class D : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
    public void Add(int i) { System.Console.Write(i); }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void CollectionExpression_14()
    {
        // Static Create extension methods does not count as a blessed Create method
        string src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> c1 = [];
MyCollection<int> c2 = [1];

[CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
class MyCollection<T>
{
    public IEnumerator<T> GetEnumerator() => default;
}

class MyCollectionBuilder
{
}

static class E
{
    extension(MyCollectionBuilder)
    {
        public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
    }
}
""";
        var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,24): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // MyCollection<int> c1 = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(5, 24),
            // (6,24): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // MyCollection<int> c2 = [1];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[1]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 24));
    }

    [Fact]
    public void CollectionExpression_15()
    {
        // extension countable property
        var src = """
C c = new C();
int[] i = [.. c];
System.Console.Write((i[0], i[1]));

static class E
{
    extension(C c)
    {
        public int Length => throw null;
    }
}

public class C 
{ 
    public System.Collections.Generic.IEnumerator<int> GetEnumerator()
    {
        yield return 1;
        yield return 2; 
    }
}
""";
        var comp = CreateCompilation(src);
        var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2)").VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (int[] V_0, //i
                System.Collections.Generic.List<int> V_1,
                System.Collections.Generic.IEnumerator<int> V_2,
                int V_3)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  newobj     "System.Collections.Generic.List<int>..ctor()"
  IL_000a:  stloc.1
  IL_000b:  callvirt   "System.Collections.Generic.IEnumerator<int> C.GetEnumerator()"
  IL_0010:  stloc.2
  .try
  {
    IL_0011:  br.s       IL_0021
    IL_0013:  ldloc.2
    IL_0014:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_0019:  stloc.3
    IL_001a:  ldloc.1
    IL_001b:  ldloc.3
    IL_001c:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
    IL_0021:  ldloc.2
    IL_0022:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0027:  brtrue.s   IL_0013
    IL_0029:  leave.s    IL_0035
  }
  finally
  {
    IL_002b:  ldloc.2
    IL_002c:  brfalse.s  IL_0034
    IL_002e:  ldloc.2
    IL_002f:  callvirt   "void System.IDisposable.Dispose()"
    IL_0034:  endfinally
  }
  IL_0035:  ldloc.1
  IL_0036:  callvirt   "int[] System.Collections.Generic.List<int>.ToArray()"
  IL_003b:  stloc.0
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.0
  IL_003e:  ldelem.i4
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.1
  IL_0041:  ldelem.i4
  IL_0042:  newobj     "System.ValueTuple<int, int>..ctor(int, int)"
  IL_0047:  box        "System.ValueTuple<int, int>"
  IL_004c:  call       "void System.Console.Write(object)"
  IL_0051:  ret
}
""");

        // non-extension countable property
        src = """
C c = new C();
int[] i = [.. c];
System.Console.Write((i[0], i[1]));

public class C 
{ 
    public System.Collections.Generic.IEnumerator<int> GetEnumerator()
    {
        yield return 1;
        yield return 2; 
    }

    public int Length => 2;
}
""";
        comp = CreateCompilation(src);
        verifier = CompileAndVerify(comp, expectedOutput: "(1, 2)").VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (int[] V_0, //i
                int V_1,
                int[] V_2,
                System.Collections.Generic.IEnumerator<int> V_3,
                int V_4)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldc.i4.0
  IL_0006:  stloc.1
  IL_0007:  dup
  IL_0008:  callvirt   "int C.Length.get"
  IL_000d:  newarr     "int"
  IL_0012:  stloc.2
  IL_0013:  callvirt   "System.Collections.Generic.IEnumerator<int> C.GetEnumerator()"
  IL_0018:  stloc.3
  .try
  {
    IL_0019:  br.s       IL_002c
    IL_001b:  ldloc.3
    IL_001c:  callvirt   "int System.Collections.Generic.IEnumerator<int>.Current.get"
    IL_0021:  stloc.s    V_4
    IL_0023:  ldloc.2
    IL_0024:  ldloc.1
    IL_0025:  ldloc.s    V_4
    IL_0027:  stelem.i4
    IL_0028:  ldloc.1
    IL_0029:  ldc.i4.1
    IL_002a:  add
    IL_002b:  stloc.1
    IL_002c:  ldloc.3
    IL_002d:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
    IL_0032:  brtrue.s   IL_001b
    IL_0034:  leave.s    IL_0040
  }
  finally
  {
    IL_0036:  ldloc.3
    IL_0037:  brfalse.s  IL_003f
    IL_0039:  ldloc.3
    IL_003a:  callvirt   "void System.IDisposable.Dispose()"
    IL_003f:  endfinally
  }
  IL_0040:  ldloc.2
  IL_0041:  stloc.0
  IL_0042:  ldloc.0
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.i4
  IL_0045:  ldloc.0
  IL_0046:  ldc.i4.1
  IL_0047:  ldelem.i4
  IL_0048:  newobj     "System.ValueTuple<int, int>..ctor(int, int)"
  IL_004d:  box        "System.ValueTuple<int, int>"
  IL_0052:  call       "void System.Console.Write(object)"
  IL_0057:  ret
}
""");
    }

    [Fact]
    public void ParamsCollection_01()
    {
        var src = """
local([1, 2]);

void local(params C c)
{
}

static class E
{
    extension(C c)
    {
        public void Add(int i) { }
    }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,12): error CS9227: 'C' does not contain a definition for a suitable instance 'Add' method
            // void local(params C c)
            Diagnostic(ErrorCode.ERR_ParamsCollectionExtensionAddMethod, "params C c").WithArguments("C").WithLocation(3, 12));

        src = """
local([1, 2]);

void local(params C c)
{
}

static class E
{
    public static void Add(this C c, int i) { }
}

public class C : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => null;
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,12): error CS9227: 'C' does not contain a definition for a suitable instance 'Add' method
            // void local(params C c)
            Diagnostic(ErrorCode.ERR_ParamsCollectionExtensionAddMethod, "params C c").WithArguments("C").WithLocation(3, 12));
    }

    [Fact]
    public void Using_01()
    {
        // non-extension static members are brought in scope
        var src = """
using static E;

M();
System.Console.Write(get_P());
set_P(43);
_ = op_Addition(0, 0);
_ = new object() + new object();

static class E
{
    extension(object)
    {
        public static void M() { System.Console.Write("M "); }
        public static int P { get => 42; set { System.Console.Write($" {value}"); } }
        public static object operator +(object o1, object o2) { System.Console.Write(" +"); return o1; }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "M 42 43 + +").VerifyDiagnostics();
    }

    [Fact]
    public void Using_02()
    {
        var src = """
using static E;

_ = P;
P = 43;

static class E
{
    extension(object)
    {
        public static int P { get => throw null; set { } }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,5): error CS0103: The name 'P' does not exist in the current context
            // _ = P;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P").WithArguments("P").WithLocation(3, 5),
            // (4,1): error CS0103: The name 'P' does not exist in the current context
            // P = 43;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P").WithArguments("P").WithLocation(4, 1));
    }

    [Fact]
    public void Using_03()
    {
        // non-extension static members are brought in scope
        var src = """
using static E;

System.Console.Write(get_P(2));
set_P(3, 43);

static class E
{
    extension(int i)
    {
        public int P { get { System.Console.Write($"get({i}) "); return 42; } set { System.Console.Write($" set({i}, {value})"); } }
    }
}
""";
        var comp = CreateCompilation(src);
        CompileAndVerify(comp, expectedOutput: "get(2) 42 set(3, 43)").VerifyDiagnostics();
    }

    [Fact]
    public void Using_04()
    {
        // using static to import an extension method, used as static method
        var src = """
using static E;

M(1);

static class E
{
    extension(int i)
    {
        public void M() { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,1): error CS0103: The name 'M' does not exist in the current context
            // M(1);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(3, 1));

        src = """
using static E;

M(1);

static class E
{
    public static void M(this int i) { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (3,1): error CS0103: The name 'M' does not exist in the current context
            // M(1);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(3, 1));

        src = """
using static E;

M(1);

static class E
{
    public static void M(int i) { }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void Using_05()
    {
        // using static to import an extension method, used as extension
        var src = """
namespace N1 
{
    static class E
    {
        extension(int i)
        {
            public void M() { System.Console.WriteLine($"M({i})"); }
        }
    }
}

namespace N2
{
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            42.M();
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "M(42)").VerifyDiagnostics();
    }

    [Fact]
    public void Using_06()
    {
        // using static to import an extension method, used as static method
        var src = """
namespace N1 
{
    static class E
    {
        extension(int i)
        {
            public void M() { }
        }
    }
}

namespace N2
{
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            M(1);
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (20,13): error CS0103: The name 'M' does not exist in the current context
            //             M(1);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(20, 13));

        src = """
namespace N1 
{
    static class E
    {
        public static void M(this int i) { }
    }
}

namespace N2
{
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            M(1);
        }
    }
}
""";
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (17,13): error CS0103: The name 'M' does not exist in the current context
            //             M(1);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(17, 13));
    }

    [Fact]
    public void Using_07()
    {
        // using static to import an extension property, used as extension
        var src = """
namespace N1 
{
    static class E
    {
        extension(int i)
        {
            public int P => 42;
        }
    }
}

namespace N2
{
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            System.Console.Write(0.P);
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Using_08()
    {
        // using static to import an extension operator, used as extension
        var src = """
namespace N1 
{
    static class E
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("+"); return c1; }
        }
    }
}

namespace N2
{
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            _ = new C() + new C();
        }
    }
}

public class C { }
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "+").VerifyDiagnostics();
    }

    [Fact]
    public void Using_09()
    {
        // importing with both using static and using, extension method
        var src = """
namespace N1 
{
    static class E
    {
        extension(int i)
        {
            public void M() { System.Console.WriteLine($"M({i})"); }
        }
    }
}

namespace N2
{
    using N1;
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            42.M();
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "M(42)").VerifyDiagnostics();
    }

    [Fact]
    public void Using_10()
    {
        // importing with both using static and using, extension property
        var src = """
namespace N1 
{
    static class E
    {
        extension(int i)
        {
            public int P => 42;
        }
    }
}

namespace N2
{
    using N1;
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            System.Console.Write(0.P);
        }
    }
}
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void Using_11()
    {
        // importing with both using static and using, extension operator
        var src = """
namespace N1 
{
    static class E
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("+"); return c1; }
        }
    }
}

namespace N2
{
    using N1;
    using static N1.E;

    public static class B
    {
        public static void Main()
        {
            _ = new C() + new C();
        }
    }
}

public class C { }
""";
        var comp = CreateCompilation(src, options: TestOptions.DebugExe);
        CompileAndVerify(comp, expectedOutput: "+").VerifyDiagnostics();
    }

    [Fact]
    public void Using_12()
    {
        // tracking unnecessary imports
        var src = """
using N1;
using N2;

new object().M1();

namespace N1 
{
    static class E1
    {
        public static void M1(this object o) { }
    }
}

namespace N2
{
    static class E2
    {
        public static void M2(this object o) { }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using N2;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));

        src = """
using N1;
using N2;

new object().M1();

namespace N1 
{
    static class E1
    {
        public static void M1(this object o) { }
    }
}

namespace N2
{
    static class E2
    {
        extension(object o)
        {
            public void M2() { }
        }
    }
}
""";
        // Tracked by https://github.com/dotnet/roslyn/issues/79440 : using directives, consider refining used imports logic
        comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics();
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_01(bool useCompilationReference, bool withPreserve)
    {
        // attribute on extension type parameter (with and without CompilerLoweringPreserve attribute)
        var libSrc = $$"""
public static class E
{
    extension<[A] T>(int i)
    {
        public void M() { }
    }
}

{{(withPreserve ? "[System.Runtime.CompilerServices.CompilerLoweringPreserve]" : "")}}
public class AAttribute : System.Attribute { }
""" + CompilerLoweringPreserveAttributeDefinition;

        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        CompileAndVerify(libComp).VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$B8D310208B4544F25EEBACB9990FC73B'<$T0>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$73F5560BE55A0A0B23905153DB511F4E'<T>
            extends [mscorlib]System.Object
        {
            .param type T
                .custom instance void AAttribute::.ctor() = (
                    01 00 00 00
                )
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    int32 i
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$73F5560BE55A0A0B23905153DB511F4E'::'<Extension>$'
        } // end of class <M>$73F5560BE55A0A0B23905153DB511F4E
        // Methods
        .method public hidebysig 
            instance void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 37 33 46 35 35 36 30 42 45
                35 35 41 30 41 30 42 32 33 39 30 35 31 35 33 44
                42 35 31 31 46 34 45 00 00
            )
            // Method begins at RVA 0x2069
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$B8D310208B4544F25EEBACB9990FC73B'::M
    } // end of class <G>$B8D310208B4544F25EEBACB9990FC73B
    // Methods
    .method public hidebysig static 
        void M<T> (
            int32 i
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param type T
            .custom instance void AAttribute::.ctor() = (
                01 00 00 00
            )
        // Method begins at RVA 0x2067
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method E::M
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.TypeParameters.Single().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.True(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.TypeParameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_02(bool useCompilationReference)
    {
        // attribute on method type parameter
        var libSrc = """
public static class E
{
    extension(int)
    {
        public static void M<[A] T>() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionMethod = extension.GetMember<MethodSymbol>("M");
            Assert.Equal("AAttribute", extensionMethod.TypeParameters.Single().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.TypeParameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_03(bool useCompilationReference)
    {
        // attribute on extension parameter
        var libSrc = """
public static class E
{
    extension([A] int i)
    {
        public void M() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.ExtensionParameter.GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.True(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.Parameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_04(bool useCompilationReference)
    {
        // attribute on method parameter
        var libSrc = """
public static class E
{
    extension(int i1)
    {
        public void M([A] int i2) { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionMethod = extension.GetMember<MethodSymbol>("M");
            Assert.Equal("AAttribute", extensionMethod.Parameters.Last().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.True(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.Parameters.Last().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_05(bool useCompilationReference)
    {
        // attribute on method
        var libSrc = """
public static class E
{
    extension(int)
    {
        [A]
        public static void M() { }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionMethod = extension.GetMember<MethodSymbol>("M");
            Assert.Equal("AAttribute", extensionMethod.GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_06(bool useCompilationReference)
    {
        // attribute on property
        var libSrc = """
public static class E
{
    extension(int)
    {
        [A]
        public static int P => 0;
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionProperty = extension.GetMember<PropertySymbol>("P");
            Assert.Equal("AAttribute", extensionProperty.GetAttributes().Single().ToString());

            var getterImplementation = comp.GlobalNamespace.GetTypeMember("E").GetMember<MethodSymbol>("get_P");
            Assert.False(getterImplementation.IsExtensionMethod);
            Assert.Empty(getterImplementation.GetAttributes());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_07(bool useCompilationReference)
    {
        // attribute on extension type parameter for property
        var libSrc = """
public static class E
{
    extension<[A] T>(T t)
    {
        public int P => 0;
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.TypeParameters.Single().GetAttributes().Single().ToString());

            var getterImplementation = comp.GlobalNamespace.GetTypeMember("E").GetMember<MethodSymbol>("get_P");
            Assert.False(getterImplementation.IsExtensionMethod);
            Assert.Equal("AAttribute", getterImplementation.TypeParameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_08(bool useCompilationReference)
    {
        // attribute on accessor
        var libSrc = """
public static class E
{
    extension(int)
    {
        public static int P
        {
            [A]
            get => 0;
        }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionProperty = extension.GetMember<PropertySymbol>("P");
            Assert.Empty(extensionProperty.GetAttributes());
            var extensionAccessor = extension.GetMember<MethodSymbol>("get_P");
            Assert.Equal("AAttribute", extensionAccessor.GetAttributes().Single().ToString());

            var getterImplementation = comp.GlobalNamespace.GetTypeMember("E").GetMember<MethodSymbol>("get_P");
            Assert.False(getterImplementation.IsExtensionMethod);
            Assert.Equal("AAttribute", getterImplementation.GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_09(bool useCompilationReference)
    {
        // attribute on extension type parameter for operator
        var libSrc = """
public static class E
{
    extension<[A] T>(T)
    {
        public static T operator +(T t1, T t2) => throw null;
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.TypeParameters.Single().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("op_Addition");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.TypeParameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_10(bool useCompilationReference)
    {
        // attribute on extension parameter for operator
        var libSrc = """
public static class E
{
    extension([A] C)
    {
        public static C operator +(C c1, C c2) => throw null;
    }
}

public class AAttribute : System.Attribute { }
public class C { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.ExtensionParameter.GetAttributes().Single().ToString());

            var extensionOperator = extension.GetMember<MethodSymbol>("op_Addition");
            foreach (var implementationParameter in extensionOperator.Parameters)
            {
                Assert.Empty(implementationParameter.GetAttributes());
            }

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("op_Addition");
            Assert.False(implementation.IsExtensionMethod);
            foreach (var implementationParameter in implementation.Parameters)
            {
                Assert.Empty(implementationParameter.GetAttributes());
            }
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_11(bool useCompilationReference)
    {
        // attribute on extension operator
        var libSrc = """
public static class E
{
    extension(C)
    {
        [A]
        public static C operator +(C c1, C c2) => throw null;
    }
}

public class AAttribute : System.Attribute { }
public class C { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionOperator = extension.GetMember<MethodSymbol>("op_Addition");
            Assert.Equal("AAttribute", extensionOperator.GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("op_Addition");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_12(bool useCompilationReference)
    {
        // attribute on extension operator parameter
        var libSrc = """
public static class E
{
    extension(C)
    {
        public static C operator +([A] C c1, [B] C c2) => throw null;
    }
}

public class AAttribute : System.Attribute { }
public class BAttribute : System.Attribute { }
public class C { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionOperator = extension.GetMember<MethodSymbol>("op_Addition");
            Assert.Equal("AAttribute", extensionOperator.Parameters.First().GetAttributes().Single().ToString());
            Assert.Equal("BAttribute", extensionOperator.Parameters.Last().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("op_Addition");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.Parameters.First().GetAttributes().Single().ToString());
            Assert.Equal("BAttribute", implementation.Parameters.Last().GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData]
    public void PropagateAttributes_13(bool useCompilationReference)
    {
        // unmanaged constraint on extension type parameter
        var libSrc = """
public static class E
{
    extension<T>(int i) where T : unmanaged
    {
        public void M() { }
    }
}
""";

        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        CompileAndVerify(libComp).VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$8A1E908054B5C3DCE56554F1F294FA98'<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) $T0>
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param type $T0
            .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
                01 00 00 00
            )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A888E0AEEFB4AB1872CCB8E7D5472CC8'<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T>
            extends [mscorlib]System.Object
        {
            .param type T
                .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
                    01 00 00 00
                )
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    int32 i
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x2067
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A888E0AEEFB4AB1872CCB8E7D5472CC8'::'<Extension>$'
        } // end of class <M>$A888E0AEEFB4AB1872CCB8E7D5472CC8
        // Methods
        .method public hidebysig 
            instance void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 38 38 38 45 30 41 45 45
                46 42 34 41 42 31 38 37 32 43 43 42 38 45 37 44
                35 34 37 32 43 43 38 00 00
            )
            // Method begins at RVA 0x2069
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$8A1E908054B5C3DCE56554F1F294FA98'::M
    } // end of class <G>$8A1E908054B5C3DCE56554F1F294FA98
    // Methods
    .method public hidebysig static 
        void M<valuetype .ctor (class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType)) T> (
            int32 i
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param type T
            .custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
                01 00 00 00
            )
        // Method begins at RVA 0x2067
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method E::M
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.True(extension.TypeParameters.Single().HasUnmanagedTypeConstraint);

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("M");
            Assert.True(implementation.IsExtensionMethod);
            Assert.True(implementation.TypeParameters.Single().HasUnmanagedTypeConstraint);
        }
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829 extension indexers"), CombinatorialData]
    public void PropagateAttributes_14(bool useCompilationReference)
    {
        // attribute on extension indexer
        var libSrc = """
public static class E
{
    extension(int i1)
    {
        [A]
        public this[int i2] => 0;
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionIndexer = extension.GetMember<PropertySymbol>("Item");
            Assert.Equal("AAttribute", extensionIndexer.GetAttributes().Single().ToString());
            var extensionGetter = extension.GetMember<MethodSymbol>("get_Item");
            Assert.Empty(extensionGetter.GetAttributes());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("get_Item");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Empty(implementation.GetAttributes());
        }
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829 extension indexers"), CombinatorialData]
    public void PropagateAttributes_15(bool useCompilationReference)
    {
        // attribute on parameters for extension indexer
        var libSrc = """
public static class E
{
    extension([A] int i1)
    {
        public this[[B] int i2] => 0;
    }
}

public class AAttribute : System.Attribute { }
public class BAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.ExtensionParameter.GetAttributes().Single().ToString());

            var extensionIndexer = extension.GetMember<PropertySymbol>("Item");
            Assert.Empty(extensionIndexer.GetAttributes());
            Assert.Equal("BAttribute", extensionIndexer.Parameters.Single().GetAttributes().Single().ToString());

            var extensionGetter = extension.GetMember<MethodSymbol>("get_Item");
            Assert.Equal("BAttribute", extensionGetter.Parameters.Single().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("get_Item");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.Parameters.First().GetAttributes().Single().ToString());
            Assert.Equal("BAttribute", implementation.Parameters.Last().GetAttributes().Single().ToString());
        }
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829 extension indexers"), CombinatorialData]
    public void PropagateAttributes_16(bool useCompilationReference)
    {
        // attribute on type parameters for extension indexer
        var libSrc = """
public static class E
{
    extension<[A] T>(T t)
    {
        public this[int i] => 0;
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            Assert.Equal("AAttribute", extension.TypeParameters.Single().GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("get_Item");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.TypeParameters.Single().GetAttributes().Single().ToString());
        }
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/78829 extension indexers"), CombinatorialData]
    public void PropagateAttributes_17(bool useCompilationReference)
    {
        // attribute on accessor for extension indexer
        var libSrc = """
public static class E
{
    extension(int i1)
    {
        public this[int i2]
        {
            [A]
            get => 0;
        }
    }
}

public class AAttribute : System.Attribute { }
""";
        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);

            var extensionGetter = extension.GetMember<MethodSymbol>("get_Item");
            Assert.Equal("AAttribute", extensionGetter.GetAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("get_Item");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.GetAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/80017")]
    public void PropagateAttributes_18(bool useCompilationReference)
    {
        // return attribute on property
        var libSrc = """
public static class E
{
    extension(int i)
    {
        public int P
        {
            [return: A]
            get => 0;
        }
    }
}

public class AAttribute : System.Attribute { }
""";

        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        CompileAndVerify(libComp).VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static
                void '<Extension>$' (
                    int32 i
                ) cil managed
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x206d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$F4B4FFE41AB49E80A4ECF390CF6EB372'::'<Extension>$'
        } // end of class <M>$F4B4FFE41AB49E80A4ECF390CF6EB372
        // Methods
        .method public hidebysig specialname
            instance int32 get_P () cil managed
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .param [0]
                .custom instance void AAttribute::.ctor() = (
                    01 00 00 00
                )
            // Method begins at RVA 0x206a
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_P
        // Properties
        .property instance int32 P()
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 46 34 42 34 46 46 45 34 31
                41 42 34 39 45 38 30 41 34 45 43 46 33 39 30 43
                46 36 45 42 33 37 32 00 00
            )
            .get instance int32 E/'<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::get_P()
        }
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method public hidebysig static
        int32 get_P (
            int32 i
        ) cil managed
    {
        .param [0]
            .custom instance void AAttribute::.ctor() = (
                01 00 00 00
            )
        // Method begins at RVA 0x2067
        // Code size 2 (0x2)
        .maxstack 8
        IL_0000: ldc.i4.0
        IL_0001: ret
    } // end of method E::get_P
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionProperty = extension.GetMember<PropertySymbol>("P");
            Assert.Empty(extensionProperty.GetAttributes());
            var extensionGetter = extensionProperty.GetMethod;
            Assert.Equal("AAttribute", extensionGetter.GetReturnTypeAttributes().Single().ToString());

            var implementation = (MethodSymbol)comp.GlobalNamespace.GetTypeMember("E").GetMember("get_P");
            Assert.False(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.GetReturnTypeAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/80017")]
    public void PropagateAttributes_19(bool useCompilationReference)
    {
        // return attribute on method
        var libSrc = """
public static class E
{
    extension(int i)
    {
        [return: A]
        public void M() { }
    }
}

public class AAttribute : System.Attribute { }
""";

        var libComp = CreateCompilation(libSrc);
        validate(libComp);

        var comp = CreateCompilation("", references: [AsReference(libComp, useCompilationReference)]);
        validate(comp);

        static void validate(CSharpCompilation comp)
        {
            var extension = comp.GlobalNamespace.GetTypeMember("E").GetTypeMembers().Single();
            Assert.True(extension.IsExtension);
            var extensionMethod = extension.GetMember<MethodSymbol>("M");
            Assert.Equal("AAttribute", extensionMethod.GetReturnTypeAttributes().Single().ToString());

            var implementation = comp.GlobalNamespace.GetTypeMember("E").GetMember<MethodSymbol>("M");
            Assert.True(implementation.IsExtensionMethod);
            Assert.Equal("AAttribute", implementation.GetReturnTypeAttributes().Single().ToString());
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/80017")]
    public void PropagateAttributes_20(bool withPreserve)
    {
        // attributes on local function in extension
        var src = $$"""
public static class E
{
    extension(int i1)
    {
        public void M()
        {
            local(0);

            [return: A]
            [B]
            void local([C] int i2) { }
        }
    }
}

{{(withPreserve ? "[System.Runtime.CompilerServices.CompilerLoweringPreserve]" : "")}}
public class AAttribute : System.Attribute { }

{{(withPreserve ? "[System.Runtime.CompilerServices.CompilerLoweringPreserve]" : "")}}
public class BAttribute : System.Attribute { }

{{(withPreserve ? "[System.Runtime.CompilerServices.CompilerLoweringPreserve]" : "")}}
public class CAttribute : System.Attribute { }
""";

        var verifier = CompileAndVerify([src, CompilerLoweringPreserveAttributeDefinition]).VerifyDiagnostics();
        verifier.VerifyTypeIL("E", """
.class public auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$531E7AC45D443AE2243E7FFAB9455D60'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    int32 i1
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x206f
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$531E7AC45D443AE2243E7FFAB9455D60'::'<Extension>$'
        } // end of class <M>$531E7AC45D443AE2243E7FFAB9455D60
        // Methods
        .method public hidebysig 
            instance void M () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 35 33 31 45 37 41 43 34 35
                44 34 34 33 41 45 32 32 34 33 45 37 46 46 41 42
                39 34 35 35 44 36 30 00 00
            )
            // Method begins at RVA 0x2071
            // Code size 2 (0x2)
            .maxstack 8
            IL_0000: ldnull
            IL_0001: throw
        } // end of method '<G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69'::M
    } // end of class <G>$BA41CFE2B5EDAEB8C1B9062F59ED4D69
    // Methods
    .method public hidebysig static 
        void M (
            int32 i1
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2067
        // Code size 7 (0x7)
        .maxstack 8
        IL_0000: ldc.i4.0
        IL_0001: call void E::'<M>g__local|1_0'(int32)
        IL_0006: ret
    } // end of method E::M
    .method assembly hidebysig static 
        void '<M>g__local|1_0' (
            int32 i2
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void BAttribute::.ctor() = (
            01 00 00 00
        )
        .param [0]
            .custom instance void AAttribute::.ctor() = (
                01 00 00 00
            )
        .param [1]
            .custom instance void CAttribute::.ctor() = (
                01 00 00 00
            )
        // Method begins at RVA 0x206f
        // Code size 1 (0x1)
        .maxstack 8
        IL_0000: ret
    } // end of method E::'<M>g__local|1_0'
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
    }
}

