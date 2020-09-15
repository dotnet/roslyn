// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial interface ISyntaxFacts
    {
        bool IsCaseSensitive { get; }
        StringComparer StringComparer { get; }

        SyntaxTrivia ElasticMarker { get; }
        SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

        ISyntaxKinds SyntaxKinds { get; }

        bool SupportsIndexingInitializer(ParseOptions options);
        bool SupportsNotPattern(ParseOptions options);
        bool SupportsThrowExpression(ParseOptions options);
        bool SupportsLocalFunctionDeclaration(ParseOptions options);

        SyntaxToken ParseToken(string text);
        SyntaxTriviaList ParseLeadingTrivia(string text);
        string EscapeIdentifier(string identifier);
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
        /// cref="ISyntaxFactsExtensions.IsReservedOrContextualKeyword"/>.  Also, consider using
        /// <see cref="ISyntaxFactsExtensions.IsWord"/> if all you need is the ability to know
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
        bool IsPreProcessorDirectiveContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool IsLiteral(SyntaxToken token);
        bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token);

        bool IsNumericLiteral(SyntaxToken token);
        bool IsVerbatimStringLiteral(SyntaxToken token);

        bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token, SyntaxNode parent);
        bool IsTypeNamedDynamic(SyntaxToken token, SyntaxNode parent);
        bool IsUsingOrExternOrImport(SyntaxNode node);
        bool IsUsingAliasDirective(SyntaxNode node);
        bool IsGlobalAssemblyAttribute(SyntaxNode node);
        bool IsGlobalModuleAttribute(SyntaxNode node);
        bool IsDeclaration(SyntaxNode node);
        bool IsTypeDeclaration(SyntaxNode node);

        bool IsRegularComment(SyntaxTrivia trivia);
        bool IsDocumentationComment(SyntaxTrivia trivia);
        bool IsElastic(SyntaxTrivia trivia);
        bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes);

        bool IsDocumentationComment(SyntaxNode node);
        bool IsNumericLiteralExpression(SyntaxNode node);
        bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode node);

        string GetText(int kind);
        bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        bool TryGetExternalSourceInfo(SyntaxNode directive, out ExternalSourceInfo info);

        bool IsObjectCreationExpressionType(SyntaxNode node);
        SyntaxNode GetObjectCreationInitializer(SyntaxNode node);
        SyntaxNode GetObjectCreationType(SyntaxNode node);

        bool IsDeclarationExpression(SyntaxNode node);

        bool IsBinaryExpression(SyntaxNode node);
        bool IsIsExpression(SyntaxNode node);
        void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        bool IsIsPatternExpression(SyntaxNode node);
        void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right);

        void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);

        bool IsCastExpression(SyntaxNode node);
        void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);

        bool IsExpressionOfInvocationExpression(SyntaxNode node);
        void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);

        SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);

        bool IsExpressionOfAwaitExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);
        bool IsExpressionOfForeach(SyntaxNode node);

        void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
            out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode;

        SyntaxNode GetOperandOfPrefixUnaryExpression(SyntaxNode node);
        SyntaxToken GetOperatorTokenOfPrefixUnaryExpression(SyntaxNode node);

        // Left side of = assignment.
        bool IsLeftSideOfAssignment(SyntaxNode node);

        bool IsSimpleAssignmentStatement(SyntaxNode statement);
        void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        // Left side of any assignment (for example = or ??= or *=  or += )
        bool IsLeftSideOfAnyAssignment(SyntaxNode node);
        // Left side of compound assignment (for example ??= or *=  or += )
        bool IsLeftSideOfCompoundAssignment(SyntaxNode node);
        SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);

        bool IsInferredAnonymousObjectMemberDeclarator(SyntaxNode node);
        bool IsOperandOfIncrementExpression(SyntaxNode node);
        bool IsOperandOfIncrementOrDecrementExpression(SyntaxNode node);

        bool IsLeftSideOfDot(SyntaxNode node);
        SyntaxNode GetRightSideOfDot(SyntaxNode node);

        /// <summary>
        /// Get the node on the left side of the dot if given a dotted expression.
        /// </summary>
        /// <param name="allowImplicitTarget">
        /// In VB, we have a member access expression with a null expression, this may be one of the
        /// following forms:
        ///     1) new With { .a = 1, .b = .a      .a refers to the anonymous type
        ///     2) With obj : .m                   .m refers to the obj type
        ///     3) new T() With { .a = 1, .b = .a  'a refers to the T type
        /// If `allowImplicitTarget` is set to true, the returned node will be set to approperiate node, otherwise, it will return null.
        /// This parameter has no affect on C# node.
        /// </param>
        SyntaxNode GetLeftSideOfDot(SyntaxNode node, bool allowImplicitTarget = false);

        bool IsRightSideOfQualifiedName(SyntaxNode node);
        bool IsLeftSideOfExplicitInterfaceSpecifier(SyntaxNode node);

#nullable enable
        bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
#nullable restore

        /// <summary>
        /// Gets the containing expression that is actually a language expression and not just typed
        /// as an ExpressionSyntax for convenience. For example, NameSyntax nodes on the right side
        /// of qualified names and member access expressions are not language expressions, yet the
        /// containing qualified names or member access expressions are indeed expressions.
        /// </summary>
        SyntaxNode GetStandaloneExpression(SyntaxNode node);

        /// <summary>
        /// Call on the `.y` part of a `x?.y` to get the entire `x?.y` conditional access expression.  This also works
        /// when there are multiple chained conditional accesses.  For example, calling this on '.y' or '.z' in
        /// `x?.y?.z` will both return the full `x?.y?.z` node.  This can be used to effectively get 'out' of the RHS of
        /// a conditional access, and commonly represents the full standalone expression that can be operated on
        /// atomically.
        /// </summary>
        SyntaxNode GetRootConditionalAccessExpression(SyntaxNode node);

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

        bool IsPointerMemberAccessExpression(SyntaxNode node);

        bool IsNamedArgument(SyntaxNode node);
        bool IsNameOfNamedArgument(SyntaxNode node);
        SyntaxToken? GetNameOfParameter(SyntaxNode node);
        SyntaxNode GetDefaultOfParameter(SyntaxNode node);
        SyntaxNode GetParameterList(SyntaxNode node);
        bool IsParameterList(SyntaxNode node);

        bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);

        void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);

        SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

        void GetPartsOfConditionalAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull);

#nullable enable
        bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
#nullable restore
        bool IsPostfixUnaryExpression(SyntaxNode node);

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
        bool IsArgument([NotNullWhen(true)] SyntaxNode node);
        RefKind GetRefKindOfArgument(SyntaxNode node);

        void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        bool LooksGeneric(SyntaxNode simpleName);

        SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);

        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);

        bool IsUsingDirectiveName(SyntaxNode node);

        bool IsAttributeName(SyntaxNode node);
        SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        bool IsAttributeNamedArgumentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node);
        bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node, out SyntaxNode initializedInstance);

        bool IsDirective(SyntaxNode node);
        bool IsStatement(SyntaxNode node);
        bool IsExecutableStatement(SyntaxNode node);

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

        SyntaxNode GetExpressionOfReturnStatement(SyntaxNode node);

        bool IsLocalFunctionStatement(SyntaxNode node);

        bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement);
        SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        SyntaxNode GetInitializerOfVariableDeclarator(SyntaxNode node);
        SyntaxNode GetValueOfEqualsValueClause(SyntaxNode node);

        bool IsThisConstructorInitializer(SyntaxToken token);
        bool IsBaseConstructorInitializer(SyntaxToken token);
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
        IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node);
        SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);

#nullable enable
        /// <summary>
        /// A node that can host a list of statements or a single statement. In addition to
        /// every "executable block", this also includes C# embedded statement owners.
        /// </summary>
        bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);
#nullable restore

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
        SyntaxNode WalkDownParentheses(SyntaxNode node);

        SyntaxNode ConvertToSingleLine(SyntaxNode node, bool useElasticTrivia = false);

        bool IsClassDeclaration(SyntaxNode node);
        bool IsNamespaceDeclaration(SyntaxNode node);
        List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode root);
        List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root);
        SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration);
        SyntaxList<SyntaxNode> GetMembersOfNamespaceDeclaration(SyntaxNode namespaceDeclaration);
        SyntaxList<SyntaxNode> GetMembersOfCompilationUnit(SyntaxNode compilationUnit);

        bool ContainsInMemberBody(SyntaxNode node, TextSpan span);
        TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, return the <see cref="TextSpan"/> representing the span of the member body
        /// it is contained within. This <see cref="TextSpan"/> is used to determine whether speculative binding should be
        /// used in performance-critical typing scenarios. Note: if this method fails to find a relevant span, it returns
        /// an empty <see cref="TextSpan"/> at position 0.
        /// </summary>
        TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node);

#nullable enable
        /// <summary>
        /// Returns the parent node that binds to the symbols that the IDE prefers for features like Quick Info and Find
        /// All References. For example, if the token is part of the type of an object creation, the parenting object
        /// creation expression is returned so that binding will return constructor symbols.
        /// </summary>
        SyntaxNode? TryGetBindableParent(SyntaxToken token);
#nullable disable

        IEnumerable<SyntaxNode> GetConstructors(SyntaxNode root, CancellationToken cancellationToken);
        bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, that represents and argument return the string representation of
        /// that arguments name.
        /// </summary>
        string GetNameForArgument(SyntaxNode argument);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, that represents an attribute argument return the string representation of
        /// that arguments name.
        /// </summary>
        string GetNameForAttributeArgument(SyntaxNode argument);

        bool IsNameOfSubpattern(SyntaxNode node);
        bool IsPropertyPatternClause(SyntaxNode node);

        bool IsAnyPattern(SyntaxNode node);

        bool IsAndPattern(SyntaxNode node);
        bool IsBinaryPattern(SyntaxNode node);
        bool IsConstantPattern(SyntaxNode node);
        bool IsDeclarationPattern(SyntaxNode node);
        bool IsNotPattern(SyntaxNode node);
        bool IsOrPattern(SyntaxNode node);
        bool IsParenthesizedPattern(SyntaxNode node);
        bool IsRecursivePattern(SyntaxNode node);
        bool IsTypePattern(SyntaxNode node);
        bool IsUnaryPattern(SyntaxNode node);
        bool IsVarPattern(SyntaxNode node);

        SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node);
        void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen);

        void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation);
        void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode positionalPart, out SyntaxNode propertyPart, out SyntaxNode designation);
        void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern);

        SyntaxNode GetTypeOfTypePattern(SyntaxNode node);

        /// <summary>
        /// <paramref name="fullHeader"/> controls how much of the type header should be considered. If <see
        /// langword="false"/> only the span up through the type name will be considered.  If <see langword="true"/>
        /// then the span through the base-list will be considered.
        /// </summary>
        bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, out SyntaxNode typeDeclaration);

        bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, out SyntaxNode propertyDeclaration);
        bool IsOnParameterHeader(SyntaxNode root, int position, out SyntaxNode parameter);
        bool IsOnMethodHeader(SyntaxNode root, int position, out SyntaxNode method);
        bool IsOnLocalFunctionHeader(SyntaxNode root, int position, out SyntaxNode localFunction);
        bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, out SyntaxNode localDeclaration);
        bool IsOnIfStatementHeader(SyntaxNode root, int position, out SyntaxNode ifStatement);
        bool IsOnWhileStatementHeader(SyntaxNode root, int position, out SyntaxNode whileStatement);
        bool IsOnForeachHeader(SyntaxNode root, int position, out SyntaxNode foreachStatement);
        bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, out SyntaxNode typeDeclaration);

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

        bool IsParameterNameXmlElementSyntax(SyntaxNode node);

        SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        bool CanHaveAccessibility(SyntaxNode declaration);

        /// <summary>
        /// Gets the accessibility of the declaration.
        /// </summary>
        Accessibility GetAccessibility(SyntaxNode declaration);

        public abstract void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers, out bool isDefault);

        public abstract SyntaxTokenList GetModifierTokens(SyntaxNode declaration);

        /// <summary>
        /// Gets the <see cref="DeclarationKind"/> for the declaration.
        /// </summary>
        DeclarationKind GetDeclarationKind(SyntaxNode declaration);

        bool IsImplicitObjectCreation(SyntaxNode node);
        SyntaxNode GetExpressionOfThrowExpression(SyntaxNode throwExpression);
        bool IsThrowStatement(SyntaxNode node);
        bool IsLocalFunction(SyntaxNode node);
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
