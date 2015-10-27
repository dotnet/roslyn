// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [ExportWorkspaceService(typeof(IDocumentSupportsSuggestionService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveDocumentSupportsCodeFixService : IDocumentSupportsSuggestionService
    {
        public bool SupportsCodeFixes(Document document)
        {
            SourceText sourceText;
            if (document.TryGetText(out sourceText))
            {
                ITextBuffer buffer = sourceText.Container.TryGetTextBuffer();
                if (buffer != null)
                {
                    IInteractiveEvaluator evaluator = (IInteractiveEvaluator)buffer.Properties[typeof(IInteractiveEvaluator)];
                    IInteractiveWindow window = evaluator?.CurrentWindow;
                    if (window?.CurrentLanguageBuffer == buffer)
                    {
                        // These are only correct if we're on the UI thread.
                        // Otherwise, they're guesses and they might change immediately even if they're correct.
                        // If we return true and the buffer later becomes readonly, it appears that the 
                        // the code fix simply has no effect.
                        return !window.IsResetting && !window.IsRunning;
                    }
                }
            }

            return false;
        }

        public bool SupportsRefactorings(Document document)
        {
            return false;
        }

        public bool SupportsRename(Document document)
        {
            return false;
        }

        public bool SupportsNavigationToAnyPosition(Document document)
        {
            return true;
        }
    }
}
