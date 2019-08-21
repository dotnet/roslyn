// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISyntaxFactsService : ILanguageService
    {
        bool IsCaseSensitive { get; }
        StringComparer StringComparer { get; }

        SyntaxTrivia ElasticMarker { get; }
        SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

        ISyntaxKindsService SyntaxKinds { get; }

        bool SupportsIndexingInitializer(ParseOptions options);
        bool SupportsThrowExpression(ParseOptions options);

        SyntaxToken ParseToken(string text);

        bool IsAwaitKeyword(SyntaxToken token);
        bool IsIdentifier(SyntaxToken token);
        bool IsGlobalNamespaceKeyword(SyntaxToken token);
        bool IsVerbatimIdentifier(SyntaxToken token);
        bool IsOperator(SyntaxToken token);
        bool IsPredefinedType(SyntaxToken token);
        bool IsPredefinedType(SyntaxToken token, PredefinedType type);
        bool IsPredefinedOperator(SyntaxToken token);
        bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op);

        /// <summary>
        /// Returns 'true' if this a 'reserved' keyword for the language.  A 'reserved' keyword is a
        /// identifier that is always treated as being a special keyword, regardless of where it is
        /// found in the token stream.  Examples of this are tokens like <see langword="class"/> and
        /// <see langword="Class"/> in C# and VB respectively.
        /// 
        /// Importantly, this does *not* include contextual keywords.  If contextual keywords are
        /// important for your scenario, use <see cref="IsContextualKeyword"/> or <see
        /// cref="ISyntaxFactsServiceExtensions.IsReservedOrContextualKeyword"/>.  Also, consider using
        /// <see cref="ISyntaxFactsServiceExtensions.IsWord"/> if all you need is the ability to know 
        /// if this is effectively any identifier in the language, regardless of whether the language
        /// is treating it as a keyword or not.
        /// </summary>
        bool IsReservedKeyword(SyntaxToken token);

        /// <summary>
        /// Returns <see langword="true"/> if this a 'contextual' keyword for the language.  A
        /// 'contextual' keyword is a identifier that is only treated as being a special keyword in
        /// certain *syntactic* contexts.  Examples of this is 'yield' in C#.  This is only a
        /// keyword if used as 'yield return' or 'yield break'.  Importantly, identifiers like <see
        /// langword="var"/>, <see langword="dynamic"/> and <see langword="nameof"/> are *not*
        /// 'contextual' keywords.  This is because they are not treated as keywords depending on
        /// the syntactic context around them.  Instead, the language always treats them identifiers
        /// that have special *semantic* meaning if they end up not binding to an existing symbol.
        /// 
        /// Importantly, if <paramref name="token"/> is not in the syntactic construct where the
        /// language thinks an identifier should be contextually treated as a keyword, then this
        /// will return <see langword="false"/>.
        /// 
        /// Or, in other words, the parser must be able to identify these cases in order to be a
        /// contextual keyword.  If identification happens afterwards, it's not contextual.
        /// </summary>
        bool IsContextualKeyword(SyntaxToken token);

        /// <summary>
        /// The set of identifiers that have special meaning directly after the `#` token in a
        /// preprocessor directive.  For example `if` or `pragma`.
        /// </summary>
        bool IsPreprocessorKeyword(SyntaxToken token);
        bool IsHashToken(SyntaxToken token);

        bool IsLiteral(SyntaxToken token);
        bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token);

        bool IsNumericLiteral(SyntaxToken token);
        bool IsCharacterLiteral(SyntaxToken token);
        bool IsStringLiteral(SyntaxToken token);
        bool IsVerbatimStringLiteral(SyntaxToken token);
        bool IsInterpolatedStringTextToken(SyntaxToken token);
        bool IsStringLiteralExpression(SyntaxNode node);

        bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token, SyntaxNode parent);
        bool IsTypeNamedDynamic(SyntaxToken token, SyntaxNode parent);
        bool IsUsingOrExternOrImport(SyntaxNode node);
        bool IsGlobalAttribute(SyntaxNode node);
        bool IsDeclaration(SyntaxNode node);
        bool IsTypeDeclaration(SyntaxNode node);

        bool IsRegularComment(SyntaxTrivia trivia);
        bool IsDocumentationComment(SyntaxTrivia trivia);
        bool IsElastic(SyntaxTrivia trivia);

        bool IsDocumentationComment(SyntaxNode node);
        bool IsNumericLiteralExpression(SyntaxNode node);
        bool IsNullLiteralExpression(SyntaxNode node);
        bool IsDefaultLiteralExpression(SyntaxNode node);
        bool IsLiteralExpression(SyntaxNode node);
        bool IsFalseLiteralExpression(SyntaxNode node);
        bool IsTrueLiteralExpression(SyntaxNode node);
        bool IsThisExpression(SyntaxNode node);
        bool IsBaseExpression(SyntaxNode node);
        bool IsExpressionStatement(SyntaxNode node);


        string GetText(int kind);
        bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        bool IsPossibleTupleContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        bool TryGetExternalSourceInfo(SyntaxNode directive, out ExternalSourceInfo info);

        bool IsObjectCreationExpressionType(SyntaxNode node);
        bool IsObjectCreationExpression(SyntaxNode node);
        SyntaxNode GetObjectCreationInitializer(SyntaxNode node);
        SyntaxNode GetObjectCreationType(SyntaxNode node);

        bool IsBinaryExpression(SyntaxNode node);
        void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);

        bool IsCastExpression(SyntaxNode node);
        void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);

        bool IsInvocationExpression(SyntaxNode node);
        bool IsExpressionOfInvocationExpression(SyntaxNode node);
        void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);

        SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);

        bool IsAwaitExpression(SyntaxNode node);
        bool IsExpressionOfAwaitExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);

        bool IsLogicalAndExpression(SyntaxNode node);
        bool IsLogicalOrExpression(SyntaxNode node);
        bool IsLogicalNotExpression(SyntaxNode node);
        bool IsConditionalAnd(SyntaxNode node);
        bool IsConditionalOr(SyntaxNode node);

        bool IsTupleExpression(SyntaxNode node);
        void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
            out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode;

        bool IsTupleType(SyntaxNode node);

        SyntaxNode GetOperandOfPrefixUnaryExpression(SyntaxNode node);
        SyntaxToken GetOperatorTokenOfPrefixUnaryExpression(SyntaxNode node);


        // Left side of = assignment.
        bool IsLeftSideOfAssignment(SyntaxNode node);

        bool IsSimpleAssignmentStatement(SyntaxNode statement);
        void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        // Left side of any assignment (for example  *=  or += )
        bool IsLeftSideOfAnyAssignment(SyntaxNode node);
        SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);

        bool IsInferredAnonymousObjectMemberDeclarator(SyntaxNode node);
        bool IsOperandOfIncrementExpression(SyntaxNode node);
        bool IsOperandOfIncrementOrDecrementExpression(SyntaxNode node);

        bool IsLeftSideOfDot(SyntaxNode node);
        SyntaxNode GetRightSideOfDot(SyntaxNode node);

        bool IsRightSideOfQualifiedName(SyntaxNode node);
        bool IsLeftSideOfExplicitInterfaceSpecifier(SyntaxNode node);

        bool IsNameOfMemberAccessExpression(SyntaxNode node);
        bool IsExpressionOfMemberAccessExpression(SyntaxNode node);

        SyntaxNode GetNameOfMemberAccessExpression(SyntaxNode node);

        /// <summary>
        /// Returns the expression node the member is being accessed off of.  If <paramref name="allowImplicitTarget"/>
        /// is <see langword="false"/>, this will be the node directly to the left of the dot-token.  If <paramref name="allowImplicitTarget"/>
        /// is <see langword="true"/>, then this can return another node in the tree that the member will be accessed
        /// off of.  For example, in VB, if you have a member-access-expression of the form ".Length" then this
        /// may return the expression in the surrounding With-statement.
        /// </summary>
        SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget = false);
        void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name);

        SyntaxNode GetTargetOfMemberBinding(SyntaxNode node);

        bool IsSimpleMemberAccessExpression(SyntaxNode node);
        bool IsPointerMemberAccessExpression(SyntaxNode node);

        bool IsNamedParameter(SyntaxNode node);
        SyntaxToken? GetNameOfParameter(SyntaxNode node);
        SyntaxNode GetDefaultOfParameter(SyntaxNode node);
        SyntaxNode GetParameterList(SyntaxNode node);

        bool IsSkippedTokensTrivia(SyntaxNode node);

        bool IsWhitespaceTrivia(SyntaxTrivia trivia);
        bool IsEndOfLineTrivia(SyntaxTrivia trivia);
        bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);

        void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);

        SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

        bool IsConditionalAccessExpression(SyntaxNode node);
        void GetPartsOfConditionalAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull);

        bool IsMemberBindingExpression(SyntaxNode node);
        bool IsPostfixUnaryExpression(SyntaxNode node);

        bool IsParenthesizedExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfParenthesizedExpression(SyntaxNode node);

        SyntaxToken GetIdentifierOfGenericName(SyntaxNode node);
        SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node);
        SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node);
        SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node);

        /// <summary>
        /// True if this is an argument with just an expression and nothing else (i.e. no ref/out,
        /// no named params, no omitted args).
        /// </summary>
        bool IsSimpleArgument(SyntaxNode node);
        bool IsArgument(SyntaxNode node);
        RefKind GetRefKindOfArgument(SyntaxNode node);
        bool IsTypeArgumentList(SyntaxNode node);
        bool IsTypeConstraint(SyntaxNode node);

        void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        bool LooksGeneric(SyntaxNode simpleName);

        SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);

        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);

        bool IsUsingDirectiveName(SyntaxNode node);
        bool IsIdentifierName(SyntaxNode node);
        bool IsGenericName(SyntaxNode node);
        bool IsQualifiedName(SyntaxNode node);

        bool IsAttribute(SyntaxNode node);
        bool IsAttributeName(SyntaxNode node);
        SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        bool IsAttributeNamedArgumentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node, out SyntaxNode initializedInstance);

        bool IsDirective(SyntaxNode node);
        bool IsForEachStatement(SyntaxNode node);
        bool IsLockStatement(SyntaxNode node);
        bool IsUsingStatement(SyntaxNode node);
        bool IsStatement(SyntaxNode node);
        bool IsExecutableStatement(SyntaxNode node);
        bool IsParameter(SyntaxNode node);

        bool IsVariableDeclarator(SyntaxNode node);
        bool IsDeconstructionAssignment(SyntaxNode node);
        bool IsDeconstructionForEachStatement(SyntaxNode node);

        /// <summary>
        /// Returns true for nodes that represent the body of a method.
        /// 
        /// For VB this will be 
        /// MethodBlockBaseSyntax.  This will be true for things like constructor, method, operator
        /// bodies as well as accessor bodies.  It will not be true for things like sub() function()
        /// lambdas.  
        /// 
        /// For C# this will be the BlockSyntax or ArrowExpressionSyntax for a 
        /// method/constructor/deconstructor/operator/accessor.  It will not be included for local
        /// functions.
        /// </summary>
        bool IsMethodBody(SyntaxNode node);

        bool IsReturnStatement(SyntaxNode node);
        SyntaxNode GetExpressionOfReturnStatement(SyntaxNode node);

        bool IsLocalDeclarationStatement(SyntaxNode node);
        bool IsLocalFunctionStatement(SyntaxNode node);

        bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement);
        SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        SyntaxNode GetInitializerOfVariableDeclarator(SyntaxNode node);
        SyntaxNode GetValueOfEqualsValueClause(SyntaxNode node);

        bool IsThisConstructorInitializer(SyntaxToken token);
        bool IsBaseConstructorInitializer(SyntaxToken token);
        bool IsQueryExpression(SyntaxNode node);
        bool IsQueryKeyword(SyntaxToken token);
        bool IsThrowExpression(SyntaxNode node);
        bool IsElementAccessExpression(SyntaxNode node);
        bool IsIndexerMemberCRef(SyntaxNode node);
        bool IsIdentifierStartCharacter(char c);
        bool IsIdentifierPartCharacter(char c);
        bool IsIdentifierEscapeCharacter(char c);
        bool IsStartOfUnicodeEscapeSequence(char c);

        bool IsValidIdentifier(string identifier);
        bool IsVerbatimIdentifier(string identifier);

        /// <summary>
        /// Returns true if the given character is a character which may be included in an
        /// identifier to specify the type of a variable.
        /// </summary>
        bool IsTypeCharacter(char c);

        bool IsBindableToken(SyntaxToken token);

        bool IsInStaticContext(SyntaxNode node);
        bool IsUnsafeContext(SyntaxNode node);

        bool IsInNamespaceOrTypeContext(SyntaxNode node);

        bool IsBaseTypeList(SyntaxNode node);

        bool IsAnonymousFunction(SyntaxNode n);

        bool IsInConstantContext(SyntaxNode node);
        bool IsInConstructor(SyntaxNode node);
        bool IsMethodLevelMember(SyntaxNode node);
        bool IsTopLevelNodeWithMembers(SyntaxNode node);
        bool HasIncompleteParentMember(SyntaxNode node);

        /// <summary>
        /// A block that has no semantics other than introducing a new scope. That is only C# BlockSyntax.
        /// </summary>
        bool IsScopeBlock(SyntaxNode node);

        /// <summary>
        /// A node that contains a list of statements. In C#, this is BlockSyntax and SwitchSectionSyntax.
        /// In VB, this includes all block statements such as a MultiLineIfBlockSyntax.
        /// </summary>
        bool IsExecutableBlock(SyntaxNode node);
        SyntaxList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node);
        SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);

        /// <summary>
        /// A node that can host a list of statements or a single statement. In addition to
        /// every "executable block", this also includes C# embedded statement owners.
        /// </summary>
        bool IsStatementContainer(SyntaxNode node);
        IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode node);

        bool AreEquivalent(SyntaxToken token1, SyntaxToken token2);
        bool AreEquivalent(SyntaxNode node1, SyntaxNode node2);

        string GetDisplayName(SyntaxNode node, DisplayNameOptions options, string rootNamespace = null);

        SyntaxNode GetContainingTypeDeclaration(SyntaxNode root, int position);
        SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true);
        SyntaxNode GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode node);

        SyntaxToken FindTokenOnLeftOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);
        SyntaxToken FindTokenOnRightOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);

        void GetPartsOfParenthesizedExpression(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen);
        SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true);
        SyntaxNode WalkDownParentheses(SyntaxNode node);

        SyntaxNode ConvertToSingleLine(SyntaxNode node, bool useElasticTrivia = false);

        SyntaxToken ToIdentifierToken(string name);
        List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root);
        SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration);
        SyntaxList<SyntaxNode> GetMembersOfNamespaceDeclaration(SyntaxNode namespaceDeclaration);

        bool ContainsInMemberBody(SyntaxNode node, TextSpan span);
        int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node);
        SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId);
        TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, return the <see cref="TextSpan"/> representing the span of the member body
        /// it is contained within. This <see cref="TextSpan"/> is used to determine whether speculative binding should be
        /// used in performance-critical typing scenarios. Note: if this method fails to find a relevant span, it returns
        /// an empty <see cref="TextSpan"/> at position 0.
        /// </summary>
        TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node);

        /// <summary>
        /// Returns the parent node that binds to the symbols that the IDE prefers for features like
        /// Quick Info and Find All References. For example, if the token is part of the type of
        /// an object creation, the parenting object creation expression is returned so that binding
        /// will return constructor symbols.
        /// </summary>
        SyntaxNode GetBindableParent(SyntaxToken token);

        IEnumerable<SyntaxNode> GetConstructors(SyntaxNode root, CancellationToken cancellationToken);
        bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, that represents and argument return the string representation of
        /// that arguments name.
        /// </summary>
        string GetNameForArgument(SyntaxNode argument);

        bool IsNameOfSubpattern(SyntaxNode node);
        bool IsPropertyPatternClause(SyntaxNode node);

        ImmutableArray<SyntaxNode> GetSelectedFieldsAndProperties(SyntaxNode root, TextSpan textSpan, bool allowPartialSelection);
        bool IsOnTypeHeader(SyntaxNode root, int position, out SyntaxNode typeDeclaration);

        bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, out SyntaxNode propertyDeclaration);
        bool IsOnParameterHeader(SyntaxNode root, int position, out SyntaxNode parameter);
        bool IsOnMethodHeader(SyntaxNode root, int position, out SyntaxNode method);
        bool IsOnLocalFunctionHeader(SyntaxNode root, int position, out SyntaxNode localFunction);
        bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, out SyntaxNode localDeclaration);
        bool IsOnIfStatementHeader(SyntaxNode root, int position, out SyntaxNode ifStatement);
        bool IsOnForeachHeader(SyntaxNode root, int position, out SyntaxNode foreachStatement);
        bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, out SyntaxNode typeDeclaration);

        // Walks the tree, starting from contextNode, looking for the first construct
        // with a missing close brace.  If found, the close brace will be added and the
        // updates root will be returned.  The context node in that new tree will also
        // be returned.
        void AddFirstMissingCloseBrace(
            SyntaxNode root, SyntaxNode contextNode,
            out SyntaxNode newRoot, out SyntaxNode newContextNode);

        SyntaxNode GetNextExecutableStatement(SyntaxNode statement);

        ImmutableArray<SyntaxTrivia> GetLeadingBlankLines(SyntaxNode node);
        TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode;

        ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxNode root);
        ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxToken firstToken);

        bool ContainsInterleavedDirective(SyntaxNode node, CancellationToken cancellationToken);
        bool ContainsInterleavedDirective(ImmutableArray<SyntaxNode> nodes, CancellationToken cancellationToken);

        string GetBannerText(SyntaxNode documentationCommentTriviaSyntax, int maxBannerLength, CancellationToken cancellationToken);

        SyntaxTokenList GetModifiers(SyntaxNode node);
        SyntaxNode WithModifiers(SyntaxNode node, SyntaxTokenList modifiers);

        Location GetDeconstructionReferenceLocation(SyntaxNode node);

        SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token);

        bool SpansPreprocessorDirective(IEnumerable<SyntaxNode> nodes);
    }

    [Flags]
    internal enum DisplayNameOptions
    {
        None = 0,
        IncludeMemberKeyword = 1,
        IncludeNamespaces = 1 << 1,
        IncludeParameters = 1 << 2,
        IncludeType = 1 << 3,
        IncludeTypeParameters = 1 << 4
    }
}
