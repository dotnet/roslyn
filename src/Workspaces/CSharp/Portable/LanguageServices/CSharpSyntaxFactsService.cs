// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ISyntaxFactsService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxFactsService : ISyntaxFactsService
    {
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

            info = default(ExternalSourceInfo);
            return false;
        }

        public bool IsRightSideOfQualifiedName(SyntaxNode node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsRightSideOfQualifiedName();
        }

        public bool IsMemberAccessExpressionName(SyntaxNode node)
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
        {
            return node.CheckParent<NameColonSyntax>(p => p.Name == node);
        }

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
                : default(SyntaxToken);
        }

        public bool IsCaseSensitive
        {
            get
            {
                return true;
            }
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
        {
            return node is UsingStatementSyntax;
        }

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
        {
            return node is QueryExpressionSyntax;
        }

        public bool IsPredefinedType(SyntaxToken token)
        {
            PredefinedType actualType;
            return TryGetPredefinedType(token, out actualType) && actualType != PredefinedType.None;
        }

        public bool IsPredefinedType(SyntaxToken token, PredefinedType type)
        {
            PredefinedType actualType;
            return TryGetPredefinedType(token, out actualType) && actualType == type;
        }

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
            PredefinedOperator actualOperator;
            return TryGetPredefinedOperator(token, out actualOperator) && actualOperator != PredefinedOperator.None;
        }

        public bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op)
        {
            PredefinedOperator actualOperator;
            return TryGetPredefinedOperator(token, out actualOperator) && actualOperator == op;
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

        public bool IsStringLiteral(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken);
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

        public bool IsMemberAccessExpression(SyntaxNode node)
        {
            return node is MemberAccessExpressionSyntax &&
                ((MemberAccessExpressionSyntax)node).Kind() == SyntaxKind.SimpleMemberAccessExpression;
        }

        public bool IsConditionalMemberAccessExpression(SyntaxNode node)
        {
            return node is ConditionalAccessExpressionSyntax;
        }

        public bool IsPointerMemberAccessExpression(SyntaxNode node)
        {
            return node is MemberAccessExpressionSyntax &&
                ((MemberAccessExpressionSyntax)node).Kind() == SyntaxKind.PointerMemberAccessExpression;
        }

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

        public SyntaxNode GetExpressionOfMemberAccessExpression(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.MemberBindingExpression)
                ? GetExpressionOfConditionalMemberAccessExpression(node.GetParentConditionalAccessExpression())
                : (node as MemberAccessExpressionSyntax)?.Expression;
        }

        public SyntaxNode GetExpressionOfConditionalMemberAccessExpression(SyntaxNode node)
        {
            return (node as ConditionalAccessExpressionSyntax)?.Expression;
        }

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
        {
            return (node as ArgumentSyntax).GetRefKind();
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
            return
                identifier != null &&
                identifier.IsParentKind(SyntaxKind.NameEquals) &&
                identifier.Parent.IsParentKind(SyntaxKind.AttributeArgument);
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
            var identifier = node as IdentifierNameSyntax;
            return
                identifier != null &&
                identifier.IsLeftSideOfAssignExpression() &&
                identifier.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression);
        }

        public bool IsElementAccessExpression(SyntaxNode node)
        {
            return node.Kind() == SyntaxKind.ElementAccessExpression;
        }

        public SyntaxNode ConvertToSingleLine(SyntaxNode node)
        {
            return node.ConvertToSingleLine();
        }

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

        public bool TryGetDeclaredSymbolInfo(SyntaxNode node, out DeclaredSymbolInfo declaredSymbolInfo)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    var classDecl = (ClassDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(classDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Class, classDecl.Identifier.Span);
                    return true;
                case SyntaxKind.ConstructorDeclaration:
                    var ctorDecl = (ConstructorDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        ctorDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Constructor,
                        ctorDecl.Identifier.Span,
                        parameterCount: (ushort)(ctorDecl.ParameterList?.Parameters.Count ?? 0));
                    return true;
                case SyntaxKind.DelegateDeclaration:
                    var delegateDecl = (DelegateDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(delegateDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Delegate, delegateDecl.Identifier.Span);
                    return true;
                case SyntaxKind.EnumDeclaration:
                    var enumDecl = (EnumDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(enumDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Enum, enumDecl.Identifier.Span);
                    return true;
                case SyntaxKind.EnumMemberDeclaration:
                    var enumMember = (EnumMemberDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(enumMember.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.EnumMember, enumMember.Identifier.Span);
                    return true;
                case SyntaxKind.EventDeclaration:
                    var eventDecl = (EventDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(ExpandExplicitInterfaceName(eventDecl.Identifier.ValueText, eventDecl.ExplicitInterfaceSpecifier),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Event, eventDecl.Identifier.Span);
                    return true;
                case SyntaxKind.IndexerDeclaration:
                    var indexerDecl = (IndexerDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(WellKnownMemberNames.Indexer,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Indexer, indexerDecl.ThisKeyword.Span);
                    return true;
                case SyntaxKind.InterfaceDeclaration:
                    var interfaceDecl = (InterfaceDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(interfaceDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Interface, interfaceDecl.Identifier.Span);
                    return true;
                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(
                        ExpandExplicitInterfaceName(method.Identifier.ValueText, method.ExplicitInterfaceSpecifier),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Method,
                        method.Identifier.Span,
                        parameterCount: (ushort)(method.ParameterList?.Parameters.Count ?? 0),
                        typeParameterCount: (ushort)(method.TypeParameterList?.Parameters.Count ?? 0));
                    return true;
                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(ExpandExplicitInterfaceName(property.Identifier.ValueText, property.ExplicitInterfaceSpecifier),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Property, property.Identifier.Span);
                    return true;
                case SyntaxKind.StructDeclaration:
                    var structDecl = (StructDeclarationSyntax)node;
                    declaredSymbolInfo = new DeclaredSymbolInfo(structDecl.Identifier.ValueText,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent),
                        DeclaredSymbolInfoKind.Struct, structDecl.Identifier.Span);
                    return true;
                case SyntaxKind.VariableDeclarator:
                    // could either be part of a field declaration or an event field declaration
                    var variableDeclarator = (VariableDeclaratorSyntax)node;
                    var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
                    var fieldDeclaration = variableDeclaration?.Parent as BaseFieldDeclarationSyntax;
                    if (fieldDeclaration != null)
                    {
                        var kind = fieldDeclaration is EventFieldDeclarationSyntax
                            ? DeclaredSymbolInfoKind.Event
                            : fieldDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.ConstKeyword)
                                ? DeclaredSymbolInfoKind.Constant
                                : DeclaredSymbolInfoKind.Field;

                        declaredSymbolInfo = new DeclaredSymbolInfo(variableDeclarator.Identifier.ValueText,
                        GetContainerDisplayName(fieldDeclaration.Parent),
                        GetFullyQualifiedContainerName(fieldDeclaration.Parent),
                        kind, variableDeclarator.Identifier.Span);
                        return true;
                    }

                    break;
            }

            declaredSymbolInfo = default(DeclaredSymbolInfo);
            return false;
        }

        private static string ExpandExplicitInterfaceName(string identifier, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier)
        {
            if (explicitInterfaceSpecifier == null)
            {
                return identifier;
            }
            else
            {
                var builder = new StringBuilder();
                ExpandTypeName(explicitInterfaceSpecifier.Name, builder);
                builder.Append('.');
                builder.Append(identifier);
                return builder.ToString();
            }
        }

        private static void ExpandTypeName(TypeSyntax type, StringBuilder builder)
        {
            switch (type.Kind())
            {
                case SyntaxKind.AliasQualifiedName:
                    var alias = (AliasQualifiedNameSyntax)type;
                    builder.Append(alias.Alias.Identifier.ValueText);
                    break;
                case SyntaxKind.ArrayType:
                    var array = (ArrayTypeSyntax)type;
                    ExpandTypeName(array.ElementType, builder);
                    for (int i = 0; i < array.RankSpecifiers.Count; i++)
                    {
                        var rankSpecifier = array.RankSpecifiers[i];
                        builder.Append(rankSpecifier.OpenBracketToken.Text);
                        for (int j = 1; j < rankSpecifier.Sizes.Count; j++)
                        {
                            builder.Append(',');
                        }

                        builder.Append(rankSpecifier.CloseBracketToken.Text);
                    }

                    break;
                case SyntaxKind.GenericName:
                    var generic = (GenericNameSyntax)type;
                    builder.Append(generic.Identifier.ValueText);
                    if (generic.TypeArgumentList != null)
                    {
                        var arguments = generic.TypeArgumentList.Arguments;
                        builder.Append(generic.TypeArgumentList.LessThanToken.Text);
                        for (int i = 0; i < arguments.Count; i++)
                        {
                            if (i != 0)
                            {
                                builder.Append(',');
                            }

                            ExpandTypeName(arguments[i], builder);
                        }

                        builder.Append(generic.TypeArgumentList.GreaterThanToken.Text);
                    }

                    break;
                case SyntaxKind.IdentifierName:
                    var identifierName = (IdentifierNameSyntax)type;
                    builder.Append(identifierName.Identifier.ValueText);
                    break;
                case SyntaxKind.NullableType:
                    var nullable = (NullableTypeSyntax)type;
                    ExpandTypeName(nullable.ElementType, builder);
                    builder.Append(nullable.QuestionToken.Text);
                    break;
                case SyntaxKind.OmittedTypeArgument:
                    // do nothing since it was omitted, but don't reach the default block
                    break;
                case SyntaxKind.PointerType:
                    var pointer = (PointerTypeSyntax)type;
                    ExpandTypeName(pointer.ElementType, builder);
                    builder.Append(pointer.AsteriskToken.Text);
                    break;
                case SyntaxKind.PredefinedType:
                    var predefined = (PredefinedTypeSyntax)type;
                    builder.Append(predefined.Keyword.Text);
                    break;
                case SyntaxKind.QualifiedName:
                    var qualified = (QualifiedNameSyntax)type;
                    ExpandTypeName(qualified.Left, builder);
                    builder.Append(qualified.DotToken.Text);
                    ExpandTypeName(qualified.Right, builder);
                    break;
                default:
                    Debug.Assert(false, "Unexpected type syntax " + type.Kind());
                    break;
            }
        }

        private string GetContainerDisplayName(SyntaxNode node)
        {
            return GetDisplayName(node, DisplayNameOptions.IncludeTypeParameters);
        }

        private string GetFullyQualifiedContainerName(SyntaxNode node)
        {
            return GetDisplayName(node, DisplayNameOptions.IncludeNamespaces);
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
                var nameToken = memberDeclaration.GetNameToken();
                if (nameToken == default(SyntaxToken))
                {
                    Debug.Assert(memberDeclaration.Kind() == SyntaxKind.ConversionOperatorDeclaration);
                    name = (memberDeclaration as ConversionOperatorDeclarationSyntax)?.Type.ToString();
                }
                else
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
            }
            else
            {
                var fieldDeclarator = node as VariableDeclaratorSyntax;
                if (fieldDeclarator != null)
                {
                    var nameToken = fieldDeclarator.Identifier;
                    if (nameToken != default(SyntaxToken))
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
                return default(TextSpan);
            }

            var member = GetContainingMemberDeclaration(node, node.SpanStart);
            if (member == null)
            {
                return default(TextSpan);
            }

            // TODO: currently we only support method for now
            var method = member as BaseMethodDeclarationSyntax;
            if (method != null)
            {
                if (method.Body == null)
                {
                    return default(TextSpan);
                }

                return GetBlockBodySpan(method.Body);
            }

            return default(TextSpan);
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
            SyntaxNode currentNode;
            Contract.ThrowIfFalse(TryGetMethodLevelMember(root, (n, i) => n == node, ref currentId, out currentNode));

            Contract.ThrowIfFalse(currentId >= 0);
            CheckMemberId(root, node, currentId);
            return currentId;
        }

        public SyntaxNode GetMethodLevelMember(SyntaxNode root, int memberId)
        {
            int currentId = 0;
            SyntaxNode currentNode;
            if (!TryGetMethodLevelMember(root, (n, i) => i == memberId, ref currentId, out currentNode))
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

            openBrace = default(SyntaxToken);
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
                        return default(TextSpan);
                    }

                    if (triviaTok.Span.End < position)
                    {
                        if (!triviaTok.HasStructure)
                        {
                            return default(TextSpan);
                        }

                        var structure = triviaTok.GetStructure();
                        if (structure is BranchingDirectiveTriviaSyntax)
                        {
                            var branch = (BranchingDirectiveTriviaSyntax)structure;
                            return !branch.IsActive || !branch.BranchTaken ? TextSpan.FromBounds(branch.FullSpan.Start, position) : default(TextSpan);
                        }
                    }
                }
            }

            return default(TextSpan);
        }
    }
}
