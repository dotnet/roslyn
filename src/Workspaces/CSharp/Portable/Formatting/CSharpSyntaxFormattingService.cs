// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class CSharpSyntaxFormattingService : CSharpSyntaxFormatting, ISyntaxFormattingService
{
    private readonly LanguageServices _services;

    [ExportLanguageServiceFactory(typeof(ISyntaxFormattingService), LanguageNames.CSharp), Shared]
    internal sealed class Factory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new CSharpSyntaxFormattingService(languageServices.LanguageServices);
    }

    private CSharpSyntaxFormattingService(LanguageServices languageServices)
        => _services = languageServices;

    public bool ShouldFormatOnTypedCharacter(
        ParsedDocument documentSyntax,
        char typedChar,
        int caretPosition,
        CancellationToken cancellationToken)
    {
        // first, find the token user just typed.
        var token = documentSyntax.Root.FindToken(Math.Max(0, caretPosition - 1), findInsideTrivia: true);
        if (token.IsMissing ||
            !ValidSingleOrMultiCharactersTokenKind(typedChar, token.Kind()) ||
            token.Kind() is SyntaxKind.EndOfFileToken or SyntaxKind.None ||
            documentSyntax.SyntaxTree.IsInNonUserCode(caretPosition, cancellationToken))
        {
            return false;
        }

        // If the token is a )  we only want to format if it's the close paren
        // of a using statement.  That way if we have nested usings, the inner
        // using will align with the outer one when the user types the close paren.
        if (token.IsKind(SyntaxKind.CloseParenToken) && !token.Parent.IsKind(SyntaxKind.UsingStatement))
        {
            return false;
        }

        // If the token is a :  we only want to format if it's a labeled statement
        // or case.  When the colon is typed we'll want ot immediately have those
        // statements snap to their appropriate indentation level.
        if (token.IsKind(SyntaxKind.ColonToken) && !(token.Parent.IsKind(SyntaxKind.LabeledStatement) || token.Parent is SwitchLabelSyntax))
        {
            return false;
        }

        // Only format an { if it is the first token on a line.  We don't want to 
        // mess with it if it's inside a line.
        if (token.IsKind(SyntaxKind.OpenBraceToken))
        {
            if (!token.IsFirstTokenOnLine(documentSyntax.Text))
            {
                return false;
            }
        }

        return true;
    }

    public ImmutableArray<TextChange> GetFormattingChangesOnTypedCharacter(
        ParsedDocument document,
        int caretPosition,
        IndentationOptions indentationOptions,
        CancellationToken cancellationToken)
    {
        var root = document.Root;
        var token = root.FindToken(Math.Max(0, caretPosition - 1), findInsideTrivia: true);
        var formattingRules = GetFormattingRules(document, caretPosition, token);

        // Do not attempt to format on open/close brace if autoformat on close brace feature is
        // off, instead just smart indent.
        //
        // We want this behavior because it's totally reasonable for a user to want to not have
        // on automatic formatting because they feel it is too aggressive.  However, by default,
        // if you have smart-indentation on and are just hitting enter, you'll common have the
        // caret placed one indent higher than your current construct.  For example, if you have:
        //
        //      if (true)
        //          $ <-- smart indent will have placed the caret here here.
        //
        // This is acceptable given that the user may want to just write a simple statement there.
        // However, if they start writing `{`, then things should snap over to be:
        //
        //      if (true)
        //      {
        //
        // Importantly, this is just an indentation change, no actual 'formatting' is done.  We do
        // the same with close brace.  If you have:
        //
        //      if (...)
        //      {
        //          bad . ly ( for (mmated+code) )  ;
        //          $ <-- smart indent will have placed the care here.
        //
        // If the user hits `}` then we will properly smart indent the `}` to match the `{`.
        // However, we won't touch any of the other code in that block, unlike if we were
        // formatting.
        var onlySmartIndent =
            (token.IsKind(SyntaxKind.CloseBraceToken) && OnlySmartIndentCloseBrace(indentationOptions.AutoFormattingOptions)) ||
            (token.IsKind(SyntaxKind.OpenBraceToken) && OnlySmartIndentOpenBrace(indentationOptions.AutoFormattingOptions));

        if (onlySmartIndent)
        {
            // if we're only doing smart indent, then ignore all edits to this token that occur before
            // the span of the token. They're irrelevant and may screw up other code the user doesn't 
            // want touched.
            var tokenEdits = FormatToken(document, indentationOptions, token, formattingRules, cancellationToken);
            return tokenEdits.Where(t => t.Span.Start >= token.FullSpan.Start).ToImmutableArray();
        }

        // if formatting range fails, do format token one at least
        var changes = FormatRange(document, indentationOptions, token, formattingRules, cancellationToken);
        if (changes.Length > 0)
        {
            return changes;
        }

        return [.. FormatToken(document, indentationOptions, token, formattingRules, cancellationToken)];
    }

    private static bool OnlySmartIndentCloseBrace(in AutoFormattingOptions options)
    {
        // User does not want auto-formatting (either in general, or for close braces in
        // specific).  So we only smart indent close braces when typed.
        return !options.FormatOnCloseBrace || !options.FormatOnTyping;
    }

    private static bool OnlySmartIndentOpenBrace(in AutoFormattingOptions options)
    {
        // User does not want auto-formatting .  So we only smart indent open braces when typed.
        // Note: there is no specific option for controlling formatting on open brace.  So we
        // don't have the symmetry with OnlySmartIndentCloseBrace.
        return !options.FormatOnTyping;
    }

    private static IList<TextChange> FormatToken(
        ParsedDocument document, IndentationOptions options, SyntaxToken token, ImmutableArray<AbstractFormattingRule> formattingRules, CancellationToken cancellationToken)
    {
        var formatter = new CSharpSmartTokenFormatter(options, formattingRules, (CompilationUnitSyntax)document.Root, document.Text);
        return formatter.FormatToken(token, cancellationToken);
    }

    private static ImmutableArray<TextChange> FormatRange(
        ParsedDocument document,
        IndentationOptions options,
        SyntaxToken endToken,
        ImmutableArray<AbstractFormattingRule> formattingRules,
        CancellationToken cancellationToken)
    {
        if (!IsEndToken(endToken))
        {
            return [];
        }

        var tokenRange = FormattingRangeHelper.FindAppropriateRange(endToken);
        if (tokenRange == null || tokenRange.Value.Item1.Equals(tokenRange.Value.Item2))
        {
            return [];
        }

        if (IsInvalidTokenKind(tokenRange.Value.Item1) || IsInvalidTokenKind(tokenRange.Value.Item2))
        {
            return [];
        }

        var formatter = new CSharpSmartTokenFormatter(options, formattingRules, (CompilationUnitSyntax)document.Root, document.Text);

        var changes = formatter.FormatRange(tokenRange.Value.Item1, tokenRange.Value.Item2, cancellationToken);
        return [.. changes];
    }

    private static IEnumerable<AbstractFormattingRule> GetTypingRules(SyntaxToken tokenBeforeCaret)
    {
        // Typing introduces several challenges around formatting.  
        // Historically we've shipped several triggers that cause formatting to happen directly while typing.
        // These include formatting of blocks when '}' is typed, formatting of statements when a ';' is typed, formatting of ```case```s when ':' typed, and many other cases.  
        // However, formatting during typing can potentially cause problems.  This is because the surrounding code may not be complete, 
        // or may otherwise have syntax errors, and thus edits could have unintended consequences.
        // 
        // Because of this, we introduce an extra rule into the set of formatting rules whose purpose is to actually make formatting *more* 
        // conservative and *less* willing willing to make edits to the tree. 
        // The primary effect this rule has is to assume that more code is on a single line (and thus should stay that way) 
        // despite what the tree actually looks like.
        // 
        // It's ok that this is only during formatting that is caused by an edit because that formatting happens 
        // implicitly and thus has to be more careful, whereas an explicit format-document call only happens on-demand 
        // and can be more aggressive about what it's doing.
        // 
        // 
        // For example, say you have the following code.
        // 
        // ```c#
        // class C
        // {
        //   int P { get {    return
        // }
        // ```
        // 
        // Hitting ';' after 'return' should ideally only affect the 'return statement' and change it to:
        // 
        // ```c#
        // class C
        // {
        //   int P { get { return;
        // }
        // ```
        // 
        // During a normal format-document call, this is not what would happen. 
        // Specifically, because the parser will consume the '}' into the accessor, 
        // it will think the accessor spans multiple lines, and thus should not stay on a single line.  This will produce:
        // 
        // ```c#
        // class C
        // {
        //   int P
        //   {
        //     get
        //     {
        //       return;
        //     }
        // ```
        // 
        // Because it's ok for this to format in that fashion if format-document is invoked, 
        // but should not happen during typing, we insert a specialized rule *only* during typing to try to control this.  
        // During normal formatting we add 'keep on single line' suppression rules for blocks we find that are on a single line.  
        // But that won't work since this span is not on a single line:
        // 
        // ```c#
        // class C
        // {
        //   int P { get [|{    return;
        // }|]
        // ```
        // 
        // So, during typing, if we see any parent block is incomplete, we'll assume that 
        // all our parent blocks are incomplete and we will place the suppression span like so:
        // 
        // ```c#
        // class C
        // {
        //   int P { get [|{     return;|]
        // }
        // ```
        // 
        // This will have the desired effect of keeping these tokens on the same line, but only during typing scenarios.  
        if (tokenBeforeCaret.Kind() is SyntaxKind.CloseBraceToken or
            SyntaxKind.EndOfFileToken)
        {
            return [];
        }

        return [TypingFormattingRule.Instance];
    }

    private static bool IsEndToken(SyntaxToken endToken)
    {
        if (endToken.IsKind(SyntaxKind.OpenBraceToken))
        {
            return false;
        }

        return true;
    }

    // We'll autoformat on n, t, e, only if they are the last character of the below
    // keywords.  
    private static bool ValidSingleOrMultiCharactersTokenKind(char typedChar, SyntaxKind kind)
        => typedChar switch
        {
            'n' => kind is SyntaxKind.RegionKeyword or SyntaxKind.EndRegionKeyword,
            't' => kind == SyntaxKind.SelectKeyword,
            'e' => kind == SyntaxKind.WhereKeyword,
            _ => true,
        };

    private static bool IsInvalidTokenKind(SyntaxToken token)
    {
        // invalid token to be formatted
        return token.Kind()
                is SyntaxKind.None
                or SyntaxKind.EndOfDirectiveToken
                or SyntaxKind.EndOfFileToken;
    }

    private ImmutableArray<AbstractFormattingRule> GetFormattingRules(ParsedDocument document, int position, SyntaxToken tokenBeforeCaret)
    {
        var formattingRuleFactory = _services.SolutionServices.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
        return
        [
            formattingRuleFactory.CreateRule(document, position),
            .. GetTypingRules(tokenBeforeCaret),
            .. Formatter.GetDefaultFormattingRules(_services),
        ];
    }

    public ImmutableArray<TextChange> GetFormattingChangesOnPaste(ParsedDocument document, TextSpan textSpan, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(document.Root, textSpan);
        var service = _services.GetRequiredService<ISyntaxFormattingService>();

        var result = service.GetFormattingResult(
            document.Root, [formattingSpan], options, [new PasteFormattingRule(), .. service.GetDefaultFormattingRules()], cancellationToken);
        return [.. result.GetTextChanges(cancellationToken)];
    }

    internal sealed class PasteFormattingRule : AbstractFormattingRule
    {
        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (currentToken.Parent != null)
            {
                var currentTokenParentParent = currentToken.Parent.Parent;
                if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
                    (currentTokenParentParent.Kind() == SyntaxKind.SimpleLambdaExpression ||
                     currentTokenParentParent.Kind() == SyntaxKind.ParenthesizedLambdaExpression ||
                     currentTokenParentParent.Kind() == SyntaxKind.AnonymousMethodExpression))
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                }
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }
    }
}
