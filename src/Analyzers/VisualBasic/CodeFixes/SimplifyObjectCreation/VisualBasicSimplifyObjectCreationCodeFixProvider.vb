' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyObjectCreation
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyObjectCreation), [Shared]>
    Friend Class VisualBasicSimplifyObjectCreationCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(IDEDiagnosticIds.SimplifyObjectCreationDiagnosticId)

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            For Each diagnostic In context.Diagnostics
                RegisterCodeFix(context, VisualBasicCodeFixesResources.Simplify_object_creation, NameOf(VisualBasicCodeFixesResources.Simplify_object_creation), diagnostic)
            Next

            Return Task.CompletedTask
        End Function

        Protected Overrides Async Function FixAllAsync(document As Document, diagnostics As ImmutableArray(Of Diagnostic), editor As SyntaxEditor, cancellationToken As CancellationToken) As Task
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            For Each diagnostic In diagnostics
                Dim node = DirectCast(root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie:=True), VariableDeclaratorSyntax)
                Dim asNewClause = SyntaxFactory.AsNewClause(node.AsClause.AsKeyword, DirectCast(node.Initializer.Value, NewExpressionSyntax))
                Dim newNode = node.Update(
                    names:=node.Names,
                    asClause:=asNewClause,
                    initializer:=Nothing)
                editor.ReplaceNode(node, newNode)
            Next
        End Function
    End Class
End Namespace
