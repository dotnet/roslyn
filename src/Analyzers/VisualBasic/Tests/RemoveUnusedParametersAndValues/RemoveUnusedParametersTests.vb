' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedParameters)>
    Public Class RemoveUnusedParametersTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), New VisualBasicRemoveUnusedValuesCodeFixProvider())
        End Function

        ' Ensure that we explicitly test missing UnusedParameterDiagnosticId, which has no corresponding code fix (non-fixable diagnostic).
        Private Overloads Function TestDiagnosticMissingAsync(initialMarkup As String) As Task
            Return TestDiagnosticMissingAsync(initialMarkup, New TestParameters(retainNonFixableDiagnostics:=True))
        End Function

        Private Shared Function Diagnostic(id As String) As DiagnosticDescription
            Return TestHelpers.Diagnostic(id)
        End Function

        <Fact>
        Public Async Function Parameter_Used() As Task
            Await TestDiagnosticMissingAsync(
$"Class C
    Sub M([|p|] As Integer)
        Dim x = p
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function Parameter_Unused() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Sub M([|p|] As Integer)
    End Sub
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId))
        End Function

        <Fact>
        Public Async Function Parameter_WrittenOnly() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Sub M([|p|] As Integer)
        p = 1
    End Sub
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId))
        End Function

        <Fact>
        Public Async Function Parameter_WrittenThenRead() As Task
            Await TestDiagnosticsAsync(
$"Class C
    Function M([|p|] As Integer) As Integer
        p = 1
        Return p
    End Function
End Class", parameters:=Nothing,
            Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId))
        End Function

        <Fact>
        Public Async Function ParameterOfMethodThatHandlesEvent() As Task
            Await TestDiagnosticMissingAsync(
$"Public Class C
    Public Event E(p As Integer)
    Dim WithEvents field As New C

    Public Sub M([|p|] As Integer) Handles field.E
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function Parameter_ConditionalDirective() As Task
            Await TestDiagnosticMissingAsync(
$"Public Class C
    Public Sub M([|p|] As Integer)
#If DEBUG Then
        System.Console.WriteLine(p)
#End If
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32851")>
        Public Async Function Parameter_Unused_SpecialNames() As Task
            Await TestDiagnosticMissingAsync(
$"Class C
    [|Sub M(_0 As Integer, _1 As Char, _3 As C)|]
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36816")>
        Public Async Function PartialMethodParameter_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Class C
    [|Partial Private Sub M(str As String)|]
    End Sub
End Class

Partial Class C
    Private Sub M(str As String)
        Dim x = str.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37988")>
        Public Async Function XmlLiteral_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Public Class C
    Sub M([|param|] As System.Xml.Linq.XElement)
        Dim a = param.<Test>
        Dim b = param.@Test
        Dim c = param...<Test>
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")>
        Public Async Function NotImplementedException_NoDiagnostic1() As Task
            Await TestDiagnosticMissingAsync(
"imports system

class C
    private sub Goo([|i|] as integer)
        throw new NotImplementedException()
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")>
        Public Async Function NotImplementedException_NoDiagnostic2() As Task
            Await TestDiagnosticMissingAsync(
"imports system

class C
    private function Goo([|i|] as integer) as integer
        throw new NotImplementedException()
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")>
        Public Async Function NotImplementedException_NoDiagnostic3() As Task
            Await TestDiagnosticMissingAsync(
"imports system

class C
    public sub new([|i|] as integer)
        throw new NotImplementedException()
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")>
        Public Async Function NotImplementedException_MultipleStatements1() As Task
            Await TestDiagnosticsAsync(
"imports system

class C
    private sub Goo([|i|] as integer)
        throw new NotImplementedException()
        return
    end sub
end class", parameters:=Nothing,
    Diagnostic(IDEDiagnosticIds.UnusedParameterDiagnosticId))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41236")>
        Public Async Function NotImplementedException_MultipleStatements2() As Task
            Await TestMissingAsync(
"imports system

class C
    private sub Goo([|i|] as integer)
        if (true)
            throw new NotImplementedException()
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")>
        Public Async Function TestMethodBody_OnlyThrowStatement() As Task
            Await TestDiagnosticMissingAsync(
"imports system

class C
    private sub Goo([|i|] as integer)
        throw new Exception()
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65275")>
        Public Async Function TestConstructorBody_OnlyThrowStatement() As Task
            Await TestDiagnosticMissingAsync(
"imports system

class C
    public sub new([|i|] as integer)
        throw new Exception()
    end sub
end class")
        End Function
    End Class
End Namespace
