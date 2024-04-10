' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyInterpolation
    <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyInterpolation)>
    Public Class SimplifyInterpolationTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicSimplifyInterpolationDiagnosticAnalyzer(),
                    New VisualBasicSimplifyInterpolationCodeFixProvider())
        End Function

        <Fact>
        Public Async Function SubsequentUnnecessarySpansDoNotRepeatTheSmartTag() As Task
            Dim parameters = New TestParameters(retainNonFixableDiagnostics:=True, includeDiagnosticsOutsideSelection:=True)

            Using workspace = CreateWorkspaceFromOptions("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}{|Unnecessary:3|})} suffix""
    End Sub
End Class", parameters)

                Dim diagnostics = Await GetDiagnosticsWorkerAsync(workspace, parameters)

                Assert.Equal(
                    {
                        ("IDE0071", DiagnosticSeverity.Info)
                    },
                    diagnostics.Select(Function(d) (d.Descriptor.Id, d.Severity)))
            End Using
        End Function

        <Fact>
        Public Async Function ToStringWithNoParameter() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString()|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithParameter() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString(""|}g{|Unnecessary:"")|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue:g} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithEscapeSequences() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString(""|}""""d""""{|Unnecessary:"")|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue:""""d""""} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithStringConstantParameter() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As System.DateTime)
        Const someConst As String = ""some format code""
        Dim v = $""prefix {someValue[||].ToString(someConst)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithCharacterLiteralParameter() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As C)
        Dim v = $""prefix {someValue[||].ToString(""f""c)} suffix""
    End Sub

    Function ToString(_ As Object) As String
        Return ""Goobar""
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithFormatProvider() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As System.DateTime)
        Dim v = $""prefix {someValue[||].ToString(""some format code"", System.Globalization.CultureInfo.CurrentCulture)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWithIntegerLiteral() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWithIntegerLiteral() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,-3} suffix""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49712")>
        Public Async Function PadLeftWithNonLiteralConstantExpression() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Const someConstant As Integer = 1
        Dim v = $""prefix {someValue[||].PadLeft(someConstant)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWithSpaceChar() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:, "" ""c)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWithSpaceChar() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:, "" ""c)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,-3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWithNonSpaceChar() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadLeft(3, ""x""c)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWithNonSpaceChar() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadRight(3, ""x""c)} suffix""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49712")>
        Public Async Function PadRightWithNonLiteralConstantExpression() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Const someConstant As Integer = 1
        Dim v = $""prefix {someValue[||].PadRight(someConstant)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithNoParameterWhenFormattingComponentIsSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].ToString():goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithStringLiteralParameterWhenFormattingComponentIsSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].ToString(""bar""):goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithNoParameterWhenAlignmentComponentIsSpecified() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString()|},3} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithNoParameterWhenBothComponentsAreSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].ToString(),3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithStringLiteralParameterWhenBothComponentsAreSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].ToString(""some format code""),3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWhenFormattingComponentIsSpecified() As Task
            Await TestInRegularAndScript1Async("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadLeft(3):goo} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWhenFormattingComponentIsSpecified() As Task
            Await TestInRegularAndScript1Async("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadRight(3):goo} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,-3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWhenAlignmentComponentIsSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadLeft(3),3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWhenAlignmentComponentIsSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadRight(3),3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftWhenBothComponentsAreSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadLeft(3),3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadRightWhenBothComponentsAreSpecified() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue[||].PadRight(3),3:goo} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ToStringWithoutFormatThenPadLeft() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}3{|Unnecessary:)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue,3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftThenToStringWithoutFormat() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3)} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftThenToStringWithoutFormatWhenAlignmentComponentIsSpecified() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|},3} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3),3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftThenPadRight_WithoutAlignment() As Task
            Await TestInRegularAndScriptAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix""
    End Sub
End Class", "
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3),-3} suffix""
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function PadLeftThenPadRight_WithAlignment() As Task
            Await TestMissingAsync("
Class C
    Sub M(someValue As String)
        Dim v = $""prefix {someValue.PadLeft(3)[||].PadRight(3),3} suffix""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnImplicitToStringReceiver() As Task
            Await TestMissingAsync("
Class C
    Public Overrides Function ToString() As String
        Return ""Goobar""
    End Function

    Function GetViaInterpolation() As String
        Return $""Hello {ToString[||]()}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnImplicitToStringReceiverWithArg() As Task
            Await TestMissingAsync("
Class C
    Function ToString(arg As String) As String
        Return ""Goobar""
    End Function

    Function GetViaInterpolation() As String
        Return $""Hello {ToString[||](""g"")}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnStaticToStringReceiver() As Task
            Await TestMissingAsync("
Class C
    Shared Function ToString() As String
        Return ""Goobar""
    End Function

    Function GetViaInterpolation() As String
        Return $""Hello {ToString[||]()}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnStaticToStringReceiverWithArg() As Task
            Await TestMissingAsync("
Class C
    Shared Function ToString(arg As String) As String
        Return ""Goobar""
    End Function

    Function GetViaInterpolation() As String
        Return $""Hello {ToString[||](""g"")}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnImplicitPadLeft() As Task
            Await TestMissingAsync("
Class C
    Function PadLeft(ByVal val As Integer) As String
        Return """"
    End Function

    Sub M(someValue As String)
        Dim v = $""prefix {[||]PadLeft(3)} suffix""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")>
        Public Async Function MissingOnStaticPadLeft() As Task
            Await TestMissingAsync("
Class C
    Shared Function PadLeft(ByVal val As Integer) As String
        Return """"
    End Function

    Sub M(someValue As String)
        Dim v = $""prefix {[||]PadLeft(3)} suffix""
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")>
        Public Async Function MissingOnBaseToString() As Task
            Await TestMissingAsync(
"Class C
    Public Overrides Function ToString() As String
        Return $""Test: {MyBase[||].ToString()}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")>
        Public Async Function MissingOnBaseToStringEvenWhenNotOverridden() As Task
            Await TestMissingAsync(
"Class C
    Function M() As String
        Return $""Test: {MyBase[||].ToString()}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")>
        Public Async Function MissingOnBaseToStringWithArgument() As Task
            Await TestMissingAsync(
"Class Base
    Public Function ToString(format As String) As String
        Return format
    End Function
End Class

Class Derived
    Inherits Base

    Public Overrides Function ToString() As String
        Return $""Test: {MyBase[||].ToString(""a"")}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")>
        Public Async Function PadLeftSimplificationIsStillOfferedOnBaseToString() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Public Overrides Function ToString() As String
        Return $""Test: {MyBase.ToString()[||].PadLeft(10)}""
    End Function
End Class",
"Class C
    Public Overrides Function ToString() As String
        Return $""Test: {MyBase.ToString(),10}""
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")>
        Public Async Function FormatComponentSimplificationIsNotOfferedOnNonIFormattableType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Function M(value As TypeNotImplementingIFormattable) As String
        Return $""Test: {value[||].ToString(""a"")}""
    End Function
End Class

Structure TypeNotImplementingIFormattable
    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function
End Structure")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")>
        Public Async Function FormatComponentSimplificationIsOfferedIFormattableType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System

Class C
    Function M(value As TypeImplementingIFormattable) As String
        Return $""Test: {value[||].ToString(""a"")}""
    End Function
End Class

Structure TypeImplementingIFormattable
    Implements IFormattable

    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function

    Private Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
        Return ""B""
    End Function
End Structure",
"Imports System

Class C
    Function M(value As TypeImplementingIFormattable) As String
        Return $""Test: {value:a}""
    End Function
End Class

Structure TypeImplementingIFormattable
    Implements IFormattable

    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function

    Private Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
        Return ""B""
    End Function
End Structure")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")>
        Public Async Function ParameterlessToStringSimplificationIsStillOfferedOnNonIFormattableType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(value As TypeNotImplementingIFormattable) As String
        Return $""Test: {value[||].ToString()}""
    End Function
End Class

Structure TypeNotImplementingIFormattable
    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function
End Structure",
"Class C
    Function M(value As TypeNotImplementingIFormattable) As String
        Return $""Test: {value}""
    End Function
End Class

Structure TypeNotImplementingIFormattable
    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function
End Structure")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")>
        Public Async Function PadLeftSimplificationIsStillOfferedOnNonIFormattableType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(value As TypeNotImplementingIFormattable) As String
        Return $""Test: {value.ToString(""a"")[||].PadLeft(10)}""
    End Function
End Class

Structure TypeNotImplementingIFormattable
    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function
End Structure",
"Class C
    Function M(value As TypeNotImplementingIFormattable) As String
        Return $""Test: {value.ToString(""a""),10}""
    End Function
End Class

Structure TypeNotImplementingIFormattable
    Public Overloads Function ToString(format As String) As String
        Return ""A""
    End Function
End Structure")
        End Function
    End Class
End Namespace
