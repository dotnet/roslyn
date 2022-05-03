// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        private readonly object? _lspSnippetExpander;
        private readonly Type? _expanderType;
        private readonly MethodInfo? _expanderMethodInfo;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynLSPSnippetExpander(
            [Import("Microsoft.VisualStudio.LanguageServer.Client.Snippets.LanguageServerSnippetExpander", AllowDefault = true)] object? languageServerSnippetExpander)
        {
            _lspSnippetExpander = languageServerSnippetExpander;

            if (_lspSnippetExpander is not null)
            {
                _expanderType = _lspSnippetExpander.GetType();
                _expanderMethodInfo = _expanderType.GetMethod("TryExpand");
            }
        }

        public bool TryExpand(TextSpan textSpan, string lspSnippetText, ITextView textView, ITextSnapshot textSnapshot)
        {
            Contract.ThrowIfFalse(CanExpandSnippet());

            var textEdit = new TextEdit()
            {
                Range = ProtocolConversions.TextSpanToRange(textSpan, textSnapshot.AsText()),
                NewText = lspSnippetText
            };

            try
            {
                // ExpanderMethodInfo should not be null at this point.
                var expandMethodResult = _expanderMethodInfo!.Invoke(_lspSnippetExpander, new object[] { textEdit, textView, textSnapshot });
                if (expandMethodResult is null)
                {
                    throw new Exception("The result of the invoked LSP snippet expander is null.");
                }

                if (!(bool)expandMethodResult)
                {
                    throw new Exception("The invoked LSP snippet expander came back as false.");
                }

                return true;
            }
            catch (Exception e)
            {
                FatalError.ReportAndCatch(e);
                return false;
            }
        }

        public bool CanExpandSnippet()
        {
            return _expanderMethodInfo is not null;
        }
    }
}
