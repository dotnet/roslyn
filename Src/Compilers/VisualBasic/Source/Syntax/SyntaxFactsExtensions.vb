Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Public Module SyntaxFactsExtensions

        ''' <summary>
        ''' Determine if the token instance represents a reserved or contextual keyword
        ''' </summary>
        <Extension()>
        Public Function IsKeyword(token As SyntaxToken) As Boolean
            Return SyntaxFacts.IsKeyword(token)
        End Function

        ''' <summary>
        ''' Determine if the kind represents a reserved keyword
        ''' </summary>
        <Extension()>
        Public Function IsReservedKeyword(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsReservedKeyword(kind)
        End Function

        ''' <summary>
        ''' Determine if the token instance represents a reserved keyword
        ''' </summary>
        <Extension()>
        Public Function IsReservedKeyword(token As SyntaxToken) As Boolean
            Return SyntaxFacts.IsReservedKeyword(token)
        End Function

        ''' <summary>
        ''' Determine if the kind represents a contextual keyword
        ''' </summary>
        <Extension()>
        Public Function IsContextualKeyword(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsContextualKeyword(kind)
        End Function

        ''' <summary>
        ''' Determine if the token instance represents a contextual keyword
        ''' </summary>
        <Extension()>
        Public Function IsContextualKeyword(token As SyntaxToken) As Boolean
            Return SyntaxFacts.IsContextualKeyword(token)
        End Function

        ''' <summary>
        ''' Determine if the token instance represents a preprocessor keyword
        ''' </summary>
        <Extension()>
        Public Function IsPreprocessorKeyword(token As SyntaxToken) As Boolean
            Return SyntaxFacts.IsPreprocessorKeyword(token)
        End Function

        ''' <summary>
        ''' Determine if the token instance represents a preprocessor keyword
        ''' </summary>
        <Extension()>
        Public Function IsPreprocessorKeyword(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPreprocessorKeyword(kind)
        End Function

        ''' <summary>
        ''' Helper to check whether the token is a predefined type
        ''' </summary>
        ''' <returns>True if it is a predefined type</returns>
        <Extension()>
        Public Function IsPredefinedType(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPredefinedType(kind)
        End Function

        ''' <summary>
        ''' Helper to check whether the token is a predefined type OR Variant keyword
        ''' </summary>
        ''' <returns>True if it is a predefined type OR Variant keyword</returns>
        <Extension()>
        Friend Function IsPredefinedTypeOrVariant(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPredefinedTypeOrVariant(kind)
        End Function


        ' File: C:\dd\vs_langs01_1\src\vb\Language\Prototype\Dev11\Native\VB\Language\Compiler\CommandLine\Parser\Parser.cpp
        ' Lines: 5373 - 5373
        ' bool .::IsSpecifier( [ _In_ Token* T ] )
        <Extension()>
        Friend Function IsSpecifier(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsSpecifier(kind)
        End Function

        ' File: C:\dd\vs_langs01_1\src\vb\Language\Prototype\Dev11\Native\VB\Language\Compiler\CommandLine\Parser\Parser.cpp
        ' Lines: 5411 - 5411
        ' bool .::CanStartSpecifierDeclaration( [ _In_ Token* T ] )
        <Extension()>
        Friend Function CanStartSpecifierDeclaration(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.CanStartSpecifierDeclaration(kind)
        End Function

        ' Test whether a given token is a relational operator.

        ' File: C:\dd\vs_langs01_1\src\vb\Language\Prototype\Dev11\Native\VB\Language\Compiler\Parser\Parser.cpp
        ' Lines: 2586 - 2586
        ' bool .::IsRelationalOperator( [ _In_ Token* TokenToTest ] )
        <Extension()>
        Function IsRelationalOperator(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsRelationalOperator(kind)
        End Function

        ' File: C:\dd\vs_langs01_1\src\vb\Language\Prototype\Dev11\Native\VB\Language\Compiler\CommandLine\Parser\Parser.cpp
        ' Lines: 8591 - 8591
        ' bool .::IsOperatorToken( [ tokens T ] )
        <Extension()>
        Public Function IsOperator(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsOperator(kind)
        End Function

        <Extension()>
        Public Function IsPreprocessorDirective(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPreprocessorDirective(kind)
        End Function

        <Extension()>
        Friend Function SupportsContinueStatement(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.SupportsContinueStatement(kind)
        End Function

        <Extension()>
        Friend Function SupportsExitStatement(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.SupportsExitStatement(kind)
        End Function

        <Extension()>
        Friend Function IsEndBlockLoopOrNextStatement(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsEndBlockLoopOrNextStatement(kind)
        End Function

        <Extension()>
        Friend Function IsXmlSyntax(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsXmlSyntax(kind)
        End Function

        ''' <summary>
        ''' Returns true if the node is the object of an invocation expression
        ''' </summary>
        <Extension()>
        Public Function IsInvoked(node As ExpressionSyntax) As Boolean
            Return SyntaxFacts.IsInvoked(node)
        End Function

        ''' <summary>
        ''' Returns true if the node is the operand of an AddressOf expression
        ''' </summary>
        <Extension()>
        Public Function IsAddressOfOperand(node As ExpressionSyntax) As Boolean
            Return SyntaxFacts.IsAddressOfOperand(node)
        End Function

        ''' <summary>
        ''' Returns true if the node is the operand of an AddressOf expression, or the object
        ''' of an invocation. This is used for special binding rules around the return value variable 
        ''' inside Functions and Property Get accessors.
        ''' </summary>
        <Extension()>
        Public Function IsInvocationOrAddressOfOperand(node As ExpressionSyntax) As Boolean
            Return SyntaxFacts.IsInvocationOrAddressOfOperand(node)
        End Function

        ' Determines whether a particular node is in a context where it must bind to a type.
        <Extension()>
        Public Function IsInTypeOnlyContext(node As ExpressionSyntax) As Boolean
            Return SyntaxFacts.IsInTypeOnlyContext(node)
        End Function

        ' Is this node in a place where it bind to an implemented member.
        <Extension()>
        Friend Function IsImplementedMember(node As SyntaxNode) As Boolean
            Return SyntaxFacts.IsImplementedMember(node)
        End Function

        ' Is this node in a place where is must bind to either a namespace or a type.
        <Extension()>
        Public Function IsInNamespaceOrTypeContext(node As SyntaxNode) As Boolean
            Return SyntaxFacts.IsInNamespaceOrTypeContext(node)
        End Function

        ' Determines if possibleBlock is a block statement and position is in the interior.
        ' If so, then return true.
        Friend Function InBlockInterior(possibleBlock As SyntaxNode, position As Integer) As Boolean
            Return SyntaxFacts.InBlockInterior(possibleBlock, position)
        End Function

        ''' <summary>
        ''' Determines if possibleLambda is a lambda expression and position is in the interior.
        ''' </summary>
        <Extension()>
        Friend Function InLambdaInterior(
                                   possibleLambda As SyntaxNode,
                                   position As Integer
                               ) As Boolean

            Return SyntaxFacts.InLambdaInterior(possibleLambda, position)
        End Function

        ' Determines if possibleBlock is a block statement and position is in the interior.
        ' If so, then return true and the body.
        <Extension()>
        Friend Function InBlockInterior(possibleBlock As SyntaxNode,
                                   position As Integer,
                                   ByRef body As SyntaxList(Of StatementSyntax)
                               ) As Boolean

            Return SyntaxFacts.InBlockInterior(possibleBlock, position, body)
        End Function

        ' Returns is possibleBlock is a block statement. If so, return the begin, body, and end statement. Note that
        ' many blocks (IfPart, TryPart, CaseBlock, etc. ) do not have immediate end statements. Also a few blocks don't
        ' have bodies that are SeparatedSyntaxList(Of StatementSyntax).
        <Extension()>
        Friend Function IsBlockStatement(
                                   possibleBlock As SyntaxNode,
                                   ByRef beginStatement As StatementSyntax,
                                   ByRef body As SyntaxList(Of StatementSyntax),
                                   ByRef endStatement As StatementSyntax
                               ) As Boolean

            Dim unusedBeginTerminator As SyntaxToken = Nothing
            Return SyntaxFacts.IsBlockStatement(possibleBlock, beginStatement, unusedBeginTerminator, body, endStatement)
        End Function

        <Extension()>
        Public Function IsAnyToken(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsAnyToken(kind)
        End Function

        <Extension()>
        Public Function IsPreprocessorPunctuation(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPreprocessorPunctuation(kind)
        End Function

        <Extension()>
        Public Function IsLanguagePunctuation(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsLanguagePunctuation(kind)
        End Function

        <Extension()>
        Public Function IsName(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsName(kind)
        End Function

        <Extension()>
        Public Function IsNamespaceMemberDeclaration(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsNamespaceMemberDeclaration(kind)
        End Function

        <Extension()>
        Public Function IsPunctuationOrKeyword(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsPunctuationOrKeyword(kind)
        End Function

        <Extension()>
        Public Function IsAttributeName(node As SyntaxNode) As Boolean
            Return SyntaxFacts.IsAttributeName(node)
        End Function

        ''' <summary>
        ''' Is the node the name of a named argument of an invocation or object creation expression, 
        ''' but not an attribute.
        ''' </summary>        
        <Extension()>
        Public Function IsNamedArgumentName(node As SyntaxNode) As Boolean
            Return SyntaxFacts.IsNamedArgumentName(node)
        End Function

        ''' <summary>
        ''' Return keyword or punctuation text based on SyntaxKind
        ''' </summary>
        <Extension()>
        Public Function GetBlockName(kind As SyntaxKind) As String
            Return SyntaxFacts.GetBlockName(kind)
        End Function

        <Extension()>
        Public Function IsAccessibilityModifier(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.PrivateKeyword,
                     SyntaxKind.ProtectedKeyword,
                     SyntaxKind.FriendKeyword,
                     SyntaxKind.PublicKeyword
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Friend Function IsTerminator(kind As SyntaxKind) As Boolean
            Return SyntaxFacts.IsTerminator(kind)
        End Function

    End Module
End Namespace