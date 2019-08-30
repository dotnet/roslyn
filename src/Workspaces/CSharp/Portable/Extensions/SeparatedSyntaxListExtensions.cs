// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SeparatedSyntaxListExtensions
    {
        private static Tuple<List<T>, List<SyntaxToken>> GetNodesAndSeparators<T>(this SeparatedSyntaxList<T> separatedList) where T : SyntaxNode
        {
            Debug.Assert(separatedList.Count == separatedList.SeparatorCount ||
                              separatedList.Count == separatedList.SeparatorCount + 1);

            var nodes = new List<T>(separatedList.Count);
            var separators = new List<SyntaxToken>(separatedList.SeparatorCount);

            for (var i = 0; i < separatedList.Count; i++)
            {
                nodes.Add(separatedList[i]);

                if (i < separatedList.SeparatorCount)
                {
                    separators.Add(separatedList.GetSeparator(i));
                }
            }

            return Tuple.Create(nodes, separators);
        }
    }
}
