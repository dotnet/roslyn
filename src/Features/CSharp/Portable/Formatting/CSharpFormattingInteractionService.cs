// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportLanguageService(typeof(IFormattingInteractionService), LanguageNames.CSharp), Shared]
    internal partial class CSharpFormattingInteractionService : IFormattingInteractionService
    {
        // All the characters that might potentially trigger formatting when typed
        private readonly char[] _supportedChars = ";{}#nte:)".ToCharArray();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFormattingInteractionService()
        {
        }

        public bool SupportsFormatDocument => true;
        public bool SupportsFormatOnPaste => true;
        public bool SupportsFormatSelection => true;
        public bool SupportsFormatOnReturn => false;

        public bool SupportsFormattingOnTypedCharacter(Document document, AutoFormattingOptions options, char ch)
        {
            var smartIndentOn = options.IndentStyle == FormattingOptions.IndentStyle.Smart;

            // We consider the proper placement of a close curly or open curly when it is typed at
            // the start of the line to be a smart-indentation operation.  As such, even if "format
            // on typing" is off, if "smart indent" is on, we'll still format this.  (However, we
            // won't touch anything else in the block this close curly belongs to.).
            //
            // See extended comment in GetFormattingChangesAsync for more details on this.
            if (smartIndentOn)
            {
                if (ch is '{' or '}')
                {
                    return true;
                }
            }

            // If format-on-typing is not on, then we don't support formatting on any other characters.
            var autoFormattingOnTyping = options.FormatOnTyping;
            if (!autoFormattingOnTyping)
            {
                return false;
            }

            if (ch == '}' && !options.FormatOnCloseBrace)
            {
                return false;
            }

            if (ch == ';' && !options.FormatOnSemicolon)
            {
                return false;
            }

            // don't auto format after these keys if smart indenting is not on.
            if ((ch == '#' || ch == 'n') && !smartIndentOn)
            {
                return false;
            }

            return _supportedChars.Contains(ch);
        }

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            TextSpan? textSpan,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var span = textSpan ?? new TextSpan(0, root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, span);
            if (documentOptions == null)
            {
                var inferredIndentationService = document.Project.Solution.Workspace.Services.GetRequiredService<IInferredIndentationService>();
                documentOptions = await inferredIndentationService.GetDocumentOptionsWithInferredIndentationAsync(document, explicitFormat: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var services = document.Project.Solution.Workspace.Services;
            var formattingOptions = SyntaxFormattingOptions.Create(documentOptions, services, document.Project.Language);
            return Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(formattingSpan), services, formattingOptions, cancellationToken).ToImmutableArray();
        }

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesOnPasteAsync(
            Document document,
            TextSpan textSpan,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, textSpan);
            var service = document.GetRequiredLanguageService<ISyntaxFormattingService>();

            var rules = new List<AbstractFormattingRule>() { new PasteFormattingRule() };
            rules.AddRange(service.GetDefaultFormattingRules());

            if (documentOptions == null)
            {
                var inferredIndentationService = document.Project.Solution.Workspace.Services.GetRequiredService<IInferredIndentationService>();
                documentOptions = await inferredIndentationService.GetDocumentOptionsWithInferredIndentationAsync(document, explicitFormat: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var formattingOptions = SyntaxFormattingOptions.Create(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
            var result = service.GetFormattingResult(root, SpecializedCollections.SingletonEnumerable(formattingSpan), formattingOptions, rules, cancellationToken);
            return result.GetTextChanges(cancellationToken).ToImmutableArray();
        }

        private static IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document, int position, SyntaxToken tokenBeforeCaret)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
            return formattingRuleFactory.CreateRule(document, position).Concat(GetTypingRules(tokenBeforeCaret)).Concat(Formatter.GetDefaultFormattingRules(document));
        }

        Task<ImmutableArray<TextChange>> IFormattingInteractionService.GetFormattingChangesOnReturnAsync(
            Document document, int caretPosition, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<TextChange>();

        private static async Task<bool> TokenShouldNotFormatOnTypeCharAsync(
            SyntaxToken token, CancellationToken cancellationToken)
        {
            // If the token is a )  we only want to format if it's the close paren
            // of a using statement.  That way if we have nested usings, the inner
            // using will align with the outer one when the user types the close paren.
            if (token.IsKind(SyntaxKind.CloseParenToken) && !token.Parent.IsKind(SyntaxKind.UsingStatement))
            {
                return true;
            }

            // If the token is a :  we only want to format if it's a labeled statement
            // or case.  When the colon is typed we'll want ot immediately have those
            // statements snap to their appropriate indentation level.
            if (token.IsKind(SyntaxKind.ColonToken) && !(token.Parent.IsKind(SyntaxKind.LabeledStatement) || token.Parent is SwitchLabelSyntax))
            {
                return true;
            }

            // Only format an { if it is the first token on a line.  We don't want to 
            // mess with it if it's inside a line.
            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                var text = await token.SyntaxTree!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (!token.IsFirstTokenOnLine(text))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            char typedChar,
            int caretPosition,
            DocumentOptionSet? documentOptions,
            CancellationToken cancellationToken)
        {
            // first, find the token user just typed.
            var token = await GetTokenBeforeTheCaretAsync(document, caretPosition, cancellationToken).ConfigureAwait(false);
            if (token.IsMissing ||
                !ValidSingleOrMultiCharactersTokenKind(typedChar, token.Kind()) ||
                token.IsKind(SyntaxKind.EndOfFileToken, SyntaxKind.None))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattingRules = GetFormattingRules(document, caretPosition, token);

            var service = document.GetLanguageService<ISyntaxFactsService>();
            if (service != null && service.IsInNonUserCode(token.SyntaxTree, caretPosition, cancellationToken))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var shouldNotFormat = await TokenShouldNotFormatOnTypeCharAsync(token, cancellationToken).ConfigureAwait(false);
            if (shouldNotFormat)
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var services = document.Project.Solution.Workspace.Services;

            if (documentOptions == null)
            {
                var inferredIndentationService = services.GetRequiredService<IInferredIndentationService>();
                documentOptions = await inferredIndentationService.GetDocumentOptionsWithInferredIndentationAsync(document, explicitFormat: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var options = IndentationOptions.From(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);

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
                (token.IsKind(SyntaxKind.CloseBraceToken) && OnlySmartIndentCloseBrace(options.AutoFormattingOptions)) ||
                (token.IsKind(SyntaxKind.OpenBraceToken) && OnlySmartIndentOpenBrace(options.AutoFormattingOptions));

            if (onlySmartIndent)
            {
                // if we're only doing smart indent, then ignore all edits to this token that occur before
                // the span of the token. They're irrelevant and may screw up other code the user doesn't 
                // want touched.
                var tokenEdits = await FormatTokenAsync(document, options, token, formattingRules, cancellationToken).ConfigureAwait(false);
                return tokenEdits.Where(t => t.Span.Start >= token.FullSpan.Start).ToImmutableArray();
            }

            // if formatting range fails, do format token one at least
            var changes = await FormatRangeAsync(document, options, token, formattingRules, cancellationToken).ConfigureAwait(false);
            if (changes.Length > 0)
            {
                return changes;
            }

            return (await FormatTokenAsync(document, options, token, formattingRules, cancellationToken).ConfigureAwait(false)).ToImmutableArray();
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

        private static async Task<SyntaxToken> GetTokenBeforeTheCaretAsync(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var position = Math.Max(0, caretPosition - 1);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position, findInsideTrivia: true);
            return token;
        }

        private static async Task<IList<TextChange>> FormatTokenAsync(Document document, IndentationOptions options, SyntaxToken token, IEnumerable<AbstractFormattingRule> formattingRules, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formatter = CreateSmartTokenFormatter(options, formattingRules, root);
            var changes = await formatter.FormatTokenAsync(document.Project.Solution.Workspace.Services, token, cancellationToken).ConfigureAwait(false);
            return changes;
        }

        private static ISmartTokenFormatter CreateSmartTokenFormatter(IndentationOptions options, IEnumerable<AbstractFormattingRule> formattingRules, SyntaxNode root)
            => new CSharpSmartTokenFormatter(options, formattingRules, (CompilationUnitSyntax)root);

        private static async Task<ImmutableArray<TextChange>> FormatRangeAsync(
            Document document,
            IndentationOptions options,
            SyntaxToken endToken,
            IEnumerable<AbstractFormattingRule> formattingRules,
            CancellationToken cancellationToken)
        {
            if (!IsEndToken(endToken))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var tokenRange = FormattingRangeHelper.FindAppropriateRange(endToken);
            if (tokenRange == null || tokenRange.Value.Item1.Equals(tokenRange.Value.Item2))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            if (IsInvalidTokenKind(tokenRange.Value.Item1) || IsInvalidTokenKind(tokenRange.Value.Item2))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var formatter = new CSharpSmartTokenFormatter(options, formattingRules, (CompilationUnitSyntax)root);

            var changes = formatter.FormatRange(document.Project.Solution.Workspace.Services, tokenRange.Value.Item1, tokenRange.Value.Item2, cancellationToken);
            return changes.ToImmutableArray();
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
                return SpecializedCollections.EmptyEnumerable<AbstractFormattingRule>();
            }

            return SpecializedCollections.SingletonEnumerable(TypingFormattingRule.Instance);
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
            return token.IsKind(SyntaxKind.None) ||
                   token.IsKind(SyntaxKind.EndOfDirectiveToken) ||
                   token.IsKind(SyntaxKind.EndOfFileToken);
        }
    }
}
