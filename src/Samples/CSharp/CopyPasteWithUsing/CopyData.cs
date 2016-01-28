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
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    internal class CopyData : IEnumerable<KeyValuePair<int, Tuple<SyntaxToken, ISymbol>>>
    {
        private readonly Dictionary<int, Tuple<SyntaxToken, ISymbol>> offsetMap;

        public string Text { get; private set; }

        public CopyData(
            string text,
            Dictionary<int, Tuple<SyntaxToken, ISymbol>> offsetMap)
        {
            this.Text = text;
            this.offsetMap = offsetMap;
        }

        public static Dictionary<int, Tuple<SyntaxToken, ISymbol>> CreateOffsetMap(Document document, TextSpan span)
        {
            var pairList = TokenSymbolPairBuilder.Build(document, span);
            if (pairList == null)
            {
                return null;
            }

            var listWithOffset = from pair in pairList
                                 let offset = pair.Item1.Span.Start - span.Start
                                 select new { Key = offset, Data = pair };

            var offsetMap = new Dictionary<int, Tuple<SyntaxToken, ISymbol>>();
            listWithOffset.Do(keyValue => offsetMap.Add(keyValue.Key, keyValue.Data));

            return offsetMap;
        }

        public IEnumerator<KeyValuePair<int, Tuple<SyntaxToken, ISymbol>>> GetEnumerator()
        {
            return this.offsetMap.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
