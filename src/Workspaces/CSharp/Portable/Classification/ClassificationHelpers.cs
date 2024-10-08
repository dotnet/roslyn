// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

internal static class ClassificationHelpers
{
    private const string FromKeyword = "from";
    private const string VarKeyword = "var";
    private const string UnmanagedKeyword = "unmanaged";
    private const string NotNullKeyword = "notnull";
    private const string DynamicKeyword = "dynamic";
    private const string AwaitKeyword = "await";

    /// <summary>
    /// Determine the classification type for a given token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The correct syntactic classification for the token.</returns>
    public static string? GetClassification(SyntaxToken token)
    {
        if (IsControlKeyword(token))
        {
            return ClassificationTypeNames.ControlKeyword;
        }
        else if (SyntaxFacts.IsKeywordKind(token.Kind()) || token.IsKind(SyntaxKind.DiscardDesignation))
        {
            // When classifying `_`, IsKeywordKind handles UnderscoreToken, but need to additional check for DiscardDesignation
            return ClassificationTypeNames.Keyword;
        }
        else if (SyntaxFacts.IsPunctuation(token.Kind()))
        {
            return GetClassificationForPunctuation(token);
        }
        else if (token.Kind() == SyntaxKind.IdentifierToken)
        {
            return GetSyntacticClassificationForIdentifier(token);
        }
        else if (IsStringToken(token))
        {
            return IsVerbatimStringToken(token)
                ? ClassificationTypeNames.VerbatimStringLiteral
                : ClassificationTypeNames.StringLiteral;
        }
        else if (token.Kind() == SyntaxKind.NumericLiteralToken)
        {
            return ClassificationTypeNames.NumericLiteral;
        }

        return null;
    }

    private static bool IsControlKeyword(SyntaxToken token)
    {
        if (token.Parent is null || !IsControlKeywordKind(token.Kind()))
        {
            return false;
        }

        return IsControlStatementKind(token.Parent.Kind());
    }

    private static bool IsControlKeywordKind(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.IfKeyword:
            case SyntaxKind.ElseKeyword:
            case SyntaxKind.WhileKeyword:
            case SyntaxKind.ForKeyword:
            case SyntaxKind.ForEachKeyword:
            case SyntaxKind.DoKeyword:
            case SyntaxKind.SwitchKeyword:
            case SyntaxKind.CaseKeyword:
            case SyntaxKind.TryKeyword:
            case SyntaxKind.CatchKeyword:
            case SyntaxKind.FinallyKeyword:
            case SyntaxKind.GotoKeyword:
            case SyntaxKind.BreakKeyword:
            case SyntaxKind.ContinueKeyword:
            case SyntaxKind.ReturnKeyword:
            case SyntaxKind.ThrowKeyword:
            case SyntaxKind.YieldKeyword:
            case SyntaxKind.DefaultKeyword: // Include DefaultKeyword as it can be part of a DefaultSwitchLabel
            case SyntaxKind.InKeyword: // Include InKeyword as it can be part of an ForEachStatement
            case SyntaxKind.WhenKeyword: // Include WhenKeyword as it can be part of a CatchFilterClause or a pattern WhenClause
                return true;
            default:
                return false;
        }
    }

    private static bool IsControlStatementKind(SyntaxKind kind)
    {
        switch (kind)
        {
            // Jump Statements
            case SyntaxKind.GotoStatement:
            case SyntaxKind.GotoCaseStatement:
            case SyntaxKind.GotoDefaultStatement:
            case SyntaxKind.BreakStatement:
            case SyntaxKind.ContinueStatement:
            case SyntaxKind.ReturnStatement:
            case SyntaxKind.YieldReturnStatement:
            case SyntaxKind.YieldBreakStatement:
            case SyntaxKind.ThrowStatement:
            case SyntaxKind.WhileStatement:
            case SyntaxKind.DoStatement:
            case SyntaxKind.ForStatement:
            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForEachVariableStatement:
            // Checked Statements
            case SyntaxKind.IfStatement:
            case SyntaxKind.ElseClause:
            case SyntaxKind.SwitchStatement:
            case SyntaxKind.SwitchSection:
            case SyntaxKind.CaseSwitchLabel:
            case SyntaxKind.CasePatternSwitchLabel:
            case SyntaxKind.DefaultSwitchLabel:
            case SyntaxKind.TryStatement:
            case SyntaxKind.CatchClause:
            case SyntaxKind.CatchFilterClause:
            case SyntaxKind.FinallyClause:
            case SyntaxKind.SwitchExpression:
            case SyntaxKind.ThrowExpression:
            case SyntaxKind.WhenClause:
                return true;
            default:
                return false;
        }
    }

    private static bool IsStringToken(SyntaxToken token)
    {
        return token.Kind()
            is SyntaxKind.StringLiteralToken
            or SyntaxKind.Utf8StringLiteralToken
            or SyntaxKind.CharacterLiteralToken
            or SyntaxKind.InterpolatedStringStartToken
            or SyntaxKind.InterpolatedVerbatimStringStartToken
            or SyntaxKind.InterpolatedStringTextToken
            or SyntaxKind.InterpolatedStringEndToken
            or SyntaxKind.InterpolatedRawStringEndToken
            or SyntaxKind.InterpolatedSingleLineRawStringStartToken
            or SyntaxKind.InterpolatedMultiLineRawStringStartToken
            or SyntaxKind.SingleLineRawStringLiteralToken
            or SyntaxKind.Utf8SingleLineRawStringLiteralToken
            or SyntaxKind.MultiLineRawStringLiteralToken
            or SyntaxKind.Utf8MultiLineRawStringLiteralToken;
    }

    private static bool IsVerbatimStringToken(SyntaxToken token)
    {
        if (token.IsVerbatimStringLiteral())
        {
            return true;
        }

        switch (token.Kind())
        {
            case SyntaxKind.InterpolatedVerbatimStringStartToken:
                return true;
            case SyntaxKind.InterpolatedStringStartToken:
                return false;

            case SyntaxKind.InterpolatedStringEndToken:
                {
                    return token.Parent is InterpolatedStringExpressionSyntax interpolatedString
                        && interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
                }

            case SyntaxKind.InterpolatedStringTextToken:
                {
                    if (token.Parent is not InterpolatedStringTextSyntax interpolatedStringText)
                    {
                        return false;
                    }

                    return interpolatedStringText.Parent is InterpolatedStringExpressionSyntax interpolatedString
                        && interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
                }
        }

        return false;
    }

    public static string? GetSyntacticClassificationForIdentifier(SyntaxToken token)
    {
        if (token.Parent is BaseTypeDeclarationSyntax typeDeclaration && typeDeclaration.Identifier == token)
        {
            return GetClassificationForTypeDeclarationIdentifier(token);
        }
        else if (token.Parent is DelegateDeclarationSyntax delegateDecl && delegateDecl.Identifier == token)
        {
            return ClassificationTypeNames.DelegateName;
        }
        else if (token.Parent is TypeParameterSyntax typeParameter && typeParameter.Identifier == token)
        {
            return ClassificationTypeNames.TypeParameterName;
        }
        else if (token.Parent is MethodDeclarationSyntax methodDeclaration && methodDeclaration.Identifier == token)
        {
            return IsExtensionMethod(methodDeclaration) ? ClassificationTypeNames.ExtensionMethodName : ClassificationTypeNames.MethodName;
        }
        else if (token.Parent is ConstructorDeclarationSyntax constructorDeclaration && constructorDeclaration.Identifier == token)
        {
            return GetClassificationTypeForConstructorOrDestructorParent(constructorDeclaration.Parent!);
        }
        else if (token.Parent is DestructorDeclarationSyntax destructorDeclaration && destructorDeclaration.Identifier == token)
        {
            return GetClassificationTypeForConstructorOrDestructorParent(destructorDeclaration.Parent!);
        }
        else if (token.Parent is LocalFunctionStatementSyntax localFunctionStatement && localFunctionStatement.Identifier == token)
        {
            return ClassificationTypeNames.MethodName;
        }
        else if (token.Parent is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Identifier == token)
        {
            return ClassificationTypeNames.PropertyName;
        }
        else if (token.Parent is EnumMemberDeclarationSyntax enumMemberDeclaration && enumMemberDeclaration.Identifier == token)
        {
            return ClassificationTypeNames.EnumMemberName;
        }
        else if (token.Parent is CatchDeclarationSyntax catchDeclaration && catchDeclaration.Identifier == token)
        {
            return ClassificationTypeNames.LocalName;
        }
        else if (token.Parent is VariableDeclaratorSyntax variableDeclarator && variableDeclarator.Identifier == token)
        {
            var varDecl = variableDeclarator.Parent as VariableDeclarationSyntax;
            return varDecl?.Parent switch
            {
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword) ? ClassificationTypeNames.ConstantName : ClassificationTypeNames.FieldName,
                LocalDeclarationStatementSyntax localDeclarationStatement => localDeclarationStatement.IsConst ? ClassificationTypeNames.ConstantName : ClassificationTypeNames.LocalName,
                EventFieldDeclarationSyntax _ => ClassificationTypeNames.EventName,
                _ => ClassificationTypeNames.LocalName,
            };
        }
        else if (token.Parent is SingleVariableDesignationSyntax singleVariableDesignation && singleVariableDesignation.Identifier == token)
        {
            return ClassificationTypeNames.LocalName;
        }
        else if (token.Parent is ParameterSyntax parameterSyntax && parameterSyntax.Identifier == token)
        {
            return ClassificationTypeNames.ParameterName;
        }
        else if (token.Parent is ForEachStatementSyntax forEachStatementSyntax && forEachStatementSyntax.Identifier == token)
        {
            return ClassificationTypeNames.LocalName;
        }
        else if (token.Parent is EventDeclarationSyntax eventDeclarationSyntax && eventDeclarationSyntax.Identifier == token)
        {
            return ClassificationTypeNames.EventName;
        }
        else if (IsActualContextualKeyword(token))
        {
            return ClassificationTypeNames.Keyword;
        }
        else if (token.Parent is IdentifierNameSyntax identifierNameSyntax && IsNamespaceName(identifierNameSyntax))
        {
            return ClassificationTypeNames.NamespaceName;
        }
        else if (token.Parent is ExternAliasDirectiveSyntax externAliasDirectiveSyntax && externAliasDirectiveSyntax.Identifier == token)
        {
            return ClassificationTypeNames.NamespaceName;
        }
        else if (token.Parent is LabeledStatementSyntax labledStatementSyntax && labledStatementSyntax.Identifier == token)
        {
            return ClassificationTypeNames.LabelName;
        }
        else
        {
            return ClassificationTypeNames.Identifier;
        }
    }

    private static string? GetClassificationTypeForConstructorOrDestructorParent(SyntaxNode parentNode)
        => parentNode.Kind() switch
        {
            SyntaxKind.ClassDeclaration => ClassificationTypeNames.ClassName,
            SyntaxKind.InterfaceDeclaration => ClassificationTypeNames.InterfaceName,
            SyntaxKind.RecordDeclaration => ClassificationTypeNames.RecordClassName,
            SyntaxKind.RecordStructDeclaration => ClassificationTypeNames.RecordStructName,
            SyntaxKind.StructDeclaration => ClassificationTypeNames.StructName,
            _ => null
        };

    private static bool IsNamespaceName(IdentifierNameSyntax identifierSyntax)
    {
        var parent = identifierSyntax.Parent;

        while (parent is QualifiedNameSyntax)
            parent = parent.Parent;

        return parent is BaseNamespaceDeclarationSyntax;
    }

    public static bool IsStaticallyDeclared(SyntaxToken token)
    {
        var parentNode = token.Parent;

        if (parentNode.IsKind(SyntaxKind.EnumMemberDeclaration))
        {
            // EnumMembers are not classified as static since there is no
            // instance equivalent of the concept and they have their own
            // classification type.
            return false;
        }
        else if (parentNode.IsKind(SyntaxKind.VariableDeclarator))
        {
            // The parent of a VariableDeclarator is a VariableDeclarationSyntax node.
            // It's parent will be the declaration syntax node.
            parentNode = parentNode!.Parent!.Parent;

            // Check if this is a field constant declaration 
            if (parentNode.GetModifiers().Any(SyntaxKind.ConstKeyword))
            {
                return true;
            }
        }

        return parentNode.GetModifiers().Any(SyntaxKind.StaticKeyword);
    }

    private static bool IsExtensionMethod(MethodDeclarationSyntax methodDeclaration)
        => methodDeclaration.ParameterList.Parameters.FirstOrDefault()?.Modifiers.Any(SyntaxKind.ThisKeyword) == true;

    private static string? GetClassificationForTypeDeclarationIdentifier(SyntaxToken identifier)
        => identifier.Parent!.Kind() switch
        {
            SyntaxKind.ClassDeclaration => ClassificationTypeNames.ClassName,
            SyntaxKind.EnumDeclaration => ClassificationTypeNames.EnumName,
            SyntaxKind.StructDeclaration => ClassificationTypeNames.StructName,
            SyntaxKind.InterfaceDeclaration => ClassificationTypeNames.InterfaceName,
            SyntaxKind.RecordDeclaration => ClassificationTypeNames.RecordClassName,
            SyntaxKind.RecordStructDeclaration => ClassificationTypeNames.RecordStructName,
            _ => null,
        };

    private static string GetClassificationForPunctuation(SyntaxToken token)
    {
        if (token.Kind().IsOperator())
        {
            // special cases...
            switch (token.Kind())
            {
                case SyntaxKind.LessThanToken:
                case SyntaxKind.GreaterThanToken:
                    // the < and > tokens of a type parameter list or function pointer parameter
                    // list should be classified as punctuation; otherwise, they're operators.
                    if (token.Parent != null)
                    {
                        if (token.Parent.Kind() is SyntaxKind.TypeParameterList or
                            SyntaxKind.TypeArgumentList or
                            SyntaxKind.FunctionPointerParameterList)
                        {
                            return ClassificationTypeNames.Punctuation;
                        }
                    }

                    break;
                case SyntaxKind.ColonToken:
                    // the : for inheritance/implements or labels should be classified as
                    // punctuation; otherwise, it's from a conditional operator.
                    if (token.Parent != null)
                    {
                        if (token.Parent.Kind() != SyntaxKind.ConditionalExpression)
                        {
                            return ClassificationTypeNames.Punctuation;
                        }
                    }

                    break;
            }

            return ClassificationTypeNames.Operator;
        }
        else
        {
            return ClassificationTypeNames.Punctuation;
        }
    }

    private static bool IsOperator(this SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.TildeToken:
            case SyntaxKind.ExclamationToken:
            case SyntaxKind.PercentToken:
            case SyntaxKind.CaretToken:
            case SyntaxKind.AmpersandToken:
            case SyntaxKind.AsteriskToken:
            case SyntaxKind.MinusToken:
            case SyntaxKind.PlusToken:
            case SyntaxKind.EqualsToken:
            case SyntaxKind.BarToken:
            case SyntaxKind.ColonToken:
            case SyntaxKind.LessThanToken:
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.DotToken:
            case SyntaxKind.QuestionToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.BarBarToken:
            case SyntaxKind.AmpersandAmpersandToken:
            case SyntaxKind.MinusMinusToken:
            case SyntaxKind.PlusPlusToken:
            case SyntaxKind.ColonColonToken:
            case SyntaxKind.QuestionQuestionToken:
            case SyntaxKind.MinusGreaterThanToken:
            case SyntaxKind.ExclamationEqualsToken:
            case SyntaxKind.EqualsEqualsToken:
            case SyntaxKind.EqualsGreaterThanToken:
            case SyntaxKind.LessThanEqualsToken:
            case SyntaxKind.LessThanLessThanToken:
            case SyntaxKind.LessThanLessThanEqualsToken:
            case SyntaxKind.GreaterThanEqualsToken:
            case SyntaxKind.GreaterThanGreaterThanToken:
            case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
            case SyntaxKind.GreaterThanGreaterThanEqualsToken:
            case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
            case SyntaxKind.SlashEqualsToken:
            case SyntaxKind.AsteriskEqualsToken:
            case SyntaxKind.BarEqualsToken:
            case SyntaxKind.AmpersandEqualsToken:
            case SyntaxKind.PlusEqualsToken:
            case SyntaxKind.MinusEqualsToken:
            case SyntaxKind.CaretEqualsToken:
            case SyntaxKind.PercentEqualsToken:
            case SyntaxKind.QuestionQuestionEqualsToken:
                return true;

            default:
                return false;
        }
    }

    private static bool IsActualContextualKeyword(SyntaxToken token)
    {
        if (token.Parent is LabeledStatementSyntax statement &&
            statement.Identifier == token)
        {
            return false;
        }

        // Ensure that the text and value text are the same. Otherwise, the identifier might
        // be escaped. I.e. "var", but not "@var"
        if (token.ToString() != token.ValueText)
        {
            return false;
        }

        // Standard cases.  We can just check the parent and see if we're
        // in the right position to be considered a contextual keyword
        if (token.Parent != null)
        {
            switch (token.ValueText)
            {
                case AwaitKeyword:
                    return token.GetNextToken(includeZeroWidth: true).IsMissing;

                case FromKeyword:
                    var fromClause = token.Parent.FirstAncestorOrSelf<FromClauseSyntax>();
                    return fromClause != null && fromClause.FromKeyword == token;

                case VarKeyword:
                    // var
                    if (token.Parent is IdentifierNameSyntax && token.Parent?.Parent is ExpressionStatementSyntax)
                    {
                        return true;
                    }

                    // we allow var any time it looks like a variable declaration, and is not in a
                    // field or event field.
                    return
                        token.Parent is IdentifierNameSyntax &&
                        token.Parent.Parent is VariableDeclarationSyntax &&
                        !(token.Parent.Parent.Parent is FieldDeclarationSyntax) &&
                        !(token.Parent.Parent.Parent is EventFieldDeclarationSyntax);

                case UnmanagedKeyword:
                case NotNullKeyword:
                    return token.Parent is IdentifierNameSyntax
                        && token.Parent.Parent is TypeConstraintSyntax
                        && token.Parent.Parent.Parent is TypeParameterConstraintClauseSyntax;
            }
        }

        return false;
    }

    internal static void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        var text2 = text.ToString(textSpan);
        var tokens = SyntaxFactory.ParseTokens(text2, initialTokenPosition: textSpan.Start);

        Worker.CollectClassifiedSpans(tokens, textSpan, result, cancellationToken);
    }

    internal static ClassifiedSpan AdjustStaleClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
    {
        // If we marked this as an identifier and it should now be a keyword
        // (or vice versa), then fix this up and return it. 
        var classificationType = classifiedSpan.ClassificationType;

        // Check if the token's type has changed.  Note: we don't check for "wasPPKeyword &&
        // !isPPKeyword" here.  That's because for fault tolerance any identifier will end up
        // being parsed as a PP keyword eventually, and if we have the check here, the text
        // flickers between blue and black while typing.  See
        // http://vstfdevdiv:8080/web/wi.aspx?id=3521 for details.
        var wasKeyword = classificationType == ClassificationTypeNames.Keyword;
        var wasIdentifier = classificationType == ClassificationTypeNames.Identifier;

        // We only do this for identifiers/keywords.
        if (wasKeyword || wasIdentifier)
        {
            // Get the current text under the tag.
            var span = classifiedSpan.TextSpan;
            var text = rawText.ToString(span);

            // Now, try to find the token that corresponds to that text.  If
            // we get 0 or 2+ tokens, then we can't do anything with this.  
            // Also, if that text includes trivia, then we can't do anything.
            var token = SyntaxFactory.ParseToken(text);
            if (token.Span.Length == span.Length)
            {
                // var, dynamic, and unmanaged are not contextual keywords.  They are always identifiers
                // (that we classify as keywords).  Because we are just parsing a token we don't
                // know if we're in the right context for them to be identifiers or keywords.
                // So, we base on decision on what they were before.  i.e. if we had a keyword
                // before, then assume it stays a keyword if we see 'var', 'dynamic', or 'unmanaged'.
                var tokenString = token.ToString();
                var isKeyword = SyntaxFacts.IsKeywordKind(token.Kind())
                    || (wasKeyword && SyntaxFacts.GetContextualKeywordKind(text) != SyntaxKind.None)
                    || (wasKeyword && (tokenString == VarKeyword || tokenString == DynamicKeyword || tokenString == UnmanagedKeyword || tokenString == NotNullKeyword));

                var isIdentifier = token.Kind() == SyntaxKind.IdentifierToken;

                // We only do this for identifiers/keywords.
                if (isKeyword || isIdentifier)
                {
                    if ((wasKeyword && !isKeyword) ||
                        (wasIdentifier && !isIdentifier))
                    {
                        // It changed!  Return the new type of tagspan.
                        return new ClassifiedSpan(
                            isKeyword ? ClassificationTypeNames.Keyword : ClassificationTypeNames.Identifier, span);
                    }
                }
            }
        }

        // didn't need to do anything to this one.
        return classifiedSpan;
    }
}
