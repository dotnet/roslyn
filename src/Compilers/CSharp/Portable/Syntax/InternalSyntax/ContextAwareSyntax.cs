// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class ContextAwareSyntax
    {
        public GlobalStatementSyntax GlobalStatement(StatementSyntax statement)
            => GlobalStatement(attributeLists: default, modifiers: default, statement);
    }
}
