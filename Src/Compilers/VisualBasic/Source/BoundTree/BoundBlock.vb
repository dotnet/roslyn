Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class BoundBlock

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Dim childNodes = Syntax.ChildNodesAndTokens

                ' Get the first child of the parent syntax.  This should be the statement that acts as the "begin" for the block.
                Dim beginStmt = childNodes(0)

                ' Get the parent block's last child
                Dim lastInParentBlock = childNodes(childNodes.Count - 1)

                ' The bound block's span starts after the "begin" statement and goes to the end of the last statement 
                ' in the parent.  If the parent syntax has an "end" statement then the "end" statement is part of the span.
                ' Otherwise the span just includes the statements in the statement list. The "end" statement is included
                ' because in the case of "End Sub" or "End Function" an implicit return is generated in the bound block.
                ' This return is associated with the "end" statement syntax and it must be part of the bound block's span.

                If Syntax.Kind <> SyntaxKind.DoLoopBottomTestBlock Then
                    ' Don't include the condition from a bottom test do loop.
                    Return TextSpan.FromBounds(beginStmt.Span.End, lastInParentBlock.Span.End)
                End If

                Return TextSpan.FromBounds(beginStmt.Span.End, lastInParentBlock.Span.Start)
            End Get
        End Property

    End Class

End Namespace
