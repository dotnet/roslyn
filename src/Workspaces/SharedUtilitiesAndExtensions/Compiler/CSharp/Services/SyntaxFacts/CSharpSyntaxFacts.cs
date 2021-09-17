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
        internal static readonly CSharpSyntaxFacts Instance = new();

        protected CSharpSyntaxFacts()
        {
        }

        public override bool IsCaseSensitive => true;

        public override StringComparer StringComparer { get; } = StringComparer.Ordinal;

        public override SyntaxTrivia ElasticMarker
            => SyntaxFactory.ElasticMarker;

        public override SyntaxTrivia ElasticCarriageReturnLineFeed
            => SyntaxFactory.ElasticCarriageReturnLineFeed;

        public override ISyntaxKinds SyntaxKinds { get; } = CSharpSyntaxKinds.Instance;

        public override bool SupportsIndexingInitializer(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp6;

        public override bool SupportsThrowExpression(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public override bool SupportsLocalFunctionDeclaration(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        public override bool SupportsRecord(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp9;

        public override bool SupportsRecordStruct(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion.IsCSharp10OrAbove();

        public override SyntaxToken ParseToken(string text)
            => SyntaxFactory.ParseToken(text);

        public override SyntaxTriviaList ParseLeadingTrivia(string text)
            => SyntaxFactory.ParseLeadingTrivia(text);

        public override string EscapeIdentifier(string identifier)
        {
            var nullIndex = identifier.IndexOf('\0');
            if (nullIndex >= 0)
            {
                identifier = identifier.Substring(0, nullIndex);
            }

            var needsEscaping = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None;
            return needsEscaping ? "@" + identifier : identifier;
        }

        public override bool IsVerbatimIdentifier(SyntaxToken token)
            => token.IsVerbatimIdentifier();

        public override bool IsOperator(SyntaxToken token)
        {
            var kind = token.Kind();

            return
                (SyntaxFacts.IsAnyUnaryExpression(kind) &&
                    (token.Parent is PrefixUnaryExpressionSyntax || token.Parent is PostfixUnaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsBinaryExpression(kind) && (token.Parent is BinaryExpressionSyntax || token.Parent is OperatorDeclarationSyntax)) ||
                (SyntaxFacts.IsAssignmentExpressionOperatorToken(kind) && token.Parent is AssignmentExpressionSyntax);
        }

        public override bool IsReservedKeyword(SyntaxToken token)
            => SyntaxFacts.IsReservedKeyword(token.Kind());

        public override bool IsContextualKeyword(SyntaxToken token)
            => SyntaxFacts.IsContextualKeyword(token.Kind());

        public override bool IsPreprocessorKeyword(SyntaxToken token)
            => SyntaxFacts.IsPreprocessorKeyword(token.Kind());

        public override bool IsPreProcessorDirectiveContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => syntaxTree.IsPreProcessorDirectiveContext(
                position, syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true), cancellationToken);

        public override bool IsEntirelyWithinStringOrCharOrNumericLiteral([NotNullWhen(true)] SyntaxTree? syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            return syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken);
        }

        public override bool IsDirective([NotNullWhen(true)] SyntaxNode? node)
            => node is DirectiveTriviaSyntax;

        public override bool TryGetExternalSourceInfo([NotNullWhen(true)] SyntaxNode? node, out ExternalSourceInfo info)
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

        public override bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsSimpleMemberAccessExpressionName();
        }

        public override bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node;

        public override bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node)
        {
            var name = node as SimpleNameSyntax;
            return name.IsMemberBindingExpressionName();
        }

        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? GetStandaloneExpression(SyntaxNode? node)
            => node is ExpressionSyntax expression ? SyntaxFactory.GetStandaloneExpression(expression) : node;

        public override SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node)
            => node.GetRootConditionalAccessExpression();

        public override bool IsDeclarationExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is DeclarationExpressionSyntax;

        public override bool IsAttributeName(SyntaxNode node)
            => SyntaxFacts.IsAttributeName(node);

        public override bool IsNamedArgument([NotNullWhen(true)] SyntaxNode? node)
            => node is ArgumentSyntax arg && arg.NameColon != null;

        public override bool IsNameOfNamedArgument([NotNullWhen(true)] SyntaxNode? node)
            => node.CheckParent<NameColonSyntax>(p => p.Name == node);

        public override SyntaxToken? GetNameOfParameter(SyntaxNode? node)
            => (node as ParameterSyntax)?.Identifier;

        public override SyntaxNode? GetDefaultOfParameter(SyntaxNode node)
            => ((ParameterSyntax)node).Default;

        public override SyntaxNode? GetParameterList(SyntaxNode node)
            => node.GetParameterList();

        public override bool IsParameterList([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.ParameterList, SyntaxKind.BracketedParameterList);

        public override SyntaxToken GetIdentifierOfGenericName(SyntaxNode node)
            => ((GenericNameSyntax)node).Identifier;

        public override bool IsUsingDirectiveName([NotNullWhen(true)] SyntaxNode? node)
            => node.IsParentKind(SyntaxKind.UsingDirective, out UsingDirectiveSyntax? usingDirective) &&
               usingDirective.Name == node;

        public override bool IsUsingAliasDirective([NotNullWhen(true)] SyntaxNode? node)
            => node is UsingDirectiveSyntax usingDirectiveNode && usingDirectiveNode.Alias != null;

        public override void GetPartsOfUsingAliasDirective(SyntaxNode node, out SyntaxToken globalKeyword, out SyntaxToken alias, out SyntaxNode name)
        {
            var usingDirective = (UsingDirectiveSyntax)node;
            globalKeyword = usingDirective.GlobalKeyword;
            alias = usingDirective.Alias!.Name.Identifier;
            name = usingDirective.Name;
        }

        public override bool IsDeconstructionForEachStatement([NotNullWhen(true)] SyntaxNode? node)
            => node is ForEachVariableStatementSyntax;

        public override bool IsDeconstructionAssignment([NotNullWhen(true)] SyntaxNode? node)
            => node is AssignmentExpressionSyntax assignment && assignment.IsDeconstruction();

        public override Location GetDeconstructionReferenceLocation(SyntaxNode node)
        {
            return node switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left.GetLocation(),
                ForEachVariableStatementSyntax @foreach => @foreach.Variable.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind()),
            };
        }

        public override bool IsStatement([NotNullWhen(true)] SyntaxNode? node)
           => node is StatementSyntax;

        public override bool IsExecutableStatement([NotNullWhen(true)] SyntaxNode? node)
            => node is StatementSyntax;

        public override bool IsMethodBody([NotNullWhen(true)] SyntaxNode? node)
        {
            if (node is BlockSyntax ||
                node is ArrowExpressionClauseSyntax)
            {
                return node.Parent is BaseMethodDeclarationSyntax ||
                       node.Parent is AccessorDeclarationSyntax;
            }

            return false;
        }

        public override SyntaxNode? GetExpressionOfReturnStatement(SyntaxNode node)
            => ((ReturnStatementSyntax)node).Expression;

        public override bool IsThisConstructorInitializer(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.ThisConstructorInitializer, out ConstructorInitializerSyntax? constructorInit) &&
               constructorInit.ThisOrBaseKeyword == token;

        public override bool IsBaseConstructorInitializer(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.BaseConstructorInitializer, out ConstructorInitializerSyntax? constructorInit) &&
               constructorInit.ThisOrBaseKeyword == token;

        public override bool IsQueryKeyword(SyntaxToken token)
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

        public override bool IsPredefinedType(SyntaxToken token)
            => TryGetPredefinedType(token, out _);

        public override bool IsPredefinedType(SyntaxToken token, PredefinedType type)
            => TryGetPredefinedType(token, out var actualType) && actualType == type;

        public override bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type)
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

        public override bool IsPredefinedOperator(SyntaxToken token)
            => TryGetPredefinedOperator(token, out var actualOperator) && actualOperator != PredefinedOperator.None;

        public override bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op)
            => TryGetPredefinedOperator(token, out var actualOperator) && actualOperator == op;

        public override bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op)
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

                case SyntaxKind.LessThanToken:
                    return PredefinedOperator.LessThan;

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

        public override string GetText(int kind)
            => SyntaxFacts.GetText((SyntaxKind)kind);

        public override bool IsIdentifierStartCharacter(char c)
            => SyntaxFacts.IsIdentifierStartCharacter(c);

        public override bool IsIdentifierPartCharacter(char c)
            => SyntaxFacts.IsIdentifierPartCharacter(c);

        public override bool IsIdentifierEscapeCharacter(char c)
            => c == '@';

        public override bool IsValidIdentifier(string identifier)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            return this.IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length;
        }

        public override bool IsVerbatimIdentifier(string identifier)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            return this.IsIdentifier(token) && !token.ContainsDiagnostics && token.ToString().Length == identifier.Length && token.IsVerbatimIdentifier();
        }

        public override bool IsTypeCharacter(char c) => false;

        public override bool IsStartOfUnicodeEscapeSequence(char c)
            => c == '\\';

        public override bool IsLiteral(SyntaxToken token)
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

        public override bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token)
            => token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken);

        public override bool IsBindableToken(SyntaxToken token)
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
            if (token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.OrderByClause))
            {
                return true;
            }

            return false;
        }

        public override bool IsPostfixUnaryExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is PostfixUnaryExpressionSyntax;

        public override bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is MemberBindingExpressionSyntax;

        public override bool IsPointerMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
            => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.PointerMemberAccessExpression;

        public override void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity)
        {
            var simpleName = (SimpleNameSyntax)node;
            name = simpleName.Identifier.ValueText;
            arity = simpleName.Arity;
        }

        public override bool LooksGeneric(SyntaxNode simpleName)
            => simpleName.IsKind(SyntaxKind.GenericName) ||
               simpleName.GetLastToken().GetNextToken().Kind() == SyntaxKind.LessThanToken;

        public override SyntaxNode? GetTargetOfMemberBinding(SyntaxNode? node)
            => (node as MemberBindingExpressionSyntax).GetParentConditionalAccessExpression()?.Expression;

        public override SyntaxNode GetNameOfMemberBindingExpression(SyntaxNode node)
            => ((MemberBindingExpressionSyntax)node).Name;

        public override SyntaxNode? GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget)
            => ((MemberAccessExpressionSyntax)node).Expression;

        public override void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
        {
            var elementAccess = (ElementAccessExpressionSyntax)node;
            expression = elementAccess.Expression;
            argumentList = elementAccess.ArgumentList;
        }

        public override SyntaxNode GetExpressionOfInterpolation(SyntaxNode node)
            => ((InterpolationSyntax)node).Expression;

        public override bool IsInStaticContext(SyntaxNode node)
            => node.IsInStaticContext();

        public override bool IsInNamespaceOrTypeContext([NotNullWhen(true)] SyntaxNode? node)
            => SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);

        public override bool IsBaseTypeList([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.BaseList);

        public override SyntaxNode GetExpressionOfArgument(SyntaxNode node)
            => ((ArgumentSyntax)node).Expression;

        public override RefKind GetRefKindOfArgument(SyntaxNode node)
            => ((ArgumentSyntax)node).GetRefKind();

        public override bool IsArgument([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.Argument);

        public override bool IsSimpleArgument([NotNullWhen(true)] SyntaxNode? node)
        {
            return node is ArgumentSyntax argument &&
                   argument.RefOrOutKeyword.Kind() == SyntaxKind.None &&
                   argument.NameColon == null;
        }

        public override bool IsInConstantContext([NotNullWhen(true)] SyntaxNode? node)
            => (node as ExpressionSyntax).IsInConstantContext();

        public override bool IsInConstructor(SyntaxNode node)
            => node.GetAncestor<ConstructorDeclarationSyntax>() != null;

        public override bool IsUnsafeContext(SyntaxNode node)
            => node.IsUnsafeContext();

        public override SyntaxNode GetNameOfAttribute(SyntaxNode node)
            => ((AttributeSyntax)node).Name;

        public override bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] SyntaxNode? node)
            => (node as IdentifierNameSyntax).IsAttributeNamedArgumentIdentifier();

        public override SyntaxNode? GetContainingTypeDeclaration(SyntaxNode? root, int position)
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

        public override SyntaxNode? GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode? node)
            => throw ExceptionUtilities.Unreachable;

        public override bool IsNameOfSubpattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.IdentifierName) &&
               node.IsParentKind(SyntaxKind.NameColon) &&
               node.Parent.IsParentKind(SyntaxKind.Subpattern);

        public override bool IsPropertyPatternClause(SyntaxNode node)
            => node.Kind() == SyntaxKind.PropertyPatternClause;

        public override bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node)
            => IsMemberInitializerNamedAssignmentIdentifier(node, out _);

        public override bool IsMemberInitializerNamedAssignmentIdentifier(
            [NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? initializedInstance)
        {
            initializedInstance = null;
            if (node is IdentifierNameSyntax identifier &&
                identifier.IsLeftSideOfAssignExpression())
            {
                if (identifier.Parent.IsParentKind(SyntaxKind.WithInitializerExpression))
                {
                    var withInitializer = identifier.Parent.GetRequiredParent();
                    initializedInstance = withInitializer.GetRequiredParent();
                    return true;
                }
                else if (identifier.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression))
                {
                    var objectInitializer = identifier.Parent.GetRequiredParent();
                    if (objectInitializer.Parent is BaseObjectCreationExpressionSyntax)
                    {
                        initializedInstance = objectInitializer.Parent;
                        return true;
                    }
                    else if (objectInitializer.IsParentKind(SyntaxKind.SimpleAssignmentExpression, out AssignmentExpressionSyntax? assignment))
                    {
                        initializedInstance = assignment.Left;
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool IsElementAccessExpression(SyntaxNode? node)
            => node.IsKind(SyntaxKind.ElementAccessExpression);

        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? ConvertToSingleLine(SyntaxNode? node, bool useElasticTrivia = false)
            => node.ConvertToSingleLine(useElasticTrivia);

        public override bool IsIndexerMemberCRef(SyntaxNode? node)
            => node.IsKind(SyntaxKind.IndexerMemberCref);

        public override SyntaxNode? GetContainingMemberDeclaration(SyntaxNode? root, int position, bool useFullSpan = true)
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

        public override bool IsMethodLevelMember([NotNullWhen(true)] SyntaxNode? node)
        {
            return node is BaseMethodDeclarationSyntax ||
                node is BasePropertyDeclarationSyntax ||
                node is EnumMemberDeclarationSyntax ||
                node is BaseFieldDeclarationSyntax;
        }

        public override bool IsTopLevelNodeWithMembers([NotNullWhen(true)] SyntaxNode? node)
        {
            return node is BaseNamespaceDeclarationSyntax ||
                   node is TypeDeclarationSyntax ||
                   node is EnumDeclarationSyntax;
        }

        private const string dotToken = ".";

        public override string GetDisplayName(SyntaxNode? node, DisplayNameOptions options, string? rootNamespace = null)
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

            var names = ArrayBuilder<string?>.GetInstance();
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
                while (parent is BaseNamespaceDeclarationSyntax)
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

        private static string? GetName(SyntaxNode node, DisplayNameOptions options)
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
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    return GetName(((BaseNamespaceDeclarationSyntax)node).Name, options);
                case SyntaxKind.QualifiedName:
                    var qualified = (QualifiedNameSyntax)node;
                    return GetName(qualified.Left, options) + dotToken + GetName(qualified.Right, options);
            }

            string? name = null;
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

        public override List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode? root)
        {
            var list = new List<SyntaxNode>();
            AppendMembers(root, list, topLevel: true, methodLevel: true);
            return list;
        }

        public override List<SyntaxNode> GetMethodLevelMembers(SyntaxNode? root)
        {
            var list = new List<SyntaxNode>();
            AppendMembers(root, list, topLevel: false, methodLevel: true);
            return list;
        }

        public override SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration)
            => ((TypeDeclarationSyntax)typeDeclaration).Members;

        private void AppendMembers(SyntaxNode? node, List<SyntaxNode> list, bool topLevel, bool methodLevel)
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

        public override TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node)
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

        public override bool ContainsInMemberBody([NotNullWhen(true)] SyntaxNode? node, TextSpan span)
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

        public override SyntaxNode? TryGetBindableParent(SyntaxToken token)
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

            if (node is VarPatternSyntax)
            {
                return node;
            }

            // Patterns are never bindable (though their constituent types/exprs may be).
            return node is PatternSyntax ? null : node;
        }

        public override IEnumerable<SyntaxNode> GetConstructors(SyntaxNode? root, CancellationToken cancellationToken)
        {
            if (root is not CompilationUnitSyntax compilationUnit)
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
                    case BaseNamespaceDeclarationSyntax @namespace:
                        AppendConstructors(@namespace.Members, constructors, cancellationToken);
                        break;
                    case ClassDeclarationSyntax @class:
                        AppendConstructors(@class.Members, constructors, cancellationToken);
                        break;
                    case RecordDeclarationSyntax record:
                        AppendConstructors(record.Members, constructors, cancellationToken);
                        break;
                    case StructDeclarationSyntax @struct:
                        AppendConstructors(@struct.Members, constructors, cancellationToken);
                        break;
                }
            }
        }

        public override bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace)
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

        public override TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
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

        public override string GetNameForArgument(SyntaxNode? argument)
            => (argument as ArgumentSyntax)?.NameColon?.Name.Identifier.ValueText ?? string.Empty;

        public override string GetNameForAttributeArgument(SyntaxNode? argument)
            => (argument as AttributeArgumentSyntax)?.NameEquals?.Name.Identifier.ValueText ?? string.Empty;

        public override bool IsLeftSideOfDot([NotNullWhen(true)] SyntaxNode? node)
            => (node as ExpressionSyntax).IsLeftSideOfDot();

        public override SyntaxNode? GetRightSideOfDot(SyntaxNode? node)
        {
            return (node as QualifiedNameSyntax)?.Right ??
                (node as MemberAccessExpressionSyntax)?.Name;
        }

        public override SyntaxNode? GetLeftSideOfDot(SyntaxNode? node, bool allowImplicitTarget)
        {
            return (node as QualifiedNameSyntax)?.Left ??
                (node as MemberAccessExpressionSyntax)?.Expression;
        }

        public override bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] SyntaxNode? node)
            => (node as NameSyntax).IsLeftSideOfExplicitInterfaceSpecifier();

        public override bool IsLeftSideOfAssignment([NotNullWhen(true)] SyntaxNode? node)
            => (node as ExpressionSyntax).IsLeftSideOfAssignExpression();

        public override bool IsLeftSideOfAnyAssignment([NotNullWhen(true)] SyntaxNode? node)
            => (node as ExpressionSyntax).IsLeftSideOfAnyAssignExpression();

        public override bool IsLeftSideOfCompoundAssignment([NotNullWhen(true)] SyntaxNode? node)
            => (node as ExpressionSyntax).IsLeftSideOfCompoundAssignExpression();

        public override SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node)
            => ((AssignmentExpressionSyntax)node).Right;

        public override bool IsInferredAnonymousObjectMemberDeclarator([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator, out AnonymousObjectMemberDeclaratorSyntax? anonObject) &&
               anonObject.NameEquals == null;

        public override bool IsOperandOfIncrementExpression([NotNullWhen(true)] SyntaxNode? node)
            => node.IsParentKind(SyntaxKind.PostIncrementExpression) ||
               node.IsParentKind(SyntaxKind.PreIncrementExpression);

        public static bool IsOperandOfDecrementExpression([NotNullWhen(true)] SyntaxNode? node)
            => node.IsParentKind(SyntaxKind.PostDecrementExpression) ||
               node.IsParentKind(SyntaxKind.PreDecrementExpression);

        public override bool IsOperandOfIncrementOrDecrementExpression([NotNullWhen(true)] SyntaxNode? node)
            => IsOperandOfIncrementExpression(node) || IsOperandOfDecrementExpression(node);

        public override SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString)
            => ((InterpolatedStringExpressionSyntax)interpolatedString).Contents;

        public override bool IsVerbatimStringLiteral(SyntaxToken token)
            => token.IsVerbatimStringLiteral();

        public override bool IsNumericLiteral(SyntaxToken token)
            => token.Kind() == SyntaxKind.NumericLiteralToken;

        public override SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode invocationExpression)
            => GetArgumentsOfArgumentList(((InvocationExpressionSyntax)invocationExpression).ArgumentList);

        public override SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode objectCreationExpression)
            => ((BaseObjectCreationExpressionSyntax)objectCreationExpression).ArgumentList is { } argumentList
                ? GetArgumentsOfArgumentList(argumentList)
                : default;

        public override SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode argumentList)
            => ((BaseArgumentListSyntax)argumentList).Arguments;

        public override bool IsRegularComment(SyntaxTrivia trivia)
            => trivia.IsRegularComment();

        public override bool IsDocumentationComment(SyntaxTrivia trivia)
            => trivia.IsDocComment();

        public override bool IsElastic(SyntaxTrivia trivia)
            => trivia.IsElastic();

        public override bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes)
            => trivia.IsPragmaDirective(out isDisable, out isActive, out errorCodes);

        public override bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia;

        public override bool IsDocumentationComment(SyntaxNode node)
            => SyntaxFacts.IsDocumentationCommentTrivia(node.Kind());

        public override bool IsUsingOrExternOrImport([NotNullWhen(true)] SyntaxNode? node)
        {
            return node.IsKind(SyntaxKind.UsingDirective) ||
                   node.IsKind(SyntaxKind.ExternAliasDirective);
        }

        public override bool IsGlobalAssemblyAttribute([NotNullWhen(true)] SyntaxNode? node)
            => IsGlobalAttribute(node, SyntaxKind.AssemblyKeyword);

        public override bool IsGlobalModuleAttribute([NotNullWhen(true)] SyntaxNode? node)
            => IsGlobalAttribute(node, SyntaxKind.ModuleKeyword);

        private static bool IsGlobalAttribute([NotNullWhen(true)] SyntaxNode? node, SyntaxKind attributeTarget)
            => node.IsKind(SyntaxKind.Attribute) &&
               node.Parent.IsKind(SyntaxKind.AttributeList, out AttributeListSyntax? attributeList) &&
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
                    return node.Parent.IsParentKind(SyntaxKind.FieldDeclaration) ||
                           node.Parent.IsParentKind(SyntaxKind.EventFieldDeclaration);

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

        public override bool IsDeclaration(SyntaxNode node)
            => SyntaxFacts.IsNamespaceMemberDeclaration(node.Kind()) || IsMemberDeclaration(node);

        public override bool IsTypeDeclaration(SyntaxNode node)
            => SyntaxFacts.IsTypeDeclaration(node.Kind());

        public override bool IsSimpleAssignmentStatement([NotNullWhen(true)] SyntaxNode? statement)
            => statement.IsKind(SyntaxKind.ExpressionStatement, out ExpressionStatementSyntax? exprStatement) &&
               exprStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);

        public override void GetPartsOfAssignmentStatement(
            SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            GetPartsOfAssignmentExpressionOrStatement(
                ((ExpressionStatementSyntax)statement).Expression, out left, out operatorToken, out right);
        }

        public override void GetPartsOfAssignmentExpressionOrStatement(
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

        public override SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node)
            => ((SimpleNameSyntax)node).Identifier;

        public override SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclaratorSyntax)node).Identifier;

        public override SyntaxToken GetIdentifierOfParameter(SyntaxNode node)
            => ((ParameterSyntax)node).Identifier;

        public override SyntaxToken GetIdentifierOfTypeDeclaration(SyntaxNode node)
            => node switch
            {
                BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier,
                DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier,
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };

        public override SyntaxToken GetIdentifierOfIdentifierName(SyntaxNode node)
            => ((IdentifierNameSyntax)node).Identifier;

        public override bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement)
        {
            return ((LocalDeclarationStatementSyntax)localDeclarationStatement).Declaration.Variables.Contains(
                (VariableDeclaratorSyntax)declarator);
        }

        public override bool AreEquivalent(SyntaxToken token1, SyntaxToken token2)
            => SyntaxFactory.AreEquivalent(token1, token2);

        public override bool AreEquivalent(SyntaxNode? node1, SyntaxNode? node2)
            => SyntaxFactory.AreEquivalent(node1, node2);

        public static SyntaxNode GetExpressionOfInvocationExpression(SyntaxNode node)
            => ((InvocationExpressionSyntax)node).Expression;

        public override bool IsExpressionOfForeach([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent is ForEachStatementSyntax foreachStatement && foreachStatement.Expression == node;

        public override SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node)
            => ((ExpressionStatementSyntax)node).Expression;

        public override bool IsIsExpression([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.IsExpression);

        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? WalkDownParentheses(SyntaxNode? node)
            => (node as ExpressionSyntax)?.WalkDownParentheses() ?? node;

        public override void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
            out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen)
        {
            var tupleExpression = (TupleExpressionSyntax)node;
            openParen = tupleExpression.OpenParenToken;
            arguments = (SeparatedSyntaxList<SyntaxNode>)tupleExpression.Arguments;
            closeParen = tupleExpression.CloseParenToken;
        }

        public override bool IsPreprocessorDirective(SyntaxTrivia trivia)
            => SyntaxFacts.IsPreprocessorDirective(trivia.Kind());

        public override bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken)
            => token.ContainsInterleavedDirective(span, cancellationToken);

        public override SyntaxTokenList GetModifiers(SyntaxNode? node)
            => node.GetModifiers();

        public override SyntaxNode? WithModifiers(SyntaxNode? node, SyntaxTokenList modifiers)
            => node.WithModifiers(modifiers);

        public override SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node)
            => ((LocalDeclarationStatementSyntax)node).Declaration.Variables;

        public override SyntaxNode? GetInitializerOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclaratorSyntax)node).Initializer;

        public override SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node)
            => ((VariableDeclarationSyntax)((VariableDeclaratorSyntax)node).Parent!).Type;

        public override SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node)
            => ((EqualsValueClauseSyntax?)node)?.Value;

        public override bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.Block);

        public override bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection, SyntaxKind.CompilationUnit);

        public override IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode? node)
        {
            return node switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSection => switchSection.Statements,
                CompilationUnitSyntax compilationUnit => compilationUnit.Members.OfType<GlobalStatementSyntax>().SelectAsArray(globalStatement => globalStatement.Statement),
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };
        }

        public override SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes)
            => nodes.FindInnermostCommonNode(node => IsExecutableBlock(node));

        public override bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node)
            => IsExecutableBlock(node) || node.IsEmbeddedStatementOwner();

        public override IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode? node)
        {
            if (IsExecutableBlock(node))
                return GetExecutableBlockStatements(node);
            else if (node.GetEmbeddedStatement() is { } embeddedStatement)
                return ImmutableArray.Create<SyntaxNode>(embeddedStatement);
            else
                return ImmutableArray<SyntaxNode>.Empty;
        }

        public override bool IsConversionExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is CastExpressionSyntax;

        public override bool IsCastExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is CastExpressionSyntax;

        public override void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression)
        {
            var cast = (CastExpressionSyntax)node;
            type = cast.Type;
            expression = cast.Expression;
        }

        public override SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.OverrideKeyword && token.Parent is MemberDeclarationSyntax member)
            {
                return member.GetNameToken();
            }

            return null;
        }

        public override SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode? node)
            => node.GetAttributeLists();

        public override bool IsParameterNameXmlElementSyntax([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.XmlElement, out XmlElementSyntax? xmlElement) &&
            xmlElement.StartTag.Name.LocalName.ValueText == DocumentationCommentXmlNames.ParameterElementName;

        public override SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia)
        {
            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationCommentTrivia)
            {
                return documentationCommentTrivia.Content;
            }

            throw ExceptionUtilities.UnexpectedValue(trivia.Kind());
        }

        public override bool IsIsPatternExpression([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.IsPatternExpression);

        public override void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right)
        {
            var isPatternExpression = (IsPatternExpressionSyntax)node;
            left = isPatternExpression.Expression;
            isToken = isPatternExpression.IsKeyword;
            right = isPatternExpression.Pattern;
        }

        public override bool IsAnyPattern([NotNullWhen(true)] SyntaxNode? node)
            => node is PatternSyntax;

        public override bool IsConstantPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.ConstantPattern);

        public override bool IsDeclarationPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.DeclarationPattern);

        public override bool IsRecursivePattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.RecursivePattern);

        public override bool IsVarPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.VarPattern);

        public override SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node)
            => ((ConstantPatternSyntax)node).Expression;

        public override void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation)
        {
            var declarationPattern = (DeclarationPatternSyntax)node;
            type = declarationPattern.Type;
            designation = declarationPattern.Designation;
        }

        public override void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode? type, out SyntaxNode? positionalPart, out SyntaxNode? propertyPart, out SyntaxNode? designation)
        {
            var recursivePattern = (RecursivePatternSyntax)node;
            type = recursivePattern.Type;
            positionalPart = recursivePattern.PositionalPatternClause;
            propertyPart = recursivePattern.PropertyPatternClause;
            designation = recursivePattern.Designation;
        }

        public override bool SupportsNotPattern(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion.IsCSharp9OrAbove();

        public override bool IsAndPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.AndPattern);

        public override bool IsBinaryPattern([NotNullWhen(true)] SyntaxNode? node)
            => node is BinaryPatternSyntax;

        public override bool IsNotPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.NotPattern);

        public override bool IsOrPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.OrPattern);

        public override bool IsParenthesizedPattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.ParenthesizedPattern);

        public override bool IsTypePattern([NotNullWhen(true)] SyntaxNode? node)
            => node.IsKind(SyntaxKind.TypePattern);

        public override bool IsUnaryPattern([NotNullWhen(true)] SyntaxNode? node)
            => node is UnaryPatternSyntax;

        public override void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen)
        {
            var parenthesizedPattern = (ParenthesizedPatternSyntax)node;
            openParen = parenthesizedPattern.OpenParenToken;
            pattern = parenthesizedPattern.Pattern;
            closeParen = parenthesizedPattern.CloseParenToken;
        }

        public override void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var binaryPattern = (BinaryPatternSyntax)node;
            left = binaryPattern.Left;
            operatorToken = binaryPattern.OperatorToken;
            right = binaryPattern.Right;
        }

        public override void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern)
        {
            var unaryPattern = (UnaryPatternSyntax)node;
            operatorToken = unaryPattern.OperatorToken;
            pattern = unaryPattern.Pattern;
        }

        public override SyntaxNode GetTypeOfTypePattern(SyntaxNode node)
            => ((TypePatternSyntax)node).Type;

        public override void GetPartsOfInterpolationExpression(SyntaxNode node,
            out SyntaxToken stringStartToken, out SyntaxList<SyntaxNode> contents, out SyntaxToken stringEndToken)
        {
            var interpolatedStringExpression = (InterpolatedStringExpressionSyntax)node;
            stringStartToken = interpolatedStringExpression.StringStartToken;
            contents = interpolatedStringExpression.Contents;
            stringEndToken = interpolatedStringExpression.StringEndToken;
        }

        public override bool IsVerbatimInterpolatedStringExpression(SyntaxNode node)
            => node is InterpolatedStringExpressionSyntax interpolatedString &&
                interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);

        #region IsXXX members

        public override bool IsAnonymousFunctionExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is AnonymousFunctionExpressionSyntax;

        public override bool IsBaseNamespaceDeclaration([NotNullWhen(true)] SyntaxNode? node)
            => node is BaseNamespaceDeclarationSyntax;

        public override bool IsBinaryExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is BinaryExpressionSyntax;

        public override bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is LiteralExpressionSyntax;

        public override bool IsMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
            => node is MemberAccessExpressionSyntax;

        public override bool IsSimpleName([NotNullWhen(true)] SyntaxNode? node)
            => node is SimpleNameSyntax;

        #endregion

        #region GetPartsOfXXX members

        public override void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var binaryExpression = (BinaryExpressionSyntax)node;
            left = binaryExpression.Left;
            operatorToken = binaryExpression.OperatorToken;
            right = binaryExpression.Right;
        }

        public override void GetPartsOfCompilationUnit(SyntaxNode node, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> attributeLists, out SyntaxList<SyntaxNode> members)
        {
            var compilationUnit = (CompilationUnitSyntax)node;
            imports = compilationUnit.Usings;
            attributeLists = compilationUnit.AttributeLists;
            members = compilationUnit.Members;
        }

        public override void GetPartsOfConditionalAccessExpression(
            SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull)
        {
            var conditionalAccess = (ConditionalAccessExpressionSyntax)node;
            expression = conditionalAccess.Expression;
            operatorToken = conditionalAccess.OperatorToken;
            whenNotNull = conditionalAccess.WhenNotNull;
        }

        public override void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse)
        {
            var conditionalExpression = (ConditionalExpressionSyntax)node;
            condition = conditionalExpression.Condition;
            whenTrue = conditionalExpression.WhenTrue;
            whenFalse = conditionalExpression.WhenFalse;
        }

        public override void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode? argumentList)
        {
            var invocation = (InvocationExpressionSyntax)node;
            expression = invocation.Expression;
            argumentList = invocation.ArgumentList;
        }

        public override void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name)
        {
            var memberAccess = (MemberAccessExpressionSyntax)node;
            expression = memberAccess.Expression;
            operatorToken = memberAccess.OperatorToken;
            name = memberAccess.Name;
        }

        public override void GetPartsOfBaseNamespaceDeclaration(SyntaxNode node, out SyntaxNode name, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> members)
        {
            var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)node;
            name = namespaceDeclaration.Name;
            imports = namespaceDeclaration.Usings;
            members = namespaceDeclaration.Members;
        }

        public override void GetPartsOfObjectCreationExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode? argumentList, out SyntaxNode? initializer)
        {
            var objectCreationExpression = (ObjectCreationExpressionSyntax)node;
            type = objectCreationExpression.Type;
            argumentList = objectCreationExpression.ArgumentList;
            initializer = objectCreationExpression.Initializer;
        }

        public override void GetPartsOfParenthesizedExpression(
            SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen)
        {
            var parenthesizedExpression = (ParenthesizedExpressionSyntax)node;
            openParen = parenthesizedExpression.OpenParenToken;
            expression = parenthesizedExpression.Expression;
            closeParen = parenthesizedExpression.CloseParenToken;
        }

        public override void GetPartsOfPrefixUnaryExpression(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode operand)
        {
            var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)node;
            operatorToken = prefixUnaryExpression.OperatorToken;
            operand = prefixUnaryExpression.Operand;
        }

        public override void GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            var qualifiedName = (QualifiedNameSyntax)node;
            left = qualifiedName.Left;
            operatorToken = qualifiedName.DotToken;
            right = qualifiedName.Right;
        }

        #endregion

        #region GetXXXOfYYY members

        public override SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node)
            => ((AwaitExpressionSyntax)node).Expression;

        public override SyntaxNode GetExpressionOfThrowExpression(SyntaxNode node)
            => ((ThrowExpressionSyntax)node).Expression;

        #endregion
    }
}
