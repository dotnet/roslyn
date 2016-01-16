' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Suppression
    Public Class VisualBasicSuppressionAllCodeTests
        Inherits AbstractSuppressionAllCodeTests

        Protected Overrides Function CreateWorkspaceFromFileAsync(definition As String, parseOptions As ParseOptions) As Threading.Tasks.Task(Of TestWorkspace)
            Return TestWorkspaceFactory.CreateVisualBasicWorkspaceAsync(definition, DirectCast(parseOptions, VisualBasicParseOptions))
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of Analyzer, ISuppressionFixProvider)
            Return New Tuple(Of Analyzer, ISuppressionFixProvider)(New Analyzer(), New VisualBasicSuppressionCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
        Public Async Function TestPragmaWarningOnEveryNodes() As Threading.Tasks.Task
            Await TestPragmaAsync(TestResource.AllInOneVisualBasicCode, VisualBasicParseOptions.Default, verifier:=Function(t) t.IndexOf("#Disable Warning", StringComparison.Ordinal) >= 0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
        Public Async Function TestSuppressionWithAttributeOnEveryNodes() As Threading.Tasks.Task
            Dim facts = New VisualBasicSyntaxFactsService()

            Await TestSuppressionWithAttributeAsync(
                TestResource.AllInOneVisualBasicCode,
                VisualBasicParseOptions.Default,
                digInto:=Function(n)
                             Dim member = facts.GetContainingMemberDeclaration(n, n.Span.Start)
                             If member Is Nothing OrElse member Is n Then
                                 Return True
                             End If

                             Return Not TypeOf n Is StatementSyntax
                         End Function,
                verifier:=Function(t) t.IndexOf("SuppressMessage", StringComparison.Ordinal) >= 0)
        End Function
    End Class
End Namespace
