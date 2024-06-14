// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal class CSharpSyntaxFacts : ISyntaxFacts
{
    internal static readonly CSharpSyntaxFacts Instance = new();

    protected CSharpSyntaxFacts()
    {
    }

    public bool IsCaseSensitive => true;

    public StringComparer StringComparer { get; } = StringComparer.Ordinal;

    public SyntaxTrivia ElasticMarker
        => SyntaxFactory.ElasticMarker;

    public SyntaxTrivia ElasticCarriageReturnLineFeed
        => SyntaxFactory.ElasticCarriageReturnLineFeed;

    public ISyntaxKinds SyntaxKinds { get; } = CSharpSyntaxKinds.Instance;

    public bool SupportsIndexingInitializer(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp6;

    public bool SupportsThrowExpression(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7;

    public bool SupportsLocalFunctionDeclaration(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7;

    public bool SupportsRecord(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp9;

    public bool SupportsRecordStruct(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp10;

    public bool SupportsTargetTypedConditionalExpression(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp9;

    public bool SupportsConstantInterpolatedStrings(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp10;

    public bool SupportsTupleDeconstruction(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7;

    // Should be supported in C# 13.
    public bool SupportsCollectionExpressionNaturalType(ParseOptions options)
        => false;

    public SyntaxToken ParseToken(string text)
        => SyntaxFactory.ParseToken(text);

    public SyntaxTriviaList ParseLeadingTrivia(string text)
        => SyntaxFactory.ParseLeadingTrivia(text);

    public string EscapeIdentifier(string identifier)
    {
        var nullIndex = identifier.IndexOf('\0');
        if (nullIndex >= 0)
        {
            identifier = identifier[..nullIndex];
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
            (SyntaxFacts.IsBinaryExpression(kind) && (token.Parent is BinaryExpressionSyntax or OperatorDeclarationSyntax or RelationalPatternSyntax)) ||
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

    public bool IsEntirelyWithinStringOrCharOrNumericLiteral([NotNullWhen(true)] SyntaxTree? syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (syntaxTree == null)
        {
            return false;
        }

        return syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken);
    }

    public bool IsDirective([NotNullWhen(true)] SyntaxNode? node)
        => node is DirectiveTriviaSyntax;

    public bool TryGetExternalSourceInfo([NotNullWhen(true)] SyntaxNode? node, out ExternalSourceInfo info)
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

    [return: NotNullIfNotNull(nameof(node))]
    public SyntaxNode? GetStandaloneExpression(SyntaxNode? node)
        => node is ExpressionSyntax expression ? SyntaxFactory.GetStandaloneExpression(expression) : node;

    public SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node)
        => node.GetRootConditionalAccessExpression();

    public bool IsDeclarationExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is DeclarationExpressionSyntax;

    public bool IsNamedArgument([NotNullWhen(true)] SyntaxNode? node)
        => node is ArgumentSyntax arg && arg.NameColon != null;

    public bool IsNameOfNamedArgument([NotNullWhen(true)] SyntaxNode? node)
        => node.CheckParent<NameColonSyntax>(p => p.Name == node);

    public SyntaxNode? GetParameterList(SyntaxNode node)
        => node.GetParameterList();

    public bool IsParameterList([NotNullWhen(true)] SyntaxNode? node)
        => node is (kind: SyntaxKind.ParameterList or SyntaxKind.BracketedParameterList);

    public bool IsUsingDirectiveName([NotNullWhen(true)] SyntaxNode? node)
        => node?.Parent is UsingDirectiveSyntax usingDirective &&
           usingDirective.NamespaceOrType == node;

    public bool IsUsingAliasDirective([NotNullWhen(true)] SyntaxNode? node)
        => node is UsingDirectiveSyntax usingDirectiveNode && usingDirectiveNode.Alias != null;

    public void GetPartsOfUsingAliasDirective(SyntaxNode node, out SyntaxToken globalKeyword, out SyntaxToken alias, out SyntaxNode name)
    {
        var usingDirective = (UsingDirectiveSyntax)node;
        globalKeyword = usingDirective.GlobalKeyword;
        alias = usingDirective.Alias!.Name.Identifier;
        name = usingDirective.NamespaceOrType;
    }

    public bool IsDeconstructionForEachStatement([NotNullWhen(true)] SyntaxNode? node)
        => node is ForEachVariableStatementSyntax;

    public bool IsDeconstructionAssignment([NotNullWhen(true)] SyntaxNode? node)
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

    public bool IsStatement([NotNullWhen(true)] SyntaxNode? node)
       => node is StatementSyntax;

    public bool IsExecutableStatement([NotNullWhen(true)] SyntaxNode? node)
        => node is StatementSyntax;

    public bool IsGlobalStatement([NotNullWhen(true)] SyntaxNode? node)
       => node is GlobalStatementSyntax;

    public SyntaxNode GetStatementOfGlobalStatement(SyntaxNode node)
        => ((GlobalStatementSyntax)node).Statement;

    public bool AreStatementsInSameContainer(SyntaxNode firstStatement, SyntaxNode secondStatement)
    {
        Debug.Assert(IsStatement(firstStatement));
        Debug.Assert(IsStatement(secondStatement));

        if (firstStatement.Parent == secondStatement.Parent)
            return true;

        if (IsGlobalStatement(firstStatement.Parent)
            && IsGlobalStatement(secondStatement.Parent)
            && firstStatement.Parent.Parent == secondStatement.Parent.Parent)
        {
            return true;
        }

        return false;

    }

    public bool IsMethodBody([NotNullWhen(true)] SyntaxNode? node)
    {
        if (node is BlockSyntax or
            ArrowExpressionClauseSyntax)
        {
            return node.Parent is BaseMethodDeclarationSyntax or
                   AccessorDeclarationSyntax;
        }

        return false;
    }

    public SyntaxNode GetExpressionOfRefExpression(SyntaxNode node)
        => ((RefExpressionSyntax)node).Expression;

    public SyntaxNode? GetExpressionOfReturnStatement(SyntaxNode node)
        => ((ReturnStatementSyntax)node).Expression;

    public bool IsThisConstructorInitializer(SyntaxToken token)
        => token.Parent is ConstructorInitializerSyntax(SyntaxKind.ThisConstructorInitializer) constructorInit &&
           constructorInit.ThisOrBaseKeyword == token;

    public bool IsBaseConstructorInitializer(SyntaxToken token)
        => token.Parent is ConstructorInitializerSyntax(SyntaxKind.BaseConstructorInitializer) constructorInit &&
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
                return token.Parent is (kind: SyntaxKind.JoinIntoClause or SyntaxKind.QueryContinuation);
            default:
                return false;
        }
    }

    public bool IsPredefinedType(SyntaxToken token)
        => TryGetPredefinedType(token, out _);

    public bool IsPredefinedType(SyntaxToken token, PredefinedType type)
        => TryGetPredefinedType(token, out var actualType) && actualType == type;

    public bool IsPredefinedType(SyntaxNode? node)
        => node is PredefinedTypeSyntax predefinedType && IsPredefinedType(predefinedType.Keyword);

    public bool IsPredefinedType(SyntaxNode? node, PredefinedType type)
        => node is PredefinedTypeSyntax predefinedType && IsPredefinedType(predefinedType.Keyword, type);

    public bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type)
    {
        type = GetPredefinedType(token);
        return type != PredefinedType.None;
    }

    private static PredefinedType GetPredefinedType(SyntaxToken token)
        => token.Kind() switch
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
            SyntaxKind.IdentifierToken => token.Text switch
            {
                "nint" => PredefinedType.IntPtr,
                "nuint" => PredefinedType.UIntPtr,
                _ => PredefinedType.None,
            },
            _ => PredefinedType.None,
        };

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

            case SyntaxKind.AmpersandAmpersandToken: // overridden bitwise & can be accessed through &&
            case SyntaxKind.AmpersandToken:
            case SyntaxKind.AmpersandEqualsToken:
                return PredefinedOperator.BitwiseAnd;

            case SyntaxKind.BarBarToken: // overridden bitwise | can be accessed through ||
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

            case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
            case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                return PredefinedOperator.UnsignedRightShift;
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
            case SyntaxKind.Utf8StringLiteralToken:
            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
            case SyntaxKind.MultiLineRawStringLiteralToken:
            case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
            case SyntaxKind.NullKeyword:
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.InterpolatedStringStartToken:
            case SyntaxKind.InterpolatedStringEndToken:
            case SyntaxKind.InterpolatedRawStringEndToken:
            case SyntaxKind.InterpolatedVerbatimStringStartToken:
            case SyntaxKind.InterpolatedStringTextToken:
            case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
            case SyntaxKind.InterpolatedMultiLineRawStringStartToken:
                return true;
            default:
                return false;
        }
    }

    public bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token)
        => token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken;

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
        if (token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.OrderByClause))
        {
            return true;
        }

        if (token.Kind() is SyntaxKind.OpenBracketToken or SyntaxKind.CloseBracketToken
            && token.Parent.IsKind(SyntaxKind.CollectionExpression))
        {
            return true;
        }

        return false;
    }

    public bool IsPostfixUnaryExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is PostfixUnaryExpressionSyntax;

    public bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is MemberBindingExpressionSyntax;

    public bool IsPointerMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
        => (node as MemberAccessExpressionSyntax)?.Kind() == SyntaxKind.PointerMemberAccessExpression;

    public void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity)
    {
        var simpleName = (SimpleNameSyntax)node;
        name = simpleName.Identifier.ValueText;
        arity = simpleName.Arity;
    }

    public bool LooksGeneric(SyntaxNode simpleName)
        => simpleName.IsKind(SyntaxKind.GenericName) ||
           simpleName.GetLastToken().GetNextToken().Kind() == SyntaxKind.LessThanToken;

    public SeparatedSyntaxList<SyntaxNode> GetTypeArgumentsOfGenericName(SyntaxNode? genericName)
        => (genericName as GenericNameSyntax)?.TypeArgumentList.Arguments ?? default;

    public SyntaxNode? GetTargetOfMemberBinding(SyntaxNode? node)
        => (node as MemberBindingExpressionSyntax).GetParentConditionalAccessExpression()?.Expression;

    public SyntaxNode GetNameOfMemberBindingExpression(SyntaxNode node)
        => ((MemberBindingExpressionSyntax)node).Name;

    public SyntaxNode? GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget)
        => ((MemberAccessExpressionSyntax)node).Expression;

    public void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
    {
        var elementAccess = (ElementAccessExpressionSyntax)node;
        expression = elementAccess.Expression;
        argumentList = elementAccess.ArgumentList;
    }

    public SyntaxNode GetExpressionOfInterpolation(SyntaxNode node)
        => ((InterpolationSyntax)node).Expression;

    public bool IsInStaticContext(SyntaxNode node)
        => node.IsInStaticContext();

    public bool IsInNamespaceOrTypeContext([NotNullWhen(true)] SyntaxNode? node)
        => SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);

    public bool IsBaseTypeList([NotNullWhen(true)] SyntaxNode? node)
        => node.IsKind(SyntaxKind.BaseList);

    public SyntaxNode GetExpressionOfArgument(SyntaxNode node)
        => ((ArgumentSyntax)node).Expression;

    public SyntaxNode GetExpressionOfAttributeArgument(SyntaxNode node)
        => ((AttributeArgumentSyntax)node).Expression;

    public RefKind GetRefKindOfArgument(SyntaxNode node)
        => ((ArgumentSyntax)node).GetRefKind();

    public bool IsArgument([NotNullWhen(true)] SyntaxNode? node)
        => node.IsKind(SyntaxKind.Argument);

    public bool IsAttributeArgument([NotNullWhen(true)] SyntaxNode? node)
        => node.IsKind(SyntaxKind.AttributeArgument);

    public bool IsSimpleArgument([NotNullWhen(true)] SyntaxNode? node)
    {
        return node is ArgumentSyntax argument &&
               argument.RefOrOutKeyword.Kind() == SyntaxKind.None &&
               argument.NameColon == null;
    }

    public bool IsInConstantContext([NotNullWhen(true)] SyntaxNode? node)
        => (node as ExpressionSyntax).IsInConstantContext();

    public bool IsInConstructor(SyntaxNode node)
        => node.GetAncestor<ConstructorDeclarationSyntax>() != null;

    public bool IsUnsafeContext(SyntaxNode node)
        => node.IsUnsafeContext();

    public bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] SyntaxNode? node)
        => (node as IdentifierNameSyntax).IsAttributeNamedArgumentIdentifier();

    public SyntaxNode? GetContainingTypeDeclaration(SyntaxNode root, int position)
    {
        return root
            .FindToken(position)
            .GetAncestors<SyntaxNode>()
            .FirstOrDefault(n => n is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
    }

    public SyntaxNode? GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode? node)
        => throw ExceptionUtilities.Unreachable();

    public bool IsNameOfSubpattern([NotNullWhen(true)] SyntaxNode? node)
        => node.IsKind(SyntaxKind.IdentifierName) &&
           node.IsParentKind(SyntaxKind.NameColon) &&
           node.Parent.IsParentKind(SyntaxKind.Subpattern);

    public bool IsPropertyPatternClause(SyntaxNode node)
        => node.Kind() == SyntaxKind.PropertyPatternClause;

    public bool IsMemberInitializerNamedAssignmentIdentifier(
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
                else if (objectInitializer?.Parent is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) assignment)
                {
                    initializedInstance = assignment.Left;
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsAnyInitializerExpression([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? creationExpression)
    {
        if (node is InitializerExpressionSyntax
            {
                Parent: BaseObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax
            })
        {
            creationExpression = node.Parent;
            return true;
        }

        creationExpression = null;
        return false;
    }

    public bool IsElementAccessExpression(SyntaxNode? node)
        => node.IsKind(SyntaxKind.ElementAccessExpression);

    [return: NotNullIfNotNull(nameof(node))]
    public SyntaxNode? ConvertToSingleLine(SyntaxNode? node, bool useElasticTrivia = false)
        => node.ConvertToSingleLine(useElasticTrivia);

    public SyntaxNode? GetContainingMemberDeclaration(SyntaxNode root, int position, bool useFullSpan = true)
        => GetContainingMemberDeclaration<MemberDeclarationSyntax>(root, position, useFullSpan);

    public SyntaxNode? GetContainingMethodDeclaration(SyntaxNode root, int position, bool useFullSpan = true)
        => GetContainingMemberDeclaration<BaseMethodDeclarationSyntax>(root, position, useFullSpan);

    private static SyntaxNode? GetContainingMemberDeclaration<TMemberDeclarationSyntax>(SyntaxNode root, int position, bool useFullSpan = true)
        where TMemberDeclarationSyntax : MemberDeclarationSyntax
    {
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
                if ((kind != SyntaxKind.GlobalStatement) && (kind != SyntaxKind.IncompleteMember) && (node is TMemberDeclarationSyntax))
                {
                    return node;
                }
            }

            node = node.Parent;
        }

        return null;
    }

    public bool IsMethodLevelMember([NotNullWhen(true)] SyntaxNode? node)
    {
        return node is BaseMethodDeclarationSyntax or
            BasePropertyDeclarationSyntax or
            EnumMemberDeclarationSyntax or
            BaseFieldDeclarationSyntax;
    }

    public bool IsTopLevelNodeWithMembers([NotNullWhen(true)] SyntaxNode? node)
    {
        return node is BaseNamespaceDeclarationSyntax or
               TypeDeclarationSyntax or
               EnumDeclarationSyntax;
    }

    private const string dotToken = ".";

    public string GetDisplayName(SyntaxNode? node, DisplayNameOptions options, string? rootNamespace = null)
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

        while (names.TryPop(out var name))
        {
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

    public List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode? root)
    {
        var list = new List<SyntaxNode>();
        AppendMembers(root, list, topLevel: true, methodLevel: true);
        return list;
    }

    public List<SyntaxNode> GetMethodLevelMembers(SyntaxNode? root)
    {
        var list = new List<SyntaxNode>();
        AppendMembers(root, list, topLevel: false, methodLevel: true);
        return list;
    }

    public SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration)
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

    public bool ContainsInMemberBody([NotNullWhen(true)] SyntaxNode? node, TextSpan span)
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
            if (parent is not NameSyntax)
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

    public IEnumerable<SyntaxNode> GetConstructors(SyntaxNode? root, CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return [];

        var constructors = new List<SyntaxNode>();
        AppendConstructors(compilationUnit.Members, constructors, cancellationToken);
        return constructors;
    }

    private static void AppendConstructors(SyntaxList<MemberDeclarationSyntax> members, List<SyntaxNode> constructors, CancellationToken cancellationToken)
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

    public string GetNameForArgument(SyntaxNode? argument)
        => (argument as ArgumentSyntax)?.NameColon?.Name.Identifier.ValueText ?? string.Empty;

    public string GetNameForAttributeArgument(SyntaxNode? argument)
        => (argument as AttributeArgumentSyntax)?.NameEquals?.Name.Identifier.ValueText ?? string.Empty;

    public bool IsLeftSideOfDot([NotNullWhen(true)] SyntaxNode? node)
        => (node as ExpressionSyntax).IsLeftSideOfDot();

    public SyntaxNode? GetRightSideOfDot(SyntaxNode? node)
    {
        return (node as QualifiedNameSyntax)?.Right ??
            (node as MemberAccessExpressionSyntax)?.Name;
    }

    public SyntaxNode? GetLeftSideOfDot(SyntaxNode? node, bool allowImplicitTarget)
    {
        return (node as QualifiedNameSyntax)?.Left ??
            (node as MemberAccessExpressionSyntax)?.Expression;
    }

    public bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] SyntaxNode? node)
        => (node as NameSyntax).IsLeftSideOfExplicitInterfaceSpecifier();

    public bool IsLeftSideOfAssignment([NotNullWhen(true)] SyntaxNode? node)
        => (node as ExpressionSyntax).IsLeftSideOfAssignExpression();

    public bool IsLeftSideOfAnyAssignment([NotNullWhen(true)] SyntaxNode? node)
        => (node as ExpressionSyntax).IsLeftSideOfAnyAssignExpression();

    public bool IsLeftSideOfCompoundAssignment([NotNullWhen(true)] SyntaxNode? node)
        => (node as ExpressionSyntax).IsLeftSideOfCompoundAssignExpression();

    public SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node)
        => ((AssignmentExpressionSyntax)node).Right;

    public bool IsInferredAnonymousObjectMemberDeclarator([NotNullWhen(true)] SyntaxNode? node)
        => node is AnonymousObjectMemberDeclaratorSyntax anonObject &&
           anonObject.NameEquals == null;

    public bool IsOperandOfIncrementExpression([NotNullWhen(true)] SyntaxNode? node)
        => node?.Parent?.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PreIncrementExpression;

    public static bool IsOperandOfDecrementExpression([NotNullWhen(true)] SyntaxNode? node)
        => node?.Parent?.Kind() is SyntaxKind.PostDecrementExpression or SyntaxKind.PreDecrementExpression;

    public bool IsOperandOfIncrementOrDecrementExpression([NotNullWhen(true)] SyntaxNode? node)
        => IsOperandOfIncrementExpression(node) || IsOperandOfDecrementExpression(node);

    public SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString)
        => ((InterpolatedStringExpressionSyntax)interpolatedString).Contents;

    public bool IsVerbatimStringLiteral(SyntaxToken token)
        => token.IsVerbatimStringLiteral();

    public bool IsNumericLiteral(SyntaxToken token)
        => token.Kind() == SyntaxKind.NumericLiteralToken;

    public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode objectCreationExpression)
        => ((BaseObjectCreationExpressionSyntax)objectCreationExpression).ArgumentList is { } argumentList
            ? GetArgumentsOfArgumentList(argumentList)
            : default;

    public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode argumentList)
        => ((BaseArgumentListSyntax)argumentList).Arguments;

    public SeparatedSyntaxList<SyntaxNode> GetArgumentsOfAttributeArgumentList(SyntaxNode argumentList)
        => ((AttributeArgumentListSyntax)argumentList).Arguments;

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

    public bool IsUsingOrExternOrImport([NotNullWhen(true)] SyntaxNode? node)
        => node?.Kind() is SyntaxKind.UsingDirective or SyntaxKind.ExternAliasDirective;

    public bool IsGlobalAssemblyAttribute([NotNullWhen(true)] SyntaxNode? node)
        => IsGlobalAttribute(node, SyntaxKind.AssemblyKeyword);

    public bool IsGlobalModuleAttribute([NotNullWhen(true)] SyntaxNode? node)
        => IsGlobalAttribute(node, SyntaxKind.ModuleKeyword);

    private static bool IsGlobalAttribute([NotNullWhen(true)] SyntaxNode? node, SyntaxKind attributeTarget)
        => node.IsKind(SyntaxKind.Attribute) &&
           node.Parent is AttributeListSyntax attributeList &&
           attributeList.Target?.Identifier.Kind() == attributeTarget;

    public bool IsDeclaration(SyntaxNode? node)
    {
        if (node is null)
            return false;

        if (SyntaxFacts.IsNamespaceMemberDeclaration(node.Kind()))
            return true;

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
                return node.Parent?.Parent?.Kind() is SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration;

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

    public bool IsTypeDeclaration(SyntaxNode node)
        => SyntaxFacts.IsTypeDeclaration(node.Kind());

    public bool IsSimpleAssignmentStatement([NotNullWhen(true)] SyntaxNode? statement)
        => statement is ExpressionStatementSyntax exprStatement &&
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

    // C# does not have assignment statements.
    public bool IsAnyAssignmentStatement([NotNullWhen(true)] SyntaxNode? node)
        => false;

    public SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node)
        => ((SimpleNameSyntax)node).Identifier;

    public SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node)
        => ((VariableDeclaratorSyntax)node).Identifier;

    public SyntaxToken GetIdentifierOfTypeDeclaration(SyntaxNode node)
        => node switch
        {
            BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier,
            DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier,
            _ => throw ExceptionUtilities.UnexpectedValue(node),
        };

    public bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement)
        => declarator is VariableDeclaratorSyntax variableDeclarator &&
           ((LocalDeclarationStatementSyntax)localDeclarationStatement).Declaration.Variables.Contains(variableDeclarator);

    public bool AreEquivalent(SyntaxToken token1, SyntaxToken token2)
        => SyntaxFactory.AreEquivalent(token1, token2);

    public bool AreEquivalent(SyntaxNode? node1, SyntaxNode? node2)
        => SyntaxFactory.AreEquivalent(node1, node2);

    public static SyntaxNode GetExpressionOfInvocationExpression(SyntaxNode node)
        => ((InvocationExpressionSyntax)node).Expression;

    public bool IsExpressionOfForeach([NotNullWhen(true)] SyntaxNode? node)
        => node?.Parent is ForEachStatementSyntax foreachStatement && foreachStatement.Expression == node;

    public SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node)
        => ((ExpressionStatementSyntax)node).Expression;

    public void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node,
        out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode
    {
        var tupleExpression = (TupleExpressionSyntax)node;
        openParen = tupleExpression.OpenParenToken;
        arguments = (SeparatedSyntaxList<TArgumentSyntax>)(SeparatedSyntaxList<SyntaxNode>)tupleExpression.Arguments;
        closeParen = tupleExpression.CloseParenToken;
    }

    public bool IsPreprocessorDirective(SyntaxTrivia trivia)
        => SyntaxFacts.IsPreprocessorDirective(trivia.Kind());

    public bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken)
        => token.ContainsInterleavedDirective(span, cancellationToken);

    public SyntaxTokenList GetModifiers(SyntaxNode? node)
        => node.GetModifiers();

    [return: NotNullIfNotNull(nameof(node))]
    public SyntaxNode? WithModifiers(SyntaxNode? node, SyntaxTokenList modifiers)
        => node.WithModifiers(modifiers);

    public SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node)
        => ((LocalDeclarationStatementSyntax)node).Declaration.Variables;

    public SyntaxNode? GetInitializerOfVariableDeclarator(SyntaxNode node)
        => ((VariableDeclaratorSyntax)node).Initializer;

    public SyntaxNode? GetInitializerOfPropertyDeclaration(SyntaxNode node)
        => ((PropertyDeclarationSyntax)node).Initializer;

    public SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node)
        => ((VariableDeclarationSyntax)((VariableDeclaratorSyntax)node).Parent!).Type;

    public SyntaxNode GetValueOfEqualsValueClause(SyntaxNode node)
        => ((EqualsValueClauseSyntax)node).Value;

    public bool IsEqualsValueOfPropertyDeclaration(SyntaxNode? node)
        => node?.Parent is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Initializer == node;

    public bool IsConversionExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is CastExpressionSyntax;

    public bool IsCastExpression([NotNullWhen(true)] SyntaxNode? node)
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

    public SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode? node)
        => node.GetAttributeLists();

    public bool IsParameterNameXmlElementSyntax([NotNullWhen(true)] SyntaxNode? node)
        => node is XmlElementSyntax xmlElement &&
        xmlElement.StartTag.Name.LocalName.ValueText == DocumentationCommentXmlNames.ParameterElementName;

    public SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia)
    {
        if (trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationCommentTrivia)
        {
            return documentationCommentTrivia.Content;
        }

        throw ExceptionUtilities.UnexpectedValue(trivia.Kind());
    }

    public void GetPartsOfAnyIsTypeExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode type)
    {
        var isPatternExpression = (BinaryExpressionSyntax)node;
        expression = isPatternExpression.Left;
        type = isPatternExpression.Right;
    }

    public void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right)
    {
        var isPatternExpression = (IsPatternExpressionSyntax)node;
        left = isPatternExpression.Expression;
        isToken = isPatternExpression.IsKeyword;
        right = isPatternExpression.Pattern;
    }

    public bool IsAnyPattern([NotNullWhen(true)] SyntaxNode? node)
        => node is PatternSyntax;

    public SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node)
        => ((ConstantPatternSyntax)node).Expression;

    public void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation)
    {
        var declarationPattern = (DeclarationPatternSyntax)node;
        type = declarationPattern.Type;
        designation = declarationPattern.Designation;
    }

    public void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode? type, out SyntaxNode? positionalPart, out SyntaxNode? propertyPart, out SyntaxNode? designation)
    {
        var recursivePattern = (RecursivePatternSyntax)node;
        type = recursivePattern.Type;
        positionalPart = recursivePattern.PositionalPatternClause;
        propertyPart = recursivePattern.PropertyPatternClause;
        designation = recursivePattern.Designation;
    }

    public bool SupportsNotPattern(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp9;

    // C# only supports the pattern form, not the expression form.
    public bool SupportsIsNotTypeExpression(ParseOptions options)
        => false;

    public bool IsBinaryPattern([NotNullWhen(true)] SyntaxNode? node)
        => node is BinaryPatternSyntax;

    public bool IsUnaryPattern([NotNullWhen(true)] SyntaxNode? node)
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

    public void GetPartsOfRelationalPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode expression)
    {
        var relationalPattern = (RelationalPatternSyntax)node;
        operatorToken = relationalPattern.OperatorToken;
        expression = relationalPattern.Expression;
    }

    public SyntaxNode GetTypeOfTypePattern(SyntaxNode node)
        => ((TypePatternSyntax)node).Type;

    public bool IsVerbatimInterpolatedStringExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is InterpolatedStringExpressionSyntax { StringStartToken: (kind: SyntaxKind.InterpolatedVerbatimStringStartToken) };

    public bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (syntaxTree == null)
        {
            return false;
        }

        return syntaxTree.IsInInactiveRegion(position, cancellationToken);
    }

    #region IsXXX members

    public bool IsAnonymousFunctionExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is AnonymousFunctionExpressionSyntax;

    public bool IsBaseNamespaceDeclaration([NotNullWhen(true)] SyntaxNode? node)
        => node is BaseNamespaceDeclarationSyntax;

    public bool IsBinaryExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is BinaryExpressionSyntax;

    public bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is LiteralExpressionSyntax;

    public bool IsMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node)
        => node is MemberAccessExpressionSyntax;

    public bool IsMethodDeclaration([NotNullWhen(true)] SyntaxNode? node)
        => node is MethodDeclarationSyntax;

    public bool IsSimpleName([NotNullWhen(true)] SyntaxNode? node)
        => node is SimpleNameSyntax;

    public bool IsAnyName([NotNullWhen(true)] SyntaxNode? node)
        => node is NameSyntax;

    public bool IsAnyType([NotNullWhen(true)] SyntaxNode? node)
        => node is TypeSyntax;

    public bool IsNamedMemberInitializer([NotNullWhen(true)] SyntaxNode? node)
        => node is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) { Left: IdentifierNameSyntax };

    public bool IsElementAccessInitializer([NotNullWhen(true)] SyntaxNode? node)
        => node is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) { Left: ImplicitElementAccessSyntax };

    public bool IsObjectMemberInitializer([NotNullWhen(true)] SyntaxNode? node)
        => node is InitializerExpressionSyntax(SyntaxKind.ObjectInitializerExpression);

    public bool IsObjectCollectionInitializer([NotNullWhen(true)] SyntaxNode? node)
        => node is InitializerExpressionSyntax(SyntaxKind.CollectionInitializerExpression);

    #endregion

    #region GetPartsOfXXX members

    public void GetPartsOfArgumentList(SyntaxNode node, out SyntaxToken openParenToken, out SeparatedSyntaxList<SyntaxNode> arguments, out SyntaxToken closeParenToken)
    {
        var argumentListNode = (ArgumentListSyntax)node;
        openParenToken = argumentListNode.OpenParenToken;
        arguments = argumentListNode.Arguments;
        closeParenToken = argumentListNode.CloseParenToken;
    }

    public void GetPartsOfAttribute(SyntaxNode node, out SyntaxNode name, out SyntaxNode? argumentList)
    {
        var attribute = (AttributeSyntax)node;
        name = attribute.Name;
        argumentList = attribute.ArgumentList;
    }

    public void GetPartsOfBaseObjectCreationExpression(SyntaxNode node, out SyntaxNode? argumentList, out SyntaxNode? initializer)
    {
        var objectCreationExpression = (BaseObjectCreationExpressionSyntax)node;
        argumentList = objectCreationExpression.ArgumentList;
        initializer = objectCreationExpression.Initializer;
    }

    public void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
    {
        var binaryExpression = (BinaryExpressionSyntax)node;
        left = binaryExpression.Left;
        operatorToken = binaryExpression.OperatorToken;
        right = binaryExpression.Right;
    }

    public void GetPartsOfCompilationUnit(SyntaxNode node, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> attributeLists, out SyntaxList<SyntaxNode> members)
    {
        var compilationUnit = (CompilationUnitSyntax)node;
        imports = compilationUnit.Usings;
        attributeLists = compilationUnit.AttributeLists;
        members = compilationUnit.Members;
    }

    public void GetPartsOfConditionalAccessExpression(
        SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull)
    {
        var conditionalAccess = (ConditionalAccessExpressionSyntax)node;
        expression = conditionalAccess.Expression;
        operatorToken = conditionalAccess.OperatorToken;
        whenNotNull = conditionalAccess.WhenNotNull;
    }

    public void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse)
    {
        var conditionalExpression = (ConditionalExpressionSyntax)node;
        condition = conditionalExpression.Condition;
        whenTrue = conditionalExpression.WhenTrue;
        whenFalse = conditionalExpression.WhenFalse;
    }

    public SyntaxNode GetExpressionOfForeachStatement(SyntaxNode statement)
    {
        var commonForeach = (CommonForEachStatementSyntax)statement;
        return commonForeach.Expression;
    }

    public void GetPartsOfGenericName(SyntaxNode node, out SyntaxToken identifier, out SeparatedSyntaxList<SyntaxNode> typeArguments)
    {
        var genericName = (GenericNameSyntax)node;
        identifier = genericName.Identifier;
        typeArguments = genericName.TypeArgumentList.Arguments;
    }

    public void GetPartsOfInterpolationExpression(SyntaxNode node,
        out SyntaxToken stringStartToken, out SyntaxList<SyntaxNode> contents, out SyntaxToken stringEndToken)
    {
        var interpolatedStringExpression = (InterpolatedStringExpressionSyntax)node;
        stringStartToken = interpolatedStringExpression.StringStartToken;
        contents = interpolatedStringExpression.Contents;
        stringEndToken = interpolatedStringExpression.StringEndToken;
    }

    public void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode? argumentList)
    {
        var invocation = (InvocationExpressionSyntax)node;
        expression = invocation.Expression;
        argumentList = invocation.ArgumentList;
    }

    public void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name)
    {
        var memberAccess = (MemberAccessExpressionSyntax)node;
        expression = memberAccess.Expression;
        operatorToken = memberAccess.OperatorToken;
        name = memberAccess.Name;
    }

    public void GetPartsOfBaseNamespaceDeclaration(SyntaxNode node, out SyntaxNode name, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> members)
    {
        var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)node;
        name = namespaceDeclaration.Name;
        imports = namespaceDeclaration.Usings;
        members = namespaceDeclaration.Members;
    }

    public void GetPartsOfNamedMemberInitializer(SyntaxNode node, out SyntaxNode identifier, out SyntaxNode expression)
    {
        var assignment = (AssignmentExpressionSyntax)node;
        identifier = assignment.Left;
        expression = assignment.Right;
    }

    public void GetPartsOfObjectCreationExpression(SyntaxNode node, out SyntaxToken keyword, out SyntaxNode type, out SyntaxNode? argumentList, out SyntaxNode? initializer)
    {
        var objectCreationExpression = (ObjectCreationExpressionSyntax)node;
        keyword = objectCreationExpression.NewKeyword;
        type = objectCreationExpression.Type;
        argumentList = objectCreationExpression.ArgumentList;
        initializer = objectCreationExpression.Initializer;
    }

    public void GetPartsOfImplicitObjectCreationExpression(SyntaxNode node, out SyntaxToken keyword, out SyntaxNode argumentList, out SyntaxNode? initializer)
    {
        var implicitObjectCreationExpression = (ImplicitObjectCreationExpressionSyntax)node;
        keyword = implicitObjectCreationExpression.NewKeyword;
        argumentList = implicitObjectCreationExpression.ArgumentList;
        initializer = implicitObjectCreationExpression.Initializer;
    }

    public void GetPartsOfParameter(SyntaxNode node, out SyntaxToken identifier, out SyntaxNode? @default)
    {
        var parameter = (ParameterSyntax)node;
        identifier = parameter.Identifier;
        @default = parameter.Default;
    }

    public void GetPartsOfParenthesizedExpression(
        SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen)
    {
        var parenthesizedExpression = (ParenthesizedExpressionSyntax)node;
        openParen = parenthesizedExpression.OpenParenToken;
        expression = parenthesizedExpression.Expression;
        closeParen = parenthesizedExpression.CloseParenToken;
    }

    public void GetPartsOfPrefixUnaryExpression(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode operand)
    {
        var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)node;
        operatorToken = prefixUnaryExpression.OperatorToken;
        operand = prefixUnaryExpression.Operand;
    }

    public void GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
    {
        var qualifiedName = (QualifiedNameSyntax)node;
        left = qualifiedName.Left;
        operatorToken = qualifiedName.DotToken;
        right = qualifiedName.Right;
    }

    #endregion

    #region GetXXXOfYYY members

    public SyntaxNode GetArgumentListOfImplicitElementAccess(SyntaxNode node)
        => ((ImplicitElementAccessSyntax)node).ArgumentList;

    public SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node)
        => ((AwaitExpressionSyntax)node).Expression;

    public SyntaxNode GetExpressionOfThrowExpression(SyntaxNode node)
        => ((ThrowExpressionSyntax)node).Expression;

    public SyntaxNode? GetExpressionOfThrowStatement(SyntaxNode node)
        => ((ThrowStatementSyntax)node).Expression;

    public SeparatedSyntaxList<SyntaxNode> GetInitializersOfObjectMemberInitializer(SyntaxNode node)
        => node is InitializerExpressionSyntax(SyntaxKind.ObjectInitializerExpression) initExpr ? initExpr.Expressions : default;

    public SeparatedSyntaxList<SyntaxNode> GetExpressionsOfObjectCollectionInitializer(SyntaxNode node)
        => node is InitializerExpressionSyntax(SyntaxKind.CollectionInitializerExpression) initExpr ? initExpr.Expressions : default;

    public SyntaxToken GetTokenOfLiteralExpression(SyntaxNode node)
        => ((LiteralExpressionSyntax)node).Token;

    #endregion
}
