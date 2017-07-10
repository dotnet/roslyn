' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseInferredMemberNameCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId)

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            context.RegisterCodeFix(New MyCodeAction(
                Function(c) FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics)

            Return SpecializedTasks.EmptyTask
        End Function

        Protected Overrides Function FixAllAsync(document As Document, diagnostics As ImmutableArray(Of Diagnostic),
                                                 editor As SyntaxEditor, cancellationToken As CancellationToken) As Task
            Dim root = editor.OriginalRoot

            For Each diagnostic In diagnostics
                Dim node = root.FindNode(diagnostic.Location.SourceSpan)
                Select Case node.Kind
                    Case SyntaxKind.NameColonEquals
                        editor.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia)
                        Exit Select

                    Case SyntaxKind.NamedFieldInitializer
                        Dim namedFieldInitializer = DirectCast(node, NamedFieldInitializerSyntax)
                        Dim inferredFieldInitializer = SyntaxFactory.InferredFieldInitializer(namedFieldInitializer.Expression).
                            WithTriviaFrom(namedFieldInitializer)
                        editor.ReplaceNode(namedFieldInitializer, inferredFieldInitializer)
                        Exit Select
                End Select
            Next

            Return SpecializedTasks.EmptyTask
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction
            Public Sub New(createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(FeaturesResources.Use_inferred_member_name, createChangedDocument, FeaturesResources.Use_inferred_member_name)

            End Sub
        End Class
    End Class
End Namespace
