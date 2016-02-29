// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class ReplaceOriginalTests : CSharpTestBase
    {
        [Fact]
        public void Members()
        {
            var source =
@"using System;
partial struct S
{
    object F() { return null; }
    static int P { get; set; }
    object this[int index] { get { return null; } }
    event EventHandler E { add { } remove { } }
}
partial struct S
{
    replace object F() { return original(); }
    replace static int P
    {
        get { return original; }
        set { original += value; }
    }
    replace object this[int index]
    {
        get { return original[index]; }
    }
    replace event EventHandler E
    {
        add { original += value; }
        remove { original -= value; }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
            // PROTOTYPE(generators): Check ReplacedMethod/Property/Event.
            // PROTOTYPE(generators): Check generated metadata includes replaced accessors but not replaced property or event.
        }

        [Fact]
        public void ExpressionBodiedMembers()
        {
            var source =
@"class C
{
    static object F() => null;
    replace static object F() => original();
    int P => 1;
    replace int P => original + 1;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation);
            VerifyIL(verifier, GetMethodByName(verifier.TestData, "C", "get_P"),
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.<get_P>v__0()""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}");
        }

        [Fact]
        public void Fields()
        {
            var source =
@"class C
{
    static object F;
    replace static object F;
    replace object G;
    object G;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,27): error CS0106: The modifier 'replace' is not valid for this item
                //     replace static object F;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F").WithArguments("replace").WithLocation(4, 27),
                // (5,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace object G;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "G").WithArguments("replace").WithLocation(5, 20),
                // (4,27): error CS0102: The type 'C' already contains a definition for 'F'
                //     replace static object F;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "F").WithArguments("C", "F").WithLocation(4, 27),
                // (6,12): error CS0102: The type 'C' already contains a definition for 'G'
                //     object G;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "G").WithArguments("C", "G").WithLocation(6, 12),
                // (4,27): warning CS0169: The field 'C.F' is never used
                //     replace static object F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(4, 27),
                // (6,12): warning CS0169: The field 'C.G' is never used
                //     object G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(6, 12),
                // (5,20): warning CS0169: The field 'C.G' is never used
                //     replace object G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(5, 20),
                // (3,19): warning CS0169: The field 'C.F' is never used
                //     static object F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(3, 19));
        }

        [Fact]
        public void Types()
        {
            var source =
@"replace class A
{
}
class B
{
    class C { }
    replace class C { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (1,15): error CS0106: The modifier 'replace' is not valid for this item
                // replace class A
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "A").WithArguments("replace").WithLocation(1, 15),
                // (6,11): error CS0106: The modifier 'replace' is not valid for this item
                //     class C { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(6, 11),
                // (7,19): error CS0102: The type 'B' already contains a definition for 'C'
                //     replace class C { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "C").WithArguments("B", "C").WithLocation(7, 19));
        }

        [Fact]
        public void Interface()
        {
            var source =
@"interface I
{
    void F();
    replace void F();
    object P { get; }
    replace object P { get; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,18): error CS0106: The modifier 'replace' is not valid for this item
                //     replace void F();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F").WithArguments("replace").WithLocation(4, 18),
                // (6,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace object P { get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("replace").WithLocation(6, 20),
                // (4,18): error CS0111: Type 'I' already defines a member called 'F' with the same parameter types
                //     replace void F();
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "F").WithArguments("F", "I").WithLocation(4, 18),
                // (6,20): error CS0102: The type 'I' already contains a definition for 'P'
                //     replace object P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("I", "P").WithLocation(6, 20));
        }

        [Fact]
        public void Accessors()
        {
            var source =
@"using System;
class C
{
    static int P { get; replace set; }
    object this[int index] { replace get { return null; } }
    event EventHandler E { replace add { } remove { } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,25): error CS1014: A get or set accessor expected
                //     static int P { get; replace set; }
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "replace").WithLocation(4, 25),
                // (5,30): error CS1014: A get or set accessor expected
                //     object this[int index] { replace get { return null; } }
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "replace").WithLocation(5, 30),
                // (6,28): error CS1055: An add or remove accessor expected
                //     event EventHandler E { replace add { } remove { } }
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "replace").WithLocation(6, 28));
        }

        [Fact]
        public void Constructors()
        {
            var source =
@"class C
{
    static C() { }
    replace static C() { }
    internal C() { }
    replace internal C() { }
}
struct S
{
    internal S(object o) { }
    replace internal S(object o) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace static C() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(4, 20),
                // (6,22): error CS0106: The modifier 'replace' is not valid for this item
                //     replace internal C() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(6, 22),
                // (4,20): error CS0111: Type 'C' already defines a member called '.cctor' with the same parameter types
                //     replace static C() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments(".cctor", "C").WithLocation(4, 20),
                // (6,22): error CS0111: Type 'C' already defines a member called '.ctor' with the same parameter types
                //     replace internal C() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments(".ctor", "C").WithLocation(6, 22),
                // (11,22): error CS0106: The modifier 'replace' is not valid for this item
                //     replace internal S(object o) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("replace").WithLocation(11, 22),
                // (11,22): error CS0111: Type 'S' already defines a member called '.ctor' with the same parameter types
                //     replace internal S(object o) { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "S").WithArguments(".ctor", "S").WithLocation(11, 22));
        }

        [Fact]
        public void DefaultConstructors()
        {
            var source =
@"class C
{
    replace public C() { }
}
struct S
{
    replace public S() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (3,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace public C() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(3, 20),
                // (7,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace public S() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("replace").WithLocation(7, 20),
                // (7,20): error CS0568: Structs cannot contain explicit parameterless constructors
                //     replace public S() { }
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "S").WithLocation(7, 20));
        }

        [Fact]
        public void ImplicitConstructors()
        {
            var source =
@"class C
{
    static object F() { return null; }
    static object x = F();
    object y = F();
    replace static C() { }
    replace public C() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (6,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace static C() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(6, 20),
                // (7,20): error CS0106: The modifier 'replace' is not valid for this item
                //     replace public C() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("replace").WithLocation(7, 20));
        }

        [Fact]
        public void Destructors()
        {
            var source =
@"class C
{
    ~C() { }
    replace ~C() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,13): error CS1519: Invalid token '~' in class, struct, or interface member declaration
                //     replace ~C() { }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "~").WithArguments("~").WithLocation(4, 13),
                // (4,14): error CS0111: Type 'C' already defines a member called '~C' with the same parameter types
                //     replace ~C() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("~C", "C").WithLocation(4, 14));
        }

        [Fact]
        public void FinalizeMethod()
        {
            var source =
@"class C
{
    ~C() { }
    replace protected override void Finalize() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,37): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     replace protected override void Finalize() { }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize").WithLocation(4, 37),
                // (4,37): error CS0249: Do not override object.Finalize. Instead, provide a destructor.
                //     replace protected override void Finalize() { }
                Diagnostic(ErrorCode.ERR_OverrideFinalizeDeprecated, "Finalize").WithLocation(4, 37));
        }

        [Fact]
        public void ExplicitImplementation()
        {
            var source =
@"using System;
interface I
{
    void F();
    object P { get; }
    object this[object index] { get; set; }
    event EventHandler E;
}
class C : I
{
    replace void I.F()
    {
        original();
    }
    replace object I.P
    {
        get { return original; }
    }
    replace object I.this[int index]
    {
        get { return original[index]; }
        set { original[index] = value; }
    }
    replace event EventHandler I.E
    {
        add { original += value; }
        remove { original -= value; }
    }
    void I.F() { }
    object I.P { get { return null; } }
    object I.this[object index] { get { return null; } set { } }
    event EventHandler I.E { add { } remove { } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            // PROTOTYPE(generators): Should note report any errors.
            compilation.VerifyDiagnostics(
                // (19,22): error CS0539: 'C.this[int]' in explicit interface declaration is not a member of interface
                //     replace object I.this[int index]
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("C.this[int]").WithLocation(19, 22),
                // (21,22): error CS8944: No original member for 'C.this[int]'
                //         get { return original[index]; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.this[int]").WithLocation(21, 22),
                // (22,15): error CS8944: No original member for 'C.this[int]'
                //         set { original[index] = value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.this[int]").WithLocation(22, 15));
        }

        [Fact]
        public void MultipleReplaces()
        {
            var source =
@"using System;
class C
{
    static void M() { }
    replace static void M() { original(); }
    replace static void M() { original(); }
    replace object P => original;
    object P => 1;
    replace object P => original;
    replace event EventHandler E
    {
        add { original += value; }
        remove { original -= value; }
    }
    replace event EventHandler E
    {
        add { original += value; }
        remove { original -= value; }
    }
    event EventHandler E;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (6,25): error CS8945: Type already contains a replacement for 'C.M()'
                //     replace static void M() { original(); }
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "M").WithArguments("C.M()").WithLocation(6, 25),
                // (9,20): error CS8945: Type already contains a replacement for 'C.P'
                //     replace object P => original;
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "P").WithArguments("C.P").WithLocation(9, 20),
                // (9,25): error CS8945: Type already contains a replacement for 'C.P.get'
                //     replace object P => original;
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "original").WithArguments("C.P.get").WithLocation(9, 25),
                // (15,32): error CS8945: Type already contains a replacement for 'C.E'
                //     replace event EventHandler E
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "E").WithArguments("C.E").WithLocation(15, 32),
                // (17,9): error CS8945: Type already contains a replacement for 'C.E.add'
                //         add { original += value; }
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "add").WithArguments("C.E.add").WithLocation(17, 9),
                // (18,9): error CS8945: Type already contains a replacement for 'C.E.remove'
                //         remove { original -= value; }
                Diagnostic(ErrorCode.ERR_DuplicateReplace, "remove").WithArguments("C.E.remove").WithLocation(18, 9),
                // (6,25): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                //     replace static void M() { original(); }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(6, 25),
                // (9,20): error CS0102: The type 'C' already contains a definition for 'P'
                //     replace object P => original;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(9, 20),
                // (15,32): error CS0102: The type 'C' already contains a definition for 'E'
                //     replace event EventHandler E
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(15, 32),
                // (17,15): error CS8944: No original member for 'C.E'
                //         add { original += value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.E").WithLocation(17, 15),
                // (18,18): error CS8944: No original member for 'C.E'
                //         remove { original -= value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.E").WithLocation(18, 18),
                // (20,24): warning CS0067: The event 'C.E' is never used
                //     event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(20, 24));
        }

        [Fact]
        public void NoOriginal()
        {
            var source =
@"using System;
class C
{
    replace object F() { return original(); }
    replace static object P
    {
        get { return original; }
        set { original = value; }
    }
    replace object this[int index]
    {
        get { return original[index]; }
        set { original[index] = value; }
    }
    replace event EventHandler E
    {
        add { original += value; }
        remove { original -= value; }
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,33): error CS8944: No original member for 'C.F()'
                //     replace object F() { return original(); }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.F()").WithLocation(4, 33),
                // (7,22): error CS8944: No original member for 'C.P'
                //         get { return original; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.P").WithLocation(7, 22),
                // (8,15): error CS8944: No original member for 'C.P'
                //         set { original = value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.P").WithLocation(8, 15),
                // (12,22): error CS8944: No original member for 'C.this[int]'
                //         get { return original[index]; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.this[int]").WithLocation(12, 22),
                // (13,15): error CS8944: No original member for 'C.this[int]'
                //         set { original[index] = value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.this[int]").WithLocation(13, 15),
                // (17,15): error CS8944: No original member for 'C.E'
                //         add { original += value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.E").WithLocation(17, 15),
                // (18,18): error CS8944: No original member for 'C.E'
                //         remove { original -= value; }
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.E").WithLocation(18, 18));

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<OriginalExpressionSyntax>().ToArray();
            Assert.Equal(7, exprs.Length);
            foreach (var expr in exprs)
            {
                var symbolInfo = model.GetSymbolInfo(expr);
                var symbol = symbolInfo.Symbol;
                // PROTOTYPE(generators): Assert this is the replace symbol.
            }
        }

        [Fact]
        public void DifferentSignatures()
        {
            var source =
@"class C
{
    // static and instance
    static void M1() { }
    replace void M1() { }
    // instance and static
    void M2() { }
    replace static void M2() { }
    // parameter names
    replace static void M3(object x) { }
    static void M3(object y) { }
    // params and array
    static void M4(params object[] a) { }
    replace static void M4(object[] a) { }
    // return type
    replace static object M5() { return original(); }
    static string M5() { return null; }
    // ref and value
    static void M6(ref object o) { }
    replace static void M6(object o) { }
    // ref and out
    replace static void M7(ref object o) { o = null; }
    static void M7(out object o) { o = null; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void DifferentTypeParameters()
        {
            var source =
@"class C
{
    static void M<T>() { }
    replace static void M<U>() { }
    static void N<T>() where T : struct { }
    replace static void N<T>() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void Abstract()
        {
            var source =
@"abstract class C
{
    internal abstract object P { get; }
    internal abstract void F();
    replace internal abstract object P { get; }
    replace internal virtual void F() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void Virtual()
        {
            var source =
@"using System;
class C
{
    internal virtual object P { get; }
    internal virtual void F() { }
    replace internal virtual object P
    {
        get { return original; }
    }
    replace internal virtual void F()
    {
        if (true)
        {
            original();
            G(original);
            G(new Action(original));
        }
    }
    static void G(Action a) { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation);
            VerifyIL(verifier, GetMethodByName(verifier.TestData, "C", "get_P"),
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object C.<get_P>v__0()""
  IL_0006:  ret
}");
            VerifyIL(verifier, GetMethodByName(verifier.TestData, "C", "F"),
@"{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""void C.<F>v__0()""
  IL_0006:  ldarg.0
  IL_0007:  dup
  IL_0008:  ldvirtftn  ""void C.<F>v__0()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  call       ""void C.G(System.Action)""
  IL_0018:  ldarg.0
  IL_0019:  dup
  IL_001a:  ldvirtftn  ""void C.<F>v__0()""
  IL_0020:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0025:  call       ""void C.G(System.Action)""
  IL_002a:  ret
}");
            // Verify IOperation references to 'original' are not virtual.
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.IfStatement);
            var rootOp = model.GetOperation(expr);
            var ops = rootOp.Descendants().ToArray();
            foreach (var op in ops)
            {
                switch (op.Kind)
                {
                    case OperationKind.InvocationExpression:
                        var invocation = (IInvocationExpression)op;
                        Assert.False(invocation.IsVirtual);
                        break;
                    case OperationKind.MethodBindingExpression:
                        var methodBinding = (IMethodBindingExpression)op;
                        Assert.False(methodBinding.IsVirtual);
                        break;
                }
            }
        }

        [Fact]
        public void Overridden()
        {
            var source =
@"abstract class A
{
    internal abstract object P { get; }
    internal virtual void F() { }
}
class B : A
{
    replace internal sealed override object P { get { return original; } }
    replace internal override void F() { original(); }
    internal sealed override object P { get { return null; } }
    internal override void F() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation);
            VerifyIL(verifier, GetMethodByName(verifier.TestData, "B", "F"),
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void B.<F>v__0()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void Attributes()
        {
            var source =
@"class A : System.Attribute
{
    public object F;
}
class C
{
    [A(F = 1)] static void F() { }
    [A(F = 1)] replace static void F<U>() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void DifferentAttributes()
        {
            var source =
@"class A : System.Attribute
{
    public object F;
}
class C
{
    [A] static void F() { }
    replace static void F() { }
    void G() { }
    [A] replace void G() { }
    [A(F = 1)] void H() { }
    [A(F = 2)] replace void H() { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void Modifiers()
        {
            // PROTOTYPE(generators): Test extern, partial, unsafe, async
        }

        [Fact]
        public void MissingSet()
        {
            var source =
@"class C
{
    object P { get; }
    replace object P { get { original = null; return null; } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,30): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //     replace object P { get { original = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "original").WithArguments("C.P").WithLocation(4, 30));
        }

        [Fact]
        public void MissingGet()
        {
            var source =
@"class C
{
    int P { set { } }
    replace int P { set { original += value; } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,27): error CS0154: The property or indexer 'C.P' cannot be used in this context because it lacks the get accessor
                //     replace int P { set { original += value; } }
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "original").WithArguments("C.P").WithLocation(4, 27));
        }

        [Fact]
        public void OriginalAsDelegate()
        {
            var source =
@"class C
{
    static void F(System.Action a)
    {
    }
    static void M()
    {
    }
    replace static void M()
    {
        F(original);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void OriginalInLambda()
        {
            var source =
@"using System;
class C
{
    static void F(System.Action a)
    {
        a();
    }
    static void M()
    {
        Console.Write(1);
    }
    replace static void M()
    {
        Console.Write(2);
        F(() => original());
    }
    static void Main()
    {
        M();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"21");
        }

        [Fact]
        public void MissingAccessors()
        {
            var source =
@"struct S
{
    object P { get; set; }
    object Q { get; set; }
    replace object P { get { return null; } }
    replace object Q { set { } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void AdditionalAccessors()
        {
            var source =
@"class C
{
    object P { get; }
    object Q { set { } }
    replace object P { get { return null; } set { } }
    replace object Q { get { return null; } set { } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void DifferentAccessors()
        {
            var source =
@"struct S
{
    object P { get; }
    object Q { set { } }
    replace object P { set { } }
    replace object Q { get { return null; } }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(); // PROTOTYPE(generators): Report errors.
        }

        [Fact]
        public void ReplaceWithAutoProperty()
        {
            var source =
@"class C
{
    object P { get; }
    object Q { get; set; }
    object R { get { return null; } }
    object S { get { return null; } set { } }
    replace object P { get; }
    replace object Q { get; set; }
    replace object R { get; }
    replace object S { get; set; }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void ReplaceWithFieldLikeEvent()
        {
            var source =
@"using System;
class C
{
    event EventHandler E;
    event EventHandler F { add { } remove { } }
    replace event EventHandler E;
    replace event EventHandler F;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (4,24): warning CS0067: The event 'C.E' is never used
                //     event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 24),
                // (7,32): warning CS0067: The event 'C.F' is never used
                //     replace event EventHandler F;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "F").WithArguments("C.F").WithLocation(7, 32),
                // (6,32): warning CS0067: The event 'C.E' is never used
                //     replace event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(6, 32));
        }

        [Fact]
        public void ParamsArray()
        {
            var source =
@"class C
{
    static void M(params object[] a)
    {
    }
    replace static void M(params object[] a)
    {
        original(a);
        original(a[0], a[1]);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// Replaced generic method. Original reference
        /// must use type arguments from replace method.
        /// </summary>
        [Fact]
        public void GenericMethod()
        {
            var source =
@"class C<T>
{
    static U M<U>() => default(U);
    replace static U M<U>() => original();
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void OriginalMethodDifferentTypeArguments()
        {
            var source =
@"class C
{
    static void M<T, U>(T t, U u) where U : T
    {
    }
    replace static void M<T, U>(T t, U u) where U : T
    {
        original(t, default(U));
        original(u, 2);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (9,21): error CS1503: Argument 2: cannot convert from 'int' to 'U'
                //         original(u, 2);
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("2", "int", "U").WithLocation(9, 21));
        }

        [Fact]
        public void OriginalMethodExplicitTypeArguments()
        {
            var source =
@"class C
{
    static void M<T>(T t)
    {
    }
    replace static void M<T>(T t)
    {
        original<T>(default(T));
        original<int>(2);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            // PROTOTYPE(generators): We might be able to report better errors if 'original'
            // is not treated as a (contextual) keyword by the parser.
            // (ParseTerm is not calling ParseAliasQualifiedName in this case.)
            compilation.VerifyDiagnostics(
                // (9,18): error CS1525: Invalid expression term 'int'
                //         original<int>(2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(9, 18),
                // (8,18): error CS0119: 'T' is a type, which is not valid in the given context
                //         original<T>(default(T));
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type").WithLocation(8, 18),
                // (8,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         original<T>(default(T));
                Diagnostic(ErrorCode.ERR_IllegalStatement, "original<T>(default(T))").WithLocation(8, 9));
        }

        [Fact]
        public void PartialReplace()
        {
            var source =
@"partial class C
{
    partial void M1()
    {
    }
    static partial void M2();
    void M3()
    {
    }
    static void M4()
    {
    }
    replace partial void M1();
    replace static partial void M2()
    {
    }
    replace static partial void M3();
    replace partial void M4()
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (13,26): error CS8946: Replacement methods cannot be partial
                //     replace partial void M1();
                Diagnostic(ErrorCode.ERR_PartialReplace, "M1").WithLocation(13, 26),
                // (14,33): error CS8946: Replacement methods cannot be partial
                //     replace static partial void M2()
                Diagnostic(ErrorCode.ERR_PartialReplace, "M2").WithLocation(14, 33),
                // (17,33): error CS8946: Replacement methods cannot be partial
                //     replace static partial void M3();
                Diagnostic(ErrorCode.ERR_PartialReplace, "M3").WithLocation(17, 33),
                // (18,26): error CS8946: Replacement methods cannot be partial
                //     replace partial void M4()
                Diagnostic(ErrorCode.ERR_PartialReplace, "M4").WithLocation(18, 26),
                // (18,26): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M4()'
                //     replace partial void M4()
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M4").WithArguments("C.M4()").WithLocation(18, 26),
                // (17,33): error CS0111: Type 'C' already defines a member called 'M3' with the same parameter types
                //     replace static partial void M3();
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M3").WithArguments("M3", "C").WithLocation(17, 33),
                // (18,26): error CS0111: Type 'C' already defines a member called 'M4' with the same parameter types
                //     replace partial void M4()
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M4").WithArguments("M4", "C").WithLocation(18, 26));
        }

        // PROTOTYPE(generators): Should allow replacing partial methods.
        [Fact]
        public void ReplacePartial()
        {
            var source =
@"partial class C
{
    partial void M1()
    {
    }
    static partial void M2();
    replace void M1()
    {
        original();
    }
    replace static void M2()
    {
        original();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (3,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M1()'
                //     partial void M1()
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M1").WithArguments("C.M1()").WithLocation(3, 18),
                // (7,18): error CS0111: Type 'C' already defines a member called 'M1' with the same parameter types
                //     replace void M1()
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C").WithLocation(7, 18),
                // (11,25): error CS0111: Type 'C' already defines a member called 'M2' with the same parameter types
                //     replace static void M2()
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M2").WithArguments("M2", "C").WithLocation(11, 25),
                // (9,9): error CS8944: No original member for 'C.M1()'
                //         original();
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.M1()").WithLocation(9, 9),
                // (13,9): error CS8944: No original member for 'C.M2()'
                //         original();
                Diagnostic(ErrorCode.ERR_NoOriginalMember, "original").WithArguments("C.M2()").WithLocation(13, 9));
        }

        [Fact]
        public void MethodNamedOriginal()
        {
            var source =
@"using System;
class C
{
    void original(bool b)
    {
        Console.Write(1);
    }
    replace void original(bool b)
    {
        Console.Write(2);
        original(b);
        if (b)
        {
            @original(false);
            this.original(false);
        }
    }
    static void Main()
    {
        (new C()).original(true);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation, expectedOutput: @"212121");
            VerifyIL(verifier, GetMethodByName(verifier.TestData, "C", "original"),
@"{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  call       ""void C.<original>v__0(bool)""
  IL_000d:  ldarg.1
  IL_000e:  brfalse.s  IL_001e
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.0
  IL_0012:  call       ""void C.original(bool)""
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.0
  IL_0019:  call       ""void C.original(bool)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void EntryPoint()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Console.Write(1);
    }
    replace static void Main()
    {
        Console.Write(2);
        original();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation, expectedOutput: @"21");
            var entryPoint = (SourceMethodSymbol)compilation.GetEntryPoint(default(CancellationToken));
            Assert.True(entryPoint.IsReplace);
            var method = GetMethodByName(verifier.TestData, "C", "Main");
            Assert.Equal("Main", method.MetadataName);
            VerifyIL(verifier, method,
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  call       ""void C.<Main>v__0()""
  IL_000b:  ret
}");
            method = (MethodSymbol)method.Replaced;
            Assert.Equal("<Main>v__0", method.MetadataName);
            VerifyIL(verifier, method,
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void UseSiteDiagnostics()
        {
            // PROTOTYPE(generators): Can we test useSiteDiagnostics in BindInvocationExpression?
        }

        [Fact]
        public void LookupReplace()
        {
            var source =
@"class C
{
    object F() { return 1; }
    replace object F() { return 2; }
    void M()
    {
        M();
    }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            int position = tree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.InvocationExpression).SpanStart;

            // Lookup all symbols.
            var symbols = model.LookupSymbols(position);
            var method = (MethodSymbol)symbols.Single(s => s.Name == "F");
            Assert.Equal("System.Object C.F()", method.ToTestDisplayString());
            Assert.True(((SourceMethodSymbol)method).IsReplace);
            Assert.Equal("System.Object C.F()", method.Replaced.ToTestDisplayString());
            Assert.Null(method.ReplacedBy);

            // Lookup by name.
            symbols = model.LookupSymbols(position, name: "F");
            Assert.Equal(1, symbols.Length);
            method = (MethodSymbol)symbols[0];
            Assert.Equal("System.Object C.F()", method.ToTestDisplayString());
            Assert.True(((SourceMethodSymbol)method).IsReplace);
            Assert.Equal("System.Object C.F()", method.Replaced.ToTestDisplayString());
            Assert.Null(method.ReplacedBy);
        }

        private static MethodSymbol GetMethodByName(CompilationTestData testData, string typeName, string methodName)
        {
            return (MethodSymbol)testData.Methods.Keys.Single(m =>
                m.Name == methodName &&
                m.ContainingType.Name == typeName &&
                (object)((MethodSymbol)m).ReplacedBy == null);
        }

        private static void VerifyIL(CompilationVerifier verifier, MethodSymbol method, string expectedIL)
        {
            var methodData = verifier.TestData.Methods[method];
            var actualIL = verifier.VisualizeIL(methodData, realIL: true);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
        }
    }
}
