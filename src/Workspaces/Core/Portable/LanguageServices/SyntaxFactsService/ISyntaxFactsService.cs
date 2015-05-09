﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISyntaxFactsService : ILanguageService
    {
        bool IsCaseSensitive { get; }

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
        bool IsStringLiteral(SyntaxToken token);
        bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token, SyntaxNode parent);
        bool IsTypeNamedDynamic(SyntaxToken token, SyntaxNode parent);

        string GetText(int kind);

        bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        bool TryGetExternalSourceInfo(SyntaxNode directive, out ExternalSourceInfo info);

        bool IsObjectCreationExpressionType(SyntaxNode node);
        bool IsObjectCreationExpression(SyntaxNode node);
        bool IsInvocationExpression(SyntaxNode node);

        bool IsRightSideOfQualifiedName(SyntaxNode node);
        bool IsMemberAccessExpressionName(SyntaxNode node);

        bool IsMemberAccessExpression(SyntaxNode node);
        bool IsPointerMemberAccessExpression(SyntaxNode node);
        bool IsNamedParameter(SyntaxNode node);

        bool IsSkippedTokensTrivia(SyntaxNode node);

        SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfConditionalMemberAccessExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        bool IsConditionalMemberAccessExpression(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);
        SyntaxToken GetIdentifierOfGenericName(SyntaxNode node);
        RefKind GetRefKindOfArgument(SyntaxNode node);
        void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);

        bool IsUsingDirectiveName(SyntaxNode node);
        bool IsGenericName(SyntaxNode node);

        bool IsAttribute(SyntaxNode node);
        bool IsAttributeName(SyntaxNode node);

        bool IsAttributeNamedArgumentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node);

        bool IsDirective(SyntaxNode node);
        bool IsForEachStatement(SyntaxNode node);
        bool IsLockStatement(SyntaxNode node);
        bool IsUsingStatement(SyntaxNode node);

        bool IsThisConstructorInitializer(SyntaxToken token);
        bool IsBaseConstructorInitializer(SyntaxToken token);
        bool IsQueryExpression(SyntaxNode node);
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

        bool IsInConstantContext(SyntaxNode node);
        bool IsInConstructor(SyntaxNode node);
        bool IsMethodLevelMember(SyntaxNode node);
        bool IsTopLevelNodeWithMembers(SyntaxNode node);
        bool HasIncompleteParentMember(SyntaxNode node);

        bool TryGetDeclaredSymbolInfo(SyntaxNode node, out DeclaredSymbolInfo declaredSymbolInfo);

        SyntaxNode GetContainingTypeDeclaration(SyntaxNode root, int position);
        SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true);
        SyntaxNode GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode node);

        SyntaxToken FindTokenOnLeftOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);
        SyntaxToken FindTokenOnRightOfPosition(SyntaxNode node, int position, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false);

        SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true);

        SyntaxNode ConvertToSingleLine(SyntaxNode node);

        SyntaxToken ToIdentifierToken(string name);

        List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root);

        bool ContainsInMemberBody(SyntaxNode node, TextSpan span);

        int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node);
        SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId);

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
    }
}
