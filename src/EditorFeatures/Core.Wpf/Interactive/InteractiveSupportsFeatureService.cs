// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.CodeAnalysis.Shared;
using System;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class InteractiveSupportsFeatureService
    {
        [ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), WorkspaceKinds.Interactive), Shared]
        internal class InteractiveTextBufferSupportsFeatureService : ITextBufferSupportsFeatureService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InteractiveTextBufferSupportsFeatureService()
            {
            }

            private static bool IsActiveLanguageBuffer(ITextBuffer textBuffer)
            {
                var evaluator = (IInteractiveEvaluator)textBuffer.Properties[typeof(IInteractiveEvaluator)];
                var window = evaluator?.CurrentWindow;
                if (window?.CurrentLanguageBuffer == textBuffer)
                {
                    // These are only correct if we're on the UI thread.
                    // Otherwise, they're guesses and they might change immediately even if they're correct.
                    // If we return true and the buffer later becomes readonly, it appears that the 
                    // the code fix simply has no effect.
                    return !window.IsResetting && !window.IsRunning;
                }

                return false;
            }

            public bool SupportsCodeFixes(ITextBuffer textBuffer)
                => IsActiveLanguageBuffer(textBuffer);

            public bool SupportsRefactorings(ITextBuffer textBuffer)
                => false;

            public bool SupportsRename(ITextBuffer textBuffer)
                => false;

            public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
                => true;
        }

        [ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), WorkspaceKinds.Interactive), Shared]
        internal class InteractiveDocumentSupportsFeatureService : IDocumentSupportsFeatureService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InteractiveDocumentSupportsFeatureService()
            {
            }

            public bool SupportsCodeFixes(Document document)
            {
                // TODO: Implement this.
                return false;
            }

            public bool SupportsRefactorings(Document document)
                => false;

            public bool SupportsRename(Document document)
                => false;

            public bool SupportsNavigationToAnyPosition(Document document)
                => true;

            public bool SupportsSemanticSnippets(Document document)
                => false;
        }
    }
}
