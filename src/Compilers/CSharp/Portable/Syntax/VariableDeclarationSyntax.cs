// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class VariableDeclarationSyntax : CSharpSyntaxNode
    {
        public VariableDeclarationSyntax Update(TypeSyntax type, SeparatedSyntaxList<VariableDeclaratorSyntax> variables)
        {
            return Update(type, variables, null);
        }

        public bool IsDeconstructionDeclaration => Deconstruction != null;
    }
}
