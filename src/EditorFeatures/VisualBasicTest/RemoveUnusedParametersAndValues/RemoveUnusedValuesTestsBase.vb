' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    Public MustInherit Class RemoveUnusedValuesTestsBase
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnusedParametersAndValuesDiagnosticAnalyzer(), New VisualBasicRemoveUnusedValuesCodeFixProvider())
        End Function

        Protected MustOverride ReadOnly Property PreferNone As IDictionary(Of OptionKey, Object)
        Protected MustOverride ReadOnly Property PreferDiscard As IDictionary(Of OptionKey, Object)
        Protected MustOverride ReadOnly Property PreferUnusedLocal As IDictionary(Of OptionKey, Object)

        Protected Overloads Function TestMissingInRegularAndScriptAsync(initialMarkup As String, options As IDictionary(Of OptionKey, Object)) As Task
            Return TestMissingInRegularAndScriptAsync(initialMarkup, New TestParameters(options:=options))
        End Function
    End Class
End Namespace
