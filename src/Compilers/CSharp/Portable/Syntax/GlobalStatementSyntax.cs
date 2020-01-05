// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class GlobalStatementSyntax
    {
        public GlobalStatementSyntax Update(StatementSyntax statement)
            => this.Update(this.AttributeLists, this.Modifiers, statement);
    }
}
