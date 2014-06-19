#If False Then
Imports System.Collections.Generic
Imports Roslyn.Compilers.VisualBasic.InternalSyntax

Namespace Roslyn.Compilers.VisualBasic
    Partial Public Class SyntaxAnnotation
        Implements ISyntaxAnnotation
        ''' <summary>
        ''' Add this annotation to the given syntax node, creating a new 
        ''' syntax node of the same type with the annotation on it.
        ''' </summary>
        Public Function AddAnnotationTo(Of T As SyntaxNode)(node As T) As T
            If node IsNot Nothing Then
                Return CType(node.Green.WithAdditionalAnnotations(Me).ToRed(), T)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Add this annotation to the given syntax token, creating a new 
        ''' syntax token of the same type with the annotation on it.
        ''' </summary>
        Public Function AddAnnotationTo(token As SyntaxToken) As SyntaxToken
            If token.Node IsNot Nothing Then
                Return New SyntaxToken(parent:=Nothing, node:=token.Node.WithAdditionalAnnotations(Me), position:=0, index:=0)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Add this annotation to the given syntax trivia, creating a new 
        ''' syntax trivia of the same type with the annotation on it.
        ''' </summary>
        Public Function AddAnnotationTo(trivia As SyntaxTrivia) As SyntaxTrivia
            If trivia.UnderlyingNode IsNot Nothing Then
                Return New SyntaxTrivia(token:=Nothing, node:=trivia.UnderlyingNode.WithAdditionalAnnotations(Me), position:=0, index:=0)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Finds all nodes with this annotation attached, 
        ''' that are on or under node.
        ''' </summary>
        Public Function FindAnnotatedNodesOrTokens(root As SyntaxNode) As IEnumerable(Of SyntaxNodeOrToken)
            If root IsNot Nothing Then
                Return NodeOrTokenResolver.Resolve(root, Me)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNodeOrToken)()
        End Function

        ''' <summary>
        ''' Finds all trivia with this annotation attached, 
        ''' that are on or under node.
        ''' </summary>
        Public Function FindAnnotatedTrivia(root As SyntaxNode) As IEnumerable(Of SyntaxTrivia)
            If root IsNot Nothing Then
                Return TriviaResolver.Resolve(root, Me)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia)()
        End Function

        ''' <summary>
        ''' Finds any annotations that are on node "from", and then
        ''' attaches them to node "to", returning a new node with the 
        ''' annotations attached. 
        ''' 
        ''' If no annotations are copied, just returns "to".
        ''' 
        ''' It can also be used manually to preserve annotations in a more
        ''' complex tree modification, even if the type of a node changes.
        ''' </summary>
        Public Shared Function CopyAnnotations(Of T As SyntaxNode)([from] As SyntaxNode, [to] As T) As T
            If [from] Is Nothing OrElse [to] Is Nothing Then
                Return Nothing
            End If

            Dim annotations = [from].Green.GetAnnotations()
            If annotations Is Nothing OrElse annotations.Length = 0 Then
                Return [to]
            End If

            Return CType([to].Green.WithAdditionalAnnotations(annotations).ToRed(), T)
        End Function

        ''' <summary>
        ''' Finds any annotations that are on token "from", and then
        ''' attaches them to token "to", returning a new token with the 
        ''' annotations attached. 
        ''' 
        ''' If no annotations are copied, just returns "to".
        ''' </summary>
        Public Shared Function CopyAnnotations([from] As SyntaxToken, [to] As SyntaxToken) As SyntaxToken
            If [from].Node Is Nothing OrElse [to].Node Is Nothing Then
                Return Nothing
            End If

            Dim annotations = [from].Node.GetAnnotations()
            If annotations Is Nothing OrElse annotations.Length = 0 Then
                Return [to]
            End If

            Return New SyntaxToken(parent:=Nothing, node:=[to].Node.WithAdditionalAnnotations(annotations), position:=0, index:=0)
        End Function

        ''' <summary>
        ''' Finds any annotations that are on trivia "from", and then
        ''' attaches them to trivia "to", returning a new trivia with the 
        ''' annotations attached. 
        ''' 
        ''' If no annotations are copied, just returns "to".
        ''' </summary>
        Public Shared Function CopyAnnotations([from] As SyntaxTrivia, [to] As SyntaxTrivia) As SyntaxTrivia
            If [from].UnderlyingNode Is Nothing OrElse [to].UnderlyingNode Is Nothing Then
                Return Nothing
            End If

            Dim annotations = [from].UnderlyingNode.GetAnnotations()
            If annotations Is Nothing OrElse annotations.Length = 0 Then
                Return [to]
            End If

            Return New SyntaxTrivia(token:=Nothing, node:=[to].UnderlyingNode.WithAdditionalAnnotations(annotations), position:=0, index:=0)
        End Function

        'INSTANT VB NOTE: This code snippet uses implicit typing. You will need to set 'Option Infer On' in the VB file or set 'Option Infer' at the project level.

#Region "ISyntaxAnnotation"
        Private Function ISyntaxAnnotation_AddAnnotationTo(Of T As CommonSyntaxNode)(node As T) As T Implements ISyntaxAnnotation.AddAnnotationTo
            Dim syntaxNode = TryCast(node, SyntaxNode)
            Return TryCast(Me.AddAnnotationTo(syntaxNode), T)
        End Function

        Private Function ISyntaxAnnotation_AddAnnotationTo(token As CommonSyntaxToken) As CommonSyntaxToken Implements ISyntaxAnnotation.AddAnnotationTo
            Return Me.AddAnnotationTo(CType(token, SyntaxToken))
        End Function

        Private Function ISyntaxAnnotation_AddAnnotationTo(trivia As CommonSyntaxTrivia) As CommonSyntaxTrivia Implements ISyntaxAnnotation.AddAnnotationTo
            Return Me.AddAnnotationTo(CType(trivia, SyntaxTrivia))
        End Function

        Private Function ISyntaxAnnotation_FindAnnotatedNodesOrTokens(root As CommonSyntaxNode) As IEnumerable(Of CommonSyntaxNodeOrToken) Implements ISyntaxAnnotation.FindAnnotatedNodesOrTokens
            Return Me.FindAnnotatedNodesOrTokens(CType(root, SyntaxNode)).Select(Function(i) CType(i, CommonSyntaxNodeOrToken))
        End Function

        Private Function ISyntaxAnnotation_FindAnnotatedTrivia(root As CommonSyntaxNode) As IEnumerable(Of CommonSyntaxTrivia) Implements ISyntaxAnnotation.FindAnnotatedTrivia
            Return Me.FindAnnotatedTrivia(CType(root, SyntaxNode)).Select(Function(t) CType(t, CommonSyntaxTrivia))
        End Function
#End Region

    End Class
End Namespace
#End If
