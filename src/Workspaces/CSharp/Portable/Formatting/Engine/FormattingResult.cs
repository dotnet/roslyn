// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <summary>
    /// this holds onto changes made by formatting engine.
    /// </summary>
    internal class FormattingResult : AbstractFormattingResult
    {
        internal FormattingResult(TreeData treeInfo, TokenStream tokenStream, TextSpan spanToFormat, TaskExecutor taskExecutor) :
            base(treeInfo, tokenStream, spanToFormat, taskExecutor)
        {
        }

        protected override SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> changeMap, CancellationToken cancellationToken)
        {
            var rewriter = new TriviaRewriter(this.TreeInfo.Root, SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, this.FormattedSpan), changeMap, cancellationToken);
            return rewriter.Transform();
        }
    }
}
