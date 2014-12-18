' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ISyntaxFactsService), LanguageNames.VisualBasic), [Shared]>
    Class VisualBasicSyntaxFactsService
        Implements ISyntaxFactsService

        Public Function IsAwaitKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsAwaitKeyword
            Return token.VBKind = SyntaxKind.AwaitKeyword
        End Function

        Public Function IsIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsIdentifier
            Return token.VBKind = SyntaxKind.IdentifierToken
        End Function

        Public Function IsGlobalNamespaceKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsGlobalNamespaceKeyword
            Return token.VBKind = SyntaxKind.GlobalKeyword
        End Function

        Public Function IsVerbatimIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsVerbatimIdentifier
            Return False
        End Function

        Public Function IsOperator(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsOperator
            Return (IsUnaryExpressionOperatorToken(CType(token.VBKind, SyntaxKind)) AndAlso TypeOf token.Parent Is UnaryExpressionSyntax) OrElse
                   (IsBinaryExpressionOperatorToken(CType(token.VBKind, SyntaxKind)) AndAlso TypeOf token.Parent Is BinaryExpressionSyntax)
        End Function

        Public Function IsContextualKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsContextualKeyword
            Return token.IsContextualKeyword()
        End Function

        Public Function IsKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsKeyword
            Return token.IsKeyword()
        End Function

        Public Function IsPreprocessorKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPreprocessorKeyword
            Return token.IsPreprocessorKeyword()
        End Function

        Public Function IsHashToken(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsHashToken
            Return token.VBKind = SyntaxKind.HashToken
        End Function

        Public Function TryGetCorrespondingOpenBrace(token As SyntaxToken, ByRef openBrace As SyntaxToken) As Boolean Implements ISyntaxFactsService.TryGetCorrespondingOpenBrace

            If token.VBKind = SyntaxKind.CloseBraceToken Then
                Dim tuples = token.Parent.GetBraces()
                openBrace = tuples.Item1
                Return openBrace.VBKind = SyntaxKind.OpenBraceToken
            End If

            Return False
        End Function

        Public Function IsInInactiveRegion(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInInactiveRegion
            Dim vbTree = TryCast(syntaxTree, SyntaxTree)
            If vbTree Is Nothing Then
                Return False
            End If

            Return vbTree.IsInInactiveRegion(position, cancellationToken)
        End Function

        Public Function IsInNonUserCode(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInNonUserCode
            Dim vbTree = TryCast(syntaxTree, SyntaxTree)
            If vbTree Is Nothing Then
                Return False
            End If

            Return vbTree.IsInNonUserCode(position, cancellationToken)
        End Function

        Public Function IsEntirelyWithinStringOrCharLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsEntirelyWithinStringOrCharLiteral
            Dim vbTree = TryCast(syntaxTree, SyntaxTree)
            If vbTree Is Nothing Then
                Return False
            End If

            Return vbTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken)
        End Function

        Public Function IsDirective(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDirective
            Return TypeOf node Is DirectiveTriviaSyntax
        End Function

        Public Function TryGetExternalSourceInfo(node As SyntaxNode, ByRef info As ExternalSourceInfo) As Boolean Implements ISyntaxFactsService.TryGetExternalSourceInfo
            Select Case node.VBKind
                Case SyntaxKind.ExternalSourceDirectiveTrivia
                    info = New ExternalSourceInfo(CInt(DirectCast(node, ExternalSourceDirectiveTriviaSyntax).LineStart.Value), False)
                    Return True

                Case SyntaxKind.EndExternalSourceDirectiveTrivia
                    info = New ExternalSourceInfo(Nothing, True)
                    Return True
            End Select

            Return False
        End Function

        Public Function IsObjectCreationExpressionType(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectCreationExpressionType
            Return node.IsParentKind(SyntaxKind.ObjectCreationExpression) AndAlso
                DirectCast(node.Parent, ObjectCreationExpressionSyntax).Type Is node
        End Function

        Public Function IsAttributeName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttributeName
            Return node.IsParentKind(SyntaxKind.Attribute) AndAlso
                DirectCast(node.Parent, AttributeSyntax).Name Is node
        End Function

        Public Function IsRightSideOfQualifiedName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsRightSideOfQualifiedName
            Dim vbNode = TryCast(node, SimpleNameSyntax)
            Return vbNode IsNot Nothing AndAlso vbNode.IsRightSideOfQualifiedName()
        End Function

        Public Function IsMemberAccessExpressionName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMemberAccessExpressionName
            Dim vbNode = TryCast(node, SimpleNameSyntax)
            Return vbNode IsNot Nothing AndAlso vbNode.IsMemberAccessExpressionName()
        End Function

        Public Function IsConditionalMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsConditionalMemberAccessExpression
            Return TypeOf node Is ConditionalAccessExpressionSyntax
        End Function

        Public Function IsInvocationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInvocationExpression
            Return TypeOf node Is InvocationExpressionSyntax
        End Function

        Public Function IsAnonymousFunction(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAnonymousFunction
            Return TypeOf node Is LambdaExpressionSyntax
        End Function

        Public Function IsGenericName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsGenericName
            Return TypeOf node Is GenericNameSyntax
        End Function

        Public Function IsNamedParameter(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNamedParameter
            Return node.CheckParent(Of SimpleArgumentSyntax)(Function(p) p.IsNamed AndAlso p.NameColonEquals.Name Is node)
        End Function

        Public Function IsSkippedTokensTrivia(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSkippedTokensTrivia
            Return TypeOf node Is SkippedTokensTriviaSyntax
        End Function

        Public Function HasIncompleteParentMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.HasIncompleteParentMember
            Return node.IsParentKind(SyntaxKind.IncompleteMember)
        End Function

        Public Function GetIdentifierOfGenericName(genericName As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetIdentifierOfGenericName
            Dim vbGenericName = TryCast(genericName, GenericNameSyntax)
            Return If(vbGenericName IsNot Nothing, vbGenericName.Identifier, Nothing)
        End Function

        Public ReadOnly Property IsCaseSensitive As Boolean Implements ISyntaxFactsService.IsCaseSensitive
            Get
                Return False
            End Get
        End Property

        Public Function IsUsingDirectiveName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingDirectiveName
            Return node.IsParentKind(SyntaxKind.SimpleImportsClause) AndAlso
                   DirectCast(node.Parent, SimpleImportsClauseSyntax).Name Is node
        End Function

        Public Function IsForEachStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsForEachStatement
            Return TypeOf node Is ForEachStatementSyntax
        End Function

        Public Function IsLockStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLockStatement
            Return TypeOf node Is SyncLockStatementSyntax
        End Function

        Public Function IsUsingStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingStatement
            Return TypeOf node Is UsingStatementSyntax
        End Function

        Public Function IsThisConstructorInitializer(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsThisConstructorInitializer
            If TypeOf token.Parent Is IdentifierNameSyntax AndAlso token.HasMatchingText(SyntaxKind.NewKeyword) Then
                Dim memberAccess = TryCast(token.Parent.Parent, MemberAccessExpressionSyntax)
                Return memberAccess.IsThisConstructorInitializer()
            End If

            Return False
        End Function

        Public Function IsBaseConstructorInitializer(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsBaseConstructorInitializer
            If TypeOf token.Parent Is IdentifierNameSyntax AndAlso token.HasMatchingText(SyntaxKind.NewKeyword) Then
                Dim memberAccess = TryCast(token.Parent.Parent, MemberAccessExpressionSyntax)
                Return memberAccess.IsBaseConstructorInitializer()
            End If

            Return False
        End Function

        Public Function IsQueryExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsQueryExpression
            Return TypeOf node Is QueryExpressionSyntax
        End Function

        Public Function IsPredefinedType(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPredefinedType
            Dim actualType As PredefinedType = PredefinedType.None
            Return TryGetPredefinedType(token, actualType) AndAlso actualType <> PredefinedType.None
        End Function

        Public Function IsPredefinedType(token As SyntaxToken, type As PredefinedType) As Boolean Implements ISyntaxFactsService.IsPredefinedType
            Dim actualType As PredefinedType = PredefinedType.None
            Return TryGetPredefinedType(token, actualType) AndAlso actualType = type
        End Function

        Public Function TryGetPredefinedType(token As SyntaxToken, ByRef type As PredefinedType) As Boolean Implements ISyntaxFactsService.TryGetPredefinedType
            type = GetPredefinedType(token)
            Return type <> PredefinedType.None
        End Function

        Private Function GetPredefinedType(token As SyntaxToken) As PredefinedType
            Select Case token.VBKind
                Case SyntaxKind.BooleanKeyword
                    Return PredefinedType.Boolean
                Case SyntaxKind.ByteKeyword
                    Return PredefinedType.Byte
                Case SyntaxKind.SByteKeyword
                    Return PredefinedType.SByte
                Case SyntaxKind.IntegerKeyword
                    Return PredefinedType.Int32
                Case SyntaxKind.UIntegerKeyword
                    Return PredefinedType.UInt32
                Case SyntaxKind.ShortKeyword
                    Return PredefinedType.Int16
                Case SyntaxKind.UShortKeyword
                    Return PredefinedType.UInt16
                Case SyntaxKind.LongKeyword
                    Return PredefinedType.Int64
                Case SyntaxKind.ULongKeyword
                    Return PredefinedType.UInt64
                Case SyntaxKind.SingleKeyword
                    Return PredefinedType.Single
                Case SyntaxKind.DoubleKeyword
                    Return PredefinedType.Double
                Case SyntaxKind.DecimalKeyword
                    Return PredefinedType.Decimal
                Case SyntaxKind.StringKeyword
                    Return PredefinedType.String
                Case SyntaxKind.CharKeyword
                    Return PredefinedType.Char
                Case SyntaxKind.ObjectKeyword
                    Return PredefinedType.Object
                Case SyntaxKind.DateKeyword
                    Return PredefinedType.DateTime
                Case Else
                    Return PredefinedType.None
            End Select
        End Function

        Public Function IsPredefinedOperator(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPredefinedOperator
            Dim actualOp As PredefinedOperator = PredefinedOperator.None
            Return TryGetPredefinedOperator(token, actualOp) AndAlso actualOp <> PredefinedOperator.None
        End Function

        Public Function IsPredefinedOperator(token As SyntaxToken, op As PredefinedOperator) As Boolean Implements ISyntaxFactsService.IsPredefinedOperator
            Dim actualOp As PredefinedOperator = PredefinedOperator.None
            Return TryGetPredefinedOperator(token, actualOp) AndAlso actualOp = op
        End Function

        Public Function TryGetPredefinedOperator(token As SyntaxToken, ByRef op As PredefinedOperator) As Boolean Implements ISyntaxFactsService.TryGetPredefinedOperator
            op = GetPredefinedOperator(token)
            Return op <> PredefinedOperator.None
        End Function

        Private Function GetPredefinedOperator(token As SyntaxToken) As PredefinedOperator
            Select Case token.VBKind
                Case SyntaxKind.PlusToken, SyntaxKind.PlusEqualsToken
                    Return PredefinedOperator.Addition

                Case SyntaxKind.MinusToken, SyntaxKind.MinusEqualsToken
                    Return PredefinedOperator.Subtraction

                Case SyntaxKind.AndKeyword, SyntaxKind.AndAlsoKeyword
                    Return PredefinedOperator.BitwiseAnd

                Case SyntaxKind.OrKeyword, SyntaxKind.OrElseKeyword
                    Return PredefinedOperator.BitwiseOr

                Case SyntaxKind.AmpersandToken, SyntaxKind.AmpersandEqualsToken
                    Return PredefinedOperator.Concatenate

                Case SyntaxKind.SlashToken, SyntaxKind.SlashEqualsToken
                    Return PredefinedOperator.Division

                Case SyntaxKind.EqualsToken
                    Return PredefinedOperator.Equality

                Case SyntaxKind.XorKeyword
                    Return PredefinedOperator.ExclusiveOr

                Case SyntaxKind.CaretToken, SyntaxKind.CaretEqualsToken
                    Return PredefinedOperator.Exponent

                Case SyntaxKind.GreaterThanToken
                    Return PredefinedOperator.GreaterThan

                Case SyntaxKind.GreaterThanEqualsToken
                    Return PredefinedOperator.GreaterThanOrEqual

                Case SyntaxKind.LessThanGreaterThanToken
                    Return PredefinedOperator.Inequality

                Case SyntaxKind.BackslashToken, SyntaxKind.BackslashEqualsToken
                    Return PredefinedOperator.IntegerDivision

                Case SyntaxKind.LessThanLessThanToken, SyntaxKind.LessThanLessThanEqualsToken
                    Return PredefinedOperator.LeftShift

                Case SyntaxKind.LessThanToken
                    Return PredefinedOperator.LessThan

                Case SyntaxKind.LessThanEqualsToken
                    Return PredefinedOperator.LessThanOrEqual

                Case SyntaxKind.LikeKeyword
                    Return PredefinedOperator.Like

                Case SyntaxKind.NotKeyword
                    Return PredefinedOperator.Complement

                Case SyntaxKind.ModKeyword
                    Return PredefinedOperator.Modulus

                Case SyntaxKind.AsteriskToken, SyntaxKind.AsteriskEqualsToken
                    Return PredefinedOperator.Multiplication

                Case SyntaxKind.GreaterThanGreaterThanToken, SyntaxKind.GreaterThanGreaterThanEqualsToken
                    Return PredefinedOperator.RightShift

                Case Else
                    Return PredefinedOperator.None
            End Select
        End Function

        Public Function GetText(kind As Integer) As String Implements ISyntaxFactsService.GetText
            Return SyntaxFacts.GetText(CType(kind, SyntaxKind))
        End Function

        Public Function IsIdentifierPartCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierPartCharacter
            Return SyntaxFacts.IsIdentifierPartCharacter(c)
        End Function

        Public Function IsIdentifierStartCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierStartCharacter
            Return SyntaxFacts.IsIdentifierStartCharacter(c)
        End Function

        Public Function IsIdentifierEscapeCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierEscapeCharacter
            Return c = "["c OrElse c = "]"c
        End Function

        Public Function IsValidIdentifier(identifier As String) As Boolean Implements ISyntaxFactsService.IsValidIdentifier
            Dim token = SyntaxFactory.ParseToken(identifier)
            ' TODO: There is no way to get the diagnostics to see if any are actually errors?
            Return IsIdentifier(token) AndAlso Not token.ContainsDiagnostics AndAlso token.ToString().Length = identifier.Length
        End Function

        Public Function IsVerbatimIdentifier(identifier As String) As Boolean Implements ISyntaxFactsService.IsVerbatimIdentifier
            Return IsValidIdentifier(identifier) AndAlso MakeHalfWidthIdentifier(identifier.First()) = "[" AndAlso MakeHalfWidthIdentifier(identifier.Last()) = "]"
        End Function

        Public Function IsTypeCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsTypeCharacter
            Return c = "%"c OrElse
                   c = "&"c OrElse
                   c = "@"c OrElse
                   c = "!"c OrElse
                   c = "#"c OrElse
                   c = "$"c
        End Function

        Public Function IsStartOfUnicodeEscapeSequence(c As Char) As Boolean Implements ISyntaxFactsService.IsStartOfUnicodeEscapeSequence
            Return False ' VB does not support identifiers with escaped unicode characters 
        End Function

        Public Function IsLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsLiteral
            Return SyntaxFacts.IsLiteralExpressionToken(CType(token.VBKind, SyntaxKind))
        End Function

        Public Function IsStringLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsStringLiteral
            Return CType(token.VBKind, SyntaxKind) = SyntaxKind.StringLiteralToken
        End Function

        Public Function IsBindableToken(token As Microsoft.CodeAnalysis.SyntaxToken) As Boolean Implements ISyntaxFactsService.IsBindableToken
            Return Me.IsWord(token) OrElse
                Me.IsLiteral(token) OrElse
                Me.IsOperator(token)
        End Function

        Public Function IsMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMemberAccessExpression
            Return TypeOf node Is MemberAccessExpressionSyntax AndAlso
                DirectCast(node, MemberAccessExpressionSyntax).VBKind = SyntaxKind.SimpleMemberAccessExpression
        End Function

        Public Function IsPointerMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsPointerMemberAccessExpression
            Return False
        End Function

        Public Sub GetNameAndArityOfSimpleName(node As SyntaxNode, ByRef name As String, ByRef arity As Integer) Implements ISyntaxFactsService.GetNameAndArityOfSimpleName
            Dim simpleName = TryCast(node, SimpleNameSyntax)
            If simpleName IsNot Nothing Then
                name = simpleName.Identifier.ValueText
                arity = simpleName.Arity
            End If
        End Sub

        Public Function GetExpressionOfMemberAccessExpression(node As SyntaxNode) As Microsoft.CodeAnalysis.SyntaxNode Implements ISyntaxFactsService.GetExpressionOfMemberAccessExpression
            Return DirectCast(node, MemberAccessExpressionSyntax).GetExpressionOfMemberAccessExpression()
        End Function

        Public Function GetExpressionOfConditionalMemberAccessExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfConditionalMemberAccessExpression
            Return TryCast(node, ConditionalAccessExpressionSyntax)?.Expression
        End Function

        Public Function IsInNamespaceOrTypeContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInNamespaceOrTypeContext
            Return SyntaxFacts.IsInNamespaceOrTypeContext(node)
        End Function

        Public Function IsInStaticContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInStaticContext
            Return node.IsInStaticContext()
        End Function

        Public Function GetExpressionOfArgument(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxNode Implements ISyntaxFactsService.GetExpressionOfArgument
            Return TryCast(node, ArgumentSyntax).GetArgumentExpression()
        End Function

        Public Function GetRefKindOfArgument(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.RefKind Implements ISyntaxFactsService.GetRefKindOfArgument
            ' TODO(cyrusn): Consider the method this argument is passed to to determine this.
            Return RefKind.None
        End Function

        Public Function IsInConstantContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstantContext
            Return node.IsInConstantContext()
        End Function

        Public Function IsInConstructor(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstructor
            Return node.GetAncestors(Of StatementSyntax).Any(Function(s) s.VBKind = SyntaxKind.ConstructorBlock)
        End Function

        Public Function IsUnsafeContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUnsafeContext
            Return False
        End Function

        Public Function GetNameOfAttribute(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxNode Implements ISyntaxFactsService.GetNameOfAttribute
            Return DirectCast(node, AttributeSyntax).Name
        End Function

        Public Function IsAttribute(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttribute
            Return TypeOf node Is AttributeSyntax
        End Function

        Public Function IsAttributeNamedArgumentIdentifier(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttributeNamedArgumentIdentifier
            Dim identifierName = TryCast(node, IdentifierNameSyntax)
            If identifierName IsNot Nothing Then
                Dim simpleArgument = TryCast(identifierName.Parent, SimpleArgumentSyntax)
                If simpleArgument IsNot Nothing AndAlso simpleArgument.IsNamed AndAlso identifierName Is simpleArgument.NameColonEquals.Name Then
                    Return simpleArgument.Parent.IsParentKind(SyntaxKind.Attribute)
                End If
            End If

            Return False
        End Function

        Public Function GetContainingTypeDeclaration(root As SyntaxNode, position As Integer) As SyntaxNode Implements ISyntaxFactsService.GetContainingTypeDeclaration
            If root Is Nothing Then
                Throw New ArgumentNullException("root")
            End If

            If position < 0 OrElse position > root.Span.End Then
                Throw New ArgumentOutOfRangeException("position")
            End If

            Return root.
                FindToken(position).
                GetAncestors(Of SyntaxNode)().
                FirstOrDefault(Function(n) TypeOf n Is TypeBlockSyntax OrElse TypeOf n Is DelegateStatementSyntax)
        End Function

        Public Function GetContainingVariableDeclaratorOfFieldDeclaration(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetContainingVariableDeclaratorOfFieldDeclaration
            If node Is Nothing Then
                Throw New ArgumentNullException("node")
            End If

            Dim parent = node.Parent

            While node IsNot Nothing
                If node.VBKind = SyntaxKind.VariableDeclarator AndAlso node.IsParentKind(SyntaxKind.FieldDeclaration) Then
                    Return node
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Public Function FindTokenOnLeftOfPosition(node As SyntaxNode,
                                                  position As Integer,
                                                  Optional includeSkipped As Boolean = True,
                                                  Optional includeDirectives As Boolean = False,
                                                  Optional includeDocumentationComments As Boolean = False) As SyntaxToken Implements ISyntaxFactsService.FindTokenOnLeftOfPosition
            Return node.FindTokenOnLeftOfPosition(position, includeSkipped, includeDirectives, includeDocumentationComments)
        End Function

        Public Function FindTokenOnRightOfPosition(node As SyntaxNode,
                                                   position As Integer,
                                                   Optional includeSkipped As Boolean = True,
                                                   Optional includeDirectives As Boolean = False,
                                                   Optional includeDocumentationComments As Boolean = False) As SyntaxToken Implements ISyntaxFactsService.FindTokenOnRightOfPosition
            Return node.FindTokenOnRightOfPosition(position, includeSkipped, includeDirectives, includeDocumentationComments)
        End Function

        Public Function IsObjectCreationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectCreationExpression
            Return TypeOf node Is ObjectCreationExpressionSyntax
        End Function

        Public Function IsObjectInitializerNamedAssignmentIdentifier(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectInitializerNamedAssignmentIdentifier
            Dim identifier = TryCast(node, IdentifierNameSyntax)
            Return If(identifier?.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name), False)
        End Function

        Public Function IsElementAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsElementAccessExpression
            ' VB doesn't have a specialized node for element access.  Instead, it just uses an
            ' invocation expression or dictionary access expression.
            Return node.VBKind = SyntaxKind.InvocationExpression OrElse node.VBKind = SyntaxKind.DictionaryAccessExpression
        End Function

        Public Function ConvertToSingleLine(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.ConvertToSingleLine
            Return node.ConvertToSingleLine()
        End Function

        Public Function ToIdentifierToken(name As String) As SyntaxToken Implements ISyntaxFactsService.ToIdentifierToken
            Return name.ToIdentifierToken()
        End Function

        Public Function Parenthesize(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.Parenthesize
            Return DirectCast(expression, ExpressionSyntax).Parenthesize()
        End Function

        Public Function IsTypeNamedVarInVariableOrFieldDeclaration(token As SyntaxToken, parent As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeNamedVarInVariableOrFieldDeclaration
            Return False
        End Function

        Public Function IsTypeNamedDynamic(token As SyntaxToken, parent As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeNamedDynamic
            Return False
        End Function

        Public Function IsIndexerMemberCRef(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsIndexerMemberCRef
            Return False
        End Function

        Public Function GetContainingMemberDeclaration(root As SyntaxNode, position As Integer) As SyntaxNode Implements ISyntaxFactsService.GetContainingMemberDeclaration
            Contract.ThrowIfNull(root, "root")
            Contract.ThrowIfTrue(position < 0 OrElse position > root.FullSpan.End, "position")

            Dim [end] = root.FullSpan.End
            If [end] = 0 Then
                ' empty file
                Return Nothing
            End If

            ' make sure position doesn't touch end of root
            position = Math.Min(position, [end] - 1)

            Dim node = root.FindToken(position).Parent
            While node IsNot Nothing
                If TypeOf node Is MethodBlockBaseSyntax AndAlso Not TypeOf node.Parent Is PropertyBlockSyntax Then
                    Return node
                End If

                If TypeOf node Is PropertyStatementSyntax AndAlso Not TypeOf node.Parent Is PropertyBlockSyntax Then
                    Return node
                End If

                If TypeOf node Is EventStatementSyntax AndAlso Not TypeOf node.Parent Is EventBlockSyntax Then
                    Return node
                End If

                If TypeOf node Is PropertyBlockSyntax OrElse
                   TypeOf node Is TypeBlockSyntax OrElse
                   TypeOf node Is EnumBlockSyntax OrElse
                   TypeOf node Is NamespaceBlockSyntax OrElse
                   TypeOf node Is EventBlockSyntax OrElse
                   TypeOf node Is FieldDeclarationSyntax Then
                    Return node
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Public Function IsMethodLevelMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMethodLevelMember
            If TypeOf node Is MethodBlockBaseSyntax AndAlso
                Not TypeOf node.Parent Is PropertyBlockSyntax AndAlso
                Not TypeOf node.Parent Is EventBlockSyntax Then
                Return True
            End If

            If TypeOf node Is MethodBaseSyntax AndAlso Not TypeOf node.Parent Is MethodBlockBaseSyntax Then
                Return True
            End If

            If TypeOf node Is PropertyStatementSyntax AndAlso Not TypeOf node.Parent Is PropertyBlockSyntax Then
                Return True
            End If

            If TypeOf node Is EventStatementSyntax AndAlso Not TypeOf node.Parent Is EventBlockSyntax Then
                Return True
            End If

            Return TypeOf node Is EventBlockSyntax OrElse
                   TypeOf node Is PropertyBlockSyntax OrElse
                   TypeOf node Is EnumMemberDeclarationSyntax OrElse
                   TypeOf node Is FieldDeclarationSyntax
        End Function

        Public Function GetMemberBodySpanForSpeculativeBinding(node As SyntaxNode) As TextSpan Implements ISyntaxFactsService.GetMemberBodySpanForSpeculativeBinding
            Dim member = GetContainingMemberDeclaration(node, node.SpanStart)
            If member Is Nothing Then
                Return Nothing
            End If

            ' TODO: currently we only support method for now
            Dim method = TryCast(member, MethodBlockBaseSyntax)
            If method IsNot Nothing Then
                If method.Begin Is Nothing OrElse method.End Is Nothing Then
                    Return Nothing
                End If

                Return TextSpan.FromBounds(method.Begin.Span.End, method.End.SpanStart)
            End If

            Return Nothing
        End Function

        Public Function ContainsInMemberBody(node As SyntaxNode, span As TextSpan) As Boolean Implements ISyntaxFactsService.ContainsInMemberBody
            Dim method = TryCast(node, MethodBlockBaseSyntax)
            If method IsNot Nothing Then
                Return method.Statements.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan(method.Statements), span)
            End If

            Dim [event] = TryCast(node, EventBlockSyntax)
            If [event] IsNot Nothing Then
                Return [event].Accessors.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan([event].Accessors), span)
            End If

            Dim [property] = TryCast(node, PropertyBlockSyntax)
            If [property] IsNot Nothing Then
                Return [property].Accessors.Count > 0 AndAlso ContainsExclusively(GetSyntaxListSpan([property].Accessors), span)
            End If

            Dim field = TryCast(node, FieldDeclarationSyntax)
            If field IsNot Nothing Then
                Return field.Declarators.Count > 0 AndAlso ContainsExclusively(GetSeparatedSyntaxListSpan(field.Declarators), span)
            End If

            Dim [enum] = TryCast(node, EnumMemberDeclarationSyntax)
            If [enum] IsNot Nothing Then
                Return [enum].Initializer IsNot Nothing AndAlso ContainsExclusively([enum].Initializer.Span, span)
            End If

            Dim propStatement = TryCast(node, PropertyStatementSyntax)
            If propStatement IsNot Nothing Then
                Return propStatement.Initializer IsNot Nothing AndAlso ContainsExclusively(propStatement.Initializer.Span, span)
            End If

            Return False
        End Function

        Private Function ContainsExclusively(outerSpan As TextSpan, innerSpan As TextSpan) As Boolean
            If innerSpan.IsEmpty Then
                Return outerSpan.Contains(innerSpan.Start)
            End If

            Return outerSpan.Contains(innerSpan)
        End Function

        Private Function GetSyntaxListSpan(Of T As SyntaxNode)(list As SyntaxList(Of T)) As TextSpan
            Contract.Requires(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Private Function GetSeparatedSyntaxListSpan(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T)) As TextSpan
            Contract.Requires(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Public Function GetMethodLevelMembers(root As SyntaxNode) As List(Of SyntaxNode) Implements ISyntaxFactsService.GetMethodLevelMembers
            Dim list = New List(Of SyntaxNode)()
            AppendMethodLevelMembers(root, list)
            Return list
        End Function

        Public Function IsTopLevelNodeWithMembers(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTopLevelNodeWithMembers
            Return TypeOf node Is NamespaceBlockSyntax OrElse
                   TypeOf node Is TypeBlockSyntax OrElse
                   TypeOf node Is EnumBlockSyntax
        End Function

        Private Sub AppendMethodLevelMembers(node As SyntaxNode, list As List(Of SyntaxNode))
            For Each member In node.GetMembers()
                If IsTopLevelNodeWithMembers(member) Then
                    AppendMethodLevelMembers(member, list)
                    Continue For
                End If

                If IsMethodLevelMember(member) Then
                    list.Add(member)
                End If
            Next
        End Sub

        Public Function GetMethodLevelMemberId(root As SyntaxNode, node As SyntaxNode) As Integer Implements ISyntaxFactsService.GetMethodLevelMemberId
            Contract.Requires(root.SyntaxTree Is node.SyntaxTree)

            Dim currentId As Integer = Nothing
            Dim currentNode As SyntaxNode = Nothing
            Contract.ThrowIfFalse(TryGetMethodLevelMember(root, Function(n, i) n Is node, currentId, currentNode))

            Contract.ThrowIfFalse(currentId >= 0)
            CheckMemberId(root, node, currentId)

            Return currentId
        End Function

        Public Function GetMethodLevelMember(root As SyntaxNode, memberId As Integer) As SyntaxNode Implements ISyntaxFactsService.GetMethodLevelMember
            Dim currentId As Integer = Nothing
            Dim currentNode As SyntaxNode = Nothing
            Contract.ThrowIfFalse(TryGetMethodLevelMember(root, Function(n, i) i = memberId, currentId, currentNode))

            Contract.ThrowIfNull(currentNode)
            CheckMemberId(root, currentNode, memberId)

            Return currentNode
        End Function

        Private Function TryGetMethodLevelMember(node As SyntaxNode, predicate As Func(Of SyntaxNode, Integer, Boolean), ByRef currentId As Integer, ByRef currentNode As SyntaxNode) As Boolean
            For Each member In node.GetMembers()
                If TypeOf member Is NamespaceBlockSyntax OrElse
                   TypeOf member Is TypeBlockSyntax OrElse
                   TypeOf member Is EnumBlockSyntax Then
                    If TryGetMethodLevelMember(member, predicate, currentId, currentNode) Then
                        Return True
                    End If

                    Continue For
                End If

                If IsMethodLevelMember(member) Then
                    If predicate(member, currentId) Then
                        currentNode = member
                        Return True
                    End If

                    currentId = currentId + 1
                End If
            Next

            currentNode = Nothing
            Return False
        End Function

        <Conditional("DEBUG")>
        Private Sub CheckMemberId(root As SyntaxNode, node As SyntaxNode, memberId As Integer)
            Dim list = GetMethodLevelMembers(root)
            Dim index = list.IndexOf(node)
            Contract.ThrowIfFalse(index = memberId)
        End Sub

        Public Function GetBindableParent(token As SyntaxToken) As SyntaxNode Implements ISyntaxFactsService.GetBindableParent
            Dim node = token.Parent
            While node IsNot Nothing
                Dim parent = node.Parent

                ' If this node is on the left side of a member access expression, don't ascend
                ' further or we'll end up binding to something else.
                Dim memberAccess = TryCast(parent, MemberAccessExpressionSyntax)
                If memberAccess IsNot Nothing Then
                    If memberAccess.Expression Is node Then
                        Exit While
                    End If
                End If

                ' If this node is on the left side of a qualified name, don't ascend
                ' further or we'll end up binding to something else.
                Dim qualifiedName = TryCast(parent, QualifiedNameSyntax)
                If qualifiedName IsNot Nothing Then
                    If qualifiedName.Left Is node Then
                        Exit While
                    End If
                End If

                ' If this node is the type of an object creation expression, return the
                ' object creation expression.
                Dim objectCreation = TryCast(parent, ObjectCreationExpressionSyntax)
                If objectCreation IsNot Nothing Then
                    If objectCreation.Type Is node Then
                        node = parent
                        Exit While
                    End If
                End If

                ' If this node is not parented by a name, we're done.
                Dim name = TryCast(parent, NameSyntax)
                If name Is Nothing Then
                    Exit While
                End If

                node = parent
            End While

            Return node
        End Function

        Public Function GetConstructors(root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode) Implements ISyntaxFactsService.GetConstructors
            Dim compilationUnit = TryCast(root, CompilationUnitSyntax)
            If compilationUnit Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
            End If

            Dim constructors = New List(Of SyntaxNode)()
            AppendConstructors(compilationUnit.Members, constructors, cancellationToken)
            Return constructors
        End Function

        Private Sub AppendConstructors(members As SyntaxList(Of StatementSyntax), constructors As List(Of SyntaxNode), cancellationToken As CancellationToken)
            For Each member As StatementSyntax In members
                cancellationToken.ThrowIfCancellationRequested()

                Dim constructor = TryCast(member, ConstructorBlockSyntax)
                If constructor IsNot Nothing Then
                    constructors.Add(constructor)
                    Continue For
                End If

                Dim [namespace] = TryCast(member, NamespaceBlockSyntax)
                If [namespace] IsNot Nothing Then
                    AppendConstructors([namespace].Members, constructors, cancellationToken)
                End If

                Dim [class] = TryCast(member, ClassBlockSyntax)
                If [class] IsNot Nothing Then
                    AppendConstructors([class].Members, constructors, cancellationToken)
                End If

                Dim [struct] = TryCast(member, StructureBlockSyntax)
                If [struct] IsNot Nothing Then
                    AppendConstructors([struct].Members, constructors, cancellationToken)
                End If
            Next
        End Sub
    End Class
End Namespace