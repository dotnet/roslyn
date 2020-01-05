' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
