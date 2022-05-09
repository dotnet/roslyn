' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveSharedFromModuleMembers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveSharedFromModuleMembers), [Shared]>
    Friend NotInheritable Class VisualBasicRemoveSharedFromModuleMembersCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        ' Methods in a Module cannot be declared '{0}'.
        Private Const BC30433 As String = NameOf(BC30433)

        ' Events in a Module cannot be declared '{0}'.
        Private Const BC30434 As String = NameOf(BC30434)

        ' Properties in a Module cannot be declared '{0}'.
        Private Const BC30503 As String = NameOf(BC30503)

        ' Variables in Modules cannot be declared '{0}'.
        Private Const BC30593 As String = NameOf(BC30593)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(
            BC30433, BC30434, BC30503, BC30593)

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            For Each diagnostic In context.Diagnostics
                Dim tokenToRemove = diagnostic.Location.FindToken(context.CancellationToken)
                If Not tokenToRemove.IsKind(SyntaxKind.SharedKeyword) Then
                    Continue For
                End If

                Dim node = diagnostic.Location.FindNode(context.CancellationToken)
                If TypeOf node IsNot FieldDeclarationSyntax AndAlso TypeOf node IsNot MethodBaseSyntax Then
                    Continue For
                End If

                context.RegisterCodeFix(
                    CodeAction.Create(
                        VBFeaturesResources.Remove_shared_keyword_from_module_member,
                        GetDocumentUpdater(context, diagnostic),
                        NameOf(VBFeaturesResources.Remove_shared_keyword_from_module_member)),
                    diagnostic)
            Next

            Return Task.CompletedTask
        End Function

        Protected Overrides Function FixAllAsync(document As Document, diagnostics As ImmutableArray(Of Diagnostic), editor As SyntaxEditor, fallbackOptions As CodeActionOptionsProvider, cancellationToken As CancellationToken) As Task
            For Each diagnostic In diagnostics
                Dim node = diagnostic.Location.FindNode(cancellationToken)
                Dim newNode = GetReplacement(document, node)
                editor.ReplaceNode(node, newNode)
            Next

            Return Task.CompletedTask
        End Function

        Private Shared Function GetReplacement(document As Document, node As SyntaxNode) As SyntaxNode
            Dim generator = SyntaxGenerator.GetGenerator(document)
            Return generator.WithModifiers(node, generator.GetModifiers(node).WithIsStatic(False))
        End Function
    End Class
End Namespace
