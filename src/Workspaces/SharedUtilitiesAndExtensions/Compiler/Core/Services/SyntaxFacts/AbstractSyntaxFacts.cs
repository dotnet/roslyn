// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFacts : ISyntaxFacts
    {
        public abstract ISyntaxKinds SyntaxKinds { get; }

        protected AbstractSyntaxFacts()
        {
        }

        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        public abstract bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode? node);

        public abstract bool IsParameterNameXmlElementSyntax(SyntaxNode? node);

        public abstract SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        public bool HasIncompleteParentMember([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent?.RawKind == SyntaxKinds.IncompleteMember;

        public abstract bool AreEquivalent(SyntaxNode? node1, SyntaxNode? node2);
        public abstract bool AreEquivalent(SyntaxToken token1, SyntaxToken token2);
        public abstract bool ContainsInMemberBody([NotNullWhen(true)] SyntaxNode? node, TextSpan span);
        public abstract bool IsAndPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsAnonymousFunctionExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsAnyPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsAttributeName(SyntaxNode node);
        public abstract bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBaseConstructorInitializer(SyntaxToken token);
        public abstract bool IsBaseNamespaceDeclaration([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBaseTypeList([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBinaryExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBinaryPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBindableToken(SyntaxToken token);
        public abstract bool IsCaseSensitive { get; }
        public abstract bool IsCastExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsConstantPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsContextualKeyword(SyntaxToken token);
        public abstract bool IsConversionExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclaration(SyntaxNode node);
        public abstract bool IsDeclarationExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclarationPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement);
        public abstract bool IsDeconstructionAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeconstructionForEachStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDirective([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDocumentationComment(SyntaxNode node);
        public abstract bool IsDocumentationComment(SyntaxTrivia trivia);
        public abstract bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);
        public abstract bool IsElastic(SyntaxTrivia trivia);
        public abstract bool IsElementAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsEntirelyWithinStringOrCharOrNumericLiteral([NotNullWhen(true)] SyntaxTree? syntaxTree, int position, CancellationToken cancellationToken);
        public abstract bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsExecutableStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsExpressionOfForeach([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsGlobalAssemblyAttribute([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsGlobalModuleAttribute([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIdentifierEscapeCharacter(char c);
        public abstract bool IsIdentifierPartCharacter(char c);
        public abstract bool IsIdentifierStartCharacter(char c);
        public abstract bool IsInConstantContext([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInConstructor(SyntaxNode node);
        public abstract bool IsIndexerMemberCRef([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInferredAnonymousObjectMemberDeclarator([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInNamespaceOrTypeContext([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInStaticContext(SyntaxNode node);
        public abstract bool IsIsExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIsPatternExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfAnyAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfCompoundAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfDot([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLiteral(SyntaxToken token);
        public abstract bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? initializedInstance);
        public abstract bool IsMethodBody([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMethodLevelMember([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfSubpattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNotPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNumericLiteral(SyntaxToken token);
        public abstract bool IsOperandOfIncrementExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsOperandOfIncrementOrDecrementExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsOperator(SyntaxToken token);
        public abstract bool IsOrPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsParameterList([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsParenthesizedPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsPointerMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsPostfixUnaryExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes);
        public abstract bool IsPredefinedOperator(SyntaxToken token);
        public abstract bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op);
        public abstract bool IsPredefinedType(SyntaxToken token);
        public abstract bool IsPredefinedType(SyntaxToken token, PredefinedType type);
        public abstract bool IsPreProcessorDirectiveContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        public abstract bool IsPreprocessorKeyword(SyntaxToken token);
        public abstract bool IsPropertyPatternClause(SyntaxNode node);
        public abstract bool IsQueryKeyword(SyntaxToken token);
        public abstract bool IsRecursivePattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsRegularComment(SyntaxTrivia trivia);
        public abstract bool IsReservedKeyword(SyntaxToken token);
        public abstract bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsSimpleArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsSimpleAssignmentStatement([NotNullWhen(true)] SyntaxNode? statement);
        public abstract bool IsSimpleName([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsStartOfUnicodeEscapeSequence(char c);
        public abstract bool IsStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token);
        public abstract bool IsThisConstructorInitializer(SyntaxToken token);
        public abstract bool IsTopLevelNodeWithMembers([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsTypeCharacter(char c);
        public abstract bool IsTypeDeclaration(SyntaxNode node);
        public abstract bool IsTypePattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsUnaryPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsUnsafeContext(SyntaxNode node);
        public abstract bool IsUsingAliasDirective([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsUsingDirectiveName([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsUsingOrExternOrImport([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsValidIdentifier(string identifier);
        public abstract bool IsVarPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsVerbatimIdentifier(string identifier);
        public abstract bool IsVerbatimIdentifier(SyntaxToken token);
        public abstract bool IsVerbatimInterpolatedStringExpression(SyntaxNode node);
        public abstract bool IsVerbatimStringLiteral(SyntaxToken token);
        public abstract bool LooksGeneric(SyntaxNode simpleName);
        public abstract bool SupportsIndexingInitializer(ParseOptions options);
        public abstract bool SupportsLocalFunctionDeclaration(ParseOptions options);
        public abstract bool SupportsNotPattern(ParseOptions options);
        public abstract bool SupportsRecord(ParseOptions options);
        public abstract bool SupportsRecordStruct(ParseOptions options);
        public abstract bool SupportsThrowExpression(ParseOptions options);
        public abstract bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);
        public abstract bool TryGetExternalSourceInfo([NotNullWhen(true)] SyntaxNode? directive, out ExternalSourceInfo info);
        public abstract bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        public abstract bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        public abstract IEnumerable<SyntaxNode> GetConstructors(SyntaxNode? root, CancellationToken cancellationToken);
        public abstract IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode? node);
        public abstract IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode? node);
        public abstract List<SyntaxNode> GetMethodLevelMembers(SyntaxNode? root);
        public abstract List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode? root);
        public abstract Location GetDeconstructionReferenceLocation(SyntaxNode node);
        public abstract RefKind GetRefKindOfArgument(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        public abstract string EscapeIdentifier(string identifier);
        public abstract string GetDisplayName(SyntaxNode? node, DisplayNameOptions options, string? rootNamespace = null);
        public abstract string GetNameForArgument(SyntaxNode? argument);
        public abstract string GetNameForAttributeArgument(SyntaxNode? argument);
        public abstract string GetText(int kind);
        public abstract StringComparer StringComparer { get; }
        public abstract SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);
        public abstract SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration);
        public abstract SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfThrowExpression(SyntaxNode node);
        public abstract SyntaxNode GetNameOfAttribute(SyntaxNode node);
        public abstract SyntaxNode GetNameOfMemberBindingExpression(SyntaxNode node);
        public abstract SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);
        public abstract SyntaxNode GetTypeOfTypePattern(SyntaxNode node);
        public abstract SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node);
        public abstract SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);
        public abstract SyntaxNode? GetContainingMemberDeclaration(SyntaxNode? root, int position, bool useFullSpan = true);
        public abstract SyntaxNode? GetContainingTypeDeclaration(SyntaxNode? root, int position);
        public abstract SyntaxNode? GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode? node);
        public abstract SyntaxNode? GetDefaultOfParameter(SyntaxNode node);
        public abstract SyntaxNode? GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget = false);
        public abstract SyntaxNode? GetExpressionOfReturnStatement(SyntaxNode node);
        public abstract SyntaxNode? GetInitializerOfVariableDeclarator(SyntaxNode node);
        public abstract SyntaxNode? GetLeftSideOfDot(SyntaxNode? node, bool allowImplicitTarget = false);
        public abstract SyntaxNode? GetParameterList(SyntaxNode node);
        public abstract SyntaxNode? GetRightSideOfDot(SyntaxNode? node);
        public abstract SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node);
        public abstract SyntaxNode? GetTargetOfMemberBinding(SyntaxNode? node);
        public abstract SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node);
        public abstract SyntaxNode? TryGetBindableParent(SyntaxToken token);
        public abstract SyntaxToken GetIdentifierOfGenericName(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfIdentifierName(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfParameter(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfTypeDeclaration(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node);
        public abstract SyntaxToken ParseToken(string text);
        public abstract SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token);
        public abstract SyntaxToken? GetNameOfParameter([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxTokenList GetModifiers(SyntaxNode? node);
        public abstract SyntaxTrivia ElasticCarriageReturnLineFeed { get; }
        public abstract SyntaxTrivia ElasticMarker { get; }
        public abstract SyntaxTriviaList ParseLeadingTrivia(string text);
        public abstract TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken);
        public abstract TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node);
        public abstract void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        public abstract void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfBaseNamespaceDeclaration(SyntaxNode node, out SyntaxNode name, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> members);
        public abstract void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);
        public abstract void GetPartsOfCompilationUnit(SyntaxNode node, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> attributeLists, out SyntaxList<SyntaxNode> members);
        public abstract void GetPartsOfConditionalAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull);
        public abstract void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);
        public abstract void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation);
        public abstract void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);
        public abstract void GetPartsOfInterpolationExpression(SyntaxNode node, out SyntaxToken stringStartToken, out SyntaxList<SyntaxNode> contents, out SyntaxToken stringEndToken);
        public abstract void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode? argumentList);
        public abstract void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right);
        public abstract void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name);
        public abstract void GetPartsOfObjectCreationExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode? argumentList, out SyntaxNode? initializer);
        public abstract void GetPartsOfParenthesizedExpression(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen);
        public abstract void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen);
        public abstract void GetPartsOfPrefixUnaryExpression(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode operand);
        public abstract void GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken dotToken, out SyntaxNode right);
        public abstract void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode? type, out SyntaxNode? positionalPart, out SyntaxNode? propertyPart, out SyntaxNode? designation);
        public abstract void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node, out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode;
        public abstract void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern);
        public abstract void GetPartsOfUsingAliasDirective(SyntaxNode node, out SyntaxToken globalKeyword, out SyntaxToken alias, out SyntaxNode name);

        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? GetStandaloneExpression(SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? WalkDownParentheses(SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? ConvertToSingleLine(SyntaxNode? node, bool useElasticTrivia = false);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? WithModifiers(SyntaxNode? node, SyntaxTokenList modifiers);
    }
}
