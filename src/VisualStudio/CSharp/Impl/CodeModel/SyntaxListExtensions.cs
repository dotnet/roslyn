// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal static class SyntaxListExtensions
    {
        public static IReadOnlyList<TNode> AsReadOnlyList<TNode>(this SyntaxList<TNode> list)
            where TNode : SyntaxNode
        {
            return list;
        }

        public static IReadOnlyList<TNode> AsReadOnlyList<TNode>(this SeparatedSyntaxList<TNode> separatedList)
            where TNode : SyntaxNode
        {
            return separatedList;
        }
    }
}
