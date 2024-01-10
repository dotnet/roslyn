' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Suppression
    <Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
    Public Class VisualBasicSuppressionAllCodeTests
        Inherits AbstractSuppressionAllCodeTests

        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddExcludedPartTypes(GetType(IDiagnosticUpdateSourceRegistrationService)) _
            .AddParts(GetType(MockDiagnosticUpdateSourceRegistrationService))

        Protected Overrides Function CreateWorkspaceFromFile(definition As String, parseOptions As ParseOptions) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(definition, DirectCast(parseOptions, VisualBasicParseOptions), composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of Analyzer, IConfigurationFixProvider)
            Return New Tuple(Of Analyzer, IConfigurationFixProvider)(New Analyzer(), New VisualBasicSuppressionCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestPragmaWarningOnEveryNodes() As Task
            Await TestPragmaAsync(TestResource.AllInOneVisualBasicCode, VisualBasicParseOptions.Default, verifier:=Function(t) t.IndexOf("#Disable Warning", StringComparison.Ordinal) >= 0)
        End Function

        <Fact>
        Public Async Function TestSuppressionWithAttributeOnEveryNodes() As Task
            Await TestSuppressionWithAttributeAsync(
                TestResource.AllInOneVisualBasicCode,
                VisualBasicParseOptions.Default,
                digInto:=Function(n)
                             Dim member = VisualBasicSyntaxFacts.Instance.GetContainingMemberDeclaration(n, n.Span.Start)
                             If member Is Nothing OrElse member Is n Then
                                 Return True
                             End If

                             Return Not TypeOf n Is StatementSyntax
                         End Function,
                verifier:=Function(t) t.IndexOf("SuppressMessage", StringComparison.Ordinal) >= 0)
        End Function
    End Class
End Namespace
