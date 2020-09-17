// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpSyntaxFacts : AbstractSyntaxFacts, ISyntaxFacts
    {
        internal static readonly CSharpSyntaxFacts Instance = new CSharpSyntaxFacts();

        protected CSharpSyntaxFacts()
        {
        }

        public bool IsCaseSensitive => true;

        public StringComparer StringComparer { get; } = StringComparer.Ordinal;

        public SyntaxTrivia ElasticMarker
            => SyntaxFactory.ElasticMarker;

        public SyntaxTrivia ElasticCarriageReturnLineFeed
            => SyntaxFactory.ElasticCarriageReturnLineFeed;

        public override ISyntaxKinds SyntaxKinds { get; } = CSharpSyntaxKinds.Instance;

        protected override IDocumentationCommentService DocumentationCommentService
            => CSharpDocumentationCommentService.Instance;

        public bool SupportsIndexingInitializer(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp6;

        public bool SupportsThrowExpression(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public bool SupportsLocalFunctionDeclaration(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public SyntaxToken ParseToken(string text)
            => SyntaxFactory.ParseToken(text);

        public SyntaxTriviaList ParseLeadingTrivia(string text)
            => SyntaxFactory.ParseLeadingTrivia(text);

        public string EscapeIdentifier(string identifier)
        {
            var nullIndex = identifier.IndexOf('\0');
            if (nullIndex >= 0)
            {
                identifier = identifier.Substring(0, nullIndex);
            }

            var needsEscaping = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None;
            return needsEscaping ? "@" + identifier : identifier;
        }

        public bool IsVerbatimIdentifier(SyntaxToken token)
            => token.IsVerbatimIdentifier();

        public bool IsOperator(SyntaxToken token)
        {
            var kind = token.Kind();

            return
                (SyntaxFacts.IsAnyUnaryExpression(kind) &&
                    (token.Parent is PrefixUnaryExpressionSyntax || token.Parent is PostfixUnaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsBinaryExpression(kind) && (token.Parent is BinaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsAssignmentExpressionOperatorToken(kind) && token.Parent is AssignmentExpressionSyntax);
        }

        public bool IsReservedKeyword(SyntaxToken token)
            => SyntaxFacts.IsReservedKeyword(token.Kind());

        public bool IsContextualKeyword(SyntaxToken token)
            => SyntaxFacts.IsContextualKeyword(token.Kind());

        public bool IsPreprocessorKeyword(SyntaxToken token)
            => SyntaxFacts.IsPreprocessorKeyword(token.Kind());

        public bool IsPreProcessorDirectiveContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => syntaxTree.IsPreProcessorDirectiveContext(
                position, syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true), cancellationToken);

        public bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            return syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken);
        }

        public bool IsDirective(SyntaxNode node)
            => node is DirectiveTriviaSyntax;

        public bool TryGetExternalSourceInfo(SyntaxNode node, out ExternalSourceInfo info)
        {
            if (node is LineDirectiveTriviaSyntax lineDirective)
            {
                if (lineDirective.Line.Kind() == SyntaxKind.DefaultKeyword)
                {
                    info = new ExternalSourceInfo(null, ends: true);
                    return true;
                }
                else if (lineDirective.Line.Kind() == SyntaxKind.NumericLiteralToken &&
                    lineDirective.Line.Value is int)
                {
                    info = new ExternalSourceInfo((int)lineDirective.Line.Value, false);
                    return true;
                }
            }

            info = default;
            return false;
        }

        public bool IsRightSideOfQualifiedName(SyntaxNode node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsRightSideOfQualifiedName();
        }

#nullable enable

        public bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsSimpleMemberAccessExpressionName();
        }

        public bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node;

        public bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsMemberBindingExpressionName();
        }

        public SyntaxNode? GetStandaloneExpression(SyntaxNode? node)
            => node is ExpressionSyntax expression ? SyntaxFactory.GetStandaloneExpression(expression) : node;

        public SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node)
            => node.GetRootConditionalAccessExpression();

#nullable restore

        public bool IsObjectCreationExpressionType(SyntaxNode node)
            => node.IsParentKind(SyntaxKind.ObjectCreationExpression, out ObjectCreationExpressionSyntax objectCreation) &&
               objectCreation.Type == node;

        public bool IsDeclarationExpression(SyntaxNode node)
            => node is DeclarationExpressionSyntax;

        public bool IsAttributeName(SyntaxNode node)
            => SyntaxFacts.IsAttributeName(node);

        public bool IsAnonymousFunction(SyntaxNode node)
        {
            return node is ParenthesizedLambdaExpressionSyntax ||
                node is SimpleLambdaExpressionSyntax ||
                node is AnonymousMethodExpressionSyntax;
        }

        public bool IsNamedArgument(SyntaxNode node)
            => node is ArgumentSyntax arg && arg.NameColon != null;

        public bool IsNameOfNamedArgument(SyntaxNode node)
            => node.CheckParent<NameColonSyntax>(p => p.Name == node);

        public SyntaxToken? GetNameOfParameter(SyntaxNode node)
            => (node as ParameterSyntax)?.Identifier;

        public SyntaxNode GetDefaultOfParameter(SyntaxNode node)
            => (node as ParameterSyntax)?.Default;

        public SyntaxNode GetParameterList(SyntaxNode node)
            => node.GetParameterList();

        public bool IsParameterList(SyntaxNode node)
            => node.IsKind(SyntaxKind.ParameterList, SyntaxKind.BracketedParameterList);

        public SyntaxToken GetIdentifierOfGenericName(SyntaxNode genericName)
        {
            return genericName is GenericNameSyntax csharpGenericName
                ? csharpGenericName.Identifier
                : default;
        }

        public bool IsUsingDirectiveName(SyntaxNode node)
            => node.IsParentKind(SyntaxKind.UsingDirective, out UsingDirectiveSyntax usingDirective) &&
               usingDirective.Name == node;

        public bool IsUsingAliasDirective(SyntaxNode node)
            => node is UsingDirectiveSyntax usingDirectiveNode && usingDirectiveNode.Alias != null;

        public bool IsDeconstructionForEachStatement(SyntaxNode node)
            => node is ForEachVariableStatementSyntax;

        public bool IsDeconstructionAssignment(SyntaxNode node)
            => node is AssignmentExpressionSyntax assignment && assignment.IsDeconstruction();

        public Location GetDeconstructionReferenceLocation(SyntaxNode node)
        {
            return node switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left.GetLocation(),
                ForEachVariableStatementSyntax @foreach => @foreach.Variable.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind()),
            };
        }

        public bool IsStatement(SyntaxNode node)
           => node is StatementSyntax;

        public bool IsExecutableStatement(SyntaxNode node)
            => node is StatementSyntax;

        public bool IsMethodBody(SyntaxNode node)
        {
            if (node is BlockSyntax ||
                node is ArrowExpressionClauseSyntax)
            {
                return node.Parent is BaseMethodDeclarationSyntax ||
                       node.Parent is AccessorDeclarationSyntax;
            }

            return false;
        }

        public SyntaxNode GetExpressionOfReturnStatement(SyntaxNode node)
            => (node as ReturnStatementSyntax)?.Expression;

        public bool IsThisConstructorInitializer(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.ThisConstructorInitializer, out ConstructorInitializerSyntax constructorInit) &&
               constructorInit.ThisOrBaseKeyword == token;

        public bool IsBaseConstructorInitializer(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.BaseConstructorInitializer, out ConstructorInitializerSyntax constructorInit) &&
               constructorInit.ThisOrBaseKeyword == token;

        public bool IsQueryKeyword(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.InKeyword:
                    return token.Parent is QueryClauseSyntax;
                case SyntaxKind.ByKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.SelectKeyword:
                    return token.Parent is SelectOrGroupClauseSyntax;
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                    return token.Parent is OrderingSyntax;
                case SyntaxKind.IntoKeyword:
                    return token.Parent.IsKind(SyntaxKind.JoinIntoClause, SyntaxKind.QueryContinuation);
                default:
                    return false;
            }
        }

        public bool IsThrowExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.ThrowExpression;

        public bool IsPredefinedType(SyntaxToken token)
            => TryGetPredefinedType(token, out _);

        public bool IsPredefinedType(SyntaxToken token, PredefinedType type)
            => TryGetPredefinedType(token, out var actualType) && actualType == type;

        public bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type)
        {
            type = GetPredefinedType(token);
            return type != PredefinedType.None;
        }

        private static PredefinedType GetPredefinedType(SyntaxToken token)
        {
            return (SyntaxKind)token.RawKind switch
            {
                SyntaxKind.BoolKeyword => PredefinedType.Boolean,
                SyntaxKind.ByteKeyword => PredefinedType.Byte,
                SyntaxKind.SByteKeyword => PredefinedType.SByte,
                SyntaxKind.IntKeyword => PredefinedType.Int32,
                SyntaxKind.UIntKeyword => PredefinedType.UInt32,
                SyntaxKind.ShortKeyword => PredefinedType.Int16,
                SyntaxKind.UShortKeyword => PredefinedType.UInt16,
                SyntaxKind.LongKeyword => PredefinedType.Int64,
                SyntaxKind.ULongKeyword => PredefinedType.UInt64,
                SyntaxKind.FloatKeyword => PredefinedType.Single,
                SyntaxKind.DoubleKeyword => PredefinedType.Double,
                SyntaxKind.DecimalKeyword => PredefinedType.Decimal,
                SyntaxKind.StringKeyword => PredefinedType.String,
                SyntaxKind.CharKeyword => PredefinedType.Char,
                SyntaxKind.ObjectKeyword => PredefinedType.Object,
                SyntaxKind.VoidKeyword => PredefinedType.Void,
                _ => PredefinedType.None,
            };
        }

        public bool IsPredefinedOperator(SyntaxToken token)
            => TryGetPredefinedOperator(token, out var actualOperator) && actualOperator != PredefinedOperator.None;

        public bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op)
            => TryGetPredefinedOperator(token, out var actualOperator) && actualOperator == op;

        public bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op)
        {
            op = GetPredefinedOperator(token);
            return op != PredefinedOperator.None;
        }

        private static PredefinedOperator GetPredefinedOperator(SyntaxToken token)
        {
            switch ((SyntaxKind)token.RawKind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.PlusEqualsToken:
                    return PredefinedOperator.Addition;

                case SyntaxKind.MinusToken:
                case SyntaxKind.MinusEqualsToken:
                    return PredefinedOperator.Subtraction;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandEqualsToken:
                    return PredefinedOperator.BitwiseAnd;

                case SyntaxKind.BarToken:
                case SyntaxKind.BarEqualsToken:
                    return PredefinedOperator.BitwiseOr;

                case SyntaxKind.MinusMinusToken:
                    return PredefinedOperator.Decrement;

                case SyntaxKind.PlusPlusToken:
                    return PredefinedOperator.Increment;

                case SyntaxKind.SlashToken:
                case SyntaxKind.SlashEqualsToken:
                    return PredefinedOperator.Division;

                case SyntaxKind.EqualsEqualsToken:
                    return PredefinedOperator.Equality;

                case SyntaxKind.CaretToken:
                case SyntaxKind.CaretEqualsToken:
                    return PredefinedOperator.ExclusiveOr;

                case SyntaxKind.GreaterThanToken:
                    return PredefinedOperator.GreaterThan;

                case SyntaxKind.GreaterThanEqualsToken:
                    return PredefinedOperator.GreaterThanOrEqual;

                case SyntaxKind.ExclamationEqualsToken:
                    return PredefinedOperator.Inequality;

                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return PredefinedOperator.LeftShift;

                case SyntaxKind.LessThanEqualsToken:
                    return PredefinedOperator.LessThanOrEqual;

                case SyntaxKind.AsteriskToken:
                case SyntaxKind.AsteriskEqualsToken:
                    return PredefinedOperator.Multiplication;

                case SyntaxKind.PercentToken:
                case SyntaxKind.PercentEqualsToken:
                    return PredefinedOperator.Modulus;

                case SyntaxKind.ExclamationToken:
                case SyntaxKind.TildeToken:
                    return PredefinedOperator.Complement;

                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return PredefinedOperator.RightShift;
            }

            return PredefinedOperator.None;
        }

        public string GetText(int kind)
            => SyntaxFacts.GetText((SyntaxKind)kind);

        public bool IsIdentifierStartCharacter(char c)
            => SyntaxFacts.IsIdentifierStartCharacter(c);

        public bool IsIdentifierPartCharacter(char c)
            => SyntaxFacts.IsIdentifierPartCharacter(c);

        public bool IsIdentifierEscapeCharacter(char c)
            => c == '@';

        public bool IsValidIdentifier(string identifier)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            return this.IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length;
        }

        public bool IsVerbatimIdentifier(string identifier)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            return this.IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length && token.IsVerbatimIdentifier();
        }

        public bool IsTypeCharacter(char c) => false;

        public bool IsStartOfUnicodeEscapeSequence(char c)
            => c == '\\';

        public bool IsLiteral(SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.InterpolatedStringStartToken:
                case SyntaxKind.InterpolatedStringEndToken:
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                case SyntaxKind.InterpolatedStringTextToken:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token)
            => token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken);

        public bool IsNumericLiteralExpression(SyntaxNode node)
            => node?.IsKind(SyntaxKind.NumericLiteralExpression) == true;

        public bool IsTypeNamedVarInVariableOrFieldDeclaration(SyntaxToken token, SyntaxNode parent)
        {
            var typedToken = token;
            var typedParent = parent;

            if (typedParent.IsKind(SyntaxKind.IdentifierName))
            {
                TypeSyntax declaredType = null;
                if (typedParent.IsParentKind(SyntaxKind.VariableDeclaration, out VariableDeclarationSyntax varDecl))
                {
                    declaredType = varDecl.Type;
                }
                else if (typedParent.IsParentKind(SyntaxKind.FieldDeclaration, out FieldDeclarationSyntax fieldDecl))
                {
                    declaredType = fieldDecl.Declaration.Type;
                }

                return declaredType == typedParent && typedToken.ValueText == "var";
            }

            return false;
        }

        public bool IsTypeNamedDynamic(SyntaxToken token, SyntaxNode parent)
        {

            if (parent is ExpressionSyntax typedParent)
            {
                if (SyntaxFacts.IsInTypeOnlyContext(typedParent) &&
                    typedParent.IsKind(SyntaxKind.IdentifierName) &&
                    token.ValueText == "dynamic")
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsBindableToken(SyntaxToken token)
        {
            if (this.IsWord(token) || this.IsLiteral(token) || this.IsOperator(token))
            {
                switch ((SyntaxKind)token.RawKind)
                {
                    case SyntaxKind.DelegateKeyword:
                    case SyntaxKind.VoidKeyword:
                        return false;
                }

                return true;
            }

            // In the order by clause a comma might be bound to ThenBy or ThenByDescending
            if (token.Kind() == SyntaxKind.CommaToken && token.Parent.Kind() == SyntaxKind.OrderByClause)
            {
                return true;
            }

            return false;
        }

        public void GetPartsOfConditionalAccessExpression(
            SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull)
        {
            var conditionalAccess = (ConditionalAccessExpressionSyntax)node;
            expression = conditionalAccess.Expression;
            operatorToken = conditionalAccess.OperatorToken;
            whenNotNull = conditionalAccess.WhenNotNull;
        }

        public bool IsPostfixUnaryExpression(SyntaxNode node)
            => node is PostfixUnaryExpressionSyntax;

#nullable enable
        public bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is MemberBindingExpressionSyntax;
#nullable restore

        public bool IsPointerMemberAccessExpression(SyntaxNode node)
            => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.PointerMemberAccessExpression;

        public void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity)
        {
            name = null;
            arity = 0;

            if (node is SimpleNameSyntax simpleName)
            {
                name = simpleName.Identifier.ValueText;
                arity = simpleName.Arity;
            }
        }

        public bool LooksGeneric(SyntaxNode simpleName)
            => simpleName.IsKind(SyntaxKind.GenericName) ||
               simpleName.GetLastToken().GetNextToken().Kind() == SyntaxKind.LessThanToken;

        public SyntaxNode GetTargetOfMemberBinding(SyntaxNode node)
            => (node as MemberBindingExpressionSyntax).GetParentConditionalAccessExpression()?.Expression;

        public SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget)
            => (node as MemberAccessExpressionSyntax)?.Expression;

        public void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
        {
            var elementAccess = node as ElementAccessExpressionSyntax;
            expression = elementAccess?.Expression;
            argumentList = elementAccess?.ArgumentList;
        }

        public SyntaxNode GetExpressionOfInterpolation(SyntaxNode node)
            => (node as InterpolationSyntax)?.Expression;

        public bool IsInStaticContext(SyntaxNode node)
            => node.IsInStaticContext();

        public bool IsInNamespaceOrTypeContext(SyntaxNode node)
            => SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);

        public bool IsBaseTypeList(SyntaxNode node)
            => node.IsKind(SyntaxKind.BaseList);

        public SyntaxNode GetExpressionOfArgument(SyntaxNode node)
            => (node as ArgumentSyntax)?.Expression;

        public RefKind GetRefKindOfArgument(SyntaxNode node)
            => (node as ArgumentSyntax).GetRefKind();

        public bool IsArgument(SyntaxNode node)
            => node.IsKind(SyntaxKind.Argument);

        public bool IsSimpleArgument(SyntaxNode node)
        {
            return node is ArgumentSyntax argument &&
                   argument.RefOrOutKeyword.Kind() == SyntaxKind.None &&
                   argument.NameColon == null;
        }

        public bool IsInConstantContext(SyntaxNode node)
            => (node as ExpressionSyntax).IsInConstantContext();

        public bool IsInConstructor(SyntaxNode node)
            => node.GetAncestor<ConstructorDeclarationSyntax>() != null;

        public bool IsUnsafeContext(SyntaxNode node)
            => node.IsUnsafeContext();

        public SyntaxNode GetNameOfAttribute(SyntaxNode node)
            => ((AttributeSyntax)node).Name;

        public SyntaxNode GetExpressionOfParenthesizedExpression(SyntaxNode node)
            => ((ParenthesizedExpressionSyntax)node).Expression;

        public bool IsAttributeNamedArgumentIdentifier(SyntaxNode node)
            => (node as IdentifierNameSyntax).IsAttributeNamedArgumentIdentifier();

        public SyntaxNode GetContainingTypeDeclaration(SyntaxNode root, int position)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (position < 0 || position > root.Span.End)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return root
                .FindToken(position)
                .GetAncestors<SyntaxNode>()
                .FirstOrDefault(n => n is BaseTypeDeclarationSyntax || n is DelegateDeclarationSyntax);
        }

        public SyntaxNode GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode node)
            => throw ExceptionUtilities.Unreachable;

        public SyntaxToken FindTokenOnLeftOfPosition(
            SyntaxNode node, int position, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return node.FindTokenOnLeftOfPosition(position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        public SyntaxToken FindTokenOnRightOfPosition(
            SyntaxNode node, int position, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return node.FindTokenOnRightOfPosition(position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        public bool IsNameOfSubpattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.IdentifierName) &&
               node.IsParentKind(SyntaxKind.NameColon) &&
               node.Parent.IsParentKind(SyntaxKind.Subpattern);

        public bool IsPropertyPatternClause(SyntaxNode node)
            => node.Kind() == SyntaxKind.PropertyPatternClause;

        public bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node)
            => IsObjectInitializerNamedAssignmentIdentifier(node, out _);

        public bool IsObjectInitializerNamedAssignmentIdentifier(
            SyntaxNode node, out SyntaxNode initializedInstance)
        {
            initializedInstance = null;
            if (node is IdentifierNameSyntax identifier &&
                identifier.IsLeftSideOfAssignExpression() &&
                identifier.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression))
            {
                var objectInitializer = identifier.Parent.Parent;
                if (objectInitializer.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    initializedInstance = objectInitializer.Parent;
                    return true;
                }
                else if (objectInitializer.IsParentKind(SyntaxKind.SimpleAssignmentExpression, out AssignmentExpressionSyntax assignment))
                {
                    initializedInstance = assignment.Left;
                    return true;
                }
            }

            return false;
        }

        public bool IsElementAccessExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.ElementAccessExpression;

        public SyntaxNode ConvertToSingleLine(SyntaxNode node, bool useElasticTrivia = false)
            => node.ConvertToSingleLine(useElasticTrivia);

        public void GetPartsOfParenthesizedExpression(
            SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen)
        {
            var parenthesizedExpression = (ParenthesizedExpressionSyntax)node;
            openParen = parenthesizedExpression.OpenParenToken;
            expression = parenthesizedExpression.Expression;
            closeParen = parenthesizedExpression.CloseParenToken;
        }

        public bool IsIndexerMemberCRef(SyntaxNode node)
            => node.Kind() == SyntaxKind.IndexerMemberCref;

        public SyntaxNode GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true)
        {
            Contract.ThrowIfNull(root, "root");
            Contract.ThrowIfTrue(position < 0 || position > root.FullSpan.End, "position");

            var end = root.FullSpan.End;
            if (end == 0)
            {
                // empty file
                return null;
            }

            // make sure position doesn't touch end of root
            position = Math.Min(position, end - 1);

            var node = root.FindToken(position).Parent;
            while (node != null)
            {
                if (useFullSpan || node.Span.Contains(position))
                {
                    var kind = node.Kind();
                    if ((kind != SyntaxKind.GlobalStatement) && (kind != SyntaxKind.IncompleteMember) && (node is MemberDeclarationSyntax))
                    {
                        return node;
                    }
                }

                node = node.Parent;
            }

            return null;
        }

        public bool IsMethodLevelMember(SyntaxNode node)
        {
            return node is BaseMethodDeclarationSyntax ||
                node is BasePropertyDeclarationSyntax ||
                node is EnumMemberDeclarationSyntax ||
                node is BaseFieldDeclarationSyntax;
        }

        public bool IsTopLevelNodeWithMembers(SyntaxNode node)
        {
            return node is NamespaceDeclarationSyntax ||
                   node is TypeDeclarationSyntax ||
                   node is EnumDeclarationSyntax;
        }

        private const string dotToken = ".";

        public string GetDisplayName(SyntaxNode node, DisplayNameOptions options, string rootNamespace = null)
        {
            if (node == null)
            {
                return string.Empty;
            }

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            // return type
            var memberDeclaration = node as MemberDeclarationSyntax;
            if ((options & DisplayNameOptions.IncludeType) != 0)
            {
                var type = memberDeclaration.GetMemberType();
                if (type != null && !type.IsMissing)
                {
                    builder.Append(type);
                    builder.Append(' ');
                }
            }

            var names = ArrayBuilder<string>.GetInstance();
            // containing type(s)
            var parent = node.GetAncestor<TypeDeclarationSyntax>() ?? node.Parent;
            while (parent is TypeDeclarationSyntax)
            {
                names.Push(GetName(parent, options));
                parent = parent.Parent;
            }

            // containing namespace(s) in source (if any)
            if ((options & DisplayNameOptions.IncludeNamespaces) != 0)
            {
                while (parent != null && parent.Kind() == SyntaxKind.NamespaceDeclaration)
                {
                    names.Add(GetName(parent, options));
                    parent = parent.Parent;
                }
            }

            while (!names.IsEmpty())
            {
                var name = names.Pop();
                if (name != null)
                {
                    builder.Append(name);
                    builder.Append(dotToken);
                }
            }

            // name (including generic type parameters)
            builder.Append(GetName(node, options));

            // parameter list (if any)
            if ((options & DisplayNameOptions.IncludeParameters) != 0)
            {
                builder.Append(memberDeclaration.GetParameterList());
            }

            return pooled.ToStringAndFree();
        }

        private static string GetName(SyntaxNode node, DisplayNameOptions options)
        {
            const string missingTokenPlaceholder = "?";

            switch (node.Kind())
            {
                case SyntaxKind.CompilationUnit:
                    return null;
                case SyntaxKind.IdentifierName:
                    var identifier = ((IdentifierNameSyntax)node).Identifier;
                    return identifier.IsMissing ? missingTokenPlaceholder : identifier.Text;
                case SyntaxKind.IncompleteMember:
                    return missingTokenPlaceholder;
                case SyntaxKind.NamespaceDeclaration:
                    return GetName(((NamespaceDeclarationSyntax)node).Name, options);
                case SyntaxKind.QualifiedName:
                    var qualified = (QualifiedNameSyntax)node;
                    return GetName(qualified.Left, options) + dotToken + GetName(qualified.Right, options);
            }

            string name = null;
            if (node is MemberDeclarationSyntax memberDeclaration)
            {
                if (memberDeclaration.Kind() == SyntaxKind.ConversionOperatorDeclaration)
                {
                    name = (memberDeclaration as ConversionOperatorDeclarationSyntax)?.Type.ToString();
                }
                else
                {
                    var nameToken = memberDeclaration.GetNameToken();
                    if (nameToken != default)
                    {
                        name = nameToken.IsMissing ? missingTokenPlaceholder : nameToken.Text;
                        if (memberDeclaration.Kind() == SyntaxKind.DestructorDeclaration)
                        {
                            name = "~" + name;
                        }
                        if ((options & DisplayNameOptions.IncludeTypeParameters) != 0)
                        {
                            var pooled = PooledStringBuilder.GetInstance();
                            var builder = pooled.Builder;
                            builder.Append(name);
                            AppendTypeParameterList(builder, memberDeclaration.GetTypeParameterList());
                            name = pooled.ToStringAndFree();
                        }
                    }
                    else
                    {
                        Debug.Assert(memberDeclaration.Kind() == SyntaxKind.IncompleteMember);
                        name = "?";
                    }
                }
            }
            else
            {
                if (node is VariableDeclaratorSyntax fieldDeclarator)
                {
                    var nameToken = fieldDeclarator.Identifier;
                    if (nameToken != default)
                    {
                        name = nameToken.IsMissing ? missingTokenPlaceholder : nameToken.Text;
                    }
                }
            }
            Debug.Assert(name != null, "Unexpected node type " + node.Kind());
            return name;
        }

        private static void AppendTypeParameterList(StringBuilder builder, TypeParameterListSyntax typeParameterList)
        {
            if (typeParameterList != null && typeParameterList.Parameters.Count > 0)
            {
                builder.Append('<');
                builder.Append(typeParameterList.Parameters[0].Identifier.ValueText);
                for (var i = 1; i < typeParameterList.Parameters.Count; i++)
                {
                    builder.Append(", ");
                    builder.Append(typeParameterList.Parameters[i].Identifier.ValueText);
                }
                builder.Append('>');
            }
        }

        public List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode root)
        {
            var list = new List<SyntaxNode>();
            AppendMembers(root, list, topLevel: true, methodLevel: true);
            return list;
        }

        public List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root)
        {
            var list = new List<SyntaxNode>();
            AppendMembers(root, list, topLevel: false, methodLevel: true);
            return list;
        }

        public bool IsClassDeclaration(SyntaxNode node)
            => node?.Kind() == SyntaxKind.ClassDeclaration;

        public bool IsNamespaceDeclaration(SyntaxNode node)
            => node?.Kind() == SyntaxKind.NamespaceDeclaration;

        public SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration)
            => ((TypeDeclarationSyntax)typeDeclaration).Members;

        public SyntaxList<SyntaxNode> GetMembersOfNamespaceDeclaration(SyntaxNode namespaceDeclaration)
            => ((NamespaceDeclarationSyntax)namespaceDeclaration).Members;

        public SyntaxList<SyntaxNode> GetMembersOfCompilationUnit(SyntaxNode compilationUnit)
            => ((CompilationUnitSyntax)compilationUnit).Members;

        private void AppendMembers(SyntaxNode node, List<SyntaxNode> list, bool topLevel, bool methodLevel)
        {
            Debug.Assert(topLevel || methodLevel);

            foreach (var member in node.GetMembers())
            {
                if (IsTopLevelNodeWithMembers(member))
                {
                    if (topLevel)
                    {
                        list.Add(member);
                    }

                    AppendMembers(member, list, topLevel, methodLevel);
                    continue;
                }

                if (methodLevel && IsMethodLevelMember(member))
                {
                    list.Add(member);
                }
            }
        }

        public TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node)
        {
            if (node.Span.IsEmpty)
            {
                return default;
            }

            var member = GetContainingMemberDeclaration(node, node.SpanStart);
            if (member == null)
            {
                return default;
            }

            // TODO: currently we only support method for now
            if (member is BaseMethodDeclarationSyntax method)
            {
                if (method.Body == null)
                {
                    return default;
                }

                return GetBlockBodySpan(method.Body);
            }

            return default;
        }

        public bool ContainsInMemberBody(SyntaxNode node, TextSpan span)
        {
            switch (node)
            {
                case ConstructorDeclarationSyntax constructor:
                    return (constructor.Body != null && GetBlockBodySpan(constructor.Body).Contains(span)) ||
                           (constructor.Initializer != null && constructor.Initializer.Span.Contains(span));
                case BaseMethodDeclarationSyntax method:
                    return method.Body != null && GetBlockBodySpan(method.Body).Contains(span);
                case BasePropertyDeclarationSyntax property:
                    return property.AccessorList != null && property.AccessorList.Span.Contains(span);
                case EnumMemberDeclarationSyntax @enum:
                    return @enum.EqualsValue != null && @enum.EqualsValue.Span.Contains(span);
                case BaseFieldDeclarationSyntax field:
                    return field.Declaration != null && field.Declaration.Span.Contains(span);
            }

            return false;
        }

        private static TextSpan GetBlockBodySpan(BlockSyntax body)
            => TextSpan.FromBounds(body.OpenBraceToken.Span.End, body.CloseBraceToken.SpanStart);

#nullable enable

        public SyntaxNode? TryGetBindableParent(SyntaxToken token)
        {
            var node = token.Parent;
            while (node != null)
            {
                var parent = node.Parent;

                // If this node is on the left side of a member access expression, don't ascend
                // further or we'll end up binding to something else.
                if (parent is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Expression == node)
                    {
                        break;
                    }
                }

                // If this node is on the left side of a qualified name, don't ascend
                // further or we'll end up binding to something else.
                if (parent is QualifiedNameSyntax qualifiedName)
                {
                    if (qualifiedName.Left == node)
                    {
                        break;
                    }
                }

                // If this node is on the left side of a alias-qualified name, don't ascend
                // further or we'll end up binding to something else.
                if (parent is AliasQualifiedNameSyntax aliasQualifiedName)
                {
                    if (aliasQualifiedName.Alias == node)
                    {
                        break;
                    }
                }

                // If this node is the type of an object creation expression, return the
                // object creation expression.
                if (parent is ObjectCreationExpressionSyntax objectCreation)
                {
                    if (objectCreation.Type == node)
                    {
                        node = parent;
                        break;
                    }
                }

                // The inside of an interpolated string is treated as its own token so we
                // need to force navigation to the parent expression syntax.
                if (node is InterpolatedStringTextSyntax && parent is InterpolatedStringExpressionSyntax)
                {
                    node = parent;
                    break;
                }

                // If this node is not parented by a name, we're done.
                if (!(parent is NameSyntax))
                {
                    break;
                }

                node = parent;
            }

            // Patterns are never bindable (though their constituent types/exprs may be).
            return node is PatternSyntax ? null : node;
        }

#nullable disable

        public IEnumerable<SyntaxNode> GetConstructors(SyntaxNode root, CancellationToken cancellationToken)
        {
            if (!(root is CompilationUnitSyntax compilationUnit))
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            var constructors = new List<SyntaxNode>();
            AppendConstructors(compilationUnit.Members, constructors, cancellationToken);
            return constructors;
        }

        private void AppendConstructors(SyntaxList<MemberDeclarationSyntax> members, List<SyntaxNode> constructors, CancellationToken cancellationToken)
        {
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (member)
                {
                    case ConstructorDeclarationSyntax constructor:
                        constructors.Add(constructor);
                        continue;
                    case NamespaceDeclarationSyntax @namespace:
                        AppendConstructors(@namespace.Members, constructors, cancellationToken);
                        break;
                    case ClassDeclarationSyntax @class:
                        AppendConstructors(@class.Members, constructors, cancellationToken);
                        break;
                    case StructDeclarationSyntax @struct:
                        AppendConstructors(@struct.Members, constructors, cancellationToken);
                        break;
                }
            }
        }

        public bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace)
        {
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                var tuple = token.Parent.GetBraces();

                openBrace = tuple.openBrace;
                return openBrace.Kind() == SyntaxKind.OpenBraceToken;
            }

            openBrace = default;
            return false;
        }

        public TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
            if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
            {
                return trivia.FullSpan;
            }

            var token = syntaxTree.FindTokenOrEndToken(position, cancellationToken);
            if (token.Kind() == SyntaxKind.EndOfFileToken)
            {
                var triviaList = token.LeadingTrivia;
                foreach (var triviaTok in triviaList.Reverse())
                {
                    if (triviaTok.Span.Contains(position))
                    {
                        return default;
                    }

                    if (triviaTok.Span.End < position)
                    {
                        if (!triviaTok.HasStructure)
                        {
                            return default;
                        }

                        var structure = triviaTok.GetStructure();
                        if (structure is BranchingDirectiveTriviaSyntax branch)
                        {
                            return !branch.IsActive || !branch.BranchTaken ? TextSpan.FromBounds(branch.FullSpan.Start, position) : default;
                        }
                    }
                }
            }

            return default;
        }

        public string GetNameForArgument(SyntaxNode argument)
            => (argument as ArgumentSyntax)?.NameColon?.Name.Identifier.ValueText ?? string.Empty;

        public string GetNameForAttributeArgument(SyntaxNode argument)
            => (argument as AttributeArgumentSyntax)?.NameEquals?.Name.Identifier.ValueText ?? string.Empty;

        public bool IsLeftSideOfDot(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfDot();

        public SyntaxNode GetRightSideOfDot(SyntaxNode node)
        {
            return (node as QualifiedNameSyntax)?.Right ??
                (node as MemberAccessExpressionSyntax)?.Name;
        }

        public SyntaxNode GetLeftSideOfDot(SyntaxNode node, bool allowImplicitTarget)
        {
            return (node as QualifiedNameSyntax)?.Left ??
                (node as MemberAccessExpressionSyntax)?.Expression;
        }

        public bool IsLeftSideOfExplicitInterfaceSpecifier(SyntaxNode node)
            => (node as NameSyntax).IsLeftSideOfExplicitInterfaceSpecifier();

        public bool IsLeftSideOfAssignment(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfAssignExpression();

        public bool IsLeftSideOfAnyAssignment(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfAnyAssignExpression();

        public bool IsLeftSideOfCompoundAssignment(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfCompoundAssignExpression();

        public SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node)
            => (node as AssignmentExpressionSyntax)?.Right;

        public bool IsInferredAnonymousObjectMemberDeclarator(SyntaxNode node)
            => node.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator, out AnonymousObjectMemberDeclaratorSyntax anonObject) &&
               anonObject.NameEquals == null;

        public bool IsOperandOfIncrementExpression(SyntaxNode node)
            => node.IsParentKind(SyntaxKind.PostIncrementExpression) ||
               node.IsParentKind(SyntaxKind.PreIncrementExpression);

        public static bool IsOperandOfDecrementExpression(SyntaxNode node)
            => node.IsParentKind(SyntaxKind.PostDecrementExpression) ||
               node.IsParentKind(SyntaxKind.PreDecrementExpression);

        public bool IsOperandOfIncrementOrDecrementExpression(SyntaxNode node)
            => IsOperandOfIncrementExpression(node) || IsOperandOfDecrementExpression(node);

        public SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString)
            => ((interpolatedString as InterpolatedStringExpressionSyntax)?.Contents).Value;

        public bool IsVerbatimStringLiteral(SyntaxToken token)
            => token.IsVerbatimStringLiteral();

        public bool IsNumericLiteral(SyntaxToken token)
            => token.Kind() == SyntaxKind.NumericLiteralToken;

        public void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
        {
            var invocation = (InvocationExpressionSyntax)node;
            expression = invocation.Expression;
            argumentList = invocation.ArgumentList;
        }

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode invocationExpression)
            => GetArgumentsOfArgumentList((invocationExpression as InvocationExpressionSyntax)?.ArgumentList);

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode objectCreationExpression)
            => GetArgumentsOfArgumentList((objectCreationExpression as ObjectCreationExpressionSyntax)?.ArgumentList);

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode argumentList)
            => (argumentList as BaseArgumentListSyntax)?.Arguments ?? default;

        public bool IsRegularComment(SyntaxTrivia trivia)
            => trivia.IsRegularComment();

        public bool IsDocumentationComment(SyntaxTrivia trivia)
            => trivia.IsDocComment();

        public bool IsElastic(SyntaxTrivia trivia)
            => trivia.IsElastic();

        public bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes)
            => trivia.IsPragmaDirective(out isDisable, out isActive, out errorCodes);

        public bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia;

        public bool IsDocumentationComment(SyntaxNode node)
            => SyntaxFacts.IsDocumentationCommentTrivia(node.Kind());

        public bool IsUsingOrExternOrImport(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.UsingDirective) ||
                   node.IsKind(SyntaxKind.ExternAliasDirective);
        }

        public bool IsGlobalAssemblyAttribute(SyntaxNode node)
            => IsGlobalAttribute(node, SyntaxKind.AssemblyKeyword);

        public bool IsGlobalModuleAttribute(SyntaxNode node)
            => IsGlobalAttribute(node, SyntaxKind.ModuleKeyword);

        private static bool IsGlobalAttribute(SyntaxNode node, SyntaxKind attributeTarget)
            => node.IsKind(SyntaxKind.Attribute) &&
               node.Parent.IsKind(SyntaxKind.AttributeList, out AttributeListSyntax attributeList) &&
               attributeList.Target?.Identifier.Kind() == attributeTarget;

        private static bool IsMemberDeclaration(SyntaxNode node)
        {
            // From the C# language spec:
            // class-member-declaration:
            //    constant-declaration
            //    field-declaration
            //    method-declaration
            //    property-declaration
            //    event-declaration
            //    indexer-declaration
            //    operator-declaration
            //    constructor-declaration
            //    destructor-declaration
            //    static-constructor-declaration
            //    type-declaration
            switch (node.Kind())
            {
                // Because fields declarations can define multiple symbols "public int a, b;"
                // We want to get the VariableDeclarator node inside the field declaration to print out the symbol for the name.
                case SyntaxKind.VariableDeclarator:
                    return node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) ||
                           node.Parent.Parent.IsKind(SyntaxKind.EventFieldDeclaration);

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsDeclaration(SyntaxNode node)
            => SyntaxFacts.IsNamespaceMemberDeclaration(node.Kind()) || IsMemberDeclaration(node);

        public bool IsTypeDeclaration(SyntaxNode node)
            => SyntaxFacts.IsTypeDeclaration(node.Kind());

        public SyntaxNode GetObjectCreationInitializer(SyntaxNode node)
            => ((ObjectCreationExpressionSyntax)node).Initializer;

        public SyntaxNode GetObjectCreationType(SyntaxNode node)
            => ((ObjectCreationExpressionSyntax)node).Type;

        public bool IsSimpleAssignmentStatement(SyntaxNode statement)
            => statement.IsKind(SyntaxKind.ExpressionStatement, out ExpressionStatementSyntax exprStatement) &&
               exprStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);

        public void GetPartsOfAssignmentStatement(
            SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            GetPartsOfAssignmentExpressionOrStatement(
                ((ExpressionStatementSyntax)statement).Expression, out left, out operatorToken, out right);
        }

        public void GetPartsOfAssignmentExpressionOrStatement(
            SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var expression = statement;
            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                expression = expressionStatement.Expression;
            }

            var assignment = (AssignmentExpressionSyntax)expression;
            left = assignment.Left;
            operatorToken = assignment.OperatorToken;
            right = assignment.Right;
        }

        public SyntaxNode GetNameOfMemberAccessExpression(SyntaxNode memberAccessExpression)
            => ((MemberAccessExpressionSyntax)memberAccessExpression).Name;

        public void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name)
        {
            var memberAccess = (MemberAccessExpressionSyntax)node;
            expression = memberAccess.Expression;
            operatorToken = memberAccess.OperatorToken;
            name = memberAccess.Name;
        }

        public SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node)
            => ((SimpleNameSyntax)node).Identifier;

        public SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclaratorSyntax)node).Identifier;

        public bool IsLocalFunctionStatement(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalFunctionStatement);

        public bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement)
        {
            return ((LocalDeclarationStatementSyntax)localDeclarationStatement).Declaration.Variables.Contains(
                (VariableDeclaratorSyntax)declarator);
        }

        public bool AreEquivalent(SyntaxToken token1, SyntaxToken token2)
            => SyntaxFactory.AreEquivalent(token1, token2);

        public bool AreEquivalent(SyntaxNode node1, SyntaxNode node2)
            => SyntaxFactory.AreEquivalent(node1, node2);

        public bool IsExpressionOfInvocationExpression(SyntaxNode node)
            => (node?.Parent as InvocationExpressionSyntax)?.Expression == node;

        public bool IsExpressionOfAwaitExpression(SyntaxNode node)
            => (node?.Parent as AwaitExpressionSyntax)?.Expression == node;

        public bool IsExpressionOfMemberAccessExpression(SyntaxNode node)
            => (node?.Parent as MemberAccessExpressionSyntax)?.Expression == node;

        public static SyntaxNode GetExpressionOfInvocationExpression(SyntaxNode node)
            => ((InvocationExpressionSyntax)node).Expression;

        public SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node)
            => ((AwaitExpressionSyntax)node).Expression;

        public bool IsExpressionOfForeach(SyntaxNode node)
            => node?.Parent is ForEachStatementSyntax foreachStatement && foreachStatement.Expression == node;

        public SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node)
            => ((ExpressionStatementSyntax)node).Expression;

        public bool IsBinaryExpression(SyntaxNode node)
            => node is BinaryExpressionSyntax;

        public bool IsIsExpression(SyntaxNode node)
            => node.IsKind(SyntaxKind.IsExpression);

        public void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var binaryExpression = (BinaryExpressionSyntax)node;
            left = binaryExpression.Left;
            operatorToken = binaryExpression.OperatorToken;
            right = binaryExpression.Right;
        }

        public void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse)
        {
            var conditionalExpression = (ConditionalExpressionSyntax)node;
            condition = conditionalExpression.Condition;
            whenTrue = conditionalExpression.WhenTrue;
            whenFalse = conditionalExpression.WhenFalse;
        }

        public SyntaxNode WalkDownParentheses(SyntaxNode node)
            => (node as ExpressionSyntax)?.WalkDownParentheses() ?? node;

        public void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
            out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode
        {
            var tupleExpression = (TupleExpressionSyntax)node;
            openParen = tupleExpression.OpenParenToken;
            arguments = (SeparatedSyntaxList<TArgumentSyntax>)(SeparatedSyntaxList<SyntaxNode>)tupleExpression.Arguments;
            closeParen = tupleExpression.CloseParenToken;
        }

        public SyntaxNode GetOperandOfPrefixUnaryExpression(SyntaxNode node)
            => ((PrefixUnaryExpressionSyntax)node).Operand;

        public SyntaxToken GetOperatorTokenOfPrefixUnaryExpression(SyntaxNode node)
            => ((PrefixUnaryExpressionSyntax)node).OperatorToken;

        public SyntaxNode GetNextExecutableStatement(SyntaxNode statement)
            => ((StatementSyntax)statement).GetNextStatement();

        public override bool IsSingleLineCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsSingleLineComment();

        public override bool IsMultiLineCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsMultiLineComment();

        public override bool IsSingleLineDocCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsSingleLineDocComment();

        public override bool IsMultiLineDocCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsMultiLineDocComment();

        public override bool IsShebangDirectiveTrivia(SyntaxTrivia trivia)
            => trivia.IsShebangDirective();

        public override bool IsPreprocessorDirective(SyntaxTrivia trivia)
            => SyntaxFacts.IsPreprocessorDirective(trivia.Kind());

        public bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, out SyntaxNode typeDeclaration)
        {
            var node = TryGetAncestorForLocation<BaseTypeDeclarationSyntax>(root, position);
            typeDeclaration = node;
            if (node == null)
                return false;

            var lastToken = (node as TypeDeclarationSyntax)?.TypeParameterList?.GetLastToken() ?? node.Identifier;
            if (fullHeader)
                lastToken = node.BaseList?.GetLastToken() ?? lastToken;

            return IsOnHeader(root, position, node, lastToken);
        }

        public bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, out SyntaxNode propertyDeclaration)
        {
            var node = TryGetAncestorForLocation<PropertyDeclarationSyntax>(root, position);
            propertyDeclaration = node;
            if (propertyDeclaration == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.Identifier);
        }

        public bool IsOnParameterHeader(SyntaxNode root, int position, out SyntaxNode parameter)
        {
            var node = TryGetAncestorForLocation<ParameterSyntax>(root, position);
            parameter = node;
            if (parameter == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node);
        }

        public bool IsOnMethodHeader(SyntaxNode root, int position, out SyntaxNode method)
        {
            var node = TryGetAncestorForLocation<MethodDeclarationSyntax>(root, position);
            method = node;
            if (method == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.ParameterList);
        }

        public bool IsOnLocalFunctionHeader(SyntaxNode root, int position, out SyntaxNode localFunction)
        {
            var node = TryGetAncestorForLocation<LocalFunctionStatementSyntax>(root, position);
            localFunction = node;
            if (localFunction == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.ParameterList);
        }

        public bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, out SyntaxNode localDeclaration)
        {
            var node = TryGetAncestorForLocation<LocalDeclarationStatementSyntax>(root, position);
            localDeclaration = node;
            if (localDeclaration == null)
            {
                return false;
            }

            var initializersExpressions = node.Declaration.Variables
                .Where(v => v.Initializer != null)
                .SelectAsArray(initializedV => initializedV.Initializer.Value);
            return IsOnHeader(root, position, node, node, holes: initializersExpressions);
        }

        public bool IsOnIfStatementHeader(SyntaxNode root, int position, out SyntaxNode ifStatement)
        {
            var node = TryGetAncestorForLocation<IfStatementSyntax>(root, position);
            ifStatement = node;
            if (ifStatement == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.CloseParenToken);
        }

        public bool IsOnWhileStatementHeader(SyntaxNode root, int position, out SyntaxNode whileStatement)
        {
            var node = TryGetAncestorForLocation<WhileStatementSyntax>(root, position);
            whileStatement = node;
            if (whileStatement == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.CloseParenToken);
        }

        public bool IsOnForeachHeader(SyntaxNode root, int position, out SyntaxNode foreachStatement)
        {
            var node = TryGetAncestorForLocation<ForEachStatementSyntax>(root, position);
            foreachStatement = node;
            if (foreachStatement == null)
            {
                return false;
            }

            return IsOnHeader(root, position, node, node.CloseParenToken);
        }

        public bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, out SyntaxNode typeDeclaration)
        {
            var token = root.FindToken(position);
            var typeDecl = token.GetAncestor<TypeDeclarationSyntax>();
            typeDeclaration = typeDecl;

            if (typeDecl == null)
            {
                return false;
            }

            if (position < typeDecl.OpenBraceToken.Span.End ||
                position > typeDecl.CloseBraceToken.Span.Start)
            {
                return false;
            }

            var line = sourceText.Lines.GetLineFromPosition(position);
            if (!line.IsEmptyOrWhitespace())
            {
                return false;
            }

            var member = typeDecl.Members.FirstOrDefault(d => d.FullSpan.Contains(position));
            if (member == null)
            {
                // There are no members, or we're after the last member.
                return true;
            }
            else
            {
                // We're within a member.  Make sure we're in the leading whitespace of
                // the member.
                if (position < member.SpanStart)
                {
                    foreach (var trivia in member.GetLeadingTrivia())
                    {
                        if (!trivia.IsWhitespaceOrEndOfLine())
                        {
                            return false;
                        }

                        if (trivia.FullSpan.Contains(position))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected override bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken)
            => token.ContainsInterleavedDirective(span, cancellationToken);

        public SyntaxTokenList GetModifiers(SyntaxNode node)
            => node.GetModifiers();

        public SyntaxNode WithModifiers(SyntaxNode node, SyntaxTokenList modifiers)
            => node.WithModifiers(modifiers);

        public bool IsLiteralExpression(SyntaxNode node)
            => node is LiteralExpressionSyntax;

        public SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node)
            => ((LocalDeclarationStatementSyntax)node).Declaration.Variables;

        public SyntaxNode GetInitializerOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclaratorSyntax)node).Initializer;

        public SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclarationSyntax)((VariableDeclaratorSyntax)node).Parent).Type;

        public SyntaxNode GetValueOfEqualsValueClause(SyntaxNode node)
            => ((EqualsValueClauseSyntax)node)?.Value;

        public bool IsScopeBlock(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block);

        public bool IsExecutableBlock(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection, SyntaxKind.CompilationUnit);

        public IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node)
        {
            return node switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSection => switchSection.Statements,
                CompilationUnitSyntax compilationUnit => compilationUnit.Members.OfType<GlobalStatementSyntax>().SelectAsArray(globalStatement => globalStatement.Statement),
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };
        }

        public SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes)
            => nodes.FindInnermostCommonNode(node => IsExecutableBlock(node));

#nullable enable
        public bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node)
            => IsExecutableBlock(node) || node.IsEmbeddedStatementOwner();
#nullable restore

        public IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode node)
            => IsExecutableBlock(node)
               ? GetExecutableBlockStatements(node)
               : ImmutableArray.Create<SyntaxNode>(node.GetEmbeddedStatement());

        public bool IsCastExpression(SyntaxNode node)
            => node is CastExpressionSyntax;

        public void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression)
        {
            var cast = (CastExpressionSyntax)node;
            type = cast.Type;
            expression = cast.Expression;
        }

        public SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.OverrideKeyword && token.Parent is MemberDeclarationSyntax member)
            {
                return member.GetNameToken();
            }

            return null;
        }

        public override SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node)
            => node.GetAttributeLists();

        public override bool IsParameterNameXmlElementSyntax(SyntaxNode node)
            => node.IsKind(SyntaxKind.XmlElement, out XmlElementSyntax xmlElement) &&
            xmlElement.StartTag.Name.LocalName.ValueText == DocumentationCommentXmlNames.ParameterElementName;

        public override SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia)
        {
            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationCommentTrivia)
            {
                return documentationCommentTrivia.Content;
            }

            throw ExceptionUtilities.UnexpectedValue(trivia.Kind());
        }

        public override bool CanHaveAccessibility(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    var declarationKind = this.GetDeclarationKind(declaration);
                    return declarationKind == DeclarationKind.Field || declarationKind == DeclarationKind.Event;

                case SyntaxKind.ConstructorDeclaration:
                    // Static constructor can't have accessibility
                    return !((ConstructorDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)declaration;
                    if (method.ExplicitInterfaceSpecifier != null)
                    {
                        // explicit interface methods can't have accessibility.
                        return false;
                    }

                    if (method.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        // partial methods can't have accessibility modifiers.
                        return false;
                    }

                    return true;

                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

                default:
                    return false;
            }
        }

        public override Accessibility GetAccessibility(SyntaxNode declaration)
        {
            if (!CanHaveAccessibility(declaration))
            {
                return Accessibility.NotApplicable;
            }

            var modifierTokens = GetModifierTokens(declaration);
            GetAccessibilityAndModifiers(modifierTokens, out var accessibility, out _, out _);
            return accessibility;
        }

        public override void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers, out bool isDefault)
        {
            accessibility = Accessibility.NotApplicable;
            modifiers = DeclarationModifiers.None;
            isDefault = false;

            foreach (var token in modifierList)
            {
                accessibility = (token.Kind(), accessibility) switch
                {
                    (SyntaxKind.PublicKeyword, _) => Accessibility.Public,

                    (SyntaxKind.PrivateKeyword, Accessibility.Protected) => Accessibility.ProtectedAndInternal,
                    (SyntaxKind.PrivateKeyword, _) => Accessibility.Private,

                    (SyntaxKind.InternalKeyword, Accessibility.Protected) => Accessibility.ProtectedOrInternal,
                    (SyntaxKind.InternalKeyword, _) => Accessibility.Internal,

                    (SyntaxKind.ProtectedKeyword, Accessibility.Private) => Accessibility.ProtectedAndInternal,
                    (SyntaxKind.ProtectedKeyword, Accessibility.Internal) => Accessibility.ProtectedOrInternal,
                    (SyntaxKind.ProtectedKeyword, _) => Accessibility.Protected,

                    _ => accessibility,
                };

                modifiers |= token.Kind() switch
                {
                    SyntaxKind.AbstractKeyword => DeclarationModifiers.Abstract,
                    SyntaxKind.NewKeyword => DeclarationModifiers.New,
                    SyntaxKind.OverrideKeyword => DeclarationModifiers.Override,
                    SyntaxKind.VirtualKeyword => DeclarationModifiers.Virtual,
                    SyntaxKind.StaticKeyword => DeclarationModifiers.Static,
                    SyntaxKind.AsyncKeyword => DeclarationModifiers.Async,
                    SyntaxKind.ConstKeyword => DeclarationModifiers.Const,
                    SyntaxKind.ReadOnlyKeyword => DeclarationModifiers.ReadOnly,
                    SyntaxKind.SealedKeyword => DeclarationModifiers.Sealed,
                    SyntaxKind.UnsafeKeyword => DeclarationModifiers.Unsafe,
                    SyntaxKind.PartialKeyword => DeclarationModifiers.Partial,
                    SyntaxKind.RefKeyword => DeclarationModifiers.Ref,
                    SyntaxKind.VolatileKeyword => DeclarationModifiers.Volatile,
                    SyntaxKind.ExternKeyword => DeclarationModifiers.Extern,
                    _ => DeclarationModifiers.None,
                };

                isDefault |= token.Kind() == SyntaxKind.DefaultKeyword;
            }
        }

        public override SyntaxTokenList GetModifierTokens(SyntaxNode declaration)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.Modifiers,
                ParameterSyntax parameter => parameter.Modifiers,
                LocalDeclarationStatementSyntax localDecl => localDecl.Modifiers,
                LocalFunctionStatementSyntax localFunc => localFunc.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                VariableDeclarationSyntax varDecl => GetModifierTokens(varDecl.Parent),
                VariableDeclaratorSyntax varDecl => GetModifierTokens(varDecl.Parent),
                _ => default,
            };

        public override DeclarationKind GetDeclarationKind(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return DeclarationKind.Class;
                case SyntaxKind.StructDeclaration:
                    return DeclarationKind.Struct;
                case SyntaxKind.InterfaceDeclaration:
                    return DeclarationKind.Interface;
                case SyntaxKind.EnumDeclaration:
                    return DeclarationKind.Enum;
                case SyntaxKind.DelegateDeclaration:
                    return DeclarationKind.Delegate;

                case SyntaxKind.MethodDeclaration:
                    return DeclarationKind.Method;
                case SyntaxKind.OperatorDeclaration:
                    return DeclarationKind.Operator;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return DeclarationKind.ConversionOperator;
                case SyntaxKind.ConstructorDeclaration:
                    return DeclarationKind.Constructor;
                case SyntaxKind.DestructorDeclaration:
                    return DeclarationKind.Destructor;

                case SyntaxKind.PropertyDeclaration:
                    return DeclarationKind.Property;
                case SyntaxKind.IndexerDeclaration:
                    return DeclarationKind.Indexer;
                case SyntaxKind.EventDeclaration:
                    return DeclarationKind.CustomEvent;
                case SyntaxKind.EnumMemberDeclaration:
                    return DeclarationKind.EnumMember;
                case SyntaxKind.CompilationUnit:
                    return DeclarationKind.CompilationUnit;
                case SyntaxKind.NamespaceDeclaration:
                    return DeclarationKind.Namespace;
                case SyntaxKind.UsingDirective:
                    return DeclarationKind.NamespaceImport;
                case SyntaxKind.Parameter:
                    return DeclarationKind.Parameter;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return DeclarationKind.LambdaExpression;

                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration != null && fd.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Field;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.EventFieldDeclaration:
                    var ef = (EventFieldDeclarationSyntax)declaration;
                    if (ef.Declaration != null && ef.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Event;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration != null && ld.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Variable;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.VariableDeclaration:
                    {
                        var vd = (VariableDeclarationSyntax)declaration;
                        if (vd.Variables.Count == 1 && vd.Parent == null)
                        {
                            // this node is the declaration if it contains only one variable and has no parent.
                            return DeclarationKind.Variable;
                        }
                        else
                        {
                            return DeclarationKind.None;
                        }
                    }

                case SyntaxKind.VariableDeclarator:
                    {
                        var vd = declaration.Parent as VariableDeclarationSyntax;

                        // this node is considered the declaration if it is one among many, or it has no parent
                        if (vd == null || vd.Variables.Count > 1)
                        {
                            if (ParentIsFieldDeclaration(vd))
                            {
                                return DeclarationKind.Field;
                            }
                            else if (ParentIsEventFieldDeclaration(vd))
                            {
                                return DeclarationKind.Event;
                            }
                            else
                            {
                                return DeclarationKind.Variable;
                            }
                        }
                        break;
                    }

                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)declaration;
                    if (list.Attributes.Count == 1)
                    {
                        return DeclarationKind.Attribute;
                    }
                    break;

                case SyntaxKind.Attribute:
                    if (!(declaration.Parent is AttributeListSyntax parentList) || parentList.Attributes.Count > 1)
                    {
                        return DeclarationKind.Attribute;
                    }
                    break;

                case SyntaxKind.GetAccessorDeclaration:
                    return DeclarationKind.GetAccessor;
                case SyntaxKind.SetAccessorDeclaration:
                    return DeclarationKind.SetAccessor;
                case SyntaxKind.AddAccessorDeclaration:
                    return DeclarationKind.AddAccessor;
                case SyntaxKind.RemoveAccessorDeclaration:
                    return DeclarationKind.RemoveAccessor;
            }

            return DeclarationKind.None;
        }

        internal static bool ParentIsFieldDeclaration(SyntaxNode node)
            => node?.Parent.IsKind(SyntaxKind.FieldDeclaration) ?? false;

        internal static bool ParentIsEventFieldDeclaration(SyntaxNode node)
            => node?.Parent.IsKind(SyntaxKind.EventFieldDeclaration) ?? false;

        internal static bool ParentIsLocalDeclarationStatement(SyntaxNode node)
            => node?.Parent.IsKind(SyntaxKind.LocalDeclarationStatement) ?? false;

        public bool IsIsPatternExpression(SyntaxNode node)
            => node.IsKind(SyntaxKind.IsPatternExpression);

        public void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right)
        {
            var isPatternExpression = (IsPatternExpressionSyntax)node;
            left = isPatternExpression.Expression;
            isToken = isPatternExpression.IsKeyword;
            right = isPatternExpression.Pattern;
        }

        public bool IsAnyPattern(SyntaxNode node)
            => node is PatternSyntax;

        public bool IsConstantPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.ConstantPattern);

        public bool IsDeclarationPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.DeclarationPattern);

        public bool IsRecursivePattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.RecursivePattern);

        public bool IsVarPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.VarPattern);

        public SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node)
            => ((ConstantPatternSyntax)node).Expression;

        public void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation)
        {
            var declarationPattern = (DeclarationPatternSyntax)node;
            type = declarationPattern.Type;
            designation = declarationPattern.Designation;
        }

        public void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode positionalPart, out SyntaxNode propertyPart, out SyntaxNode designation)
        {
            var recursivePattern = (RecursivePatternSyntax)node;
            type = recursivePattern.Type;
            positionalPart = recursivePattern.PositionalPatternClause;
            propertyPart = recursivePattern.PropertyPatternClause;
            designation = recursivePattern.Designation;
        }

        public bool SupportsNotPattern(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion.IsCSharp9OrAbove();

        public bool IsAndPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.AndPattern);

        public bool IsBinaryPattern(SyntaxNode node)
            => node is BinaryPatternSyntax;

        public bool IsNotPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.NotPattern);

        public bool IsOrPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.OrPattern);

        public bool IsParenthesizedPattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.ParenthesizedPattern);

        public bool IsTypePattern(SyntaxNode node)
            => node.IsKind(SyntaxKind.TypePattern);

        public bool IsUnaryPattern(SyntaxNode node)
            => node is UnaryPatternSyntax;

        public void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen)
        {
            var parenthesizedPattern = (ParenthesizedPatternSyntax)node;
            openParen = parenthesizedPattern.OpenParenToken;
            pattern = parenthesizedPattern.Pattern;
            closeParen = parenthesizedPattern.CloseParenToken;
        }

        public void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var binaryPattern = (BinaryPatternSyntax)node;
            left = binaryPattern.Left;
            operatorToken = binaryPattern.OperatorToken;
            right = binaryPattern.Right;
        }

        public void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern)
        {
            var unaryPattern = (UnaryPatternSyntax)node;
            operatorToken = unaryPattern.OperatorToken;
            pattern = unaryPattern.Pattern;
        }

        public SyntaxNode GetTypeOfTypePattern(SyntaxNode node)
            => ((TypePatternSyntax)node).Type;

        public bool IsImplicitObjectCreation(SyntaxNode node)
            => node.IsKind(SyntaxKind.ImplicitObjectCreationExpression);

        public SyntaxNode GetExpressionOfThrowExpression(SyntaxNode throwExpression)
            => ((ThrowExpressionSyntax)throwExpression).Expression;

        public bool IsThrowStatement(SyntaxNode node)
            => node.IsKind(SyntaxKind.ThrowStatement);

        public bool IsLocalFunction(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalFunctionStatement);
    }
}
