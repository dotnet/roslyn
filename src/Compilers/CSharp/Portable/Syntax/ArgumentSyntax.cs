// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ArgumentSyntax
    {
        public ArgumentSyntax Update(NameColonSyntax nameColon, SyntaxToken refOrOutKeyword, ExpressionSyntax expression)
        {
            return this.Update(nameColon, refOrOutKeyword, expression, type: null, identifier: default(SyntaxToken)) ;
        }

        public ArgumentSyntax Update(NameColonSyntax nameColon, SyntaxToken refOrOutKeyword, TypeSyntax type, SyntaxToken identifier)
        {
            return this.Update(nameColon, refOrOutKeyword, null, type, identifier);
        }
    }
}
