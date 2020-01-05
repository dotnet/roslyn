' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseObjectInitializer
    Public Class UseObjectInitializerTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseObjectInitializerDiagnosticAnalyzer(),
                    New VisualBasicUseObjectInitializerCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnVariableDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnVariableDeclarator2() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c As [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c As New C With {
            .i = 1
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnAssignmentExpression() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = [||]New C()
        c.i = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = New C With {
            .i = 1
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestStopOnDuplicateMember() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
        c.i = 2
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
        c.i = 2
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestComplexInitializer() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = [||]New C()
        array(0).i = 1
        array(0).j = 2
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = New C With {
            .i = 1,
            .j = 2
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestNotOnCompoundAssignment() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1
        c.j += 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
        c.j += 1
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestMissingWithExistingInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C() With {
            .i = 1
        }
        c.j = 1
    End Sub
End Class")
        End Function

        <WorkItem(15012, "https://github.com/dotnet/roslyn/issues/15012")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestMissingIfImplicitMemberAccessWouldChange() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Sub M()
        With New String()
            Dim x As ProcessStartInfo = [||]New ProcessStartInfo()
            x.Arguments = .Length.ToString()
        End With
    End Sub
End Class")
        End Function

        <WorkItem(15012, "https://github.com/dotnet/roslyn/issues/15012")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestIfImplicitMemberAccessWouldNotChange() As Task
            Await TestInRegularAndScriptAsync(
"                            
Class C
    Sub M()
        Dim x As ProcessStartInfo = [||]New ProcessStartInfo()
        x.Arguments = Sub()
                         With New String()
                            Dim a = .Length.ToString()
                         End With
                      End Sub()
    End Sub
End Class",
"                            
Class C
    Sub M()
        Dim x As ProcessStartInfo = New ProcessStartInfo With {
            .Arguments = Sub()
                             With New String()
                                 Dim a = .Length.ToString()
                             End With
                         End Sub()
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestFixAllInDocument() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = {|FixAllInDocument:New|} C()
        array(0).i = 1
        array(0).j = 2

        array(1) = New C()
        array(1).i = 3
        array(1).j = 4
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = New C With {
            .i = 1,
            .j = 2
        }

        array(1) = New C With {
            .i = 3,
            .j = 4
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [||]New C()
        c.i = 1 ' Goo
        c.j = 2 ' Bar
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1, ' Goo
            .j = 2 ' Bar
            }
    End Sub
End Class")
        End Function

        <WorkItem(15525, "https://github.com/dotnet/roslyn/issues/15525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestTrivia2() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M()
        Dim XmlAppConfigReader As [||]New XmlTextReader(Reader)

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        XmlAppConfigReader.DtdProcessing = DtdProcessing.Prohibit
        XmlAppConfigReader.WhitespaceHandling = WhitespaceHandling.All
    End Sub
End Class",
"
Class C
    Sub M()
        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        Dim XmlAppConfigReader As New XmlTextReader(Reader) With {
            .DtdProcessing = DtdProcessing.Prohibit,
            .WhitespaceHandling = WhitespaceHandling.All
        }
    End Sub
End Class")
        End Function

        <WorkItem(15525, "https://github.com/dotnet/roslyn/issues/15525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestTrivia3() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M()
        Dim XmlAppConfigReader As [||]New XmlTextReader(Reader)

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        XmlAppConfigReader.DtdProcessing = DtdProcessing.Prohibit

        ' Bar
        XmlAppConfigReader.WhitespaceHandling = WhitespaceHandling.All
    End Sub
End Class",
"
Class C
    Sub M()
        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        ' Bar
        Dim XmlAppConfigReader As New XmlTextReader(Reader) With {
            .DtdProcessing = DtdProcessing.Prohibit,
            .WhitespaceHandling = WhitespaceHandling.All
        }
    End Sub
End Class")
        End Function

        <WorkItem(401322, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=401322")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestSharedMember() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim x As Integer
    Shared y As Integer

    Sub M()
        Dim z = [||]New C()
        z.x = 1
        z.y = 2
    End Sub
End Class
",
"
Class C
    Dim x As Integer
    Shared y As Integer

    Sub M()
        Dim z = New C With {
            .x = 1
        }
        z.y = 2
    End Sub
End Class
")
        End Function

        <WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestWithExplicitImplementedInterfaceMembers1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    Sub Bar()
        Dim c As IExample = [||]New Goo
        c.Name = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property LastName As String Implements IExample.LastName
End Class
")
        End Function

        <WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestWithExplicitImplementedInterfaceMembers2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    Sub Bar()
        Dim c As IExample = [||]New Goo
        c.Name = String.Empty
        c.LastName = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property LastName As String Implements IExample.LastName
End Class
")
        End Function

        <WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestWithExplicitImplementedInterfaceMembers3() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    Sub Bar()
        Dim c As IExample = [||]New Goo
        c.LastName = String.Empty
        c.Name = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property LastName As String Implements IExample.LastName
End Class
",
"
class C
    Sub Bar()
        Dim c As IExample = New Goo With {
            .LastName = String.Empty
        }
        c.Name = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property LastName As String Implements IExample.LastName
End Class
")
        End Function

        <WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestWithExplicitImplementedInterfaceMembers4() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    Sub Bar()
        Dim c As IExample = [||]New Goo
        c.LastName = String.Empty
        c.Name = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property MyLastName As String Implements IExample.LastName
End Class
",
"
class C
    Sub Bar()
        Dim c As IExample = New Goo With {
            .MyLastName = String.Empty
        }
        c.Name = String.Empty
    End Sub
End Class

Interface IExample
    Property Name As String
    Property LastName As String
End Interface

Class Goo
    Implements IExample

    Private Property Name As String Implements IExample.Name
    Public Property MyLastName As String Implements IExample.LastName
End Class
")
        End Function
    End Class
End Namespace
