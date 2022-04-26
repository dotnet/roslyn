// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    [Export(typeof(IRoslynLSPSnippetExpander))]
    [Export(typeof(RoslynLSPSnippetExpander))]
    internal class RoslynLSPSnippetExpander : IRoslynLSPSnippetExpander
    {
        protected readonly object? LspSnippetExpander;
        protected readonly Type? ExpanderType;
        protected readonly MethodInfo? ExpanderMethodInfo;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynLSPSnippetExpander(
            [Import("Microsoft.VisualStudio.LanguageServer.Client.Snippets.LanguageServerSnippetExpander", AllowDefault = true)] object? languageServerSnippetExpander)
        {
            LspSnippetExpander = languageServerSnippetExpander;

            if (LspSnippetExpander is not null)
            {
                ExpanderType = LspSnippetExpander.GetType();
                ExpanderMethodInfo = ExpanderType.GetMethod("TryExpand");
            }
        }

        public bool TryExpand(TextSpan textSpan, string? lspSnippetText, ITextView textView, ITextSnapshot textSnapshot)
        {
            Contract.ThrowIfFalse(CanExpandSnippet());

            var textEdit = new TextEdit()
            {
                Range = ProtocolConversions.TextSpanToRange(textSpan, textSnapshot.AsText()),
                NewText = lspSnippetText!
            };

            try
            {
                // ExpanderMethodInfo should not be null at this point.
                var expandMethodResult = ExpanderMethodInfo!.Invoke(LspSnippetExpander, new object[] { textEdit, textView, textSnapshot });
                return expandMethodResult is not null && (bool)expandMethodResult;
            }
            catch
            {
                return false;
            }
        }

        public bool CanExpandSnippet()
        {
            return ExpanderMethodInfo is not null;
        }
    }
}
