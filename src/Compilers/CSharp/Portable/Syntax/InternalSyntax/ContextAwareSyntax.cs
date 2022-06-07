// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class ContextAwareSyntax
    {
        public GlobalStatementSyntax GlobalStatement(StatementSyntax statement)
            => GlobalStatement(attributeLists: default, modifiers: default, statement);
    }
}
