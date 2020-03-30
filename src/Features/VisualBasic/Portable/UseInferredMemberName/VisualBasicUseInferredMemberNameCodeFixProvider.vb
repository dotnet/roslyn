﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.UseInferredMemberName
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseInferredMemberNameCodeFixProvider
        Inherits AbstractUseInferredMemberNameCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
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
