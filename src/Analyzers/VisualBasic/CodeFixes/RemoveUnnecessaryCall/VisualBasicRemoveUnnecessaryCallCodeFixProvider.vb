' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCall
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCall), [Shared]>
    Friend Class VisualBasicRemoveUnnecessaryCallCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCallDiagnosticId)

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            For Each diagnostic In context.Diagnostics
                RegisterCodeFix(context, VisualBasicAnalyzersResources.Remove_Call, NameOf(VisualBasicAnalyzersResources.Remove_Call), diagnostic)
            Next

            Return Task.CompletedTask
        End Function

        Protected Overrides Async Function FixAllAsync(document As Document, diagnostics As ImmutableArray(Of Diagnostic), editor As SyntaxEditor, fallbackOptions As CodeActionOptionsProvider, cancellationToken As CancellationToken) As Task
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            For Each diagnostic In diagnostics
                Dim node = DirectCast(root.FindNode(diagnostic.AdditionalLocations(0).SourceSpan), CallStatementSyntax)
                ' replace ExpressionSyntax with ExpressionStatementSyntax
                editor.ReplaceNode(node, SyntaxFactory.ExpressionStatement(node.Invocation).WithTriviaFrom(node))
            Next
        End Function
    End Class
End Namespace
