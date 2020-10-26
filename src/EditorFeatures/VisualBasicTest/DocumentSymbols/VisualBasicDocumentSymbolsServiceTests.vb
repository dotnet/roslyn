' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.DocumentSymbols
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentSymbols
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.DocumentSymbols

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentSymbols
    Public Class VisualBasicDocumentSymbolsServiceTests
        Inherits AbstractDocumentSymbolsServiceTests(Of VisualBasicTestWorkspaceFixture)

        Protected Overrides Function GetDocumentSymbolsServicePartType() As Type
            Return GetType(VisualBasicDocumentSymbolsService)
        End Function

        Protected Overrides Function GetDocumentSymbolsService(document1 As Document) As IDocumentSymbolsService
            Return Assert.IsType(Of VisualBasicDocumentSymbolsService)(MyBase.GetDocumentSymbolsService(document1))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Classes() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Class C2
    End Class
End Class
Class C4 ' Intentionally out of order to demonstrate sorting
End Class
Class C3
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  C1.C2
C4
C3
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
C1.C2
C3
C4
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Modules() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Module M1
    Module M2
    End Module
End Module
Module M4 ' Intentionally out of order to demonstrate sorting
End Module
Module M3
End Module
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
M1
  M1.M2
M4
M3
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
M1
M1.M2
M3
M4
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__NestedMethods() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Sub S1()
    End Sub

    Function F1(p1 As Object)
        Return p1
    End Function

    Class C2
        Sub S2()
        End Sub

        Function F2(p2 As Object)
            Return p2
        End Function
    End Class
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.S1()
  Function C1.F1(p1 As System.Object) As System.Object
  C1.C2
    Sub C1.C2.S2()
    Function C1.C2.F2(p2 As System.Object) As System.Object
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Function C1.F1(p1 As System.Object) As System.Object
  Sub C1.S1()
C1.C2
  Function C1.C2.F2(p2 As System.Object) As System.Object
  Sub C1.C2.S2()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Locals() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Sub S1()
        Dim i1 As New Integer()
        Dim i2, i3 = 1
        Shared Dim i4 As Integer
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.S1()
    i1 As System.Int32
    i2 As System.Object
    i3 As System.Int32
    i4 As System.Int32
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.S1()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Fields() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Private Dim f1 As Integer
    Class C2
        Private Dim f2 As Integer
    End Class
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  C1.f1 As System.Int32
  C1.C2
    C1.C2.f2 As System.Int32
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  C1.f1 As System.Int32
C1.C2
  C1.C2.f2 As System.Int32
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Properties() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Private Readonly Property P1 As Integer
    Public Property P2 As Object
    Public Property P3 As String
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  ReadOnly Property C1.P1 As System.Int32
  Property C1.P2 As System.Object
  Property C1.P3 As System.String
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  ReadOnly Property C1.P1 As System.Int32
  Property C1.P2 As System.Object
  Property C1.P3 As System.String
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Events() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Imports System
Class C1
    Public Event E1()
    Public Event E2 As EventHandler

    Public Custom Event E3 As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler

        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler

        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event 
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Event C1.E1()
  Event C1.E2 As System.EventHandler
  Event C1.E3 As System.EventHandler
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Event C1.E1()
  Event C1.E2 As System.EventHandler
  Event C1.E3 As System.EventHandler
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Constants() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Const i1 As Integer = 1 

    Public Sub S1()
        Const i2 As Integer = 2
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  C1.i1 As System.Int32
  Sub C1.S1()
    i2 As System.Int32
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  C1.i1 As System.Int32
  Sub C1.S1()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__NamespaceSymbolsNotIncluded() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Namespace N1
    Class C1
    End Class
End Namespace
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
N1.C1
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
N1.C1
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Ordering() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Class C1
    Property P1 As Integer
    Sub S1()
    End Sub

    Sub Finalize()
    End Sub

    Sub finalize(i As Integer)
    End Sub

    Overrides Sub Finalize()
    End Sub

    Sub New(i As Integer)
    End Sub

    Sub New()
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Property C1.P1 As System.Int32
  Sub C1.S1()
  Sub C1.Finalize()
  Sub C1.finalize(i As System.Int32)
  Sub C1.Finalize()
  Sub C1..ctor(i As System.Int32)
  Sub C1..ctor()
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1..ctor()
  Sub C1..ctor(i As System.Int32)
  Sub C1.Finalize()
  Sub C1.finalize(i As System.Int32)
  Sub C1.Finalize()
  Property C1.P1 As System.Int32
  Sub C1.S1()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Partials() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Partial Class C1
    Partial Sub M()
    End Sub
End Class
Partial Class C1
    Sub M()
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.M()
C1
  Sub C1.M()
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.M()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__TypeParameters() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Partial Class C1(Of T1, T2)
    Partial Sub M(Of T3, T4)()
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1(Of T1, T2)
  T1
  T2
  Sub C1(Of T1, T2).M(Of T3, T4)()
    T3
    T4
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1(Of T1, T2)
  Sub C1(Of T1, T2).M(Of T3, T4)()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__NothingReturnedInsideLambdas() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Imports System
Partial Class C1
    Partial Sub M()
        Dim a As Action = Sub()
                              Dim i2 As Integer
                          End Sub
        Dim f As Func(Of Object) = Function()
                                       Dim i2 As Integer
                                       Return 1
                                   End Function
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.M()
    a As System.Action
    f As System.Func(Of System.Object)
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
  Sub C1.M()
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Delegates() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Delegate Sub D1()
Class C1
    Delegate Function D2() As Integer
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
D1
C1
  C1.D2
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
C1
D1
C1.D2
]]></Text>.Value)
        End Function
    End Class
End Namespace
