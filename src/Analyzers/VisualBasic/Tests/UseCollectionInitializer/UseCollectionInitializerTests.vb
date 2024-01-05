' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseCollectionInitializer
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)>
    Public Class UseCollectionInitializerTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseCollectionInitializerDiagnosticAnalyzer(),
                    New VisualBasicUseCollectionInitializerCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestOnVariableDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add(1)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = New List(Of Integer) From {
            1
        }
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70015")>
        Public Async Function TestNotParenthesized() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||](New List(Of Integer)())
        c.Add(1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestDoNotRemoveNonEmptyArgumentList() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)(Nothing)
        c.Add(1)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = New List(Of Integer)(Nothing) From {
            1
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOnVariableDeclarator2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c As [||]New List(Of Integer)()
        c.Add(1)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c As New List(Of Integer) From {
            1
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOnAssignmentExpression() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c as List(Of Integer) = Nothing
        c = [||]New List(Of Integer)()
        c.Add(1)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c as List(Of Integer) = Nothing
        c = New List(Of Integer) From {
            1
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnNamedArg() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add(value:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnZeroArgs() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnNoArgs() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnOmittedArg() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add(1,,2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestComplexInitializer() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim array As List(Of Integer)()

        array(0) = [||]New List(Of Integer)()
        array(0).Add(1)
        array(0).Add(2)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim array As List(Of Integer)()

        array(0) = New List(Of Integer) From {
            1,
            2
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMultipleArgs() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add(1, 2)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = New List(Of Integer) From {
            {1, 2}
        }
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")>
        Public Async Function TestWithExistingInitializer() As Task
            Await TestInRegularAndScript1Async(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer) From {
            1
        }
        c.Add(1)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer) From {
            1,
            1
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFixAllInDocument() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim array As List(Of Integer)()

        array(0) = {|FixAllInDocument:New|} List(Of Integer)()
        array(0).Add(1)
        array(0).Add(2)

        array(1) = New List(Of Integer)()
        array(1).Add(3)
        array(1).Add(4)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim array As List(Of Integer)()

        array(0) = New List(Of Integer) From {
            1,
            2
        }

        array(1) = New List(Of Integer) From {
            3,
            4
        }
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        c.Add(1) ' Goo
        c.Add(2) ' Bar
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = New List(Of Integer) From {
            1, ' Goo
            2 ' Bar
            }
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/pull/15528")>
        Public Async Function TestTrivia2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Collections.Generic
Class C
    Sub M()
        Dim c = [||]New List(Of Integer)()
        ' Goo
        c.Add(1)
        ' Bar
        c.Add(2)
    End Sub
End Class",
"
Imports System.Collections.Generic
Class C
    Sub M()
        ' Goo
        ' Bar
        Dim c = New List(Of Integer) From {
            1,
            2
        }
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/pull/23672")>
        Public Async Function TestMissingWithExplicitImplementedAddMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System.Dynamic
Imports System.Collections.Generic
Class C
    Sub M()
        Dim obj As IDictionary(Of String, Object) = [||]New ExpandoObject()
        obj.Add(""string"", ""v"")
        obj.Add(""int"", 1)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69106")>
        Public Async Function TestNotWithCollectionInitializerArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class C
    Sub M()
        Dim Data As [||]New List(Of IEnumerable(Of String))
        Data.Add({""Goo"", ""Bar"", ""Baz"", ""Buzz""})
    End Sub
End Class")
        End Function
    End Class
End Namespace
