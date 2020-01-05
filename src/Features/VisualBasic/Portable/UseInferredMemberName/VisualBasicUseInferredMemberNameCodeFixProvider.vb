' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.UseInferredMemberName
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseInferredMemberNameCodeFixProvider
        Inherits AbstractUseInferredMemberNameCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Sub LanguageSpecificRemoveSuggestedNode(editor As SyntaxEditor, node As SyntaxNode)
            Select Case node.Kind
                Case SyntaxKind.NameColonEquals
                    editor.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia Or SyntaxRemoveOptions.AddElasticMarker)

                Case SyntaxKind.NamedFieldInitializer
                    Dim namedFieldInitializer = DirectCast(node, NamedFieldInitializerSyntax)
                    Dim inferredFieldInitializer = SyntaxFactory.InferredFieldInitializer(namedFieldInitializer.Expression).
                        WithTriviaFrom(namedFieldInitializer)
                    editor.ReplaceNode(namedFieldInitializer, inferredFieldInitializer)
            End Select
        End Sub
    End Class
End Namespace
