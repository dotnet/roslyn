// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal abstract class AbstractEditorBraceCompletionSessionFactory : ForegroundThreadAffinitizedObject, IEditorBraceCompletionSessionFactory
    {
        protected AbstractEditorBraceCompletionSessionFactory(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        protected abstract bool IsSupportedOpeningBrace(char openingBrace);

        protected abstract IEditorBraceCompletionSession CreateEditorSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken);

        public IEditorBraceCompletionSession TryCreateSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            if (IsSupportedOpeningBrace(openingBrace) &&
                CheckCodeContext(document, openingPosition, openingBrace, cancellationToken))
            {
                return CreateEditorSession(document, openingPosition, openingBrace, cancellationToken);
            }

            return null;
        }

        protected virtual bool CheckCodeContext(Document document, int position, char openingBrace, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // check that the user is not typing in a string literal or comment
            var tree = document.GetSyntaxRootSynchronously(cancellationToken).SyntaxTree;
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            return !syntaxFactsService.IsInNonUserCode(tree, position, cancellationToken);
        }
    }
}
