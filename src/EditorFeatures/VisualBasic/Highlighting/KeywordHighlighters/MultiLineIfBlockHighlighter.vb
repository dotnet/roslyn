' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MultiLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of MultiLineIfBlockSyntax)

        Protected Overloads Overrides Iterator Function GetHighlights(ifBlock As MultiLineIfBlockSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return

            With ifBlock.IfStatement
                ' ElseIf case
                Yield .IfKeyword.Span

                If .ThenKeyword.Kind <> SyntaxKind.None Then Yield .ThenKeyword.Span
            End With

            For Each elseIfBlock In ifBlock.ElseIfBlocks
                If cancellationToken.IsCancellationRequested Then Return
                ' 
                With elseIfBlock.ElseIfStatement
                    ' ElseIf case
                    Yield .ElseIfKeyword.Span

                    If .ThenKeyword.Kind <> SyntaxKind.None Then Yield .ThenKeyword.Span
                End With

            Next

            If ifBlock.ElseBlock IsNot Nothing Then Yield ifBlock.ElseBlock.ElseStatement.ElseKeyword.Span

            Yield ifBlock.EndIfStatement.Span

        End Function
    End Class
End Namespace
