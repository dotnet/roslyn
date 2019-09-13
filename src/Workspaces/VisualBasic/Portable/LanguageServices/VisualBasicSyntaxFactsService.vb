' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Class VisualBasicSyntaxFactsService
        Inherits AbstractSyntaxFactsService
        Implements ISyntaxFactsService

        Public Shared ReadOnly Property Instance As New VisualBasicSyntaxFactsService

        Private Sub New()
        End Sub

        Public ReadOnly Property IsCaseSensitive As Boolean Implements ISyntaxFactsService.IsCaseSensitive
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property StringComparer As StringComparer Implements ISyntaxFactsService.StringComparer
            Get
                Return CaseInsensitiveComparison.Comparer
            End Get
        End Property

        Public ReadOnly Property ElasticMarker As SyntaxTrivia Implements ISyntaxFactsService.ElasticMarker
            Get
                Return SyntaxFactory.ElasticMarker
            End Get
        End Property

        Public ReadOnly Property ElasticCarriageReturnLineFeed As SyntaxTrivia Implements ISyntaxFactsService.ElasticCarriageReturnLineFeed
            Get
                Return SyntaxFactory.ElasticCarriageReturnLineFeed
            End Get
        End Property

        Public Overrides ReadOnly Property SyntaxKinds As ISyntaxKindsService = VisualBasicSyntaxKindsService.Instance Implements ISyntaxFactsService.SyntaxKinds

        Protected Overrides ReadOnly Property DocumentationCommentService As IDocumentationCommentService
            Get
                Return VisualBasicDocumentationCommentService.Instance
            End Get
        End Property

        Public Function SupportsIndexingInitializer(options As ParseOptions) As Boolean Implements ISyntaxFactsService.SupportsIndexingInitializer
            Return False
        End Function

        Public Function SupportsThrowExpression(options As ParseOptions) As Boolean Implements ISyntaxFactsService.SupportsThrowExpression
            Return False
        End Function

        Public Function ParseToken(text As String) As SyntaxToken Implements ISyntaxFactsService.ParseToken
            Return SyntaxFactory.ParseToken(text, startStatement:=True)
        End Function

        Private Function ISyntaxFactsService_IsAwaitKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsAwaitKeyword
            Return IsAwaitKeyword(token)
        End Function

        Private Function ISyntaxFactsService_IsIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsIdentifier
            Return IsIdentifier(token)
        End Function

        Private Function ISyntaxFactsService_IsGlobalNamespaceKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsGlobalNamespaceKeyword
            Return IsGlobalNamespaceKeyword(token)
        End Function

        Public Function IsVerbatimIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsVerbatimIdentifier
            Return False
        End Function

        Public Function IsOperator(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsOperator
            Return (IsUnaryExpressionOperatorToken(CType(token.Kind, SyntaxKind)) AndAlso (TypeOf token.Parent Is UnaryExpressionSyntax OrElse TypeOf token.Parent Is OperatorStatementSyntax)) OrElse
                   (IsBinaryExpressionOperatorToken(CType(token.Kind, SyntaxKind)) AndAlso (TypeOf token.Parent Is BinaryExpressionSyntax OrElse TypeOf token.Parent Is OperatorStatementSyntax))
        End Function

        Public Function IsContextualKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsContextualKeyword
            Return token.IsContextualKeyword()
        End Function

        Public Function IsReservedKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsReservedKeyword
            Return token.IsReservedKeyword()
        End Function

        Public Function IsPreprocessorKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPreprocessorKeyword
            Return token.IsPreprocessorKeyword()
        End Function

        Public Function ISyntaxFactsService_IsHashToken(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsHashToken
            Return IsHashToken(token)
        End Function

        Public Function TryGetCorrespondingOpenBrace(token As SyntaxToken, ByRef openBrace As SyntaxToken) As Boolean Implements ISyntaxFactsService.TryGetCorrespondingOpenBrace

            If token.Kind = SyntaxKind.CloseBraceToken Then
                Dim tuples = token.Parent.GetBraces()
                openBrace = tuples.openBrace
                Return openBrace.Kind = SyntaxKind.OpenBraceToken
            End If

            Return False
        End Function

        Public Function IsInInactiveRegion(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInInactiveRegion
            If syntaxTree Is Nothing Then
                Return False
            End If

            Return syntaxTree.IsInInactiveRegion(position, cancellationToken)
        End Function

        Public Function IsInNonUserCode(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInNonUserCode
            If syntaxTree Is Nothing Then
                Return False
            End If

            Return syntaxTree.IsInNonUserCode(position, cancellationToken)
        End Function

        Public Function IsEntirelyWithinStringOrCharOrNumericLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsEntirelyWithinStringOrCharOrNumericLiteral
            If syntaxTree Is Nothing Then
                Return False
            End If

            Return syntaxTree.IsEntirelyWithinStringOrCharOrNumericLiteral(position, cancellationToken)
        End Function

        Public Function IsDirective(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDirective
            Return TypeOf node Is DirectiveTriviaSyntax
        End Function

        Public Function TryGetExternalSourceInfo(node As SyntaxNode, ByRef info As ExternalSourceInfo) As Boolean Implements ISyntaxFactsService.TryGetExternalSourceInfo
            Select Case node.Kind
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

        Public Function IsNameOfMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNameOfMemberAccessExpression
            Dim vbNode = TryCast(node, SimpleNameSyntax)
            Return vbNode IsNot Nothing AndAlso vbNode.IsMemberAccessExpressionName()
        End Function

        Public Function IsConditionalAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsConditionalAccessExpression
            Return TypeOf node Is ConditionalAccessExpressionSyntax
        End Function

        Public Sub GetPartsOfConditionalAccessExpression(node As SyntaxNode, ByRef expression As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef whenNotNull As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfConditionalAccessExpression
            Dim conditionalAccess = DirectCast(node, ConditionalAccessExpressionSyntax)
            expression = conditionalAccess.Expression
            operatorToken = conditionalAccess.QuestionMarkToken
            whenNotNull = conditionalAccess.WhenNotNull
        End Sub

        Public Function IsInvocationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInvocationExpression
            Return TypeOf node Is InvocationExpressionSyntax
        End Function

        Public Function IsAnonymousFunction(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAnonymousFunction
            Return TypeOf node Is LambdaExpressionSyntax
        End Function

        Public Function IsGenericName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsGenericName
            Return TypeOf node Is GenericNameSyntax
        End Function

        Public Function IsQualifiedName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsQualifiedName
            Return node.IsKind(SyntaxKind.QualifiedName)
        End Function

        Public Function IsNamedParameter(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNamedParameter
            Return node.CheckParent(Of SimpleArgumentSyntax)(Function(p) p.IsNamed AndAlso p.NameColonEquals.Name Is node)
        End Function

        Public Function GetNameOfParameter(node As SyntaxNode) As SyntaxToken? Implements ISyntaxFactsService.GetNameOfParameter
            Return TryCast(node, ParameterSyntax)?.Identifier?.Identifier
        End Function

        Public Function GetDefaultOfParameter(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetDefaultOfParameter
            Return TryCast(node, ParameterSyntax)?.Default
        End Function

        Public Function GetParameterList(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetParameterList
            Return VisualBasicSyntaxGenerator.GetParameterList(node)
        End Function

        Public Function IsSkippedTokensTrivia(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSkippedTokensTrivia
            Return TypeOf node Is SkippedTokensTriviaSyntax
        End Function

        Public Function ISyntaxFactsService_HasIncompleteParentMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.HasIncompleteParentMember
            Return HasIncompleteParentMember(node)
        End Function

        Public Function GetIdentifierOfGenericName(genericName As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetIdentifierOfGenericName
            Dim vbGenericName = TryCast(genericName, GenericNameSyntax)
            Return If(vbGenericName IsNot Nothing, vbGenericName.Identifier, Nothing)
        End Function

        Public Function IsUsingDirectiveName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingDirectiveName
            Return node.IsParentKind(SyntaxKind.SimpleImportsClause) AndAlso
                   DirectCast(node.Parent, SimpleImportsClauseSyntax).Name Is node
        End Function

        Public Function IsForEachStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsForEachStatement
            Return TypeOf node Is ForEachStatementSyntax
        End Function

        Public Function IsDeconstructionAssignment(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDeconstructionAssignment
            Return False
        End Function

        Public Function IsDeconstructionForEachStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDeconstructionForEachStatement
            Return False
        End Function

        Public Function IsLockStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLockStatement
            Return TypeOf node Is SyncLockStatementSyntax
        End Function

        Private Function ISyntaxFactsService_IsUsingStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingStatement
            Return IsUsingStatement(node)
        End Function

        Private Function ISyntaxFactsService_IsReturnStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsReturnStatement
            Return IsReturnStatement(node)
        End Function

        Public Function IsStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsStatement
            Return TypeOf node Is StatementSyntax
        End Function

        Public Function IsExecutableStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExecutableStatement
            Return TypeOf node Is ExecutableStatementSyntax
        End Function

        Public Function IsParameter(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsParameter
            Return TypeOf node Is ParameterSyntax
        End Function

        Public Function IsVariableDeclarator(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsVariableDeclarator
            Return TypeOf node Is VariableDeclaratorSyntax
        End Function

        Public Function IsMethodBody(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMethodBody
            Return TypeOf node Is MethodBlockBaseSyntax
        End Function

        Public Function GetExpressionOfReturnStatement(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfReturnStatement
            Return TryCast(node, ReturnStatementSyntax)?.Expression
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
            Return node.Kind() = SyntaxKind.QueryExpression
        End Function

        Public Function IsQueryKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsQueryKeyword
            Select Case token.Kind()
                Case _
                    SyntaxKind.JoinKeyword,
                    SyntaxKind.IntoKeyword,
                    SyntaxKind.AggregateKeyword,
                    SyntaxKind.DistinctKeyword,
                    SyntaxKind.SkipKeyword,
                    SyntaxKind.TakeKeyword,
                    SyntaxKind.LetKeyword,
                    SyntaxKind.ByKeyword,
                    SyntaxKind.OrderKeyword,
                    SyntaxKind.WhereKeyword,
                    SyntaxKind.OnKeyword,
                    SyntaxKind.FromKeyword,
                    SyntaxKind.WhileKeyword,
                    SyntaxKind.SelectKeyword
                    Return TypeOf token.Parent Is QueryClauseSyntax
                Case SyntaxKind.GroupKeyword
                    Return (TypeOf token.Parent Is QueryClauseSyntax) OrElse (token.Parent.IsKind(SyntaxKind.GroupAggregation))
                Case SyntaxKind.EqualsKeyword
                    Return TypeOf token.Parent Is JoinConditionSyntax
                Case SyntaxKind.AscendingKeyword, SyntaxKind.DescendingKeyword
                    Return TypeOf token.Parent Is OrderingSyntax
                Case SyntaxKind.InKeyword
                    Return TypeOf token.Parent Is CollectionRangeVariableSyntax
                Case Else
                    Return False
            End Select
        End Function

        Public Function IsThrowExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsThrowExpression
            ' VB does not support throw expressions currently.
            Return False
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
            Select Case token.Kind
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
            Select Case token.Kind
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
            Select Case token.Kind()
                Case _
                        SyntaxKind.IntegerLiteralToken,
                        SyntaxKind.CharacterLiteralToken,
                        SyntaxKind.DecimalLiteralToken,
                        SyntaxKind.FloatingLiteralToken,
                        SyntaxKind.DateLiteralToken,
                        SyntaxKind.StringLiteralToken,
                        SyntaxKind.DollarSignDoubleQuoteToken,
                        SyntaxKind.DoubleQuoteToken,
                        SyntaxKind.InterpolatedStringTextToken,
                        SyntaxKind.TrueKeyword,
                        SyntaxKind.FalseKeyword,
                        SyntaxKind.NothingKeyword
                    Return True
            End Select

            Return False
        End Function

        Public Function IsStringLiteralOrInterpolatedStringLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsStringLiteralOrInterpolatedStringLiteral
            Return token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken)
        End Function

        Public Function IsNumericLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNumericLiteralExpression
            Return If(node Is Nothing, False, node.IsKind(SyntaxKind.NumericLiteralExpression))
        End Function

        Public Function IsBindableToken(token As Microsoft.CodeAnalysis.SyntaxToken) As Boolean Implements ISyntaxFactsService.IsBindableToken
            Return Me.IsWord(token) OrElse
                Me.IsLiteral(token) OrElse
                Me.IsOperator(token)
        End Function

        Public Function IsSimpleMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSimpleMemberAccessExpression
            Return TypeOf node Is MemberAccessExpressionSyntax AndAlso
                DirectCast(node, MemberAccessExpressionSyntax).Kind = SyntaxKind.SimpleMemberAccessExpression
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

        Public Function LooksGeneric(name As SyntaxNode) As Boolean Implements ISyntaxFactsService.LooksGeneric
            Return name.IsKind(SyntaxKind.GenericName)
        End Function

        Public Function GetExpressionOfMemberAccessExpression(node As SyntaxNode, Optional allowImplicitTarget As Boolean = False) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfMemberAccessExpression
            Return TryCast(node, MemberAccessExpressionSyntax)?.GetExpressionOfMemberAccessExpression(allowImplicitTarget)
        End Function

        Public Function GetTargetOfMemberBinding(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetTargetOfMemberBinding
            ' Member bindings are a C# concept.
            Return Nothing
        End Function

        Public Sub GetPartsOfElementAccessExpression(node As SyntaxNode, ByRef expression As SyntaxNode, ByRef argumentList As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfElementAccessExpression
            Dim invocation = TryCast(node, InvocationExpressionSyntax)
            If invocation IsNot Nothing Then
                expression = invocation?.Expression
                argumentList = invocation?.ArgumentList
                Return
            End If

            If node.Kind() = SyntaxKind.DictionaryAccessExpression Then
                GetPartsOfMemberAccessExpression(node, expression, argumentList)
                Return
            End If

            Return
        End Sub

        Public Function GetExpressionOfInterpolation(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfInterpolation
            Return TryCast(node, InterpolationSyntax)?.Expression
        End Function

        Public Function IsInNamespaceOrTypeContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInNamespaceOrTypeContext
            Return SyntaxFacts.IsInNamespaceOrTypeContext(node)
        End Function

        Public Function IsBaseTypeList(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsBaseTypeList
            Return TryCast(node, InheritsOrImplementsStatementSyntax) IsNot Nothing
        End Function

        Public Function IsInStaticContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInStaticContext
            Return node.IsInStaticContext()
        End Function

        Public Function GetExpressionOfArgument(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxNode Implements ISyntaxFactsService.GetExpressionOfArgument
            Return TryCast(node, ArgumentSyntax).GetArgumentExpression()
        End Function

        Public Function GetRefKindOfArgument(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.RefKind Implements ISyntaxFactsService.GetRefKindOfArgument
            ' TODO(cyrusn): Consider the method this argument is passed to, to determine this.
            Return RefKind.None
        End Function

        Public Function IsArgument(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsArgument
            Return TypeOf node Is ArgumentSyntax
        End Function

        Public Function IsSimpleArgument(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSimpleArgument
            Dim argument = TryCast(node, ArgumentSyntax)
            Return argument IsNot Nothing AndAlso Not argument.IsNamed AndAlso Not argument.IsOmitted
        End Function

        Public Function IsTypeArgumentList(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeArgumentList
            Return node.IsKind(SyntaxKind.TypeArgumentList)
        End Function

        Public Function IsTypeConstraint(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeConstraint
            Return node.IsKind(SyntaxKind.TypeConstraint)
        End Function

        Public Function IsInConstantContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstantContext
            Return node.IsInConstantContext()
        End Function

        Public Function IsInConstructor(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstructor
            Return node.GetAncestors(Of StatementSyntax).Any(Function(s) s.Kind = SyntaxKind.ConstructorBlock)
        End Function

        Public Function IsUnsafeContext(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUnsafeContext
            Return False
        End Function

        Public Function GetNameOfAttribute(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxNode Implements ISyntaxFactsService.GetNameOfAttribute
            Return DirectCast(node, AttributeSyntax).Name
        End Function

        Public Function IsParenthesizedExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsParenthesizedExpression
            Return node.Kind() = SyntaxKind.ParenthesizedExpression
        End Function

        Public Function GetExpressionOfParenthesizedExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfParenthesizedExpression
            Return DirectCast(node, ParenthesizedExpressionSyntax).Expression
        End Function

        Public Function IsAttribute(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttribute
            Return TypeOf node Is AttributeSyntax
        End Function

        Public Function IsAttributeNamedArgumentIdentifier(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttributeNamedArgumentIdentifier
            Dim identifierName = TryCast(node, IdentifierNameSyntax)
            Return identifierName.IsParentKind(SyntaxKind.NameColonEquals) AndAlso
                identifierName.Parent.IsParentKind(SyntaxKind.SimpleArgument) AndAlso
                identifierName.Parent.Parent.IsParentKind(SyntaxKind.ArgumentList) AndAlso
                identifierName.Parent.Parent.Parent.IsParentKind(SyntaxKind.Attribute)
        End Function

        Public Function GetContainingTypeDeclaration(root As SyntaxNode, position As Integer) As SyntaxNode Implements ISyntaxFactsService.GetContainingTypeDeclaration
            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            If position < 0 OrElse position > root.Span.End Then
                Throw New ArgumentOutOfRangeException(NameOf(position))
            End If

            Return root.
                FindToken(position).
                GetAncestors(Of SyntaxNode)().
                FirstOrDefault(Function(n) TypeOf n Is TypeBlockSyntax OrElse TypeOf n Is DelegateStatementSyntax)
        End Function

        Public Function GetContainingVariableDeclaratorOfFieldDeclaration(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetContainingVariableDeclaratorOfFieldDeclaration
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim parent = node.Parent

            While node IsNot Nothing
                If node.Kind = SyntaxKind.VariableDeclarator AndAlso node.IsParentKind(SyntaxKind.FieldDeclaration) Then
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
            Dim unused As SyntaxNode = Nothing
            Return IsObjectInitializerNamedAssignmentIdentifier(node, unused)
        End Function

        Public Function IsObjectInitializerNamedAssignmentIdentifier(
                node As SyntaxNode,
                ByRef initializedInstance As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectInitializerNamedAssignmentIdentifier

            Dim identifier = TryCast(node, IdentifierNameSyntax)
            If identifier?.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name) Then
                ' .parent is the NamedField.
                ' .parent.parent is the ObjectInitializer.
                ' .parent.parent.parent will be the ObjectCreationExpression.
                initializedInstance = identifier.Parent.Parent.Parent
                Return True
            End If

            Return False
        End Function

        Public Function IsNameOfSubpattern(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNameOfSubpattern
            Return False
        End Function

        Public Function IsPropertyPatternClause(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsPropertyPatternClause
            Return False
        End Function

        Public Function IsElementAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsElementAccessExpression
            ' VB doesn't have a specialized node for element access.  Instead, it just uses an
            ' invocation expression or dictionary access expression.
            Return node.Kind = SyntaxKind.InvocationExpression OrElse node.Kind = SyntaxKind.DictionaryAccessExpression
        End Function

        Public Function ToIdentifierToken(name As String) As SyntaxToken Implements ISyntaxFactsService.ToIdentifierToken
            Return name.ToIdentifierToken()
        End Function

        Public Sub GetPartsOfParenthesizedExpression(
            node As SyntaxNode, ByRef openParen As SyntaxToken, ByRef expression As SyntaxNode, ByRef closeParen As SyntaxToken) Implements ISyntaxFactsService.GetPartsOfParenthesizedExpression

            Dim parenthesizedExpression = DirectCast(node, ParenthesizedExpressionSyntax)
            openParen = parenthesizedExpression.OpenParenToken
            expression = parenthesizedExpression.Expression
            closeParen = parenthesizedExpression.CloseParenToken
        End Sub

        Public Function Parenthesize(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True, Optional addSimplifierAnnotation As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.Parenthesize
            Return DirectCast(expression, ExpressionSyntax).Parenthesize(addSimplifierAnnotation)
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

        Public Function GetContainingMemberDeclaration(root As SyntaxNode, position As Integer, Optional useFullSpan As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.GetContainingMemberDeclaration
            Contract.ThrowIfNull(root, NameOf(root))
            Contract.ThrowIfTrue(position < 0 OrElse position > root.FullSpan.End, NameOf(position))

            Dim [end] = root.FullSpan.End
            If [end] = 0 Then
                ' empty file
                Return Nothing
            End If

            ' make sure position doesn't touch end of root
            position = Math.Min(position, [end] - 1)

            Dim node = root.FindToken(position).Parent
            While node IsNot Nothing
                If useFullSpan OrElse node.Span.Contains(position) Then

                    If TypeOf node Is MethodBlockBaseSyntax AndAlso Not TypeOf node.Parent Is PropertyBlockSyntax Then
                        Return node
                    End If

                    If TypeOf node Is MethodBaseSyntax AndAlso Not TypeOf node.Parent Is MethodBlockBaseSyntax Then
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
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Public Function IsMethodLevelMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMethodLevelMember

            ' Note: Derived types of MethodBaseSyntax are expanded explicitly, since PropertyStatementSyntax and
            ' EventStatementSyntax will NOT be parented by MethodBlockBaseSyntax.  Additionally, there are things
            ' like AccessorStatementSyntax and DelegateStatementSyntax that we never want to tread as method level
            ' members.

            If TypeOf node Is MethodStatementSyntax AndAlso Not TypeOf node.Parent Is MethodBlockBaseSyntax Then
                Return True
            End If

            If TypeOf node Is SubNewStatementSyntax AndAlso Not TypeOf node.Parent Is MethodBlockBaseSyntax Then
                Return True
            End If

            If TypeOf node Is OperatorStatementSyntax AndAlso Not TypeOf node.Parent Is MethodBlockBaseSyntax Then
                Return True
            End If

            If TypeOf node Is PropertyStatementSyntax AndAlso Not TypeOf node.Parent Is PropertyBlockSyntax Then
                Return True
            End If

            If TypeOf node Is EventStatementSyntax AndAlso Not TypeOf node.Parent Is EventBlockSyntax Then
                Return True
            End If

            If TypeOf node Is DeclareStatementSyntax Then
                Return True
            End If

            Return TypeOf node Is ConstructorBlockSyntax OrElse
                   TypeOf node Is MethodBlockSyntax OrElse
                   TypeOf node Is OperatorBlockSyntax OrElse
                   TypeOf node Is EventBlockSyntax OrElse
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
                If method.BlockStatement Is Nothing OrElse method.EndBlockStatement Is Nothing Then
                    Return Nothing
                End If

                ' We don't want to include the BlockStatement or any trailing trivia up to and including its statement
                ' terminator in the span. Instead, we use the start of the first statement's leading trivia (if any) up
                ' to the start of the EndBlockStatement. If there aren't any statements in the block, we use the start
                ' of the EndBlockStatements leading trivia.

                Dim firstStatement = method.Statements.FirstOrDefault()
                Dim spanStart = If(firstStatement IsNot Nothing,
                                   firstStatement.FullSpan.Start,
                                   method.EndBlockStatement.FullSpan.Start)

                Return TextSpan.FromBounds(spanStart, method.EndBlockStatement.SpanStart)
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
            Debug.Assert(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Private Function GetSeparatedSyntaxListSpan(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T)) As TextSpan
            Debug.Assert(list.Count > 0)
            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Public Function GetMethodLevelMembers(root As SyntaxNode) As List(Of SyntaxNode) Implements ISyntaxFactsService.GetMethodLevelMembers
            Dim list = New List(Of SyntaxNode)()
            AppendMethodLevelMembers(root, list)
            Return list
        End Function

        Public Function GetMembersOfTypeDeclaration(typeDeclaration As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetMembersOfTypeDeclaration
            Return DirectCast(typeDeclaration, TypeBlockSyntax).Members
        End Function

        Public Function GetMembersOfNamespaceDeclaration(namespaceDeclaration As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetMembersOfNamespaceDeclaration
            Return DirectCast(namespaceDeclaration, NamespaceBlockSyntax).Members
        End Function

        Public Function IsTopLevelNodeWithMembers(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTopLevelNodeWithMembers
            Return TypeOf node Is NamespaceBlockSyntax OrElse
                   TypeOf node Is TypeBlockSyntax OrElse
                   TypeOf node Is EnumBlockSyntax
        End Function

        Private Const s_dotToken As String = "."

        Public Function GetDisplayName(node As SyntaxNode, options As DisplayNameOptions, Optional rootNamespace As String = Nothing) As String Implements ISyntaxFactsService.GetDisplayName
            If node Is Nothing Then
                Return String.Empty
            End If

            Dim pooled = PooledStringBuilder.GetInstance()
            Dim builder = pooled.Builder

            ' member keyword (if any)
            Dim memberDeclaration = TryCast(node, DeclarationStatementSyntax)
            If (options And DisplayNameOptions.IncludeMemberKeyword) <> 0 Then
                Dim keywordToken = memberDeclaration.GetMemberKeywordToken()
                If keywordToken <> Nothing AndAlso Not keywordToken.IsMissing Then
                    builder.Append(keywordToken.Text)
                    builder.Append(" "c)
                End If
            End If

            Dim names = ArrayBuilder(Of String).GetInstance()
            ' containing type(s)
            Dim parent = node.Parent
            While TypeOf parent Is TypeBlockSyntax
                names.Push(GetName(parent, options, containsGlobalKeyword:=False))
                parent = parent.Parent
            End While

            If (options And DisplayNameOptions.IncludeNamespaces) <> 0 Then
                ' containing namespace(s) in source (if any)
                Dim containsGlobalKeyword As Boolean = False
                While parent IsNot Nothing AndAlso parent.Kind() = SyntaxKind.NamespaceBlock
                    names.Push(GetName(parent, options, containsGlobalKeyword))
                    parent = parent.Parent
                End While
                ' root namespace (if any)
                If Not containsGlobalKeyword AndAlso Not String.IsNullOrEmpty(rootNamespace) Then
                    builder.Append(rootNamespace)
                    builder.Append(s_dotToken)
                End If
            End If

            While Not names.IsEmpty()
                Dim name = names.Pop()
                If name IsNot Nothing Then
                    builder.Append(name)
                    builder.Append(s_dotToken)
                End If
            End While
            names.Free()

            ' name (include generic type parameters)
            builder.Append(GetName(node, options, containsGlobalKeyword:=False))

            ' parameter list (if any)
            If (options And DisplayNameOptions.IncludeParameters) <> 0 Then
                builder.Append(memberDeclaration.GetParameterList())
            End If

            ' As clause (if any)
            If (options And DisplayNameOptions.IncludeType) <> 0 Then
                Dim asClause = memberDeclaration.GetAsClause()
                If asClause IsNot Nothing Then
                    builder.Append(" "c)
                    builder.Append(asClause)
                End If
            End If

            Return pooled.ToStringAndFree()
        End Function

        Private Shared Function GetName(node As SyntaxNode, options As DisplayNameOptions, ByRef containsGlobalKeyword As Boolean) As String
            Const missingTokenPlaceholder As String = "?"

            Select Case node.Kind()
                Case SyntaxKind.CompilationUnit
                    Return Nothing
                Case SyntaxKind.IdentifierName
                    Dim identifier = DirectCast(node, IdentifierNameSyntax).Identifier
                    Return If(identifier.IsMissing, missingTokenPlaceholder, identifier.Text)
                Case SyntaxKind.IncompleteMember
                    Return missingTokenPlaceholder
                Case SyntaxKind.NamespaceBlock
                    Dim nameSyntax = CType(node, NamespaceBlockSyntax).NamespaceStatement.Name
                    If nameSyntax.Kind() = SyntaxKind.GlobalName Then
                        containsGlobalKeyword = True
                        Return Nothing
                    Else
                        Return GetName(nameSyntax, options, containsGlobalKeyword)
                    End If
                Case SyntaxKind.QualifiedName
                    Dim qualified = CType(node, QualifiedNameSyntax)
                    If qualified.Left.Kind() = SyntaxKind.GlobalName Then
                        containsGlobalKeyword = True
                        Return GetName(qualified.Right, options, containsGlobalKeyword) ' don't use the Global prefix if specified
                    Else
                        Return GetName(qualified.Left, options, containsGlobalKeyword) + s_dotToken + GetName(qualified.Right, options, containsGlobalKeyword)
                    End If
            End Select

            Dim name As String = Nothing
            Dim memberDeclaration = TryCast(node, DeclarationStatementSyntax)
            If memberDeclaration IsNot Nothing Then
                Dim nameToken = memberDeclaration.GetNameToken()
                If nameToken <> Nothing Then
                    name = If(nameToken.IsMissing, missingTokenPlaceholder, nameToken.Text)
                    If (options And DisplayNameOptions.IncludeTypeParameters) <> 0 Then
                        Dim pooled = PooledStringBuilder.GetInstance()
                        Dim builder = pooled.Builder
                        builder.Append(name)
                        AppendTypeParameterList(builder, memberDeclaration.GetTypeParameterList())
                        name = pooled.ToStringAndFree()
                    End If
                End If
            End If
            Debug.Assert(name IsNot Nothing, "Unexpected node type " + node.Kind().ToString())
            Return name
        End Function

        Private Shared Sub AppendTypeParameterList(builder As StringBuilder, typeParameterList As TypeParameterListSyntax)
            If typeParameterList IsNot Nothing AndAlso typeParameterList.Parameters.Count > 0 Then
                builder.Append("(Of ")
                builder.Append(typeParameterList.Parameters(0).Identifier.Text)
                For i = 1 To typeParameterList.Parameters.Count - 1
                    builder.Append(", ")
                    builder.Append(typeParameterList.Parameters(i).Identifier.Text)
                Next
                builder.Append(")"c)
            End If
        End Sub

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
            Debug.Assert(root.SyntaxTree Is node.SyntaxTree)

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

            If Not TryGetMethodLevelMember(root, Function(n, i) i = memberId, currentId, currentNode) Then
                Return Nothing
            End If

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

                    currentId += 1
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

                ' The inside of an interpolated string is treated as its own token so we
                ' need to force navigation to the parent expression syntax.
                If TypeOf node Is InterpolatedStringTextSyntax AndAlso TypeOf parent Is InterpolatedStringExpressionSyntax Then
                    node = parent
                    Exit While
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

        Public Function GetInactiveRegionSpanAroundPosition(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As TextSpan Implements ISyntaxFactsService.GetInactiveRegionSpanAroundPosition
            Dim trivia = tree.FindTriviaToLeft(position, cancellationToken)
            If trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                Return trivia.FullSpan
            End If

            Return Nothing
        End Function

        Public Function GetNameForArgument(argument As SyntaxNode) As String Implements ISyntaxFactsService.GetNameForArgument
            If TryCast(argument, ArgumentSyntax)?.IsNamed Then
                Return DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ValueText
            End If

            Return String.Empty
        End Function

        Public Function IsLeftSideOfDot(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLeftSideOfDot
            Return TryCast(node, ExpressionSyntax).IsLeftSideOfDot()
        End Function

        Public Function GetRightSideOfDot(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetRightSideOfDot
            Return If(TryCast(node, QualifiedNameSyntax)?.Right,
                      TryCast(node, MemberAccessExpressionSyntax)?.Name)
        End Function

        Public Function IsLeftSideOfExplicitInterfaceSpecifier(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLeftSideOfExplicitInterfaceSpecifier
            Return IsLeftSideOfDot(node) AndAlso TryCast(node.Parent.Parent, ImplementsClauseSyntax) IsNot Nothing
        End Function

        Public Function IsLeftSideOfAssignment(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLeftSideOfAssignment
            Return TryCast(node, ExpressionSyntax).IsLeftSideOfAnyAssignStatement
        End Function

        Public Function IsLeftSideOfAnyAssignment(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLeftSideOfAnyAssignment
            Return TryCast(node, ExpressionSyntax).IsLeftSideOfAnyAssignStatement
        End Function

        Public Function GetRightHandSideOfAssignment(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetRightHandSideOfAssignment
            Return TryCast(node, AssignmentStatementSyntax)?.Right
        End Function

        Public Function IsInferredAnonymousObjectMemberDeclarator(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInferredAnonymousObjectMemberDeclarator
            Return node.IsKind(SyntaxKind.InferredFieldInitializer)
        End Function

        Public Function IsOperandOfIncrementExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOperandOfIncrementExpression
            Return False
        End Function

        Public Function IsOperandOfIncrementOrDecrementExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOperandOfIncrementOrDecrementExpression
            Return False
        End Function

        Public Function GetContentsOfInterpolatedString(interpolatedString As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetContentsOfInterpolatedString
            Return (TryCast(interpolatedString, InterpolatedStringExpressionSyntax)?.Contents).Value
        End Function

        Public Function IsNumericLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsNumericLiteral
            Return token.Kind = SyntaxKind.DecimalLiteralToken OrElse
                   token.Kind = SyntaxKind.FloatingLiteralToken OrElse
                   token.Kind = SyntaxKind.IntegerLiteralToken
        End Function

        Public Function IsCharacterLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsCharacterLiteral
            Return token.Kind() = SyntaxKind.CharacterLiteralToken
        End Function

        Public Overrides Function IsStringLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsStringLiteral
            Return token.IsKind(SyntaxKind.StringLiteralToken)
        End Function

        Public Overrides Function IsInterpolatedStringTextToken(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsInterpolatedStringTextToken
            Return token.IsKind(SyntaxKind.InterpolatedStringTextToken)
        End Function

        Public Function IsStringLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsStringLiteralExpression
            Return node.Kind() = SyntaxKind.StringLiteralExpression
        End Function

        Public Function IsCharacterLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsCharacterLiteralExpression
            Return node.Kind() = SyntaxKind.CharacterLiteralExpression
        End Function

        Public Function IsVerbatimStringLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsVerbatimStringLiteral
            ' VB does not have verbatim strings
            Return False
        End Function

        Public Function GetArgumentsOfInvocationExpression(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetArgumentsOfInvocationExpression
            Return GetArgumentsOfArgumentList(TryCast(node, InvocationExpressionSyntax)?.ArgumentList)
        End Function

        Public Function GetArgumentsOfObjectCreationExpression(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetArgumentsOfObjectCreationExpression
            Return GetArgumentsOfArgumentList(TryCast(node, ObjectCreationExpressionSyntax)?.ArgumentList)
        End Function

        Public Function GetArgumentsOfArgumentList(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetArgumentsOfArgumentList
            Dim arguments = TryCast(node, ArgumentListSyntax)?.Arguments
            Return If(arguments.HasValue, arguments.Value, Nothing)
        End Function

        Public Function ConvertToSingleLine(node As SyntaxNode, Optional useElasticTrivia As Boolean = False) As SyntaxNode Implements ISyntaxFactsService.ConvertToSingleLine
            Return node.ConvertToSingleLine(useElasticTrivia)
        End Function

        Public Function IsDocumentationComment(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDocumentationComment
            Return node.IsKind(SyntaxKind.DocumentationCommentTrivia)
        End Function

        Public Function IsUsingOrExternOrImport(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingOrExternOrImport
            Return node.IsKind(SyntaxKind.ImportsStatement)
        End Function

        Public Function IsGlobalAttribute(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsGlobalAttribute
            If node.IsKind(SyntaxKind.Attribute) Then
                Dim attributeNode = CType(node, AttributeSyntax)
                If attributeNode.Target IsNot Nothing Then
                    Return attributeNode.Target.AttributeModifier.IsKind(SyntaxKind.AssemblyKeyword)
                End If
            End If

            Return False
        End Function

        Public Function IsDeclaration(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDeclaration
            ' From the Visual Basic language spec:
            ' NamespaceMemberDeclaration  :=
            '    NamespaceDeclaration  |
            '    TypeDeclaration
            ' TypeDeclaration  ::=
            '    ModuleDeclaration  |
            '    NonModuleDeclaration
            ' NonModuleDeclaration  ::=
            '    EnumDeclaration  |
            '    StructureDeclaration  |
            '    InterfaceDeclaration  |
            '    ClassDeclaration  |
            '    DelegateDeclaration
            ' ClassMemberDeclaration  ::=
            '    NonModuleDeclaration  |
            '    EventMemberDeclaration  |
            '    VariableMemberDeclaration  |
            '    ConstantMemberDeclaration  |
            '    MethodMemberDeclaration  |
            '    PropertyMemberDeclaration  |
            '    ConstructorMemberDeclaration  |
            '    OperatorDeclaration
            Select Case node.Kind()
                ' Because fields declarations can define multiple symbols "Public a, b As Integer" 
                ' We want to get the VariableDeclarator node inside the field declaration to print out the symbol for the name.
                Case SyntaxKind.VariableDeclarator
                    If (node.Parent.IsKind(SyntaxKind.FieldDeclaration)) Then
                        Return True
                    End If
                    Return False

                Case SyntaxKind.NamespaceStatement,
                     SyntaxKind.NamespaceBlock,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.EnumBlock,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ClassStatement,
                     SyntaxKind.ClassBlock,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.EventStatement,
                     SyntaxKind.EventBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.FieldDeclaration,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.OperatorBlock
                    Return True
            End Select

            Return False
        End Function

        ' TypeDeclaration  ::=
        '    ModuleDeclaration  |
        '    NonModuleDeclaration
        ' NonModuleDeclaration  ::=
        '    EnumDeclaration  |
        '    StructureDeclaration  |
        '    InterfaceDeclaration  |
        '    ClassDeclaration  |
        '    DelegateDeclaration
        Public Function IsTypeDeclaration(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeDeclaration
            Select Case node.Kind()
                Case SyntaxKind.EnumBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ClassBlock,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return True
            End Select

            Return False
        End Function

        Public Sub AddFirstMissingCloseBrace(root As SyntaxNode, contextNode As SyntaxNode, ByRef newRoot As SyntaxNode, ByRef newContextNode As SyntaxNode) Implements ISyntaxFactsService.AddFirstMissingCloseBrace
            ' Nothing to be done.  VB doesn't have close braces
            newRoot = root
            newContextNode = contextNode
        End Sub

        Public Function GetObjectCreationInitializer(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetObjectCreationInitializer
            Return DirectCast(node, ObjectCreationExpressionSyntax).Initializer
        End Function

        Public Function GetObjectCreationType(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetObjectCreationType
            Return DirectCast(node, ObjectCreationExpressionSyntax).Type
        End Function

        Public Function IsSimpleAssignmentStatement(statement As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSimpleAssignmentStatement
            Return statement.IsKind(SyntaxKind.SimpleAssignmentStatement)
        End Function

        Public Sub GetPartsOfAssignmentStatement(statement As SyntaxNode, ByRef left As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef right As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfAssignmentStatement
            ' VB only has assignment statements, so this can just delegate to that helper
            GetPartsOfAssignmentExpressionOrStatement(statement, left, operatorToken, right)
        End Sub

        Public Sub GetPartsOfAssignmentExpressionOrStatement(statement As SyntaxNode, ByRef left As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef right As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfAssignmentExpressionOrStatement
            Dim assignment = DirectCast(statement, AssignmentStatementSyntax)
            left = assignment.Left
            operatorToken = assignment.OperatorToken
            right = assignment.Right
        End Sub

        Public Function GetNameOfMemberAccessExpression(memberAccessExpression As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetNameOfMemberAccessExpression
            Return DirectCast(memberAccessExpression, MemberAccessExpressionSyntax).Name
        End Function

        Public Function GetIdentifierOfSimpleName(node As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetIdentifierOfSimpleName
            Return DirectCast(node, SimpleNameSyntax).Identifier
        End Function

        Public Function GetIdentifierOfVariableDeclarator(node As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetIdentifierOfVariableDeclarator
            Return DirectCast(node, VariableDeclaratorSyntax).Names.Last().Identifier
        End Function

        Public Function IsIdentifierName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsIdentifierName
            Return node.IsKind(SyntaxKind.IdentifierName)
        End Function

        Public Function IsLocalDeclarationStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLocalDeclarationStatement
            Return node.IsKind(SyntaxKind.LocalDeclarationStatement)
        End Function

        Public Function IsLocalFunctionStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLocalFunctionStatement
            ' VB does not have local funtions
            Return False
        End Function

        Public Function IsDeclaratorOfLocalDeclarationStatement(declarator As SyntaxNode, localDeclarationStatement As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDeclaratorOfLocalDeclarationStatement
            Return DirectCast(localDeclarationStatement, LocalDeclarationStatementSyntax).Declarators.
                Contains(DirectCast(declarator, VariableDeclaratorSyntax))
        End Function

        Public Function AreEquivalent(token1 As SyntaxToken, token2 As SyntaxToken) As Boolean Implements ISyntaxFactsService.AreEquivalent
            Return SyntaxFactory.AreEquivalent(token1, token2)
        End Function

        Public Function AreEquivalent(node1 As SyntaxNode, node2 As SyntaxNode) As Boolean Implements ISyntaxFactsService.AreEquivalent
            Return SyntaxFactory.AreEquivalent(node1, node2)
        End Function

        Public Function IsExpressionOfInvocationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExpressionOfInvocationExpression
            Return node IsNot Nothing AndAlso TryCast(node.Parent, InvocationExpressionSyntax)?.Expression Is node
        End Function

        Public Function IsAwaitExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAwaitExpression
            Return node.IsKind(SyntaxKind.AwaitExpression)
        End Function

        Public Function IsExpressionOfAwaitExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExpressionOfAwaitExpression
            Return node IsNot Nothing AndAlso TryCast(node.Parent, AwaitExpressionSyntax)?.Expression Is node
        End Function

        Public Function IsExpressionOfMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExpressionOfMemberAccessExpression
            Return node IsNot Nothing AndAlso TryCast(node.Parent, MemberAccessExpressionSyntax)?.Expression Is node
        End Function

        Public Function GetExpressionOfAwaitExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfAwaitExpression
            Return DirectCast(node, AwaitExpressionSyntax).Expression
        End Function

        Public Function IsExpressionOfForeach(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExpressionOfForeach
            Return node IsNot Nothing AndAlso TryCast(node.Parent, ForEachStatementSyntax)?.Expression Is node
        End Function

        Public Function IsPossibleTupleContext(
            syntaxTree As SyntaxTree,
            position As Integer,
            cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsPossibleTupleContext

            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Return syntaxTree.IsPossibleTupleContext(token, position)
        End Function

        Public Function GetExpressionOfExpressionStatement(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfExpressionStatement
            Return DirectCast(node, ExpressionStatementSyntax).Expression
        End Function

        Public Function IsNullLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNullLiteralExpression
            Return node.Kind() = SyntaxKind.NothingLiteralExpression
        End Function

        Public Function IsDefaultLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDefaultLiteralExpression
            Return IsNullLiteralExpression(node)
        End Function

        Public Function IsBinaryExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsBinaryExpression
            Return TypeOf node Is BinaryExpressionSyntax
        End Function

        Public Sub GetPartsOfBinaryExpression(node As SyntaxNode, ByRef left As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef right As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfBinaryExpression
            Dim binaryExpression = DirectCast(node, BinaryExpressionSyntax)
            left = binaryExpression.Left
            operatorToken = binaryExpression.OperatorToken
            right = binaryExpression.Right
        End Sub

        Public Sub GetPartsOfConditionalExpression(node As SyntaxNode, ByRef condition As SyntaxNode, ByRef whenTrue As SyntaxNode, ByRef whenFalse As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfConditionalExpression
            Dim conditionalExpression = DirectCast(node, TernaryConditionalExpressionSyntax)
            condition = conditionalExpression.Condition
            whenTrue = conditionalExpression.WhenTrue
            whenFalse = conditionalExpression.WhenFalse
        End Sub

        Public Function WalkDownParentheses(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.WalkDownParentheses
            Return If(TryCast(node, ExpressionSyntax)?.WalkDownParentheses(), node)
        End Function

        Public Function IsLogicalAndExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLogicalAndExpression
            Return node.IsKind(SyntaxKind.AndAlsoExpression)
        End Function

        Public Function IsLogicalOrExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLogicalOrExpression
            Return node.IsKind(SyntaxKind.OrElseExpression)
        End Function

        Public Function IsLogicalNotExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLogicalNotExpression
            Return node.IsKind(SyntaxKind.NotExpression)
        End Function

        Public Function IsConditionalAnd(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsConditionalAnd
            Return node.Kind() = SyntaxKind.AndAlsoExpression
        End Function

        Public Function IsConditionalOr(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsConditionalOr
            Return node.Kind() = SyntaxKind.OrElseExpression
        End Function

        Public Function IsTupleExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTupleExpression
            Return node.Kind() = SyntaxKind.TupleExpression
        End Function

        Public Function IsTupleType(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTupleType
            Return node.Kind() = SyntaxKind.TupleType
        End Function

        Public Sub GetPartsOfTupleExpression(Of TArgumentSyntax As SyntaxNode)(
                node As SyntaxNode, ByRef openParen As SyntaxToken, ByRef arguments As SeparatedSyntaxList(Of TArgumentSyntax), ByRef closeParen As SyntaxToken) Implements ISyntaxFactsService.GetPartsOfTupleExpression

            Dim tupleExpr = DirectCast(node, TupleExpressionSyntax)
            openParen = tupleExpr.OpenParenToken
            arguments = CType(CType(tupleExpr.Arguments, SeparatedSyntaxList(Of SyntaxNode)), SeparatedSyntaxList(Of TArgumentSyntax))
            closeParen = tupleExpr.CloseParenToken
        End Sub

        Public Function GetOperandOfPrefixUnaryExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetOperandOfPrefixUnaryExpression
            Return DirectCast(node, UnaryExpressionSyntax).Operand
        End Function

        Public Function GetOperatorTokenOfPrefixUnaryExpression(node As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetOperatorTokenOfPrefixUnaryExpression
            Return DirectCast(node, UnaryExpressionSyntax).OperatorToken
        End Function

        Public Sub GetPartsOfMemberAccessExpression(node As SyntaxNode, ByRef expression As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef name As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfMemberAccessExpression
            Dim memberAccess = DirectCast(node, MemberAccessExpressionSyntax)
            expression = memberAccess.Expression
            operatorToken = memberAccess.OperatorToken
            name = memberAccess.Name
        End Sub

        Public Function GetNextExecutableStatement(statement As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetNextExecutableStatement
            Return DirectCast(statement, StatementSyntax).GetNextStatement()?.FirstAncestorOrSelf(Of ExecutableStatementSyntax)
        End Function

        Public Overrides Function IsWhitespaceTrivia(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsWhitespaceTrivia
            Return trivia.IsWhitespace()
        End Function

        Public Overrides Function IsEndOfLineTrivia(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsEndOfLineTrivia
            Return trivia.IsEndOfLine()
        End Function

        Public Overrides Function IsSingleLineCommentTrivia(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.CommentTrivia
        End Function

        Public Overrides Function IsMultiLineCommentTrivia(trivia As SyntaxTrivia) As Boolean
            ' VB does not have multi-line comments.
            Return False
        End Function

        Public Overrides Function IsSingleLineDocCommentTrivia(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.DocumentationCommentTrivia
        End Function

        Public Overrides Function IsMultiLineDocCommentTrivia(trivia As SyntaxTrivia) As Boolean
            ' VB does not have multi-line comments.
            Return False
        End Function

        Public Overrides Function IsShebangDirectiveTrivia(trivia As SyntaxTrivia) As Boolean
            ' VB does not have shebang directives.
            Return False
        End Function

        Public Overrides Function IsPreprocessorDirective(trivia As SyntaxTrivia) As Boolean
            Return SyntaxFacts.IsPreprocessorDirective(trivia.Kind())
        End Function

        Public Function IsRegularComment(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsRegularComment
            Return trivia.Kind = SyntaxKind.CommentTrivia
        End Function

        Public Function IsDocumentationComment(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsDocumentationComment
            Return trivia.Kind = SyntaxKind.DocumentationCommentTrivia
        End Function

        Public Function IsElastic(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsElastic
            Return trivia.IsElastic()
        End Function

        Public Function IsOnTypeHeader(root As SyntaxNode, position As Integer, ByRef typeDeclaration As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnTypeHeader
            Dim node = TryGetAncestorForLocation(Of TypeStatementSyntax)(root, position)
            typeDeclaration = node

            If node Is Nothing Then
                Return Nothing
            End If

            Return IsOnHeader(root, position, node, If(node.TypeParameterList?.GetLastToken(), node.Identifier))
        End Function

        Public Function IsOnPropertyDeclarationHeader(root As SyntaxNode, position As Integer, ByRef propertyDeclaration As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnPropertyDeclarationHeader
            Dim node = TryGetAncestorForLocation(Of PropertyStatementSyntax)(root, position)
            propertyDeclaration = node

            If propertyDeclaration Is Nothing Then
                Return False
            End If

            If node.AsClause IsNot Nothing Then
                Return IsOnHeader(root, position, node, node.AsClause)
            End If

            Return IsOnHeader(root, position, node, node.Identifier)
        End Function

        Public Function IsOnParameterHeader(root As SyntaxNode, position As Integer, ByRef parameter As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnParameterHeader
            Dim node = TryGetAncestorForLocation(Of ParameterSyntax)(root, position)
            parameter = node

            If parameter Is Nothing Then
                Return False
            End If

            Return IsOnHeader(root, position, node, node)
        End Function

        Public Function IsOnMethodHeader(root As SyntaxNode, position As Integer, ByRef method As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnMethodHeader
            Dim node = TryGetAncestorForLocation(Of MethodStatementSyntax)(root, position)
            method = node

            If method Is Nothing Then
                Return False
            End If

            If node.HasReturnType() Then
                Return IsOnHeader(root, position, method, node.GetReturnType())
            End If

            If node.ParameterList IsNot Nothing Then
                Return IsOnHeader(root, position, method, node.ParameterList)
            End If

            Return IsOnHeader(root, position, node, node)
        End Function

        Public Function IsOnLocalFunctionHeader(root As SyntaxNode, position As Integer, ByRef localFunction As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnLocalFunctionHeader
            ' No local functions in VisualBasic
            Return False
        End Function

        Public Function IsOnLocalDeclarationHeader(root As SyntaxNode, position As Integer, ByRef localDeclaration As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnLocalDeclarationHeader
            Dim node = TryGetAncestorForLocation(Of LocalDeclarationStatementSyntax)(root, position)
            localDeclaration = node

            If localDeclaration Is Nothing Then
                Return False
            End If

            Dim initializersExpressions = node.Declarators.
                Where(Function(d) d.Initializer IsNot Nothing).
                SelectAsArray(Function(initialized) initialized.Initializer.Value)
            Return IsOnHeader(root, position, node, node, initializersExpressions)
        End Function

        Public Function IsOnIfStatementHeader(root As SyntaxNode, position As Integer, ByRef ifStatement As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnIfStatementHeader
            ifStatement = Nothing

            Dim multipleLineNode = TryGetAncestorForLocation(Of MultiLineIfBlockSyntax)(root, position)
            If multipleLineNode IsNot Nothing Then
                ifStatement = multipleLineNode
                Return IsOnHeader(root, position, multipleLineNode.IfStatement, multipleLineNode.IfStatement)
            End If

            Dim singleLineNode = TryGetAncestorForLocation(Of SingleLineIfStatementSyntax)(root, position)
            If singleLineNode IsNot Nothing Then
                ifStatement = singleLineNode
                Return IsOnHeader(root, position, singleLineNode, singleLineNode.Condition)
            End If

            Return False
        End Function

        Public Function IsOnForeachHeader(root As SyntaxNode, position As Integer, ByRef foreachStatement As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsOnForeachHeader
            Dim node = TryGetAncestorForLocation(Of ForEachBlockSyntax)(root, position)
            foreachStatement = node

            If foreachStatement Is Nothing Then
                Return False
            End If

            Return IsOnHeader(root, position, node, node.ForEachStatement)
        End Function

        Public Function IsBetweenTypeMembers(sourceText As SourceText, root As SyntaxNode, position As Integer, ByRef typeDeclaration As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsBetweenTypeMembers
            Dim token = root.FindToken(position)
            Dim typeDecl = token.GetAncestor(Of TypeBlockSyntax)
            typeDeclaration = typeDecl

            If typeDecl IsNot Nothing Then
                Dim start = If(typeDecl.Implements.LastOrDefault()?.Span.End,
                               If(typeDecl.Inherits.LastOrDefault()?.Span.End,
                                  typeDecl.BlockStatement.Span.End))

                If position >= start AndAlso
                   position <= typeDecl.EndBlockStatement.Span.Start Then

                    Dim line = sourceText.Lines.GetLineFromPosition(position)
                    If Not line.IsEmptyOrWhitespace() Then
                        Return False
                    End If

                    Dim member = typeDecl.Members.FirstOrDefault(Function(d) d.FullSpan.Contains(position))
                    If member Is Nothing Then
                        ' There are no members, Or we're after the last member.
                        Return True
                    Else
                        ' We're within a member.  Make sure we're in the leading whitespace of
                        ' the member.
                        If position < member.SpanStart Then
                            For Each trivia In member.GetLeadingTrivia()
                                If Not trivia.IsWhitespaceOrEndOfLine() Then
                                    Return False
                                End If

                                If trivia.FullSpan.Contains(position) Then
                                    Return True
                                End If
                            Next
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Public Function GetSelectedFieldsAndProperties(root As SyntaxNode, textSpan As TextSpan, allowPartialSelection As Boolean) As ImmutableArray(Of SyntaxNode) Implements ISyntaxFactsService.GetSelectedFieldsAndProperties
            Return ImmutableArray(Of SyntaxNode).CastUp(root.GetSelectedFieldsAndPropertiesInSpan(textSpan, allowPartialSelection))
        End Function

        Private Function ISyntaxFactsService_GetFileBanner(root As SyntaxNode) As ImmutableArray(Of SyntaxTrivia) Implements ISyntaxFactsService.GetFileBanner
            Return GetFileBanner(root)
        End Function

        Private Function ISyntaxFactsService_GetFileBanner(firstToken As SyntaxToken) As ImmutableArray(Of SyntaxTrivia) Implements ISyntaxFactsService.GetFileBanner
            Return GetFileBanner(firstToken)
        End Function

        Protected Overrides Function ContainsInterleavedDirective(span As TextSpan, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return token.ContainsInterleavedDirective(span, cancellationToken)
        End Function

        Private Function ISyntaxFactsService_ContainsInterleavedDirective(node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.ContainsInterleavedDirective
            Return ContainsInterleavedDirective(node, cancellationToken)
        End Function

        Private Function ISyntaxFactsService_ContainsInterleavedDirective1(nodes As ImmutableArray(Of SyntaxNode), cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.ContainsInterleavedDirective
            Return ContainsInterleavedDirective(nodes, cancellationToken)
        End Function

        Public Function IsDocumentationCommentExteriorTrivia(trivia As SyntaxTrivia) As Boolean Implements ISyntaxFactsService.IsDocumentationCommentExteriorTrivia
            Return trivia.Kind() = SyntaxKind.DocumentationCommentExteriorTrivia
        End Function

        Private Function ISyntaxFactsService_GetBannerText(documentationCommentTriviaSyntax As SyntaxNode, maxBannerLength As Integer, cancellationToken As CancellationToken) As String Implements ISyntaxFactsService.GetBannerText
            Return GetBannerText(documentationCommentTriviaSyntax, maxBannerLength, cancellationToken)
        End Function

        Public Function GetModifiers(node As SyntaxNode) As SyntaxTokenList Implements ISyntaxFactsService.GetModifiers
            Return node.GetModifiers()
        End Function

        Public Function WithModifiers(node As SyntaxNode, modifiers As SyntaxTokenList) As SyntaxNode Implements ISyntaxFactsService.WithModifiers
            Return node.WithModifiers(modifiers)
        End Function

        Public Function IsLiteralExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLiteralExpression
            Return TypeOf node Is LiteralExpressionSyntax
        End Function

        Public Function IsThisExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsThisExpression
            Return node.IsKind(SyntaxKind.MeExpression)
        End Function

        Public Function IsBaseExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsBaseExpression
            Return node.IsKind(SyntaxKind.MyBaseExpression)
        End Function

        Public Function IsFalseLiteralExpression(expression As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsFalseLiteralExpression
            Return expression.IsKind(SyntaxKind.FalseLiteralExpression)
        End Function

        Public Function IsTrueLiteralExpression(expression As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTrueLiteralExpression
            Return expression.IsKind(SyntaxKind.TrueLiteralExpression)
        End Function

        Public Function GetVariablesOfLocalDeclarationStatement(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetVariablesOfLocalDeclarationStatement
            Return DirectCast(node, LocalDeclarationStatementSyntax).Declarators
        End Function

        Public Function GetInitializerOfVariableDeclarator(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetInitializerOfVariableDeclarator
            Return DirectCast(node, VariableDeclaratorSyntax).Initializer
        End Function

        Public Function GetTypeOfVariableDeclarator(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetTypeOfVariableDeclarator
            Dim declarator = DirectCast(node, VariableDeclaratorSyntax)
            Return TryCast(declarator.AsClause, SimpleAsClauseSyntax)?.Type
        End Function

        Public Function GetValueOfEqualsValueClause(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetValueOfEqualsValueClause
            Return DirectCast(node, EqualsValueSyntax).Value
        End Function

        Public Function IsScopeBlock(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsScopeBlock
            ' VB has no equivalent of curly braces.
            Return False
        End Function

        Public Function IsExecutableBlock(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExecutableBlock
            Return node.IsExecutableBlock()
        End Function

        Public Function GetExecutableBlockStatements(node As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetExecutableBlockStatements
            Return node.GetExecutableBlockStatements()
        End Function

        Public Function FindInnermostCommonExecutableBlock(nodes As IEnumerable(Of SyntaxNode)) As SyntaxNode Implements ISyntaxFactsService.FindInnermostCommonExecutableBlock
            Return nodes.FindInnermostCommonExecutableBlock()
        End Function

        Public Function IsStatementContainer(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsStatementContainer
            Return IsExecutableBlock(node)
        End Function

        Public Function GetStatementContainerStatements(node As SyntaxNode) As IReadOnlyList(Of SyntaxNode) Implements ISyntaxFactsService.GetStatementContainerStatements
            Return GetExecutableBlockStatements(node)
        End Function

        Private Function ISyntaxFactsService_GetLeadingBlankLines(node As SyntaxNode) As ImmutableArray(Of SyntaxTrivia) Implements ISyntaxFactsService.GetLeadingBlankLines
            Return MyBase.GetLeadingBlankLines(node)
        End Function

        Private Function ISyntaxFactsService_GetNodeWithoutLeadingBlankLines(Of TSyntaxNode As SyntaxNode)(node As TSyntaxNode) As TSyntaxNode Implements ISyntaxFactsService.GetNodeWithoutLeadingBlankLines
            Return MyBase.GetNodeWithoutLeadingBlankLines(node)
        End Function

        Public Function IsCastExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsCastExpression
            Return node.Kind = SyntaxKind.DirectCastExpression
        End Function

        Public Sub GetPartsOfCastExpression(node As SyntaxNode, ByRef type As SyntaxNode, ByRef expression As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfCastExpression
            Dim cast = DirectCast(node, DirectCastExpressionSyntax)
            type = cast.Type
            expression = cast.Expression
        End Sub

        Public Function GetDeconstructionReferenceLocation(node As SyntaxNode) As Location Implements ISyntaxFactsService.GetDeconstructionReferenceLocation
            Throw New NotImplementedException()
        End Function

        Public Function GetDeclarationIdentifierIfOverride(token As SyntaxToken) As SyntaxToken? Implements ISyntaxFactsService.GetDeclarationIdentifierIfOverride
            If token.Kind() = SyntaxKind.OverridesKeyword Then
                Dim parent = token.Parent

                Select Case parent.Kind()
                    Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                        Dim method = DirectCast(parent, MethodStatementSyntax)
                        Return method.Identifier

                    Case SyntaxKind.PropertyStatement
                        Dim [property] = DirectCast(parent, PropertyStatementSyntax)
                        Return [property].Identifier
                End Select
            End If

            Return Nothing
        End Function

        Public Shadows Function SpansPreprocessorDirective(nodes As IEnumerable(Of SyntaxNode)) As Boolean Implements ISyntaxFactsService.SpansPreprocessorDirective
            Return MyBase.SpansPreprocessorDirective(nodes)
        End Function

        Public Shadows Function SpansPreprocessorDirective(tokens As IEnumerable(Of SyntaxToken)) As Boolean
            Return MyBase.SpansPreprocessorDirective(tokens)
        End Function

        Public Sub GetPartsOfInvocationExpression(node As SyntaxNode, ByRef expression As SyntaxNode, ByRef argumentList As SyntaxNode) Implements ISyntaxFactsService.GetPartsOfInvocationExpression
            Dim invocation = DirectCast(node, InvocationExpressionSyntax)
            expression = invocation.Expression
            argumentList = invocation.ArgumentList
        End Sub

        Public Function IsPostfixUnaryExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsPostfixUnaryExpression
            ' Does not exist in VB.
            Return False
        End Function

        Public Function IsMemberBindingExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMemberBindingExpression
            ' Does not exist in VB.
            Return False
        End Function

        Public Overrides Function GetAttributeLists(node As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFactsService.GetAttributeLists
            Return VisualBasicSyntaxGenerator.GetAttributeLists(node)
        End Function

        Private Function ISyntaxFactsService_IsExpressionStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsExpressionStatement
            Return MyBase.IsExpressionStatement(node)
        End Function
    End Class
End Namespace
