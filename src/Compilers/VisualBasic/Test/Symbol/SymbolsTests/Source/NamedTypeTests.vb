' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class NamedTypeTests
        Inherits BasicTestBase

        Public Shared ReadOnly TestData As Object()() = New Object()() {
            New Object() {
"
structure C
end structure"
            },
            New Object() {
"
enum C
end enum"
            },
            New Object() {
"
interface C
end interface"
            },
            New Object() {
"
delegate sub C()"
            }
        }

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType1(type As String)

            Dim compilation = CreateCompilation($"<System.CLSCompliant(false)> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType2(type As String)

            Dim compilation = CreateCompilation($"<System.Runtime.InteropServices.TypeIdentifierAttribute> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType3(type As String)

            Dim compilation = CreateCompilation($"<System.Runtime.InteropServices.TypeIdentifier> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType4(type As String)

            Dim compilation = CreateCompilation($"
imports System.Runtime.InteropServices

<TypeIdentifierAttribute> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType5(type As String)

            Dim compilation = CreateCompilation($"
imports System.Runtime.InteropServices

<TypeIdentifier> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType6(type As String)

            Dim compilation = CreateCompilation($"
imports TI = System.Runtime.InteropServices.TypeIdentifierAttribute

<TI> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType7(type As String)

            Dim compilation = CreateCompilation($"
imports TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute

<TIAttribute> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType8(type As String)

            Dim compilation = CreateCompilation($"
imports TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute

<TI> {type}")
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType9(type As String)

            Dim compilation = CreateCompilation({
                $"
<TI> {type}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TI"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TI")))
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType10(type As String)

            Dim compilation = CreateCompilation({
                $"
<TIAttribute> {type}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TIAttribute"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TIAttribute")))
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType11(type As String)

            Dim compilation = CreateCompilation({
                $"
<TI> {type}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TIAttribute"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TIAttribute")))
            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType12(type As String)

            Dim compilation = CreateCompilation({
                $"
imports X = TIAttribute
namespace N
{{
    <X> {type}
}}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TIAttribute"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TIAttribute")))

            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamespaceSymbol)("N").GetMember(Of NamedTypeSymbol)("C")

            ' VB doesn't allow an import to reference a global alias
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType13(type As String)

            Dim compilation = CreateCompilation({
                $"
imports XAttribute = TIAttribute
namespace N
{{
    <XAttribute> {type}
}}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TIAttribute"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TIAttribute")))

            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamespaceSymbol)("N").GetMember(Of NamedTypeSymbol)("C")

            ' VB doesn't allow an import to reference a global alias
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub

        <Theory, MemberData(NameOf(TestData))>
        <WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")>
        Public Sub IsExplicitDefinitionOfNoPiaLocalType14(type As String)
            Dim compilation = CreateCompilation({
                "global imports TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute",
                $"
imports XAttribute = TIAttribute
namespace N
{{
    <X> {type}
}}"
}, options:=TestOptions.ReleaseDll.WithGlobalImports(New GlobalImport(
                SimpleImportsClause(
                    ImportAliasClause("TIAttribute"),
                    ParseName("System.Runtime.InteropServices.TypeIdentifierAttribute")), "TIAttribute")))

            Dim namedType = compilation.GlobalNamespace.GetMember(Of NamespaceSymbol)("N").GetMember(Of NamedTypeSymbol)("C")

            ' VB doesn't allow an import to reference a global alias
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType)
        End Sub
    End Class
End Namespace
