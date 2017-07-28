﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
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

        public SyntaxTrivia ElasticCarriageReturnLineFeed
            => SyntaxFactory.ElasticCarriageReturnLineFeed;

        protected override IDocumentationCommentService DocumentationCommentService
            => CSharpDocumentationCommentService.Instance;

        public bool SupportsIndexingInitializer(ParseOptions options) 
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp6;

        public bool SupportsThrowExpression(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public bool IsAwaitKeyword(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.AwaitKeyword);
        }

        public bool IsIdentifier(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.IdentifierToken);
        }

        public bool IsGlobalNamespaceKeyword(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.GlobalKeyword);
        }

        public bool IsVerbatimIdentifier(SyntaxToken token)
        {
            return token.IsVerbatimIdentifier();
        }

        public bool IsOperator(SyntaxToken token)
        {
            var kind = token.Kind();

            return
                (SyntaxFacts.IsAnyUnaryExpression(kind) &&
                    (token.Parent is PrefixUnaryExpressionSyntax || token.Parent is PostfixUnaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsBinaryExpression(kind) && (token.Parent is BinaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsAssignmentExpressionOperatorToken(kind) && token.Parent is AssignmentExpressionSyntax);
        }

        public bool IsKeyword(SyntaxToken token)
        {
            var kind = (SyntaxKind)token.RawKind;
            return
                SyntaxFacts.IsKeywordKind(kind); // both contextual and reserved keywords
        }

        public bool IsContextualKeyword(SyntaxToken token)
        {
            var kind = (SyntaxKind)token.RawKind;
            return
                SyntaxFacts.IsContextualKeyword(kind);
        }

        public bool IsPreprocessorKeyword(SyntaxToken token)
        {
            var kind = (SyntaxKind)token.RawKind;
            return
                SyntaxFacts.IsPreprocessorKeyword(kind);
        }

        public bool IsHashToken(SyntaxToken token)
        {
            return (SyntaxKind)token.RawKind == SyntaxKind.HashToken;
        }

        public bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var csharpTree = syntaxTree as SyntaxTree;
            if (csharpTree == null)
            {
                return false;
            }

            return csharpTree.IsInInactiveRegion(position, cancellationToken);
        }

        public bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var csharpTree = syntaxTree as SyntaxTree;
            if (csharpTree == null)
            {
                return false;
            }

            return csharpTree.IsInNonUserCode(position, cancellationToken);
        }

        public bool IsEntirelyWithinStringOrCharOrNumericLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var csharpTree = syntaxTree as SyntaxTree;
            if (csharpTree == null)
            {
                return false;
            }

            return csharpTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken);
        }

        public bool IsDirective(SyntaxNode node)
        {
            return node is DirectiveTriviaSyntax;
        }

        public bool TryGetExternalSourceInfo(SyntaxNode node, out ExternalSourceInfo info)
        {
            var lineDirective = node as LineDirectiveTriviaSyntax;
            if (lineDirective != null)
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
        {
            return node is GenericNameSyntax;
        }

        public bool IsNamedParameter(SyntaxNode node)
            => node.CheckParent<NameColonSyntax>(p => p.Name == node);

        public SyntaxNode GetDefaultOfParameter(SyntaxNode node)
            => (node as ParameterSyntax)?.Default;

        public SyntaxNode GetParameterList(SyntaxNode node)
            => CSharpSyntaxGenerator.GetParameterList(node);

        public bool IsSkippedTokensTrivia(SyntaxNode node)
        {
            return node is SkippedTokensTriviaSyntax;
        }

        public bool HasIncompleteParentMember(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.IncompleteMember);
        }

        public SyntaxToken GetIdentifierOfGenericName(SyntaxNode genericName)
        {
            var csharpGenericName = genericName as GenericNameSyntax;
            return csharpGenericName != null
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
        {
            return node is ForEachStatementSyntax;
        }

        public bool IsLockStatement(SyntaxNode node)
        {
            return node is LockStatementSyntax;
        }

        public bool IsUsingStatement(SyntaxNode node)
            => node.Kind() == SyntaxKind.UsingStatement;

        public bool IsReturnStatement(SyntaxNode node)
            => node.Kind() == SyntaxKind.ReturnStatement;

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
            switch ((SyntaxKind)token.RawKind)
            {
                case SyntaxKind.BoolKeyword:
                    return PredefinedType.Boolean;
                case SyntaxKind.ByteKeyword:
                    return PredefinedType.Byte;
                case SyntaxKind.SByteKeyword:
                    return PredefinedType.SByte;
                case SyntaxKind.IntKeyword:
                    return PredefinedType.Int32;
                case SyntaxKind.UIntKeyword:
                    return PredefinedType.UInt32;
                case SyntaxKind.ShortKeyword:
                    return PredefinedType.Int16;
                case SyntaxKind.UShortKeyword:
                    return PredefinedType.UInt16;
                case SyntaxKind.LongKeyword:
                    return PredefinedType.Int64;
                case SyntaxKind.ULongKeyword:
                    return PredefinedType.UInt64;
                case SyntaxKind.FloatKeyword:
                    return PredefinedType.Single;
                case SyntaxKind.DoubleKeyword:
                    return PredefinedType.Double;
                case SyntaxKind.DecimalKeyword:
                    return PredefinedType.Decimal;
                case SyntaxKind.StringKeyword:
                    return PredefinedType.String;
                case SyntaxKind.CharKeyword:
                    return PredefinedType.Char;
                case SyntaxKind.ObjectKeyword:
                    return PredefinedType.Object;
                case SyntaxKind.VoidKeyword:
                    return PredefinedType.Void;
                default:
                    return PredefinedType.None;
            }
        }

        public bool IsPredefinedOperator(SyntaxToken token)
        {
            return TryGetPredefinedOperator(token, out var actualOperator) && actualOperator != PredefinedOperator.None;
        }

        public bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op)
        {
            return TryGetPredefinedOperator(token, out var actualOperator) && actualOperator == op;
        }

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
        {
            return SyntaxFacts.GetText((SyntaxKind)kind);
        }

        public bool IsIdentifierStartCharacter(char c)
        {
            return SyntaxFacts.IsIdentifierStartCharacter(c);
        }

        public bool IsIdentifierPartCharacter(char c)
        {
            return SyntaxFacts.IsIdentifierPartCharacter(c);
        }

        public bool IsIdentifierEscapeCharacter(char c)
        {
            return c == '@';
        }

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

        public bool IsTypeCharacter(char c)
        {
            return false;
        }

        public bool IsStartOfUnicodeEscapeSequence(char c)
        {
            return c == '\\';
        }

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
            }

            return false;
        }

        public bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken);
        }

        public bool IsNumericLiteralExpression(SyntaxNode node)
        {
            return node?.IsKind(SyntaxKind.NumericLiteralExpression) == true;
        }

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
            var typedParent = parent as ExpressionSyntax;

            if (typedParent != null)
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

            return false;
        }

        public bool IsSimpleMemberAccessExpression(SyntaxNode node)
            => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.SimpleMemberAccessExpression;

        public bool IsConditionalMemberAccessExpression(SyntaxNode node)
            => node is ConditionalAccessExpressionSyntax;

        public bool IsPointerMemberAccessExpression(SyntaxNode node)
            => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.PointerMemberAccessExpression;

        public void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity)
        {
            name = null;
            arity = 0;

            var simpleName = node as SimpleNameSyntax;
            if (simpleName != null)
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

        public SyntaxNode GetExpressionOfConditionalAccessExpression(SyntaxNode node)
            => (node as ConditionalAccessExpressionSyntax)?.Expression;

        public SyntaxNode GetExpressionOfElementAccessExpression(SyntaxNode node)
            => (node as ElementAccessExpressionSyntax)?.Expression;

        public SyntaxNode GetArgumentListOfElementAccessExpression(SyntaxNode node)
            => (node as ElementAccessExpressionSyntax)?.ArgumentList;

        public SyntaxNode GetExpressionOfInterpolation(SyntaxNode node)
            => (node as InterpolationSyntax)?.Expression;

        public bool IsInStaticContext(SyntaxNode node)
        {
            return node.IsInStaticContext();
        }

        public bool IsInNamespaceOrTypeContext(SyntaxNode node)
        {
            return SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);
        }

        public SyntaxNode GetExpressionOfArgument(SyntaxNode node)
        {
            return ((ArgumentSyntax)node).Expression;
        }

        public RefKind GetRefKindOfArgument(SyntaxNode node)
            => (node as ArgumentSyntax).GetRefKind();

        public bool IsSimpleArgument(SyntaxNode node)
        {
            var argument = (ArgumentSyntax)node;
            return argument.RefOrOutKeyword.Kind() == SyntaxKind.None &&
                   argument.NameColon == null;
        }


        public bool IsInConstantContext(SyntaxNode node)
        {
            return (node as ExpressionSyntax).IsInConstantContext();
        }

        public bool IsInConstructor(SyntaxNode node)
        {
            return node.GetAncestor<ConstructorDeclarationSyntax>() != null;
        }

        public bool IsUnsafeContext(SyntaxNode node)
        {
            return node.IsUnsafeContext();
        }

        public SyntaxNode GetNameOfAttribute(SyntaxNode node)
        {
            return ((AttributeSyntax)node).Name;
        }

        public bool IsAttribute(SyntaxNode node)
        {
            return node is AttributeSyntax;
        }

        public bool IsAttributeNamedArgumentIdentifier(SyntaxNode node)
        {
            var identifier = node as IdentifierNameSyntax;
            return identifier.IsAttributeNamedArgumentIdentifier();
        }

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
        {
            throw ExceptionUtilities.Unreachable;
        }

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
        {
            return node is ObjectCreationExpressionSyntax;
        }

        public bool IsObjectInitializerNamedAssignmentIdentifier(SyntaxNode node)
        {
            return IsObjectInitializerNamedAssignmentIdentifier(node, out var unused);
        }

        public bool IsObjectInitializerNamedAssignmentIdentifier(
            SyntaxNode node, out SyntaxNode initializedInstance)
        {
            initializedInstance = null;
            var identifier = node as IdentifierNameSyntax;
            if (identifier != null &&
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

        public SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia)
        {
            return ((ExpressionSyntax)expression).Parenthesize(includeElasticTrivia);
        }

        public bool IsIndexerMemberCRef(SyntaxNode node)
        {
            return node.Kind() == SyntaxKind.IndexerMemberCref;
        }

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
            return node is BaseMethodDeclarationSyntax || node is BasePropertyDeclarationSyntax || node is EnumMemberDeclarationSyntax || node is BaseFieldDeclarationSyntax;
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
            var memberDeclaration = node as MemberDeclarationSyntax;
            if (memberDeclaration != null)
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
                var fieldDeclarator = node as VariableDeclaratorSyntax;
                if (fieldDeclarator != null)
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
                for (int i = 1; i < typeParameterList.Parameters.Count; i++)
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
            var method = member as BaseMethodDeclarationSyntax;
            if (method != null)
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
            var constructor = node as ConstructorDeclarationSyntax;
            if (constructor != null)
            {
                return (constructor.Body != null && GetBlockBodySpan(constructor.Body).Contains(span)) ||
                       (constructor.Initializer != null && constructor.Initializer.Span.Contains(span));
            }

            var method = node as BaseMethodDeclarationSyntax;
            if (method != null)
            {
                return method.Body != null && GetBlockBodySpan(method.Body).Contains(span);
            }

            var property = node as BasePropertyDeclarationSyntax;
            if (property != null)
            {
                return property.AccessorList != null && property.AccessorList.Span.Contains(span);
            }

            var @enum = node as EnumMemberDeclarationSyntax;
            if (@enum != null)
            {
                return @enum.EqualsValue != null && @enum.EqualsValue.Span.Contains(span);
            }

            var field = node as BaseFieldDeclarationSyntax;
            if (field != null)
            {
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
            Contract.Requires(root.SyntaxTree == node.SyntaxTree);

            int currentId = 0;
            Contract.ThrowIfFalse(TryGetMethodLevelMember(root, (n, i) => n == node, ref currentId, out var currentNode));

            Contract.ThrowIfFalse(currentId >= 0);
            CheckMemberId(root, node, currentId);
            return currentId;
        }

        public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
        {
            int currentId = 0;
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
                var memberAccess = parent as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    if (memberAccess.Expression == node)
                    {
                        break;
                    }
                }

                // If this node is on the left side of a qualified name, don't ascend 
                // further or we'll end up binding to something else.
                var qualifiedName = parent as QualifiedNameSyntax;
                if (qualifiedName != null)
                {
                    if (qualifiedName.Left == node)
                    {
                        break;
                    }
                }

                // If this node is on the left side of a alias-qualified name, don't ascend 
                // further or we'll end up binding to something else.
                var aliasQualifiedName = parent as AliasQualifiedNameSyntax;
                if (aliasQualifiedName != null)
                {
                    if (aliasQualifiedName.Alias == node)
                    {
                        break;
                    }
                }

                // If this node is the type of an object creation expression, return the
                // object creation expression.
                var objectCreation = parent as ObjectCreationExpressionSyntax;
                if (objectCreation != null)
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
            var compilationUnit = root as CompilationUnitSyntax;
            if (compilationUnit == null)
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

                var constructor = member as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    constructors.Add(constructor);
                    continue;
                }

                var @namespace = member as NamespaceDeclarationSyntax;
                if (@namespace != null)
                {
                    AppendConstructors(@namespace.Members, constructors, cancellationToken);
                }

                var @class = member as ClassDeclarationSyntax;
                if (@class != null)
                {
                    AppendConstructors(@class.Members, constructors, cancellationToken);
                }

                var @struct = member as StructDeclarationSyntax;
                if (@struct != null)
                {
                    AppendConstructors(@struct.Members, constructors, cancellationToken);
                }
            }
        }

        public bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace)
        {
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                var tuple = token.Parent.GetBraces();

                openBrace = tuple.Item1;
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

        public bool IsLeftSideOfAssignment(SyntaxNode node)
        {
            return (node as ExpressionSyntax).IsLeftSideOfAssignExpression();
        }

        public bool IsLeftSideOfAnyAssignment(SyntaxNode node)
        {
            return (node as ExpressionSyntax).IsLeftSideOfAnyAssignExpression();
        }

        public SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node)
        {
            return (node as AssignmentExpressionSyntax)?.Right;
        }

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

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode invocationExpression)
            => GetArgumentsOfArgumentList((invocationExpression as InvocationExpressionSyntax)?.ArgumentList);

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode invocationExpression)
            => GetArgumentsOfArgumentList((invocationExpression as ObjectCreationExpressionSyntax)?.ArgumentList);

        public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode argumentList)
            => (argumentList as ArgumentListSyntax)?.Arguments ?? default(SeparatedSyntaxList<SyntaxNode>);

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

        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        public void AddFirstMissingCloseBrace(
            SyntaxNode root, SyntaxNode contextNode, 
            out SyntaxNode newRoot, out SyntaxNode newContextNode)
        {
            // First, annotate the context node in the tree so that we can find it again
            // after we've done all the rewriting.
            // var currentRoot = root.ReplaceNode(contextNode, contextNode.WithAdditionalAnnotations(s_annotation));
            newRoot = new AddFirstMissingCloseBaceRewriter(contextNode).Visit(root);
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
            var assignment = (AssignmentExpressionSyntax)((ExpressionStatementSyntax)statement).Expression;
            left = assignment.Left;
            operatorToken = assignment.OperatorToken;
            right = assignment.Right;
        }

        public SyntaxNode GetNameOfMemberAccessExpression(SyntaxNode memberAccessExpression)
            => ((MemberAccessExpressionSyntax)memberAccessExpression).Name;

        public SyntaxToken GetOperatorTokenOfMemberAccessExpression(SyntaxNode memberAccessExpression)
            => ((MemberAccessExpressionSyntax)memberAccessExpression).OperatorToken;

        public void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode name)
        {
            var memberAccess = (MemberAccessExpressionSyntax)node;
            expression = memberAccess.Expression;
            name = memberAccess.Name;
        }

        public SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node)
        {
            return ((SimpleNameSyntax)node).Identifier;
        }

        public SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node)
        {
            return ((VariableDeclaratorSyntax)node).Identifier;
        }

        public bool IsIdentifierName(SyntaxNode node)
            => node.IsKind(SyntaxKind.IdentifierName);

        public bool IsLocalDeclarationStatement(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.LocalDeclarationStatement);
        }

        public bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement)
        {
            return ((LocalDeclarationStatementSyntax)localDeclarationStatement).Declaration.Variables.Contains(
                (VariableDeclaratorSyntax)declarator);
        }

        public bool AreEquivalent(SyntaxToken token1, SyntaxToken token2)
        {
            return SyntaxFactory.AreEquivalent(token1, token2);
        }

        public bool AreEquivalent(SyntaxNode node1, SyntaxNode node2)
        {
            return SyntaxFactory.AreEquivalent(node1, node2);
        }

        public bool IsExpressionOfInvocationExpression(SyntaxNode node)
        {
            return node != null && (node.Parent as InvocationExpressionSyntax)?.Expression == node;
        }

        public bool IsExpressionOfAwaitExpression(SyntaxNode node)
        {
            return node != null && (node.Parent as AwaitExpressionSyntax)?.Expression == node;
        }

        public bool IsExpressionOfMemberAccessExpression(SyntaxNode node)
        {
            return node != null && (node.Parent as MemberAccessExpressionSyntax)?.Expression == node;
        }

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

        public void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxNode right)
        {
            var binaryExpression = (BinaryExpressionSyntax)node;
            left = binaryExpression.Left;
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

        public bool IsLogicalNotExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalNotExpression;

        public SyntaxNode GetOperandOfPrefixUnaryExpression(SyntaxNode node)
            => ((PrefixUnaryExpressionSyntax)node).Operand;

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

        private class AddFirstMissingCloseBaceRewriter: CSharpSyntaxRewriter
        {
            private readonly SyntaxNode _contextNode; 
            private bool _seenContextNode = false;
            private bool _addedFirstCloseCurly = false;

            public AddFirstMissingCloseBaceRewriter(SyntaxNode contextNode)
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
                if (braces.Item1.Kind() == SyntaxKind.None && 
                    braces.Item2.Kind() == SyntaxKind.None)
                {
                    // Not an item with braces.  Just pass it up.
                    return rewritten;
                }

                // See if the close brace is missing.  If it's the first missing one 
                // we're seeing then definitely add it.
                if (braces.Item2.IsMissing)
                {
                    if (!_addedFirstCloseCurly)
                    {
                        var closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                            .WithAdditionalAnnotations(Formatter.Annotation);
                        rewritten = rewritten.ReplaceToken(braces.Item2, closeBrace);
                        _addedFirstCloseCurly = true;
                    }
                }
                else
                {
                    // Ask for the close brace to be formatted so that all the braces
                    // up the spine are in the right location.
                    rewritten = rewritten.ReplaceToken(braces.Item2,
                        braces.Item2.WithAdditionalAnnotations(Formatter.Annotation));
                }

                return rewritten;
            }
        }

        public bool IsOnTypeHeader(SyntaxNode root, int position)
        {
            var token = root.FindToken(position);
            if (token.Kind() == SyntaxKind.EndOfFileToken)
            {
                token = token.GetPreviousToken();
            }

            var typeDecl = token.GetAncestor<BaseTypeDeclarationSyntax>();
            if (typeDecl == null)
            {
                return false;
            }

            var start = typeDecl.AttributeLists.LastOrDefault()?.GetLastToken().GetNextToken().SpanStart ??
                        typeDecl.SpanStart;
            var end = typeDecl.GetTypeParameterList()?.GetLastToken().FullSpan.End ??
                        typeDecl.Identifier.FullSpan.End;

            return position >= start && position <= end;
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

        public ImmutableArray<SyntaxNode> GetSelectedMembers(SyntaxNode root, TextSpan textSpan)
            => ImmutableArray<SyntaxNode>.CastUp(root.GetMembersInSpan(textSpan));

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

        public SyntaxNode GetValueOfEqualsValueClause(SyntaxNode node)
            => ((EqualsValueClauseSyntax)node).Value;

        public bool IsExecutableBlock(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block);

        public SyntaxList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode node)
            => ((BlockSyntax)node).Statements;

        public SyntaxNode FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes)
            => nodes.FindInnermostCommonBlock();
    }
}
