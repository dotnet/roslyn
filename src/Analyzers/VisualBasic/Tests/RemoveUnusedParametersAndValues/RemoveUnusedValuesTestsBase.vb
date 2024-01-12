' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    Public MustInherit Class RemoveUnusedValuesTestsBase
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), New VisualBasicRemoveUnusedValuesCodeFixProvider())
        End Function

        Private Protected MustOverride ReadOnly Property PreferNone As OptionsCollection
        Private Protected MustOverride ReadOnly Property PreferDiscard As OptionsCollection
        Private Protected MustOverride ReadOnly Property PreferUnusedLocal As OptionsCollection

        Private Protected Overloads Function TestMissingInRegularAndScriptAsync(initialMarkup As String, options As OptionsCollection) As Task
            Return TestMissingInRegularAndScriptAsync(initialMarkup, New TestParameters(options:=options))
        End Function
    End Class
End Namespace
