// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class AliasQualifiedNameSyntax : NameSyntax
    {
        // This override is only intended to support cases where a caller has a value statically typed as NameSyntax in hand 
        // and neither knows nor cares to determine whether that name is qualified or not.
        // If a value is statically typed as a AliasQualifiedNameSyntax calling Name directly is preferred.
        internal override SimpleNameSyntax GetUnqualifiedName()
        {
            return this.Name;
        }

        internal override string ErrorDisplayName()
        {
            return Alias.ErrorDisplayName() + "::" + Name.ErrorDisplayName();
        }
    }
}
