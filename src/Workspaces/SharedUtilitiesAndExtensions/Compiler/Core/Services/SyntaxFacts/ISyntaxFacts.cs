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
    /// <summary>
    /// Contains helpers to allow features and other algorithms to run over C# and Visual Basic code in a uniform fashion.
    /// It should be thought of a generalized way to apply type-pattern-matching and syntax-deconstruction in a uniform
    /// fashion over the languages. Helpers in this type should only be one of the following forms:
    /// <list type="bullet">
    /// <item>
    /// 'IsXXX' where 'XXX' exactly matches one of the same named syntax (node, token, trivia, list, etc.) constructs that 
    /// both C# and VB have. For example 'IsSimpleName' to correspond to C# and VB's SimpleNameSyntax node.  These 'checking' 
    /// methods should never fail.  For non leaf node types this should be implemented as a typecheck ('is' in C#, 'typeof ... is'
    /// in VB).  For leaf nodes, this should be implemented by deffering to <see cref="ISyntaxKinds"/> to check against the 
    /// raw kind of the node.
    /// </item>
    /// <item>
    /// 'GetPartsOfXXX(SyntaxNode node, out SyntaxNode/SyntaxToken part1, ...)' where 'XXX' one of the same named Syntax constructs
    /// that both C# and VB have, and where the returned parts correspond to the members those nodes have in common across the 
    /// languages.  For example 'GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken dotToken, out SyntaxNode right)'
    /// VB.  These functions should throw if passed a node that the corresponding 'IsXXX' did not return <see langword="true"/> for.
    /// For nodes that only have a single child, 'GetPartsOfXXX' is not not needed and can be replaced with the easier to use
    /// 'GetXXXOfYYY' to get that single child.
    /// </item>
    /// <item>
    /// 'GetXxxOfYYY' where 'XXX' matches the name of a property on a 'YYY' syntax construct that both C# and VB have.  For
    /// example 'GetExpressionOfMemberAccessExpression' corresponding to MemberAccessExpressionsyntax.Expression in both C# and
    /// VB.  These functions should throw if passed a node that the corresponding 'IsYYY' did not return <see langword="true"/> for.
    /// For nodes that only have a single child, these functions can stay here.  For nodes with multiple children, these should migrate
    /// to <see cref="ISyntaxFactsExtensions"/> and be built off of 'GetPartsOfXXX'.
    /// </item>
    /// <item>
    /// Absolutely trivial questions that relate to syntax and can be asked sensibly of each language.  For example,
    /// if certain constructs (like 'patterns') are supported in that language or not.
    /// </item>
    /// </list>
    ///
    /// <para>Importantly, avoid:</para>
    ///
    /// <list type="bullet">
    /// <item>
    /// Functions that attempt to blur the lines between similar constructs in the same language.  For example, a QualifiedName
    /// is not the same as a MemberAccessExpression (despite A.B being representable as either depending on context). 
    /// Features that need to handle both should make it clear that they are doing so, showing that they're doing the right
    /// thing for the contexts each can arise in (for the above example in 'type' vs 'expression' contexts).
    /// </item>
    /// <item>
    /// Functions which are effectively specific to a single feature are are just trying to find a place to place complex
    /// feature logic in a place such that it can run over VB or C#.  For example, a function to determine if a position
    /// is on the 'header' of a node.  a 'header' is a not a well defined syntax concept that can be trivially asked of
    /// nodes in either language.  It is an excapsulation of a feature (or set of features) level idea that should be in
    /// its own dedicated service.
    /// </item>
    /// <item>
    /// Functions that mutate or update syntax constructs for example 'WithXXX'.  These should be on SyntaxGenerator or
    /// some other feature specific service.
    /// </item>
    /// <item>
    /// Functions that a single item when one language may allow for multiple.  For example 'GetIdentifierOfVariableDeclarator'.
    /// In VB a VariableDeclarator can itself have several names, so calling code must be written to check for that and handle
    /// it apropriately.  Functions like this make it seem like that doesn't need to be considered, easily allowing for bugs
    /// to creep in.
    /// </item>
    /// <item>
    /// Abbreviating or otherwise changing the names that C# and VB share here.  For example use 'ObjectCreationExpression'
    /// not 'ObjectCreation'.  This prevents accidental duplication and keeps consistency with all members.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Many helpers in this type currently violate the above 'dos' and 'do nots'.  They should be removed and either 
    /// inlined directly into the feature that needs if (if only a single feature), or moved into a dedicated service
    /// for that purpose if needed by multiple features.
    /// </remarks>
    internal interface ISyntaxFacts
    {
        bool IsCaseSensitive { get; }
        StringComparer StringComparer { get; }

        SyntaxTrivia ElasticMarker { get; }
        SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

        ISyntaxKinds SyntaxKinds { get; }

        bool SupportsIndexingInitializer(ParseOptions options);
        bool SupportsLocalFunctionDeclaration(ParseOptions options);
        bool SupportsNotPattern(ParseOptions options);
        bool SupportsRecord(ParseOptions options);
        bool SupportsRecordStruct(ParseOptions options);
        bool SupportsThrowExpression(ParseOptions options);
        bool SupportsTargetTypedConditionalExpression(ParseOptions options);
        bool SupportsIsNotTypeExpression(ParseOptions options);
        bool SupportsConstantInterpolatedStrings(ParseOptions options);

        SyntaxToken ParseToken(string text);
        SyntaxTriviaList ParseLeadingTrivia(string text);
        string EscapeIdentifier(string identifier);
        bool IsVerbatimIdentifier(SyntaxToken token);
        bool IsOperator(SyntaxToken token);
        bool IsPredefinedOperator(SyntaxToken token);
        bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op);

        bool IsPredefinedType(SyntaxToken token);
        bool IsPredefinedType(SyntaxToken token, PredefinedType type);

        bool IsPredefinedType([NotNullWhen(true)] SyntaxNode? node);
        bool IsPredefinedType([NotNullWhen(true)] SyntaxNode? node, PredefinedType type);

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

        bool IsUsingOrExternOrImport([NotNullWhen(true)] SyntaxNode? node);
        bool IsGlobalAssemblyAttribute([NotNullWhen(true)] SyntaxNode? node);
        bool IsGlobalModuleAttribute([NotNullWhen(true)] SyntaxNode? node);
        bool IsDeclaration([NotNullWhen(true)] SyntaxNode? node);
        bool IsTypeDeclaration(SyntaxNode node);

        bool IsUsingAliasDirective([NotNullWhen(true)] SyntaxNode? node);

        bool IsRegularComment(SyntaxTrivia trivia);
        bool IsDocumentationComment(SyntaxTrivia trivia);
        bool IsElastic(SyntaxTrivia trivia);
        bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes);

        bool IsPreprocessorDirective(SyntaxTrivia trivia);

        bool IsDocumentationComment(SyntaxNode node);

        string GetText(int kind);
        bool IsEntirelyWithinStringOrCharOrNumericLiteral([NotNullWhen(true)] SyntaxTree? syntaxTree, int position, CancellationToken cancellationToken);

        bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        bool TryGetExternalSourceInfo([NotNullWhen(true)] SyntaxNode? directive, out ExternalSourceInfo info);

        bool IsDeclarationExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsIsTypeExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsIsNotTypeExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsIsPatternExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsConversionExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsCastExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsExpressionOfForeach([NotNullWhen(true)] SyntaxNode? node);
        SyntaxNode GetExpressionOfForeachStatement(SyntaxNode node);

        void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
            out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode;

        bool IsVerbatimInterpolatedStringExpression(SyntaxNode node);

        // Left side of = assignment.
        bool IsLeftSideOfAssignment([NotNullWhen(true)] SyntaxNode? node);

        bool IsSimpleAssignmentStatement([NotNullWhen(true)] SyntaxNode? statement);
        void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        // Left side of any assignment (for example = or ??= or *=  or += )
        bool IsLeftSideOfAnyAssignment([NotNullWhen(true)] SyntaxNode? node);
        // Left side of compound assignment (for example ??= or *=  or += )
        bool IsLeftSideOfCompoundAssignment([NotNullWhen(true)] SyntaxNode? node);
        SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);

        bool IsInferredAnonymousObjectMemberDeclarator([NotNullWhen(true)] SyntaxNode? node);
        bool IsOperandOfIncrementExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsOperandOfIncrementOrDecrementExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsLeftSideOfDot([NotNullWhen(true)] SyntaxNode? node);
        SyntaxNode? GetRightSideOfDot(SyntaxNode? node);

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
        SyntaxNode? GetLeftSideOfDot(SyntaxNode? node, bool allowImplicitTarget = false);

        bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] SyntaxNode? node);

        bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);

        /// <summary>
        /// Gets the containing expression that is actually a language expression and not just typed
        /// as an ExpressionSyntax for convenience. For example, NameSyntax nodes on the right side
        /// of qualified names and member access expressions are not language expressions, yet the
        /// containing qualified names or member access expressions are indeed expressions.
        /// </summary>
        [return: NotNullIfNotNull("node")]
        SyntaxNode? GetStandaloneExpression(SyntaxNode? node);

        /// <summary>
        /// Call on the `.y` part of a `x?.y` to get the entire `x?.y` conditional access expression.  This also works
        /// when there are multiple chained conditional accesses.  For example, calling this on '.y' or '.z' in
        /// `x?.y?.z` will both return the full `x?.y?.z` node.  This can be used to effectively get 'out' of the RHS of
        /// a conditional access, and commonly represents the full standalone expression that can be operated on
        /// atomically.
        /// </summary>
        SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node);

        /// <summary>
        /// Returns the expression node the member is being accessed off of.  If <paramref name="allowImplicitTarget"/>
        /// is <see langword="false"/>, this will be the node directly to the left of the dot-token.  If <paramref name="allowImplicitTarget"/>
        /// is <see langword="true"/>, then this can return another node in the tree that the member will be accessed
        /// off of.  For example, in VB, if you have a member-access-expression of the form ".Length" then this
        /// may return the expression in the surrounding With-statement.
        /// </summary>
        SyntaxNode? GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget = false);

        SyntaxNode? GetTargetOfMemberBinding(SyntaxNode? node);

        SyntaxNode GetNameOfMemberBindingExpression(SyntaxNode node);

        bool IsPointerMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);

        bool IsNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        bool IsNameOfNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        SyntaxNode? GetParameterList(SyntaxNode node);
        bool IsParameterList([NotNullWhen(true)] SyntaxNode? node);

        bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);

        void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);

        SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        SyntaxNode GetExpressionOfAttributeArgument(SyntaxNode node);
        SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

        bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsPostfixUnaryExpression([NotNullWhen(true)] SyntaxNode? node);

        SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node);
        SyntaxToken GetIdentifierOfTypeDeclaration(SyntaxNode node);
        SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node);
        SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node);

        /// <summary>
        /// True if this is an argument with just an expression and nothing else (i.e. no ref/out,
        /// no named params, no omitted args).
        /// </summary>
        bool IsSimpleArgument([NotNullWhen(true)] SyntaxNode? node);
        bool IsArgument([NotNullWhen(true)] SyntaxNode? node);
        bool IsAttributeArgument([NotNullWhen(true)] SyntaxNode? node);
        RefKind GetRefKindOfArgument(SyntaxNode node);

        void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        bool LooksGeneric(SyntaxNode simpleName);

        SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);

        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetArgumentsOfAttributeArgumentList(SyntaxNode node);

        bool IsUsingDirectiveName([NotNullWhen(true)] SyntaxNode? node);

        bool IsAttributeName(SyntaxNode node);
        // Violation.  Doesn't correspond to any shared structure for vb/c#
        SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode? node);

        bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] SyntaxNode? node);
        bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? initializedInstance);
        bool IsAnyInitializerExpression([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? creationExpression);

        bool IsDirective([NotNullWhen(true)] SyntaxNode? node);
        bool IsStatement([NotNullWhen(true)] SyntaxNode? node);
        bool IsExecutableStatement([NotNullWhen(true)] SyntaxNode? node);
        bool IsGlobalStatement([NotNullWhen(true)] SyntaxNode? node);
        bool AreStatementsInSameContainer(SyntaxNode firstStatement, SyntaxNode secondStatement);

        bool IsDeconstructionAssignment([NotNullWhen(true)] SyntaxNode? node);
        bool IsDeconstructionForEachStatement([NotNullWhen(true)] SyntaxNode? node);

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
        bool IsMethodBody([NotNullWhen(true)] SyntaxNode? node);

        bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement);
        SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        SyntaxNode? GetInitializerOfVariableDeclarator(SyntaxNode node);

        bool IsThisConstructorInitializer(SyntaxToken token);
        bool IsBaseConstructorInitializer(SyntaxToken token);
        bool IsQueryKeyword(SyntaxToken token);
        bool IsElementAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsIndexerMemberCRef([NotNullWhen(true)] SyntaxNode? node);
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

        // Violation.  This is a feature level API for QuickInfo.
        bool IsBindableToken(SyntaxToken token);

        bool IsInStaticContext(SyntaxNode node);
        bool IsUnsafeContext(SyntaxNode node);

        bool IsInNamespaceOrTypeContext([NotNullWhen(true)] SyntaxNode? node);

        bool IsBaseTypeList([NotNullWhen(true)] SyntaxNode? node);

        bool IsInConstantContext([NotNullWhen(true)] SyntaxNode? node);
        bool IsInConstructor(SyntaxNode node);
        bool IsMethodLevelMember([NotNullWhen(true)] SyntaxNode? node);
        bool IsTopLevelNodeWithMembers([NotNullWhen(true)] SyntaxNode? node);

        bool AreEquivalent(SyntaxToken token1, SyntaxToken token2);
        bool AreEquivalent(SyntaxNode? node1, SyntaxNode? node2);

        string GetDisplayName(SyntaxNode? node, DisplayNameOptions options, string? rootNamespace = null);

        // Violation.  This is a feature level API.  How 'position' relates to 'containment' is not defined.
        SyntaxNode? GetContainingTypeDeclaration(SyntaxNode root, int position);
        SyntaxNode? GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true);
        SyntaxNode? GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode? node);

        // Violation.  This is a feature level API.
        [return: NotNullIfNotNull("node")]
        SyntaxNode? ConvertToSingleLine(SyntaxNode? node, bool useElasticTrivia = false);

        // Violation.  This is a feature level API.
        List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode? root);
        // Violation.  This is a feature level API.
        List<SyntaxNode> GetMethodLevelMembers(SyntaxNode? root);
        SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration);

        // Violation.  This is a feature level API.
        bool ContainsInMemberBody([NotNullWhen(true)] SyntaxNode? node, TextSpan span);
        // Violation.  This is a feature level API.
        TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, return the <see cref="TextSpan"/> representing the span of the member body
        /// it is contained within. This <see cref="TextSpan"/> is used to determine whether speculative binding should be
        /// used in performance-critical typing scenarios. Note: if this method fails to find a relevant span, it returns
        /// an empty <see cref="TextSpan"/> at position 0.
        /// </summary>
        // Violation.  This is a feature level API.
        TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node);

        /// <summary>
        /// Returns the parent node that binds to the symbols that the IDE prefers for features like Quick Info and Find
        /// All References. For example, if the token is part of the type of an object creation, the parenting object
        /// creation expression is returned so that binding will return constructor symbols.
        /// </summary>
        // Violation.  This is a feature level API.
        SyntaxNode? TryGetBindableParent(SyntaxToken token);

        // Violation.  This is a feature level API.
        IEnumerable<SyntaxNode> GetConstructors(SyntaxNode? root, CancellationToken cancellationToken);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, that represents and argument return the string representation of
        /// that arguments name.
        /// </summary>
        // Violation.  This is a feature level API.
        string GetNameForArgument(SyntaxNode? argument);

        /// <summary>
        /// Given a <see cref="SyntaxNode"/>, that represents an attribute argument return the string representation of
        /// that arguments name.
        /// </summary>
        // Violation.  This is a feature level API.
        string GetNameForAttributeArgument(SyntaxNode? argument);

        bool IsNameOfSubpattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsPropertyPatternClause(SyntaxNode node);

        bool IsAnyPattern([NotNullWhen(true)] SyntaxNode? node);

        bool IsAndPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsBinaryPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsConstantPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsDeclarationPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsNotPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsOrPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsParenthesizedPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsRecursivePattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsTypePattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsUnaryPattern([NotNullWhen(true)] SyntaxNode? node);
        bool IsVarPattern([NotNullWhen(true)] SyntaxNode? node);

        SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node);
        SyntaxNode GetTypeOfTypePattern(SyntaxNode node);

        void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen);
        void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation);
        void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode? type, out SyntaxNode? positionalPart, out SyntaxNode? propertyPart, out SyntaxNode? designation);
        void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern);

        bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        SyntaxTokenList GetModifiers(SyntaxNode? node);

        // Violation.  WithXXX methods should not be here, but should be in SyntaxGenerator.
        [return: NotNullIfNotNull("node")]
        SyntaxNode? WithModifiers(SyntaxNode? node, SyntaxTokenList modifiers);

        // Violation.  This is a feature level API.
        Location GetDeconstructionReferenceLocation(SyntaxNode node);

        // Violation.  This is a feature level API.
        SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token);

        bool IsParameterNameXmlElementSyntax([NotNullWhen(true)] SyntaxNode? node);

        SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        #region IsXXX members

        bool IsAnonymousFunctionExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsBaseNamespaceDeclaration([NotNullWhen(true)] SyntaxNode? node);
        bool IsBinaryExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        bool IsSimpleName([NotNullWhen(true)] SyntaxNode? node);

        bool IsNamedMemberInitializer([NotNullWhen(true)] SyntaxNode? node);
        bool IsElementAccessInitializer([NotNullWhen(true)] SyntaxNode? node);
        bool IsObjectMemberInitializer([NotNullWhen(true)] SyntaxNode? node);
        bool IsObjectCollectionInitializer([NotNullWhen(true)] SyntaxNode? node);

        #endregion

        #region GetPartsOfXXX members

        void GetPartsOfAnyIsTypeExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode type);
        void GetPartsOfBaseNamespaceDeclaration(SyntaxNode node, out SyntaxNode name, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> members);
        void GetPartsOfBaseObjectCreationExpression(SyntaxNode node, out SyntaxNode? argumentList, out SyntaxNode? initializer);
        void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);
        void GetPartsOfCompilationUnit(SyntaxNode node, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> attributeLists, out SyntaxList<SyntaxNode> members);
        void GetPartsOfConditionalAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull);
        void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);
        void GetPartsOfGenericName(SyntaxNode node, out SyntaxToken identifier, out SeparatedSyntaxList<SyntaxNode> typeArguments);
        void GetPartsOfInterpolationExpression(SyntaxNode node, out SyntaxToken stringStartToken, out SyntaxList<SyntaxNode> contents, out SyntaxToken stringEndToken);
        void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode? argumentList);
        void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right);
        void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name);
        void GetPartsOfNamedMemberInitializer(SyntaxNode node, out SyntaxNode name, out SyntaxNode expression);
        void GetPartsOfObjectCreationExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode? argumentList, out SyntaxNode? initializer);
        void GetPartsOfParameter(SyntaxNode node, out SyntaxToken identifier, out SyntaxNode? @default);
        void GetPartsOfParenthesizedExpression(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen);
        void GetPartsOfPrefixUnaryExpression(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode operand);
        void GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken dotToken, out SyntaxNode right);
        void GetPartsOfUsingAliasDirective(SyntaxNode node, out SyntaxToken globalKeyword, out SyntaxToken alias, out SyntaxNode name);

        #endregion

        #region GetXXXOfYYYMembers

        // note: this is only for nodes that have a single child nodes.  If a node has multiple child nodes, then
        // ISyntaxFacts should have a GetPartsOfXXX helper instead, and GetXXXOfYYY should be built off of that
        // inside ISyntaxFactsExtensions

        SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);
        SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);
        SyntaxNode? GetExpressionOfReturnStatement(SyntaxNode node);
        SyntaxNode GetExpressionOfThrowExpression(SyntaxNode node);
        SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node);

        SeparatedSyntaxList<SyntaxNode> GetInitializersOfObjectMemberInitializer(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetExpressionsOfObjectCollectionInitializer(SyntaxNode node);

        #endregion
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
