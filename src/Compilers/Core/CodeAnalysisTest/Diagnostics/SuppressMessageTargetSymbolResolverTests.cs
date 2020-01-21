// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class SuppressMessageTargetSymbolResolverTests
    {
        [Fact]
        public void TestResolveGlobalNamespace1()
        {
            VerifyNamespaceResolution("namespace $$N {}", LanguageNames.CSharp, false, "N");
        }

        [Fact]
        public void TestResolveGlobalNamespace2()
        {
            VerifyNamespaceResolution(@"
Namespace $$N 
End Namespace",
                LanguageNames.VisualBasic, false, "N");
        }

        [Fact]
        public void TestResolveNestedNamespace1()
        {
            VerifyNamespaceResolution(@"
namespace A
{
    namespace B.$$C
    {
    }
}
",
                LanguageNames.CSharp, false, "A.B.C");
        }

        [Fact]
        public void TestResolveNestedNamespace2()
        {
            VerifyNamespaceResolution(@"
Namespace A
    Namespace B.$$C
    End Namespace
End Namespace
",
                LanguageNames.VisualBasic, false, "A.B.C");
        }

        [Fact]
        public void TestResolveNamespaceWithSameNameAsGenericInterface1()
        {
            VerifyNamespaceResolution(@"
namespace $$IGoo
{
}
interface IGoo<T>
{
}
",
                LanguageNames.CSharp, false, "IGoo");
        }

        [Fact]
        public void TestResolveNamespaceWithSameNameAsGenericInterface2()
        {
            VerifyNamespaceResolution(@"
Namespace $$IGoo
End Namespace
Interface IGoo(Of T)
End Interface
",
                LanguageNames.VisualBasic, false, "IGoo");
        }

        [Fact]
        public void TestDontPartiallyResolveNamespace1()
        {
            VerifyNoNamespaceResolution(@"
namespace A
{
    namespace B
    {
    }
}
",
                LanguageNames.CSharp, false, "A+B", "A#B");
        }

        [Fact]
        public void TestDontPartiallyResolveNamespace2()
        {
            VerifyNoNamespaceResolution(@"
Namespace A
    Namespace B
    End Namespace
End Namespace
",
                LanguageNames.VisualBasic, false, "A+B", "A#B");
        }

        [Fact]
        public void TestResolveGlobalType1()
        {
            VerifyTypeResolution("class $$C {}", LanguageNames.CSharp, false, "C");
        }

        [Fact]
        public void TestResolveGlobalType2()
        {
            VerifyTypeResolution(@"
Class $$C
End Class",
                LanguageNames.VisualBasic, false, "C");
        }

        [Fact]
        public void TestResolveModule()
        {
            VerifyTypeResolution(@"
Module $$C
End Module",
                LanguageNames.VisualBasic, false, "C");
        }

        [Fact]
        public void TestResolveTypeInModule()
        {
            VerifyTypeResolution(@"
Module M
    Class $$C
    End Class
End Module",
                LanguageNames.VisualBasic, false, "M+C");
        }

        [Fact]
        public void TestDontPartiallyResolveTypeInModule()
        {
            VerifyNoTypeResolution(@"
Module M
    Class C
    End Class
End Module",
                LanguageNames.VisualBasic, false, "M.C");
        }

        [Fact]
        public void TestNestedTypesInModule()
        {
            VerifyTypeResolution(@"
Module M
    Class C
        Structure $$D
        End Structure
    End Class
End Module",
                LanguageNames.VisualBasic, false, "M+C+D");
        }

        [Fact]
        public void TestResolveTypeInNamespace1()
        {
            VerifyTypeResolution(@"
namespace N1.N2
{
    class $$C
    {
    }
}
",
                LanguageNames.CSharp, false, "N1.N2.C");
        }

        [Fact]
        public void TestResolveTypeInNamespace2()
        {
            VerifyTypeResolution(@"
Namespace N1.N2
    Class $$C
    End Class
End Namespace
",
                LanguageNames.VisualBasic, false, "N1.N2.C");
        }

        [Fact]
        public void TestResolveTypeNestedInGlobalType1()
        {
            VerifyTypeResolution(@"
class C
{
    interface $$D
    {
    }
}
",
                LanguageNames.CSharp, false, "C+D");
        }

        [Fact]
        public void TestResolveTypeNestedInGlobalType2()
        {
            VerifyTypeResolution(@"
Class C
     Public Delegate Sub $$D()
End Class
",
                LanguageNames.VisualBasic, false, "C+D");
        }

        [Fact]
        public void TestResolveNestedType1()
        {
            VerifyTypeResolution(@"
namespace N
{
    class C
    {
        class D
        {
            class $$E
            { }
        }
    }
}
",
                LanguageNames.CSharp, false, "N.C+D+E");
        }

        [Fact]
        public void TestResolveNestedType2()
        {
            VerifyTypeResolution(@"
Namespace N
    Class C
        Class D
            Class $$E
            End Class
        End Class
    End Class
End Namespace
",
                LanguageNames.VisualBasic, false, "N.C+D+E");
        }

        [Fact]
        public void TestResolveGenericType1()
        {
            VerifyTypeResolution(@"
class D<T>
{
}
class $$D<T1, T2, T3>
{
}
",
                LanguageNames.CSharp, false, "D`3");
        }

        [Fact]
        public void TestResolveGenericType2()
        {
            VerifyTypeResolution(@"
Class D(Of T)
End Class
Class $$D(Of T1, T2, T3)
End Class
",
                LanguageNames.VisualBasic, false, "D`3");
        }

        [Fact]
        public void TestDontResolveGenericType1()
        {
            VerifyNoTypeResolution(@"
class D<T1, T2, T3>
{
}
",
                LanguageNames.CSharp, false, "D");
        }

        [Fact]
        public void TestDontResolveGenericType2()
        {
            VerifyNoTypeResolution(@"
Class D(Of T1, T2, T3)
End Class
",
                LanguageNames.VisualBasic, false, "D");
        }

        [Fact]
        public void TestResolveRootNamespace()
        {
            VerifyTypeResolution(@"
Module M
    Class $$C
    End Class
End Module
",
                LanguageNames.VisualBasic, true, "RootNamespace.M+C");
        }

        [Fact]
        public void TestDontPartiallyResolveType1()
        {
            VerifyNoTypeResolution(@"
class A
{
    class B
    {
    }
}
",
                LanguageNames.CSharp, false, "A.B");
        }

        [Fact]
        public void TestDontPartiallyResolveType2()
        {
            VerifyNoTypeResolution(@"
Class A
    Class B
    End Class
End Class
",
                LanguageNames.VisualBasic, false, "A.B");
        }

        [Fact]
        public void TestResolveField1()
        {
            VerifyMemberResolution(@"
class C
{
    string $$s;
}
",
                LanguageNames.CSharp, false,
                "C.#s",
                "C.s");
        }

        [Fact]
        public void TestResolveField2()
        {
            VerifyMemberResolution(@"
Class C
    Dim $$s As String
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#s",
                "C.s");
        }

        [Fact]
        public void TestResolveProperty1()
        {
            VerifyMemberResolution(@"
class C
{
    public string $$StringProperty { get; set; }
}
",
                LanguageNames.CSharp, false,
                "C.#StringProperty",
                "C.StringProperty");
        }

        [Fact]
        public void TestResolveProperty2()
        {
            VerifyMemberResolution(@"
Class C
    Public Property $$StringProperty 
        Get 
        End Get
        Set
        End Set
    End Property
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#StringProperty",
                "C.StringProperty");
        }

        [Fact]
        public void TestResolveEvent1()
        {
            VerifyMemberResolution(@"
class C
{
    public event System.EventHandler<int> $$E;
}
",
                LanguageNames.CSharp, false,
                "e:C.#E",
                "C.E");
        }

        [Fact]
        public void TestResolveEvent2()
        {
            VerifyMemberResolution(@"
Class C
    Public Event $$E As System.EventHandler(Of Integer)
End Class
",
                LanguageNames.VisualBasic, false,
                "e:C.#E",
                "C.E");
        }

        [Fact]
        public void TestDontResolveNonEvent1()
        {
            VerifyNoMemberResolution(@"
public class C
{
    public int E;
}
",
               LanguageNames.CSharp, false, "e:C.E");
        }

        [Fact]
        public void TestDontResolveNonEvent2()
        {
            VerifyNoMemberResolution(@"
Public Class C
    Public Dim E As Integer
End Class
",
               LanguageNames.VisualBasic, false, "e:C.E");
        }

        [Fact]
        public void TestResolvePropertySetMethod1()
        {
            VerifyMemberResolution(@"
class C
{
    public string StringProperty { get; $$set; }
}
",
                LanguageNames.CSharp, false,
                "C.#set_StringProperty(System.String)",
                "C.set_StringProperty(System.String):System.Void");
        }

        [Fact]
        public void TestResolvePropertySetMethod2()
        {
            VerifyMemberResolution(@"
Class C
    Public Property StringProperty As String
        Get
        End Get
        $$Set
        End Set
    End Property
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#set_StringProperty(System.String)",
                "C.set_StringProperty(System.String):System.Void");
        }

        [Fact]
        public void TestResolveEventAddMethod()
        {
            VerifyMemberResolution(@"
class C
{
    public delegate void Del(int x);
    public event Del E
    {
        $$add { }
        remove { }
    }
}
",
                LanguageNames.CSharp, false,
                "C.#add_E(C.Del)",
                "C.add_E(C.Del):System.Void");
        }

        [Fact]
        public void TestResolveEventRemoveMethod()
        {
            VerifyMemberResolution(@"
class C
{
    public delegate void Del(int x);
    public event Del E
    {
        add { }
        $$remove { }
    }
}
",
                LanguageNames.CSharp, false,
                "C.#remove_E(C.Del)",
                "C.remove_E(C.Del):System.Void");
        }

        [Fact]
        public void TestResolveVoidMethod1()
        {
            VerifyMemberResolution(@"
class C
{
    void Goo() {}
    void $$Goo(int x) {}
    void Goo(string x) {}
}
",
            LanguageNames.CSharp, false,
            "C.#Goo(System.Int32)",
            "C.Goo(System.Int32):System.Void");
        }

        [Fact]
        public void TestResolveVoidMethod2()
        {
            VerifyMemberResolution(@"
Class C
    Sub Goo() 
    End Sub
    Sub $$Goo(ByVal x as Integer)
    End Sub
    Sub Goo(ByVal x as String)
    End Sub
End Class
",
            LanguageNames.VisualBasic, false,
            "C.#Goo(System.Int32)",
            "C.Goo(System.Int32):System.Void");
        }

        [Fact]
        public void TestResolveMethod1()
        {
            VerifyMemberResolution(@"
class C
{
    void Goo() {}
    string Goo(int x) {}
    string $$Goo(string x) {}
}
",
            LanguageNames.CSharp, false,
            "C.#Goo(System.String)",
            "C.Goo(System.String):System.String");
        }

        [Fact]
        public void TestResolveMethod2()
        {
            VerifyMemberResolution(@"
Class C
    Sub Goo() 
    End Sub
    Function Goo(ByVal x As Integer) As String
    End Function
    Function $$Goo(ByVal x As String) As String 
    End Function
End Class
",
            LanguageNames.VisualBasic, false,
            "C.#Goo(System.String)",
            "C.Goo(System.String):System.String");
        }

        [Fact]
        public void TestResolveOverloadedGenericMethod1()
        {
            VerifyMemberResolution(@"
class C
{
    int Goo<T>(T x) {}
    int $$Goo<T>(T x, params T[] y) {}
}
",
                LanguageNames.CSharp, false,
                "C.#Goo`1(!!0,!!0[])",
                "C.Goo(T,T[]):System.Int32");

            VerifyMemberResolution(@"
class C
{
    int [|Goo|]<T>(T x) {}
    int [|Goo|]<T>(T x, T y) {}
}
",
                LanguageNames.CSharp, false, "C.Goo():System.Int32");
        }

        [Fact]
        public void TestResolveOverloadedGenericMethod2()
        {
            VerifyMemberResolution(@"
Class C
    Function Goo(Of T)(ByVal x As T) As Integer
    End Function
    Function $$Goo(Of T)(ByVal x As T, ByVal y as T()) As Integer
    End Function
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#Goo`1(!!0,!!0[])",
                "C.Goo(T,T[]):System.Int32");

            VerifyMemberResolution(@"
Class C
    Function [|Goo|](Of T)(ByVal x As T) As Integer
    End Function
    Function [|Goo|](Of T)(ByVal x As T, ByVal y As T) As Integer
    End Function
End Class
",
                LanguageNames.VisualBasic, false, "C.Goo():System.Int32");
        }

        [Fact]
        public void TestResolveMethodOverloadedOnArity1()
        {
            VerifyMemberResolution(@"
interface I
{
    void M<T>();
    void $$M<T1, T2>();
}
",
                LanguageNames.CSharp, false, "I.#M`2()");

            VerifyMemberResolution(@"
interface I
{
    void [|M|]<T>();
    void [|M|]<T1, T2>();
}
",
                LanguageNames.CSharp, false, "I.M():System.Void");
        }

        [Fact]
        public void TestResolveMethodOverloadedOnArity2()
        {
            VerifyMemberResolution(@"
Interface I
    Sub M(Of T)
    Sub $$M(Of T1, T2)
End Interface
",
                LanguageNames.VisualBasic, false, "I.#M`2()");

            VerifyMemberResolution(@"
Interface I
    Sub [|M|](Of T)
    Sub [|M|](Of T1, T2)
End Interface
",
                LanguageNames.VisualBasic, false, "I.M():System.Void");
        }

        [Fact]
        public void TestResolveConstructor1()
        {
            VerifyMemberResolution(@"
class C
{
    $$C() {}
}
",
                LanguageNames.CSharp, false,
                "C.#.ctor()",
                "C..ctor()");
        }

        [Fact]
        public void TestResolveConstructor2()
        {
            VerifyMemberResolution(@"
Class C
    Sub $$New()
    End Sub
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#.ctor()",
                "C..ctor()");
        }

        [Fact]
        public void TestResolveStaticConstructor1()
        {
            VerifyMemberResolution(@"
class C
{
    static $$C() {}
}
",
                LanguageNames.CSharp, false,
                "C.#.cctor()",
                "C..cctor()");
        }

        [Fact]
        public void TestResolveStaticConstructor2()
        {
            VerifyMemberResolution(@"
Class C
    Shared Sub $$New()
    End Sub
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#.cctor()",
                "C..cctor()");
        }

        [Fact]
        public void TestResolveSimpleOperator1()
        {
            VerifyMemberResolution(@"
class C
{
    public static bool operator $$==(C a, C b)
    {
        return true;
    }
}
",
                LanguageNames.CSharp, false,
                "C.#op_Equality(C,C)",
                "C.op_Equality(C,C):System.Boolean");
        }

        [Fact]
        public void TestResolveSimpleOperator2()
        {
            VerifyMemberResolution(@"
Class C
    Public Shared Operator $$+(a as C, b as C) As Boolean
        return true
    End Operator
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#op_Addition(C,C)",
                "C.op_Addition(C,C):System.Boolean");
        }

        [Fact]
        public void TestResolveIndexer1()
        {
            VerifyMemberResolution(@"
class C
{
    public C $$this[int i, string j]
    {
        get { return this; }
    }

    public C this[string i]
    {
        get { return this; }
    }
}
",
                LanguageNames.CSharp, false,
                "C.#Item[System.Int32,System.String]",
                "C.Item[System.Int32,System.String]");
        }

        [Fact]
        public void TestResolveIndexer2()
        {
            VerifyMemberResolution(@"
Class C
	Public Default ReadOnly Property $$Item(i As Integer, j As String) As C
		Get
			Return Me
		End Get
	End Property

	Public Default ReadOnly Property Item(i As String) As C
		Get
			Return Me
		End Get
	End Property
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#Item[System.Int32,System.String]",
                "C.Item[System.Int32,System.String]");
        }

        [Fact]
        public void TestResolveIndexerAccessorMethod()
        {
            VerifyMemberResolution(@"
class C
{
    public C this[int i]
    {
        get { return this; }
    }

    public C this[string i]
    {
        $$get { return this; }
    }
}
",
                LanguageNames.CSharp, false,
                "C.#get_Item(System.String)",
                "C.get_Item(System.String):C");
        }

        [Fact]
        public void TestResolveExplicitOperator()
        {
            VerifyMemberResolution(@"
class C
{
    public static explicit operator $$bool(C c)
    {
        return C == null;
    }

    public static explicit operator string(C c)
    {
        return string.Empty;
    }
}
",
                LanguageNames.CSharp, false,
                "C.#op_Explicit(C):System.Boolean",
                "C.op_Explicit(C):System.Boolean");
        }

        [Fact]
        public void TestResolveMethodWithComplexParameterTypes1()
        {
            VerifyMemberResolution(@"
class C
{
    public unsafe static bool $$IsComplex<T0, T1>(int* a, ref int b, ref T0 c, T1[] d)
    {
        return true;
    }
}
",
                LanguageNames.CSharp, false,
                "C.#IsComplex`2(System.Int32*,System.Int32&,!!0&,!!1[])",
                "C.IsComplex(System.Int32*,System.Int32&,T0&,T1[]):System.Boolean");
        }

        [Fact]
        public void TestResolveMethodWithComplexParameterTypes2()
        {
            VerifyMemberResolution(@"
Class C
	Public Shared Function $$IsComplex(Of T0, T1)(a As Integer, ByRef b As Integer, ByRef c As T0, d As T1()) As Boolean
		Return True
	End Function
End Class",
                LanguageNames.VisualBasic, false,
                "C.#IsComplex`2(System.Int32,System.Int32&,!!0&,!!1[])",
                "C.IsComplex(System.Int32,System.Int32&,T0&,T1[]):System.Boolean");
        }

        [Fact]
        public void TestFinalize1()
        {
            VerifyMemberResolution(@"
class A
{
    ~$$A()
    {
    }
}
",
                LanguageNames.CSharp, false,
                "A.#Finalize()",
                "A.Finalize():System.Void");
        }

        [Fact]
        public void TestFinalize2()
        {
            VerifyMemberResolution(@"
Class A
	Protected Overrides Sub $$Finalize()
		Try
		Finally
			MyBase.Finalize()
		End Try
	End Sub
End Class
",
                LanguageNames.VisualBasic, false,
                "A.#Finalize()",
                "A.Finalize():System.Void");
        }

        [Fact]
        public void TestResolveMethodWithComplexReturnType1()
        {
            VerifyMemberResolution(@"
class C
{
    public static T[][][,,][,] $$GetComplex<T>()
    {
        return null;
    }
}
",
                LanguageNames.CSharp, false,
                "C.#GetComplex`1()",
                "C.GetComplex():T[,][,,][][]");
        }

        [Fact]
        public void TestResolveMethodWithComplexReturnType2()
        {
            VerifyMemberResolution(@"
Class C
	Public Shared Function $$GetComplex(Of T)() As T()()(,,)(,)
		Return Nothing
	End Function
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#GetComplex`1()",
                "C.GetComplex():T[,][,,][][]");
        }

        [Fact]
        public void TestResolveMethodWithGenericParametersAndReturnTypeFromContainingClass1()
        {
            VerifyMemberResolution(@"
public class C<T0>
{
    public class D<T1>
    {
        public T3 $$M<T2, T3>(T0 a, T1 b, T2 c)
        {
            return default(T3);
        }
    }
}
",
                LanguageNames.CSharp, false,
                "C`1+D`1.#M`2(!0,!1,!!0)",
                "C`1+D`1.M(T0,T1,T2):T3");
        }

        [Fact]
        public void TestResolveMethodWithGenericParametersAndReturnTypeFromContainingClass2()
        {
            VerifyMemberResolution(@"
Public Class C(Of T0)
	Public Class D(Of T1)
		Public Function $$M(Of T2, T3)(a As T0, b As T1, c As T2) As T3
			Return Nothing
		End Function
	End Class
End Class
",
                LanguageNames.VisualBasic, false,
                "C`1+D`1.#M`2(!0,!1,!!0)",
                "C`1+D`1.M(T0,T1,T2):T3");
        }

        [Fact]
        public void TestResolveIndexerWithGenericParametersTypeFromContainingClass1()
        {
            VerifyMemberResolution(@"
public class C<T0>
{
    public class D<T1>
    {
        public T0 $$this[T1 a]
        {
            get { return default(T0); }
        }
    }
}
",
                LanguageNames.CSharp, false,
                "C`1+D`1.#Item[!1]",
                "C`1+D`1.Item[!1]:!0");
        }

        [Fact]
        public void TestResolveIndexerWithGenericParametersTypeFromContainingClass2()
        {
            VerifyMemberResolution(@"
Public Class C(Of T0)
	Public Class D(Of T1)
		Public Default ReadOnly Property $$Item(a As T1) As T0
			Get
				Return Nothing
			End Get
		End Property
	End Class
End Class
",
                LanguageNames.VisualBasic, false,
                "C`1+D`1.#Item[!1]",
                "C`1+D`1.Item[!1]:!0");
        }

        [Fact]
        public void TestResolveMethodOnOutParameter1()
        {
            VerifyMemberResolution(@"
class C
{
    void M0(int x)
    {
    }

    void $$M1(out int x)
    {
        x = 1;
    }
}
",
                LanguageNames.CSharp, false,
                "C.#M1(System.Int32&)",
                "C.M1(System.Int32&):System.Void");
        }

        [Fact]
        public void TestResolveMethodOnOutParameter2()
        {
            VerifyMemberResolution(@"
Class C
	Private Sub M0(x As Integer)
	End Sub

	Private Sub $$M1(ByRef x As Integer)
		x = 1
	End Sub
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#M1(System.Int32&)",
                "C.M1(System.Int32&):System.Void");
        }

        [Fact]
        public void TestResolveMethodWithInstantiatedGenericParameterAndReturnType1()
        {
            VerifyMemberResolution(@"
class G<T0,T1>
{
}

class C<T3>
{
    G<int,int> $$M<T4>(G<double, double> g, G<T3, T4[]> h)
    {
    }
}
",
                LanguageNames.CSharp, false,
                "C.#M`1(G`2<System.Double,System.Double>,G`2<!0,!!0[]>)",
                "C.M(G`2<System.Double,System.Double>,G`2<T3,T4[]>):G`2<System.Int32,System.Int32>");
        }

        [Fact]
        public void TestResolveMethodWithInstantiatedGenericParameterAndReturnType2()
        {
            VerifyMemberResolution(@"
Class G(Of T0, T1)
End Class

Class C(Of T3)
	Private Function $$M(Of T4)(g As G(Of Double, Double), h As G(Of T3, T4())) As G(Of Integer, Integer)
	End Function
End Class
",
                LanguageNames.VisualBasic, false,
                "C.#M`1(G`2<System.Double,System.Double>,G`2<!0,!!0[]>)",
                "C.M(G`2<System.Double,System.Double>,G`2<T3,T4[]>):G`2<System.Int32,System.Int32>");
        }

        [Fact]
        public void TestResolveEscapedName1()
        {
            VerifyMemberResolution(@"
namespace @namespace
{
    class @class
    {
        int $$@if;
    }
}
",
                LanguageNames.CSharp, false,
                "namespace.class.if");
        }

        [Fact]
        public void TestResolveEscapedName2()
        {
            VerifyMemberResolution(@"
Namespace [Namespace]
	Class [Class]
		Private $$[If] As Integer
	End Class
End Namespace
",
                LanguageNames.VisualBasic, false,
                "Namespace.Class.If");
        }

        [Fact]
        public void TestResolveMethodIgnoresConvention1()
        {
            VerifyMemberResolution(@"
class C
{
    string $$Goo(string x) {}
}
",
            LanguageNames.CSharp, false,
            "C.#[vararg]Goo(System.String)",
            "C.#[cdecl]Goo(System.String)",
            "C.#[fastcall]Goo(System.String)",
            "C.#[stdcall]Goo(System.String)",
            "C.#[thiscall]Goo(System.String)");
        }

        [Fact]
        public void TestResolveMethodIgnoresConvention2()
        {
            VerifyMemberResolution(@"
Class C
    Private Function $$Goo(x As String) As String
End Function
End Class
",
            LanguageNames.VisualBasic, false,
            "C.#[vararg]Goo(System.String)",
            "C.#[cdecl]Goo(System.String)",
            "C.#[fastcall]Goo(System.String)",
            "C.#[stdcall]Goo(System.String)",
            "C.#[thiscall]Goo(System.String)");
        }

        [Fact]
        public void TestNoResolutionForMalformedNames1()
        {
            VerifyNoMemberResolution(@"
public class C<T0>
{
    public class D<T4>
    {
        int @namespace;

        public T3 M<T2, T3>(T0 a, T4 b, T2 c)
        {
            return null;
        }
    }
}
",
                LanguageNames.CSharp, false,
                "C`1+D`1.#M`2(!0,!1,!!0", // Missing close paren
                "C`1+D`1.M`2(T0,T4,T2):", // Missing return type
                "C`1+D`1.M`2(T0,T4,T2", // Missing close paren
                "C`1+D`1+M`2(T0,T4,T2)", // '+' instead of '.' delimiter
                "C`1.D`1.M`2(T0,T4,T2)", // '.' instead of '+' delimiter
                "C`1+D`1.@namespace", // Escaped name
                "C`1+D`1.#[blah]M`2(!0,!1,!!0)"); // Invalid calling convention
        }

        [Fact]
        public void TestNoResolutionForMalformedNames2()
        {
            VerifyNoMemberResolution(@"
Public Class C(Of T0)
	Public Class D(Of T1)
		Private [Namespace] As Integer

		Public Function M(Of T2, T3)(a As T0, b As T1, c As T2) As T3
			Return Nothing
		End Function
	End Class
End Class
",
                LanguageNames.VisualBasic, false,
                "C`1+D`1.#M`2(!0,!1,!!0", // Missing close paren 
                "C`1+D`1.M`2(T0,T1,T2):", // Missing return type
                "C`1+D`1.M`2(T0,T1,T2", // Missing close paren
                "C`1+D`1+M`2(T0,T1,T2)", // '+' instead of '.' delimiter
                "C`1.D`1.M`2(T0,T1,T2)", // '.' instead of '+' delimiter
                "C`1+D`1.[Namespace]", // Escaped name
                "C`1+D`1.#[blah]M`2(!0,!1,!!0)"); // Invalid calling convention
        }

        private static void VerifyNamespaceResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Namespace, language, rootNamespaceName);
        }

        private static void VerifyNoNamespaceResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyNoResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Namespace, language, rootNamespaceName);
        }

        private static void VerifyTypeResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Type, language, rootNamespaceName);
        }

        private static void VerifyNoTypeResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyNoResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Type, language, rootNamespaceName);
        }

        private static void VerifyMemberResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Member, language, rootNamespaceName);
        }

        private static void VerifyNoMemberResolution(string markup, string language, bool rootNamespace, params string[] fxCopFullyQualifiedNames)
        {
            string rootNamespaceName = "";
            if (rootNamespace)
            {
                rootNamespaceName = "RootNamespace";
            }

            VerifyNoResolution(markup, fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope.Member, language, rootNamespaceName);
        }

        private static void VerifyResolution(string markup, string[] fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope scope, string language, string rootNamespace)
        {
            // Parse out the span containing the declaration of the expected symbol
            MarkupTestFile.GetPositionAndSpans(markup,
                out var source, out var pos, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            Assert.True(pos != null || spans.Count > 0, "Must specify a position or spans marking expected symbols for resolution");

            // Get the expected symbol from the given position
            var syntaxTree = CreateSyntaxTree(source, language);
            var compilation = CreateCompilation(syntaxTree, language, rootNamespace);
            var model = compilation.GetSemanticModel(syntaxTree);
            var expectedSymbols = new List<ISymbol>();

            bool shouldResolveSingleSymbol = pos != null;
            if (shouldResolveSingleSymbol)
            {
                expectedSymbols.Add(GetSymbolAtPosition(model, pos.Value));
            }
            else
            {
                foreach (var span in spans.Values.First())
                {
                    expectedSymbols.Add(GetSymbolAtPosition(model, span.Start));
                }
            }

            // Resolve the symbol based on each given FxCop fully-qualified name
            foreach (var fxCopName in fxCopFullyQualifiedNames)
            {
                var symbols = SuppressMessageAttributeState.ResolveTargetSymbols(compilation, fxCopName, scope);

                if (shouldResolveSingleSymbol)
                {
                    var expectedSymbol = expectedSymbols.Single();

                    if (symbols.Count() > 1)
                    {
                        Assert.True(false,
                            string.Format("Expected to resolve FxCop fully-qualified name '{0}' to '{1}': got multiple symbols:\r\n{2}",
                                fxCopName, expectedSymbol, string.Join("\r\n", symbols)));
                    }

                    var symbol = symbols.SingleOrDefault();
                    Assert.True(expectedSymbol == symbol,
                        string.Format("Failed to resolve FxCop fully-qualified name '{0}' to symbol '{1}': got '{2}'",
                            fxCopName, expectedSymbol, symbol));
                }
                else
                {
                    foreach (var symbol in symbols)
                    {
                        Assert.True(expectedSymbols.Contains(symbol),
                            string.Format("Failed to resolve FxCop fully-qualified name '{0}' to symbols:\r\n{1}\r\nResolved to unexpected symbol '{2}'",
                                fxCopName, string.Join("\r\n", expectedSymbols), symbol));
                    }
                }
            }
        }

        private static ISymbol GetSymbolAtPosition(SemanticModel model, int pos)
        {
            var token = model.SyntaxTree.GetRoot().FindToken(pos);
            Assert.NotNull(token.Parent);

            var location = token.GetLocation();
            var q = from node in token.Parent.AncestorsAndSelf()
                    let candidate = model.GetDeclaredSymbol(node)
                    where candidate != null && candidate.Locations.Contains(location)
                    select candidate;

            var symbol = q.FirstOrDefault();
            Assert.NotNull(symbol);
            return symbol;
        }

        private static void VerifyNoResolution(string source, string[] fxCopFullyQualifiedNames, SuppressMessageAttributeState.TargetScope scope, string language, string rootNamespace)
        {
            var compilation = CreateCompilation(source, language, rootNamespace);

            foreach (var fxCopName in fxCopFullyQualifiedNames)
            {
                var symbols = SuppressMessageAttributeState.ResolveTargetSymbols(compilation, fxCopName, scope);

                Assert.True(symbols.FirstOrDefault() == null,
                    string.Format("Did not expect FxCop fully-qualified name '{0}' to resolve to any symbol: resolved to:\r\n{1}",
                        fxCopName, string.Join("\r\n", symbols)));
            }
        }

        private static Compilation CreateCompilation(SyntaxTree syntaxTree, string language, string rootNamespace)
        {
            string projectName = "TestProject";

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace: rootNamespace),
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
        }

        private static Compilation CreateCompilation(string source, string language, string rootNamespace)
        {
            return CreateCompilation(CreateSyntaxTree(source, language), language, rootNamespace);
        }

        private static SyntaxTree CreateSyntaxTree(string source, string language)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";

            return language == LanguageNames.CSharp ?
                CSharpSyntaxTree.ParseText(source, path: fileName) :
                VisualBasicSyntaxTree.ParseText(source, path: fileName);
        }
    }
}
