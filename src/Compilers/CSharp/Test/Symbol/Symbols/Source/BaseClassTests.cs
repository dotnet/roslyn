// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Retargeting = Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class BaseClassTests : CSharpTestBase
    {
        [Fact]
        public void CyclicBases1()
        {
            var text =
@"
class X : Y {}
class Y : X {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            var y = global.GetTypeMembers("Y", 0).Single();
            Assert.NotEqual(y, x.BaseType);
            Assert.NotEqual(x, y.BaseType);
            Assert.Equal(SymbolKind.ErrorType, x.BaseType.Kind);
            Assert.Equal(SymbolKind.ErrorType, y.BaseType.Kind);
            Assert.Equal("Y", x.BaseType.Name);
            Assert.Equal("X", y.BaseType.Name);
        }

        [Fact]
        public void CyclicBases2()
        {
            var text =
@"
class X : Y.n {}
class Y : X.n {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            var y = global.GetTypeMembers("Y", 0).Single();
            Assert.NotEqual(y, x.BaseType);
            Assert.NotEqual(x, y.BaseType);
            Assert.Equal(SymbolKind.ErrorType, x.BaseType.Kind);
            Assert.Equal(SymbolKind.ErrorType, y.BaseType.Kind);
            Assert.Equal("n", x.BaseType.Name);
            Assert.Equal("n", y.BaseType.Name);
        }

        [Fact]
        public void CyclicBases3()
        {
            var C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1;
            var C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2;

            var text =
@"
class C4 : C1 {}
";

            var comp = CreateCompilationWithMscorlib(text, new[] { C1, C2 });
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("C4", 0).Single();

            var x_base_base = x.BaseType.BaseType as ErrorTypeSymbol;
            var er = x_base_base.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'C2' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }

        [WorkItem(538506, "DevDiv")]
        [Fact]
        public void CyclicBasesRegress4140()
        {
            var text =
@"
class A<T>
{
    class B : A<E> { }
    class E : B.E { }
}

";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 1).Single();
            var b = a.GetTypeMembers("B", 0).Single();
            var e = a.GetTypeMembers("E", 0).Single();
            Assert.NotEqual(e, e.BaseType);

            var x_base = e.BaseType as ErrorTypeSymbol;
            var er = x_base.ErrorInfo;

            Assert.Equal("error CS0146: Circular base class dependency involving 'A<A<T>.E>.E' and 'A<T>.E'",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }

        [WorkItem(538526, "DevDiv")]
        [Fact]
        public void CyclicBasesRegress4166()
        {
            var text =
@"
class A<T> {
    public class C : B.D { }
}

class B {
    public class D : A<int>.C { }
}

";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 1).Single();
            var b = global.GetTypeMembers("B", 0).Single();
            var d = b.GetTypeMembers("D", 0).Single();
            Assert.NotEqual(d, d.BaseType);

            var x_base = d.BaseType as ErrorTypeSymbol;
            var er = x_base.ErrorInfo;

            Assert.Equal("error CS0146: Circular base class dependency involving 'A<int>.C' and 'B.D'",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }

        [WorkItem(4169, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CyclicBasesRegress4169()
        {
            var text =
@"
class A : object, A.IC
{
    protected interface IC { }
}

";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var ic = a.GetTypeMembers("IC", 0).Single();
            Assert.Equal(a.Interfaces[0], ic);

            var diagnostics = comp.GetDeclarationDiagnostics();
            Assert.Equal(0, diagnostics.Count());
        }

        [WorkItem(527551, "DevDiv")]
        [Fact]
        public void CyclicBasesRegress4168()
        {
            var text =
@"
class A : object, A.B.B.IC
{
    public class B : A {
        public interface IC { }
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var b = a.GetTypeMembers("B", 0).Single();
            var ic = b.GetTypeMembers("IC", 0).Single();
            Assert.NotEqual(b, b.BaseType);
            Assert.NotEqual(a, b.BaseType);
            Assert.Equal(SymbolKind.ErrorType, a.Interfaces[0].Kind);
            Assert.NotEqual(ic, a.Interfaces[0]);

            var diagnostics = comp.GetDeclarationDiagnostics();
            Assert.Equal(2, diagnostics.Count());
        }

        [Fact]
        public void CyclicBases4()
        {
            var text =
@"
class A<T> : B<A<T>> { }
class B<T> : A<B<T>> {
    A<T> F() { return null; }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.GetDeclarationDiagnostics().Verify(
    // (2,7): error CS0146: Circular base class dependency involving 'B<A<T>>' and 'A<T>'
    // class A<T> : B<A<T>> { }
    Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("B<A<T>>", "A<T>"),
    // (3,7): error CS0146: Circular base class dependency involving 'A<B<T>>' and 'B<T>'
    // class B<T> : A<B<T>> {
    Diagnostic(ErrorCode.ERR_CircularBase, "B").WithArguments("A<B<T>>", "B<T>")
                );
        }

        [Fact]
        public void CyclicBases5()
        {
            // bases are cyclic, but you can still find members when binding bases
            var text =
@"
class A : B {
  public class X { }
}

class B : A {
  public class Y { }
}

class Z : A.Y { }
class W : B.X { }

";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var z = global.GetTypeMembers("Z", 0).Single();
            var w = global.GetTypeMembers("W", 0).Single();
            var zBase = z.BaseType;
            Assert.Equal("Y", zBase.Name);
            var wBase = w.BaseType;
            Assert.Equal("X", wBase.Name);
        }

        [Fact]
        public void CyclicBases6()
        {
            // bases are cyclic, but you can still search for members w/o infinite looping in binder
            var text =
@"
class A : B {
  public class X {}
}

class B : C {
  public class Y {}
}

class C : A {
  public class Z {}
}

";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();

            //var aBase = a.BaseType;
            //Assert.True(aBase.IsErrorType());
            //Assert.Equal("B", aBase.Name);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var classA = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var someMemberInA = classA.Members[0];
            int positionInA = someMemberInA.SpanStart;

            var members = model.LookupSymbols(positionInA, a, "Z");
            Assert.Equal(1, members.Length);
            Assert.False(((TypeSymbol)members[0]).IsErrorType());
            Assert.Equal("C.Z", members[0].ToTestDisplayString());

            var members2 = model.LookupSymbols(positionInA, a, "Q");
            Assert.Equal(0, members2.Length);
        }

        [Fact]
        public void CyclicBases7()
        {
            // bases are cyclic, but you can still search for members w/o infinite looping in binder
            var text =
@"
class A : B<A.Y> {
  public class X {}
}

class B<T> : A {
  public class Y {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();

            //var aBase = a.BaseType;
            //Assert.True(aBase.IsErrorType());
            //Assert.Equal("B", aBase.Name);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var classA = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var someMemberInA = classA.Members[0];
            int positionInA = someMemberInA.SpanStart;

            var members = model.LookupSymbols(positionInA, a, "Q");
            Assert.Equal(0, members.Length);
        }

        [Fact]
        public void CyclicBases8()
        {
            var text = @"
public class A
{
    protected class B
    {
        protected class C
        {
            public class X { }
        }
    }
}
internal class F : A
{
    private class D : B
    {
        public class E : C.X { }
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (16,22): error CS0060: Inconsistent accessibility: base class 'A.B.C.X' is less accessible than class 'F.D.E'
                //         public class E : C.X { }
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "E").WithArguments("F.D.E", "A.B.C.X")
                );
        }

        [Fact, WorkItem(7878, "https://github.com/dotnet/roslyn/issues/7878")]
        public void BadVisibilityPartial()
        {
            var text = @"
internal class NV
{
}

public partial class C1
{
}

partial class C1 : NV
{
}

public partial class C1
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (10,15): error CS0060: Inconsistent accessibility: base class 'NV' is less accessible than class 'C1'
                // partial class C1 : NV
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C1").WithArguments("C1", "NV").WithLocation(10, 15));
        }

        [Fact, WorkItem(7878, "https://github.com/dotnet/roslyn/issues/7878")]
        public void StaticBasePartial()
        {
            var text = @"
static class NV
{
}

public partial class C1
{
}

partial class C1 : NV
{
}

public partial class C1
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (10,15): error CS0709: 'C1': cannot derive from static class 'NV'
                // partial class C1 : NV
                Diagnostic(ErrorCode.ERR_StaticBaseClass, "C1").WithArguments("NV", "C1").WithLocation(10, 15),
                // (10,15): error CS0060: Inconsistent accessibility: base class 'NV' is less accessible than class 'C1'
                // partial class C1 : NV
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C1").WithArguments("C1", "NV").WithLocation(10, 15));
        }


        [Fact, WorkItem(7878, "https://github.com/dotnet/roslyn/issues/7878")]
        public void BadVisInterfacePartial()
        {
            var text = @"
interface IFoo
{
    void Moo();
}

interface IBaz
{
    void Noo();
}

interface IBam
{
    void Zoo();
}

public partial interface IBar
{
}

partial interface IBar : IFoo, IBam
{
}

partial interface IBar : IBaz, IBaz
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (25,32): error CS0528: 'IBaz' is already listed in interface list
                // partial interface IBar : IBaz, IBaz
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "IBaz").WithArguments("IBaz").WithLocation(25, 32),
                // (21,19): error CS0061: Inconsistent accessibility: base interface 'IFoo' is less accessible than interface 'IBar'
                // partial interface IBar : IFoo, IBam
                Diagnostic(ErrorCode.ERR_BadVisBaseInterface, "IBar").WithArguments("IBar", "IFoo").WithLocation(21, 19),
                // (21,19): error CS0061: Inconsistent accessibility: base interface 'IBam' is less accessible than interface 'IBar'
                // partial interface IBar : IFoo, IBam
                Diagnostic(ErrorCode.ERR_BadVisBaseInterface, "IBar").WithArguments("IBar", "IBam").WithLocation(21, 19),
                // (25,19): error CS0061: Inconsistent accessibility: base interface 'IBaz' is less accessible than interface 'IBar'
                // partial interface IBar : IBaz, IBaz
                Diagnostic(ErrorCode.ERR_BadVisBaseInterface, "IBar").WithArguments("IBar", "IBaz").WithLocation(25, 19));
        }

        [Fact]
        public void EricLiCase1()
        {
            // should not be cyclic
            var text =
@"
interface I<T> {}
class A {
    public class B {}
}
class C : A, I<C.B> {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMembers("C", 0).Single();
            var cBase = c.BaseType;
            Assert.False(cBase.IsErrorType());
            Assert.Equal("A", cBase.Name);
            Assert.True(c.Interfaces.Single().TypeArguments.Single().IsErrorType()); //can't see base of C while evaluating C.B
        }

        [Fact]
        public void EricLiCase2()
        {
            // should not be cyclic
            var text =
@"
interface I<T> {}
class E : I<E> {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var e = global.GetTypeMembers("E", 0).Single();
            Assert.Equal(1, e.Interfaces.Length);
            Assert.Equal("I<E>", e.Interfaces[0].ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase3()
        {
            // should not be cyclic
            var text =
@"
interface I<T> {}
class G : I<G.H> {
    public class H {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var g = global.GetTypeMembers("G", 0).Single();
            Assert.Equal(1, g.Interfaces.Length);
            Assert.Equal("I<G.H>", g.Interfaces[0].ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase4()
        {
            // should not be cyclic
            var text =
@"
interface I<T> {}
class J : I<J.K.L> {
    public class K {
        public class L {}
    }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var j = global.GetTypeMembers("J", 0).Single();
            Assert.Equal(1, j.Interfaces.Length);
            Assert.Equal("I<J.K.L>", j.Interfaces[0].ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase5()
        {
            // should be cyclic
            var text =
@"class M : M {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var m = global.GetTypeMembers("M", 0).Single();
            Assert.True(m.BaseType.IsErrorType());
        }

        [Fact]
        public void EricLiCase6()
        {
            // should not be cyclic
            var text =
@"
class N<T> {}
class O : N<O> {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var o = global.GetTypeMembers("O", 0).Single();
            Assert.False(o.BaseType.IsErrorType());
            Assert.Equal("N<O>", o.BaseType.ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase7()
        {
            // should not be cyclic
            var text =
@"
class N<T> {}
class P : N<P.Q> {
    public class Q {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var p = global.GetTypeMembers("P", 0).Single();
            Assert.False(p.BaseType.IsErrorType());
            Assert.Equal("N<P.Q>", p.BaseType.ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase8()
        {
            // should not be cyclic
            var text =
@"
class N<T> {}
class R : N<R.S.T>{
    public class S {
        public class T {}
    }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var r = global.GetTypeMembers("R", 0).Single();
            var rBase = r.BaseType;
            Assert.False(rBase.IsErrorType());
            Assert.Equal("N<R.S.T>", rBase.ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase9()
        {
            // should not be cyclic, legal to implement an inner interface
            var text =
@"
class U : U.I
{
   public interface I {};
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var u = global.GetTypeMembers("U", 0).Single();
            var ifaces = u.Interfaces;
            Assert.Equal(1, ifaces.Length);
            Assert.False(ifaces[0].IsErrorType());
            Assert.Equal("U.I", ifaces[0].ToTestDisplayString());
        }


        [Fact]
        public void EricLiCase10()
        {
            // should not be cyclic, legal to implement an inner interface
            var text =
@"
interface IX : C.IY {}
class C : IX {
    public interface IY {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMembers("C", 0).Single();
            var ifaces = c.Interfaces;
            Assert.Equal(1, ifaces.Length);
            Assert.False(ifaces[0].IsErrorType());
            Assert.Equal("IX", ifaces[0].ToTestDisplayString());
            var ix = ifaces[0];
            var ixFaces = ix.Interfaces;
            Assert.Equal(1, ixFaces.Length);
            Assert.False(ixFaces[0].IsErrorType());
            Assert.Equal("C.IY", ixFaces[0].ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase11()
        {
            // should not be cyclic, legal to implement an inner interface
            var text =
@"
class X : Y.I {}
class Y : X {
    public interface I {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("X", 0).Single();
            var ifaces = x.Interfaces;
            Assert.Equal(1, ifaces.Length);
            Assert.False(ifaces[0].IsErrorType());
            Assert.Equal("Y.I", ifaces[0].ToTestDisplayString());
        }

        [Fact]
        public void EricLiCase12()
        {
            // G should not be in scope
            var text =
@"
class B : G { 
   public class G {} 
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var b = global.GetTypeMembers("B", 0).Single();
            Assert.True(b.BaseType.IsErrorType());
        }

        [Fact]
        public void EricLiCase14()
        {
            // this should be cyclic
            var text =
@"
   class B {}
   class D {}
   class Z<T> : E<B> {}
   class E<U> : Z<D> {}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var z = global.GetTypeMembers("Z", 1).Single();
            Assert.True(z.BaseType.IsErrorType());
        }

        [Fact]
        public void VladResCase01()
        {
            var text = @"
class A : A { } 
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (2,7): error CS0146: Circular base class dependency involving 'A' and 'A'
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("A", "A"));
        }

        [Fact]
        public void VladResCase02()
        {
            var text = @"
class A : B { }
class B : A { } 
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (2,7): error CS0146: Circular base class dependency involving 'B' and 'A'
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("B", "A"),
                // (3,7): error CS0146: Circular base class dependency involving 'A' and 'B'
                Diagnostic(ErrorCode.ERR_CircularBase, "B").WithArguments("A", "B"));
        }

        [Fact]
        public void VladResCase03()
        {
            var text = @"
class A : A.B
{
    public class B { }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (2,7): error CS0146: Circular base class dependency involving 'A.B' and 'A'
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("A.B", "A"));
        }

        [Fact]
        public void VladResCase04()
        {
            var text = @"
class A : A.I
{
    public interface I { }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase05()
        {
            var text = @"
class A : A.I
{
    private interface I { }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase06()
        {
            var text = @"
class A : A.B.I
{
    private class B : A
    {
        public interface I { }
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase07()
        {
            var text = @"
class A : A.B.B.I
{
    private class B : A
    {
        public interface I { }
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("A", "A.B"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "B").WithArguments("B", "A.B"));
        }

        [Fact]
        public void VladResCase08()
        {
            var text = @"
class A : C<A.B>
{
    public class B
    {
    }
}

class C<T> { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase09()
        {
            var text = @"
class A : C<A.B.D>
{
    public class B
    {
        public class D { }
    }
}

class C<T> { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase10()
        {
            var text = @"
class A : C<A.B.B>
{
    public class B : A { }
}

class C<T> { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("A", "A.B"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "B").WithArguments("B", "A.B"));
        }

        [Fact]
        public void VladResCase11()
        {
            var text = @"
class A : C<E>
{
    public class B
    {
        public class D { }
    }
}
class C<T> { }
class E : A.B.D { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase12()
        {
            var text = @"
class A : C<E.F>
{
    public class B
    {
        public class D
        {
            public class F { }
        }
    }
}
class C<T> { }
class E : A.B.D { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void VladResCase13()
        {
            var text = @"
class A<T>
{
    public class B { }
}

class C : A<D.B> { }

class D : C { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,15): error CS0426: The type name 'B' does not exist in the type 'D'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "B").WithArguments("B", "D"));
        }

        [Fact]
        public void VladResCase14()
        {
            var text = @"
class A<T>
{
    public class B { }
}

class C : A<C>, I<C.B> { }

interface I<T> { }
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,21): error CS0146: Circular base class dependency involving 'C' and 'C'
                Diagnostic(ErrorCode.ERR_CircularBase, "B").WithArguments("C", "C"));
        }

        [Fact]
        public void VladResCase15()
        {
            var text = @"
class X
{
    public interface Z { }
}

class A
{
    public class X
    {
        public class V { }
    }
}

class B : A, B.Y.Z
{
    public class Y : X { }
    public class C : B.Y.V { }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CircularBase, "X").WithArguments("B", "B.Y"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Z").WithArguments("Z", "B.Y"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "V").WithArguments("V", "B.Y"));
        }

        [Fact]
        public void VladResCase16()
        {
            var text = @"
class X
{
    public interface Z { }
}

class A<T>
{
    public class X
    {
        public class V { }
    }
}

class B : A<B.Y.Z>
{
    public class Y : X { }
    public class C : B.Y.V { }
}

";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (15,17): error CS0146: Circular base class dependency involving 'B.Y' and 'B'
                Diagnostic(ErrorCode.ERR_CircularBase, "X").WithArguments("B", "B.Y"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Z").WithArguments("Z", "B.Y"),
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "V").WithArguments("V", "B.Y"));
        }

        [Fact]
        public void CyclicInterfaces3()
        {
            var C1 = TestReferences.SymbolsTests.CyclicInheritance.Class1;
            var C2 = TestReferences.SymbolsTests.CyclicInheritance.Class2;

            var text =
@"
interface I4 : I1 {}
";
            var comp = CreateCompilationWithMscorlib(text, new[] { C1, C2 });
            var global = comp.GlobalNamespace;
            var x = global.GetTypeMembers("I4", 0).Single();

            var x_base_base = x.Interfaces.First().Interfaces.First() as ErrorTypeSymbol;
            var er = x_base_base.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'I2' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }


        [Fact]
        public void CyclicRetargeted4()
        {
            var ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll;

            var text =
@"
public class ClassB : ClassA {}
";
            var comp = CreateCompilationWithMscorlib(text, new[] { ClassAv1 }, assemblyName: "ClassB");

            var global1 = comp.GlobalNamespace;
            var B1 = global1.GetTypeMembers("ClassB", 0).Single();
            var A1 = global1.GetTypeMembers("ClassA", 0).Single();

            var B_base = B1.BaseType;
            var A_base = A1.BaseType;
            Assert.True(B1.IsFromCompilation(comp));
            Assert.IsAssignableFrom<PENamedTypeSymbol>(B_base);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(A_base);

            var ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll;
            text =
@"
public class ClassC : ClassB {}
";

            var comp2 = CreateCompilationWithMscorlib(text, new MetadataReference[] { ClassAv2, new CSharpCompilationReference(comp) });

            var global = comp2.GlobalNamespace;
            var B2 = global.GetTypeMembers("ClassB", 0).Single();
            var C = global.GetTypeMembers("ClassC", 0).Single();

            Assert.IsType<Retargeting.RetargetingNamedTypeSymbol>(B2);
            Assert.Same(B1, ((Retargeting.RetargetingNamedTypeSymbol)B2).UnderlyingNamedType);
            Assert.Same(C.BaseType, B2);

            var errorBase = B2.BaseType as ErrorTypeSymbol;
            var er = errorBase.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassA' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var A2 = global.GetTypeMembers("ClassA", 0).Single();

            var errorBase1 = A2.BaseType as ErrorTypeSymbol;
            er = errorBase1.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassB' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }


        [Fact]
        public void CyclicRetargeted5()
        {
            var ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll;
            var ClassBv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassB.netmodule;

            var text = @"// hi";
            var comp = CreateCompilationWithMscorlib(text, new[]
                {
                    ClassAv1,
                    ClassBv1
                },
                assemblyName: "ClassB");

            var global1 = comp.GlobalNamespace;
            var B1 = global1.GetTypeMembers("ClassB", 0).Distinct().Single();
            var A1 = global1.GetTypeMembers("ClassA", 0).Single();

            var B_base = B1.BaseType;
            var A_base = A1.BaseType;
            Assert.IsAssignableFrom<PENamedTypeSymbol>(B1);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(B_base);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(A_base);

            var ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll;
            text =
@"
public class ClassC : ClassB {}
";

            var comp2 = CreateCompilationWithMscorlib(text, new MetadataReference[]
            {
                ClassAv2,
                new CSharpCompilationReference(comp)
            });

            var global = comp2.GlobalNamespace;
            var B2 = global.GetTypeMembers("ClassB", 0).Single();
            var C = global.GetTypeMembers("ClassC", 0).Single();

            Assert.IsAssignableFrom<PENamedTypeSymbol>(B2);
            Assert.NotEqual(B1, B2);
            Assert.Same(((PEModuleSymbol)B1.ContainingModule).Module, ((PEModuleSymbol)B2.ContainingModule).Module);
            Assert.Equal(((PENamedTypeSymbol)B1).Handle, ((PENamedTypeSymbol)B2).Handle);
            Assert.Same(C.BaseType, B2);

            var errorBase = B2.BaseType as ErrorTypeSymbol;
            var er = errorBase.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassA' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var A2 = global.GetTypeMembers("ClassA", 0).Single();

            var errorBase1 = A2.BaseType as ErrorTypeSymbol;
            er = errorBase1.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassB' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));
        }


        [Fact]
        public void CyclicRetargeted6()
        {
            var ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll;

            var text =
@"
public class ClassB : ClassA {}
";
            var comp = CreateCompilationWithMscorlib(text, new[] { ClassAv2 }, assemblyName: "ClassB");

            var global1 = comp.GlobalNamespace;
            var B1 = global1.GetTypeMembers("ClassB", 0).Single();
            var A1 = global1.GetTypeMembers("ClassA", 0).Single();

            var B_base = B1.BaseType;
            var A_base = A1.BaseType;

            Assert.True(B1.IsFromCompilation(comp));

            var errorBase = B_base as ErrorTypeSymbol;
            var er = errorBase.ErrorInfo;

            Assert.Equal("error CS0146: Circular base class dependency involving 'ClassA' and 'ClassB'",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var errorBase1 = A_base as ErrorTypeSymbol;
            er = errorBase1.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassB' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll;
            text =
@"
public class ClassC : ClassB {}
";

            var comp2 = CreateCompilationWithMscorlib(text, new MetadataReference[]
            {
                ClassAv1,
                new CSharpCompilationReference(comp),
            });

            var global = comp2.GlobalNamespace;
            var A2 = global.GetTypeMembers("ClassA", 0).Single();
            var B2 = global.GetTypeMembers("ClassB", 0).Single();
            var C = global.GetTypeMembers("ClassC", 0).Single();

            Assert.IsType<Retargeting.RetargetingNamedTypeSymbol>(B2);
            Assert.Same(B1, ((Retargeting.RetargetingNamedTypeSymbol)B2).UnderlyingNamedType);
            Assert.Same(C.BaseType, B2);
            Assert.Same(B2.BaseType, A2);
        }


        [Fact]
        public void CyclicRetargeted7()
        {
            var ClassAv2 = TestReferences.SymbolsTests.RetargetingCycle.V2.ClassA.dll;
            var ClassBv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassB.netmodule;

            var text = @"// hi";
            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[]
                {
                    ClassAv2,
                    ClassBv1,
                },
                assemblyName: "ClassB");

            var global1 = comp.GlobalNamespace;
            var B1 = global1.GetTypeMembers("ClassB", 0).Distinct().Single();
            var A1 = global1.GetTypeMembers("ClassA", 0).Single();

            var B_base = B1.BaseType;
            var A_base = A1.BaseType;
            Assert.IsAssignableFrom<PENamedTypeSymbol>(B1);

            var errorBase = B_base as ErrorTypeSymbol;
            var er = errorBase.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassA' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var errorBase1 = A_base as ErrorTypeSymbol;
            er = errorBase1.ErrorInfo;

            Assert.Equal("error CS0268: Imported type 'ClassB' is invalid. It contains a circular base class dependency.",
                er.ToString(EnsureEnglishUICulture.PreferredOrNull));

            var ClassAv1 = TestReferences.SymbolsTests.RetargetingCycle.V1.ClassA.dll;
            text =
@"
public class ClassC : ClassB {}
";

            var comp2 = CreateCompilationWithMscorlib(text, new MetadataReference[]
            {
                ClassAv1,
                new CSharpCompilationReference(comp)
            });

            var global = comp2.GlobalNamespace;
            var B2 = global.GetTypeMembers("ClassB", 0).Single();
            var C = global.GetTypeMembers("ClassC", 0).Single();

            Assert.IsAssignableFrom<PENamedTypeSymbol>(B2);
            Assert.NotEqual(B1, B2);
            Assert.Same(((PEModuleSymbol)B1.ContainingModule).Module, ((PEModuleSymbol)B2.ContainingModule).Module);
            Assert.Equal(((PENamedTypeSymbol)B1).Handle, ((PENamedTypeSymbol)B2).Handle);
            Assert.Same(C.BaseType, B2);

            var A2 = global.GetTypeMembers("ClassA", 0).Single();

            Assert.IsAssignableFrom<PENamedTypeSymbol>(A2.BaseType);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(B2.BaseType);
        }

        [Fact]
        public void NestedNames1()
        {
            var text =
@"
namespace N
{
    static class C
    {
        class A<T>
        {
            class B<U> : A<B<U>>.D { }
            private class D { }
        }
    }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var n = global.GetMembers("N").OfType<NamespaceSymbol>().Single();
            var c = n.GetTypeMembers("C", 0).Single();
            var a = c.GetTypeMembers("A", 1).Single();
            var b = a.GetTypeMembers("B", 1).Single();
            var d = a.GetTypeMembers("D", 0).Single();
            Assert.Equal(Accessibility.Private, d.DeclaredAccessibility);
            Assert.Equal(d.OriginalDefinition, b.BaseType.OriginalDefinition);
            Assert.NotEqual(d, b.BaseType);
        }

        [Fact]
        public void Using1()
        {
            var text =
@"
namespace N1 {
  class A {}
}
namespace N2 {
  using N1; // bring N1.A into scope
  class B : A {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var n2 = global.GetMembers("N2").Single() as NamespaceSymbol;
            var a = n1.GetTypeMembers("A", 0).Single();
            var b = n2.GetTypeMembers("B", 0).Single();
            Assert.Equal(a, b.BaseType);
        }

        [Fact]
        public void Using2()
        {
            var text =
@"
namespace N1 {
  class A<T> {}
}
namespace N2 {
  using X = N1.A<B>; // bring N1.A into scope
  class B : X {}
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var n2 = global.GetMembers("N2").Single() as NamespaceSymbol;
            var a = n1.GetTypeMembers("A", 1).Single();
            var b = n2.GetTypeMembers("B", 0).Single();
            var bt = b.BaseType;
            Assert.Equal(a, b.BaseType.OriginalDefinition);
            Assert.Equal(b, (b.BaseType as NamedTypeSymbol).TypeArguments[0]);
        }

        [Fact]
        public void Using3()
        {
            var text =
@"
using @global = N;
namespace N { class C {} }
class D : global::N.C {}";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var d = global.GetMembers("D").Single() as NamedTypeSymbol;
            Assert.NotEqual(SymbolKind.ErrorType, d.BaseType.Kind);
        }

        [Fact]
        public void Arrays1()
        {
            var text =
@"
class G<T> { }
class C : G<C[,][]>
{
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var g = global.GetTypeMembers("G", 1).Single();
            var c = global.GetTypeMembers("C", 0).Single();
            Assert.Equal(g, c.BaseType.OriginalDefinition);
            var garg = c.BaseType.TypeArguments[0];
            Assert.Equal(SymbolKind.ArrayType, garg.Kind);
            var carr1 = garg as ArrayTypeSymbol;
            var carr2 = carr1.ElementType as ArrayTypeSymbol;
            Assert.Equal(c, carr2.ElementType);
            Assert.Equal(2, carr1.Rank);
            Assert.Equal(1, carr2.Rank);
            Assert.True(carr2.IsSZArray);
        }

        [Fact]
        public void MultiSource()
        {
            var text1 =
@"
using N2;
namespace N1 {
  class A {}
}
partial class X {
  class B1 : B {}
}
partial class Broken {
  class A2 : A {} // error: A not found
}
";
            var text2 =
@"
using N1;
namespace N2 {
  class B {}
}
partial class X {
  class A1 : A {}
}
partial class Broken {
  class B2 : B {} // error: B not found
}
";
            var comp = CreateCompilation(new[] { text1, text2 });
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var n2 = global.GetMembers("N2").Single() as NamespaceSymbol;
            var a = n1.GetTypeMembers("A", 0).Single();
            var b = n2.GetTypeMembers("B", 0).Single();
            var x = global.GetTypeMembers("X", 0).Single();
            var a1 = x.GetTypeMembers("A1", 0).Single();
            Assert.Equal(a, a1.BaseType);
            var b1 = x.GetTypeMembers("B1", 0).Single();
            Assert.Equal(b, b1.BaseType);
            var broken = global.GetTypeMembers("Broken", 0).Single();
            var a2 = broken.GetTypeMembers("A2", 0).Single();
            Assert.NotEqual(a, a2.BaseType);
            Assert.Equal(SymbolKind.ErrorType, a2.BaseType.Kind);
            var b2 = broken.GetTypeMembers("B2", 0).Single();
            Assert.NotEqual(b, b2.BaseType);
            Assert.Equal(SymbolKind.ErrorType, b2.BaseType.Kind);
        }

        [Fact]
        public void CyclicUsing1()
        {
            var text =
@"
using M = B.X;
using N = A.Y;
public class A : M { }
public class B : N { }
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var b = global.GetTypeMembers("B", 0).Single();
            var abase = a.BaseType;
            Assert.Equal(SymbolKind.ErrorType, abase.Kind);
            var bbase = b.BaseType;
            Assert.Equal(SymbolKind.ErrorType, bbase.Kind);
        }

        [Fact]
        public void BaseError()
        {
            var text = "class C : Bar { }";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(537401, "DevDiv")]
        [Fact]
        public void NamespaceClassInterfaceEscapedIdentifier()
        {
            var text = @"
namespace @if
{
    public interface @break { }
    public class @int<@string> { }
    public class @float : @int<@break> : @if.@break { }
}";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            NamespaceSymbol nif = (NamespaceSymbol)comp.SourceModule.GlobalNamespace.GetMembers("if").Single();
            Assert.Equal("if", nif.Name);
            Assert.Equal("@if", nif.ToString());
            NamedTypeSymbol cfloat = (NamedTypeSymbol)nif.GetMembers("float").Single();
            Assert.Equal("float", cfloat.Name);
            Assert.Equal("@if.@float", cfloat.ToString());
            NamedTypeSymbol cint = cfloat.BaseType;
            Assert.Equal("int", cint.Name);
            Assert.Equal("@if.@int<@if.@break>", cint.ToString());
            NamedTypeSymbol ibreak = cfloat.Interfaces.Single();
            Assert.Equal("break", ibreak.Name);
            Assert.Equal("@if.@break", ibreak.ToString());
        }

        [WorkItem(539328, "DevDiv")]
        [WorkItem(539789, "DevDiv")]
        [Fact]
        public void AccessInBaseClauseCheckedWithRespectToContainer()
        {
            var text = @"
class X
{
    protected class A { }
}
 
class Y : X
{
    private class C : X.A { }
    private class B { }
}";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            var diags = comp.GetDeclarationDiagnostics();
            Assert.Empty(diags);
        }

        /// <summary>
        /// The base type of a nested type should not change depending on
        /// whether or not the base type of the containing type has been
        /// evaluated.
        /// </summary>
        [WorkItem(539744, "DevDiv")]
        [Fact]
        public void BaseTypeEvaluationOrder()
        {
            var text = @"
class A<T>
{
    public class X { }
}
class B : A<B.Y.Error>
{
    public class Y : X { }
}
";
            //B.BaseType, B.Y.BaseType
            {
                var comp = CreateCompilationWithMscorlib(text);

                var classB = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("B")[0];
                var classY = (NamedTypeSymbol)classB.GetMembers("Y")[0];

                var baseB = classB.BaseType;
                Assert.Equal("A<B.Y.Error>", baseB.ToTestDisplayString());
                Assert.False(baseB.IsErrorType());

                var baseY = classY.BaseType;
                Assert.Equal("X", baseY.ToTestDisplayString());
                Assert.True(baseY.IsErrorType());
            }

            //B.Y.BaseType, B.BaseType
            {
                var comp = CreateCompilationWithMscorlib(text);

                var classB = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("B")[0];
                var classY = (NamedTypeSymbol)classB.GetMembers("Y")[0];

                var baseY = classY.BaseType;
                Assert.Equal("X", baseY.ToTestDisplayString());
                Assert.True(baseY.IsErrorType());

                var baseB = classB.BaseType;
                Assert.Equal("A<B.Y.Error>", baseB.ToTestDisplayString());
                Assert.False(baseB.IsErrorType());
            }
        }

        [Fact]
        public void BaseInterfacesInMetadata()
        {
            var text = @"
interface I1 { }
interface I2 : I1 { }
class C : I2 { }
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var baseInterface = global.GetMember<NamedTypeSymbol>("I1");
            var derivedInterface = global.GetMember<NamedTypeSymbol>("I2");
            var @class = global.GetMember<NamedTypeSymbol>("C");

            var bothInterfaces = ImmutableArray.Create<NamedTypeSymbol>(baseInterface, derivedInterface);

            Assert.Equal(baseInterface, derivedInterface.AllInterfaces.Single());
            Assert.Equal(derivedInterface, @class.Interfaces.Single());
            Assert.True(@class.AllInterfaces.SetEquals(bothInterfaces, EqualityComparer<NamedTypeSymbol>.Default));

            var typeDef = (Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var context = new EmitContext(module, null, new DiagnosticBag());
            var cciInterfaces = typeDef.Interfaces(context).Cast<NamedTypeSymbol>().AsImmutable();
            Assert.True(cciInterfaces.SetEquals(bothInterfaces, EqualityComparer<NamedTypeSymbol>.Default));
            context.Diagnostics.Verify();
        }

        [Fact(), WorkItem(544454, "DevDiv")]
        public void InterfaceImplementedWithPrivateType()
        {
            var textA = @"
using System;
using System.Collections;
using System.Collections.Generic;

public class A: IEnumerable<A.MyPrivateType>
{
    private class MyPrivateType {}

    IEnumerator<MyPrivateType> IEnumerable<A.MyPrivateType>.GetEnumerator()
    { throw new NotImplementedException(); }

    IEnumerator IEnumerable.GetEnumerator()
    { throw new NotImplementedException(); }
}";

            var textB = @"
using System.Collections.Generic;

class Z
{
    public IEnumerable<object> foo(A a)
    { 
        return a;
    }
}";

            CSharpCompilation c1 = CreateCompilationWithMscorlib(textA);
            CSharpCompilation c2 = CreateCompilationWithMscorlib(textB, new[] { new CSharpCompilationReference(c1) });

            //Works this way, but doesn't when compilation is supplied as metadata
            Assert.Equal(0, c1.GetDiagnostics().Count());
            Assert.Equal(0, c2.GetDiagnostics().Count());

            var metadata1 = c1.EmitToArray(options: new EmitOptions(metadataOnly: true));
            c2 = CreateCompilationWithMscorlib(textB, new[] { MetadataReference.CreateFromImage(metadata1) });

            Assert.Equal(0, c2.GetDiagnostics().Count());
        }

        [WorkItem(545365, "DevDiv")]
        [Fact()]
        public void ProtectedInternalNestedBaseClass()
        {
            var source1 = @"
public class PublicClass
{
    protected internal class ProtectedInternalClass
    {
        public ProtectedInternalClass()
        {
        }
    }
}
";

            var source2 = @"
class C : PublicClass.ProtectedInternalClass
{
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, assemblyName: "One");
            compilation1.VerifyDiagnostics();

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { new CSharpCompilationReference(compilation1) }, assemblyName: "Two");
            compilation2.VerifyDiagnostics(
                // (2,23): error CS0122: 'PublicClass.ProtectedInternalClass' is inaccessible due to its protection level
                // class C : PublicClass.ProtectedInternalClass
                Diagnostic(ErrorCode.ERR_BadAccess, "ProtectedInternalClass").WithArguments("PublicClass.ProtectedInternalClass"));
        }

        [WorkItem(545365, "DevDiv")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ProtectedAndInternalNestedBaseClass()
        {
            // Note: the problem was with the "protected" check so we use InternalsVisibleTo to make
            // the "internal" check succeed.
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) } 

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string)
           = {string('Test')}
} 

.class public auto ansi beforefieldinit PublicClass
       extends [mscorlib]System.Object
{
  .class auto ansi nested famandassem beforefieldinit ProtectedAndInternalClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

  } // end of class ProtectedAndInternalClass

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

} // end of class PublicClass
";

            var csharp = @"
class C : PublicClass.ProtectedAndInternalClass
{
}
";
            CreateCompilationWithCustomILSource(csharp, il, appendDefaultHeader: false).VerifyDiagnostics(
                // (2,23): error CS0122: 'PublicClass.ProtectedAndInternalClass' is inaccessible due to its protection level
                // class C : PublicClass.ProtectedAndInternalClass
                Diagnostic(ErrorCode.ERR_BadAccess, "ProtectedAndInternalClass").WithArguments("PublicClass.ProtectedAndInternalClass"));
        }

        [WorkItem(530144, "DevDiv")]
        [Fact()]
        public void UnifyingBaseInterfaces01()
        {
            var il = @"
.assembly extern mscorlib
{
}
.assembly a
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module a.dll


.class interface public abstract auto ansi J`1<T>
{
}

.class interface public abstract auto ansi I`1<T>
       implements class J`1<int32>,
                  class J`1<!T>
{
}";

            var csharp =
@"public class C
{
    public static I<int> x;
    static void F(I<int> x)
    {
        I<int> t = C.x;
    }
}

public class D : I<int> {}
public interface I2 : I<int> {}";
            CreateCompilationWithCustomILSource(csharp, il, appendDefaultHeader: false).VerifyDiagnostics(
                // (10,14): error CS0648: 'I<int>' is a type not supported by the language
                // public class D : I<int> {}
                Diagnostic(ErrorCode.ERR_BogusType, "D").WithArguments("I<int>"),
                // (11,18): error CS0648: 'I<int>' is a type not supported by the language
                // public interface I2 : I<int> {}
                Diagnostic(ErrorCode.ERR_BogusType, "I2").WithArguments("I<int>"),
                // (4,26): error CS0648: 'I<int>' is a type not supported by the language
                //     static void F(I<int> x)
                Diagnostic(ErrorCode.ERR_BogusType, "x").WithArguments("I<int>"),
                // (3,19): error CS0648: 'I<int>' is a type not supported by the language
                //     public static I<int> x;
                Diagnostic(ErrorCode.ERR_BogusType, "I<int>").WithArguments("I<int>"),
                // (6,9): error CS0648: 'I<int>' is a type not supported by the language
                //         I<int> t = C.x;
                Diagnostic(ErrorCode.ERR_BogusType, "I<int>").WithArguments("I<int>")
            );
        }

        [WorkItem(530144, "DevDiv")]
        [Fact()]
        public void UnifyingBaseInterfaces02()
        {
            var il = @"
.assembly extern mscorlib
{
}
.assembly a
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module a.dll


.class interface public abstract auto ansi J`1<T>
{
}

.class interface public abstract auto ansi I`1<T>
       implements class J`1<object>,
                  class J`1<!T>
{
}";

            var csharp =
@"public class C
{
    public static I<dynamic> x;
    static void F(I<dynamic> x)
    {
        I<dynamic> t = C.x;
    }
}";
            CreateCompilationWithCustomILSource(csharp, il, appendDefaultHeader: false, references: new[] { SystemCoreRef }).VerifyDiagnostics(
                // (4,30): error CS0648: 'I<dynamic>' is a type not supported by the language
                //     static void F(I<dynamic> x)
                Diagnostic(ErrorCode.ERR_BogusType, "x").WithArguments("I<dynamic>"),
                // (3,19): error CS0648: 'I<dynamic>' is a type not supported by the language
                //     public static I<dynamic> x;
                Diagnostic(ErrorCode.ERR_BogusType, "I<dynamic>").WithArguments("I<dynamic>"),
                // (6,9): error CS0648: 'I<dynamic>' is a type not supported by the language
                //         I<dynamic> t = C.x;
                Diagnostic(ErrorCode.ERR_BogusType, "I<dynamic>").WithArguments("I<dynamic>")
            );
        }

        [WorkItem(545365, "DevDiv")]
        [Fact()]
        public void ProtectedNestedBaseClass()
        {
            var source1 = @"
public class PublicClass
{
    protected class ProtectedClass
    {
        public ProtectedClass()
        {
        }
    }
}
";

            var source2 = @"
class C : PublicClass.ProtectedClass
{
}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, assemblyName: "One");
            compilation1.VerifyDiagnostics();

            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { new CSharpCompilationReference(compilation1) }, assemblyName: "Two");
            compilation2.VerifyDiagnostics(
                // (2,23): error CS0122: 'PublicClass.ProtectedClass' is inaccessible due to its protection level
                // class C : PublicClass.ProtectedClass
                Diagnostic(ErrorCode.ERR_BadAccess, "ProtectedClass").WithArguments("PublicClass.ProtectedClass"));
        }

        [WorkItem(545589, "DevDiv")]
        [Fact]
        public void MissingTypeArgumentInBase()
        {
            var text =
@"interface I<out T> { }
 
class B : I<object>
{
    public static void Foo<T>(I<T> x)
    {
    }
 
    public static void Foo<T>() where T : I<>
    {
    }
 
    static void Main()
    {
        Foo(new B());
    }
}";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            comp.VerifyDiagnostics(
                // (9,43): error CS7003: Unexpected use of an unbound generic name
                //     public static void Foo<T>() where T : I<>
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "I<>")
                );
        }

        [WorkItem(792711, "DevDiv")]
        [Fact]
        public void Repro792711()
        {
            var source = @"
public class Base<T>
{
}

public class Derived<T> : Base<Derived<T>>
{
}
";

            var metadataRef = CreateCompilationWithMscorlib(source).EmitToImageReference(embedInteropTypes: true);

            var comp = CreateCompilationWithMscorlib("", new[] { metadataRef });
            var derived = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            Assert.Equal(TypeKind.Class, derived.TypeKind);
        }

        [WorkItem(872825, "DevDiv")]
        [Fact]
        public void InaccessibleStructInterface()
        {
            var source =
@"class C
{
    protected interface I
    {
    }
}
struct S : C.I
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (7,14): error CS0122: 'C.I' is inaccessible due to its protection level
                // struct S : C.I
                Diagnostic(ErrorCode.ERR_BadAccess, "I").WithArguments("C.I").WithLocation(7, 14));
        }

        [WorkItem(872948, "DevDiv")]
        [Fact]
        public void MissingNestedMemberInStructImplementsClause()
        {
            var source =
@"struct S : S.I
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            // Ideally report "CS0426: The type name 'I' does not exist in the type 'S'"
            // instead. Bug #896959.
            compilation.VerifyDiagnostics(
                // (1,14): error CS0146: Circular base class dependency involving 'S' and 'S'
                // struct S : S.I
                Diagnostic(ErrorCode.ERR_CircularBase, "I").WithArguments("S", "S").WithLocation(1, 14));
        }

        [WorkItem(896959, "DevDiv")]
        [Fact(Skip = "896959")]
        public void MissingNestedMemberInClassImplementsClause()
        {
            var source =
@"class C : C.I
{
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (1,13): error CS0426: The type name 'I' does not exist in the type 'C'
                // class C : C.I
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "I").WithArguments("I", "C").WithLocation(1, 13));
        }

        [Fact, WorkItem(1085632, "DevDiv")]
        public void BaseLookupRecursionWithStaticImport01()
        {
            const string source =
@"using A<int>.B;
using D;

class A<T> : C
{
    public static class B { }
}

class D
{
    public class C { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                    // (4,14): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
                    // class A<T> : C
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(4, 14),
                    // (1,7): error CS0138: A 'using namespace' directive can only be applied to namespaces; 'A<int>.B' is a type not a namespace. Consider a 'using static' directive instead
                    // using A<int>.B;
                    Diagnostic(ErrorCode.ERR_BadUsingNamespace, "A<int>.B").WithArguments("A<int>.B").WithLocation(1, 7),
                    // (2,7): error CS0138: A 'using namespace' directive can only be applied to namespaces; 'D' is a type not a namespace. Consider a 'using static' directive instead
                    // using D;
                    Diagnostic(ErrorCode.ERR_BadUsingNamespace, "D").WithArguments("D").WithLocation(2, 7),
                    // (2,1): hidden CS8019: Unnecessary using directive.
                    // using D;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using D;").WithLocation(2, 1),
                    // (1,1): hidden CS8019: Unnecessary using directive.
                    // using A<int>.B;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A<int>.B;").WithLocation(1, 1)
                );
        }

        [Fact, WorkItem(1085632, "DevDiv")]
        public void BaseLookupRecursionWithStaticImport02()
        {
            const string source =
@"using static A<int>.B;
using static D;

class A<T> : C
{
    public static class B { }
}

class D
{
    public class C { }
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                    // (1,1): hidden CS8019: Unnecessary using directive.
                    // using static A<int>.B;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A<int>.B;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void BindBases()
        {
            // Ensure good semantic model data even in error scenarios
            var text =
@"
class B {
  public B(long x) {}
}

class D : B {
  extern D(int x) : base(y) {}
  static int y;
}";
            var comp = CreateCompilationWithMscorlib45(text);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var baseY = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "y").OfType<ExpressionSyntax>().First();
            var typeInfo = model.GetTypeInfo(baseY);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int64, typeInfo.ConvertedType.SpecialType);
        }

        [Fact, WorkItem(5697, "https://github.com/dotnet/roslyn/issues/5697")]
        public void InheritThroughStaticImportOfGenericTypeWithConstraint_01()
        {
            var text =
@"
using static CrashTest.Crash<CrashTest.Class2>; 

namespace CrashTest 
{ 
    class Class2 : AbstractClass 
    { 
    } 

    public static class Crash<T> 
        where T: Crash<T>.AbstractClass 
    { 
        public abstract class AbstractClass 
        { 
            public int Id { get; set; } 
        } 
    } 
}";
            var comp = CreateCompilationWithMscorlib(text);
            CompileAndVerify(comp);
        }

        [Fact, WorkItem(5697, "https://github.com/dotnet/roslyn/issues/5697")]
        public void InheritThroughStaticImportOfGenericTypeWithConstraint_02()
        {
            var text =
@"
using static CrashTest.Crash<object>; 

namespace CrashTest 
{ 
    class Class2 : AbstractClass 
    { 
    } 

    public static class Crash<T> 
        where T: Crash<T>.AbstractClass 
    { 
        public abstract class AbstractClass 
        { 
            public int Id { get; set; } 
        } 
    } 

    class Class3
    {
        AbstractClass Test()
        {
            return null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
    // (6,11): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'Crash<T>'. There is no implicit reference conversion from 'object' to 'CrashTest.Crash<object>.AbstractClass'.
    //     class Class2 : AbstractClass 
    Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Class2").WithArguments("CrashTest.Crash<T>", "CrashTest.Crash<object>.AbstractClass", "T", "object").WithLocation(6, 11),
    // (21,23): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'Crash<T>'. There is no implicit reference conversion from 'object' to 'CrashTest.Crash<object>.AbstractClass'.
    //         AbstractClass Test()
    Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Test").WithArguments("CrashTest.Crash<T>", "CrashTest.Crash<object>.AbstractClass", "T", "object").WithLocation(21, 23)
                );
        }

        [Fact, WorkItem(5697, "https://github.com/dotnet/roslyn/issues/5697")]
        public void InheritThroughStaticImportOfGenericTypeWithConstraint_03()
        {
            var text =
@"
using static CrashTest.Crash<CrashTest.Class2>; 

namespace CrashTest 
{ 
    [System.Obsolete]
    class Class2 : AbstractClass 
    { 
    } 

    [System.Obsolete]
    public static class Crash<T> 
        where T: Crash<T>.AbstractClass 
    { 
        public abstract class AbstractClass 
        { 
            public int Id { get; set; } 
        } 
    } 
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
    // (2,30): warning CS0612: 'Class2' is obsolete
    // using static CrashTest.Crash<CrashTest.Class2>; 
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "CrashTest.Class2").WithArguments("CrashTest.Class2").WithLocation(2, 30)
                );
        }

        [Fact, WorkItem(5697, "https://github.com/dotnet/roslyn/issues/5697")]
        public void InheritThroughStaticImportOfGenericTypeWithConstraint_04()
        {
            var text =
@"
using static CrashTest.Crash<CrashTest.Class2>; 

namespace CrashTest 
{ 
    class Class2 : AbstractClass 
    { 
    } 

    public static class Crash<T> 
        where T: Crash<T>.AbstractClass 
    { 
        [System.Obsolete]
        public abstract class AbstractClass 
        { 
            public int Id { get; set; } 
        } 
    } 
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
    // (11,18): warning CS0612: 'Crash<T>.AbstractClass' is obsolete
    //         where T: Crash<T>.AbstractClass 
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Crash<T>.AbstractClass").WithArguments("CrashTest.Crash<T>.AbstractClass").WithLocation(11, 18),
    // (6,20): warning CS0612: 'Crash<Class2>.AbstractClass' is obsolete
    //     class Class2 : AbstractClass 
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "AbstractClass").WithArguments("CrashTest.Crash<CrashTest.Class2>.AbstractClass").WithLocation(6, 20)
                );
        }

        [Fact, WorkItem(5697, "https://github.com/dotnet/roslyn/issues/5697")]
        public void InheritThroughStaticImportOfGenericTypeWithConstraint_05()
        {
            var text =
@"
using CrashTest.Crash<CrashTest.Class2>; 

namespace CrashTest 
{ 
    class Class2 : AbstractClass 
    { 
    } 

    public static class Crash<T> 
        where T: Crash<T>.AbstractClass 
    { 
        public abstract class AbstractClass 
        { 
            public int Id { get; set; } 
        } 
    } 
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
    // (2,7): error CS0138: A 'using namespace' directive can only be applied to namespaces; 'Crash<Class2>' is a type not a namespace. Consider a 'using static' directive instead
    // using CrashTest.Crash<CrashTest.Class2>; 
    Diagnostic(ErrorCode.ERR_BadUsingNamespace, "CrashTest.Crash<CrashTest.Class2>").WithArguments("CrashTest.Crash<CrashTest.Class2>").WithLocation(2, 7),
    // (6,20): error CS0246: The type or namespace name 'AbstractClass' could not be found (are you missing a using directive or an assembly reference?)
    //     class Class2 : AbstractClass 
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "AbstractClass").WithArguments("AbstractClass").WithLocation(6, 20),
    // (2,1): hidden CS8019: Unnecessary using directive.
    // using CrashTest.Crash<CrashTest.Class2>; 
    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using CrashTest.Crash<CrashTest.Class2>;").WithLocation(2, 1)
                );
        }
    }
}
