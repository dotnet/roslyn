﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        bool IsKeyword(SyntaxToken token);
        bool IsContextualKeyword(SyntaxToken token);
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

        bool IsRegularComment(SyntaxTrivia trivia);
        bool IsDocumentationComment(SyntaxTrivia trivia);
        bool IsElastic(SyntaxTrivia trivia);

        bool IsDocumentationComment(SyntaxNode node);
        bool IsNumericLiteralExpression(SyntaxNode node);
        bool IsNullLiteralExpression(SyntaxNode node);
        bool IsDefaultLiteralExpression(SyntaxNode node);
        bool IsLiteralExpression(SyntaxNode node);

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
        void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxNode right);
        void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);

        bool IsCastExpression(SyntaxNode node);
        void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);

        bool IsInvocationExpression(SyntaxNode node);
        bool IsExpressionOfInvocationExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfInvocationExpression(SyntaxNode node);

        SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);

        bool IsExpressionOfAwaitExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);

        bool IsLogicalAndExpression(SyntaxNode node);
        bool IsLogicalNotExpression(SyntaxNode node);
        SyntaxNode GetOperandOfPrefixUnaryExpression(SyntaxNode node);

        // Left side of = assignment.
        bool IsLeftSideOfAssignment(SyntaxNode node);

        bool IsSimpleAssignmentStatement(SyntaxNode statement);
        void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        // Left side of any assignment (for example  *=  or += )
        bool IsLeftSideOfAnyAssignment(SyntaxNode node);
        SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);

        bool IsInferredAnonymousObjectMemberDeclarator(SyntaxNode node);
        bool IsOperandOfIncrementExpression(SyntaxNode node);
        bool IsOperandOfIncrementOrDecrementExpression(SyntaxNode node);

        bool IsLeftSideOfDot(SyntaxNode node);
        SyntaxNode GetRightSideOfDot(SyntaxNode node);

        bool IsRightSideOfQualifiedName(SyntaxNode node);

        bool IsNameOfMemberAccessExpression(SyntaxNode node);
        bool IsExpressionOfMemberAccessExpression(SyntaxNode node);

        SyntaxNode GetNameOfMemberAccessExpression(SyntaxNode node);

        /// <summary>
        /// Returns the expression node the member is being accessed off of.  If <paramref name="allowImplicitTarget"/>
        /// is <code>false</code>, this will be the node directly to the left of the dot-token.  If <paramref name="allowImplicitTarget"/>
        /// is <code>true</code>, then this can return another node in the tree that the member will be accessed
        /// off of.  For example, in VB, if you have a member-access-expression of the form ".Length" then this
        /// may return the expression in the surrounding With-statement.
        /// </summary>
        SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget = false);
        SyntaxToken GetOperatorTokenOfMemberAccessExpression(SyntaxNode node);
        void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode name);

        SyntaxNode GetTargetOfMemberBinding(SyntaxNode node);

        bool IsSimpleMemberAccessExpression(SyntaxNode node);
        bool IsPointerMemberAccessExpression(SyntaxNode node);

        bool IsNamedParameter(SyntaxNode node);
        SyntaxNode GetDefaultOfParameter(SyntaxNode node);
        SyntaxNode GetParameterList(SyntaxNode node);

        bool IsSkippedTokensTrivia(SyntaxNode node);

        bool IsWhitespaceTrivia(SyntaxTrivia trivia);
        bool IsEndOfLineTrivia(SyntaxTrivia trivia);
        bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);

        SyntaxNode GetExpressionOfConditionalAccessExpression(SyntaxNode node);

        SyntaxNode GetExpressionOfElementAccessExpression(SyntaxNode node);
        SyntaxNode GetArgumentListOfElementAccessExpression(SyntaxNode node);

        SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        bool IsConditionalMemberAccessExpression(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

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

        void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        bool LooksGeneric(SyntaxNode simpleName);

        SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);

        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);

        bool IsUsingDirectiveName(SyntaxNode node);
        bool IsIdentifierName(SyntaxNode node);
        bool IsGenericName(SyntaxNode node);

        bool IsAttribute(SyntaxNode node);
        bool IsAttributeName(SyntaxNode node);

        bool IsAttributeNamedArgumentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node, out SyntaxNode initializedInstance);

        bool IsDirective(SyntaxNode node);
        bool IsForEachStatement(SyntaxNode node);
        bool IsLockStatement(SyntaxNode node);
        bool IsUsingStatement(SyntaxNode node);
        bool IsStatement(SyntaxNode node);
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

        bool IsAnonymousFunction(SyntaxNode n);

        bool IsLocalFunction(SyntaxNode n);

        bool IsInConstantContext(SyntaxNode node);
        bool IsInConstructor(SyntaxNode node);
        bool IsMethodLevelMember(SyntaxNode node);
        bool IsTopLevelNodeWithMembers(SyntaxNode node);
        bool HasIncompleteParentMember(SyntaxNode node);

        bool IsExecutableBlock(SyntaxNode node);
        SyntaxList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node);
        SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);

        bool AreEquivalent(SyntaxToken token1, SyntaxToken token2);
        bool AreEquivalent(SyntaxNode node1, SyntaxNode node2);

        string GetDisplayName(SyntaxNode node, DisplayNameOptions options, string rootNamespace = null);

        SyntaxNode GetContainingTypeDeclaration(SyntaxNode root, int position);
        SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true);
        SyntaxNode GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode node);

        SyntaxToken FindTokenOnLeftOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);
        SyntaxToken FindTokenOnRightOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);

        SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true);
        SyntaxNode WalkDownParentheses(SyntaxNode node);

        SyntaxNode ConvertToSingleLine(SyntaxNode node, bool useElasticTrivia = false);

        SyntaxToken ToIdentifierToken(string name);
        List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root);

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

        ImmutableArray<SyntaxNode> GetSelectedMembers(SyntaxNode root, TextSpan textSpan);
        bool IsOnTypeHeader(SyntaxNode root, int position);
        bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position);

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
