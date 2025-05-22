// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
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

    [Fact]
    public void AnonymousType_01()
    {
        var src = """
var person = new { Name = "John", Age = 30 };
person.M();
person.M2();
_ = person.P;

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
        // Tracked by https://github.com/dotnet/roslyn/issues/76130 : should work
        var comp = CreateCompilation(src);
        comp.VerifyEmitDiagnostics(
            // (4,12): error CS1061: '<anonymous type: string Name, int Age>' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type '<anonymous type: string Name, int Age>' could be found (are you missing a using directive or an assembly reference?)
            // _ = person.P;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("<anonymous type: string Name, int Age>", "P").WithLocation(4, 12));
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

object? oNotNull2 = new object();
oNotNull2.P = new object();

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
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 14));
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
        // Tracked by https://github.com/dotnet/roslyn/issues/76130 : incorrect nullability analysis for property pattern with extension property (unexpected warning)
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
}

