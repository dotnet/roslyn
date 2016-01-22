// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    internal class PasteHandler
    {
        private readonly IWpfTextView view;

        public PasteHandler(IWpfTextView view)
        {
            this.view = view;
        }

        public bool CheckApplicable(ITextBuffer subjectBuffer, CopyData copyData)
        {
            if (copyData == null)
            {
                return false;
            }

            var selection = view.Selection;
            var spans = selection.GetSnapshotSpansOnBuffer(subjectBuffer);
            if (spans.Count() != 1)
            {
                return false;
            }

            var document = subjectBuffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
            if (document == null)
            {
                return false;
            }

            if (document.SourceCodeKind != SourceCodeKind.Regular)
            {
                return false;
            }

            var span = spans.Select(s => new TextSpan(s.Start, s.Length)).First();
            var newSpan = new TextSpan(span.Start, copyData.Text.Length);

            var newDocument = ForkNewDocument(copyData, document, span);

            // analyze in new document
            var newOffsetMap = CopyData.CreateOffsetMap(newDocument, newSpan);

            foreach (var pair in copyData)
            {
                var offset = pair.Key;
                var token = pair.Value.Item1;
                var symbol = pair.Value.Item2;

                // check whether existing symbol is same one
                if (IsSameSymbol(offset, token, symbol, newOffsetMap))
                {
                    continue;
                }

                // missing using, 
            }

            return false;
        }

        private bool IsSameSymbol(int offset, SyntaxToken token, ISymbol symbol, Dictionary<int, Tuple<SyntaxToken, ISymbol>> newOffsetMap)
        {
            if (!newOffsetMap.ContainsKey(offset))
            {
                return false;
            }

            var pair = newOffsetMap[offset];
            var newToken = pair.Item1;
            var newSymbol = pair.Item2;

            // make sure it is pointing to same token
            if (token.Kind() != newToken.Kind() ||
                token.ValueText != newToken.ValueText)
            {
                return false;
            }

            // looks like there is no service exposed to support moving a symbol in one compilation to another.
            return false;
        }

        private static Document ForkNewDocument(CopyData copyData, Document document, TextSpan span)
        {
            // here we assume paste data is what is copied before.
            // we will check this assumption when things are actually pasted.
            var newText = document.GetTextAsync().Result.ToString().Remove(span.Start, span.Length).Insert(span.Start, copyData.Text);

            // fork solution
            return document.WithText(SourceText.From(newText));
        }

        public void Apply(ITextBuffer buffer, CopyData copyData)
        {
            throw new NotImplementedException();
        }
    }
}
