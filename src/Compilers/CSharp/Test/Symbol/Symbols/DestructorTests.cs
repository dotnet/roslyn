// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class DestructorTests : CSharpTestBase
    {
        [Fact]
        public void Error_Parameters()
        {
            var source = @"
class C
{
    ~C(int x) {}
}";
            // This is a parse error.
            var comp = CreateCompilation(source);
            Assert.NotEmpty(comp.GetParseDiagnostics());
        }

        [Fact]
        public void Error_ReturnValue()
        {
            var source = @"
class C
{
    ~C() { return 1; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,12): error CS0127: Since 'C.~C()' returns void, a return keyword must not be followed by an object expression
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("C.~C()"));
        }

        [Fact]
        public void Error_Duplicate()
        {
            var source = @"
class Q
{
    class C
    {
        ~C() { }
        ~C() { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,10): error CS0111: Type 'Q.C' already defines a member called '~C' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("~C", "Q.C"));
        }

        [Fact]
        public void Error_InsideStructOrInterface()
        {
            var source = @"
struct S
{
    ~S() { }
}

interface I
{
    ~I();
}";
            CreateCompilation(source).VerifyDiagnostics(
                //  error CS0575: Only class types can contain destructors
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "S").WithArguments("S.~S()"),
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "I").WithArguments("I.~I()"),
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "I"));
        }

        [Fact]
        public void Error_Modifiers()
        {
            var source = @"
class C1
{
    public ~C1() { }
}

class C2
{
    virtual ~C2() { }
}

class C3
{
    override ~C3() { }
}

abstract class C4
{
    abstract ~C4();
}

class C5
{
    new ~C5() { }
}

class C6
{
    static ~C6() { }
}

class C7
{
    extern ~C7();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,13): error CS0106: The modifier 'public' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C1").WithArguments("public"),
                // (9,14): error CS0106: The modifier 'virtual' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C2").WithArguments("virtual"),
                // (14,15): error CS0106: The modifier 'override' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C3").WithArguments("override"),
                // (19,15): error CS0106: The modifier 'abstract' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C4").WithArguments("abstract"),
                // (24,10): error CS0106: The modifier 'new' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C5").WithArguments("new"),
                // (29,13): error CS0106: The modifier 'static' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C6").WithArguments("static"),
                // (34,13): warning CS0626: Method, operator, or accessor 'C7.~C7()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "C7").WithArguments("C7.~C7()"));
        }

        [WorkItem(528912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528912")]
        [Fact]
        public void InvokeFinalize()
        {
            var source = @"
using System;

class A
{
    ~A()
    {
        Finalize(); //CS0245
        Action a = Finalize;
    }
}

class B : A
{
    void Goo()
    {
        this.Finalize(); //CS0245
    }
}

class C
{
    public void Finalize()
    {
        Finalize();
        Action a = Finalize;
    }
}

class D : C
{
    void Goo()
    {
        Finalize();
        Action a = Finalize;
    }
}

class E
{
    protected virtual void Finalize() { }
}

class F : E
{
    ~F()
    {
        Finalize(); //CS0245 in Roslyn 
        Action a = Finalize;
    }
}

class G : F
{
    void Goo()
    {
        Finalize(); //CS0245 in Roslyn 
        Action a = Finalize;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (23,17): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (41,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (8,9): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
                Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "Finalize()"),
                // (17,9): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
                Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "this.Finalize()"),

                // These occur in Roslyn, but not dev11, because Roslyn makes F.Finalize a runtime finalizer.

                // (48,9): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
                Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "Finalize()"),
                // (57,9): error CS0245: Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
                Diagnostic(ErrorCode.ERR_CallingFinalizeDeprecated, "Finalize()"));
        }

        [WorkItem(528912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528912")]
        [Fact]
        public void InvokeBaseFinalize()
        {
            var source = @"
using System;

class A
{
    ~A()
    {
        base.Finalize(); //CS0250
        Action a = base.Finalize;
    }
}

class B : A
{
    void Goo()
    {
        base.Finalize(); //CS0250
    }
}

class C
{
    public void Finalize()
    {
        base.Finalize(); //CS0250
        Action a = base.Finalize;
    }
}

class D : C
{
    void Goo()
    {
        base.Finalize();
        Action a = base.Finalize;
    }
}

class E
{
    protected virtual void Finalize() { }
}

class F : E
{
    ~F()
    {
        base.Finalize();
        Action a = base.Finalize;
    }
}

class G : F
{
    void Goo()
    {
        base.Finalize();
        Action a = base.Finalize;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (23,17): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (41,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (8,9): error CS0250: Do not directly call your base class Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()"),
                // (17,9): error CS0250: Do not directly call your base class Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()"),
                // (25,9): error CS0250: Do not directly call your base class Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()"),

                // This is new in Roslyn.  It is reported because F.Finalize is now a runtime finalizer.

                // (57,9): error CS0250: Do not directly call your base class Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()"));
        }

        [Fact]
        public void OverrideFinalize()
        {
            // These are cases where overriding Finalize was okay in dev11.  Error cases are in SymbolErrorTests.
            var source = @"
class A
{
    protected virtual void Finalize() { }
}

class B : A
{
    protected override void Finalize() { }
}

class C : A
{
    ~C() { }
}

class D : C
{
    protected override void Finalize() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (9,29): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (19,29): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),

                // This is new in Roslyn.  It is reported because C.Finalize is now a runtime finalizer.

                // (19,29): error CS0249: Do not override object.Finalize. Instead, provide a destructor.
                Diagnostic(ErrorCode.ERR_OverrideFinalizeDeprecated, "Finalize"));
        }

        [Fact]
        public void ImplementFinalize()
        {
            // Note: this would work if the accessibility was public
            var source = @"
interface I
{
    void Finalize();
}

class C : I
{
    ~C() { }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (4,10): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     void Finalize();
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize").WithLocation(4, 10),
                // (7,11): error CS0737: 'C' does not implement interface member 'I.Finalize()'. 'C.~C()' cannot implement an interface member because it is not public.
                // class C : I
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I").WithArguments("C", "I.Finalize()", "C.~C()").WithLocation(7, 11));
        }

        [WorkItem(528912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528912")]
        [Fact]
        public void DestructorDelegate()
        {
            var source = @"
class C
{
    static void Main()
    {
        C c = new C();
        System.Action a = c.Finalize;
        a();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void SemanticModel()
        {
            var source = @"
class C
{
    ~C() { this.Finalize(); } //NOTE: not legal to call Finalize, but we still want semantic info
}
";

            var compilation = (Compilation)CreateCompilation(source);

            var destructor = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>(WellKnownMemberNames.DestructorName);
            Assert.Equal(MethodKind.Destructor, destructor.MethodKind);
            Assert.Equal(WellKnownMemberNames.DestructorName, destructor.Name);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var destructorDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DestructorDeclarationSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(destructorDecl);
            Assert.NotNull(declaredSymbol);
            Assert.Equal(destructor, declaredSymbol);

            var finalizeSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Name;
            Assert.Equal(WellKnownMemberNames.DestructorName, finalizeSyntax.ToString());

            var info = model.GetSymbolInfo(finalizeSyntax);
            Assert.NotEqual(default, info);
            Assert.Equal(destructor, info.Symbol);

            var lookupSymbols = model.LookupSymbols(finalizeSyntax.SpanStart, name: WellKnownMemberNames.DestructorName);
            Assert.Equal(2, lookupSymbols.Length); // Also includes object.Finalize
            Assert.Contains(destructor, lookupSymbols);
        }

        [Fact]
        public void KeywordIdentifier()
        {
            var source = @"
class @ref
{
    ~@ref() { }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(546830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546830")]
        [Fact]
        public void OverrideSealedFinalizer()
        {
            var il = @"
.class public auto ansi Base
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method family strict virtual final instance void 
          Finalize() cil managed
  {
    ret
  }
}
";

            var source = @"
class Derived : Base
{
    ~Derived() { }
}";
            CreateCompilationWithILAndMscorlib40(source, il).VerifyDiagnostics(
                // (4,6): error CS0239: 'Derived.~Derived()': cannot override inherited member 'Base.~Base()' because it is sealed
                //     ~Derived() { }
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Derived").WithArguments("Derived.~Derived()", "Base.~Base()"));
        }

        [WorkItem(546830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546830")]
        [Fact]
        public void OverrideNewslotVirtualFinalFinalizer()
        {
            var il = @"
.class public auto ansi Base
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method family strict virtual newslot final instance void 
          Finalize() cil managed
  {
    ret
  }
}
";

            var source = @"
class Derived : Base
{
    ~Derived() { }
}";

            // BREAK: Dev11 doesn't report this error, but it does generate code that won't run,
            // so this change is reasonable.
            CreateCompilationWithILAndMscorlib40(source, il).VerifyDiagnostics(
                // (4,6): error CS0239: 'Derived.~Derived()': cannot override inherited member 'Base.Finalize()' because it is sealed
                //     ~Derived() { }
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Derived").WithArguments("Derived.~Derived()", "Base.Finalize()"));
        }

        [WorkItem(528903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528903")]
        [Fact]
        public void AbstractFinalize()
        {
            var vb = @"
Public MustInherit Class Base
    Protected MustOverride Overrides Sub Finalize()
End Class
";

            var source = @"
class Derived : Base
{
    ~Derived() { }
}";

            var vbRef = CreateVisualBasicCompilation("VB", vb).EmitToImageReference();

            // In dev11, compilation succeeded, but the finalizer would fail at runtime when it made
            // a non-virtual call to the abstract method Base.Finalize.
            CreateCompilation(source, new[] { vbRef }).VerifyDiagnostics(
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.~Base()'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.~Base()"));
        }

        [WorkItem(647933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/647933")]
        [Fact]
        public void ConditionalAttributeOnDestructor()
        {
            var source = @"
using System.Diagnostics;
public class Test 
{
    [Conditional(""Debug"")]
    ~Test(){}
    public static int Main()
    {
        return 1;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (5,6): error CS0577: The Conditional attribute is not valid on 'Test.~Test()' because it is a constructor, destructor, operator, or explicit interface implementation
                //     [Conditional("Debug")]
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"Conditional(""Debug"")").WithArguments("Test.~Test()"));
        }
    }
}
