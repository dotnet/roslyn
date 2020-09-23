' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
    Public Class AddImportTestsWithAddImportDiagnosticProvider
        Inherits AbstractAddImportTests

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                        New VisualBasicAddImportCodeFixProvider())
        End Function

        <WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
        <Fact>
        Public Async Function TestUnknownIdentifierInAttributeSyntaxWithoutTarget() As Task
            Await TestAsync(
"Class Class1
    <[|Extension|]>
End Class",
                "Imports System.Runtime.CompilerServices

Class Class1
    <Extension>
End Class", TestHost.InProcess)
        End Function

        <WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestUnknownIdentifierGenericName() As Task
            Await TestAsync(
"Class C
    Inherits Attribute
    Public Sub New(x As System.Type)
    End Sub
    <C([|List(Of Integer)|])>
End Class",
                "Imports System.Collections.Generic

Class C
    Inherits Attribute
    Public Sub New(x As System.Type)
    End Sub
    <C(List(Of Integer))>
End Class", TestHost.InProcess)
        End Function

        <WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestUnknownIdentifierAddNamespaceImport() As Task
            Await TestAsync(
"Class Class1
    <[|Tasks.Task|]>
End Class",
                "Imports System.Threading

Class Class1
    <Tasks.Task>
End Class", TestHost.InProcess)
        End Function

        <WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestUnknownAttributeInModule() As Task
            Await TestAsync(
"Module Goo
    <[|Extension|]>
End Module",
                "Imports System.Runtime.CompilerServices

Module Goo
    <Extension>
End Module", TestHost.InProcess)

            Await TestAsync(
"Module Goo
    <[|Extension()|]>
End Module",
                "Imports System.Runtime.CompilerServices

Module Goo
    <Extension()>
End Module", TestHost.InProcess)
        End Function

        <WorkItem(938296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938296")>
        <Fact>
        Public Async Function TestNullParentInNode() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic

Class MultiDictionary(Of K, V)
    Inherits Dictionary(Of K, HashSet(Of V))

    Sub M()
        Dim hs = New HashSet(Of V)([|Comparer|])
    End Sub
End Class")
        End Function

        <Theory>
        <CombinatorialData>
        <WorkItem(1744, "https://github.com/dotnet/roslyn/issues/1744")>
        Public Async Function TestImportIncompleteSub() As Task
            Await TestAsync(
"Class A
    Dim a As Action = Sub()
                          Try
                          Catch ex As [|TestException|]
 End Sub
End Class
Namespace T
    Class TestException
        Inherits Exception
    End Class
End Namespace",
                "Imports T

Class A
    Dim a As Action = Sub()
                          Try
                          Catch ex As TestException
 End Sub
End Class
Namespace T
    Class TestException
        Inherits Exception
    End Class
End Namespace", TestHost.InProcess)
        End Function

        <WorkItem(1239, "https://github.com/dotnet/roslyn/issues/1239")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestImportIncompleteSub2() As Task
            Await TestAsync(
"Imports System.Linq
Namespace X
    Class Test
    End Class
End Namespace
Class C
    Sub New()
        Dim s As Action = Sub()
                              Dim a = New [|Test|]()",
                "Imports System.Linq
Imports X

Namespace X
    Class Test
    End Class
End Namespace
Class C
    Sub New()
        Dim s As Action = Sub()
                              Dim a = New Test()", TestHost.InProcess)
        End Function

        <WorkItem(23667, "https://github.com/dotnet/roslyn/issues/23667")>
        <Fact>
        Public Async Function TestMissingDiagnosticForNameOf() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class Class1
    Sub M()
        Dim a As Action = Sub()
                            Dim x = [|NameOf|](System)
                            Dim x2
                          End Function
    End Sub
    Extension")
        End Function
    End Class
End Namespace
