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

        Protected Overrides Function CreateWorkspaceFromFile(definition As String, parseOptions As ParseOptions) As TestWorkspace
            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(definition, DirectCast(parseOptions, VisualBasicParseOptions))
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of Analyzer, ISuppressionFixProvider)
            Return New Tuple(Of Analyzer, ISuppressionFixProvider)(New Analyzer(), New VisualBasicSuppressionCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
        Public Sub TestPragmaWarningOnEveryNodes()
            TestPragma(TestResource.AllInOneVisualBasicCode, VisualBasicParseOptions.Default, verifier:=Function(t) t.IndexOf("#Disable Warning", StringComparison.Ordinal) >= 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
        Public Sub TestSuppressionWithAttributeOnEveryNodes()
            Dim facts = New VisualBasicSyntaxFactsService()

            TestSuppressionWithAttribute(
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
        End Sub
    End Class
End Namespace
