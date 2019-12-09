// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeSyntax
    {
        public bool IsVar => IsIdentifierName("var");
        public bool IsUnmanaged => IsIdentifierName("unmanaged");
        public bool IsNotNull => IsIdentifierName("notnull");

        private bool IsIdentifierName(string id) => this is IdentifierNameSyntax name && name.Identifier.ToString() == id;
    }
}
