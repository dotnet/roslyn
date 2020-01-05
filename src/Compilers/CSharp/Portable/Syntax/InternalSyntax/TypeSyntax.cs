// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeSyntax
    {
        public bool IsVar => this is IdentifierNameSyntax name && name.Identifier.ToString() == "var";

        public bool IsUnmanaged => this is IdentifierNameSyntax name && name.Identifier.ToString() == "unmanaged";

        public bool IsNotNull => this is IdentifierNameSyntax name && name.Identifier.ToString() == "notnull";
    }
}
