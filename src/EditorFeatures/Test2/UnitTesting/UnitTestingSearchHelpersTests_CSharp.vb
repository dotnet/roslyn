' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnitTesting
    Partial Public Class UnitTestingSearchHelpersTests
        Private Shared Async Function TestCSharp(text As String, query As UnitTestingSearchQuery, host As TestHost) As Task
            Using workspace = TestWorkspace.CreateCSharp(text,
                composition:=If(host = TestHost.OutOfProcess, s_outOffProcessComposition, s_inProcessComposition))

                Await Test(query, workspace)
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestType1(host As TestHost) As Task
            Await TestCSharp("
[Test]
class [|Outer|]
{
}", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestType1_NoAttribute(host As TestHost) As Task
            Await TestCSharp("
class [|Outer|]
{
}", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestType1_CaseSensitive(host As TestHost) As Task
            Await TestCSharp("
[Test]
class Outer
{
}", UnitTestingSearchQuery.ForType("outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestGenericType1(host As TestHost) As Task
            Await TestCSharp("
[Test]
class [|Outer|]<T>
{
}", UnitTestingSearchQuery.ForType("Outer`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestGenericType2(host As TestHost) As Task
            Await TestCSharp("
[Test]
class Outer<T>
{
}", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestGenericType2_NonStrict(host As TestHost) As Task
            Await TestCSharp("
[Test]
class [|Outer|]<T>
{
}", UnitTestingSearchQuery.ForType("Outer", strict:=False), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestGenericType3(host As TestHost) As Task
            Await TestCSharp("
[Test]
class Outer<T>
{
}", UnitTestingSearchQuery.ForType("Outer`2"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestGenericType3_NonStrict(host As TestHost) As Task
            Await TestCSharp("
[Test]
class [|Outer|]<T>
{
}", UnitTestingSearchQuery.ForType("Outer`2", strict:=False), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType1(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    class [|Inner|]
    {
    }
}", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType1_NoAttribute1(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    class [|Inner|]
    {
    }
}", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType1_NoAttribute2(host As TestHost) As Task
            Await TestCSharp("
[Test]
class Outer
{
    class [|Inner|]
    {
    }
}", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType2(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    class [|Inner|]
    {
    }
}", UnitTestingSearchQuery.ForType("Outer+Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType3(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    class [|Inner|]<T>
    {
    }
}", UnitTestingSearchQuery.ForType("Outer+Inner`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType4(host As TestHost) As Task
            Await TestCSharp("
class Outer<T>
{
    [Test]
    class [|Inner|]
    {
    }
}", UnitTestingSearchQuery.ForType("Outer`1+Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestNestedType5(host As TestHost) As Task
            Await TestCSharp("
class Outer<T>
{
    [Test]
    class [|Inner|]<U>
    {
    }
}", UnitTestingSearchQuery.ForType("Outer`1+Inner`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace1(host As TestHost) As Task
            Await TestCSharp("
namespace N
{
    [Test]
    class [|Outer|]
    {
    }
}", UnitTestingSearchQuery.ForType("N.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace1_CaseSensitive1(host As TestHost) As Task
            Await TestCSharp("
namespace N
{
    [Test]
    class Outer
    {
    }
}", UnitTestingSearchQuery.ForType("N.outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace1_CaseSensitive2(host As TestHost) As Task
            Await TestCSharp("
namespace N
{
    [Test]
    class Outer
    {
    }
}", UnitTestingSearchQuery.ForType("n.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace1_CaseSensitive3(host As TestHost) As Task
            Await TestCSharp("
namespace N
{
    [Test]
    class Outer
    {
    }
}", UnitTestingSearchQuery.ForType("n.outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace2(host As TestHost) As Task
            Await TestCSharp("
namespace N
{
    [Test]
    class Outer
    {
    }
}", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace3(host As TestHost) As Task
            Await TestCSharp("
namespace N1.N2
{
    [Test]
    class [|Outer|]
    {
    }
}", UnitTestingSearchQuery.ForType("N1.N2.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestTypeWithNamespace4(host As TestHost) As Task
            Await TestCSharp("
namespace N1
{
    namespace N2
    {
        [Test]
        class [|Outer|]
        {
        }
    }
}", UnitTestingSearchQuery.ForType("N1.N2.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod1(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|]() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod1_NoAttribute1(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    void Goo() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod1_NoAttribute2(host As TestHost) As Task
            Await TestCSharp("
[Test]
class Outer
{
    void Goo() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod2(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void Goo() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod2_NonStrict(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|]() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=0, strict:=False), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod3(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void Goo() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod3_NonStrict(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|]() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1, strict:=False), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod4(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void Goo<T>() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod5(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void Goo(int i) { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod6(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|]<T>() { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod7(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|](int a) { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod8(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|]<T>(int a) { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod9(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    class Inner
    {
        [Test]
        void [|Goo|]() { }
    }
}", UnitTestingSearchQuery.ForMethod("Outer+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod10(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    class Inner<T>
    {
        [Test]
        void [|Goo|]() { }
    }
}", UnitTestingSearchQuery.ForMethod("Outer+Inner`1", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod11(host As TestHost) As Task
            Await TestCSharp("
class Outer<T>
{
    class Inner
    {
        [Test]
        void [|Goo|]() { }
    }
}", UnitTestingSearchQuery.ForMethod("Outer`1+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod12(host As TestHost) As Task
            Await TestCSharp("
class Outer<T>
{
    class Inner<U>
    {
        [Test]
        void [|Goo|]() { }
    }
}", UnitTestingSearchQuery.ForMethod("Outer`1+Inner`1", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestMethod13(host As TestHost) As Task
            Await TestCSharp("
namespace N1.N2
{
    class Outer
    {
        class Inner
        {
            [Test]
            void [|Goo|]() { }
        }
    }
}", UnitTestingSearchQuery.ForMethod("N1.N2.Outer+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CS_TestExtensionMethod1(host As TestHost) As Task
            Await TestCSharp("
class Outer
{
    [Test]
    void [|Goo|](this Outer o) { }
}", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function
    End Class
End Namespace
