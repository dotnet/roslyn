// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

internal static class CompletionUtilities
{
    internal static TextSpan GetCompletionItemSpan(SourceText text, int position)
        => CommonCompletionUtilities.GetWordSpan(text, position, IsCompletionItemStartCharacter, IsWordCharacter);

    public static bool IsWordStartCharacter(char ch)
        => SyntaxFacts.IsIdentifierStartCharacter(ch);

    public static bool IsWordCharacter(char ch)
        => SyntaxFacts.IsIdentifierStartCharacter(ch) || SyntaxFacts.IsIdentifierPartCharacter(ch);

    public static bool IsCompletionItemStartCharacter(char ch)
        => ch == '@' || IsWordCharacter(ch);

    public static bool TreatAsDot(SyntaxToken token, int characterPosition)
    {
        if (token.Kind() == SyntaxKind.DotToken)
            return true;

        // if we're right after the first dot in .. then that's considered completion on dot.
        if (token.Kind() == SyntaxKind.DotDotToken && token.SpanStart == characterPosition)
            return true;

        return false;
    }

    public static SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        var tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeSkipped: true);
        var dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position);

        // Has to be a . or a .. token
        if (!TreatAsDot(dotToken, position - 1))
            return null;

        // don't want to trigger after a number. All other cases after dot are ok.
        if (dotToken.GetPreviousToken().Kind() == SyntaxKind.NumericLiteralToken)
            return null;

        return dotToken;
    }

    internal static bool IsTriggerCharacter(SourceText text, int characterPosition, in CompletionOptions options)
    {
        var ch = text[characterPosition];

        // Trigger off of a normal `.`, but not off of `..`
        if (ch == '.' && !(characterPosition >= 1 && text[characterPosition - 1] == '.'))
        {
            return true;
        }

        // Trigger for directive
        if (ch == '#')
        {
            return true;
        }

        // Trigger on pointer member access
        if (ch == '>' && characterPosition >= 1 && text[characterPosition - 1] == '-')
        {
            return true;
        }

        // Trigger on alias name
        if (ch == ':' && characterPosition >= 1 && text[characterPosition - 1] == ':')
        {
            return true;
        }

        if (options.TriggerOnTypingLetters && IsStartingNewWord(text, characterPosition))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tells if we are in positions like this: <c>#nullable $$</c> or <c>#pragma warning $$</c>
    /// </summary>
    internal static bool IsCompilerDirectiveTriggerCharacter(SourceText text, int characterPosition)
    {
        while (text[characterPosition] == ' ' ||
               char.IsLetter(text[characterPosition]))
        {
            characterPosition--;

            if (characterPosition < 0)
                return false;
        }

        return text[characterPosition] == '#';
    }

    internal static ImmutableHashSet<char> CommonTriggerCharacters { get; } = ['.', '#', '>', ':'];

    internal static ImmutableHashSet<char> CommonTriggerCharactersWithArgumentList { get; } = ['.', '#', '>', ':', '(', '[', ' '];

    internal static bool IsTriggerCharacterOrArgumentListCharacter(SourceText text, int characterPosition, in CompletionOptions options)
        => IsTriggerCharacter(text, characterPosition, options) || IsArgumentListCharacter(text, characterPosition);

    private static bool IsArgumentListCharacter(SourceText text, int characterPosition)
        => IsArgumentListCharacter(text[characterPosition]);

    internal static bool IsArgumentListCharacter(char ch)
        => ch is '(' or '[' or ' ';

    internal static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition, in CompletionOptions options)
    {
        // Bring up on space or at the start of a word.
        var ch = text[characterPosition];
        return SpaceTypedNotBeforeWord(ch, text, characterPosition) ||
            (IsStartingNewWord(text, characterPosition) && options.TriggerOnTypingLetters);
    }

    internal static ImmutableHashSet<char> SpaceTriggerCharacter => [' '];

    private static bool SpaceTypedNotBeforeWord(char ch, SourceText text, int characterPosition)
        => ch == ' ' && (characterPosition == text.Length - 1 || !IsWordStartCharacter(text[characterPosition + 1]));

    public static bool IsStartingNewWord(SourceText text, int characterPosition)
    {
        return CommonCompletionUtilities.IsStartingNewWord(
            text, characterPosition, IsWordStartCharacter, IsWordCharacter);
    }

    public static (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(
        ISymbol symbol, SyntaxContext context)
    {
        var insertionText = GetInsertionText(symbol, context);
        var suffix = symbol.GetArity() == 0 ? "" : "<>";

        return (insertionText, suffix, insertionText);
    }

    public static string GetInsertionText(ISymbol symbol, SyntaxContext context)
    {
        if (CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context, out var name))
        {
            // Cannot escape Attribute name with the suffix removed. Only use the name with
            // the suffix removed if it does not need to be escaped.
            if (name.Equals(name.EscapeIdentifier()))
            {
                return name;
            }
        }

        if (symbol.Kind == SymbolKind.Label &&
            symbol.DeclaringSyntaxReferences[0].GetSyntax().Kind() == SyntaxKind.DefaultSwitchLabel)
        {
            return symbol.Name;
        }

        return symbol.Name.EscapeIdentifier(isQueryContext: context.IsInQuery);
    }

    public static SyntaxNode GetTargetCaretPositionForMethod(BaseMethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.Body is null)
        {
            return methodDeclaration;
        }
        else
        {
            // move to the end of the last statement in the method
            var lastStatement = methodDeclaration.Body.Statements.Last();
            return lastStatement;
        }
    }

    public static TextSpan GetTargetSelectionSpanForMethod(BaseMethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.ExpressionBody is not null)
        {
            // select the expression span
            return methodDeclaration.ExpressionBody.Expression.Span;
        }
        else if (methodDeclaration.Body is not null)
        {
            // select the last statement in the method
            return methodDeclaration.Body.Statements.Last().Span;
        }
        else
        {
            return methodDeclaration.Span;
        }
    }

    public static TextSpan GetTargetSelectionSpanForInsertedMember(SyntaxNode caretTarget)
    {
        switch (caretTarget)
        {
            case EventFieldDeclarationSyntax:
                // Inserted Event declarations are a single line, so move caret to the end of the line.
                return new TextSpan(caretTarget.Span.End, 0);

            case BaseMethodDeclarationSyntax methodDeclaration:
                return GetTargetSelectionSpanForMethod(methodDeclaration);

            case BasePropertyDeclarationSyntax propertyDeclaration:
                {
                    if (propertyDeclaration.AccessorList is { Accessors: [var firstAccessor, ..] })
                    {
                        // select the last statement of the first accessor
                        if (firstAccessor.Body is { Statements: [.., var lastStatement] })
                            return lastStatement.Span;

                        if (firstAccessor.ExpressionBody is { Expression: { } expression })
                            return expression.Span;
                    }
                    else if (propertyDeclaration is PropertyDeclarationSyntax { ExpressionBody.Expression: { } expression })
                    {
                        // expression-bodied property: select the expression
                        return expression.Span;
                    }

                    // property: no accessors; move caret to the end of the declaration
                    return new TextSpan(propertyDeclaration.Span.End, 0);
                }

            default:
                throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Determines whether the specified position in the syntax tree is a valid context for speculatively typing
    /// a type parameter (which might be undeclared yet). This handles cases where the user may be in the middle of typing a generic type, tuple
    /// and ref type as well.
    /// 
    /// For example, when you typed `public TBuilder$$`, you might want to type `public TBuilder M&lt;TBuilder&gt;(){}`,
    /// so TBuilder is a valid speculative type parameter context.
    /// </summary>
    public static bool IsSpeculativeTypeParameterContext(SyntaxTree syntaxTree, int position, SemanticModel? semanticModel, bool includeStatementContexts, CancellationToken cancellationToken)
    {
        var spanStart = position;

        // We could be in the middle of a ref/generic/tuple type, instead of a simple T case.
        // If we managed to walk out and get a different SpanStart, we treat it as a simple $$T case.
        while (true)
        {
            var oldSpanStart = spanStart;

            spanStart = WalkOutOfGenericType(syntaxTree, spanStart, semanticModel, cancellationToken);
            spanStart = WalkOutOfTupleType(syntaxTree, spanStart, cancellationToken);
            spanStart = WalkOutOfRefType(syntaxTree, spanStart, cancellationToken);

            if (spanStart == oldSpanStart)
            {
                break;
            }
        }

        var token = syntaxTree.FindTokenOnLeftOfPosition(spanStart, cancellationToken);

        // Always want to allow in member declaration and delegate return type context, for example:
        // class C
        // {
        //     public T$$
        // }
        //
        // delegate T$$
        if (syntaxTree.IsMemberDeclarationContext(spanStart, context: null, SyntaxKindSet.AllMemberModifiers, SyntaxKindSet.NonEnumTypeDeclarations, canBePartial: true, cancellationToken) ||
            syntaxTree.IsGlobalMemberDeclarationContext(spanStart, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            syntaxTree.IsDelegateReturnTypeContext(spanStart, token))
        {
            return true;
        }

        // Because it's less likely the user wants to type a (undeclared) type parameter when they are inside a method body, treating them so
        // might intefere with user intention. For example, while it's fine to provide a speculative `T` item in a statement context,
        // since typing 2 characters would filter it out, but for selection, we don't want to soft-select item `TypeBuilder`after `TB`
        // is typed in the example below (as if user want to add `TBuilder` to method declaration later):
        //
        // class C
        // {
        //     void M()
        //     {
        //         TB$$
        //     }
        if (includeStatementContexts)
        {
            return syntaxTree.IsStatementContext(spanStart, token, cancellationToken) ||
                syntaxTree.IsGlobalStatementContext(spanStart, cancellationToken);
        }

        return false;

        static int WalkOutOfGenericType(SyntaxTree syntaxTree, int position, SemanticModel? semanticModel, CancellationToken cancellationToken)
        {
            var spanStart = position;
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            if (syntaxTree.IsGenericTypeArgumentContext(position, token, cancellationToken, semanticModel))
            {
                if (syntaxTree.IsInPartiallyWrittenGeneric(spanStart, cancellationToken, out var nameToken))
                {
                    spanStart = nameToken.SpanStart;
                }

                // If the user types Goo<T, automatic brace completion will insert the close brace
                // and the generic won't be "partially written".
                if (spanStart == position)
                {
                    spanStart = token.GetAncestor<GenericNameSyntax>()?.SpanStart ?? spanStart;
                }

                var tokenLeftOfGenericName = syntaxTree.FindTokenOnLeftOfPosition(spanStart, cancellationToken);
                if (tokenLeftOfGenericName.IsKind(SyntaxKind.DotToken) && tokenLeftOfGenericName.Parent.IsKind(SyntaxKind.QualifiedName))
                {
                    spanStart = tokenLeftOfGenericName.Parent.SpanStart;
                }
            }

            return spanStart;
        }

        static int WalkOutOfRefType(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var prevToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                      .GetPreviousTokenIfTouchingWord(position);

            if (prevToken.Kind() is SyntaxKind.RefKeyword or SyntaxKind.ReadOnlyKeyword && prevToken.Parent.IsKind(SyntaxKind.RefType))
            {
                return prevToken.SpanStart;
            }

            return position;
        }

        static int WalkOutOfTupleType(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var prevToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                      .GetPreviousTokenIfTouchingWord(position);

            if (prevToken.IsPossibleTupleOpenParenOrComma())
            {
                return prevToken.Parent!.SpanStart;
            }

            return position;
        }
    }
}
