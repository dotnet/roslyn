// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpSyntaxFactsService : AbstractSyntaxFactsService, ISyntaxFactsService
    {
        internal static readonly CSharpSyntaxFactsService Instance = new CSharpSyntaxFactsService();

        private CSharpSyntaxFactsService()
        {
        }

        public bool IsCaseSensitive => true;

        public StringComparer StringComparer { get; } = StringComparer.Ordinal;

        public SyntaxTrivia ElasticMarker
            => SyntaxFactory.ElasticMarker;

        public SyntaxTrivia ElasticCarriageReturnLineFeed
            => SyntaxFactory.ElasticCarriageReturnLineFeed;

        public override ISyntaxKindsService SyntaxKinds { get; } = CSharpSyntaxKindsService.Instance;

        protected override IDocumentationCommentService DocumentationCommentService
            => CSharpDocumentationCommentService.Instance;

        public bool SupportsIndexingInitializer(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp6;

        public bool SupportsThrowExpression(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public SyntaxToken ParseToken(string text)
            => SyntaxFactory.ParseToken(text);

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

        public bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            return syntaxTree.IsInInactiveRegion(position, cancellationToken);
        }

        public bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            return syntaxTree.IsInNonUserCode(position, cancellationToken);
        }

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

        public bool IsNameOfMemberAccessExpression(SyntaxNode node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsMemberAccessExpressionName();
        }

        public bool IsObjectCreationExpressionType(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.ObjectCreationExpression) &&
                ((ObjectCreationExpressionSyntax)node.Parent).Type == node;
        }

        public bool IsAttributeName(SyntaxNode node)
        {
            return SyntaxFacts.IsAttributeName(node);
        }

        public bool IsInvocationExpression(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax;
        }

        public bool IsAnonymousFunction(SyntaxNode node)
        {
            return node is ParenthesizedLambdaExpressionSyntax ||
                node is SimpleLambdaExpressionSyntax ||
                node is AnonymousMethodExpressionSyntax;
        }

        public bool IsGenericName(SyntaxNode node)
            => node is GenericNameSyntax;

        public bool IsQualifiedName(SyntaxNode node)
            => node.IsKind(SyntaxKind.QualifiedName);

        public bool IsNamedParameter(SyntaxNode node)
            => node.CheckParent<NameColonSyntax>(p => p.Name == node);

        public SyntaxToken? GetNameOfParameter(SyntaxNode node)
            => (node as ParameterSyntax)?.Identifier;

        public SyntaxNode GetDefaultOfParameter(SyntaxNode node)
            => (node as ParameterSyntax)?.Default;

        public SyntaxNode GetParameterList(SyntaxNode node)
            => CSharpSyntaxGenerator.GetParameterList(node);

        public bool IsSkippedTokensTrivia(SyntaxNode node)
            => node is SkippedTokensTriviaSyntax;

        public SyntaxToken GetIdentifierOfGenericName(SyntaxNode genericName)
        {
            return genericName is GenericNameSyntax csharpGenericName
                ? csharpGenericName.Identifier
                : default;
        }

        public bool IsUsingDirectiveName(SyntaxNode node)
        {
            return
                node.IsParentKind(SyntaxKind.UsingDirective) &&
                ((UsingDirectiveSyntax)node.Parent).Name == node;
        }

        public bool IsForEachStatement(SyntaxNode node)
            => node is ForEachStatementSyntax;

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

        public bool IsLockStatement(SyntaxNode node)
            => node is LockStatementSyntax;

        public bool IsStatement(SyntaxNode node)
           => node is StatementSyntax;

        public bool IsExecutableStatement(SyntaxNode node)
            => node is StatementSyntax;

        public bool IsParameter(SyntaxNode node)
            => node is ParameterSyntax;

        public bool IsVariableDeclarator(SyntaxNode node)
            => node is VariableDeclaratorSyntax;

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
        {
            return token.Parent.IsKind(SyntaxKind.ThisConstructorInitializer) &&
                ((ConstructorInitializerSyntax)token.Parent).ThisOrBaseKeyword == token;
        }

        public bool IsBaseConstructorInitializer(SyntaxToken token)
        {
            return token.Parent.IsKind(SyntaxKind.BaseConstructorInitializer) &&
                ((ConstructorInitializerSyntax)token.Parent).ThisOrBaseKeyword == token;
        }

        public bool IsQueryExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.QueryExpression;

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
            => TryGetPredefinedType(token, out var actualType) && actualType != PredefinedType.None;

        public bool IsPredefinedType(SyntaxToken token, PredefinedType type)
            => TryGetPredefinedType(token, out var actualType) && actualType == type;

        public bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type)
        {
            type = GetPredefinedType(token);
            return type != PredefinedType.None;
        }

        private PredefinedType GetPredefinedType(SyntaxToken token)
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

        private PredefinedOperator GetPredefinedOperator(SyntaxToken token)
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
            return IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length;
        }

        public bool IsVerbatimIdentifier(string identifier)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            return IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length && token.IsVerbatimIdentifier();
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
                if (typedParent.IsParentKind(SyntaxKind.VariableDeclaration))
                {
                    declaredType = ((VariableDeclarationSyntax)typedParent.Parent).Type;
                }
                else if (typedParent.IsParentKind(SyntaxKind.FieldDeclaration))
                {
                    declaredType = ((FieldDeclarationSyntax)typedParent.Parent).Declaration.Type;
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

        public bool IsSimpleMemberAccessExpression(SyntaxNode node)
            => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.SimpleMemberAccessExpression;

        public bool IsConditionalAccessExpression(SyntaxNode node)
            => node is ConditionalAccessExpressionSyntax;

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

        public bool IsMemberBindingExpression(SyntaxNode node)
            => node is MemberBindingExpressionSyntax;

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
            => ((ArgumentSyntax)node).Expression;

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

        public bool IsTypeArgumentList(SyntaxNode node)
            => node.IsKind(SyntaxKind.TypeArgumentList);

        public bool IsTypeConstraint(SyntaxNode node)
            => node.IsKind(SyntaxKind.TypeConstraint);

        public bool IsInConstantContext(SyntaxNode node)
            => (node as ExpressionSyntax).IsInConstantContext();

        public bool IsInConstructor(SyntaxNode node)
            => node.GetAncestor<ConstructorDeclarationSyntax>() != null;

        public bool IsUnsafeContext(SyntaxNode node)
            => node.IsUnsafeContext();

        public SyntaxNode GetNameOfAttribute(SyntaxNode node)
            => ((AttributeSyntax)node).Name;

        public bool IsParenthesizedExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.ParenthesizedExpression;

        public SyntaxNode GetExpressionOfParenthesizedExpression(SyntaxNode node)
            => ((ParenthesizedExpressionSyntax)node).Expression;

        public bool IsAttribute(SyntaxNode node)
            => node is AttributeSyntax;

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

        public bool IsObjectCreationExpression(SyntaxNode node)
            => node is ObjectCreationExpressionSyntax;

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
                else if (objectInitializer.IsParentKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    initializedInstance = ((AssignmentExpressionSyntax)objectInitializer.Parent).Left;
                    return true;
                }
            }

            return false;
        }

        public bool IsElementAccessExpression(SyntaxNode node)
        {
            return node.Kind() == SyntaxKind.ElementAccessExpression;
        }

        public SyntaxNode ConvertToSingleLine(SyntaxNode node, bool useElasticTrivia = false)
            => node.ConvertToSingleLine(useElasticTrivia);

        public SyntaxToken ToIdentifierToken(string name)
        {
            return name.ToIdentifierToken();
        }

        public SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia, bool addSimplifierAnnotation)
            => ((ExpressionSyntax)expression).Parenthesize(includeElasticTrivia, addSimplifierAnnotation);

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

        public List<SyntaxNode> GetMethodLevelMembers(SyntaxNode root)
        {
            var list = new List<SyntaxNode>();
            AppendMethodLevelMembers(root, list);
            return list;
        }

        public SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration)
            => ((TypeDeclarationSyntax)typeDeclaration).Members;

        public SyntaxList<SyntaxNode> GetMembersOfNamespaceDeclaration(SyntaxNode namespaceDeclaration)
            => ((NamespaceDeclarationSyntax)namespaceDeclaration).Members;

        private void AppendMethodLevelMembers(SyntaxNode node, List<SyntaxNode> list)
        {
            foreach (var member in node.GetMembers())
            {
                if (IsTopLevelNodeWithMembers(member))
                {
                    AppendMethodLevelMembers(member, list);
                    continue;
                }

                if (IsMethodLevelMember(member))
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

        private TextSpan GetBlockBodySpan(BlockSyntax body)
        {
            return TextSpan.FromBounds(body.OpenBraceToken.Span.End, body.CloseBraceToken.SpanStart);
        }

        public int GetMethodLevelMemberId(SyntaxNode root, SyntaxNode node)
        {
            Debug.Assert(root.SyntaxTree == node.SyntaxTree);

            var currentId = 0;
            Contract.ThrowIfFalse(TryGetMethodLevelMember(root, (n, i) => n == node, ref currentId, out var currentNode));

            Contract.ThrowIfFalse(currentId >= 0);
            CheckMemberId(root, node, currentId);
            return currentId;
        }

        public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
        {
            var currentId = 0;
            if (!TryGetMethodLevelMember(root, (n, i) => i == memberId, ref currentId, out var currentNode))
            {
                return null;
            }

            Contract.ThrowIfNull(currentNode);
            CheckMemberId(root, currentNode, memberId);
            return currentNode;
        }

        private bool TryGetMethodLevelMember(
            SyntaxNode node, Func<SyntaxNode, int, bool> predicate, ref int currentId, out SyntaxNode currentNode)
        {
            foreach (var member in node.GetMembers())
            {
                if (IsTopLevelNodeWithMembers(member))
                {
                    if (TryGetMethodLevelMember(member, predicate, ref currentId, out currentNode))
                    {
                        return true;
                    }

                    continue;
                }

                if (IsMethodLevelMember(member))
                {
                    if (predicate(member, currentId))
                    {
                        currentNode = member;
                        return true;
                    }

                    currentId++;
                }
            }

            currentNode = null;
            return false;
        }

        [Conditional("DEBUG")]
        private void CheckMemberId(SyntaxNode root, SyntaxNode node, int memberId)
        {
            var list = GetMethodLevelMembers(root);
            var index = list.IndexOf(node);

            Contract.ThrowIfFalse(index == memberId);
        }

        public SyntaxNode GetBindableParent(SyntaxToken token)
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
                var name = parent as NameSyntax;
                if (name == null)
                {
                    break;
                }

                node = parent;
            }

            return node;
        }

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
        {
            if ((argument as ArgumentSyntax)?.NameColon != null)
            {
                return (argument as ArgumentSyntax).NameColon.Name.Identifier.ValueText;
            }

            return string.Empty;
        }

        public bool IsLeftSideOfDot(SyntaxNode node)
        {
            return (node as ExpressionSyntax).IsLeftSideOfDot();
        }

        public SyntaxNode GetRightSideOfDot(SyntaxNode node)
        {
            return (node as QualifiedNameSyntax)?.Right ??
                (node as MemberAccessExpressionSyntax)?.Name;
        }

        public bool IsLeftSideOfExplicitInterfaceSpecifier(SyntaxNode node)
            => (node as NameSyntax).IsLeftSideOfExplicitInterfaceSpecifier();

        public bool IsLeftSideOfAssignment(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfAssignExpression();

        public bool IsLeftSideOfAnyAssignment(SyntaxNode node)
            => (node as ExpressionSyntax).IsLeftSideOfAnyAssignExpression();

        public SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node)
            => (node as AssignmentExpressionSyntax)?.Right;

        public bool IsInferredAnonymousObjectMemberDeclarator(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator) &&
                ((AnonymousObjectMemberDeclaratorSyntax)node).NameEquals == null;
        }

        public bool IsOperandOfIncrementExpression(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.PostIncrementExpression) ||
                node.IsParentKind(SyntaxKind.PreIncrementExpression);
        }

        public bool IsOperandOfDecrementExpression(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.PostDecrementExpression) ||
                node.IsParentKind(SyntaxKind.PreDecrementExpression);
        }

        public bool IsOperandOfIncrementOrDecrementExpression(SyntaxNode node)
        {
            return IsOperandOfIncrementExpression(node) || IsOperandOfDecrementExpression(node);
        }

        public SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString)
        {
            return ((interpolatedString as InterpolatedStringExpressionSyntax)?.Contents).Value;
        }

        public override bool IsStringLiteral(SyntaxToken token)
            => token.IsKind(SyntaxKind.StringLiteralToken);

        public override bool IsInterpolatedStringTextToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.InterpolatedStringTextToken);

        public bool IsStringLiteralExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.StringLiteralExpression;

        public bool IsVerbatimStringLiteral(SyntaxToken token)
            => token.IsVerbatimStringLiteral();

        public bool IsNumericLiteral(SyntaxToken token)
            => token.Kind() == SyntaxKind.NumericLiteralToken;

        public bool IsCharacterLiteral(SyntaxToken token)
            => token.Kind() == SyntaxKind.CharacterLiteralToken;

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
            => (argumentList as BaseArgumentListSyntax)?.Arguments ?? default(SeparatedSyntaxList<SyntaxNode>);

        public bool IsRegularComment(SyntaxTrivia trivia)
            => trivia.IsRegularComment();

        public bool IsDocumentationComment(SyntaxTrivia trivia)
            => trivia.IsDocComment();

        public bool IsElastic(SyntaxTrivia trivia)
            => trivia.IsElastic();

        public bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia;

        public bool IsDocumentationComment(SyntaxNode node)
            => SyntaxFacts.IsDocumentationCommentTrivia(node.Kind());

        public bool IsUsingOrExternOrImport(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.UsingDirective) ||
                   node.IsKind(SyntaxKind.ExternAliasDirective);
        }

        public bool IsGlobalAttribute(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.Attribute) && node.Parent.IsKind(SyntaxKind.AttributeList) &&
                   ((AttributeListSyntax)node.Parent).Target?.Identifier.Kind() == SyntaxKind.AssemblyKeyword;
        }

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
        {
            return SyntaxFacts.IsNamespaceMemberDeclaration(node.Kind()) || IsMemberDeclaration(node);
        }

        public bool IsTypeDeclaration(SyntaxNode node)
            => SyntaxFacts.IsTypeDeclaration(node.Kind());

        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        public void AddFirstMissingCloseBrace(
            SyntaxNode root, SyntaxNode contextNode,
            out SyntaxNode newRoot, out SyntaxNode newContextNode)
        {
            // First, annotate the context node in the tree so that we can find it again
            // after we've done all the rewriting.
            // var currentRoot = root.ReplaceNode(contextNode, contextNode.WithAdditionalAnnotations(s_annotation));
            newRoot = new AddFirstMissingCloseBraceRewriter(contextNode).Visit(root);
            newContextNode = newRoot.GetAnnotatedNodes(s_annotation).Single();
        }

        public SyntaxNode GetObjectCreationInitializer(SyntaxNode node)
            => ((ObjectCreationExpressionSyntax)node).Initializer;

        public SyntaxNode GetObjectCreationType(SyntaxNode node)
            => ((ObjectCreationExpressionSyntax)node).Type;

        public bool IsSimpleAssignmentStatement(SyntaxNode statement)
        {
            return statement.IsKind(SyntaxKind.ExpressionStatement) &&
                ((ExpressionStatementSyntax)statement).Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);
        }

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

        public bool IsIdentifierName(SyntaxNode node)
            => node.IsKind(SyntaxKind.IdentifierName);

        public bool IsLocalDeclarationStatement(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalDeclarationStatement);

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

        public bool IsAwaitExpression(SyntaxNode node)
            => node.IsKind(SyntaxKind.AwaitExpression);

        public bool IsExpressionOfAwaitExpression(SyntaxNode node)
            => (node?.Parent as AwaitExpressionSyntax)?.Expression == node;

        public bool IsExpressionOfMemberAccessExpression(SyntaxNode node)
            => (node?.Parent as MemberAccessExpressionSyntax)?.Expression == node;

        public SyntaxNode GetExpressionOfInvocationExpression(SyntaxNode node)
            => ((InvocationExpressionSyntax)node).Expression;

        public SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node)
            => ((AwaitExpressionSyntax)node).Expression;

        public bool IsPossibleTupleContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return syntaxTree.IsPossibleTupleContext(token, position);
        }

        public SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node)
            => ((ExpressionStatementSyntax)node).Expression;

        public bool IsNullLiteralExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.NullLiteralExpression;

        public bool IsDefaultLiteralExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.DefaultLiteralExpression;

        public bool IsBinaryExpression(SyntaxNode node)
            => node is BinaryExpressionSyntax;

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

        public bool IsLogicalAndExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalAndExpression;

        public bool IsLogicalOrExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalOrExpression;

        public bool IsLogicalNotExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalNotExpression;

        public bool IsConditionalAnd(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalAndExpression;

        public bool IsConditionalOr(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalOrExpression;

        public bool IsTupleExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.TupleExpression;

        public bool IsTupleType(SyntaxNode node)
            => node.Kind() == SyntaxKind.TupleType;

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

        public override bool IsWhitespaceTrivia(SyntaxTrivia trivia)
            => trivia.IsWhitespace();

        public override bool IsEndOfLineTrivia(SyntaxTrivia trivia)
            => trivia.IsEndOfLine();

        public override bool IsSingleLineCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsSingleLineComment();

        public override bool IsMultiLineCommentTrivia(SyntaxTrivia trivia)
            => trivia.IsMultiLineComment();

        public override bool IsShebangDirectiveTrivia(SyntaxTrivia trivia)
            => trivia.IsShebangDirective();

        public override bool IsPreprocessorDirective(SyntaxTrivia trivia)
            => SyntaxFacts.IsPreprocessorDirective(trivia.Kind());

        private class AddFirstMissingCloseBraceRewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxNode _contextNode;
            private bool _seenContextNode = false;
            private bool _addedFirstCloseCurly = false;

            public AddFirstMissingCloseBraceRewriter(SyntaxNode contextNode)
            {
                _contextNode = contextNode;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == _contextNode)
                {
                    _seenContextNode = true;

                    // Annotate the context node so we can find it again in the new tree
                    // after we've added the close curly.
                    return node.WithAdditionalAnnotations(s_annotation);
                }

                // rewrite this node normally.
                var rewritten = base.Visit(node);
                if (rewritten == node)
                {
                    return rewritten;
                }

                // This node changed.  That means that something underneath us got
                // rewritten.  (i.e. we added the annotation to the context node).
                Debug.Assert(_seenContextNode);

                // Ok, we're past the context node now.  See if this is a node with 
                // curlies.  If so, if it has a missing close curly then add in the 
                // missing curly.  Also, even if it doesn't have missing curlies, 
                // then still ask to format its close curly to make sure all the 
                // curlies up the stack are properly formatted.
                var braces = rewritten.GetBraces();
                if (braces.openBrace.Kind() == SyntaxKind.None &&
                    braces.closeBrace.Kind() == SyntaxKind.None)
                {
                    // Not an item with braces.  Just pass it up.
                    return rewritten;
                }

                // See if the close brace is missing.  If it's the first missing one 
                // we're seeing then definitely add it.
                if (braces.closeBrace.IsMissing)
                {
                    if (!_addedFirstCloseCurly)
                    {
                        var closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                            .WithAdditionalAnnotations(Formatter.Annotation);
                        rewritten = rewritten.ReplaceToken(braces.closeBrace, closeBrace);
                        _addedFirstCloseCurly = true;
                    }
                }
                else
                {
                    // Ask for the close brace to be formatted so that all the braces
                    // up the spine are in the right location.
                    rewritten = rewritten.ReplaceToken(braces.closeBrace,
                        braces.closeBrace.WithAdditionalAnnotations(Formatter.Annotation));
                }

                return rewritten;
            }
        }

        public bool IsOnTypeHeader(SyntaxNode root, int position, out SyntaxNode typeDeclaration)
        {
            var node = TryGetAncestorForLocation<BaseTypeDeclarationSyntax>(position, root);
            typeDeclaration = node;
            if (node == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.Identifier);
        }

        public bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, out SyntaxNode propertyDeclaration)
        {
            var node = TryGetAncestorForLocation<PropertyDeclarationSyntax>(position, root);
            propertyDeclaration = node;
            if (propertyDeclaration == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.Identifier);
        }

        public bool IsOnParameterHeader(SyntaxNode root, int position, out SyntaxNode parameter)
        {
            var node = TryGetAncestorForLocation<ParameterSyntax>(position, root);
            parameter = node;
            if (parameter == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node);
        }

        public bool IsOnMethodHeader(SyntaxNode root, int position, out SyntaxNode method)
        {
            var node = TryGetAncestorForLocation<MethodDeclarationSyntax>(position, root);
            method = node;
            if (method == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.ParameterList);
        }

        public bool IsOnLocalFunctionHeader(SyntaxNode root, int position, out SyntaxNode localFunction)
        {
            var node = TryGetAncestorForLocation<LocalFunctionStatementSyntax>(position, root);
            localFunction = node;
            if (localFunction == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.ParameterList);
        }

        public bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, out SyntaxNode localDeclaration)
        {
            var node = TryGetAncestorForLocation<LocalDeclarationStatementSyntax>(position, root);
            localDeclaration = node;
            if (localDeclaration == null)
            {
                return false;
            }

            var initializersExpressions = node.Declaration.Variables
                .Where(v => v.Initializer != null)
                .SelectAsArray(initializedV => initializedV.Initializer.Value);
            return IsOnHeader(position, node, node, holes: initializersExpressions);
        }

        public bool IsOnIfStatementHeader(SyntaxNode root, int position, out SyntaxNode ifStatement)
        {
            var node = TryGetAncestorForLocation<IfStatementSyntax>(position, root);
            ifStatement = node;
            if (ifStatement == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.CloseParenToken);
        }

        public bool IsOnForeachHeader(SyntaxNode root, int position, out SyntaxNode foreachStatement)
        {
            var node = TryGetAncestorForLocation<ForEachStatementSyntax>(position, root);
            foreachStatement = node;
            if (foreachStatement == null)
            {
                return false;
            }

            return IsOnHeader(position, node, node.CloseParenToken);
        }

        public bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position)
        {
            var token = root.FindToken(position);
            var typeDecl = token.GetAncestor<TypeDeclarationSyntax>();
            if (typeDecl != null)
            {
                if (position >= typeDecl.OpenBraceToken.Span.End &&
                    position <= typeDecl.CloseBraceToken.Span.Start)
                {
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
                }
            }

            return false;
        }

        public ImmutableArray<SyntaxNode> GetSelectedFieldsAndProperties(SyntaxNode root, TextSpan textSpan, bool allowPartialSelection)
            => ImmutableArray<SyntaxNode>.CastUp(root.GetFieldsAndPropertiesInSpan(textSpan, allowPartialSelection));

        protected override bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken)
            => token.ContainsInterleavedDirective(span, cancellationToken);

        public SyntaxTokenList GetModifiers(SyntaxNode node)
            => node.GetModifiers();

        public SyntaxNode WithModifiers(SyntaxNode node, SyntaxTokenList modifiers)
            => node.WithModifiers(modifiers);

        public bool IsLiteralExpression(SyntaxNode node)
            => node is LiteralExpressionSyntax;

        public bool IsThisExpression(SyntaxNode node)
            => node.IsKind(SyntaxKind.ThisExpression);

        public bool IsBaseExpression(SyntaxNode node)
            => node.IsKind(SyntaxKind.BaseExpression);

        public bool IsFalseLiteralExpression(SyntaxNode expression)
            => expression.IsKind(SyntaxKind.FalseLiteralExpression);

        public bool IsTrueLiteralExpression(SyntaxNode expression)
            => expression.IsKind(SyntaxKind.TrueLiteralExpression);

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
            => node.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection);

        public SyntaxList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node)
        {
            return node switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSection => switchSection.Statements,
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };
        }

        public SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes)
            => nodes.FindInnermostCommonNode(node => IsExecutableBlock(node));

        public bool IsStatementContainer(SyntaxNode node)
            => IsExecutableBlock(node) || node.IsEmbeddedStatementOwner();

        public IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode node)
            => IsExecutableBlock(node)
               ? GetExecutableBlockStatements(node)
               : (IReadOnlyList<SyntaxNode>)ImmutableArray.Create<SyntaxNode>(node.GetEmbeddedStatement());

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
            => CSharpSyntaxGenerator.GetAttributeLists(node);
    }
}
