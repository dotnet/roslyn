// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DelegateTests : CSharpTestBase
    {
        [Fact]
        public void MissingTypes()
        {
            var text =
@"
delegate void A();
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,15): error CS0518: Predefined type 'System.MulticastDelegate' is not defined or imported
                // delegate void A();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.MulticastDelegate"),
                // (2,10): error CS0518: Predefined type 'System.Void' is not defined or imported
                // delegate void A();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void"),
                // (2,1): error CS0518: Predefined type 'System.Void' is not defined or imported
                // delegate void A();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "delegate void A();").WithArguments("System.Void"),
                // (2,1): error CS0518: Predefined type 'System.Object' is not defined or imported
                // delegate void A();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "delegate void A();").WithArguments("System.Object"),
                // (2,1): error CS0518: Predefined type 'System.IntPtr' is not defined or imported
                // delegate void A();
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "delegate void A();").WithArguments("System.IntPtr")
                );
        }

        [WorkItem(530363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530363")]
        [Fact]
        public void MissingAsyncTypes()
        {
            var source = "delegate void A();";
            var comp = CreateCompilation(
                trees: new[] { Parse(source) },
                references: new[] { MinCorlibRef });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Simple1()
        {
            var text =
@"
class A {
    delegate void D();
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var d = a.GetMembers("D")[0] as NamedTypeSymbol;
            var tmp = d.GetMembers();
            Assert.Equal(d.Locations[0], d.DelegateInvokeMethod.Locations[0], EqualityComparer<Location>.Default);
            Assert.Equal(d.Locations[0], d.InstanceConstructors[0].Locations[0], EqualityComparer<Location>.Default);
        }

        [Fact]
        public void Duplicate()
        {
            var text =
@"
delegate void D(int x);
delegate void D(float y);
";
            var comp = CreateCompilationWithMscorlib(text);
            var diags = comp.GetDeclarationDiagnostics();
            Assert.Equal(1, diags.Count());

            var global = comp.GlobalNamespace;
            var d = global.GetTypeMembers("D", 0);
            Assert.Equal(2, d.Length);
        }

        [Fact]
        public void MetadataDelegateField()
        {
            var text =
@"
class A {
    public System.Func<int> Field;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var field = a.GetMembers("Field")[0] as FieldSymbol;
            var fieldType = field.Type.TypeSymbol as NamedTypeSymbol;
            Assert.Equal(TypeKind.Delegate, fieldType.TypeKind);
            var invoke = fieldType.DelegateInvokeMethod;
            Assert.Equal(MethodKind.DelegateInvoke, invoke.MethodKind);
            var ctor = fieldType.InstanceConstructors[0];
            Assert.Equal(2, ctor.Parameters.Length);
            Assert.Equal(comp.GetSpecialType(SpecialType.System_Object), ctor.Parameters[0].Type.TypeSymbol);
            Assert.Equal(comp.GetSpecialType(SpecialType.System_IntPtr), ctor.Parameters[1].Type.TypeSymbol);
        }

        [WorkItem(537188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537188")]
        [Fact]
        public void SimpleDelegate()
        {
            var text =
@"delegate void MyDel(int n);";

            var comp = CreateCompilationWithMscorlib(text);
            var v = comp.GlobalNamespace.GetTypeMembers("MyDel", 0).Single();
            Assert.NotEqual(null, v);
            Assert.Equal(SymbolKind.NamedType, v.Kind);
            Assert.Equal(TypeKind.Delegate, v.TypeKind);
            Assert.True(v.IsReferenceType);
            Assert.False(v.IsValueType);
            Assert.True(v.IsSealed);
            Assert.False(v.IsAbstract);
            Assert.Equal(0, v.Arity); // number of type parameters
            Assert.Equal(1, v.DelegateInvokeMethod.Parameters.Length);
            Assert.Equal(Accessibility.Internal, v.DeclaredAccessibility);
            Assert.Equal("System.MulticastDelegate", v.BaseType.ToTestDisplayString());
        }

        [WorkItem(537188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537188")]
        [WorkItem(538707, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538707")]
        [Fact]
        public void BeginInvokeEndInvoke()
        {
            var text = @"
delegate int MyDel(int x, ref int y, out int z);
namespace System
{
  interface IAsyncResult {}
}
";

            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var myDel = global.GetTypeMembers("MyDel", 0).Single() as NamedTypeSymbol;

            var invoke = myDel.DelegateInvokeMethod;

            var beginInvoke = myDel.GetMembers("BeginInvoke").Single() as MethodSymbol;
            Assert.Equal(invoke.Parameters.Length + 2, beginInvoke.Parameters.Length);
            Assert.Equal(TypeKind.Interface, beginInvoke.ReturnType.TypeKind);
            Assert.Equal("System.IAsyncResult", beginInvoke.ReturnType.ToTestDisplayString());
            for (int i = 0; i < invoke.Parameters.Length; i++)
            {
                Assert.Equal(invoke.Parameters[i].Type, beginInvoke.Parameters[i].Type);
                Assert.Equal(invoke.Parameters[i].RefKind, beginInvoke.Parameters[i].RefKind);
            }
            var lastParameterType = beginInvoke.Parameters[invoke.Parameters.Length].Type;
            Assert.Equal("System.AsyncCallback", lastParameterType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_AsyncCallback, lastParameterType.SpecialType);
            Assert.Equal("System.Object", beginInvoke.Parameters[invoke.Parameters.Length + 1].Type.ToTestDisplayString());

            var endInvoke = myDel.GetMembers("EndInvoke").Single() as MethodSymbol;
            Assert.Equal(invoke.ReturnType, endInvoke.ReturnType);
            int k = 0;
            for (int i = 0; i < invoke.Parameters.Length; i++)
            {
                if (invoke.Parameters[i].RefKind != RefKind.None)
                {
                    Assert.Equal(invoke.Parameters[i].Type, endInvoke.Parameters[k].Type);
                    Assert.Equal(invoke.Parameters[i].RefKind, endInvoke.Parameters[k++].RefKind);
                }
            }
            lastParameterType = endInvoke.Parameters[k++].Type;
            Assert.Equal("System.IAsyncResult", lastParameterType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_IAsyncResult, lastParameterType.SpecialType);
            Assert.Equal(k, endInvoke.Parameters.Length);
        }

        [WorkItem(537188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537188")]
        [Fact]
        public void GenericDelegate()
        {
            var text =
@"namespace NS
{
    internal delegate void D<Q>(Q q);
}";

            var comp = CreateCompilationWithMscorlib(text);
            var namespaceNS = comp.GlobalNamespace.GetMembers("NS").First() as NamespaceOrTypeSymbol;
            Assert.Equal(1, namespaceNS.GetTypeMembers().Length);

            var d = namespaceNS.GetTypeMembers("D").First();
            Assert.Equal(namespaceNS, d.ContainingSymbol);
            Assert.Equal(SymbolKind.NamedType, d.Kind);
            Assert.Equal(TypeKind.Delegate, d.TypeKind);
            Assert.Equal(Accessibility.Internal, d.DeclaredAccessibility);
            Assert.Equal(1, d.TypeParameters.Length);
            Assert.Equal("Q", d.TypeParameters[0].Name);
            var q = d.TypeParameters[0];
            Assert.Equal(q.ContainingSymbol, d);
            Assert.Equal(d.DelegateInvokeMethod.Parameters[0].Type.TypeSymbol, q);

            // same as type parameter
            Assert.Equal(1, d.TypeArguments.Length);
        }

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void DelegateEscapedIdentifier()
        {
            var text = @"
delegate void @out();
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            NamedTypeSymbol dout = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("out").Single();
            Assert.Equal("out", dout.Name);
            Assert.Equal("@out", dout.ToString());
        }

        [Fact]
        public void DelegatesEverywhere()
        {
            var text = @"
using System;
delegate int Intf(int x);
class C
{
    Intf I;
    Intf Method(Intf f1)
    {
        Intf i = f1;
        i = f1;
        I = i;
        i = I;
        Delegate d1 = f1;
        MulticastDelegate m1 = f1;
        object o1 = f1;
        f1 = i;
        return f1;
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void DelegateCreation()
        {
            var text = @"
namespace CSSample
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        delegate void D1();
        delegate void D2();

        delegate int D3(int x);

        static D1 d1;
        static D2 d2;
        static D3 d3;

        internal virtual void V() { }
        void M() { }
        static void S() { }

        static int M2(int x) { return x; }

        static void F(Program p)
        {
            // Good cases
            d2 = new D2(d1);
            d1 = new D1(d2);
            d1 = new D1(d2.Invoke);
            d1 = new D1(d1);
            d1 = new D1(p.V);
            d1 = new D1(p.M);
            d1 = new D1(S);
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (17,19): warning CS0169: The field 'CSSample.Program.d3' is never used
                //         static D3 d3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "d3").WithArguments("CSSample.Program.d3"));
        }

        [WorkItem(538722, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538722")]
        [Fact]
        public void MulticastIsNotDelegate()
        {
            var text = @"
using System;
class Program
{
  static void Main()
  {
    MulticastDelegate d = Main;
  }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,27): error CS0428: Cannot convert method group 'Main' to non-delegate type 'System.MulticastDelegate'. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "System.MulticastDelegate"));
        }

        [WorkItem(538706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538706")]
        [Fact]
        public void DelegateMethodParameterNames()
        {
            var text = @"
delegate int D(int x, ref int y, out int z);
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            NamedTypeSymbol d = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("D").Single();

            MethodSymbol invoke = d.DelegateInvokeMethod;
            ImmutableArray<ParameterSymbol> invokeParameters = invoke.Parameters;
            Assert.Equal(3, invokeParameters.Length);
            Assert.Equal("x", invokeParameters[0].Name);
            Assert.Equal("y", invokeParameters[1].Name);
            Assert.Equal("z", invokeParameters[2].Name);

            MethodSymbol beginInvoke = (MethodSymbol)d.GetMembers("BeginInvoke").Single();
            ImmutableArray<ParameterSymbol> beginInvokeParameters = beginInvoke.Parameters;
            Assert.Equal(5, beginInvokeParameters.Length);
            Assert.Equal("x", beginInvokeParameters[0].Name);
            Assert.Equal("y", beginInvokeParameters[1].Name);
            Assert.Equal("z", beginInvokeParameters[2].Name);
            Assert.Equal("callback", beginInvokeParameters[3].Name);
            Assert.Equal("object", beginInvokeParameters[4].Name);

            MethodSymbol endInvoke = (MethodSymbol)d.GetMembers("EndInvoke").Single();
            ImmutableArray<ParameterSymbol> endInvokeParameters = endInvoke.Parameters;
            Assert.Equal(3, endInvokeParameters.Length);
            Assert.Equal("y", endInvokeParameters[0].Name);
            Assert.Equal("z", endInvokeParameters[1].Name);
            Assert.Equal("result", endInvokeParameters[2].Name);
        }

        [WorkItem(541179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541179")]
        [Fact]
        public void DelegateWithTypeParameterNamedInvoke()
        {
            var text = @"
delegate void F<Invoke>(Invoke i);
 
class Foo 
{ 
  void M(int i)
  {
    F<int> x = M;
  }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [WorkItem(612002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612002")]
        [Fact]
        public void DelegateWithOutParameterNamedResult()
        {
            var text = @"
delegate void D(out int result);
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            NamedTypeSymbol d = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("D").Single();

            MethodSymbol invoke = d.DelegateInvokeMethod;
            ImmutableArray<ParameterSymbol> invokeParameters = invoke.Parameters;
            Assert.Equal(1, invokeParameters.Length);
            Assert.Equal("result", invokeParameters[0].Name);

            MethodSymbol beginInvoke = (MethodSymbol)d.GetMembers("BeginInvoke").Single();
            ImmutableArray<ParameterSymbol> beginInvokeParameters = beginInvoke.Parameters;
            Assert.Equal(3, beginInvokeParameters.Length);
            Assert.Equal("result", beginInvokeParameters[0].Name);
            Assert.Equal("callback", beginInvokeParameters[1].Name);
            Assert.Equal("object", beginInvokeParameters[2].Name);

            MethodSymbol endInvoke = (MethodSymbol)d.GetMembers("EndInvoke").Single();
            ImmutableArray<ParameterSymbol> endInvokeParameters = endInvoke.Parameters;
            Assert.Equal(2, endInvokeParameters.Length);
            Assert.Equal("result", endInvokeParameters[0].Name);
            Assert.Equal("__result", endInvokeParameters[1].Name);
        }

        [WorkItem(612002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612002")]
        [Fact]
        public void DelegateWithOutParameterNamedResult2()
        {
            var text = @"
delegate void D(out int @__result);
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            NamedTypeSymbol d = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("D").Single();

            MethodSymbol invoke = d.DelegateInvokeMethod;
            ImmutableArray<ParameterSymbol> invokeParameters = invoke.Parameters;
            Assert.Equal(1, invokeParameters.Length);
            Assert.Equal("__result", invokeParameters[0].Name);

            MethodSymbol beginInvoke = (MethodSymbol)d.GetMembers("BeginInvoke").Single();
            ImmutableArray<ParameterSymbol> beginInvokeParameters = beginInvoke.Parameters;
            Assert.Equal(3, beginInvokeParameters.Length);
            Assert.Equal("__result", beginInvokeParameters[0].Name);
            Assert.Equal("callback", beginInvokeParameters[1].Name);
            Assert.Equal("object", beginInvokeParameters[2].Name);

            MethodSymbol endInvoke = (MethodSymbol)d.GetMembers("EndInvoke").Single();
            ImmutableArray<ParameterSymbol> endInvokeParameters = endInvoke.Parameters;
            Assert.Equal(2, endInvokeParameters.Length);
            Assert.Equal("__result", endInvokeParameters[0].Name);
            Assert.Equal("result", endInvokeParameters[1].Name);
        }

        [WorkItem(612002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612002")]
        [Fact]
        public void DelegateWithOutParameterNamedResult3()
        {
            var text = @"
delegate void D(out int result, out int @__result);
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            NamedTypeSymbol d = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("D").Single();

            MethodSymbol invoke = d.DelegateInvokeMethod;
            ImmutableArray<ParameterSymbol> invokeParameters = invoke.Parameters;
            Assert.Equal(2, invokeParameters.Length);
            Assert.Equal("result", invokeParameters[0].Name);
            Assert.Equal("__result", invokeParameters[1].Name);

            MethodSymbol beginInvoke = (MethodSymbol)d.GetMembers("BeginInvoke").Single();
            ImmutableArray<ParameterSymbol> beginInvokeParameters = beginInvoke.Parameters;
            Assert.Equal(4, beginInvokeParameters.Length);
            Assert.Equal("result", invokeParameters[0].Name);
            Assert.Equal("__result", invokeParameters[1].Name);
            Assert.Equal("callback", beginInvokeParameters[2].Name);
            Assert.Equal("object", beginInvokeParameters[3].Name);

            MethodSymbol endInvoke = (MethodSymbol)d.GetMembers("EndInvoke").Single();
            ImmutableArray<ParameterSymbol> endInvokeParameters = endInvoke.Parameters;
            Assert.Equal(3, endInvokeParameters.Length);
            Assert.Equal("result", endInvokeParameters[0].Name);
            Assert.Equal("__result", endInvokeParameters[1].Name);
            Assert.Equal("____result", endInvokeParameters[2].Name);
        }

        [WorkItem(612002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612002")]
        [Fact]
        public void DelegateWithParametersNamedCallbackAndObject()
        {
            var text = @"
delegate void D(int callback, int @object);
";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            NamedTypeSymbol d = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("D").Single();

            MethodSymbol invoke = d.DelegateInvokeMethod;
            ImmutableArray<ParameterSymbol> invokeParameters = invoke.Parameters;
            Assert.Equal(2, invokeParameters.Length);
            Assert.Equal("callback", invokeParameters[0].Name);
            Assert.Equal("object", invokeParameters[1].Name);

            MethodSymbol beginInvoke = (MethodSymbol)d.GetMembers("BeginInvoke").Single();
            ImmutableArray<ParameterSymbol> beginInvokeParameters = beginInvoke.Parameters;
            Assert.Equal(4, beginInvokeParameters.Length);
            Assert.Equal("callback", beginInvokeParameters[0].Name);
            Assert.Equal("object", beginInvokeParameters[1].Name);
            Assert.Equal("__callback", beginInvokeParameters[2].Name);
            Assert.Equal("__object", beginInvokeParameters[3].Name);

            MethodSymbol endInvoke = (MethodSymbol)d.GetMembers("EndInvoke").Single();
            ImmutableArray<ParameterSymbol> endInvokeParameters = endInvoke.Parameters;
            Assert.Equal(1, endInvokeParameters.Length);
            Assert.Equal("result", endInvokeParameters[0].Name);
        }

        [Fact]
        public void DelegateConversion()
        {
            var text = @"
public class A { }
public class B { }
public class C<T> { }

public class DelegateTest
{
    public static void FT<T>(T t) { }
    public static void FTT<T>(T t1, T t2) { }
    public static void FTi<T>(T t, int x) { }
    public static void FST<S, T>(S s, T t) { }
    public static void FCT<T>(C<T> c) { }

    public static void PT<T>(params T[] args) { }
    public static void PTT<T>(T t, params T[] arr) { }
    public static void PST<S, T>(S s, params T[] arr) { }

    public delegate void Da(A x);
    public delegate void Daa(A x, A y);
    public delegate void Dab(A x, B y);
    public delegate void Dai(A x, int y);

    public delegate void Dpa(A[] x);
    public delegate void Dapa(A x, A[] y);
    public delegate void Dapb(A x, B[] y);
    public delegate void Dpapa(A[] x, A[] y);

    public delegate void Dca(C<A> x);

    public delegate void D1<T>(T t);

    public static void Run()
    {
        Da da;
        Daa daa;
        Dai dai;
        Dab dab;
        Dpa dpa;
        Dapa dapa;
        Dapb dapb;
        Dpapa dpapa;
        Dca dca;

        da = new Da(FTT);
        da = new Da(FCT);
        da = new Da(PT);

        daa = new Daa(FT);
        daa = new Daa(FTi);
        daa = new Daa(PTT);
        daa = new Daa(PST);

        dai = new Dai(FT);
        dai = new Dai(FTT);
        dai = new Dai(PTT);
        dai = new Dai(PST);

        dab = new Dab(FT);
        dab = new Dab(FTT);
        dab = new Dab(FTi);
        dab = new Dab(PTT);
        dab = new Dab(PST);

        dpa = new Dpa(FTT);
        dpa = new Dpa(FCT);

        dapa = new Dapa(FT);
        dapa = new Dapa(FTT);

        dapb = new Dapb(FT);
        dapb = new Dapb(FTT);
        dapb = new Dapb(PTT);

        dpapa = new Dpapa(FT);
        dpapa = new Dpapa(PTT);

        dca = new Dca(FTT);
        dca = new Dca(PT);

        RunG(null);
    }

    public static void RunG<T>(T t) { }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // These match Dev10.

                // (44,14): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Da'
                //         da = new Da(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Da(FTT)").WithArguments("FTT", "DelegateTest.Da"),
                // (48,15): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Daa'
                //         daa = new Daa(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Daa(FT)").WithArguments("FT", "DelegateTest.Daa"),
                // (49,15): error CS0123: No overload for 'FTi' matches delegate 'DelegateTest.Daa'
                //         daa = new Daa(FTi);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Daa(FTi)").WithArguments("FTi", "DelegateTest.Daa"),
                // (50,15): error CS0123: No overload for 'PTT' matches delegate 'DelegateTest.Daa'
                //         daa = new Daa(PTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Daa(PTT)").WithArguments("PTT", "DelegateTest.Daa"),
                // (53,15): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Dai'
                //         dai = new Dai(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dai(FT)").WithArguments("FT", "DelegateTest.Dai"),
                // (55,15): error CS0123: No overload for 'PTT' matches delegate 'DelegateTest.Dai'
                //         dai = new Dai(PTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dai(PTT)").WithArguments("PTT", "DelegateTest.Dai"),
                // (58,15): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Dab'
                //         dab = new Dab(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dab(FT)").WithArguments("FT", "DelegateTest.Dab"),
                // (60,15): error CS0123: No overload for 'FTi' matches delegate 'DelegateTest.Dab'
                //         dab = new Dab(FTi);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dab(FTi)").WithArguments("FTi", "DelegateTest.Dab"),
                // (61,15): error CS0123: No overload for 'PTT' matches delegate 'DelegateTest.Dab'
                //         dab = new Dab(PTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dab(PTT)").WithArguments("PTT", "DelegateTest.Dab"),
                // (64,15): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dpa'
                //         dpa = new Dpa(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dpa(FTT)").WithArguments("FTT", "DelegateTest.Dpa"),
                // (67,16): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Dapa'
                //         dapa = new Dapa(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dapa(FT)").WithArguments("FT", "DelegateTest.Dapa"),
                // (70,16): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Dapb'
                //         dapb = new Dapb(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dapb(FT)").WithArguments("FT", "DelegateTest.Dapb"),
                // (74,17): error CS0123: No overload for 'FT' matches delegate 'DelegateTest.Dpapa'
                //         dpapa = new Dpapa(FT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dpapa(FT)").WithArguments("FT", "DelegateTest.Dpapa"),
                // (77,15): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dca'
                //         dca = new Dca(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dca(FTT)").WithArguments("FTT", "DelegateTest.Dca"),
                // (80,9): error CS0411: The type arguments for method 'DelegateTest.RunG<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         RunG(null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "RunG").WithArguments("DelegateTest.RunG<T>(T)"),

                // Dev10 reports CS0411 (ERR_CantInferMethTypeArgs) for these.

                // (45,14): error CS0123: No overload for 'FCT' matches delegate 'DelegateTest.Da'
                //         da = new Da(FCT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Da(FCT)").WithArguments("FCT", "DelegateTest.Da"),
                // (46,14): error CS0123: No overload for 'PT' matches delegate 'DelegateTest.Da'
                //         da = new Da(PT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Da(PT)").WithArguments("PT", "DelegateTest.Da"),
                // (51,15): error CS0123: No overload for 'PST' matches delegate 'DelegateTest.Daa'
                //         daa = new Daa(PST);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Daa(PST)").WithArguments("PST", "DelegateTest.Daa"),
                // (54,15): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dai'
                //         dai = new Dai(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dai(FTT)").WithArguments("FTT", "DelegateTest.Dai"),
                // (56,15): error CS0123: No overload for 'PST' matches delegate 'DelegateTest.Dai'
                //         dai = new Dai(PST);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dai(PST)").WithArguments("PST", "DelegateTest.Dai"),
                // (59,15): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dab'
                //         dab = new Dab(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dab(FTT)").WithArguments("FTT", "DelegateTest.Dab"),
                // (62,15): error CS0123: No overload for 'PST' matches delegate 'DelegateTest.Dab'
                //         dab = new Dab(PST);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dab(PST)").WithArguments("PST", "DelegateTest.Dab"),
                // (65,15): error CS0123: No overload for 'FCT' matches delegate 'DelegateTest.Dpa'
                //         dpa = new Dpa(FCT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dpa(FCT)").WithArguments("FCT", "DelegateTest.Dpa"),
                // (68,16): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dapa'
                //         dapa = new Dapa(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dapa(FTT)").WithArguments("FTT", "DelegateTest.Dapa"),
                // (71,16): error CS0123: No overload for 'FTT' matches delegate 'DelegateTest.Dapb'
                //         dapb = new Dapb(FTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dapb(FTT)").WithArguments("FTT", "DelegateTest.Dapb"),
                // (72,16): error CS0123: No overload for 'PTT' matches delegate 'DelegateTest.Dapb'
                //         dapb = new Dapb(PTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dapb(PTT)").WithArguments("PTT", "DelegateTest.Dapb"),
                // (75,17): error CS0123: No overload for 'PTT' matches delegate 'DelegateTest.Dpapa'
                //         dpapa = new Dpapa(PTT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dpapa(PTT)").WithArguments("PTT", "DelegateTest.Dpapa"),
                // (78,15): error CS0123: No overload for 'PT' matches delegate 'DelegateTest.Dca'
                //         dca = new Dca(PT);
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Dca(PT)").WithArguments("PT", "DelegateTest.Dca"));
        }

        [Fact]
        public void CastFromMulticastDelegate()
        {
            var source =
@"using System;

delegate void D();
class Program
{
    public static void Main(string[] args)
    {
        MulticastDelegate d = null;
        D d2 = (D)d;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(634014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634014")]
        [Fact]
        public void DelegateTest634014()
        {
            var source =
@"delegate void D(ref int x);
class C
{
    D d = async delegate { };
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,11): error CS1988: Async methods cannot have ref or out parameters
                //     D d = async delegate { };
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "async delegate { }"),
                // (4,11): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     D d = async delegate { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "async delegate { }")
                );
        }
    }
}
