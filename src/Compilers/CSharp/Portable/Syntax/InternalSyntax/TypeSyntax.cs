// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeSyntax
    {
        public bool IsVar => this is IdentifierNameSyntax name && name.Identifier.ToString() == "var";

        public bool IsUnmanaged => this is IdentifierNameSyntax name && name.Identifier.ToString() == "unmanaged";

        public bool IsNotNull => this is IdentifierNameSyntax name && name.Identifier.ToString() == "notnull";
    }
}
