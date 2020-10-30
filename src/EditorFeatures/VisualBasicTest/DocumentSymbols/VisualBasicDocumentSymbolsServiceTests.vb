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

        Private Protected Overrides Function GetDocumentSymbolsService(document1 As Document) As IDocumentSymbolsService
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
ClassInternal C1
  ClassPublic C2
ClassInternal C4
ClassInternal C3
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
ClassPublic C2
ClassInternal C3
ClassInternal C4
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
ModuleInternal M1
  ModulePublic M2
ModuleInternal M4
ModuleInternal M3
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ModuleInternal M1
ModulePublic M2
ModuleInternal M3
ModuleInternal M4
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
ClassInternal C1
  MethodPublic S1()
  MethodPublic F1(Object) As Object
  ClassPublic C2
    MethodPublic S2()
    MethodPublic F2(Object) As Object
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  MethodPublic F1(Object) As Object
  MethodPublic S1()
ClassPublic C2
  MethodPublic F2(Object) As Object
  MethodPublic S2()
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
ClassInternal C1
  MethodPublic S1()
    Local i1
    Local i2
    Local i3
    Local i4
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  MethodPublic S1()
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
ClassInternal C1
  FieldPrivate f1 As Integer
  ClassPublic C2
    FieldPrivate f2 As Integer
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  FieldPrivate f1 As Integer
ClassPublic C2
  FieldPrivate f2 As Integer
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
ClassInternal C1
  PropertyPrivate P1 As Integer
  PropertyPublic P2 As Object
  PropertyPublic P3 As String
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  PropertyPrivate P1 As Integer
  PropertyPublic P2 As Object
  PropertyPublic P3 As String
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
ClassInternal C1
  EventPublic E1()
  EventPublic E2 As EventHandler
  EventPublic E3 As EventHandler
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  EventPublic E1()
  EventPublic E2 As EventHandler
  EventPublic E3 As EventHandler
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
ClassInternal C1
  ConstantPrivate i1 As Integer Constant
  MethodPublic S1()
    Local i2 Constant
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  ConstantPrivate i1 As Integer Constant
  MethodPublic S1()
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
ClassInternal C1
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
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
ClassInternal C1
  PropertyPublic P1 As Integer
  MethodPublic S1()
  MethodPublic Finalize()
  MethodPublic finalize(Integer)
  MethodPublic Finalize()
  ConstructorPublic New(Integer)
  ConstructorPublic New()
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  ConstructorPublic New()
  ConstructorPublic New(Integer)
  MethodPublic Finalize()
  MethodPublic finalize(Integer)
  MethodPublic Finalize()
  PropertyPublic P1 As Integer
  MethodPublic S1()
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
ClassInternal C1
  MethodPublic M()
ClassInternal C1
  MethodPublic M()
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  MethodPublic M()
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
ClassInternal C1(Of T1, T2)
  TypeParameter T1
  TypeParameter T2
  MethodPublic M(Of T3, T4)()
    TypeParameter T3
    TypeParameter T4
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1(Of T1, T2)
  MethodPublic M(Of T3, T4)()
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
ClassInternal C1
  MethodPublic M()
    Local a
    Local f
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
  MethodPublic M()
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
DelegateInternal D1
ClassInternal C1
  DelegatePublic D2
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
ClassInternal C1
DelegateInternal D1
DelegatePublic D2
]]></Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider>
        Public Async Function TestDocumentSymbols__Obsolete() As Task
            Await AssertExpectedContent(<Text><![CDATA[
Imports System
<Obsolete>
Class C1
    <Obsolete>
    Delegate Function D1() As Integer
    <Obsolete>
    Public Dim i1 As Integer
    <Obsolete>
    Public Property I2 As Integer
    <Obsolete>
    Protected Sub M3()
    End Sub
End Class
]]></Text>.Value,
expectedHierarchicalLayout:=<Text><![CDATA[
(obsolete) ClassInternal C1
  (obsolete) DelegatePublic D1
  (obsolete) FieldPublic i1 As Integer
  (obsolete) PropertyPublic I2 As Integer
  (obsolete) MethodProtected M3()
]]></Text>.Value,
expectedNonHierarchicalLayout:=<Text><![CDATA[
(obsolete) ClassInternal C1
  (obsolete) FieldPublic i1 As Integer
  (obsolete) PropertyPublic I2 As Integer
  (obsolete) MethodProtected M3()
  (obsolete) DelegatePublic D1
]]></Text>.Value)
        End Function
    End Class
End Namespace
