// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion
{
    [ExportLanguageService(typeof(IEditorBraceCompletionSessionFactory), LanguageNames.CSharp), Shared]
    internal class CSharpEditorBraceCompletionSessionFactory : AbstractEditorBraceCompletionSessionFactory
    {
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly ISmartIndentationService _smartIndentationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorBraceCompletionSessionFactory(IThreadingContext threadingContext, ISmartIndentationService smartIndentationService, ITextBufferUndoManagerProvider undoManager)
            : base(threadingContext)
        {
            _smartIndentationService = smartIndentationService;
            _undoManager = undoManager;
        }

        protected override bool IsSupportedOpeningBrace(char openingBrace)
        {
            switch (openingBrace)
            {
                case BraceCompletionSessionProvider.Bracket.OpenCharacter:
                case BraceCompletionSessionProvider.CurlyBrace.OpenCharacter:
                case BraceCompletionSessionProvider.Parenthesis.OpenCharacter:
                case BraceCompletionSessionProvider.SingleQuote.OpenCharacter:
                case BraceCompletionSessionProvider.DoubleQuote.OpenCharacter:
                case BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter:
                    return true;
            }

            return false;
        }

        protected override bool CheckCodeContext(Document document, int position, char openingBrace, CancellationToken cancellationToken)
        {
            // SPECIAL CASE: Allow in curly braces in string literals to support interpolated strings.
            if (openingBrace == BraceCompletionSessionProvider.CurlyBrace.OpenCharacter &&
                InterpolationCompletionSession.IsContext(document, position, cancellationToken))
            {
                return true;
            }

            if (openingBrace == BraceCompletionSessionProvider.DoubleQuote.OpenCharacter &&
                InterpolatedStringCompletionSession.IsContext(document, position, cancellationToken))
            {
                return true;
            }

            // Otherwise, defer to the base implementation.
            return base.CheckCodeContext(document, position, openingBrace, cancellationToken);
        }

        protected override IEditorBraceCompletionSession CreateEditorSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            return openingBrace switch
            {
                BraceCompletionSessionProvider.CurlyBrace.OpenCharacter => InterpolationCompletionSession.IsContext(document, openingPosition, cancellationToken)
                    ? new InterpolationCompletionSession()
                    : (IEditorBraceCompletionSession)new CurlyBraceCompletionSession(syntaxFactsService, _smartIndentationService, _undoManager),

                BraceCompletionSessionProvider.DoubleQuote.OpenCharacter => InterpolatedStringCompletionSession.IsContext(document, openingPosition, cancellationToken)
                    ? new InterpolatedStringCompletionSession()
                    : (IEditorBraceCompletionSession)new StringLiteralCompletionSession(syntaxFactsService),

                BraceCompletionSessionProvider.Bracket.OpenCharacter => new BracketCompletionSession(syntaxFactsService),
                BraceCompletionSessionProvider.Parenthesis.OpenCharacter => new ParenthesisCompletionSession(syntaxFactsService),
                BraceCompletionSessionProvider.SingleQuote.OpenCharacter => new CharLiteralCompletionSession(syntaxFactsService),
                BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter => new LessAndGreaterThanCompletionSession(syntaxFactsService),
                _ => throw ExceptionUtilities.UnexpectedValue(openingBrace),
            };
        }
    }
}
