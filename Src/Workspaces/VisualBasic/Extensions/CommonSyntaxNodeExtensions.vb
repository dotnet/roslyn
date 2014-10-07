Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Module CommonSyntaxNodeExtensions
#If REMOVE Then
        <Extension()>
        Public Function IsKind(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Dim syntaxNode = TryCast(node, SyntaxNode)

            Return syntaxNode IsNot Nothing AndAlso
                   syntaxNode.Kind = kind
        End Function
#End If

        <Extension()>
        Public Function IsParentKind(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return node IsNot Nothing AndAlso
                   node.Parent.IsKind(kind)
        End Function

        <Extension()>
        Public Function MatchesKind(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Dim syntaxNode = TryCast(node, SyntaxNode)
            If syntaxNode Is Nothing Then
                Return False
            End If

            Return syntaxNode.Kind = kind
        End Function

        <Extension()>
        Public Function MatchesKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2
        End Function

        <Extension()>
        Public Function MatchesKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2 OrElse
                   node.Kind = kind3
        End Function

        <Extension()>
        Public Function MatchesKind(node As SyntaxNode, ParamArray kinds As SyntaxKind()) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return kinds.Contains(node.VisualBasicKind())
        End Function

        <Extension()>
        Public Function IsInConstantContext(expression As SyntaxNode) As Boolean
            If expression.GetAncestor(Of ParameterSyntax)() IsNot Nothing Then
                Return True
            End If

            ' TODO(cyrusn): Add more cases
            Return False
        End Function

        <Extension()>
        Public Function IsInStaticContext(node As SyntaxNode) As Boolean
            Dim containingType = node.GetAncestorOrThis(Of TypeBlockSyntax)()
            If containingType.IsKind(SyntaxKind.ModuleBlock) Then
                Return True
            End If

            Return node.GetAncestorsOrThis(Of StatementSyntax)().
                        SelectMany(Function(s) s.GetModifiers()).
                        Any(Function(t) t.Kind = SyntaxKind.SharedKeyword)
        End Function

        <Extension()>
        Public Function IsStatementContainerNode(node As SyntaxNode) As Boolean
            If node.IsExecutableBlock() Then
                Return True
            End If

            Dim singleLineLambdaExpression = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambdaExpression IsNot Nothing AndAlso TypeOf singleLineLambdaExpression.Body Is ExecutableStatementSyntax Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function GetStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Contract.ThrowIfNull(node)
            Contract.ThrowIfFalse(node.IsStatementContainerNode())

            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Return methodBlock.Statements
            End If

            Dim whileBlock = TryCast(node, WhileBlockSyntax)
            If whileBlock IsNot Nothing Then
                Return whileBlock.Statements
            End If

            Dim usingBlock = TryCast(node, UsingBlockSyntax)
            If usingBlock IsNot Nothing Then
                Return usingBlock.Statements
            End If

            Dim syncLockBlock = TryCast(node, SyncLockBlockSyntax)
            If syncLockBlock IsNot Nothing Then
                Return syncLockBlock.Statements
            End If

            Dim withBlock = TryCast(node, WithBlockSyntax)
            If withBlock IsNot Nothing Then
                Return withBlock.Statements
            End If

            Dim singleLineIfPart = TryCast(node, SingleLineIfPartSyntax)
            If singleLineIfPart IsNot Nothing Then
                Return singleLineIfPart.Statements
            End If

            Dim singleLineElsePart = TryCast(node, SingleLineElsePartSyntax)
            If singleLineElsePart IsNot Nothing Then
                Return singleLineElsePart.Statements
            End If

            Dim ifPart = TryCast(node, IfPartSyntax)
            If ifPart IsNot Nothing Then
                Return ifPart.Statements
            End If

            Dim elsePart = TryCast(node, ElsePartSyntax)
            If elsePart IsNot Nothing Then
                Return elsePart.Statements
            End If

            Dim tryPart = TryCast(node, TryPartSyntax)
            If tryPart IsNot Nothing Then
                Return tryPart.Statements
            End If

            Dim catchPart = TryCast(node, CatchPartSyntax)
            If catchPart IsNot Nothing Then
                Return catchPart.Statements
            End If

            Dim finallyPart = TryCast(node, FinallyPartSyntax)
            If finallyPart IsNot Nothing Then
                Return finallyPart.Statements
            End If

            Dim caseBlock = TryCast(node, CaseBlockSyntax)
            If caseBlock IsNot Nothing Then
                Return caseBlock.Statements
            End If

            Dim doLoopBlock = TryCast(node, DoLoopBlockSyntax)
            If doLoopBlock IsNot Nothing Then
                Return doLoopBlock.Statements
            End If

            Dim forBlock = TryCast(node, ForBlockSyntax)
            If forBlock IsNot Nothing Then
                Return forBlock.Statements
            End If

            Dim singleLineLambdaExpression = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambdaExpression IsNot Nothing Then
                Return If(TypeOf singleLineLambdaExpression.Body Is StatementSyntax,
                            SyntaxFactory.List(DirectCast(singleLineLambdaExpression.Body, StatementSyntax)),
                            Nothing)
            End If

            Dim multiLineLambdaExpression = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambdaExpression IsNot Nothing Then
                Return multiLineLambdaExpression.Statements
            End If

            Return Contract.FailWithReturn(Of SyntaxList(Of StatementSyntax))("unknown statements container!")
        End Function

    End Module
End Namespace