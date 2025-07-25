// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;

internal static class SyntaxListExtensions
{
    extension<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
    {
        public IReadOnlyList<TNode> AsReadOnlyList()
        {
            return list;
        }
    }

    extension<TNode>(SeparatedSyntaxList<TNode> separatedList) where TNode : SyntaxNode
    {
        public IReadOnlyList<TNode> AsReadOnlyList()
        {
            return separatedList;
        }
    }
}
