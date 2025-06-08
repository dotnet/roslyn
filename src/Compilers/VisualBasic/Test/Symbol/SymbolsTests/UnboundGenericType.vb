' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols

    Partial Public Class InstantiatingGenerics

        <Fact>
        Public Sub UnboundGenericType1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
Interface I1
End Interface

Class C1
    Class C2(Of T1, T2)
        Implements I1

        Class C7
        End Class

        Sub M1()
        End Sub
    End Class
End Class

Class C3(Of T)
    Class C4
        Class C5(Of T1, T2)
            Class C7
            End Class

            Sub M1()
            End Sub
        End Class

        Class C7
        End Class

        Sub M1()
        End Sub
    End Class

    Class C6(Of T1)
        Class C7(Of T1)
        End Class
    End Class

    Sub M1()
    End Sub
End Class
    </file>
</compilation>)

            Dim c1 = compilation.GetTypeByMetadataName("C1")
            Dim c2 = c1.GetTypeMembers("C2").Single()
            Dim c3 = compilation.GetTypeByMetadataName("C3`1")
            Dim c4 = c3.GetTypeMembers("C4").Single()
            Dim c5 = c4.GetTypeMembers("C5").Single()
            Dim c6 = c3.GetTypeMembers("C6").Single()
            Dim c7 = c6.GetTypeMembers("C7").Single()

            Assert.False(c1.IsUnboundGenericType)
            Assert.False(c2.IsUnboundGenericType)
            Assert.False(c3.IsUnboundGenericType)
            Assert.False(c4.IsUnboundGenericType)
            Assert.False(c5.IsUnboundGenericType)
            Assert.False(c6.IsUnboundGenericType)
            Assert.False(IsUnboundGenericType(c6))
            Assert.False(IsUnboundGenericType(Nothing))
            Assert.False(IsUnboundGenericType(compilation.CreateArrayTypeSymbol(c1)))

            Assert.Throws(Of InvalidOperationException)(Sub() c1.ConstructUnboundGenericType())

            Dim u_c2 = c2.ConstructUnboundGenericType()
            Assert.True(u_c2.IsUnboundGenericType)
            Assert.True(IsUnboundGenericType(u_c2))
            u_c2.VerifyGenericInstantiationInvariants()
            Assert.Same(c2.ContainingSymbol, u_c2.ContainingSymbol)
            Assert.Equal(u_c2, u_c2)
            Dim u__c2 = c2.ConstructUnboundGenericType()
            Assert.NotSame(u__c2, u_c2)
            Assert.Equal(u__c2, u_c2)
            Assert.Equal(u__c2.GetHashCode(), u_c2.GetHashCode())
            Assert.Equal("C7", u_c2.MemberNames.Single())
            Assert.Equal("C1.C2(Of ,).C7", u_c2.GetMembers().Single().ToTestDisplayString())
            Assert.Equal("C1.C2(Of ,).C7", u_c2.GetMembers("c7").Single().ToTestDisplayString())
            Assert.Equal(0, u_c2.GetMembers("M1").Length)
            Assert.Equal("C1.C2(Of ,).C7", u_c2.GetTypeMembers().Single().ToTestDisplayString())
            Assert.Equal("C1.C2(Of ,).C7", u_c2.GetTypeMembers("c7").Single().ToTestDisplayString())
            Assert.Equal("C1.C2(Of ,).C7", u_c2.GetTypeMembers("c7", 0).Single().ToTestDisplayString())
            Assert.Equal(0, u_c2.GetTypeMembers("C7", 1).Length)

            Dim u_c3 = c3.ConstructUnboundGenericType()
            Assert.True(u_c3.IsUnboundGenericType)
            u_c3.VerifyGenericInstantiationInvariants()
            Assert.Same(c3.ContainingSymbol, u_c3.ContainingSymbol)
            Assert.Equal(u_c3, u_c3)
            Dim u__c3 = c3.ConstructUnboundGenericType()
            Assert.NotSame(u__c3, u_c3)
            Assert.Equal(u__c3, u_c3)
            Assert.Equal(u__c3.GetHashCode(), u_c3.GetHashCode())
            Assert.Equal("C4, C6", String.Join(", ", u_c3.MemberNames))
            Assert.Equal("C3(Of ).C4, C3(Of ).C6(Of T1)", String.Join(", ", u_c3.GetMembers().Select(Function(s) s.ToTestDisplayString())))

            Assert.Equal(0, u_c3.GetMembers().As(Of NamedTypeSymbol)().Where(Function(s) Not s.ContainingType.IsUnboundGenericType OrElse s.IsUnboundGenericType <> (s.Arity = 0)).Count)
            Assert.Equal("C3(Of ).C6(Of T1)", String.Join(", ", u_c3.GetMembers("c6").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal(0, u_c3.GetMembers("c6").As(Of NamedTypeSymbol)().Where(Function(s) Not s.ContainingType.IsUnboundGenericType OrElse s.IsUnboundGenericType <> (s.Arity = 0)).Count)
            Assert.Equal(0, u_c3.GetMembers("M1").Length)
            Assert.Equal("C3(Of ).C4, C3(Of ).C6(Of T1)", String.Join(", ", u_c3.GetTypeMembers().Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal(0, u_c3.GetTypeMembers().As(Of NamedTypeSymbol)().Where(Function(s) Not s.ContainingType.IsUnboundGenericType OrElse s.IsUnboundGenericType <> (s.Arity = 0)).Count)
            Assert.Equal("C3(Of ).C4", String.Join(", ", u_c3.GetTypeMembers("c4").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal(0, u_c3.GetTypeMembers("c4").As(Of NamedTypeSymbol)().Where(Function(s) Not s.ContainingType.IsUnboundGenericType OrElse s.IsUnboundGenericType <> (s.Arity = 0)).Count)
            Assert.Equal("C3(Of ).C6(Of T1)", u_c3.GetTypeMembers("c6", 1).Single().ToTestDisplayString())
            Assert.Equal(0, u_c3.GetTypeMembers("c6", 1).Where(Function(s) Not s.ContainingType.IsUnboundGenericType OrElse s.IsUnboundGenericType <> (s.Arity = 0)).Count)
            Assert.Equal(0, u_c3.GetTypeMembers("C4", 1).Length)

            Assert.NotEqual(u_c2, u_c3)

            Dim u_c4 = c4.ConstructUnboundGenericType()
            Assert.True(u_c4.IsUnboundGenericType)
            u_c4.VerifyGenericInstantiationInvariants()
            Assert.Equal(u_c3, u_c4.ContainingSymbol)
            Assert.Equal(u_c4, u_c4)
            Dim u__c4 = c4.ConstructUnboundGenericType()
            Assert.NotSame(u__c4, u_c4)
            Assert.Equal(u__c4, u_c4)
            Assert.Equal(u__c4.GetHashCode(), u_c4.GetHashCode())
            Assert.Equal("C5, C7", String.Join(", ", u_c4.MemberNames))
            Assert.Equal("C3(Of ).C4.C5(Of T1, T2), C3(Of ).C4.C7", String.Join(", ", u_c4.GetMembers().Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C5(Of T1, T2)", String.Join(", ", u_c4.GetMembers("c5").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal(0, u_c4.GetMembers("M1").Length)
            Assert.Equal("C3(Of ).C4.C5(Of T1, T2), C3(Of ).C4.C7", String.Join(", ", u_c4.GetTypeMembers().Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C7", String.Join(", ", u_c4.GetTypeMembers("c7").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C5(Of T1, T2)", u_c4.GetTypeMembers("c5", 2).Single().ToTestDisplayString())
            Assert.Equal(0, u_c4.GetTypeMembers("C5", 1).Length)
            Assert.Same(u_c4, u_c4.ConstructedFrom)
            Assert.Same(u_c4, u_c4.ConstructUnboundGenericType())

            Dim u_c5 = c5.ConstructUnboundGenericType()
            Assert.True(u_c5.IsUnboundGenericType)
            u_c5.VerifyGenericInstantiationInvariants()
            Assert.Equal(u_c4, u_c5.ContainingSymbol)
            Assert.Equal(u_c5, u_c5)
            Dim u__c5 = c5.ConstructUnboundGenericType()
            Assert.NotSame(u__c5, u_c5)
            Assert.Equal(u__c5, u_c5)
            Assert.Equal(u__c5.GetHashCode(), u_c5.GetHashCode())
            Assert.Equal("C7", String.Join(", ", u_c5.MemberNames))
            Assert.Equal("C3(Of ).C4.C5(Of ,).C7", String.Join(", ", u_c5.GetMembers().Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C5(Of ,).C7", String.Join(", ", u_c5.GetMembers("c7").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal(0, u_c5.GetMembers("M1").Length)
            Assert.Equal("C3(Of ).C4.C5(Of ,).C7", String.Join(", ", u_c5.GetTypeMembers().Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C5(Of ,).C7", String.Join(", ", u_c5.GetTypeMembers("c7").Select(Function(s) s.ToTestDisplayString())))
            Assert.Equal("C3(Of ).C4.C5(Of ,).C7", u_c5.GetTypeMembers("C7", 0).Single().ToTestDisplayString())
            Assert.Equal(0, u_c5.GetTypeMembers("C4", 1).Length)

            Dim u_c5_cf = u_c5.ConstructedFrom
            Dim u__c5_cf = u__c5.ConstructedFrom

            Assert.Equal(u_c5_cf, u_c5_cf)
            Assert.NotSame(u__c5_cf, u_c5_cf)
            Assert.Equal(u__c5_cf, u_c5_cf)
            Assert.Equal(u__c5_cf.GetHashCode(), u_c5_cf.GetHashCode())

            Assert.False(u_c5_cf.IsUnboundGenericType)
            u_c5_cf.VerifyGenericInstantiationInvariants()

            Assert.Same(u_c5.ContainingSymbol, u_c5_cf.ContainingSymbol)
            Assert.Same(u_c5_cf, u_c5_cf.ConstructedFrom)
            Assert.Same(u_c5, u_c5_cf.ConstructUnboundGenericType())

            Assert.Equal(0, u_c5_cf.MemberNames.Count())
            Assert.Equal(0, u_c5_cf.GetMembers().Length)
            Assert.Equal(0, u_c5_cf.GetMembers("c7").Length)
            Assert.Equal(0, u_c5_cf.GetMembers("M1").Length)
            Assert.Equal(0, u_c5_cf.GetTypeMembers().Length)
            Assert.Equal(0, u_c5_cf.GetTypeMembers("c7").Length)
            Assert.Equal(0, u_c5_cf.GetTypeMembers("C7", 0).Length)
            Assert.Equal(0, u_c5_cf.GetTypeMembers("C4", 1).Length)

            Dim u_c6 = c6.ConstructUnboundGenericType()
            Assert.True(u_c6.IsUnboundGenericType)
            u_c6.VerifyGenericInstantiationInvariants()
            Assert.Equal(u_c3, u_c6.ContainingSymbol)
            Assert.Equal(u_c6, u_c6)
            Dim u__c6 = c6.ConstructUnboundGenericType()
            Assert.NotSame(u__c6, u_c6)
            Assert.Equal(u__c6, u_c6)
            Assert.Equal(u__c6.GetHashCode(), u_c6.GetHashCode())

            Dim u_c6_cf = u_c6.ConstructedFrom
            Dim u__c6_cf = u__c6.ConstructedFrom

            Assert.Equal(u_c6_cf, u_c6_cf)
            Assert.NotSame(u__c6_cf, u_c6_cf)
            Assert.Equal(u__c6_cf, u_c6_cf)
            Assert.Equal(u__c6_cf.GetHashCode(), u_c6_cf.GetHashCode())

            Assert.False(u_c6_cf.IsUnboundGenericType)
            u_c6_cf.VerifyGenericInstantiationInvariants()

            Assert.Same(u_c6.ContainingSymbol, u_c6_cf.ContainingSymbol)
            Assert.Same(u_c6_cf, u_c6_cf.ConstructedFrom)
            Assert.Same(u_c6, u_c6_cf.ConstructUnboundGenericType())

            Dim u_c7 = c7.ConstructUnboundGenericType()
            Assert.True(u_c7.IsUnboundGenericType)
            u_c7.VerifyGenericInstantiationInvariants()
            Assert.Equal(u_c6, u_c7.ContainingSymbol)
            Assert.Equal(u_c7, u_c7)
            Dim u__c7 = c7.ConstructUnboundGenericType()
            Assert.NotSame(u__c7, u_c7)
            Assert.Equal(u__c7, u_c7)
            Assert.Equal(u__c7.GetHashCode(), u_c7.GetHashCode())

            Dim u_c7_cf = u_c7.ConstructedFrom
            Dim u__c7_cf = u__c7.ConstructedFrom

            Assert.Equal(u_c7_cf, u_c7_cf)
            Assert.NotSame(u__c7_cf, u_c7_cf)
            Assert.Equal(u__c7_cf, u_c7_cf)
            Assert.Equal(u__c7_cf.GetHashCode(), u_c7_cf.GetHashCode())

            Assert.False(u_c7_cf.IsUnboundGenericType)
            u_c7_cf.VerifyGenericInstantiationInvariants()

            Assert.Same(u_c7.ContainingSymbol, u_c7_cf.ContainingSymbol)
            Assert.Same(u_c7_cf, u_c7_cf.ConstructedFrom)
            Assert.Same(u_c7, u_c7_cf.ConstructUnboundGenericType())
        End Sub

        <Fact>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Public Sub UnboundGenericType_IsSerializable()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb"><![CDATA[

Class C3(Of T)
    Class C6(Of T1)
    End Class
End Class

<System.Serializable>
Class C3S(Of T)
    <System.Serializable>
    Class C6S(Of T1)
    End Class
End Class
    ]]></file>
</compilation>)

            Dim c3 = compilation.GetTypeByMetadataName("C3`1")
            Dim c6 = c3.GetTypeMembers("C6").Single()

            Dim u_c3 = c3.ConstructUnboundGenericType()
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Symbols.UnboundGenericType+ConstructedSymbol", u_c3.GetType().FullName)
            Assert.False(DirectCast(u_c3, INamedTypeSymbol).IsSerializable)

            Dim c3c6 = u_c3.GetMember("C6")
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Symbols.UnboundGenericType+ConstructedFromSymbol", c3c6.GetType().FullName)
            Assert.False(DirectCast(c3c6, INamedTypeSymbol).IsSerializable)

            Dim c3s = compilation.GetTypeByMetadataName("C3S`1")
            Dim c6s = c3s.GetTypeMembers("C6S").Single()

            Dim u_c3s = c3s.ConstructUnboundGenericType()
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Symbols.UnboundGenericType+ConstructedSymbol", u_c3s.GetType().FullName)
            Assert.True(DirectCast(u_c3s, INamedTypeSymbol).IsSerializable)

            Dim c3c6s = u_c3s.GetMember("C6S")
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Symbols.UnboundGenericType+ConstructedFromSymbol", c3c6s.GetType().FullName)
            Assert.True(DirectCast(c3c6s, INamedTypeSymbol).IsSerializable)
        End Sub

        <Fact>
        <WorkItem(41779, "https://github.com/dotnet/roslyn/issues/41779")>
        Public Sub UnboundGenericType_Bug41779_Original()

            Dim compilation = CompilationUtils.CreateCompilation(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Interface I
    Function GetService() As Object
End Interface

Module Program
    <Extension()>
    Private Function GetService(Of T)(ByVal obj As I) As T
        Return "default"
    End Function

    Private Sub M(ByVal provider As I)
        provider.GetService(Of)()
        provider.GetService(Of)().ToString()
        provider.GetService(Of)()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30311: Value of type 'String' cannot be converted to 'T'.
        Return "default"
               ~~~~~~~~~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
BC30182: Type expected.
        provider.GetService(Of)().ToString()
                              ~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
                </expected>)

        End Sub

        <Fact>
        <WorkItem(41779, "https://github.com/dotnet/roslyn/issues/41779")>
        Public Sub UnboundGenericType_Bug41779_DoubleArgs()

            Dim compilation = CompilationUtils.CreateCompilation(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Interface I
    Function GetService() As Object
End Interface

Module Program
    <Extension()>
    Private Function GetService(Of T1, T2)(ByVal obj As I) As T1
        Return "default"
    End Function

    Private Sub M(ByVal provider As I)
        provider.GetService(Of)()
        provider.GetService(Of)().ToString()
        provider.GetService(Of)()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30311: Value of type 'String' cannot be converted to 'T1'.
        Return "default"
               ~~~~~~~~~
BC32087: Overload resolution failed because no accessible 'GetService' accepts this number of type arguments.
        provider.GetService(Of)()
                 ~~~~~~~~~~~~~~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
BC32087: Overload resolution failed because no accessible 'GetService' accepts this number of type arguments.
        provider.GetService(Of)().ToString()
                 ~~~~~~~~~~~~~~
BC30182: Type expected.
        provider.GetService(Of)().ToString()
                              ~
BC32087: Overload resolution failed because no accessible 'GetService' accepts this number of type arguments.
        provider.GetService(Of)()
                 ~~~~~~~~~~~~~~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
            </expected>)

        End Sub

        <Fact>
        <WorkItem(41779, "https://github.com/dotnet/roslyn/issues/41779")>
        Public Sub UnboundGenericType_Bug41779_Instance()

            Dim compilation = CompilationUtils.CreateCompilation(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Interface I
    Function GetService() As Object
End Interface

Interface J
    Function GetService(Of T)() As Object
End Interface

Interface K
    Function GetService(Of T1, T2)() As Object
End Interface

Module Program
    Private Sub M(ByVal provider As I)
        provider.GetService(Of)()
        provider.GetService(Of)().ToString()
    End Sub

    Private Sub M(ByVal provider As J)
        provider.GetService(Of)()
        provider.GetService(Of)().ToString()
    End Sub

    Private Sub M(ByVal provider As K)
        provider.GetService(Of)()
        provider.GetService(Of)().ToString()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC32045: 'Function GetService() As Object' has no type parameters and so cannot have type arguments.
        provider.GetService(Of)()
                           ~~~~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
BC32045: 'Function GetService() As Object' has no type parameters and so cannot have type arguments.
        provider.GetService(Of)().ToString()
                           ~~~~
BC30182: Type expected.
        provider.GetService(Of)().ToString()
                              ~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
BC30182: Type expected.
        provider.GetService(Of)().ToString()
                              ~
BC32042: Too few type arguments to 'Function GetService(Of T1, T2)() As Object'.
        provider.GetService(Of)()
                           ~~~~
BC30182: Type expected.
        provider.GetService(Of)()
                              ~
BC32042: Too few type arguments to 'Function GetService(Of T1, T2)() As Object'.
        provider.GetService(Of)().ToString()
                           ~~~~
BC30182: Type expected.
        provider.GetService(Of)().ToString()
                              ~
            </expected>)

        End Sub

        <Fact>
        <WorkItem(41779, "https://github.com/dotnet/roslyn/issues/41779")>
        Public Sub UnboundGenericType_Bug41779_Extension()

            Dim compilation = CompilationUtils.CreateCompilation(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Interface I
End Interface

Module Program
    <Extension()>
    Private Sub GetServiceA(ByVal obj As I)
    End Sub

    <Extension()>
    Private Function GetServiceB(Of T)(ByVal obj As I) As T
        Return "default"
    End Function

    <Extension()>
    Private Function GetServiceC(Of T1, T2)(ByVal obj As I) As T1
        Return "default"
    End Function

    Private Sub M(ByVal provider As I)
        provider.GetServiceA(Of)()
        provider.GetServiceA(Of)().ToString()
        provider.GetServiceB(Of)()
        provider.GetServiceB(Of)().ToString()
        provider.GetServiceC(Of)()
        provider.GetServiceC(Of)().ToString()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30311: Value of type 'String' cannot be converted to 'T'.
        Return "default"
               ~~~~~~~~~
BC30311: Value of type 'String' cannot be converted to 'T1'.
        Return "default"
               ~~~~~~~~~
BC36907: Extension method 'Private Sub GetServiceA()' defined in 'Program' is not generic (or has no free type parameters) and so cannot have type arguments.
        provider.GetServiceA(Of)()
                            ~~~~
BC30182: Type expected.
        provider.GetServiceA(Of)()
                               ~
BC36907: Extension method 'Private Sub GetServiceA()' defined in 'Program' is not generic (or has no free type parameters) and so cannot have type arguments.
        provider.GetServiceA(Of)().ToString()
                            ~~~~
BC30182: Type expected.
        provider.GetServiceA(Of)().ToString()
                               ~
BC30182: Type expected.
        provider.GetServiceB(Of)()
                               ~
BC30182: Type expected.
        provider.GetServiceB(Of)().ToString()
                               ~
BC36590: Too few type arguments to extension method 'Private Function GetServiceC(Of T1, T2)() As T1' defined in 'Program'.
        provider.GetServiceC(Of)()
                            ~~~~
BC30182: Type expected.
        provider.GetServiceC(Of)()
                               ~
BC36590: Too few type arguments to extension method 'Private Function GetServiceC(Of T1, T2)() As T1' defined in 'Program'.
        provider.GetServiceC(Of)().ToString()
                            ~~~~
BC30182: Type expected.
        provider.GetServiceC(Of)().ToString()
                               ~
            </expected>)

        End Sub

        <Fact>
        <WorkItem(41779, "https://github.com/dotnet/roslyn/issues/41779")>
        Public Sub UnboundGenericType_Bug41779_Function()

            Dim compilation = CompilationUtils.CreateCompilation(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Module Program
    Private Function GetServiceA() As Object
        Return Nothing
    End Function

    Private Function GetServiceB(Of T)() As T
        Return "default"
    End Function

    Private Function GetServiceC(Of T1, T2)() As T1
        Return "default"
    End Function

    Private Sub M()
        GetServiceA(Of)()
        GetServiceA(Of)().ToString()
        GetServiceB(Of)()
        GetServiceB(Of)().ToString()
        GetServiceC(Of)()
        GetServiceC(Of)().ToString()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30311: Value of type 'String' cannot be converted to 'T'.
        Return "default"
               ~~~~~~~~~
BC30311: Value of type 'String' cannot be converted to 'T1'.
        Return "default"
               ~~~~~~~~~
BC32045: 'Private Function GetServiceA() As Object' has no type parameters and so cannot have type arguments.
        GetServiceA(Of)()
                   ~~~~
BC30182: Type expected.
        GetServiceA(Of)()
                      ~
BC32045: 'Private Function GetServiceA() As Object' has no type parameters and so cannot have type arguments.
        GetServiceA(Of)().ToString()
                   ~~~~
BC30182: Type expected.
        GetServiceA(Of)().ToString()
                      ~
BC30182: Type expected.
        GetServiceB(Of)()
                      ~
BC30182: Type expected.
        GetServiceB(Of)().ToString()
                      ~
BC32042: Too few type arguments to 'Private Function GetServiceC(Of T1, T2)() As T1'.
        GetServiceC(Of)()
                   ~~~~
BC30182: Type expected.
        GetServiceC(Of)()
                      ~
BC32042: Too few type arguments to 'Private Function GetServiceC(Of T1, T2)() As T1'.
        GetServiceC(Of)().ToString()
                   ~~~~
BC30182: Type expected.
        GetServiceC(Of)().ToString()
                      ~
            </expected>)

        End Sub

    End Class

End Namespace
