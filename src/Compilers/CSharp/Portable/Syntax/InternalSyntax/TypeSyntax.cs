// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeSyntax
    {
        public bool IsVar
        {
            get
            {
                var ts = this as IdentifierNameSyntax;
                return ts != null && ts.Identifier.ToString() == "var";
            }
        }
    }
}
