' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnitTesting
    Public Class UnitTestingSearchHelpersTests
        Private Shared Async Function TestVisualBasic(text As String, query As UnitTestingSearchQuery, host As TestHost) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(text,
                composition:=If(host = TestHost.OutOfProcess, s_outOffProcessComposition, s_inProcessComposition))

                Await Test(query, workspace)
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestType1(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class [|Outer|]
end class
", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestType1_NoAttribute(host As TestHost) As Task
            Await TestVisualBasic("
class [|Outer|]
end class
", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestType1_CaseInsensitive(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class [|Outer|]
end class
", UnitTestingSearchQuery.ForType("outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestGenericType1(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class [|Outer|](of T)
end class
", UnitTestingSearchQuery.ForType("Outer`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestGenericType2(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class Outer(of T)
end class
", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestGenericType3(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class Outer(of T)
end class
", UnitTestingSearchQuery.ForType("Outer`2"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType1(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    class [|Inner|]
    end class
end class
", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType1_NoAttribute1(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    class [|Inner|]
    end class
end class
", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType1_NoAttribute2(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class Outer
    class [|Inner|]
    end class
end class
", UnitTestingSearchQuery.ForType("Outer.Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType2(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    class [|Inner|]
    end class
end class
", UnitTestingSearchQuery.ForType("Outer+Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType3(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    class [|Inner|](of T)
    end class
end class
", UnitTestingSearchQuery.ForType("Outer+Inner`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType4(host As TestHost) As Task
            Await TestVisualBasic("
class Outer(of T)
    <Test>
    class [|Inner|]
    end class
end class
", UnitTestingSearchQuery.ForType("Outer`1+Inner"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestNestedType5(host As TestHost) As Task
            Await TestVisualBasic("
class Outer(of T)
    <Test>
    class [|Inner|](of U)
    end class
end class
", UnitTestingSearchQuery.ForType("Outer`1+Inner`1"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace1(host As TestHost) As Task
            Await TestVisualBasic("
namespace N
    <Test>
    class [|Outer|]
    end class
end namespace", UnitTestingSearchQuery.ForType("N.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace1_CaseInsensitive1(host As TestHost) As Task
            Await TestVisualBasic("
namespace N
    <Test>
    class [|Outer|]
    end class
end namespace", UnitTestingSearchQuery.ForType("n.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace1_CaseInsensitive2(host As TestHost) As Task
            Await TestVisualBasic("
namespace N
    <Test>
    class [|Outer|]
    end class
end namespace", UnitTestingSearchQuery.ForType("N.outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace1_CaseInsensitive3(host As TestHost) As Task
            Await TestVisualBasic("
namespace N
    <Test>
    class [|Outer|]
    end class
end namespace", UnitTestingSearchQuery.ForType("n.outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace2(host As TestHost) As Task
            Await TestVisualBasic("
namespace N
    <Test>
    class Outer
    end class
end namespace
", UnitTestingSearchQuery.ForType("Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace3(host As TestHost) As Task
            Await TestVisualBasic("
namespace N1.N2
    <Test>
    class [|Outer|]
    end class
end namespace", UnitTestingSearchQuery.ForType("N1.N2.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestTypeWithNamespace4(host As TestHost) As Task
            Await TestVisualBasic("
namespace N1
    namespace N2
        <Test>
        class [|Outer|]
        end class
    end namespace
end namespace
", UnitTestingSearchQuery.ForType("N1.N2.Outer"), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod1(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub [|Goo|]()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod1_NoAttribute1(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    sub Goo()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod1_NoAttribute2(host As TestHost) As Task
            Await TestVisualBasic("
<Test>
class Outer
    sub Goo()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod2(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub Goo()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod3(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub Goo()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod4(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub Goo(of T)()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod5(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub Goo(i as integer)
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod6(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub [|Goo|](of T)()
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod7(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub [|Goo|](a as integer)
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod8(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    <Test>
    sub [|Goo|](of T)(a as integer)
    end sub
end class
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=1, methodParameterCount:=1), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod9(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    class Inner
        <Test>
        sub [|Goo|]()
        end sub
    end class
end class
", UnitTestingSearchQuery.ForMethod("Outer+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod10(host As TestHost) As Task
            Await TestVisualBasic("
class Outer
    class Inner(of T)
        <Test>
        sub [|Goo|]()
        end sub
    end class
end class
", UnitTestingSearchQuery.ForMethod("Outer+Inner`1", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod11(host As TestHost) As Task
            Await TestVisualBasic("
class Outer(of T)
    class Inner
        <Test>
        sub [|Goo|]()
        end sub
    end class
end class
", UnitTestingSearchQuery.ForMethod("Outer`1+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod12(host As TestHost) As Task
            Await TestVisualBasic("
class Outer(of T)
    class Inner(of U)
        <Test>
        sub [|Goo|]()
        end sub
    end class
end class
", UnitTestingSearchQuery.ForMethod("Outer`1+Inner`1", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestMethod13(host As TestHost) As Task
            Await TestVisualBasic("
namespace N1.N2
    class Outer
        class Inner
            <Test>
            sub [|Goo|]()
            end sub
        end class
    end class
end namespace
", UnitTestingSearchQuery.ForMethod("N1.N2.Outer+Inner", "Goo", methodArity:=0, methodParameterCount:=0), host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function VB_TestExtensionMethod1(host As TestHost) As Task
            Await TestVisualBasic("
module Outer
    <Test>
    sub [|Goo|](i as integer)
    end sub
end module
", UnitTestingSearchQuery.ForMethod("Outer", "Goo", methodArity:=0, methodParameterCount:=1), host)
        End Function
    End Class
End Namespace
