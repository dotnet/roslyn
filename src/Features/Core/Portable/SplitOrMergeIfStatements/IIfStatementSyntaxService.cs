// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal interface IIfStatementSyntaxService : ILanguageService
    {
        int IfKeywordKind { get; }

        int LogicalAndExpressionKind { get; }

        int LogicalOrExpressionKind { get; }
    }
}
