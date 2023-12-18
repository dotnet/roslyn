' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    <UseExportProvider>
    Public Class VisualBasicAutomationObjectTests
        Inherits AbstractAutomationObjectTests

        Protected Overrides ReadOnly Property SkippedOptionsForOptionChangedTest As ImmutableArray(Of String) = ImmutableArray.Create(
            "ClosedFileDiagnostics", "BasicClosedFileDiagnostics")

        Protected Overrides ReadOnly Property SkippedOptionsForProperStorageTest As ImmutableArray(Of String) = ImmutableArray(Of String).Empty

        Protected Overrides Function GetNewValue(value As Object, propertyName As String) As Object
            If propertyName = "Style_RemoveUnnecessarySuppressionExclusions" Then
                Assert.IsType(Of String)(value)
                Return If(DirectCast(value, String) = "", "all", "")
            End If

            If TypeOf value Is Integer Then
                Return If(DirectCast(value, Integer) = 0, 1, 0)
            ElseIf TypeOf value Is String Then

                Dim xElement As XElement = XElement.Parse(DirectCast(value, String))
                If xElement.Attribute("DiagnosticSeverity").ToString() = "Error" Then
                    xElement.SetAttributeValue("DiagnosticSeverity", "Hidden")
                Else
                    xElement.SetAttributeValue("DiagnosticSeverity", "Error")
                End If

                Return xElement.ToString()
            ElseIf TypeOf value Is Boolean Then
                Return Not DirectCast(value, Boolean)
            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function CreateAutomationObject(workspace As TestWorkspace) As AbstractAutomationObject
            Return New AutomationObject(workspace)
        End Function

        Protected Overrides Function CreateWorkspace(Optional composition As TestComposition = Nothing) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic("", composition:=composition)
        End Function

        Protected Overrides Iterator Function CreatePageControls(optionStore As OptionStore, workspace As TestWorkspace) As IEnumerable(Of AbstractOptionPageControl)
            ' TODO: AdvancedOptionPageControl(optionStore)
            Yield New IntelliSenseOptionPageControl(optionStore)
        End Function
    End Class
End Namespace
