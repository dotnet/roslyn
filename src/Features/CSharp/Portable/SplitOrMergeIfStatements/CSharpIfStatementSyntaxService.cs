// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportLanguageService(typeof(IIfStatementSyntaxService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpIfStatementSyntaxService : IIfStatementSyntaxService
    {
        public int IfKeywordKind => (int)SyntaxKind.IfKeyword;

        public int LogicalAndExpressionKind => (int)SyntaxKind.LogicalAndExpression;

        public int LogicalOrExpressionKind => (int)SyntaxKind.LogicalOrExpression;
    }
}
