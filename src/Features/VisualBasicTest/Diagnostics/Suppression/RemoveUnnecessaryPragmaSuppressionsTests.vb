' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoveUnnecessarySuppressions

    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)>
    <WorkItem("https://github.com/dotnet/roslyn/issues/44177")>
    Public NotInheritable Class RemoveUnnecessaryInlineSuppressionsTests
        Inherits AbstractUnnecessarySuppressionDiagnosticTest

        Public Sub New(logger As ITestOutputHelper)
            MyBase.New(logger)
        End Sub

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Return parameters.WithCompilationOptions(If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Friend Overrides ReadOnly Property CodeFixProvider As CodeFixProvider
            Get
                Return New RemoveUnnecessaryInlineSuppressionsCodeFixProvider()
            End Get
        End Property

        Friend Overrides ReadOnly Property SuppressionAnalyzer As AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
            Get
                Return New VisualBasicRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer()
            End Get
        End Property

        Friend Overrides ReadOnly Property OtherAnalyzers As ImmutableArray(Of DiagnosticAnalyzer)
            Get
                Return ImmutableArray.Create(Of DiagnosticAnalyzer)(New VisualBasicCompilerDiagnosticAnalyzer())
            End Get
        End Property

        <Fact>
        Public Async Function TestDoNotRemoveNecessaryPragmaSuppression() As Task
            Await TestMissingAsync($"
Imports System
Class C
    Sub Method()
[|#Disable Warning BC42024 ' Unused local
        Dim x As Integer
#Enable Warning BC42024 ' Unused local|]
    End Sub
End Class")
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestDoNotRemoveUnsupportedDiagnosticSuppression(disable As Boolean) As Task
            Dim disableOrEnable = If(disable, "Disable", "Enable")

            Dim pragmas As New StringBuilder
            For Each id In GetUnsupportedDiagnosticIds()
                pragmas.AppendLine($"#{disableOrEnable} Warning {id}")
            Next

            Await TestMissingAsync($"[|{pragmas}|]")
        End Function

        Private Function GetUnsupportedDiagnosticIds() As ImmutableArray(Of String)
            Dim errorCodes = [Enum].GetValues(GetType(ERRID))
            Dim supported = DirectCast(OtherAnalyzers(0), VisualBasicCompilerDiagnosticAnalyzer).GetSupportedErrorCodes()

            Dim builder = ArrayBuilder(Of String).GetInstance()
            For Each errorCode As Integer In errorCodes
                If Not supported.Contains(errorCode) AndAlso errorCode > 0 Then
                    builder.Add("BC" & errorCode.ToString("D5"))
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestRemoveUnnecessaryPragmaSuppression(testFixFromDisable As Boolean) As Task
            Dim disablePrefix, disableSuffix, enablePrefix, enableSuffix As String
            If testFixFromDisable Then
                disablePrefix = "[|"
                disableSuffix = "|]"
                enablePrefix = ""
                enableSuffix = ""
            Else
                disablePrefix = ""
                disableSuffix = ""
                enablePrefix = "[|"
                enableSuffix = "|]"
            End If

            Await TestInRegularAndScriptAsync($"
Imports System
Class C
    Sub Method()
{disablePrefix}#Disable Warning BC42024 ' Unused local{disableSuffix}
        Dim x As Integer
{enablePrefix}#Enable Warning BC42024 ' Unused local{enableSuffix}
        x = 1
    End Sub
End Class", $"
Imports System
Class C
    Sub Method()
        Dim x As Integer
        x = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRemoveUnnecessaryAttributeSuppression_Method() As Task
            Await TestInRegularAndScriptAsync($"
Imports System
Class C
    [|<System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")>|]
    Sub Method()
        Dim x As Integer
        x = 1
    End Sub
End Class", $"
Imports System
Class C
    Sub Method()
        Dim x As Integer
        x = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRemoveUnnecessaryAttributeSuppression_Field() As Task
            Await TestInRegularAndScriptAsync($"
Imports System
Class C
    [|<System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")>|]
    Dim f As Integer
End Class", $"
Imports System
Class C
    Dim f As Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestRemoveUnnecessaryAttributeSuppression_Property() As Task
            Await TestInRegularAndScriptAsync($"
Imports System
Class C
    [|<System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")>|]
    Public ReadOnly Property P As Integer
End Class", $"
Imports System
Class C
    Public ReadOnly Property P As Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestRemoveUnnecessaryAttributeSuppression_Event() As Task
            Await TestInRegularAndScriptAsync($"
Imports System
Class C
    [|<System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")>|]
    Public Event SampleEvent As EventHandler
End Class", $"
Imports System
Class C
    Public Event SampleEvent As EventHandler
End Class")
        End Function
    End Class
End Namespace
