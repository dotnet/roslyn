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

namespace Microsoft.CodeAnalysis.Snippets
{
    [Export(typeof(IRoslynLSPSnippetExpander))]
    [Export(typeof(RoslynLSPSnippetExpander))]
    internal class RoslynLSPSnippetExpander : IRoslynLSPSnippetExpander
    {
        protected object _lspSnippetExpander;
        protected Type _expanderType;
        protected MethodInfo? _expanderMethodInfo;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynLSPSnippetExpander([Import("Microsoft.VisualStudio.LanguageServer.Client.Snippets.LanguageServerSnippetExpander")] object languageServerSnippetExpander)
        {
            _lspSnippetExpander = languageServerSnippetExpander;
            _expanderType = languageServerSnippetExpander.GetType();
            _expanderMethodInfo = _expanderType.GetMethod("TryExpand");
        }

        public bool TryExpand(TextChange textChange, SourceText sourceText, string? lspSnippetText, ITextView textView, ITextSnapshot textSnapshot)
        {
            if (_expanderMethodInfo is null)
            {
                return false;
            }

            var textEdit = new TextEdit()
            {
                Range = ProtocolConversions.TextSpanToRange(textChange.Span, sourceText),
                NewText = lspSnippetText!
            };

            var expandMethodResult = _expanderMethodInfo.Invoke(_lspSnippetExpander, new object[] { textEdit, textView, textSnapshot });
            return expandMethodResult is not null && (bool)expandMethodResult;
        }

        public bool CanExpandSnippet()
        {
            return _expanderMethodInfo is not null;
        }
    }
}
