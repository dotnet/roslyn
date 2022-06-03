' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseObjectInitializer
    Public Class UseObjectInitializerTests
        Private Shared Async Function TestInRegularAndScriptAsync(testCode As String, fixedCode As String) As Task
            Await New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }.RunAsync()
        End Function

        Private Shared Async Function TestMissingInRegularAndScriptAsync(testCode As String) As Task
            Await New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = testCode
            }.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestOnVariableDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Sub M()
        Dim c = [|New|] C()
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
        Dim c As [|New|] C()
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
        c = [|New|] C()
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
        Dim c = [|New|] C()
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

        array(0) = [|New|] C()
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
        Dim c = [|New|] C()
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
        <WorkItem(39146, "https://github.com/dotnet/roslyn/issues/39146")>
        Public Async Function TestWithExistingInitializer() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [|New|] C() With {
            .i = 1
        }
        c.j = 1
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [|New|] C With {
            .i = 1,
            .j = 1
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        <WorkItem(39146, "https://github.com/dotnet/roslyn/issues/39146")>
        Public Async Function TestWithExistingInitializerNotIfAlreadyInitialized() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [|New|] C() With {
            .i = 1
        }
        c.j = 1
        c.i = 2
    End Sub
End Class",
"
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = [|New|] C With {
            .i = 1,
            .j = 1
        }
        c.i = 2
    End Sub
End Class")
        End Function

        <WorkItem(15012, "https://github.com/dotnet/roslyn/issues/15012")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestMissingIfImplicitMemberAccessWouldChange() As Task
            Await TestMissingInRegularAndScriptAsync(
"
imports system.diagnostics

Class C
    Sub M()
        With New String(Nothing)
            Dim x As ProcessStartInfo = New ProcessStartInfo()
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
imports system.diagnostics

Class C
    Sub M()
        Dim x As ProcessStartInfo = [|New|] ProcessStartInfo()
        x.Arguments = {|BC30491:Sub()
                         With New String(Nothing)
                            Dim a = .Length.ToString()
                         End With
                      End Sub()|}
    End Sub
End Class",
"
imports system.diagnostics

Class C
    Sub M()
        Dim x As ProcessStartInfo = New ProcessStartInfo With {
            .Arguments = {|BC30491:Sub()
                             With New String(Nothing)
                                 Dim a = .Length.ToString()
                             End With
                         End Sub()|}
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

        array(0) = [|New|] C()
        array(0).i = 1
        array(0).j = 2

        array(1) = [|New|] C()
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
        Dim c = [|New|] C()
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
    Sub M(Reader as String)
        Dim XmlAppConfigReader As [|New|] XmlTextReader(Reader)

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        XmlAppConfigReader.x = 0
        XmlAppConfigReader.y = 1
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
",
"
Class C
    Sub M(Reader as String)
        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        Dim XmlAppConfigReader As New XmlTextReader(Reader) With {
            .x = 0,
            .y = 1
        }
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
")
        End Function

        <WorkItem(15525, "https://github.com/dotnet/roslyn/issues/15525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
        Public Async Function TestTrivia3() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M(Reader as String)
        Dim XmlAppConfigReader As [|New|] XmlTextReader(Reader)

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        XmlAppConfigReader.x = 0

        ' Bar
        XmlAppConfigReader.y = 1
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
",
"
Class C
    Sub M(Reader as String)
        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        ' Bar
        Dim XmlAppConfigReader As New XmlTextReader(Reader) With {
            .x = 0,
            .y = 1
        }
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
")
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
        Dim z = [|New|] C()
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
        Dim c As IExample = New Goo
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
        Dim c As IExample = New Goo
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
        Dim c As IExample = [|New|] Goo
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
        Dim c As IExample = [|New|] Goo
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
