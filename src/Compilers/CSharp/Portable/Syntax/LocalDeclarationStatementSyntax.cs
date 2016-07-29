// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax
    {
        public LocalDeclarationStatementSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items)
        {
            return this.WithDeclaration(this.Declaration.WithVariables(this.Declaration.Variables.AddRange(items)));
        }
    }
}
