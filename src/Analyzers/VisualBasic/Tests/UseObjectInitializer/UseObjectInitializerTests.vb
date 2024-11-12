' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer.VisualBasicUseObjectInitializerCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseObjectInitializer
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)>
    Public Class UseObjectInitializerTests
        Private Shared Async Function TestMissingInRegularAndScriptAsync(testCode As String) As Task
            Await New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = testCode
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestOnVariableDeclarator() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(5,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestOnVariableDeclarator2() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c As {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c As New C With {
            .i = 1
        }
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(5,18): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestOnAssignmentExpression() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c as C = Nothing
        c = New C With {
            .i = 1
        }
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,13): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestStopOnDuplicateMember() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1
        c.i = 2
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
        c.i = 2
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(5,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestComplexInitializer() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = {|#1:{|#0:New|} C()|}
        {|#2:array(0)|}.i = 1
        {|#3:array(0)|}.j = 2
    End Sub
End Class"
            Dim fixedCode = "
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
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(8,20): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestNotOnCompoundAssignment() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1
        c.j += 1
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1
        }
        c.j += 1
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")>
        Public Async Function TestWithExistingInitializer() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C() With {
            .i = 1
        }|}
        {|#2:c|}.j = 1
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1,
            .j = 1
        }
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")>
        Public Async Function TestWithExistingInitializerNotIfAlreadyInitialized() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C() With {
            .i = 1
        }|}
        {|#2:c|}.j = 1
        c.i = 2
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1,
            .j = 1
        }
        c.i = 2
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15012")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15012")>
        Public Async Function TestIfImplicitMemberAccessWouldNotChange() As Task
            Dim testCode = "
imports system.diagnostics

Class C
    Sub M()
        Dim x As ProcessStartInfo = {|#1:{|#0:New|} ProcessStartInfo()|}
        {|#2:x|}.Arguments = {|BC30491:Sub()
                         With New String(Nothing)
                            Dim a = .Length.ToString()
                         End With
                      End Sub()|}
    End Sub
End Class"
            Dim fixedCode = "
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
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,37): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestFixAllInDocument() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim array As C()

        array(0) = {|#1:{|#0:New|} C()|}
        {|#2:array(0)|}.i = 1
        {|#3:array(0)|}.j = 2

        array(1) = {|#5:{|#4:New|} C()|}
        {|#6:array(1)|}.i = 3
        {|#7:array(1)|}.j = 4
    End Sub
End Class"
            Dim fixedCode = "
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
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(8,20): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3))

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(12,20): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(4).WithLocation(5).WithLocation(6).WithLocation(7))

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTrivia1() As Task
            Dim testCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = {|#1:{|#0:New|} C()|}
        {|#2:c|}.i = 1 ' Goo
        {|#3:c|}.j = 2 ' Bar
    End Sub
End Class"
            Dim fixedCode = "
Class C
    Dim i As Integer
    Dim j As Integer
    Sub M()
        Dim c = New C With {
            .i = 1, ' Goo
            .j = 2 ' Bar
            }
    End Sub
End Class"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(6,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15525")>
        Public Async Function TestTrivia2() As Task
            Dim testCode = "
Class C
    Sub M(Reader as String)
        Dim XmlAppConfigReader As {|#1:{|#0:New|} XmlTextReader(Reader)|}

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        {|#2:XmlAppConfigReader|}.x = 0
        {|#3:XmlAppConfigReader|}.y = 1
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
"
            Dim fixedCode = "
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
"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(4,35): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15525")>
        Public Async Function TestTrivia3() As Task
            Dim testCode = "
Class C
    Sub M(Reader as String)
        Dim XmlAppConfigReader As {|#1:{|#0:New|} XmlTextReader(Reader)|}

        ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        {|#2:XmlAppConfigReader|}.x = 0

        ' Bar
        {|#3:XmlAppConfigReader|}.y = 1
    End Sub
End Class

class XmlTextReader
    public sub new(reader as string)
    end sub

    public x as integer
    public y as integer
end class
"
            Dim fixedCode = "
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
"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(4,35): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=401322")>
        Public Async Function TestSharedMember() As Task
            Dim testCode = "
Class C
    Dim x As Integer
    Shared y As Integer

    Sub M()
        Dim z = {|#1:{|#0:New|} C()|}
        {|#2:z|}.x = 1
        z.y = 2
    End Sub
End Class
"
            Dim fixedCode = "
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
"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(7,17): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")>
        Public Async Function TestWithExplicitImplementedInterfaceMembers3() As Task
            Dim testCode = "
class C
    Sub Bar()
        Dim c As IExample = {|#1:{|#0:New|} Goo|}
        {|#2:c|}.LastName = String.Empty
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
"
            Dim fixedCode = "
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
"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(4,29): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")>
        Public Async Function TestWithExplicitImplementedInterfaceMembers4() As Task
            Dim testCode = "
class C
    Sub Bar()
        Dim c As IExample = {|#1:{|#0:New|} Goo|}
        {|#2:c|}.LastName = String.Empty
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
"
            Dim fixedCode = "
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
"
            Dim test = New VerifyVB.Test With {
                .TestCode = testCode,
                .FixedCode = fixedCode
            }

            test.ExpectedDiagnostics.Add(
                                         _ ' /0/Test0.vb(4,29): info IDE0017: Object initialization can be simplified
                VerifyVB.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2))

            Await test.RunAsync()
        End Function
    End Class
End Namespace
