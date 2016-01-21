// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax : StatementSyntax
    {
        public bool IsConst => this.Modifiers.Any(SyntaxKind.ConstKeyword);

        internal LocalDeclarationKind LocalDeclarationKind =>
            IsConst ? LocalDeclarationKind.Constant : LocalDeclarationKind.RegularVariable;
    }
}
